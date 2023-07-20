using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
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

		[HttpPost("gen_roles")]
		[AllowAnonymous]
		public async Task<IActionResult> CreateDefaultRoles() {
			bool adminExists = await _roleManager.RoleExistsAsync("admin");
			if (!adminExists) {
				var role = new AppRole("admin");
				await _roleManager.CreateAsync(role);
			}

			return Ok();
		}

		[HttpPost("grant_role/{uid}/{role}")]
		public async Task<IActionResult> GrantUserRole(int uid, string role) {
			AppUser? user = await _userManager.FindByIdAsync(uid.ToString());	// Horrific
			if (user == null)
				return BadRequest("User not found");

			AppRole? roleFind = await _roleManager.FindByNameAsync(role);
			if (roleFind == null)
				return BadRequest("Role not found");

			await _userManager.AddClaimAsync(user, new Claim("role", role));

			return Ok();
		}

		[HttpPost("create_role/{role}")]
		public async Task<IActionResult> CreateRole(string role) {
			var result = await _roleManager.CreateAsync(new AppRole(role));
			if (result.Succeeded)
				return Ok();
			else
				return BadRequest(result.Errors);
		}
	}
}
