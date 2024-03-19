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
using DocumentsQA_Backend.Repository;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/note")]
	[Authorize]
	public class NoteController : Controller {
		private readonly ILogger<NoteController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IProjectRepository _repoProject;

		public NoteController(
			ILogger<NoteController> logger,
			DataContext dataContext,
			IAccessService access,
			IProjectRepository repoProject)
		{

			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets project nodes, ordered by number
		/// </summary>
		[HttpGet("")]
		public async Task<IActionResult> GetNotes([FromQuery] bool sticky = true) {
			var project = await _repoProject.GetProjectAsync();
			var listNotesTables = project.Notes

				.OrderBy(x => x.Num)
				.Select(x => x.ToJsonTable(0));

			return Ok(listNotesTables);
		}

		/// <summary>
		/// Adds a project note
		/// </summary>
		[HttpPost("")]
		public async Task<IActionResult> AddNote([FromBody] AddNoteDTO dto) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			var note = new Note {
				ProjectId = project.Id,
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
		[HttpDelete("")]
		public async Task<IActionResult> DeleteNote([FromQuery] int num) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			Note? note = project.Notes.Find(x => x.Num == num);
			if (note == null)
				return BadRequest("Note not found");

			project.Notes.Remove(note);
			await _dataContext.SaveChangesAsync();

			return Ok(project.Notes.Count);
		}
	}
}
