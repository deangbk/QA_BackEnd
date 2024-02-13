using System;
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

		[HttpGet("count_content/{pid}")]
		public async Task<IActionResult> CountContent(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

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

		/// <summary>
		/// Gets project nodes, ordered by number
		/// </summary>
		[HttpGet("get_notes/{pid}")]
		public async Task<IActionResult> GetComments(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			var listNotesTables = project.Notes
				.OrderBy(x => x.Num)
				.Select(x => x.ToJsonTable(0));

			return Ok(listNotesTables);
		}

		/// <summary>
		/// Adds a project note
		/// </summary>
		[HttpPost("add_note/{pid}")]
		public async Task<IActionResult> AddNote(int pid, [FromBody] AddNoteDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowManageProject(project))
				return Unauthorized();

			var note = new Note {
				ProjectId = pid,
				PostedById = _access.GetUserID(),

				Text = dto.Text,
				Description = dto.Description ?? "",
				Category = dto.Category ?? "general",
				Sticky = dto.Sticky ?? false,

				DatePosted = DateTime.Now,
			};

			var maxCommentNo = PostHelpers.GetHighestNoteNo(project);
			note.Num = maxCommentNo + 1;

			project.Notes.Add(note);
			await _dataContext.SaveChangesAsync();

			return Ok(note.Id);
		}

		/// <summary>
		/// Removes a project note
		/// </summary>
		[HttpDelete("delete_note/{pid}")]
		public async Task<IActionResult> DeleteNote(int pid, [FromQuery] int num) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowManageProject(project))
				return Unauthorized();

			Note? note = project.Notes.Find(x => x.Num == num);
			if (note == null)
				return BadRequest("Note not found");

			project.Notes.Remove(note);
			await _dataContext.SaveChangesAsync();

			return Ok(project.Notes.Count);
		}
	}
}
