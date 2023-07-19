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

	[Route("api/user")]
	[Authorize]
	public class UserController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<UserAuthController> _logger;

		private readonly SignInManager<AppUser> _signinManager;

		public UserController(DataContext dataContext, ILogger<UserAuthController> logger,
			SignInManager<AppUser> signinManager) {

			_dataContext = dataContext;
			_logger = logger;

			_signinManager = signinManager;
		}

		// -----------------------------------------------------

		[HttpPost("logout")]
		public async Task<IActionResult> LogOut() {
			await _signinManager.SignOutAsync();

			_logger.LogInformation("User Logout");

			return Ok();
		}
	}
}
