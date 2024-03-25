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

namespace DocumentsQA_Backend.Helpers {
	public static class DocumentHelpers {
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

		public static async Task<bool> CheckDuplicate(DataContext dataContext, Document document) {
			bool exists = await dataContext.Documents
				.Where(x => x.ProjectId == document.ProjectId && x.FileUrl == document.FileUrl)
				.AnyAsync();
			return exists;
		}

		public static string GetDocumentProjectDirectory(int projectId) {
			return $"Documents/{projectId}/";
		}
		public static string GetDocumentFileRoute(Document document) {
			//return $"{contentRoot}/prj-{projectId}/{document.FileUrl}";
			return GetDocumentProjectDirectory(document.ProjectId) 
				+ $"{document.FileUrl}";
		}
	}
}
