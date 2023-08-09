using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/post")]
	[Authorize]
	public class PostController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<PostController> _logger;

		private readonly IAccessService _access;

		public PostController(DataContext dataContext, ILogger<PostController> logger, IAccessService access) {
			_dataContext = dataContext;
			_logger = logger;

			_access = access;
		}

		// -----------------------------------------------------

		[HttpGet("get_post/{pid}")]
		public async Task<IActionResult> GetPosts(int pid, 
			[FromQuery] PostGetFilterDTO filterDTO, [FromQuery] int details = 0) {

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			bool bElevated = _access.UserHasElevatedAccess();

			IQueryable<Question> query;
			if (bElevated)
				query = project.Questions.AsQueryable();
			else
				query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);

			if (filterDTO.PostedFrom is not null) {
				query = query.Where(x => x.DatePosted >= filterDTO.PostedFrom);
			}
			if (filterDTO.PostedTo is not null) {
				query = query.Where(x => x.DatePosted < filterDTO.PostedTo);
			}

			var listPosts = await query.ToListAsync();
			var listPostTables = listPosts.Select(x => Mapper.FromPost(x, details));

			return Ok(listPostTables);
		}

		[HttpGet("get_page/{pid}")]
		public async Task<IActionResult> GetPostsPage(int pid, [FromQuery] PostGetFilterDTO filterDTO,
			[FromQuery] PaginateDTO pageDTO, [FromQuery] int details = 0) {

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			bool bElevated = _access.IsSuperUser();

			IQueryable<Question> query;
			if (bElevated)
				query = _dataContext.Questions.AsQueryable();
			else
				query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);

			if (filterDTO.TicketID != null) {
				query = query.Where(x => x.Id == filterDTO.TicketID);
			}
			if (filterDTO.PosterID != null) {
				query = query.Where(x => x.PostedById == filterDTO.PosterID);
			}
			if (filterDTO.Tranche != null) {
				query = query.Where(x => x.Account == null || x.Account.Tranche.Name == filterDTO.Tranche);
			}
			if (filterDTO.PostedFrom is not null) {
				query = query.Where(x => x.DatePosted >= filterDTO.PostedFrom);
			}
			if (filterDTO.PostedTo is not null) {
				query = query.Where(x => x.DatePosted < filterDTO.PostedTo);
			}
			if (filterDTO.OnlyAnswered != null) {
				if (filterDTO.OnlyAnswered.Value)
					query = query.Where(x => x.QuestionAnswer != null);
				else
					query = query.Where(x => x.QuestionAnswer == null);
			}
			if (filterDTO.SearchTerm != null) {
				query = query.Where(x => EF.Functions.Contains(x.QuestionText, filterDTO.SearchTerm));
			}

			int countTotal = await query.CountAsync();
			int countPerPage = pageDTO.CountPerPage;
			int maxPages = (int)Math.Ceiling(countTotal / (double)countPerPage);

			// Paginate result
			{
				query = query
					.Skip(pageDTO.Page * countPerPage)
					.Take(countPerPage);
			}

			var listPosts = await query.ToListAsync();
			var listPostTables = listPosts.Select(x => Mapper.FromPost(x, details));

			return Ok(new JsonTable() {
				["count_total"] = countTotal,
				["posts"] = listPostTables,
			});
		}

		[HttpGet("get_comments/{id}")]
		public async Task<IActionResult> GetPostComments(int id) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");
			Project project = question.Project;

			if (!_access.AllowToProject(project))
				return Unauthorized();

			var listComments = await _dataContext.Comments
				.Where(x => x.QuestionId == id)
				.OrderBy(x => x.CommentNum)
				.ToListAsync();
			var listCommentTables = listComments.Select(x => Mapper.FromComment(x));

			return Ok(listCommentTables);
		}
	}
}
