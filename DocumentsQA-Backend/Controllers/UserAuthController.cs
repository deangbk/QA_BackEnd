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
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/auth")]
	[ApiController]
	public class UserAuthController : ControllerBase {
		private readonly DataContext _dataContext;
		private readonly ILogger<UserAuthController> _logger;

		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signinManager;
		private readonly RoleManager<AppRole> _roleManager;

		public UserAuthController(DataContext dataContext, ILogger<UserAuthController> logger,
			UserManager<AppUser> userManager, SignInManager<AppUser> signinManager, RoleManager<AppRole> roleManager) {

			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
			_signinManager = signinManager;
			_roleManager = roleManager;
		}

		// -----------------------------------------------------

		[HttpPost("create")]
		public async Task<IActionResult> CreateUser([FromForm] UserCredentials uc) {
			var user = new AppUser() {
				UserName = uc.Email,
				Email = uc.Email,
				DisplayName = uc.DisplayName,
				DateCreated = DateTime.Now,
			};
			if (uc.DisplayName.Length == 0)
				return BadRequest("DisplayName must not be empty");

			var result = await _userManager.CreateAsync(user, uc.Password);

			if (result.Succeeded) {
				// Set user role
				{
					string role = AppRole.User;
					await _userManager.AddClaimAsync(user, new Claim("role", role));
					await _userManager.AddToRoleAsync(user, role);
				}

				var token = await _CreateUserToken(uc);
				return Ok(token);
			}
			else {
				return BadRequest(result.Errors);
			}
		}

		private async Task<AuthResponse> _CreateUserToken(UserCredentials uc) {
			var claims = new List<Claim> {
				//new Claim("email", uc.Email),
			};

			{
				var user = await _userManager.FindByEmailAsync(uc.Email);
				if (user != null) {
					claims.Add(new Claim("id", user.Id.ToString()));
					claims.Add(new Claim("name", user.DisplayName));

					// Add role claims for the user
					{
						var userClaims = await _userManager.GetClaimsAsync(user);
						claims.AddRange(userClaims.Where(x => x.Type == "role"));
					}
				}
			}

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

		[HttpPost("login")]
		public async Task<IActionResult> LogIn([FromForm] UserCredentials uc) {
			var user = await _userManager.FindByNameAsync(uc.Email);
			var result = await _signinManager.CheckPasswordSignInAsync(user, uc.Password, false);

			if (result.Succeeded) {
				var token = await _CreateUserToken(uc);
				return Ok(token);
			}
			else if (result.IsLockedOut) {
				return BadRequest("User currently locked out.");
			}
			else {
				return BadRequest("Incorrect login.");
			}
		}
	}
}