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
using DocumentsQA_Backend.Repository;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/admin")]
	[Authorize(Policy = "Role_Admin")]
	public class AdminController : Controller {
		private readonly ILogger<AdminController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IEmailService _emailService;

		private readonly UserManager<AppUser> _userManager;
		private readonly RoleManager<AppRole> _roleManager;

		private readonly AdminHelpers _adminHelper;

		public AdminController(
			ILogger<AdminController> logger,
			DataContext dataContext, IAccessService access,

			IEmailService emailService,

			UserManager<AppUser> userManager, RoleManager<AppRole> roleManager,

			AdminHelpers adminHelper)
		{
			_logger = logger;

			_emailService = emailService;

			_dataContext = dataContext;
			_access = access;

			_userManager = userManager;
			_roleManager = roleManager;

			_adminHelper = adminHelper;
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
				AppRole.User, 
				AppRole.Manager, 
				AppRole.Admin 
			};

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

			await _adminHelper.GrantUserRole(user, roleFind);

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

			await _adminHelper.RemoveUserRole(user, roleFind);

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
				await _adminHelper.MakeProjectManagers(project, users);

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
				await _adminHelper.MakeProjectManagers(project, userIds);

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

			//_adminHelper.ClearUsersTrancheAccess(project, new List<int> { uid });
			_adminHelper.RemoveProjectManagers(project, new List<int> { uid });

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets list of all admins
		/// </summary>
		[HttpGet("list")]
		public async Task<IActionResult> GetAdmins() {
			var listAdmin = await _userManager.GetUsersInRoleAsync(AppRole.Admin.Name);

			return Ok(listAdmin.Select(x => x.ToJsonTable(1)));
		}

		/// <summary>
		/// Creates an admin
		/// </summary>
		[HttpPost("create")]
		public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDTO dto) {
			var date = DateTime.Now;
			var rnd = new Random(date.GetHashCode());

			var emailNorm = _userManager.NormalizeEmail(dto.Email);

			{
				var exists = await _dataContext.Users
					.Where(x => x.ProjectId == null && x.NormalizedEmail == emailNorm)
					.AnyAsync();
				if (!exists)
					return BadRequest("Admin with this email already exists");
			}

			var user = new AppUser {
				Email = dto.Email,
				UserName = dto.Email,
				DisplayName = dto.Email,
				Company = "system",
				DateCreated = date,
			};

			var password = AuthHelpers.GeneratePassword(rnd, 12);

			var result = await _userManager.CreateAsync(user, password);
			if (!result.Succeeded)
				return BadRequest(result.Errors);

			{
				// Everyone must have user role
				await _adminHelper.GrantUserRole(user, AppRole.User);
				await _adminHelper.GrantUserRole(user, AppRole.Admin);
			}

			{
				var composer = new MailMessageComposerCreateAdmin() {
					User = user,
					Password = password,
				};

				MailData mailData = new() {
					Recipients = new() { dto.Email },
					Subject = composer.GetSubject(),
					Message = composer.GetMessage(),
				};
				await _emailService.SendMail(mailData);
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}
	}

	class MailMessageComposerCreateAdmin : IMailMessageComposer {
		public AppUser User { get; set; } = null!;
		public string Password { get; set; } = null!;

		public string GetSubject() => "You have been granted Admin Credentials";
		public string GetMessage() {
			// TODO: Make this pretty

			string message = $"<p>Your password is \"{Password}\"</p>";

			string messageHtml = "<html>";
			messageHtml += "<head><style>p { margin: 4px; }</style></head>";
			messageHtml += "<body>" + message + "</body></html>";

			return messageHtml;
		}
	}
}
