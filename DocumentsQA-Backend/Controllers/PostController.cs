﻿using System;
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
			int countTotal = listPosts.Count;

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

			var listPostTables = listPosts.Select(x => x.ToJsonTable(details));

			return Ok(new JsonTable() {
				["count_total"] = countTotal,
				["posts"] = listPostTables,
			});
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
				createDTO.Text, createDTO.Category ?? "general",
				_access.GetUserID());

			// Set the question number to 1 more than the current highest
			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);
			question.QuestionNum = maxQuestionNo + 1;

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
				createDTO.Text, createDTO.Category ?? "general",
				_access.GetUserID());

			question.AccountId = account.Id;

			// Set the question number to 1 more than the current highest
			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);
			question.QuestionNum = maxQuestionNo + 1;

			project.Questions.Add(question);
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
			var userId = _access.GetUserID();

			question.QuestionAnswer = answerDTO.Answer;
			question.AnsweredById = userId;
			question.DateAnswered = time;
			question.DateLastEdited = time;

			// Automatically approve
			PostHelpers.ApproveAnswer(question, userId, true);

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
			var userId = _access.GetUserID();

			foreach (var i in questions) {
				PostHelpers.ApproveQuestion(i, userId, approveDTO.Approve);
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
			var userId = _access.GetUserID();

			foreach (var i in questions) {
				PostHelpers.ApproveAnswer(i, userId, approveDTO.Approve);
				i.DateLastEdited = time;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Posts general questions in bulk
		/// </summary>
		[HttpPost("post_question_g_multiple/{pid}")]
		public async Task<IActionResult> PostGeneralQuestionMultiple(int pid, [FromBody] PostCreateMultipleDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Unauthorized();

			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);

			List<Question> listQuestions = new();

			foreach (var i in dto.Posts) {
				var question = PostHelpers.CreateQuestion(
					QuestionType.General, project.Id,
					i.Text, i.Category ?? "general",
					_access.GetUserID());

				// Increment num with each question added
				question.QuestionNum = ++maxQuestionNo;

				listQuestions.Add(question);
			}

			project.Questions.AddRange(listQuestions);
			await _dataContext.SaveChangesAsync();

			// Return IDs of all created questions
			var questionIds = listQuestions.Select(x => x.Id).ToList();

			return Ok(questionIds);
		}

		/// <summary>
		/// Posts account questions in bulk
		/// </summary>
		[HttpPost("post_question_a_multiple/{pid}")]
		public async Task<IActionResult> PostAccountQuestionMultiple(int pid, [FromBody] PostCreateMultipleDTO dto) {
			if (dto.Posts.Any(x => x.AccountId == null)) {
				ModelState.AddModelError("AccountId", "AccountId cannot be null");
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Unauthorized();

			{
				// Detect invalid accounts + check access

				var accountIds = dto.Posts.Select(x => x.AccountId!.Value);
				var mapAccounts = await Queries.GetAccountsMapFromIds(_dataContext, accountIds);

				foreach (var (_, i) in mapAccounts!) {
					if (!_access.AllowToTranche(i.Tranche))
						return Unauthorized();
				}

				if (mapAccounts!.Count != dto.Posts.Count) {
					var invalidAccounts = accountIds.Except(mapAccounts.Keys);
					return BadRequest("Account not found: " + ValueHelpers.PrintEnumerable(invalidAccounts));
				}
			}

			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);

			List<Question> listQuestions = new();

			foreach (var i in dto.Posts) {
				var question = PostHelpers.CreateQuestion(
					QuestionType.Account, project.Id,
					i.Text, i.Category ?? "general",
					_access.GetUserID());

				question.AccountId = i.AccountId;

				// Increment num with each question added
				question.QuestionNum = ++maxQuestionNo;

				listQuestions.Add(question);
			}

			project.Questions.AddRange(listQuestions);
			await _dataContext.SaveChangesAsync();

			// Return IDs of all created questions
			var questionIds = listQuestions.Select(x => x.Id).ToList();

			return Ok(questionIds);
		}

		/// <summary>
		/// Edit questions in bulk
		/// </summary>
		[HttpPost("edit_question_multiple/{pid}")]
		public async Task<IActionResult> EditQuestionMultiple(int pid, [FromBody] PostEditMultipleDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Unauthorized();

			var ids = dto.Posts.Select(x => x.Id);
			var mapQuestions = await Queries.GetQuestionsMapFromIds(_dataContext, ids);

			{
				// Detect invalid questions + check access

				foreach (var (_, i) in mapQuestions!) {
					if (!PostHelpers.AllowUserEditPost(_access, i))
						return Unauthorized();
				}

				if (mapQuestions!.Count != dto.Posts.Count) {
					var invalidIds = ids.Except(mapQuestions.Keys);
					return BadRequest("Question not found: " + ValueHelpers.PrintEnumerable(invalidIds));
				}
			}

			var userId = _access.GetUserID();

			foreach (var i in dto.Posts) {
				var question = mapQuestions[i.Id];
				PostHelpers.EditQuestion(question,
					i.Text, i.Category ?? question.Category,
					userId);
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Adds answers to questions in bulk
		/// </summary>
		[HttpPut("set_answer_multiple/{pid}")]
		public async Task<IActionResult> SetAnswerMultiple(int pid, [FromBody] PostSetAnswerMultipleDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			// Only staff can add answers
			if (!_access.AllowManageProject(project))
				return Unauthorized();

			var ids = dto.Answers.Select(x => x.Id);
			var mapQuestions = await Queries.GetQuestionsMapFromIds(_dataContext, ids);

			{
				// Detect invalid questions + check access

				// No need to check for per-question access; a staff always has management rights to the whole project
				/*
				foreach (var (_, i) in mapQuestions!) {
					if (!PostHelpers.AllowUserManagePost(_access, i))
						return Unauthorized();
				}
				*/

				if (mapQuestions!.Count != dto.Answers.Count) {
					var invalidIds = ids.Except(mapQuestions.Keys);
					return BadRequest("Question not found: " + ValueHelpers.PrintEnumerable(invalidIds));
				}
			}

			var time = DateTime.Now;
			var userId = _access.GetUserID();

			foreach (var i in dto.Answers) {
				var question = mapQuestions[i.Id];

				question.QuestionAnswer = i.Answer;
				question.AnsweredById = userId;
				question.DateAnswered = time;
				question.DateLastEdited = time;

				// Don't auto-approve on bulk answer
				PostHelpers.ApproveQuestion(question, userId, true);
				PostHelpers.ApproveAnswer(question, userId, false);
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Get question comments, ordered by number
		/// </summary>
		[HttpGet("get_comments/{id}")]
		public async Task<IActionResult> GetComments(int id) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!PostHelpers.AllowUserReadPost(_access, question))
				return Unauthorized();

			var listComments = question.Comments;
			var listCommentTables = listComments
				.OrderBy(x => x.CommentNum)
				.Select(x => x.ToJsonTable(1));

			return Ok(listCommentTables);
		}

		/// <summary>
		/// Add comment to question
		/// </summary>
		[HttpPost("add_comment/{id}")]
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
		[HttpDelete("delete_comment/{id}")]
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
	}
}
