﻿using System;
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
		/// Gets project nodes
		/// <para>Sticky notes are ordered by number</para>
		/// <para>Normal notes are ordered in reverse chronological order</para>
		/// </summary>
		[HttpGet("")]
		public async Task<IActionResult> GetNotes([FromQuery] bool sticky = true, [FromQuery] int count = -1) {
			var project = await _repoProject.GetProjectAsync();

			List<Note> notes = new();

			if (sticky) {
				notes = project.Notes
					.Where(x => x.Sticky)
					.OrderBy(x => x.Num)
					.ToList();

				IEnumerable<Note> normalNotes = project.Notes
					.Where(x => !x.Sticky)
					.OrderByDescending(x => x.DatePosted);
				if (count >= 0) {
					normalNotes = normalNotes.Take(count);
				}

				notes.AddRange(normalNotes);
			}
			else {
				notes = project.Notes
					.OrderBy(x => x.Num)
					.ToList();
			}

			return Ok(notes.Select(x => x.ToJsonTable(1)));
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
		[HttpDelete("{num}")]
		public async Task<IActionResult> DeleteNote(int num) {
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
