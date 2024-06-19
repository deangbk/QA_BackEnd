using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/user")]
	[Authorize(Policy = "Project_Access")]
	public class UserController : Controller {
		private readonly ILogger<UserController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly UserManager<AppUser> _userManager;

		private readonly AuthHelpers _authHelper;
		private readonly IProjectRepository _repoProject;

		public UserController(
			ILogger<UserController> logger,

			DataContext dataContext, 
			IAccessService access,

			UserManager<AppUser> userManager,

			AuthHelpers authHelper,
			IProjectRepository repoProject)
		{
			_logger = logger;

			_dataContext = dataContext;
			_access = access;

			_userManager = userManager;

			_authHelper = authHelper;
			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		private JsonTable _UserToResTable(AppUser user, int details) {
			var table = user.ToJsonTable(details);

			var trancheAccesses = ProjectHelpers.GetUserTrancheAccessesInProject(
				user, _access.GetProjectID());
			table["tranches"] = trancheAccesses
				.Select(x => x.ToJsonTable(0))
				.ToList();

			return table;
		}

		/// <summary>
		/// Gets self info
		/// </summary>
		[HttpGet("")]
		public async Task<IActionResult> GetUser([FromQuery] int details = 0) {
			int userId = _access.GetUserID();

			AppUser? user = await Queries.GetUserFromId(_dataContext, userId);
			if (user == null)
				return BadRequest("User not found");

			var table = _UserToResTable(user, details);
			return Ok(table);
		}

		/// <summary>
		/// Gets info of a specific user
		/// </summary>
		[HttpGet("{uid}")]
		public async Task<IActionResult> GetUserFromId(int uid, [FromQuery] int details = 0) {
			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			if (uid != _access.GetUserID()) {
				if (!await _authHelper.CanManageUser(user))
					return Forbid();
			}

			var table = _UserToResTable(user, details);
			return Ok(table);
		}

		/// <summary>
		/// Edits a user
		/// </summary>
		[HttpPut("edit/{uid}")]
		public async Task<IActionResult> EditUser(int uid, [FromBody] EditUserDTO edit) {
			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			if (!await _authHelper.CanManageUser(user)) {
				if (uid != _access.GetUserID())
					return Forbid("Unauthorized to edit other users");
				if (edit.Tranches != null)
					return Forbid("Unauthorized to edit own tranche access");
			}

			var project = await _repoProject.GetProjectAsync();

			if (edit.DisplayName != null) {
				user.DisplayName = edit.DisplayName;
			}
			if (edit.Tranches != null) {
				var tranches = project.Tranches
					.Where(x => edit.Tranches.Any(y => x.Name == y))
					.ToList();
				if (tranches.Count == 0) {
					return BadRequest("Illegal to set tranche accesses to empty");
				}

				user.TrancheAccesses = tranches;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Deletes a user
		/// </summary>
		[HttpDelete("{uid}")]
		[Authorize(Policy = "Role_Admin")]
		public async Task<IActionResult> DeleteUser(int uid) {
			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			if (uid == _access.GetUserID())
				return BadRequest("Cannot delete self");

			// DeleteAsync internally saves changes
			await _userManager.DeleteAsync(user);

			return Ok();
		}

		// -----------------------------------------------------


	}
}
