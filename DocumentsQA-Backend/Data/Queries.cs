﻿using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public static class Queries {
		public static async Task<Project?> GetProjectFromId(DataContext dataContext, int id) {
			var res = await dataContext.Projects.FindAsync(id);
			return res;
		}
		public static async Task<Question?> GetQuestionFromId(DataContext dataContext, int id) {
			var res = await dataContext.Questions.FindAsync(id);
			return res;
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
