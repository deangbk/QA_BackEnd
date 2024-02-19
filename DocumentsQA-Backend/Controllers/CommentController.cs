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

	[Route("api/comment")]
	[Authorize]
	public class CommentController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<CommentController> _logger;

		private readonly IAccessService _access;

		public CommentController(DataContext dataContext, ILogger<CommentController> logger, IAccessService access) {
			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Get question comments, ordered by number
		/// </summary>
		[HttpGet("{id}")]
		public async Task<IActionResult> GetComments(int id) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!PostHelpers.AllowUserReadPost(_access, question))
				return Unauthorized();

			var listCommentTables = question.Comments
				.OrderBy(x => x.CommentNum)
				.Select(x => x.ToJsonTable(1));

			return Ok(listCommentTables);
		}

		/// <summary>
		/// Add comment to question
		/// </summary>
		[HttpPost("{id}")]
		public async Task<IActionResult> AddComment(int id, [FromBody] PostAddCommentDTO dto) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!PostHelpers.AllowUserReadPost(_access, question))
				return Unauthorized();

			var comment = new Comment {
				CommentText = dto.Text,
				DatePosted = DateTime.Now,

				QuestionId = id,
				PostedById = _access.GetUserID(),
			};

			var maxCommentNo = PostHelpers.GetHighestCommentNo(question);
			comment.CommentNum = maxCommentNo + 1;

			question.Comments.Add(comment);
			await _dataContext.SaveChangesAsync();

			return Ok(comment.Id);
		}

		/// <summary>
		/// Remove comment question
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteComment(int id, [FromQuery] int num) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			// Only staff can do this
			if (!_access.AllowManageProject(question.Project))
				return Unauthorized();

			Comment? comment = question.Comments.Find(x => x.CommentNum == num);
			if (comment == null)
				return BadRequest("Comment not found");

			question.Comments.Remove(comment);
			await _dataContext.SaveChangesAsync();

			return Ok(question.Comments.Count);
		}

		/// <summary>
		/// Remove all comment questions from a post
		/// </summary>
		[HttpDelete("all/{id}")]
		public async Task<IActionResult> DeleteAllComments(int id) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			// Only staff can do this
			if (!_access.AllowManageProject(question.Project))
				return Unauthorized();

			question.Comments.Clear();
			await _dataContext.SaveChangesAsync();

			return Ok();
		}
	}
}
