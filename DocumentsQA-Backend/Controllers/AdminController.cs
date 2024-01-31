using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

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
				throw new AccessUnauthorizedException("Admin status required");
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
		[HttpPut("grant_role/{uid}/{role}")]
		public async Task<IActionResult> GrantUserRole(int uid, string role) {
			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());	// Horrific
			if (user == null)
				return BadRequest("User not found");

			AppRole? roleFind = await _roleManager.FindByNameAsync(role);
			if (roleFind == null)
				return BadRequest("Role not found");

			await _GrantUserRole(user, roleFind);

			return Ok();
		}
		private async Task _GrantUserRole(AppUser user, AppRole role) {
			var roleExists = await _userManager.IsInRoleAsync(user, role.Name);
			if (!roleExists) {
				await _userManager.AddClaimAsync(user, new Claim("role", role.Name));
				await _userManager.AddToRoleAsync(user, role.Name);
			}
		}

		/// <summary>
		/// Removes a role from a user
		/// </summary>
		[HttpDelete("remove_role/{uid}/{role}")]
		public async Task<IActionResult> RemoveUserRole(int uid, string role) {
			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());	// Horrific
			if (user == null)
				return BadRequest("User not found");

			AppRole? roleFind = await _roleManager.FindByNameAsync(role);
			if (roleFind == null)
				return BadRequest("Role not found");

			var claims = await _userManager.GetClaimsAsync(user);
			var roleClaims = claims
				.Where(x => x.ValueType == "role")
				.Where(x => x.Value == role);

			await _userManager.RemoveClaimsAsync(user, roleClaims);
			await _userManager.RemoveFromRoleAsync(user, role);

			return Ok();
		}

		// -----------------------------------------------------

		[HttpPost("create_project")]
		public async Task<IActionResult> CreateProject([FromForm] CreateProjectDTO dto) {
			Project project = new Project {
				Name = dto.Name,
				DisplayName = dto.Name,
				CompanyName = dto.Company,
				ProjectStartDate = dto.DateStart,
				ProjectEndDate = dto.DateEnd,
				LastEmailSentDate = DateTime.MinValue,
			};

			List<string> tranches;
			try {
				tranches = dto.InitialTranches
					.Split(",")
					.Select(x => x.Trim().Truncate(16))
					.Where(x => x.Length > 0)
					.ToList();
			}
			catch (Exception) {
				return BadRequest("Tranches: incorrect input format");
			}

			// Wrap all operations in a transaction so failure would revert the entire thing
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				_dataContext.Projects.Add(project);
				await _dataContext.SaveChangesAsync();

				project.Tranches = tranches.Select(x => new Tranche {
					ProjectId = project.Id,
					Name = x,
				}).ToList();
				await _dataContext.SaveChangesAsync();

				await transaction.CommitAsync();
			}

			return Ok(project.Id);
		}

		// -----------------------------------------------------

		public static IEnumerable<EJoinClass> ExcludeExistingTrancheAccess(Tranche tranche, IEnumerable<int> userIds) {
			// Remove all IDs that already have access to the tranche
			return userIds
				.Except(tranche.UserAccesses.Select(x => x.Id))
				.Select(x => EJoinClass.TrancheUser(tranche.Id, x));
		}
		public static IEnumerable<EJoinClass> ExcludeExistingProjectManage(Project project, IEnumerable<int> userIds) {
			// Remove all IDs that already have access to the project
			return userIds
				.Except(project.UserManagers.Select(x => x.Id))
				.Select(x => EJoinClass.ProjectUser(project.Id, x));
		}

		/// <summary>
		/// Grants project management rights to a user
		/// <para>Also grants the manager role to the user if they're not already one</para>
		/// <para>To grant simply read access, see <see cref="ManagerController.GrantTrancheAccess"/></para>
		/// </summary>
		[HttpPut("grant_manage/{pid}/{uid}")]
		public async Task<IActionResult> GrantProjectManagement(int pid, int uid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());	// Horrific
			if (user == null)
				return BadRequest("User not found");

			// Make the user a manager if they're not already one
			await _GrantUserRole(user, AppRole.Manager);

			// Grant elevated access (manager) to all tranches in the project
			{
				// Add access for each tranche in project
				foreach (var i in project.Tranches) {
					if (!i.UserAccesses.Exists(x => x.Id == uid))
						i.UserAccesses.Add(user);
				}
				// Add to project manager
				if (!project.UserManagers.Exists(x => x.Id == uid))
					project.UserManagers.Add(user);
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
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
		[HttpPut("grant_manage_withfile/{pid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]	// 4MB
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

			// Verify user IDs
			{
				var userIdsExist = await _dataContext.Users
					.Where(x => userIds.Any(y => y == x.Id))
					.Select(x => x.Id)
					.ToListAsync();
				if (userIdsExist.Count != userIds.Count) {
					var invalidUsers = userIds.Except(userIdsExist);
					return BadRequest("Users don't exist: " + string.Join(", ", invalidUsers));
				}
			}

			// Make the users managers if they're not already one
			foreach (var id in userIds) {
				AppUser? user = await _userManager.FindByIdAsync(id.ToString());	// Horrific
				await _GrantUserRole(user, AppRole.Manager);
			}

			// Grant elevated access (manager) to all tranches in the project
			{
				var dbSetTranche = _dataContext.Set<EJoinClass>("TrancheUserAccess");
				var dbSetManager = _dataContext.Set<EJoinClass>("ProjectUserManage");

				// Add access for each tranche in project
				foreach (var i in project.Tranches) {
					dbSetTranche.AddRange(ExcludeExistingTrancheAccess(i, userIds));
				}
				// Add to project manager
				dbSetManager.AddRange(ExcludeExistingProjectManage(project, userIds));
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		/// <summary>
		/// Removes project management rights from a user, also removes all tranche read access
		/// <para>Does not remove the user's manager role</para>
		/// </summary>
		[HttpDelete("remove_manage/{pid}/{uid}")]
		public async Task<IActionResult> RemoveProjectManagement(int pid, int uid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			// Remove access from all tranches
			foreach (var i in project.Tranches) {
				i.UserAccesses.RemoveAll(x => x.Id == uid);
			}
			// Remove manage access for the project
			project.UserManagers.RemoveAll(x => x.Id == uid);

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		/*
		[HttpDelete("remove_manage_withfile/{pid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]	// 4MB
		public async Task<IActionResult> RemoveProjectManagementFromFile(int pid, [FromForm] IFormFile file) {
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

			// Remove access from all tranches
			foreach (var i in project.Tranches) {
				i.UserAccesses.RemoveAll(x => userIds.Any(y => y == x.Id));
			}
			// Also remove manage access for the project
			project.UserManagers.RemoveAll(x => userIds.Any(y => y == x.Id));

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		*/
	}
}
