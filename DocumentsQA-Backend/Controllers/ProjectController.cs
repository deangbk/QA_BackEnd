﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/project")]
	[Authorize]
	public class ProjectController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<PostController> _logger;

		private readonly IAccessService _access;

		public ProjectController(DataContext dataContext, ILogger<PostController> logger, IAccessService access) {
			_dataContext = dataContext;
			_logger = logger;
			_access = access;
		}

		// -----------------------------------------------------

		[HttpGet("get/{pid}")]
		public async Task<IActionResult> GetProjectInfo(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(HttpContext, project))
				return Unauthorized();

			return Ok(Mapper.FromProject(project, 2));
		}

		[HttpGet("users/{pid}")]
		public async Task<IActionResult> GetProjectUsers(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(HttpContext, project))
				return Unauthorized();

			var listUserIds = project.UserAccesses.Select(x => x.Id).ToList();

			return Ok(listUserIds);
		}
	}
}