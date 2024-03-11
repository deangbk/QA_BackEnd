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
		[HttpPost("page/{pid}")]
		public async Task<IActionResult> GetPosts(int pid, [FromBody] PostGetFilterAndPaginateDTO dto,
			[FromQuery] int details = 0)
		{
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

			var filterDTO = dto.Filter;
			var pageDTO = dto.Paginate;

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

			// Paginate result; but return everything if paginate DTO doesn't exist
			if (pageDTO != null) {
				int countPerPage = pageDTO.CountPerPage;
				int maxPages = (int)Math.Ceiling(countTotal / (double)countPerPage);

				listPosts = listPosts
					.Skip(pageDTO.Page!.Value * countPerPage)
					.Take(countPerPage)
					.ToList();
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
		[HttpPost("general/{pid}")]
		public async Task<IActionResult> PostGeneralQuestion(int pid, [FromBody] PostCreateDTO createDTO) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

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
		[HttpPost("account/{pid}")]
		public async Task<IActionResult> PostAccountQuestion(int pid, [FromBody] PostCreateDTO createDTO) {
			if (createDTO.AccountId == null) {
				ModelState.AddModelError("AccountId", "AccountId cannot be null");
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			Account? account = await Queries.GetAccountFromId(_dataContext, createDTO.AccountId.Value);
			if (account == null)
				return BadRequest("Account not found");

			if (!_access.AllowToTranche(account.Tranche))
				return Forbid();

			Project project = account.Project;

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
		/// Adds an answer to a question
		/// </summary>
		[HttpPut("answer/{pid}")]
		public async Task<IActionResult> SetAnswer(int pid, [FromBody] PostSetAnswerDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			// Only staff can add answer
			if (!_access.AllowManageProject(project))
				return Forbid();

			Question? question = await Queries.GetQuestionFromId(_dataContext, dto.Id!.Value);
			if (question == null)
				return BadRequest("Question not found");

			var time = DateTime.Now;
			var userId = _access.GetUserID();

			question.QuestionAnswer = dto.Answer;
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
				return Forbid();

			PostHelpers.EditQuestion(question,
				editDTO.Text, editDTO.Category ?? question.Category,
				_access.GetUserID());

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteQuestion(int id) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!PostHelpers.AllowUserEditPost(_access, question))
				return Forbid();

			question.Project.Questions.Remove(question);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Sets the approval status of questions
		/// </summary>
		[HttpPut("approve/q/{pid}")]
		public async Task<IActionResult> SetPostsApprovalQ(int pid, [FromBody] PostSetApproveDTO approveDTO) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			var questions = project.Questions
				.Where(x => approveDTO.Questions.Any(y => y == x.Id))
				.ToList();
			if (questions.Count != approveDTO.Questions.Count) {
				var invalidIds = approveDTO.Questions
					.Except(questions.Select(x => x.Id));
				return BadRequest("Questions not found: " + invalidIds.ToStringEx());
			}

			var time = DateTime.Now;
			var userId = _access.GetUserID();

			foreach (var i in questions) {
				PostHelpers.ApproveQuestion(i, userId, approveDTO.Approve!.Value);
				i.DateLastEdited = time;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Sets the approval status of answers to questions
		/// </summary>
		[HttpPut("approve/a/{pid}")]
		public async Task<IActionResult> SetPostsApprovalA(int pid, [FromBody] PostSetApproveDTO approveDTO) {
     
            Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			var questions = project.Questions
				.Where(x => approveDTO.Questions.Any(y => y == x.Id))
				.ToList();
			if (questions.Count != approveDTO.Questions.Count) {
				var invalidIds = approveDTO.Questions
					.Except(questions.Select(x => x.Id));
				return BadRequest("Questions not found: " + invalidIds.ToStringEx());
			}

			{
				var unanswered = questions
					.Where(x => x.QuestionAnswer == null)
					.Select(x => x.Id)
					.ToList();
				if (unanswered.Count > 0) {
					return BadRequest("Unanswered questions: " + unanswered.ToStringEx());
				}
			}

			var time = DateTime.Now;
			var userId = _access.GetUserID();

			foreach (var i in questions) {
				PostHelpers.ApproveAnswer(i, userId, approveDTO.Approve!.Value);
				i.DateLastEdited = time;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Posts general questions in bulk
		/// </summary>
		[HttpPost("bulk/general/{pid}")]
		public async Task<IActionResult> PostGeneralQuestionMultiple(int pid, [FromBody] List<PostCreateDTO> dtos) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);

			List<Question> listQuestions = new();

			foreach (var i in dtos) {
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
		[HttpPost("bulk/account/{pid}")]
		public async Task<IActionResult> PostAccountQuestionMultiple(int pid, [FromBody] List<PostCreateDTO> dtos) {
			if (dtos.Any(x => x.AccountId == null)) {
				ModelState.AddModelError("AccountId", "AccountId cannot be null");
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

			{
				// Detect invalid accounts + check access

				var accountIds = dtos.Select(x => x.AccountId!.Value);
				var mapAccounts = await Queries.GetAccountsMapFromIds(_dataContext, accountIds);

				foreach (var (_, i) in mapAccounts!) {
					if (!_access.AllowToTranche(i.Tranche))
						return Forbid($"No access to account \"{i.GetIdentifierName()}\"");
				}

				if (mapAccounts!.Count != dtos.Count) {
					var invalidAccounts = accountIds.Except(mapAccounts.Keys);
					return BadRequest("Account not found: " + invalidAccounts.ToStringEx());
				}
			}

			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);

			List<Question> listQuestions = new();

			foreach (var i in dtos) {
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
		[HttpPut("bulk/edit/{pid}")]
		public async Task<IActionResult> EditQuestionMultiple(int pid, [FromBody] List<PostEditMultipleDTO> dtos) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();

			var ids = dtos.Select(x => x.Id!.Value);
			var mapQuestions = await Queries.GetQuestionsMapFromIds(_dataContext, ids);

			{
				// Detect invalid questions + check access

				foreach (var (_, i) in mapQuestions!) {
					if (!PostHelpers.AllowUserEditPost(_access, i))
						return Forbid($"No access to question {i.Id}");
				}

				if (mapQuestions!.Count != dtos.Count) {
					var invalidIds = ids.Except(mapQuestions.Keys);
					return BadRequest("Question not found: " + invalidIds.ToStringEx());
				}
			}

			var userId = _access.GetUserID();

			foreach (var i in dtos) {
				var question = mapQuestions[i.Id!.Value];
				PostHelpers.EditQuestion(question,
					i.Text, i.Category ?? question.Category,
					userId);
			}

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}

		/// <summary>
		/// Adds answers to questions in bulk
		/// </summary>
		[HttpPut("bulk/answer/{pid}")]
		public async Task<IActionResult> SetAnswerMultiple(int pid, [FromBody] List<PostSetAnswerDTO> dtos) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			// Only staff can add answers
			if (!_access.AllowManageProject(project))
				return Forbid();

			var ids = dtos.Select(x => x.Id!.Value);
			var mapQuestions = await Queries.GetQuestionsMapFromIds(_dataContext, ids);

			{
				// Detect invalid questions

				if (mapQuestions!.Count != dtos.Count) {
					var invalidIds = ids.Except(mapQuestions.Keys);
					return BadRequest("Question not found: " + invalidIds.ToStringEx());
				}
			}

			var time = DateTime.Now;
			var userId = _access.GetUserID();

			foreach (var i in dtos) {
				var question = mapQuestions[i.Id!.Value];

				question.QuestionAnswer = i.Answer;
				question.AnsweredById = userId;
				question.DateAnswered = time;
				question.DateLastEdited = time;

				// Don't auto-approve on bulk answer
				PostHelpers.ApproveQuestion(question, userId, true);
				PostHelpers.ApproveAnswer(question, userId, false);
			}

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}
	}
}
