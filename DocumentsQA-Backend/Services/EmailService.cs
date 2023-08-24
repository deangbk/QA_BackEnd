using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DocumentsQA_Backend.Controllers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Services {
	public class MailData {
		string Subject { get; set; } = null!;
	}

	public interface IEmailService {
		void NotifyQuestion(Question question);
		void NotifyApproval(Question question);
		void NotifyEdit(Question question);
	}

	public class EmailService : IEmailService {
		private readonly DataContext _dataContext;
		private readonly ILogger<EmailService> _logger;

		private readonly UserManager<AppUser> _userManager;

		public EmailService(DataContext dataContext, ILogger<EmailService> logger, UserManager<AppUser> userManager) {
			_dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
		}

		// -----------------------------------------------------

		private bool _SendMail() {
			return false;
		}

		public void NotifyQuestion(Question question) {
			
		}

		public void NotifyApproval(Question question) {

		}

		public void NotifyEdit(Question question) {

		}
	}
}
