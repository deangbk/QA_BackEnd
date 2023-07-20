﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/post")]
	[Authorize]
	public class PostController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<PostController> _logger;

		public PostController(DataContext dataContext, ILogger<PostController> logger) {
			_dataContext = dataContext;
			_logger = logger;
		}

		// -----------------------------------------------------

		[HttpGet("get_page/{pid}")]
		public async Task<IActionResult> GetPostsPage(int pid, [FromQuery] PostGetFilter filter, [FromQuery] PostGet get) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			var query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);

			if (filter.TicketID != null) {
				query = query.Where(x => x.Id == filter.TicketID);
			}
			if (filter.PosterID != null) {
				query = query.Where(x => x.PostedById == filter.PosterID);
			}
			if (filter.Tranche != null) {
				query = query.Where(x => x.Tranche.Name == filter.Tranche);
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
	}
}