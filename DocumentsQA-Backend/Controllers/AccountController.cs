using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/account")]
	[Authorize]
	public class AccountController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<AccountController> _logger;

		private readonly IAccessService _access;

		public AccountController(DataContext dataContext, ILogger<AccountController> logger, IAccessService access) {
			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();
		}

		// -----------------------------------------------------

		[HttpGet("{aid}")]
		public async Task<IActionResult> GetAccount(int aid, [FromQuery] int details = 0) {
			Account? account = await Queries.GetAccountFromId(_dataContext, aid);
			if (account == null)
				return BadRequest("Account not found");
			if (!_access.AllowToTranche(account.Tranche))
				return Unauthorized();

			return Ok(account.ToJsonTable(details));
		}

		[HttpPost("{pid}")]
		public async Task<IActionResult> CreateAccount(int pid, [FromBody] CreateAccountDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.IsAdmin())
				return Unauthorized();

			Tranche? tranche = project.Tranches.Find(x => x.Name == dto.Name);
			if (tranche == null)
				return BadRequest("Tranche not found");

			bool bExist = await _dataContext.Accounts.AnyAsync(x =>
				x.ProjectId == project.Id 
				&& x.AccountNo == dto.Number && x.AccountName == dto.Name);
			if (bExist)
				return BadRequest("Account already exists");

			Account account = new() {
				TrancheId = tranche.Id,
				AccountNo = dto.Number!.Value,
				AccountName = dto.Name,
			};

			_dataContext.Accounts.Add(account);
			await _dataContext.SaveChangesAsync();

			return Ok(project.Id);
		}

		[HttpPut("edit/{aid}")]
		public async Task<IActionResult> EditAccount(int aid, [FromBody] EditAccountDTO dto) {
			Account? account = await Queries.GetAccountFromId(_dataContext, aid);
			if (account == null)
				return BadRequest("Account not found");
			if (!_access.IsAdmin())
				return Unauthorized();

			Project project = account.Project;
			Tranche tranche = account.Tranche;

			if (dto.Tranche != null) {
				// Reparent to another tranche

				Tranche? newTranche = project.Tranches.Find(x => x.Name == dto.Name);
				if (newTranche == null)
					return BadRequest("Tranche not found");

				tranche = newTranche;
				account.TrancheId = newTranche.Id;
			}
			if (dto.Number != null) {
				// Edit number, duplicates not allowed (in the entire project)

				bool bExist = await _dataContext.Accounts.AnyAsync(x => 
					x.ProjectId == project.Id && x.AccountNo == dto.Number);
				if (bExist)
					return BadRequest("Account number already exists");

				account.AccountNo = dto.Number.Value;
			}
			if (dto.Name != null) {
				// Edit name, duplicates not allowed (in the entire project)

				bool bExist = await _dataContext.Accounts.AnyAsync(x =>
					x.ProjectId == project.Id && x.AccountName == dto.Name);
				if (bExist)
					return BadRequest("Account name already exists");

				account.AccountName = dto.Name;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{aid}")]
		public async Task<IActionResult> DeleteAccount(int aid) {
			Account? account = await Queries.GetAccountFromId(_dataContext, aid);
			if (account == null)
				return BadRequest("Account not found");
			if (!_access.IsAdmin())
				return Unauthorized();

			_dataContext.Accounts.Remove(account);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}
	}
}
