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
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

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
		[HttpGet("info/{pid}")]
		public async Task<IActionResult> GetProjectInfo(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

			return Ok(project.ToJsonTable(2));
		}

		/// <summary>
		/// Gets project tranches information
		/// </summary>
		[HttpGet("tranches")]
		public async Task<IActionResult> GetProjectTranches() {
			Project? project = await Queries.GetProjectFromId(
				_dataContext, _access.GetProjectID());
			if (project == null)
				return BadRequest("Project not found");

			int userId = _access.GetUserID();

			List<Tranche> tranches = new();
			if (!_access.IsSuperUser()) {
				var user = await Queries.GetUserFromId(_dataContext, userId);
				if (user != null) {
					tranches = ProjectHelpers.GetUserTrancheAccessesInProject(user, project.Id);
				}
			}
			else {
				tranches = project.Tranches;
			}

			//var mapTrancheAccounts = tranches.ToDictionary(x => x, x => x.Accounts);

			var resTable = tranches.Select(x => x.ToJsonTable(1));
			return Ok(resTable);
		}

		/// <summary>
		/// Gets list of all users with project read access
		/// <para>Admins are not included</para>
		/// </summary>
		[HttpGet("users/{pid}")]
		public async Task<IActionResult> GetProjectUsers(int pid, [FromQuery] int details = 0)
		{
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			var listUserIds = ProjectHelpers.GetProjectUserAccesses(project);

			if (details > 0) {
				// TODO: Maybe optimize these

				var trancheUsersMap = ProjectHelpers.GetTrancheUserAccessesMap(project);
				var trancheDataMap = project.Tranches
					.ToDictionary(x => x.Id, x => x.ToJsonTable(0));

				var mapUsers = (await Queries.GetUsersMapFromIds(_dataContext, listUserIds))!;
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
		[HttpGet("managers/{pid}")]
		public async Task<IActionResult> GetProjectManagers(int pid, [FromQuery] int details = -1) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

			var listManagerIds = project.UserManagers
				.Select(x => x.Id)
				.ToList();

			if (details >= 0) {
				var trancheUsersMap = ProjectHelpers.GetTrancheUserAccessesMap(project);
				var trancheDataMap = project.Tranches
					.ToDictionary(x => x.Id, x => x.ToJsonTable(0));

				var mapUsers = (await Queries.GetUsersMapFromIds(_dataContext, listManagerIds))!;
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

		[HttpGet("content/{pid}")]
		public async Task<IActionResult> CountContent(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

			int countGeneralPosts = project.Questions
				.Where(x => x.Type == QuestionType.General)
				.Count();
			int countAccountPosts = project.Questions
				.Where(x => x.Type == QuestionType.Account)
				.Count();
			int countDocuments = await _dataContext.Documents
				.Where(x => x.ProjectId == pid)
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
			Project project = new Project {
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
