using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Data;
using System.Text;

using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/auth")]
	[ApiController]
	public class UserAuthController : ControllerBase {
		private readonly ILogger<UserAuthController> _logger;
		
		private readonly DataContext _dataContext;
		

		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signinManager;
		private readonly RoleManager<AppRole> _roleManager;

		public UserAuthController(
			ILogger<UserAuthController> logger, 
			DataContext dataContext, 
			UserManager<AppUser> userManager, 
			SignInManager<AppUser> signinManager, 
			RoleManager<AppRole> roleManager)
		{
			_logger = logger;

			_dataContext = dataContext;

			_userManager = userManager;
			_signinManager = signinManager;
			_roleManager = roleManager;
		}

		// -----------------------------------------------------

		private async Task<AppUser?> _TrySignIn(int pid, LoginDTO uc) {
			// _signinManager.SignInAsync creates a cookie under the hood so don't use that

			string actualEmail = AuthHelpers.ComposeUsername(pid, uc.Email);
			var user = await _userManager.FindByEmailAsync(actualEmail);

			// Admins don't have a project ID prefixed to their emails
			if (user == null)
				user = await _userManager.FindByEmailAsync(uc.Email);

			if (user != null) {
				var result = await _signinManager.CheckPasswordSignInAsync(user, uc.Password, false);
				return result.Succeeded ? user : null;
			}

			return null;
		}

		// -----------------------------------------------------

		private AuthResponse _CreateUserToken(List<Claim> claims) {
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Initialize.JwtKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

			var tokenDescriptor = new JwtSecurityToken(
				issuer: null, audience: null,
				claims: claims,
				expires: DateTime.Now.AddDays(2),
				signingCredentials: creds
			);

			return new AuthResponse() {
				Token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor),
				Expiration = tokenDescriptor.ValidTo,
			};
		}

		[HttpPost("login/{pid}")]
		public async Task<IActionResult> Login(int pid, [FromBody] LoginDTO uc) {
			var user = await _TrySignIn(pid, uc);
			if (user != null) {
				var claims = new List<Claim>();

				{
					claims.Add(new Claim("id", user.Id.ToString()));
					claims.Add(new Claim("name", user.DisplayName));
					claims.Add(new Claim("project", pid.ToString()));

					// Add role claims for the user
					{
						var userClaims = await _userManager.GetClaimsAsync(user);
						claims.AddRange(userClaims.Where(x => x.Type == "role"));
					}
				}

				var token = _CreateUserToken(claims);
				return Ok(token);
			}
			else {
				return BadRequest("Incorrect login");
			}
		}
	}
}