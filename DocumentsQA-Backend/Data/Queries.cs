using System.Linq;

using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public static class Queries {
		public static async Task<T?> GetEntityFromId<T>(DbSet<T> source, object id)
			where T : class
		{
			return await source.FindAsync(id);
		}
		/*
		// TODO: Maybe figure this out later
		public static async Task<Dictionary<K, T>> GetEntitiesMapFromIds<T, K>(
			DbSet<T> source, Func<T, K> keySelector, IEnumerable<K> ids
		)
			where T : class
			where K : notnull
		{
			return await source
				.Where(x => ids.Contains(keySelector(x)))	// keySelector(x) cannot be translated into a query
				.ToDictionaryAsync(x => keySelector(x), x => x);
		}
		*/

		public static Task<Project?> GetProjectFromId(DataContext dataContext, int id)
			=> GetEntityFromId(dataContext.Projects, id);
		public static Task<Tranche?> GetTrancheFromId(DataContext dataContext, int id)
			=> GetEntityFromId(dataContext.Tranches, id);
		public static Task<Account?> GetAccountFromId(DataContext dataContext, int id)
			=> GetEntityFromId(dataContext.Accounts, id);
		public static Task<AppUser?> GetUserFromId(DataContext dataContext, int id)
			=> GetEntityFromId(dataContext.Users, id);
		public static Task<Question?> GetQuestionFromId(DataContext dataContext, int id)
			=> GetEntityFromId(dataContext.Questions, id);
		public static Task<Comment?> GetCommentFromId(DataContext dataContext, int id)
			=> GetEntityFromId(dataContext.Comments, id);
		public static Task<Document?> GetDocumentFromId(DataContext dataContext, int id)
			=> GetEntityFromId(dataContext.Documents, id);

		public static async Task<Account?> GetAccountFromIdName(DataContext dataContext, string id) {
			var res = await dataContext.Accounts
				.FirstAsync(x => id == dataContext.GetAccountIdentifierName(x.Id));
			return res;
		}

		public static async Task<Dictionary<int, Tranche>> GetTrancheMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Tranches
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}
		public static async Task<Dictionary<int, AppUser>> GetUsersMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Users
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}
		public static async Task<Dictionary<int, Account>> GetAccountsMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Accounts
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}
		public static async Task<Dictionary<int, Account>> GetAccountsMapFromIdNames(DataContext dataContext, IEnumerable<string> ids) {
			return await dataContext.Accounts
				.Where(x => ids.Contains(dataContext.GetAccountIdentifierName(x.Id)))
				.ToDictionaryAsync(x => x.Id, x => x);
		}
		public static async Task<Dictionary<int, Question>> GetQuestionsMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Questions
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}
		public static async Task<Dictionary<int, Document>> GetDocumentsMapFromIds(DataContext dataContext, IEnumerable<int> ids) {
			return await dataContext.Documents
				.Where(x => ids.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, x => x);
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets query of all questions in the project
		/// </summary>
		public static IQueryable<Question> GetProjectQuestions(DataContext dataContext, int pid) {
			return dataContext.Questions
				.Where(x => x.ProjectId == pid);
		}
		/// <summary>
		/// Gets query of all approved questions in the project
		/// </summary>
		public static IQueryable<Question> GetApprovedQuestionsQuery(DataContext dataContext, int pid) {
			return GetProjectQuestions(dataContext, pid)
				.Where(x => x.QuestionApprovedById != null);
		}
		/// <summary>
		/// Gets query of all unapproved questions in the project
		/// </summary>
		public static IQueryable<Question> GetUnapprovedQuestionsQuery(DataContext dataContext, int pid) {
			return GetProjectQuestions(dataContext, pid)
				.Where(x => x.QuestionApprovedById == null);
		}
    }
}
