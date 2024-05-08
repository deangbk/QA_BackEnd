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
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/post")]
	[Authorize]
	public class PostController : Controller {
		private readonly ILogger<PostController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IProjectRepository _repoProject;

		public PostController(
			ILogger<PostController> logger, 
			DataContext dataContext, IAccessService access, 
			IProjectRepository repoProject) 
		{
			_logger = logger;

			_dataContext = dataContext;
			_access = access;


			_repoProject = repoProject;
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
		[HttpPost("page")]
		public async Task<IActionResult> GetPosts([FromBody] PostGetFilterAndPaginateDTO dto,
			[FromQuery] int details = 0)
		{
			var project = await _repoProject.GetProjectAsync();

			var filterDTO = dto.Filter;
			var pageDTO = dto.Paginate;

			// Gets only approved
			IQueryable<Question> query = Queries.GetApprovedQuestionsQuery(_dataContext, project.Id);

			try {
				query = PostHelpers.FilterQuery(query, filterDTO);
			}
			catch (ArgumentException e) {
				ModelState.AddModelError(e.ParamName!, e.Message);
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			// Then filter based on access
			var listPosts = PostHelpers.FilterUserReadPost(_access, await query.ToListAsync());
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
		/// Adds an answer to a question
		/// </summary>
		[HttpPut("answer")]
		public async Task<IActionResult> SetAnswer([FromBody] PostSetAnswerDTO dto) {
			var project = await _repoProject.GetProjectAsync();
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
		/// Edits a question, sent to post temporaily to fix a bug will fix later--- Nat don't change it
		/// </summary>
		[HttpPost("edit")]
		public async Task<IActionResult> EditQuestion([FromBody] PostEditDTO dto) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			Question? question = await Queries.GetQuestionFromId(_dataContext, dto.Id!.Value);
			if (question == null)
				return BadRequest("Question not found");

			var userId = _access.GetUserID();

			PostHelpers.EditQuestion(question, dto, userId, 
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
		[HttpPut("approve")]
		public async Task<IActionResult> SetPostsApproval([FromBody] PostSetApproveDTO dto, [FromQuery] string mode) {
			var project = await _repoProject.GetProjectAsync();
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

			var questions = await Queries.GetQuestionsMapFromIds(_dataContext, dto.Questions);
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
			var userId = _access.GetUserID();

			bool approve = dto.Approve!.Value;

			foreach (var (_, q) in questions) {
				if (modeI == 0) {
					PostHelpers.ApproveQuestion(q, userId, approve);
				}
				else {
					PostHelpers.ApproveAnswer(q, userId, approve);
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
		[HttpPost("bulk")]
		public async Task<IActionResult> PostQuestionMultiple([FromBody] List<PostCreateDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();

			var userId = _access.GetUserID();
			bool isStaff = _access.IsSuperUser();

			var accountIds = dtos
				.Where(x => x.AccountId != null)
				.Select(x => x.AccountId!.Value)
				.ToList();
			var mapAccounts = await Queries.GetAccountsMapFromIds(_dataContext, accountIds);

			if (accountIds.Count > 0) {
				// Detect invalid accounts + check access

				{
					var err = ValueHelpers.CheckInvalidIds(
						accountIds, mapAccounts.Keys, "Account");
					if (err != null) {
						return BadRequest(err);
					}
				}

				var tranches = mapAccounts.Values
					.Select(x => x.Tranche).Distinct();
				foreach (var t in tranches) {
					if (!_access.AllowToTranche(t))
						return Forbid($"No access to tranche \"{t.Name}\" (id={t.Id})");
				}

				if (!isStaff) {
					if (dtos.Where(x => x.PostAs != null).Any()) {
						return Forbid($"post_as can only be used by managers");
					}
					if (dtos.Where(x => x.Approve == true).Any()) {
						return Forbid($"approve can only be used by managers");
					}
				}
			}

			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);

			var listQuestions = dtos.Select(d => {
				var question = PostHelpers.CreateQuestion(
					QuestionType.General, project.Id,
					d.Text, d.Category ?? "general",
					d.PostAs ?? userId);
				if (d.DateSent is not null)
					question.DateSent = d.DateSent.Value;

				if (d.AccountId != null) {
					question.Type = QuestionType.Account;
					question.AccountId = d.AccountId;
				}

				// Increment num with each question added
				question.QuestionNum = ++maxQuestionNo;

				// Auto-approve if the request is made by a manager
				if (isStaff && d.Approve == true) {
					PostHelpers.ApproveQuestion(question, userId, true);
				}

				return question;
			}).ToList();

			project.Questions.AddRange(listQuestions);
			await _dataContext.SaveChangesAsync();

			// Return IDs of all created questions
			var questionIds = listQuestions.Select(x => x.Id).ToList();
			return Ok(questionIds);
		}

		/// <summary>
		/// Edit questions in bulk
		/// </summary>
		[HttpPut("bulk/edit")]
		public async Task<IActionResult> EditQuestionMultiple([FromBody] List<PostEditDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			var ids = dtos.Select(x => x.Id!.Value);
			var mapQuestions = await Queries.GetQuestionsMapFromIds(_dataContext, ids);

			// Detect invalid questions
			{
				var err = ValueHelpers.CheckInvalidIds(
					ids, mapQuestions.Keys, "Question");
				if (err != null) {
					return BadRequest(err);
				}
			}

			var userId = _access.GetUserID();

			foreach (var dto in dtos) {
				var question = mapQuestions[dto.Id!.Value];
				PostHelpers.EditQuestion(question, dto, userId,
					question.QuestionApprovedBy != null);
			}

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}

		/// <summary>
		/// Adds answers to questions in bulk
		/// </summary>
		[HttpPut("bulk/answer")]
		public async Task<IActionResult> SetAnswerMultiple([FromBody] List<PostSetAnswerDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			var ids = dtos.Select(x => x.Id!.Value);
			var mapQuestions = await Queries.GetQuestionsMapFromIds(_dataContext, ids);

			// Detect invalid questions
			{
				var err = ValueHelpers.CheckInvalidIds(
					ids, mapQuestions.Keys, "Question");
				if (err != null) {
					return BadRequest(err);
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
