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

		private readonly int _userId, _projectId;

		public PostController(DataContext dataContext, ILogger<PostController> logger, IAccessService access) {
			_dataContext = dataContext;
			_logger = logger;

			_access = access;
			{
			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();

				_userId = _access.GetUserID();
				_projectId = _access.GetProjectID();
		}
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

			question.QuestionAnswer = dto.Answer;
			question.AnsweredById = _userId;
			question.DateAnswered = time;
			question.DateLastEdited = time;

			// Automatically approve
			PostHelpers.ApproveAnswer(question, _userId, true);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Edits a question
		/// </summary>
		[HttpPut("edit/{pid}")]
		public async Task<IActionResult> EditQuestion(int pid, [FromBody] PostEditDTO dto) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			// Only staff can edit
			if (!_access.AllowManageProject(project))
				return Forbid();

			Question? question = await Queries.GetQuestionFromId(_dataContext, dto.Id!.Value);
			if (question == null)
				return BadRequest("Question not found");

			PostHelpers.EditQuestion(question, dto, _userId, 
				question.QuestionApprovedBy != null);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteQuestion(int id) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!PostHelpers.AllowUserManagePost(_access, question))
				return Forbid();

			question.Project.Questions.Remove(question);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Sets the approval status of questions or answers
		/// </summary>
		[HttpPut("bulk/approve/{pid}")]
		public async Task<IActionResult> SetPostsApproval(int pid, [FromBody] PostSetApproveDTO dto, [FromQuery] string mode) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			int modeI = mode switch {
				"q" => 0,
				"a" => 1,
				_ => -1,
			};
			if (modeI == -1) {
				return BadRequest("mode must be either \"q\" or \"a\"");
				}

			var questions = (await Queries.GetQuestionsMapFromIds(_dataContext, dto.Questions))!;
			{
				// Check invalid IDs and duplicate IDs

				var err = ValueHelpers.CheckInvalidIds(
					dto.Questions, questions.Keys, "Question");
				if (err != null) {
					return BadRequest(err);
				}
			}

			// If approving answers, all posts must already have an answer
			if (modeI == 1) {
				var unanswered = questions
					.Where(x => x.Value.QuestionAnswer == null)
					.Select(x => x.Value.Id)
					.ToList();
				if (unanswered.Any()) {
					return BadRequest("Unanswered questions: " + unanswered.ToStringEx());
				}
			}

			var time = DateTime.Now;
			bool approve = dto.Approve!.Value;

			foreach (var (_, q) in questions) {
				if (modeI == 0) {
					PostHelpers.ApproveQuestion(q, _userId, approve);
			}
				else {
					PostHelpers.ApproveAnswer(q, _userId, approve);
				}

				q.DateLastEdited = time;
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

				{
					var err = ValueHelpers.CheckInvalidIds(
						accountIds, mapAccounts.Keys, "Account");
					if (err != null) {
						return BadRequest(err);
					}
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
		public async Task<IActionResult> EditQuestionMultiple(int pid, [FromBody] List<PostEditDTO> dtos) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			var ids = dtos.Select(x => x.Id!.Value);
			var mapQuestions = await Queries.GetQuestionsMapFromIds(_dataContext, ids);

			// Detect invalid questions
			{
				var err = ValueHelpers.CheckInvalidIds(
					ids, mapQuestions!.Keys, "Question");
				if (err != null) {
					return BadRequest(err);
				}
			}

			foreach (var dto in dtos) {
				var question = mapQuestions[dto.Id!.Value];
				PostHelpers.EditQuestion(question, dto, _userId,
					question.QuestionApprovedBy != null);
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

			// Detect invalid questions
			{
				var err = ValueHelpers.CheckInvalidIds(
					ids, mapQuestions!.Keys, "Question");
				if (err != null) {
					return BadRequest(err);
				}
			}

			var time = DateTime.Now;

			foreach (var i in dtos) {
				var question = mapQuestions[i.Id!.Value];

				question.QuestionAnswer = i.Answer;
				question.AnsweredById = _userId;
				question.DateAnswered = time;
				question.DateLastEdited = time;

				// Don't auto-approve on bulk answer
				PostHelpers.ApproveQuestion(question, _userId, true);
				PostHelpers.ApproveAnswer(question, _userId, false);
			}

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}
	}
}
