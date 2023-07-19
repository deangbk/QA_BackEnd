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
		private readonly RoleManager<AppUser> _roleManager;

		public UserAuthController(DataContext dataContext, ILogger<UserAuthController> logger,
			UserManager<AppUser> userManager, SignInManager<AppUser> signinManager, RoleManager<AppUser> roleManager) {

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
			};

			var result = await _userManager.CreateAsync(user, uc.Password);

			if (result.Succeeded) {
				return Ok(_CreateUserToken(uc));
			}
			else {
				return BadRequest(result.Errors);
			}
		}

		private AuthResponse _CreateUserToken(UserCredentials uc) {
			var claims = new Claim[] {
				new Claim("email", uc.Email),
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Initialize.JwtKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

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
			var result = await _signinManager.PasswordSignInAsync(uc.Email, uc.Password, false, false);

			if (result.Succeeded) {
				return Ok(_CreateUserToken(uc));
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