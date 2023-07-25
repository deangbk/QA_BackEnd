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
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
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
			_dataContext.Projects.Add(project);

			await _dataContext.SaveChangesAsync();

			return Ok(project.Id);
		}

		[HttpPut("grant_access/{pid}/{uid}")]
		public async Task<IActionResult> GrantProjectAccess(int pid, int uid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());	// Horrific
			if (user == null)
				return BadRequest("User not found");

			if (project.UserAccesses.Find(x => x.Id == uid) == null)
				project.UserAccesses.Add(user);

			return Ok();
		}
		[HttpPut("grant_access_withfile/{pid}/{uid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]	// 4MB
		public async Task<IActionResult> GrantProjectAccessFromFile(int pid, [FromForm] IFormFile file) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			/*
			List<int> userIdsGrant = new();

			try {
				using (var stream = file.OpenReadStream()) {
					using var reader = new StreamReader(stream, Encoding.ASCII);

					var lines = new List<string>();
					while (!reader.EndOfStream) {
						var line = reader.ReadLine();
						if (line != null && line.Length > 0)
							lines.Add(line);
					}

					foreach (var line in lines) {
						var data = line.Split(' ').Select(x => x.Trim()).ToArray();
						var trancheId = int.Parse(data[0]);
						var userId = int.Parse(data[1]);
						userIdsGrant.AddRange(userId);
					}
				}
			}
			catch (Exception e) {
				return BadRequest("File parse error: " + e.Message);
			}

			var existingAccesses = project.UserAccesses.Select(x => x.Id).ToList();

			// Remove all users who already have access to the project
			var newAccesses = userIdsGrant.Except(existingAccesses);

			var accessesObj = newAccesses.Select(x => new ProjectUserAccess() {
				ProjectId = pid,
				UserId = x,
			});
			_dataContext.ProjectUserAccesses.AddRange(accessesObj);
			*/
			var rows = await _dataContext.SaveChangesAsync();

			return Ok(rows);
		}
	}
}
