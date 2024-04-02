using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.DTO;

namespace DocumentsQA_Backend.Repository {
	public class EventLogRepository : IEventLogRepository {
		private readonly IHttpContextAccessor _httpContextAccessor;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		public EventLogRepository(
			IHttpContextAccessor httpContextAccessor, 
			DataContext dataContext, IAccessService access)
		{
			_httpContextAccessor = httpContextAccessor;

			_dataContext = dataContext;
			_access = access;
		}

		// -----------------------------------------------------

		private async Task _CleanInvalid() {
			// TODO: Implement system to clean invalid records
			//       Necessary because EventLogs_Login does not have FK relationships set up
			throw new NotImplementedException();
		}

		public async Task AddLoginEvent() {
			await AddLoginEvent(_access.GetProjectID());
		}
		public async Task AddLoginEvent(int projectId) {
			IPAddress? address = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

			var log = new LogInEvent {
				Timestamp = DateTime.Now,
				UserId = _access.GetUserID(),
				ProjectId = projectId,

				IPAddress = address ?? IPAddress.Any,
			};
			_dataContext.EventLogs_Login.Add(log);
			await _dataContext.SaveChangesAsync();
		}

		public async Task AddViewEvent(ViewType type, int id) {
			var log = new ViewEvent {
				Timestamp = DateTime.Now,
				UserId = _access.GetUserID(),
				Type = type,
				ViewId = id,
			};

			switch (type) {
				case ViewType.Question: {
					var question = await Queries.GetQuestionFromId(_dataContext, id);
					if (question == null)
						throw new ArgumentException("Invalid id");

					log.ProjectId = question.ProjectId;

					break;
				}
				case ViewType.Account: {
					var account = await Queries.GetAccountFromId(_dataContext, id);
					if (account == null)
						throw new ArgumentException("Invalid id");

					log.ProjectId = account.ProjectId;

					break;
				}
				case ViewType.Tranche: {
					var tranche = await Queries.GetTrancheFromId(_dataContext, id);
					if (tranche == null)
						throw new ArgumentException("Invalid id");

					log.ProjectId = tranche.ProjectId;

					break;
				}
			}

			_dataContext.EventLogs_View.Add(log);
			await _dataContext.SaveChangesAsync();
		}
	}

	/// <summary>
	/// No event logs tracking
	/// </summary>
	public class EventLogRepository_Null : IEventLogRepository {
		public EventLogRepository_Null() { }

		// -----------------------------------------------------

		public Task AddLoginEvent() {
			return Task.CompletedTask;
		}
		public Task AddLoginEvent(int projectId) {
			return Task.CompletedTask;
		}

		public Task AddViewEvent(ViewType type, int id) {
			return Task.CompletedTask;
		}
	}
}
