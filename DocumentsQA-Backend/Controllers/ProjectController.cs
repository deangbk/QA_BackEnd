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
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;


	[Route("api/project")]
	[Authorize]
	public class ProjectController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<PostController> _logger;

		private readonly IAccessService _access;

		public ProjectController(DataContext dataContext, ILogger<PostController> logger, 
			IAccessService access) {

			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets project information
		/// </summary>
		[HttpGet("get/{pid}")]
		public async Task<IActionResult> GetProjectInfo(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			return Ok(project.ToJsonTable(2));
		}

		/// <summary>
		/// Gets list of all users with project read access, and project management access
		/// <para>Admins are not included</para>
		/// </summary>
		[HttpGet("users/{pid}")]
		public async Task<IActionResult> GetProjectUsers(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowManageProject(project))
				return Unauthorized();

			var listManagerIds = project.UserManagers
				.Select(x => x.Id)
				.ToList();
			var listUserIds = project.Tranches
				.SelectMany(x => x.UserAccesses)
				.Select(x => x.Id)
				.Distinct()
				.ToList();

			var listAccessIds = listManagerIds
				.Union(listUserIds)
				.ToList();

			return Ok(listUserIds);
		}

		/// <summary>
		/// Gets list of all users with project management access
		/// <para>Admins are not included</para>
		/// </summary>
		[HttpGet("managers/{pid}")]
		public async Task<IActionResult> GetProjectManagers(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			var listManagerIds = project.UserManagers
				.Select(x => x.Id)
				.ToList();

			return Ok(listManagerIds);
		}

		// -----------------------------------------------------

		
	}
}
