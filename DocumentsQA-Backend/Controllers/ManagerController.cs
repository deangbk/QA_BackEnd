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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Helpers;

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
				throw new AccessUnauthorizedException("Manager status required");
		}

		// -----------------------------------------------------

		/// <summary>
		/// Grants tranche read access for a user to a project
		/// <para>To grant management rights, see <see cref="AdminController.GrantProjectManagement"/></para>
		/// </summary>
		[HttpPut("grant_access/{tid}/{uid}")]
		public async Task<IActionResult> GrantTrancheAccess(int tid, int uid) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Unauthorized();

			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());   // Horrific
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
		[HttpPut("grant_access_withfile/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> GrantTrancheAccessFromFile(int tid, [FromForm] IFormFile file) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Unauthorized();

			List<int> userIdsGrant = new();
			try {
				using var stream = file.OpenReadStream();
				userIdsGrant = FileHelpers.ReadIntListFromFile(stream);
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
		[HttpDelete("remove_access/{tid}/{uid}")]
		public async Task<IActionResult> RemoveTrancheAccess(int tid, int uid) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Unauthorized();

			if (project.UserManagers.Exists(x => x.Id == uid)) {
				return BadRequest("Cannot remove access: user has elevated rights");
			}

			tranche.UserAccesses.RemoveAll(x => x.Id == uid);

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		/*
		[HttpDelete("remove_access_withfile/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> RemoveTrancheAccessFromFile(int tid, [FromForm] IFormFile file) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!_access.AllowManageProject(project))
				return Unauthorized();

			List<int> userIdsGrant = new();
			try {
				using var stream = file.OpenReadStream();
				userIdsGrant = FileHelpers.ReadIntListFromFile(stream);
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
		[HttpPost("add_project_users/{pid}")]
		[RequestSizeLimit(bytes: 16 * 1024 * 1024)]  // 16MB
		public async Task<IActionResult> AddUsersFromFile(int pid, [FromForm] IFormFile file) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowManageProject(project))
				return Unauthorized();

			DateTime dateCreated = DateTime.Now;

			var fileLines = new List<string>();
			{
				try {
					using var stream = file.OpenReadStream();
					using (var reader = new StreamReader(stream, Encoding.Unicode)) {
						while (!reader.EndOfStream) {
							var line = reader.ReadLine();
							if (line != null && line.Length > 0)
								fileLines.Add(line);
						}
					}
				}
				catch (Exception e) {
					return BadRequest("File open error: " + e.Message);
				}
			}

			Dictionary<string, int> trancheMap = project.Tranches
				.ToDictionary(x => x.Name, x => x.Id);
			List<(string email, string pass, string name, HashSet<int>? tranches)> listUserData = new();

			{
				Random rnd = new Random(DateTime.Now.GetHashCode());
				const string passwordChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
				var _GeneratePassword = (int length) => {
					return string.Concat(Enumerable.Range(0, length)
						.Select(x => passwordChars[rnd.Next() % passwordChars.Length]));
				};

				int iLine = 1;
				try {
					foreach (var line in fileLines) {
						var data = line.Split(',').Select(x => x.Trim()).ToArray();

						string email = data[0];
						string displayName = data[1];

						HashSet<int>? tranches = null;

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

						string randPassword = _GeneratePassword(8);

						listUserData.Add((email, displayName, randPassword, tranches));
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

			List<AppUser> listUsers = new();
			try {
				// Wrap all operations in a transaction so failure would revert the entire thing
				using (var transaction = _dataContext.Database.BeginTransaction()) {
					// Warning: Inefficient
					// If the system is to be scaled in the future, find some way to efficiently bulk-create users
					//	rather than repeatedly awaiting CreateAsync

					foreach (var (email, pass, name, _) in listUserData) {
						var user = new AppUser {
							Email = email,
							UserName = email,
							DisplayName = name,
							Company = project.CompanyName,
							FavouriteProjectId = project.Id,
							DateCreated = dateCreated,
						};
						listUsers.Add(user);

						var result = await _userManager.CreateAsync(user, pass);
						if (!result.Succeeded)
							throw new Exception(email);
					}

					foreach (var iTranche in project.Tranches) {
						var accesses = listUsers
							.Select((user, i) => (user, i))
							.Where(x => {
								var tranches = listUserData[x.i].tranches;
								return tranches == null || tranches.Contains(iTranche.Id);
							})
							.Select(x => x.user)
							.ToArray();
						iTranche.UserAccesses.AddRange(accesses);
					}

					await _dataContext.SaveChangesAsync();
					await transaction.CommitAsync();
				}
			}
			catch (Exception e) {
				return BadRequest("Users create failed: " + e.Message);
			}

			// Return IDs of all created users
			var userIds = listUsers.Select(x => x.Id).ToList();

			return Ok(userIds);
		}
	}
}
