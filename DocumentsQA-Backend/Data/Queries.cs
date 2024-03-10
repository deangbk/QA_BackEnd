using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public static class Queries {
		public static async Task<Project?> GetProjectFromId(DataContext dataContext, int id) {
			var res = await dataContext.Projects.FindAsync(id);
			return res;
		}
		public static async Task<Tranche?> GetTrancheFromId(DataContext dataContext, int id) {
			var res = await dataContext.Tranches.FindAsync(id);
			return res;
		}
		public static async Task<Account?> GetAccountFromId(DataContext dataContext, int id) {
			var res = await dataContext.Accounts.FindAsync(id);
			return res;
		}
		public static async Task<AppUser?> GetUserFromId(DataContext dataContext, int id) {
			var res = await dataContext.Users.FindAsync(id);
			return res;
		}
		public static async Task<Question?> GetQuestionFromId(DataContext dataContext, int id) {
			var res = await dataContext.Questions.FindAsync(id);
			return res;
		}
		public static async Task<Comment?> GetCommentFromId(DataContext dataContext, int id) {
			var res = await dataContext.Comments.FindAsync(id);
			return res;
		}
		public static async Task<Document?> GetDocumentFromId(DataContext dataContext, int id) {
			var res = await dataContext.Documents.FindAsync(id);
			return res;
		}

		public static async Task<Dictionary<int, Account>?> GetAccountsMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Accounts
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}
		public static async Task<Dictionary<int, Question>?> GetQuestionsMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Questions
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}
		public static async Task<Dictionary<int, Document>?> GetDocumentsMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Documents
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}

		/// <summary>
		/// Gets query of all approved questions in the project
		/// </summary>
		public static IQueryable<Question> GetApprovedQuestionsQuery(DataContext dataContext, int pid) {
			return dataContext.Questions
				.Where(x => x.ProjectId == pid)
				.Where(x => x.QuestionApprovedById != null);
		}
		/// <summary>
		/// Gets query of all unapproved questions in the project
		/// </summary>
		public static IQueryable<Question> GetUnapprovedQuestionsQuery(DataContext dataContext, int pid) {
			return dataContext.Questions
				.Where(x => x.ProjectId == pid)
				.Where(x => x.QuestionApprovedById == null);
		}

        public static IQueryable<Question> GetAllQuestionsQuery(DataContext dataContext, int pid)
        {
			return dataContext.Questions
				.Where(x => x.ProjectId == pid);
                
        }
		/// <summary>
		/// Get single question by id
		/// </summary>
		/// <param name="dataContext"></param>
		/// <param name="qid"></param>
		/// <returns></returns>
        public static IQueryable<Question> GetSingleQuestionsQuery(DataContext dataContext, int qid)
        {
            return  dataContext.Questions
                .Where(x => x.Id== qid);

        }
    }
}
