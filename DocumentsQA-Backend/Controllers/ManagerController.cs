using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	[Route("api/manage")]
	[Authorize]
	public class ManagerController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<ManagerController> _logger;

		private readonly UserManager<AppUser> _userManager;
		private readonly IAccessService _access;

		public ManagerController(DataContext dataContext, ILogger<ManagerController> logger,
			UserManager<AppUser> userManager, IAccessService access) {

			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
			_access = access;

			if (!_access.IsSuperUser())
				throw new AccessForbiddenException("Manager access required");
		}

		// -----------------------------------------------------

		/// <summary>
		/// Grants tranche read access for a user to a project
		/// <para>To grant management rights, see <see cref="AdminController.GrantProjectManagement"/></para>
		/// </summary>
		[HttpPut("grant/access/{tid}/{uid}")]
		public async Task<IActionResult> GrantTrancheAccess(int tid, int uid) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Forbid();

			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			if (!tranche.UserAccesses.Exists(x => x.Id == uid))
				tranche.UserAccesses.Add(user);

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		/// <summary>
		/// Grants tranche read access for a group of users to a project
		/// <para>To grant management rights, see <see cref="AdminController.GrantProjectManagementFromFile"/></para>
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
		[HttpPut("grant/access/file/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> GrantTrancheAccessFromFile(int tid, [FromForm] IFormFile file) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Forbid();

			List<int> userIdsGrant;
			try {
				string contents = await FileHelpers.ReadIFormFile(file);
				userIdsGrant = ValueHelpers.SplitIntString(contents).ToList();
			}
			catch (Exception e) {
				return BadRequest("File parse error: " + e.Message);
			}

			var dbSetTranche = _dataContext.Set<EJoinClass>("TrancheUserAccess");
			dbSetTranche.AddRange(AdminController.ExcludeExistingTrancheAccess(
				tranche, userIdsGrant));

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		/// <summary>
		/// Removes user tranche read access
		/// <para>To remove management rights, see <see cref="AdminController.RemoveProjectManagement"/></para>
		/// </summary>
		[HttpDelete("ungrant/access/{tid}/{uid}")]
		public async Task<IActionResult> RemoveTrancheAccess(int tid, int uid) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Forbid();

			if (project.UserManagers.Exists(x => x.Id == uid)) {
				return BadRequest("Cannot remove access: user has elevated rights");
			}

			tranche.UserAccesses.RemoveAll(x => x.Id == uid);

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		/*
		[HttpDelete("ungrant/access/file/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> RemoveTrancheAccessFromFile(int tid, [FromForm] IFormFile file) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Forbid();

			List<int> userIdsGrant = new();
			try {
				string contents = await FileHelpers.ReadIFormFile(file);
				userIdsGrant = ValueHelpers.SplitIntString(contents).ToList();
			}
			catch (Exception e) {
				return BadRequest("File parse error: " + e.Message);
			}

			if (!project.UserManagers.Exists(x => userIdsGrant.Any(y => y == x.Id))) {
				tranche.UserAccesses.RemoveAll(x => userIdsGrant.Any(y => y == x.Id));
			}
			else {
				return BadRequest("Cannot remove access: one or more users have elevated rights");
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		*/

		// -----------------------------------------------------

		private class _TmpUserData {
			public AppUser? User { get; set; } = null;
			public string Email { get; set; } = string.Empty;
			public string Password { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
			public string Company { get; set; } = string.Empty;
			public HashSet<int>? Tranches { get; set; }
		}

		private async Task _AddUsersIntoDatabase(List<_TmpUserData> users, Project project) {
			int projectId = project.Id;
			DateTime date = DateTime.Now;

			// Wrap all operations in a transaction so failure would revert the entire thing
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				// Warning: Inefficient
				// If the system is to be scaled in the future, find some way to efficiently bulk-create users
				//	rather than repeatedly awaiting CreateAsync

				foreach (var u in users) {
					string actualEmail = AuthHelpers.ComposeUsername(projectId, u.Email);

					var user = new AppUser {
						Email = actualEmail,
						UserName = actualEmail,
						DisplayName = u.Name,
						Company = u.Company,
						DateCreated = date,
					};
					u.User = user;

					var result = await _userManager.CreateAsync(user, u.Password);
					if (!result.Succeeded)
						throw new Exception(u.Email);

					// Set user role
					await AppRole.AddRoleToUser(_userManager, user, AppRole.User);
				}

				foreach (var iTranche in project.Tranches) {
					var accesses = users
						.Where(x => x.Tranches == null || x.Tranches.Contains(iTranche.Id))
						.Select(x => x.User!)
						.ToArray();
					iTranche.UserAccesses.AddRange(accesses);
				}

				await _dataContext.SaveChangesAsync();
				await transaction.CommitAsync();
			}
		}

		/// <summary>
		/// Creates new user in bulk, with access to specific tranches of a project
		/// <para>Each line is a user data; email, display name [, tranches access...]</para>
		/// <para>A user might also be created without any initial tranche access</para>
		/// <para>Putting "*" as the tranche access will give access to all of the project's tranches</para>
		/// <example>
		/// Example file structure is:
		/// <code>
		///		aaaa@email.com, Maria, TrancheA
		///		bbbb@email.com, hhhhhhhhhhh, TrancheA, TrancheB, TrancheC
		///		cccc@email.com, M*rjorie T*ylor Gr*ene
		///		dddd@email.com, asdkajfhsd, TrancheB
		///		zzzz@fbc.us.gov, Jesse, *
		/// </code>
		/// </example>
		/// </summary>
		[HttpPost("bulk/create_user/{pid}")]
		[RequestSizeLimit(bytes: 16 * 1024 * 1024)]  // 16MB
		public async Task<IActionResult> AddUsersFromFile(int pid, [FromForm] IFormFile file) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			List<string> fileLines;
			{
				try {
					string contents = await FileHelpers.ReadIFormFile(file);
					fileLines = contents.SplitLines();
				}
				catch (Exception e) {
					return BadRequest("File open error: " + e.Message);
				}
			}

			DateTime date = DateTime.Now;
			string projectCompany = project.CompanyName;

			List<_TmpUserData> listUser = new();
			{
				Dictionary<string, int> trancheMap = project.Tranches
					.ToDictionary(x => x.Name, x => x.Id);

				var rnd = new Random(date.GetHashCode());

				int iLine = 1;
				try {
					foreach (var line in fileLines) {
						var data = line.Split(',').Select(x => x.Trim()).ToArray();

						string email = data[0];
						string displayName = data[1];

						HashSet<int>? tranches;

						if (data.Length > 2) {
							// * means all tranches, represented with a null, because this is unfortunately not Rust
							if (data.Length == 3 && data[2] == "*") {
								tranches = null;
							}
							else {
								// Collect all tranches after as varargs-like
								tranches = data.Skip(2)
									.Select(x => trancheMap[x])
									.ToHashSet();
							}
						}
						else {
							// No access to any tranche -> empty set
							tranches = new();
						}

						listUser.Add(new _TmpUserData {
							Email = email,
							Password = AuthHelpers.GeneratePassword(rnd, 8),
							Name = displayName,
							Company = projectCompany,
							Tranches = tranches,
						});

						++iLine;
					}
				}
				catch (Exception e) {
					if (e is KeyNotFoundException) {
						return BadRequest($"Tranche not found in project at line=\"{iLine}\"");
					}
					return BadRequest($"File parse error [line={iLine}]: " + e.Message);
				}
			}

			try {
				await _AddUsersIntoDatabase(listUser, project);
			}
			catch (Exception e) {
				return BadRequest("Users create failed: " + e.Message);
			}

			// Return data all created users
			var userInfos = listUser
				.Select(x => new {
					id = x.User!.Id,
					user = x.Email,
					pass = x.Password,
				})
				.ToList();

			return Ok(userInfos);
		}

		[HttpPost("bulk/create_user/json/{pid}")]
		public async Task<IActionResult> AddUsers(int pid, [FromBody] List<AddUserDTO> dtos) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			DateTime date = DateTime.Now;
			string projectCompany = project.CompanyName;

			List<_TmpUserData> listUser = new();
			{
				Dictionary<string, int> trancheMap = project.Tranches
					.ToDictionary(x => x.Name, x => x.Id);

				var rnd = new Random(date.GetHashCode());

				try {
					foreach (var user in dtos) {
						HashSet<int>? tranches;

						// null means all tranches
						if (user.Tranches != null) {
							// No access to any tranche -> empty set

							tranches = user.Tranches
								.Select(x => trancheMap[x.Trim()])
								.ToHashSet();
						}
						else {
							tranches = null;
						}

						listUser.Add(new _TmpUserData {
							Email = user.Email,
							Password = AuthHelpers.GeneratePassword(rnd, 8),
							Name = user.Name,
							Company = user.Company ?? projectCompany,
							Tranches = tranches,
						});
					}
				}
				catch (KeyNotFoundException e) {
					return BadRequest($"Tranche not found in project: \"{e.Message}\"");
				}
			}

			try {
				await _AddUsersIntoDatabase(listUser, project);
			}
			catch (Exception e) {
				return BadRequest("Users create failed: " + e.Message);
			}

			// Return data all created users
			var userInfos = listUser
				.Select(x => new {
					id = x.User!.Id,
					user = x.Email,
					pass = x.Password,
				})
				.ToList();

			return Ok(userInfos);
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets project questions as paginated list
		/// <para>Valid filters for filterDTO:</para>
		/// <list type="bullet">
		///		<item>TicketID</item>
		///		<item>PosterID</item>
		///		<item>Tranche</item>
		///		<item>Account</item>
		///		<item>PostedFrom</item>
		///		<item>PostedTo</item>
		///		<item>OnlyAnswered</item>
		///		<item>SearchTerm</item>
		/// </list>
		/// </summary>
		[HttpPost("post/{pid}")]
		public async Task<IActionResult> GetPosts(int pid, [FromBody] PostGetFilterDTO filterDTO, [FromQuery] int details = 0) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			IQueryable<Question> query;
			{
				switch (filterDTO.Approved) {
					case true:
						query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);		// Gets only approved
						break;
					case false:
						query = Queries.GetUnapprovedQuestionsQuery(_dataContext, pid);		// Gets only unapproved
						break;
					case null:
						query = _dataContext.Questions
							.Where(x => x.ProjectId == pid);
						break;
				}
			}

			try {
				query = PostHelpers.FilterQuery(query, filterDTO);
			}
			catch (ArgumentException e) {
				ModelState.AddModelError(e.ParamName!, e.Message);
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			var listPosts = await query.ToListAsync();
			var listPostTables = listPosts.Select(x => x.ToJsonTable(details));

			return Ok(listPostTables);
		}
        }
}
