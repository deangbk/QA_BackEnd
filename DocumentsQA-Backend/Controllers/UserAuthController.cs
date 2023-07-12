using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/auth")]
	[Authorize]
	[ApiController]
	public class UserAuthController : ControllerBase {
		private readonly DataContext _dataContext;
		private readonly ILogger<UserAuthController> _logger;

		public UserAuthController(DataContext dataContext, ILogger<UserAuthController> logger) {
			_dataContext = dataContext;
			_logger = logger;
		}

		// -----------------------------------------------------

		[HttpGet("test")]
		public DateTime GetTest() {
			return DateTime.Now;
		}
	}
}