using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/project")]
	[Authorize]
	public class ProjectController : Controller {
		private readonly ILogger<ProjectController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IProjectRepository _repoProject;

		public ProjectController(
			ILogger<ProjectController> logger, 
			DataContext dataContext, IAccessService access, 
			IProjectRepository repoProject) 
		{
			_logger = logger;

			_dataContext = dataContext;
			_access = access;

			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets project information
		/// </summary>
		[HttpGet("")]
		public async Task<IActionResult> GetProjectInfo() {
			var project = await _repoProject.GetProjectAsync();

			return Ok(project.ToJsonTable(2));
		}

		/// <summary>
		/// Gets project tranches information
		/// </summary>
		[HttpGet("tranches")]
		public async Task<IActionResult> GetProjectTranches() {
			var project = await _repoProject.GetProjectAsync();

			List<Tranche> tranches = new();
			if (!_access.IsSuperUser()) {
				var user = await Queries.GetUserFromId(_dataContext, _access.GetUserID());
				if (user != null) {
					tranches = ProjectHelpers.GetUserTrancheAccessesInProject(user, project.Id);
				}
			}
			else {
				tranches = project.Tranches;
			}

			var resTable = tranches.Select(x => x.ToJsonTable(1));
			return Ok(resTable);
		}

		/// <summary>
		/// Gets list of all users with project read access
		/// <para>Admins are not included</para>
		/// </summary>
		[HttpGet("users")]
		public async Task<IActionResult> GetProjectUsers([FromQuery] int details = -1) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			var listUserIds = ProjectHelpers.GetProjectUserAccesses(project);

			if (details >= 0) {
				// TODO: Maybe optimize these

				var trancheUsersMap = ProjectHelpers.GetTrancheUserAccessesMap(project);
				var trancheDataMap = project.Tranches
					.ToDictionary(x => x.Id, x => x.ToJsonTable(0));

				var mapUsers = await Queries.GetUsersMapFromIds(_dataContext, listUserIds);
				var listRes = mapUsers
					.Select(x => {
						var tableBase = x.Value.ToJsonTable(details);
						tableBase["tranches"] = trancheUsersMap
							.Where(y => y.Value.Contains(x.Key))
							.Select(y => trancheDataMap[y.Key.Id])
							.ToList();
						return tableBase;
					});

				return Ok(listRes);
			}
			else {
				return Ok(listUserIds);
			}
		}

		/// <summary>
		/// Gets list of all users with project management access
		/// <para>Admins are not included</para>
		/// </summary>
		[HttpGet("managers")]
		public async Task<IActionResult> GetProjectManagers([FromQuery] int details = -1) {
			var project = await _repoProject.GetProjectAsync();

			var listManagerIds = project.UserManagers
				.Select(x => x.Id)
				.ToList();

			if (details >= 0) {
				var trancheUsersMap = ProjectHelpers.GetTrancheUserAccessesMap(project);
				var trancheDataMap = project.Tranches
					.ToDictionary(x => x.Id, x => x.ToJsonTable(0));

				var mapUsers = await Queries.GetUsersMapFromIds(_dataContext, listManagerIds);
				var listRes = mapUsers
					.Select(x => {
						var tableBase = x.Value.ToJsonTable(details);
						tableBase["tranches"] = trancheUsersMap
							.Where(y => y.Value.Contains(x.Key))
							.Select(y => trancheDataMap[y.Key.Id])
							.ToList();
						return tableBase;
					});

				return Ok(listRes);
			}
			else {
				return Ok(listManagerIds);
			}
		}

		[HttpGet("content")]
		public async Task<IActionResult> CountContent() {
			var project = await _repoProject.GetProjectAsync();

			int countGeneralPosts = project.Questions
				.Where(x => x.Type == QuestionType.General)
				.Count();
			int countAccountPosts = project.Questions
				.Where(x => x.Type == QuestionType.Account)
				.Count();
			int countDocuments = await _dataContext.Documents
				.Where(x => x.ProjectId == project.Id)
				.CountAsync();

			return Ok(new JsonTable {
				["gen_posts"] = countGeneralPosts,
				["acc_posts"] = countAccountPosts,
				["documents"] = countDocuments,
			});
		}

		// -----------------------------------------------------

		[HttpPost("create")]
		public async Task<IActionResult> CreateProject([FromBody] CreateProjectDTO dto) {
			if (_access.IsAdmin())
				return Forbid();

			var project = new Project {
				Name = dto.Name,
				DisplayName = dto.Name,
				CompanyName = dto.Company,
				ProjectStartDate = dto.DateStart!.Value,
				ProjectEndDate = dto.DateEnd!.Value,
				LastEmailSentDate = DateTime.MinValue,
			};

			List<string> tranches;
			try {
				tranches = dto.InitialTranches
					.Split(",")
					.Select(x => x.Trim().Truncate(16))
					.Where(x => x.Length > 0)
					.ToList();
			}
			catch (Exception) {
				return BadRequest("Tranches: incorrect input format");
			}

			// Wrap all operations in a transaction so failure would revert the entire thing
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				_dataContext.Projects.Add(project);
				await _dataContext.SaveChangesAsync();

				project.Tranches = tranches.Select(x => new Tranche {
					ProjectId = project.Id,
					Name = x,
				}).ToList();
				await _dataContext.SaveChangesAsync();

				await transaction.CommitAsync();
			}

			return Ok(project.Id);
		}
	}
}
