using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.DTO;

namespace DocumentsQA_Backend.Repository {
	public class ProjectRepository : IProjectRepository {
		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		public ProjectRepository(DataContext dataContext, IAccessService access) {
			_dataContext = dataContext;
			_access = access;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Project is guaranteed to be non-null
		/// See <see cref="ProjectAccessMiddleware"/>
		/// </summary>
		public async Task<Project> GetProjectAsync() {
			Project? project = await Queries.GetProjectFromId(_dataContext, _access.GetProjectID());
			return project!;
		}

		public async Task<Tranche?> GetTrancheAsync(int id) {
			Project project = await GetProjectAsync();
			return project.Tranches.FirstOrDefault(x => x.Id == id);
		}
	}
}
