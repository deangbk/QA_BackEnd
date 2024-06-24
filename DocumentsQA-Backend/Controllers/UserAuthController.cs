using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
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

	[Route("api/auth")]
	[ApiController]
	public class UserAuthController : ControllerBase {
		private readonly ILogger<UserAuthController> _logger;
		private readonly IServiceProvider _services;

		private readonly DataContext _dataContext;

		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signinManager;
		private readonly RoleManager<AppRole> _roleManager;

		private readonly IEmailService _emailService;
		private readonly IEventLogRepository _repoEventLog;

		public UserAuthController(
			ILogger<UserAuthController> logger,
			IServiceProvider services,

			DataContext dataContext, 

			UserManager<AppUser> userManager, 
			SignInManager<AppUser> signinManager, 
			RoleManager<AppRole> roleManager,

			IEmailService emailService,
			IEventLogRepository repoEventLog
		) {
			_logger = logger;
			_services = services;

			_dataContext = dataContext;

			_userManager = userManager;
			_signinManager = signinManager;
			_roleManager = roleManager;

			_emailService = emailService;
			_repoEventLog = repoEventLog;
		}

		// -----------------------------------------------------

		private async Task<AppUser?> _FindUser(string email, string? projectName) {
			var project = projectName != null ?
				await Queries.GetProjectFromName(_dataContext, projectName) : null;
			return await _FindUser(email, project);
		}
		private async Task<AppUser?> _FindUser(string email, Project? project) {
			var emailNorm = _userManager.NormalizeEmail(email);

			AppUser? user = null;

			var users = await _dataContext.Users
				.Where(x => x.NormalizedEmail == emailNorm)
				.ToListAsync();
			if (users.Count > 1 && project != null) {
				user = users.Find(x => x.ProjectId == project.Id);
			}
			else if (users.Count == 1) {
				user = users.First();
			}

			return user;
		}

		private async Task<bool> _TrySignIn(AppUser? user, string password) {
			// _signinManager.SignInAsync creates a cookie under the hood so don't use that

			if (user != null) {
				var result = await _signinManager.CheckPasswordSignInAsync(user, password, false);
				return result.Succeeded;
			}
			return false;
		}

		// -----------------------------------------------------

		// TODO: Rework the sign-in system to use dual auth and refresh token

		private AuthResponse _CreateUserToken(List<Claim> claims, TimeSpan clockSkew) {
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Initialize.JwtKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

			var tokenDescriptor = new JwtSecurityToken(
				issuer: null, audience: null,
				claims: claims,
				expires: DateTime.Now.Add(clockSkew),
				signingCredentials: creds
			);

			return new AuthResponse() {
				Token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor),
				Expiration = tokenDescriptor.ValidTo,
			};
		}

		private async Task<AuthResponse> _TryLoginToProject(string projectName, LoginDTO uc) {
			var project = await Queries.GetProjectFromName(_dataContext, projectName);
			if (project == null)
				throw new InvalidDataException("Invalid project");

			var user = await _FindUser(uc.Email, project);
			if (user == null || (!await _TrySignIn(user, uc.Password)))
				throw new InvalidDataException("Incorrect login");

			var userRoles = await _userManager.GetRolesAsync(user);

			var claims = new List<Claim>();
			{
				claims.Add(new Claim("id", user.Id.ToString()));
				claims.Add(new Claim("name", user.DisplayName));
				claims.Add(new Claim("proj", project.Id.ToString()));
				claims.Add(new Claim("projn", project.Name.ToString()));

				// Add role claims for the user
				{
					var userRoleClaims = userRoles
						.Select(x => new Claim("role", x))
						.ToList();
					claims.AddRange(userRoleClaims);
				}
			}

			await _repoEventLog.AddLoginEvent(project.Id);

			var token = _CreateUserToken(claims, TimeSpan.FromDays(1));
			return token;
		}

		[HttpPost("login/{pname}")]
		public async Task<IActionResult> Login([FromRoute] string pname, [FromBody] LoginDTO uc) {
			try {
				var token = await _TryLoginToProject(pname, uc);
				return Ok(token);
			}
			catch (InvalidDataException e) {
				return BadRequest(e.Message);
			}
		}

		private async Task<AuthResponse> _TryLoginToAdmin(LoginDTO uc) {
			var user = await _FindUser(uc.Email, (Project?)null);
			if (user == null || (!await _TrySignIn(user, uc.Password)))
				throw new InvalidDataException("Incorrect login");

			{
				var userRoles = await _userManager.GetRolesAsync(user);
				if (!userRoles.Any(x => x == AppRole.Admin.Name))
					throw new InvalidDataException("Incorrect login");

			var claims = new List<Claim>();
			{
				claims.Add(new Claim("id", user.Id.ToString()));
				claims.Add(new Claim("name", user.DisplayName));

				// Add role claims for the user
				{
					var userRoleClaims = userRoles
						.Select(x => new Claim("role", x))
						.ToList();
					claims.AddRange(userRoleClaims);
				}
			}

			var token = _CreateUserToken(claims, TimeSpan.FromHours(1));
			return token;
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDTO uc) {
			try {
				var token = await _TryLoginToAdmin(uc);
				return Ok(token);
			}
			catch (InvalidDataException e) {
				return BadRequest(e.Message);
			}
		}

		// -----------------------------------------------------

		[HttpPost("password/change")]
		[Authorize]
		public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeDTO dto) {
			var access = _services.GetRequiredService<IAccessService>();

			int userId = access.GetUserID();
			AppUser? user = await Queries.GetUserFromId(_dataContext, userId);
			if (user == null)
				return BadRequest("User not found");

			var res = await _userManager.ChangePasswordAsync(user, dto.Old, dto.New);
			if (res == null || !res.Succeeded) {
				if (res == null)
					return StatusCode(500);

				var topErr = res.Errors.First();
				return BadRequest($"{topErr.Code}: {topErr.Description}");
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		class PasswordResetToken {
			public int UserId { get; set; }
			public string Project { get; set; } = null!;
			public string Token { get; set; } = null!;

			public string ToBase64() {
				var combine = $"{UserId}&{Project}&{Token}";
				return Base64UrlEncoder.Encode(combine);
			}
			public static PasswordResetToken? FromBase64(string encoded) {
				var args = Base64UrlEncoder.Decode(encoded).Split('&');
				if (args.Length != 3)
					return null;

				try {
					return new PasswordResetToken {
						UserId = int.Parse(args[0]),
						Project = args[1],
						Token = args[2]
					};
				}
				catch (Exception) {
					return null;
				}
			}
		}

		[HttpPost("password/reset")]
		public async Task<IActionResult> GenerateResetPasswordToken([FromBody] PasswordResetDTO dto) {
			var user = await _FindUser(dto.Email, dto.Project);
			if (user == null)
				return NotFound();

			// Revoke any previous tokens (also logs out the user)
			await _userManager.UpdateSecurityStampAsync(user);

			var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
			if (resetToken == null)
				return BadRequest();

			var resetTokenCont = new PasswordResetToken {
				UserId = user.Id,
				Project = user.Project?.Name ?? "",
				Token = resetToken,
			};

			{
				var url = $"http://localhost:4200/user/reset/pass/{resetTokenCont.ToBase64()}";
				var composer = new MailMessageComposerResetPassword() {
					ResetUrl = url,
					Project = user.Project?.DisplayName,
				};

				MailData mailData = new() {
					Recipients = new() { user.Email },
					Subject = composer.GetSubject(),
					Message = composer.GetMessage(),
				};
				await _emailService.SendMail(mailData);
			}

			return Ok();
		}

		[HttpGet("password/reset2/{token}")]
		public async Task<IActionResult> VerifyResetPasswordWithToken(string token) {
			var resetTokenCont = PasswordResetToken.FromBase64(token);
			if (resetTokenCont == null)
				return BadRequest("Invalid token");

			AppUser? user = await Queries.GetUserFromId(_dataContext, resetTokenCont.UserId);
			if (user == null)
				return BadRequest("User not found");

			var ok = await _userManager.VerifyUserTokenAsync(
				user, _userManager.Options.Tokens.PasswordResetTokenProvider,
				"ResetPassword", resetTokenCont.Token);
			if (ok) {
				return Ok();
			}
			else {
				return BadRequest("Invalid token");
			}
		}

		[HttpPost("password/reset2")]
		public async Task<IActionResult> ResetPasswordWithToken([FromBody] PasswordResetTokenDTO dto) {
			var resetTokenCont = PasswordResetToken.FromBase64(dto.Token);
			if (resetTokenCont == null)
				return BadRequest("Invalid token");

			AppUser? user = await Queries.GetUserFromId(_dataContext, resetTokenCont.UserId);
			if (user == null)
				return BadRequest("User not found");

			// TODO: Invalidate existing login tokens after password reset

			var res = await _userManager.ResetPasswordAsync(user, resetTokenCont.Token, dto.Password);
			if (res == null || !res.Succeeded) {
				if (res == null)
					return StatusCode(500);

				var topErr = res.Errors.First();
				return BadRequest($"{topErr.Code}: {topErr.Description}");
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}
	}

	class MailMessageComposerResetPassword : IMailMessageComposer {
		public string ResetUrl { get; set; } = null!;
		public string? Project { get; set; }

		public string GetSubject() => "Reset Your Password";
		public string GetMessage() {
			// TODO: Make this pretty

			string message = "<p>";
			{
				message += "You've requested to reset your password";
				if (Project != null) {
					message += $" for project \"{Project}\". ";
				}
				else {
					message +=  ". ";
				}

				message += $"If you did not make such a request, please ignore this message.";
				message += $"</p>";

				message += $"<p>Otherwise, please click on the link below to reset your password.</p><br>";
				message += $"<p><a href=\"{ResetUrl}\">Reset Password</a></p><br>";
				message += $"<p>This link will remain valid for 1 hour.</p>";
			}

			string messageHtml = "<html>";
			messageHtml += "<head><style>p { margin: 4px; }</style></head>";
			messageHtml += "<body>" + message + "</body></html>";

			return messageHtml;
		}
	}
}