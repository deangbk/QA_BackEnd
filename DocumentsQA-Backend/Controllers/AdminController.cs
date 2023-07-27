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

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/admin")]
	[ApiController]
	//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
	public class AdminController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<PostController> _logger;

		private readonly UserManager<AppUser> _userManager;
		private readonly RoleManager<AppRole> _roleManager;

		public AdminController(DataContext dataContext, ILogger<PostController> logger, 
			UserManager<AppUser> userManager, RoleManager<AppRole> roleManager) {

			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
			_roleManager = roleManager;
		}

		// -----------------------------------------------------

		[HttpPut("gen_roles")]
		[AllowAnonymous]
		public async Task<IActionResult> CreateDefaultRoles() {
			string[] roles = new[] { AppRole.User, AppRole.Manager, AppRole.Admin };

			foreach (var roleName in roles) {
				bool roleExists = await _roleManager.RoleExistsAsync(roleName);
				if (!roleExists) {
					var role = new AppRole(roleName);
					await _roleManager.CreateAsync(role);
				}
			}

			return Ok();
		}

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

		[HttpPut("grant_role/{uid}/{role}")]
		public async Task<IActionResult> GrantUserRole(int uid, string role) {
			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());	// Horrific
			if (user == null)
				return BadRequest("User not found");

			AppRole? roleFind = await _roleManager.FindByNameAsync(role);
			if (roleFind == null)
				return BadRequest("Role not found");

			var claims = await _userManager.GetClaimsAsync(user);
			var roleExists = claims
				.Where(x => x.ValueType == "role")
				.Any(x => x.Value == role);

			if (!roleExists) {
				await _userManager.AddClaimAsync(user, new Claim("role", role));
				await _userManager.AddToRoleAsync(user, role);
			}

			return Ok();
		}
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

		private IEnumerable<EJoinClass> _ExcludeAccess_Tranche(Tranche tranche, IEnumerable<int> userIds) {
			// Remove all IDs that already have access to the tranche
			return userIds
				.Except(tranche.UserAccesses.Select(x => x.Id))
				.Select(x => EJoinClass.TrancheUser(tranche.Id, x));
		}
		private IEnumerable<EJoinClass> _ExcludeAccess_Project(Project project, IEnumerable<int> userIds) {
			// Remove all IDs that already have access to the project
			return userIds
				.Except(project.UserManagers.Select(x => x.Id))
				.Select(x => EJoinClass.ProjectUser(project.Id, x));
		}

		private List<int> _ReadIntListFromFile(IFormFile file) {
			List<int> res = new();
			using (var stream = file.OpenReadStream()) {
				using var reader = new StreamReader(stream, Encoding.ASCII);

				var lines = new List<string>();
				while (!reader.EndOfStream) {
					var line = reader.ReadLine();
					if (line != null && line.Length > 0)
						lines.Add(line);
				}

				foreach (var line in lines) {
					var data = line.Split(',').Select(x => int.Parse(x.Trim()));
					res.AddRange(data);
				}
			}
			return res;
		}

		[HttpPut("grant_access/{tid}/{uid}")]
		public async Task<IActionResult> GrantTrancheAccess(int tid, int uid, [FromRoute] bool elevated) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());	// Horrific
			if (user == null)
				return BadRequest("User not found");

			if (!elevated) {
				// Grant normal access to only one tranche

				if (!tranche.UserAccesses.Exists(x => x.Id == uid))
					tranche.UserAccesses.Add(user);
			}
			else {
				// Grant elevated access (manager) to all tranches in the project

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
		[HttpPut("grant_access_withfile/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]	// 4MB
		public async Task<IActionResult> GrantTrancheAccessFromFile(int tid, 
			[FromQuery] bool elevated, [FromForm] IFormFile file) {

			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			List<int> userIdsGrant = new();
			try {
				userIdsGrant = _ReadIntListFromFile(file);
			}
			catch (Exception e) {
				return BadRequest("File parse error: " + e.Message);
			}

			var dbSetTranche = _dataContext.Set<EJoinClass>("TrancheUserAccess");
			var dbSetManager = _dataContext.Set<EJoinClass>("ProjectUserManage");

			if (!elevated) {
				// Grant normal access to only one tranche

				dbSetTranche.AddRange(_ExcludeAccess_Tranche(tranche, userIdsGrant));
			}
			else {
				// Grant elevated access (manager) to all tranches in the project

				// Add access for each tranche in project
				foreach (var i in project.Tranches) {
					dbSetTranche.AddRange(_ExcludeAccess_Tranche(i, userIdsGrant));
				}

				// Add to project manager
				dbSetManager.AddRange(_ExcludeAccess_Project(project, userIdsGrant));
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		[HttpDelete("remove_access/{tid}/{uid}")]
		public async Task<IActionResult> RemoveTrancheAccess(int tid, int uid, [FromQuery] bool elevated) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			if (!elevated) {
				// Remove access from one tranche
				if (!project.UserManagers.Exists(x => x.Id == uid)) {
					tranche.UserAccesses.RemoveAll(x => x.Id == uid);
				}
				else {
					return BadRequest("Cannot remove tranche access from the user "
						+ "as they have elevated rights for this project\n"
						+ "Call API with elevated=true");
				}
			}
			else {
				// Remove access from all tranches
				foreach (var i in project.Tranches) {
					i.UserAccesses.RemoveAll(x => x.Id == uid);
				}

				// Also remove manage access for the project
				project.UserManagers.RemoveAll(x => x.Id == uid);
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
		[HttpDelete("remove_access_withfile/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]	// 4MB
		public async Task<IActionResult> RemoveTrancheAccessFromFile(int tid, 
			[FromQuery] bool elevated, [FromForm] IFormFile file) {

			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");
			Project project = tranche.Project;

			List<int> userIdsGrant = new();
			try {
				userIdsGrant = _ReadIntListFromFile(file);
			}
			catch (Exception e) {
				return BadRequest("File parse error: " + e.Message);
			}

			if (!elevated) {
				// Remove access from one tranche
				if (!project.UserManagers.Exists(x => userIdsGrant.Any(y => y == x.Id))) {
					tranche.UserAccesses.RemoveAll(x => userIdsGrant.Any(y => y == x.Id));
				}
				else {
					return BadRequest("Cannot remove tranche access from one or more users "
						+ "as they have elevated rights for this project\n"
						+ "Call API with elevated=true");
				}
			}
			else {
				// Remove access from all tranches
				foreach (var i in project.Tranches) {
					i.UserAccesses.RemoveAll(x => userIdsGrant.Any(y => y == x.Id));
				}

				// Also remove manage access for the project
				project.UserManagers.RemoveAll(x => userIdsGrant.Any(y => y == x.Id));
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}
	}
}
