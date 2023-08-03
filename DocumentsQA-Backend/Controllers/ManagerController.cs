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
	//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsStaff")]
	public class ManagerController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<ManagerController> _logger;

		private readonly UserManager<AppUser> _userManager;
		private readonly AccessService _access;

		public ManagerController(DataContext dataContext, ILogger<ManagerController> logger,
			UserManager<AppUser> userManager, AccessService access) {

			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
			_access = access;
		}

		// -----------------------------------------------------

		[HttpPut("grant_access/{tid}/{uid}")]
		public async Task<IActionResult> GrantTrancheAccess(int tid, int uid) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!await _access.AllowManageProject(project))
				return Unauthorized();

			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());   // Horrific
			if (user == null)
				return BadRequest("User not found");

			if (!tranche.UserAccesses.Exists(x => x.Id == uid))
				tranche.UserAccesses.Add(user);

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		[HttpPut("grant_access_withfile/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> GrantTrancheAccessFromFile(int tid, [FromForm] IFormFile file) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!await _access.AllowManageProject(project))
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

		[HttpDelete("remove_access/{tid}/{uid}")]
		public async Task<IActionResult> RemoveTrancheAccess(int tid, int uid, [FromQuery] bool elevated) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!await _access.AllowManageProject(project))
				return Unauthorized();

			if (!project.UserManagers.Exists(x => x.Id == uid)) {
				tranche.UserAccesses.RemoveAll(x => x.Id == uid);
			}
			else {
				return BadRequest("Cannot remove access: user has elevated rights");
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		[HttpDelete("remove_access_withfile/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> RemoveTrancheAccessFromFile(int tid, [FromForm] IFormFile file) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!await _access.AllowManageProject(project))
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

		// -----------------------------------------------------

		[HttpPost("add_project_users/{pid}")]
		[RequestSizeLimit(bytes: 16 * 1024 * 1024)]  // 16MB
		public async Task<IActionResult> AddUsersFromFile(int pid, [FromForm] IFormFile file) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!await _access.AllowManageProject(project))
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
			List<(string email, string pass, string name, HashSet<int> tranches)> listUserData = new();

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

						HashSet<int> tranches = new();
						if (data.Length > 2) {
							tranches = data[2].Split(',')
								.Select(x => trancheMap[x])
								.ToHashSet();
						}

						string randPassword = _GeneratePassword(8);

						listUserData.Add((email, displayName, randPassword, tranches));
						++iLine;
					}
				}
				catch (Exception e) {
					return BadRequest($"File parse error [line={iLine}]: " + e.Message);
				}
			}

			List<AppUser> listUsers = new();
			try {
				using (var transaction = _dataContext.Database.BeginTransaction()) {
					// Warning: Inefficient
					// If the system is to be scaled in the future, find some way to efficiently bulk-create users
					//	rather than repeatedly awaiting CreateAsync

					foreach (var i in listUserData) {
						var user = new AppUser {
							Email = i.email,
							UserName = i.email,
							DisplayName = i.name,
							Company = project.CompanyName,
							FavouriteProjectId = project.Id,
							DateCreated = dateCreated,
						};
						listUsers.Add(user);

						var result = await _userManager.CreateAsync(user, i.pass);
						if (!result.Succeeded)
							throw new Exception(i.email);
					}

					foreach (var iTranche in project.Tranches) {
						var accesses = listUsers
							.Select((user, i) => (user, i))
							.Where(x => listUserData[x.i].tranches.Contains(iTranche.Id))
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

			return Ok(listUserData.Count);
		}
	}
}
