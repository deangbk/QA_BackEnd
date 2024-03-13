using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	[Route("api/admin")]
	[ApiController]
	[Authorize]
	public class AdminController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<PostController> _logger;

		private readonly IAccessService _access;

		private readonly UserManager<AppUser> _userManager;
		private readonly RoleManager<AppRole> _roleManager;

		public AdminController(DataContext dataContext, ILogger<PostController> logger, IAccessService access,
			UserManager<AppUser> userManager, RoleManager<AppRole> roleManager) {

			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			_userManager = userManager;
			_roleManager = roleManager;

			if (!_access.IsAdmin())
				throw new AccessForbiddenException("Admin access required");
		}

		// -----------------------------------------------------

		/// <summary>
		/// Generates default roles:
		/// <list type="bullet">
		///		<item>"user"</item>
		///		<item>"manager"</item>
		///		<item>"admin"</item>
		/// </list>
		/// </summary>
		[HttpPut("gen_roles")]
		public async Task<IActionResult> CreateDefaultRoles() {
			AppRole[] roles = new[] {
				AppRole.User, AppRole.Manager, AppRole.Admin };

			foreach (var role in roles) {
				string roleName = role.Name;
				bool roleExists = await _roleManager.RoleExistsAsync(roleName);
				if (!roleExists) {
					await _roleManager.CreateAsync(role);
				}
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/*
		[HttpPut("create_role/{role}")]
		public async Task<IActionResult> CreateRole(string role) {
			var roleExists = await _roleManager.RoleExistsAsync(role);
			if (roleExists)
				return Ok();

			var result = await _roleManager.CreateAsync(new AppRole(role));
			if (result.Succeeded)
				return Ok();
			else
				return BadRequest(result.Errors);
		}
		*/

		/// <summary>
		/// Grants a role to a user
		/// </summary>
		[HttpPut("grant/role/{uid}/{role}")]
		public async Task<IActionResult> GrantUserRole(int uid, string role) {
			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			AppRole? roleFind = await _roleManager.FindByNameAsync(role);
			if (roleFind == null)
				return BadRequest("Role not found");

			await AdminHelpers.GrantUserRole(_userManager, user, roleFind);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Removes a role from a user
		/// </summary>
		[HttpDelete("ungrant/role/{uid}/{role}")]
		public async Task<IActionResult> RemoveUserRole(int uid, string role) {
			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			AppRole? roleFind = await _roleManager.FindByNameAsync(role);
			if (roleFind == null)
				return BadRequest("Role not found");

			await AdminHelpers.RemoveUserRole(_userManager, user, roleFind);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Grants project management rights to users
		/// <para>Also grants the manager role to the user if they're not already one</para>
		/// <para>To grant simply read access, see <see cref="ManagerController.GrantTrancheAccess"/></para>
		/// </summary>
		[HttpPut("grant/manage/{pid}")]
		public async Task<IActionResult> GrantProjectManagements(int pid, [FromBody] List<int> users) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			int rowsAdded;
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				await AdminHelpers.MakeProjectManagers(_dataContext, _userManager,
					project, users);

				rowsAdded = await _dataContext.SaveChangesAsync();
				await transaction.CommitAsync();
			}

			return Ok(rowsAdded);
		}

		/// <summary>
		/// Grants project management rights to a group of users
		/// <para>Also grants the manager role to the users if they're not already one</para>
		/// <para>To grant simply read access, see <see cref="ManagerController.GrantTrancheAccessFromFile"/></para>
		/// <example>
		/// Example file structure is (newline is interpreted as a comma):
		/// <code>
		///		100, 101, 102
		///		103
		///		104, 105
		///		110, 120, 1111
		/// </code>
		/// </example>
		/// </summary>
		[HttpPut("grant/manage/file/{pid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> GrantProjectManagementFromFile(int pid, [FromForm] IFormFile file) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			List<int> userIds = new();
			try {
				string contents = await FileHelpers.ReadIFormFile(file);
				userIds = ValueHelpers.SplitIntString(contents).ToList();
			}
			catch (Exception e) {
				return BadRequest("File parse error: " + e.Message);
			}

			int rowsAdded;
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				await AdminHelpers.MakeProjectManagers(_dataContext, _userManager,
					project, userIds);

				rowsAdded = await _dataContext.SaveChangesAsync();
				await transaction.CommitAsync();
			}

			return Ok(rowsAdded);
		}

		/// <summary>
		/// Removes project management rights from a user, also removes all tranche read access
		/// <para>Does not remove the user's manager role</para>
		/// </summary>
		[HttpDelete("ungrant/manage/{pid}/{uid}")]
		public async Task<IActionResult> RemoveProjectManagement(int pid, int uid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			AdminHelpers.ClearUsersTrancheAccess(_dataContext, new List<int> { uid });
			AdminHelpers.RemoveProjectManagers(_dataContext, pid, new List<int> { uid });

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
	}
}
