﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/user")]
	[Authorize]
	public class UserController : Controller {
		private readonly ILogger<UserController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly SignInManager<AppUser> _signinManager;

		private readonly IProjectRepository _repoProject;

		public UserController(
			ILogger<UserController> logger,
			DataContext dataContext, IAccessService access,
			SignInManager<AppUser> signinManager,
			IProjectRepository repoProject)
		{
			_logger = logger;

			_dataContext = dataContext;
			_access = access;

			_signinManager = signinManager;

			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		private JsonTable _UserToResTable(AppUser user, int details) {
			var table = user.ToJsonTable(details);

			int projectId = _access.GetProjectID();
			var trancheAccesses = ProjectHelpers.GetUserTrancheAccessesInProject(
				user, projectId);
			table["tranches"] = trancheAccesses
				.Select(x => x.ToJsonTable(0))
				.ToList();

			return table;
		}

		/// <summary>
		/// Gets self info
		/// </summary>
		[HttpGet("")]
		public async Task<IActionResult> GetUser([FromQuery] int details = 0) {
			int userId = _access.GetUserID();

			AppUser? user = await Queries.GetUserFromId(_dataContext, userId);
			if (user == null)
				return BadRequest("User not found");

			var table = _UserToResTable(user, details);
			return Ok(table);
		}

		/// <summary>
		/// Gets info of a specific user
		/// </summary>
		[HttpGet("{uid}")]
		public async Task<IActionResult> GetUserFromId(int uid, [FromQuery] int details = 0) {
			Project? project = await Queries.GetProjectFromId(
				_dataContext, _access.GetProjectID());
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			var table = _UserToResTable(user, details);
			return Ok(table);
		}
	}
}
