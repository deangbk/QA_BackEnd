using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;
using DocumentsQA_Backend.Repository;

namespace DocumentsQA_Backend.Controllers {
	[Route("api/log")]
	[ApiController]
	[Authorize]
	public class TelemetryController : Controller {
		private readonly ILogger<TelemetryController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IEventLogRepository _repoEventLog;

		public TelemetryController(
			ILogger<TelemetryController> logger,

			DataContext dataContext,
			IAccessService access,

			IEventLogRepository repoEventLog)
		{
			_logger = logger;

			_dataContext = dataContext;
			_access = access;

			_repoEventLog = repoEventLog;
		}

		// -----------------------------------------------------

		/// <summary>
		/// TODO: Find some method of safeguarding the POST APIs?
		/// </summary>
		private void _VerifySecret(string secret, string expect) {
			if (secret != expect)
				throw new AccessForbiddenException("Wrong access key");
		}

		// -----------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		[HttpGet("")]
		public async Task<IActionResult> Get() {
			return StatusCode((int)HttpStatusCode.NotImplemented);
		}

		/// <summary>
		/// 
		/// </summary>
		[HttpPost("add/question/{id}")]
		public async Task<IActionResult> AddQuestionView(int id) {
			await _repoEventLog.AddViewEvent(ViewType.Question, id);

			return Ok();
		}

		/// <summary>
		/// 
		/// </summary>
		[HttpPost("add/account/{id}")]
		public async Task<IActionResult> AddAccountView(int id) {
			await _repoEventLog.AddViewEvent(ViewType.Account, id);

			return Ok();
		}

		/// <summary>
		/// 
		/// </summary>
		[HttpPost("add/tranche/{id}")]
		public async Task<IActionResult> AddTrancheView(int id) {
			await _repoEventLog.AddViewEvent(ViewType.Tranche, id);

			return Ok();
		}

		/// <summary>
		/// 
		/// </summary>
		[HttpPost("add/document/{id}")]
		public async Task<IActionResult> AddDocumentView(int id) {
			await _repoEventLog.AddViewEvent(ViewType.Document, id);

			return Ok();
		}
	}
}
