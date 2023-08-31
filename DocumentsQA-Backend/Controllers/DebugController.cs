using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/debug")]
	public class DebugController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<DebugController> _logger;

		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signinManager;
		private readonly RoleManager<AppRole> _roleManager;

		private readonly IEmailService _emailService;

		public DebugController(DataContext dataContext, ILogger<DebugController> logger,
			UserManager<AppUser> userManager, SignInManager<AppUser> signinManager, RoleManager<AppRole> roleManager,
			IEmailService emailService) {

			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
			_signinManager = signinManager;
			_roleManager = roleManager;

			_emailService = emailService;
		}

		// -----------------------------------------------------

		[HttpPost("create_users")]
		public async Task<IActionResult> CreateUsers() {
			var rolesMap = new Dictionary<AppRole, AppRole[]> {
				[AppRole.Admin] = new[] { AppRole.Admin, AppRole.User },
				[AppRole.Manager] = new[] { AppRole.Manager, AppRole.User },
				[AppRole.User] = new[] { AppRole.User },
			};

			var _NewUser = async (string name, string email, string pass, AppRole role) => {
				var user = new AppUser() {
					UserName = email,
					Email = email,
					DisplayName = name,
					Company = "Holy Roman Empire",
					DateCreated = DateTime.Now,
				};

				var result = await _userManager.CreateAsync(user, pass);
				if (result.Succeeded) {
					// Set user role
					var roles = rolesMap[role];
					foreach (var iRole in roles) {
						await _userManager.AddClaimAsync(user, new Claim("role", iRole.Name));
						await _userManager.AddToRoleAsync(user, iRole.Name);
					}
				}
				return result.Succeeded;
			};

			string password = "pasaworda55";

			for (int i = 0; i < 2; ++i)
				await _NewUser("DragonAdmin" + i, i.ToString() + "@test.admin", password, AppRole.Admin);

			for (int i = 0; i < 3; ++i)
				await _NewUser("ForumModerator" + i, i.ToString() + "@test.manager", password, AppRole.Manager);

			for (int i = 0; i < 8; ++i)
				await _NewUser("StupidUser" + i, i.ToString() + "@test.user", password, AppRole.User);

			return Ok();
		}

		[HttpPost("create_project/{name}")]
		public async Task<IActionResult> CreateProject(string name) {
			Project project = new Project {
				Name = name,
				DisplayName = name,
				CompanyName = "Holy Roman Empire",
				ProjectStartDate = DateTime.Now,
				ProjectEndDate = new DateTime(2025, 12, 1),
			};
			_dataContext.Projects.Add(project);

			await _dataContext.SaveChangesAsync();

			return Ok(project.Id);
		}

		[HttpPost("create_tranches/{pid}")]
		public async Task<IActionResult> CreateProjectTranches(int pid) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			var _MakeTranche = (string name) => new Tranche { Project = project, Name = name };

			var tranches = "ABCDEF".Select(c => _MakeTranche(c.ToString())).ToList();

			project.Tranches = new List<Tranche>(tranches);
			var count = await _dataContext.SaveChangesAsync();

			return Ok(count);
		}

		[HttpPost("send_test_mail")]
		public async Task<IActionResult> TestSendEmail([FromQuery] string email) {
			var res = await _emailService.SendTestEmail(email);
			if (res)
				return Ok();
			else
				return StatusCode(500);
		}

		[HttpPut("create_accounts/{tid}/{count}")]
		public async Task<IActionResult> CreateAccounts(int tid, int count) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");

			Random rnd = new Random(DateTime.Now.GetHashCode());
			var _RandString = (int len) => {
				const string chars = "abcde fghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ";
				string s = "";
				for (int i = 0; i < len; ++i) {
					s += chars[rnd.Next(0, chars.Length)];
				}
				return s;
			};

			var names = Enumerable.Range(0, count)
				.Select(x => _RandString(rnd.Next(8, 32)))
				.ToArray();
			var accounts = names.Select(
				(x, i) => new Account() {
					TrancheId = tranche.Id,
					AccountNo = i + 1,
					AccountName = x,
				})
				.ToList();

			// Wrap all operations in a transaction so failure would revert the entire thing
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				_dataContext.Accounts.Where(x => x.TrancheId == tid).ExecuteDelete();
				await _dataContext.SaveChangesAsync();

				_dataContext.Accounts.AddRange(accounts);
				await _dataContext.SaveChangesAsync();

				await transaction.CommitAsync();
			}

			return Ok();
		}

		[HttpPost("create_questions/{tid}/{postBy}/{count}")]
		public async Task<IActionResult> CreateQuestions(int tid, int postBy, int count) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, tid);
			if (tranche == null)
				return BadRequest("Tranche not found");

			Random rnd = new Random(DateTime.Now.GetHashCode());
			var _RandString = (int len) => {
				string s = "";
				for (int i = 0; i < len; ++i) {
					if (rnd.NextDouble() < 0.5)
						s += (char)rnd.Next(0x20, 0x7e + 1);		// English chars
					else
						s += (char)rnd.Next(0x0e01, 0x0e33 + 1);	// Thai chars
				}
				return s;
			};

			int maxQuestionNum = 1;
			try {
				maxQuestionNum = await _dataContext.Questions
					.Where(x => x.ProjectId == tranche.ProjectId)
					.Select(x => x.QuestionNum)
					.MaxAsync();
			}
			catch (Exception) {
				maxQuestionNum = 1;
			}

			var accounts = await _dataContext.Accounts
				.Where(x => x.TrancheId == tranche.Id)
				.Select(x => x.Id)
				.ToListAsync();

			var now = DateTime.Now;
			var questions = Enumerable.Range(1, count).Select(x => new Question {
				QuestionNum = maxQuestionNum + x,
				Type = QuestionType.Account,

				ProjectId = tranche.ProjectId,
				AccountId = accounts[rnd.Next(0, accounts.Count)],

				QuestionText = _RandString(rnd.Next(16, 128)),

				PostedById = postBy,
				LastEditorId = postBy,

				DatePosted = now,
				DateLastEdited = now,
			}).ToList();

			tranche.Project.Questions.AddRange(questions);
			var rows = await _dataContext.SaveChangesAsync();

			return Ok(rows);
		}
	}
}
