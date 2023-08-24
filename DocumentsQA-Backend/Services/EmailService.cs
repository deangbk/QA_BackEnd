using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Controllers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Services {
	public class MailData {
		public string Subject { get; set; } = null!;
		public string Recipient { get; set; } = null!;
		public string Message { get; set; } = null!;
	}

	public class EmailService : IScopedProcessingService {
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

		private bool _SendMail(MailMessage mail) {
			using SmtpClient smtp = new SmtpClient();

			smtp.Host = "mail.thainpl.com";
			smtp.Port = 25;
			smtp.Credentials = new NetworkCredential("eximadmin@thainpl.com", "annie111");
			smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
			smtp.EnableSsl = true;

			try {
				smtp.Send(mail);
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
				_logger.LogInformation("EmailService: Sending daily emails");

				await _SendDailyEmails();

				DateTime now = DateTime.Now;
				DateTime tomorrow = now.AddDays(1);
				DateTime nextTaskTime = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 
					_dailyEmailSchedule, 0, 0);

				await Task.Delay(nextTaskTime - now, stoppingToken);
			}
		}

		private async Task _SendDailyEmails() {
			// Handle general questions
			{
				// Group by projects
				var qByProject = await _dataContext.Questions
					.Where(x => !x.DailyEmailSent && x.Type == QuestionType.General)
					.GroupBy(x => x.ProjectId)
					.ToListAsync();

				foreach (var group in qByProject) {
					var questions = group.ToList();
					if (questions.Count > 0) {
						Project project = questions.First().Project;

						var followers = ProjectHelpers.GetProjectUserAccesses(project);

						await _SendEmailsSub(questions, followers);
					}
				}
			}

			// Handle account questions
			{

			}
		}

		private async Task _SendEmailsSub(List<Question> updatedQuestions, List<int> userIds) {
			var users = await _dataContext.Users
				.Join(userIds,
					u => u.Id,
					x => x,
					(u, x) => new {
						u.Id,
						u.Email,
					})
				.ToListAsync();
			foreach (var i in users) {

			}
		}
	}
}
