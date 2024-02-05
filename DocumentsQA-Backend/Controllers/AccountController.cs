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

		[HttpPost("create_account/{pid}")]
		public async Task<IActionResult> CreateAccount(int pid, [FromBody] CreateAccountDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.IsAdmin())
				return Unauthorized();

			Tranche? tranche = project.Tranches.Find(x => x.Name == dto.Name);
			if (tranche == null)
				return BadRequest("Tranche not found");

			bool bExist = await _dataContext.Accounts.AnyAsync(
				x => x.AccountName == dto.Name || x.AccountNo == dto.Number);
			if (bExist)
				return BadRequest("Account already exists");

			Account account = new Account {
				TrancheId = tranche.Id,
				AccountNo = dto.Number,
				AccountName = dto.Name,
			};

			_dataContext.Accounts.Add(account);
			await _dataContext.SaveChangesAsync();

			return Ok(project.Id);
		}
	}
}
