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

		/// <summary>
		/// Gets project questions as paginated list
		/// <para>Valid filters for filterDTO:</para>
		/// <list type="bullet">
		///		<item>TicketID</item>
		///		<item>PosterID</item>
		///		<item>Tranche</item>
		///		<item>Account</item>
		///		<item>PostedFrom</item>
		///		<item>PostedTo</item>
		///		<item>OnlyAnswered</item>
		///		<item>SearchTerm</item>
		/// </list>
		/// <para>Will only return approved questions.</para>
		/// </summary>
		[HttpGet("get_posts/{pid}")]
		public async Task<IActionResult> GetPosts(int pid, [FromQuery] PostGetFilterDTO filterDTO,
			[FromQuery] PaginateDTO pageDTO, [FromQuery] int details = 0) {

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			// Gets only approved
			IQueryable<Question> query = Queries.GetApprovedQuestionsQuery(_dataContext, pid);

			try {
				query = PostHelpers.FilterQuery(query, filterDTO);
			}
			catch (ArgumentException e) {
				ModelState.AddModelError(e.ParamName!, e.Message);
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			// Then filter based on access
			//    NOTE: Might be a little inefficient, but the access logic is too complicated to be handled on the DB
			var listPosts = (await query.ToListAsync())
				.Where(x => PostHelpers.AllowUserReadPost(_access, x))
				.ToList();
			int countTotal = listPosts.Count();

			// Paginate result; but return everything if Page is less than 0
			{
				int countPerPage = pageDTO.CountPerPage;

				if (pageDTO.Page >= 0 && pageDTO.CountPerPage >= 1) {
					int maxPages = (int)Math.Ceiling(countTotal / (double)countPerPage);

					listPosts = listPosts
						.Skip(pageDTO.Page * countPerPage)
						.Take(countPerPage)
						.ToList();
				}
			}

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

			if (!PostHelpers.AllowUserReadPost(_access, question))
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
		public async Task<IActionResult> PostGeneralQuestion(int pid, [FromBody] PostCreateDTO createDTO) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Unauthorized();

			var question = PostHelpers.CreateQuestion(
				QuestionType.General, project.Id, 
				createDTO.Text, createDTO.Category ?? QuestionCategory.General, 
				_access.GetUserID());

			project.Questions.Add(question);

			await _dataContext.SaveChangesAsync();
			return Ok(question.Id);
		}

		/// <summary>
		/// Posts an account question
		/// </summary>
		[HttpPost("post_question_a/{pid}")]
		public async Task<IActionResult> PostAccountQuestion(int pid, [FromBody] PostCreateDTO createDTO) {
			if (createDTO.AccountId == null) {
				ModelState.AddModelError("AccountId", "AccountId cannot be null");
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Unauthorized();

			Account? account = await Queries.GetAccountFromId(_dataContext, createDTO.AccountId.Value);
			if (account == null)
				return BadRequest("Account not found");

			if (!_access.AllowToTranche(account.Tranche))
				return Unauthorized();

			var question = PostHelpers.CreateQuestion(
				QuestionType.Account, project.Id,
				createDTO.Text, createDTO.Category ?? QuestionCategory.General,
				_access.GetUserID());
			question.AccountId = account.Id;

			account.Project.Questions.Add(question);
			await _dataContext.SaveChangesAsync();

			return Ok(question.Id);
		}

		/// <summary>
		/// Adds an answer to the question
		/// </summary>
		[HttpPut("set_answer/{id}")]
		public async Task<IActionResult> SetAnswer(int id, [FromBody] PostSetAnswerDTO answerDTO) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			// Only staff can add answer
			if (!PostHelpers.AllowUserManagePost(_access, question))
				return Unauthorized();

			var time = DateTime.Now;

			question.QuestionAnswer = answerDTO.Answer;
			question.AnsweredById = _access.GetUserID();
			question.DateAnswered = time;
			question.DateLastEdited = time;

			// Automatically approve
			question.AnswerApprovedById = _access.GetUserID();
			question.DateAnswerApproved = time;

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Edits the question
		/// </summary>
		[HttpPut("edit/{id}")]
		public async Task<IActionResult> EditQuestion(int id, [FromBody] PostEditDTO editDTO) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!PostHelpers.AllowUserEditPost(_access, question))
				return Unauthorized();

			PostHelpers.EditQuestion(question, 
				editDTO.Text, editDTO.Category ?? question.Category, 
				_access.GetUserID());

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Sets the approval status of questions
		/// </summary>
		[HttpPut("set_approval_q/{pid}")]
		public async Task<IActionResult> SetPostsApprovalQ(int pid, [FromBody] PostSetApproveDTO approveDTO) {
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
				return BadRequest("Questions not found: " + ValueHelpers.PrintEnumerable(invalidIds));
			}

			var time = DateTime.Now;

			foreach (var i in questions) {
				if (approveDTO.Approve) {
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
		public async Task<IActionResult> SetPostsApprovalA(int pid, bool approve, [FromBody] PostSetApproveDTO approveDTO) {
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
				return BadRequest("Questions not found: " + ValueHelpers.PrintEnumerable(invalidIds));
			}

			{
				var unanswered = questions
					.Where(x => x.QuestionAnswer == null)
					.Select(x => x.Id)
					.ToList();
				if (unanswered.Count > 0) {
					return BadRequest("Unanswered questions: " + ValueHelpers.PrintEnumerable(unanswered));
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
