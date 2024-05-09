using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Repository;

namespace DocumentsQA_Backend.Helpers {
	public class DocumentHelpers {
		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IProjectRepository _repoProject;

		public DocumentHelpers(
			DataContext dataContext, 
			IAccessService access,
			IProjectRepository repoProject)
		{
			_dataContext = dataContext;
			_access = access;


			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		public static DocumentType? ParseDocumentType(string str) => str.ToLower() switch {
			"bid"		=> DocumentType.Bid,
			"post" or "question" 
						=> DocumentType.Question,
			"account"	=> DocumentType.Account,
			"transaction" or "trans" 
						=> DocumentType.Transaction,
			_ => null,
		};

		public static Document CreateFromDTO(int projectId, DocumentUploadDTO upload, string fileName) {
			string fileExt = Path.GetExtension(upload.Url)[1..];		// substr to remove the dot

			var document = new Document {
				FileUrl = upload.Url,
				FileName = fileName,
				FileType = fileExt,
				Description = upload.Description,

				Hidden = upload.Hidden ?? false,
				AllowPrint = upload.Printable ?? false,

				//AssocQuestionId = upload.AssocQuestion,
				//AssocAccountId = upload.AssocAccount,
				ProjectId = projectId,

				DateUploaded = DateTime.Now,
			};

			return document;
		}

		public async Task<bool> CheckDuplicate(Document document) {
			bool exists = await _dataContext.Documents
				.Where(x => x.ProjectId == document.ProjectId && x.FileUrl == document.FileUrl)
				.AnyAsync();
			return exists;
		}

		public async Task<List<Document>> GetDocuments(DocumentGetDTO getDto) {
			var project = await _repoProject.GetProjectAsync();

			var baseQuery = _dataContext.Documents
				.Where(x => x.ProjectId == project.Id);

			var filter = getDto.Filter;
			var page = getDto.Paginate;

			if (filter != null) {
				if (filter.SearchTerm != null) {
					baseQuery = baseQuery.Where(x => EF.Functions.Contains(x.FileName, filter.SearchTerm));
				}
				if (filter.UploaderID != null) {
					baseQuery = baseQuery.Where(x => x.UploadedById == filter.UploaderID);
				}
				if (filter.PostedFrom is not null) {
					baseQuery = baseQuery.Where(x => x.DateUploaded >= filter.PostedFrom);
				}
				if (filter.PostedTo is not null) {
					baseQuery = baseQuery.Where(x => x.DateUploaded < filter.PostedTo);
				}
				if (filter.AllowPrint != null) {
					baseQuery = baseQuery.Where(x => x.AllowPrint == filter.AllowPrint);
				}

				if (filter.Category != null) {
					var typeMatch = DocumentHelpers.ParseDocumentType(filter.Category)
						?? DocumentType.Bid;
					baseQuery = baseQuery.Where(x => x.Type == typeMatch);
				}
				if (filter.AssocQuestion != null) {
					baseQuery = baseQuery.Where(x => x.Type == DocumentType.Question
						&& x.AssocQuestionId == filter.AssocQuestion);
				}
				if (filter.AssocAccount != null) {
					baseQuery = baseQuery.Where(x => x.Type == DocumentType.Account
						&& x.AssocAccountId == filter.AssocAccount);
				}
				if (filter.AssocTranche != null) {
					// TODO: Test this
					try {
						int trancheId = await _dataContext.Tranches
							.Where(x => x.Name == filter.AssocTranche)
							.Select(x => x.Id)
							.FirstAsync();
						baseQuery = baseQuery.Where(x =>
							(x.Type == DocumentType.Account && x.AssocAccount!.TrancheId == trancheId) ||
							(x.Type == DocumentType.Question && x.AssocQuestion!.Account!.TrancheId == trancheId));
					}
					catch (InvalidOperationException) { }
				}
			}

			baseQuery = baseQuery.OrderByDescending(x => x.DateUploaded);
			var listDocuments = await baseQuery.ToListAsync();

			// Allow staff to everything, but filter based on access for regular users
			if (!_access.IsSuperUser()) {
				var allowedTranches = project.Tranches
					.Where(x => _access.AllowToTranche(x))
					.ToHashSet();
				var allowedAccounts = allowedTranches
					.SelectMany(x => x.Accounts)
					.Select(x => x.Id)
					.ToHashSet();

				listDocuments = listDocuments
					.Where(x => x.Type switch {
						DocumentType.Account => allowedAccounts.Contains(x.AssocAccountId!.Value),
						DocumentType.Question => x.AssocQuestion!.AccountId == null
							|| allowedAccounts.Contains((int)x.AssocQuestion!.AccountId),
						_ => true,
					})
					.ToList();
			}

			// Paginate result; but return everything if paginate DTO doesn't exist
			if (page != null) {
				int countPerPage = page.CountPerPage;
				int maxPages = (int)Math.Ceiling(listDocuments.Count / (double)countPerPage);

				listDocuments = listDocuments
					.Skip(page.Page!.Value * countPerPage)
					.Take(countPerPage)
					.ToList();
			}

			return listDocuments;
		}

		public static string GetDocumentProjectDirectory(int projectId) {
			return FileHelpers.GetResourceDirectory(projectId, "Documents");
		}
		public static string GetDocumentFileRoute(Document document) {
			return GetDocumentProjectDirectory(document.ProjectId) 
				+ $"{document.FileUrl}";
		}
	}
}
