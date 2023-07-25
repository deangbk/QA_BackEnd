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

		[HttpGet("get_page/{pid}")]
		public async Task<IActionResult> GetPostsPage(int pid, 
			[FromQuery] PostGetFilterDTO filter, [FromQuery] PostGetDTO get) {

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(HttpContext, project))
				return Unauthorized();

			var query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);

			if (filter.TicketID != null) {
				query = query.Where(x => x.Id == filter.TicketID);
			}
			if (filter.PosterID != null) {
				query = query.Where(x => x.PostedById == filter.PosterID);
			}
			if (filter.Tranche != null) {
				query = query.Where(x => x.Account.Tranche.Name == filter.Tranche);
			}
			if (filter.PostedFrom is not null && filter.PostedTo is not null) {
				query = query.Where(x => x.DatePosted >= filter.PostedFrom && x.DatePosted < filter.PostedTo);
			}
			if (filter.OnlyAnswered != null) {
				if (filter.OnlyAnswered.Value) {
					query = query.Where(x => x.QuestionAnswer != null);
				}
				else {
					query = query.Where(x => x.QuestionAnswer == null);
				}
			}
			if (filter.SearchTerm != null) {
				query = query.Where(x => EF.Functions.Contains(x.QuestionText, filter.SearchTerm));
			}

			int countTotal = await query.CountAsync();
			int countPerPage = get.PostsPerPage;
			int maxPages = (int)Math.Ceiling(countTotal / (double)countPerPage);

			// Paginate result
			{
				query = query
					.Skip(get.Page * countPerPage)
					.Take(countPerPage);
			}

			var listPosts = await query.ToListAsync();
			var listPostTables = listPosts.Select(x => Mapper.FromPost(x, get.DetailsLevel));

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

			var listComments = await _dataContext.Comments
				.Where(x => x.QuestionId == id)
				.OrderBy(x => x.CommentNum)
				.ToListAsync();
			var listCommentTables = listComments.Select(x => Mapper.FromComment(x));

			return Ok(listCommentTables);
		}
	}
}
