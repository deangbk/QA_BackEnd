using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public static class Queries {
		public static async Task<Project?> GetProjectFromId(DataContext dataContext, int id) {
			var project = await dataContext.Projects.FindAsync(id);
			return project;
		}

		/// <summary>
		/// Gets query of all approved questions in the project
		/// </summary>
		public static IQueryable<Question> GetApprovedQuestionsQuery(DataContext dataContext, int pid) {
			return dataContext.Questions
				.Where(x => x.ProjectId == pid)
				.Where(x => x.QuestionApprovedById != null);
		}
	}
}
