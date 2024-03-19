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

			{
				if (!_access.IsValidUser())
					throw new AccessUnauthorizedException();

				_userId = _access.GetUserID();
				_projectId = _access.GetProjectID();
			}

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
		[HttpPut("approve/{pid}")]
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
		[HttpPost("bulk/{pid}")]
		public async Task<IActionResult> PostQuestionMultiple(int pid, [FromBody] List<PostCreateDTO> dtos) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();
			bool isStaff = _access.IsSuperUser();

			var accountIds = dtos
				.Where(x => x.AccountId != null)
				.Select(x => x.AccountId!.Value)
				.ToList();
			var mapAccounts = (await Queries.GetAccountsMapFromIds(_dataContext, accountIds))!;

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

				if (!isStaff && dtos.Where(x => x.PostAs != null).Any()) {
					return Forbid($"post_as can only be used by managers");
				}
			}

			var maxQuestionNo = PostHelpers.GetHighestQuestionNo(project);

			var listQuestions = dtos.Select(d => {
				var question = PostHelpers.CreateQuestion(
					QuestionType.General, project.Id,
					d.Text, d.Category ?? "general",
					d.PostAs ?? _userId);
				if (d.DateSent is not null)
					question.DateSent = d.DateSent.Value;

				if (d.AccountId != null) {
					question.Type = QuestionType.Account;
					question.AccountId = d.AccountId;
				}

				// Increment num with each question added
				question.QuestionNum = ++maxQuestionNo;

				// Auto-approve if the request is made by a manager
				if (isStaff) {
					PostHelpers.ApproveQuestion(question, _userId, true);
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
