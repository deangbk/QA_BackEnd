using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Services {
	internal interface IMailMessageComposer {
		public string GetSubject();
		public string GetMessage();
	}
	internal class MailMessageComposerDaily : IMailMessageComposer {
		public IEnumerable<Question> Questions { get; private set; }
		public IEnumerable<Document> Documents { get; private set; }

		public MailMessageComposerDaily(IEnumerable<Question> questions, IEnumerable<Document> documents) {
			Questions = questions;
			Documents = documents;
		}

		public string GetSubject() => "Situation Log Updated";
		public string GetMessage() {
			string message =
				  "<p>Investor Dearie,</p><br>"
				+ "<p>Situation log updated, let us take ibuprofen and explore xeno biology together.</p><br>";

			if (Questions.Any()) {
				// Split into chunks of 6 ticket IDs per line
				List<string> lines = Questions
					.Select(x => x.Id)
					.OrderBy(x => x)
					.Chunk(6)
					.Select(x => string.Join(" ", x.Select(x => string.Format("{0,5}", x))))
					.ToList();
				message +=
					  "<p>Thouse questiones thou'rt followeth, updaethe'd they've. Lay thine eyes upon them, prithee thee.</p>"
					+ "<br><p>" + string.Join("</br>", lines) + "<p><br>";
			}
			if (Documents.Any()) {
				List<string> lines = Documents
					.OrderBy(x => x.DateUploaded)
					.Select(x => x.FileName)
					.ToList();
				message +=
					  "<p>These memetic kill agents have also been uploaded, check them out!!</p>"
					+ "<br><p>" + string.Join("</br>", lines) + "<p><br>";
			}

			message +=
				  "<p>Long live the Dragon Emperor.</p>"
				+ "<p>Avocado.</p>";

			string messageHtml = "<html>";
			messageHtml += "<head><style>p { margin: 4px; }</style></head>";
			messageHtml += "<body>" + message + "</body></html>";

			return messageHtml;
		}
	}
	internal class MailMessageComposerRetroactive : IMailMessageComposer {
		public IEnumerable<Question> Questions { get; private set; }
		public IEnumerable<Document> Documents { get; private set; }

		public MailMessageComposerRetroactive(IEnumerable<Question> questions, IEnumerable<Document> documents) {
			Questions = questions;
			Documents = documents;
		}

		public string GetSubject() => "Welcome";
		public string GetMessage() {
			string message =
				  "<p>Investor Dearie,</p><br>"
				+ "<p>You were late to the party. While you were existn't, these things happened:</p><br>";

			if (Questions.Any()) {
				// Split into chunks of 6 ticket IDs per line
				List<string> lines = Questions
					.Select(x => x.Id)
					.OrderBy(x => x)
					.Chunk(6)
					.Select(x => string.Join(" ", x.Select(x => string.Format("{0,5}", x))))
					.ToList();
				message +=
					  "<p>These stuff happened:</p>"
					+ "<br><p>" + string.Join("</br>", lines) + "<p><br>";
			}
			if (Documents.Any()) {
				List<string> lines = Documents
					.OrderBy(x => x.DateUploaded)
					.Select(x => x.FileName)
					.ToList();
				message +=
					  "<p>These Very Classified Documents were uploaded:</p>"
					+ "<br><p>" + string.Join("</br>", lines) + "<p><br>";
			}

			message +=
				  "<p>Long live the Dragon Emperor.</p>"
				+ "<p>Mayonnaise.</p>";

			string messageHtml = "<html>";
			messageHtml += "<head><style>p { margin: 4px; }</style></head>";
			messageHtml += "<body>" + message + "</body></html>";

			return messageHtml;
		}
	}
	internal class MailMessageComposerTest : IMailMessageComposer {
		public MailMessageComposerTest() {
		}

		public string GetSubject() => "Test Test Test";
		public string GetMessage() {
			string message =
				  "<p>Investor Dearie,</p><br>"
				+ "<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent euismod, sem quis ultrices auctor, "
				+ "turpis dolor gravida nulla, at rutrum dui ante ac eros. Curabitur ante turpis, luctus sit amet magna vel, "
				+ "finibus aliquet magna. Quisque sed sollicitudin orci, ut eleifend metus. Donec vel posuere est, et convallis magna.</p><br>";

			{
				List<string> lines = Enumerable.Range(0, 20)
					.Chunk(6)
					.Select(x => string.Join(" ", x.Select(x => string.Format("{0,5}", x))))
					.ToList();
				message +=
					  "<p>These stuff happened:</p>"
					+ "<br><p>" + string.Join("<br>", lines) + "<p><br>";
			}
			{
				List<string> lines = new() {
					"sjkfgkdgjfk",
					"43t3reve",
					"aaaaaaaaaaaaaaaaaaaa"
				};
				message +=
					  "<p>These documents:</p>"
					+ "<br><p>" + string.Join("<br>", lines) + "<p><br>";
			}

			message += "<p>Respectfully, ggggggggggggggggggggggggggggggggggggggggggggg.</p>";

			string messageHtml = "<html>";
			messageHtml += "<head><style>p { margin: 4px; }</style></head>";
			messageHtml += "<body>" + message + "</body></html>";

			return messageHtml;
		}
	}

	// --------------------------------------------------------------------------

	public class MailData {
		public string Subject { get; set; } = null!;
		public List<string> Recipients { get; set; } = null!;
		public string Message { get; set; } = null!;
	}

	public interface IEmailService {
		public Task<bool> SendDailyEmails();
		public Task<bool> SendNewUserEmail(AppUser user);
		public Task<bool> SendTestEmail(string email);
	}
	public class EmailService : IScopedProcessingService, IEmailService {
		private readonly DataContext _dataContext;
		private readonly ILogger<EmailService> _logger;

		private readonly UserManager<AppUser> _userManager;

		private readonly int _dailyEmailSchedule = 6;		// hour value -> 6:00

		public EmailService(DataContext dataContext, ILogger<EmailService> logger, UserManager<AppUser> userManager) {
			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
		}

		// -----------------------------------------------------

		private async Task<bool> _SendMail(MailData data) {
			/*
			var (host, from, pass, port, ssl) = (
				"smtp.gmail.com",
				new MailAddress("", "Awoo"),
				"", 587, true);
			*/
			var (host, from, pass, port, ssl) = (
				"mail.thainpl.com",
				new MailAddress("eximadmin@thainpl.com", "Dragon"),
				"annie111", 25, false);

			using SmtpClient smtp = new() {
				Host = host,
				Port = port,
				Credentials = new NetworkCredential(from.Address, pass),
				DeliveryMethod = SmtpDeliveryMethod.Network,
				EnableSsl = ssl,
			};

			MailMessage message = new() {
				From = from,
				Subject = data.Subject,
				Body = data.Message,
				IsBodyHtml = true,
			};
			foreach (var i in data.Recipients)
				message.To.Add(i);

			try {
				await smtp.SendMailAsync(message);
				return true;
			}
			catch (Exception e) {
				_logger.LogError(e, "Failed to send email");
				return false;
			}
		}

		// -----------------------------------------------------

		public async Task Work(CancellationToken stoppingToken) {
			while (!stoppingToken.IsCancellationRequested) {
				await SendDailyEmails();

				DateTime now = DateTime.Now;
				DateTime tomorrow = now.AddDays(1);
				DateTime nextTaskTime = new DateTime(
					tomorrow.Year, tomorrow.Month, tomorrow.Day, 
					_dailyEmailSchedule, 0, 0);

				await Task.Delay(nextTaskTime - now, stoppingToken);
			}
		}

		// -----------------------------------------------------

		internal class _SendDailyEmails_UData {
			public string Email { get; set; }
			public List<Question> Questions { get; set; }
			public List<Document> Documents { get; set; }

			public _SendDailyEmails_UData(List<Question> q, List<Document> d, string email) {
				Questions = q;
				Documents = d;
				Email = email;
			}
		};
		public async Task<bool> SendDailyEmails() {
			_logger.LogInformation("EmailService: Sending daily emails");

			// TODO: Untested, probably doesn't work properly

			bool res = true;
			DateTime now = DateTime.Now;

			// The filter there is kind of useless innit
			var projects = await _dataContext.Projects
				.Where(x => x.LastEmailSentDate < now)
				.ToListAsync();

			foreach (var project in projects)
			{
				// Base query: check if they were updated/added after the previous daily email operation
				var queryQuestions = _dataContext.Questions
					.Where(x => x.ProjectId == project.Id && x.DateLastEdited > project.LastEmailSentDate);
				var queryDocuments = _dataContext.Documents
					.Where(x => x.ProjectId == project.Id && x.DateUploaded > project.LastEmailSentDate);

				Dictionary<int, _SendDailyEmails_UData> mapUserData = new();

				// Handle general questions
				{
					// Get general questions
					var questions = await queryQuestions
						.Where(x => x.Type == QuestionType.General)
						.ToListAsync();

					// Get general documents
					var documents = await queryDocuments
						.Where(x => x.Type == DocumentType.General)
						.ToListAsync();
					{
						// Get documents attached to general questions
						var questionIds = questions.Select(x => x.Id);
						var add = await queryDocuments
							.Where(x => x.Type == DocumentType.Question)
							.Where(x => questionIds.Any(y => y == (x.AssocQuestionId ?? -1)))   // Coalesce nullable
							.ToListAsync();
						documents.AddRange(add);
					}

					// Add to all users with this project access
					var followers = ProjectHelpers.GetProjectUserAccesses(project);
					var followersEx = await _dataContext.Users
						.Where(x => followers.Any(y => y == x.Id))
						.Select(x => new { x.Id, x.Email })
						.ToListAsync();
					foreach (var x in followersEx)
					{
						mapUserData[x.Id] = new(questions, documents, x.Email);
					}
				}

				// Handle account questions
				{
					var questionsByAccount = await queryQuestions
						.Where(x => x.Type == QuestionType.Account && x.AccountId != null)
						.GroupBy(x => x.Account!)
						.ToListAsync();
					foreach (var group in questionsByAccount)
					{
						var account = group.Key;
						var tranche = account.Tranche;

						// Get questions in this tranche
						var questions = group.ToList();

						// Get documents attached to accounts in this tranche
						var documents = await queryDocuments
							.Where(x => x.Type == DocumentType.Account)
							.Where(x => x.AssocAccountId == account.Id)
							.ToListAsync();
						{
							var questionIds = questions.Select(x => x.Id);

							// Get documents attached to questions in this tranche
							var add = await queryDocuments
								.Where(x => x.Type == DocumentType.Question)
								.Where(x => questionIds.Any(y => y == (x.AssocQuestionId ?? -1)))   // Coalesce nullable
								.ToListAsync();
							documents.AddRange(add);
						}

						// Add to all users with access to this tranche
						var followers = ProjectHelpers.GetTrancheUserAccesses(tranche);
						foreach (var id in followers)
						{
							var userData = mapUserData[id];
							userData.Questions.AddRange(questions);
							userData.Documents.AddRange(documents);
						}
					}
				}

				{
					// Send emails concurrently

					List<Task<bool>> sendRes = new();

					foreach (var (id, data) in mapUserData)
					{
						// Some entries may be repeated, get only unique ids
						// Less code than using HashMap
						var questions = data.Questions.DistinctBy(x => x.Id);
						var documents = data.Documents.DistinctBy(x => x.Id);

						sendRes.Add(_SendEmailsSub(new MailMessageComposerRetroactive(questions, documents),
							new List<string>() { data.Email }));
					}

					var listRes = await Task.WhenAll(sendRes);
					if (!listRes.All(x => x))
					{
						_logger.LogInformation("EmailService: Failed to send some emails");
						res = false;
					}
				}
			}

			_logger.LogInformation("EmailService: Updating projects LastEmailSentDate");
			foreach (var project in projects)
			{
				project.LastEmailSentDate = now;
			}
			await _dataContext.SaveChangesAsync();

			return true; // res;
		}

		public async Task<bool> SendNewUserEmail(AppUser user) {
			_logger.LogInformation("EmailService: Sending retroactive updates to new user");

			// TODO: Untested, probably doesn't work properly

			DateTime now = DateTime.Now;

			// TODO: Maybe change CanUserAccessProject into a DB function
			var projects = (await _dataContext.Projects
				.ToArrayAsync())
				.Where(x => ProjectHelpers.CanUserAccessProject(x, user.Id))
				.ToList();

			// Group by projects
			foreach (var project in projects) {
				var tranches = ProjectHelpers.GetUserTrancheAccessesInProject(user, project.Id)
					.Select(x => x.Id)
					.ToList();

				// Get stuff posted/uploaded in the past
				var queryQuestions = _dataContext.Questions
					.Where(x => x.ProjectId == project.Id && x.DateLastEdited < user.DateCreated);
				var queryDocuments = _dataContext.Documents
					.Where(x => x.ProjectId == project.Id && x.DateUploaded < user.DateCreated);

				// Get all general questions
				var questions = await queryQuestions
					.Where(x => x.Type == QuestionType.General)
					.ToListAsync();
				{
					// Get all account questions the user has access to
					var add = await queryQuestions
						.Where(x => x.Type == QuestionType.Account && x.AccountId != null)
						.Where(x => tranches.Any(y => y == x.Account.TrancheId))
						.ToListAsync();
					questions.AddRange(add);
				}

				// Get all general documents
				var documents = await queryDocuments
					.Where(x => x.Type == DocumentType.General)
					.ToListAsync();
				{
					var questionIds = questions.Select(x => x.Id);

					// Get all documents attached to questions
					var add = await queryDocuments
						.Where(x => x.Type == DocumentType.Question)
						.Where(x => questionIds.Any(y => y == x.AssocQuestionId))
						.ToListAsync();
					documents.AddRange(add);
				}
				{
					var accountIds = await _dataContext.Accounts
						.Where(x => tranches.Any(y => y == x.TrancheId))
						.Select(x => x.Id)
						.ToListAsync();

					// Get all documents attached to accounts
					var add = await queryDocuments
						.Where(x => x.Type == DocumentType.Account)
						.Where(x => accountIds.Any(y => y == x.AssocAccountId))
						.ToListAsync();
					documents.AddRange(add);
				}

				var res = await _SendEmailsSub(new MailMessageComposerRetroactive(questions, documents), 
					new List<string>() { user.Email });
				if (!res)
					return res;
			}

			return true;
		}
		public Task<bool> SendTestEmail(string email) {
			return _SendEmailsSub(new MailMessageComposerTest(), new List<string>() { email });
		}

		// Maybe function overloading was a mistake
		private async Task<bool> _SendEmailsSub(IMailMessageComposer composer, List<int> userIds) {
			var emails = await _dataContext.Users
				.Join(userIds,
					u => u.Id,
					x => x,
					(u, x) => u.Email)
				.ToListAsync();
			return await _SendEmailsSub(composer, emails);
		}
		private Task<bool> _SendEmailsSub(IMailMessageComposer composer, List<AppUser> users) {
			var emails = users.Select(x => x.Email).ToList();
			return _SendEmailsSub(composer, emails);
		}
		private async Task<bool> _SendEmailsSub(IMailMessageComposer composer, List<string> sendEmails) {
			MailData mailData = new() {
				Subject = composer.GetSubject(),
				Recipients = sendEmails,
				Message = composer.GetMessage(),
			};
			return await _SendMail(mailData);
		}
	}
}
