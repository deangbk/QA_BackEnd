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

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/note")]
	[Authorize]
	public class NoteController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<NoteController> _logger;

		private readonly IAccessService _access;

		public NoteController(DataContext dataContext, ILogger<NoteController> logger,
			IAccessService access) {

			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets project nodes, ordered by number
		/// </summary>
		[HttpGet("{pid}")]
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
		[HttpPost("{pid}")]
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
		[HttpDelete("{pid}")]
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
