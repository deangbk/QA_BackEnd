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
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Security.Cryptography;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/debug")]
	public class DebugController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<DebugController> _logger;

		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signinManager;
		private readonly RoleManager<AppRole> _roleManager;

		public DebugController(DataContext dataContext, ILogger<DebugController> logger,
			UserManager<AppUser> userManager, SignInManager<AppUser> signinManager, RoleManager<AppRole> roleManager) {

			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
			_signinManager = signinManager;
			_roleManager = roleManager;
		}

		// -----------------------------------------------------

		[HttpPost("create_users")]
		public async Task<IActionResult> CreateUsers() {
			var _NewUser = async (string name, string email, string pass, AppRole role) => {
				var user = new AppUser() {
					UserName = email,
					Email = email,
					DisplayName = name,
					Company = "Holy Roman Empire",
					DateCreated = DateTime.Now,
				};

				var result = await _userManager.CreateAsync(user, pass);
				if (result.Succeeded) {
					// Set user role
					await _userManager.AddClaimAsync(user, new Claim("role", role.Name));
					await _userManager.AddToRoleAsync(user, role.Name);
				}
				return result.Succeeded;
			};

			string password = "pasaworda55";

			for (int i = 0; i < 2; ++i)
				await _NewUser("DragonAdmin" + i, i.ToString() + "@test.admin", password, AppRole.Admin);

			for (int i = 0; i < 3; ++i)
				await _NewUser("ForumModerator" + i, i.ToString() + "@test.manager", password, AppRole.Manager);

			for (int i = 0; i < 8; ++i)
				await _NewUser("StupidUser" + i, i.ToString() + "@test.user", password, AppRole.User);

			return Ok();
		}

		[HttpPost("create_project/{name}")]
		public async Task<IActionResult> CreateProject(string name) {
			Project project = new Project {
				Name = name,
				DisplayName = name,
				CompanyName = "Holy Roman Empire",
				ProjectStartDate = DateTime.Now,
				ProjectEndDate = new DateTime(2025, 12, 1),
			};
			_dataContext.Projects.Add(project);

			await _dataContext.SaveChangesAsync();

			return Ok(project.Id);
		}

		[HttpPost("create_tranches/{pid}")]
		public async Task<IActionResult> CreateProjectTranches(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			var _MakeTranche = (string name) => new Tranche { Project = project, Name = name };

			var tranches = "ABCDEF".Select(c => _MakeTranche(c.ToString())).ToList();

			project.Tranches = new List<Tranche>(tranches);
			var count = await _dataContext.SaveChangesAsync();

			return Ok(count);
		}
	}
}
