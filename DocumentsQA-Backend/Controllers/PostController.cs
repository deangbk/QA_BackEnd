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

			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();
		}

		// -----------------------------------------------------

		private bool _CheckQuestionAccess(Question question) {
			if (question.Type == QuestionType.General) {
				Project project = question.Project;
				return _access.AllowToProject(project);
			}
			else {
				Tranche tranche = question.Account!.Tranche;
				return _access.AllowToTranche(tranche);
			}
		}

		private static bool? _CheckGetPostPermissions(bool? getType, bool bUserIsElevated) {
			// Handle get permissions for normal users
			if (!bUserIsElevated) {

				// If requesting for everything -> resolve to get only approved
				if (getType == null) {
					getType = true;
				}

				// If requesting for unapproved -> throw a 401
				else if (getType == false) {
					throw new AccessUnauthorizedException("Cannot get list of unapproved questions");
				}
			}
			return getType;
		}

		/// <summary>
		/// Gets project questions
		/// <para>Valid filters for filterDTO:</para>
		/// <list type="bullet">
		///		<item>PostedFrom</item>
		///		<item>PostedTo</item>
		///		<item>Approved</item>
		/// </list>
		/// <para>If user has management rights, Approved will work normally.</para>
		/// <para>If user is a normal user, unapproved questions cannot be queried, regardless of Approved.</para>
		/// </summary>
		[HttpGet("get_posts/{pid}")]
		public async Task<IActionResult> GetPosts(int pid, 
			[FromQuery] PostGetFilterDTO filterDTO, [FromQuery] int details = 0) {

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			IQueryable<Question> query;
			{
				bool bElevated = _access.IsSuperUser();
				bool? approved = _CheckGetPostPermissions(filterDTO.Approved, bElevated);
				switch (approved) {
					case true:
						query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);	// Gets only approved
						break;
					case false:
						query = Queries.GetUnapprovedQuestionsQuery(_dataContext, pid); // Gets only unapproved
						break;
					case null:
						query = project.Questions.AsQueryable();	// Gets everything
						break;
				}
			}

			if (filterDTO.Type is not null) {
				string typeName = filterDTO.Type.ToLower();
				if (typeName == "general") {
					query = query.Where(x => x.Type == QuestionType.General);
				}
				else if (typeName == "account") {
					if (filterDTO.Account == null) {
						ModelState.AddModelError("Account", "Account number must not be null");
						return BadRequest(new ValidationProblemDetails(ModelState));
					}

					query = query.Where(x => x.Type == QuestionType.Account 
						&& x.AccountId == filterDTO.Account);
				}
			}

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

		/// <summary>
		/// Gets project questions as paginated list
		/// <para>Valid filters for filterDTO:</para>
		/// <list type="bullet">
		///		<item>TicketID</item>
		///		<item>PosterID</item>
		///		<item>Tranche</item>
		///		<item>PostedFrom</item>
		///		<item>PostedTo</item>
		///		<item>OnlyAnswered</item>
		///		<item>SearchTerm</item>
		/// </list>
		/// <para>If user has management rights, Approved will work normally.</para>
		/// <para>If user is a normal user, unapproved questions cannot be queried, regardless of Approved.</para>
		/// </summary>
		[HttpGet("get_posts_page/{pid}")]
		public async Task<IActionResult> GetPostsPage(int pid, [FromQuery] PostGetFilterDTO filterDTO,
			[FromQuery] PaginateDTO pageDTO, [FromQuery] int details = 0) {

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			IQueryable<Question> query;
			{
				bool bElevated = _access.IsSuperUser();
				bool? approved = _CheckGetPostPermissions(filterDTO.Approved, bElevated);
				switch (approved) {
					case true:
						query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);	// Gets only approved
						break;
					case false:
						query = Queries.GetUnapprovedQuestionsQuery(_dataContext, pid); // Gets only unapproved
						break;
					case null:
						query = project.Questions.AsQueryable();	// Gets everything
						break;
				}
			}

			if (filterDTO.Type is not null) {
				string typeName = filterDTO.Type.ToLower();
				if (typeName == "general") {
					query = query.Where(x => x.Type == QuestionType.General);
				}
				else if (typeName == "account") {
					if (filterDTO.Account == null) {
						ModelState.AddModelError("Account", "Account number must not be null");
						return BadRequest(new ValidationProblemDetails(ModelState));
					}

					query = query.Where(x => x.Type == QuestionType.Account
						&& x.AccountId == filterDTO.Account);
				}
			}

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

		/// <summary>
		/// Gets all comments on a question
		/// </summary>
		[HttpGet("get_comments/{id}")]
		public async Task<IActionResult> GetPostComments(int id) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!_CheckQuestionAccess(question))
				return Unauthorized();

			var listComments = await _dataContext.Comments
				.Where(x => x.QuestionId == id)
				.OrderBy(x => x.CommentNum)
				.ToListAsync();
			var listCommentTables = listComments.Select(x => Mapper.FromComment(x));

			return Ok(listCommentTables);
		}

		// -----------------------------------------------------

		/// <summary>
		/// Posts a general question
		/// </summary>
		[HttpPost("post_question_g/{pid}")]
		public async Task<IActionResult> PostGeneralQuestion(int pid, [FromForm] PostCreateDTO createDTO) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Unauthorized();

			var time = DateTime.Now;
			var userId = _access.GetUserID();

			Question question = new Question {
				QuestionNum = 0,
				Type = QuestionType.General,

				ProjectId = project.Id,
				AccountId = null,

				QuestionText = createDTO.Text,

				PostedById = userId,

				LastEditorId = userId,

				DatePosted = time,
				DateLastEdited = time,
			};

			project.Questions.Add(question);

			await _dataContext.SaveChangesAsync();
			return Ok(question.Id);
		}

		/// <summary>
		/// Posts an account question
		/// </summary>
		[HttpPost("post_question_a/{id}")]
		public async Task<IActionResult> PostAccountQuestion(int id, [FromForm] PostCreateDTO createDTO) {
			Account? account = await Queries.GetAccountFromId(_dataContext, id);
			if (account == null)
				return BadRequest("Account not found");
			Tranche tranche = account.Tranche;

			if (!_access.AllowToTranche(tranche))
				return Unauthorized();

			var time = DateTime.Now;
			var userId = _access.GetUserID();

			Question question = new Question {
				QuestionNum = 0,
				Type = QuestionType.General,

				ProjectId = tranche.ProjectId,
				AccountId = account.Id,

				QuestionText = createDTO.Text,

				PostedById = userId,

				LastEditorId = userId,

				DatePosted = time,
				DateLastEdited = time,
			};

			tranche.Project.Questions.Add(question);

			await _dataContext.SaveChangesAsync();
			return Ok(question.Id);
		}

		/// <summary>
		/// Adds an answer to the question
		/// </summary>
		[HttpPut("set_answer/{id}")]
		public async Task<IActionResult> SetAnswer(int id, [FromForm] PostSetAnswerDTO answerDTO) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!_CheckQuestionAccess(question))
				return Unauthorized();

			var time = DateTime.Now;

			question.QuestionAnswer = answerDTO.Answer;
			question.AnsweredById = _access.GetUserID();
			question.DateAnswered = time;
			question.DateLastEdited = time;

			question.QuestionApprovedById = null;
			question.DateQuestionApproved = null;
			question.AnswerApprovedById = null;
			question.DateAnswerApproved = null;

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Sets the approval status of questions
		/// </summary>
		[HttpPut("set_approval_q/{pid}/{approve}")]
		public async Task<IActionResult> SetPostsApprovalQ(int pid, bool approve, [FromForm] PostSetApproveDTO approveDTO) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Unauthorized();

			var questions = project.Questions
				.Where(x => approveDTO.Questions.Any(y => y == x.Id))
				.ToList();
			if (questions.Count != approveDTO.Questions.Count) {
				var invalidIds = approveDTO.Questions
					.Except(questions.Select(x => x.Id));
				return BadRequest("Questions not found: " + string.Join(", ", invalidIds));
			}

			var time = DateTime.Now;

			foreach (var i in questions) {
				if (approve) {
					i.QuestionApprovedById = _access.GetUserID();
					i.DateQuestionApproved = time;
				}
				else {
					// Unapproving the question also unapproves its answer
					i.QuestionApprovedById = null;
					i.DateQuestionApproved = null;
					i.AnswerApprovedById = null;
					i.DateAnswerApproved = null;
				}
				i.DateLastEdited = time;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Sets the approval status of answers to questions
		/// </summary>
		[HttpPut("set_approval_a/{pid}/{approve}")]
		public async Task<IActionResult> SetPostsApprovalA(int pid, bool approve, [FromForm] PostSetApproveDTO approveDTO) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Unauthorized();

			var questions = project.Questions
				.Where(x => approveDTO.Questions.Any(y => y == x.Id))
				.ToList();
			if (questions.Count != approveDTO.Questions.Count) {
				var invalidIds = approveDTO.Questions
					.Except(questions.Select(x => x.Id));
				return BadRequest("Questions not found: " + string.Join(", ", invalidIds));
			}

			{
				var unanswered = questions
					.Where(x => x.QuestionAnswer == null)
					.Select(x => x.Id)
					.ToList();
				if (unanswered.Count > 0) {
					return BadRequest("Unanswered questions: " + string.Join(", ", unanswered));
				}
			}

			var time = DateTime.Now;

			foreach (var i in questions) {
				if (approve) {
					i.AnswerApprovedById = _access.GetUserID();
					i.DateAnswerApproved = time;
				}
				else {
					i.AnswerApprovedById = null;
					i.DateAnswerApproved = null;
				}
				i.DateLastEdited = time;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}
	}
}
