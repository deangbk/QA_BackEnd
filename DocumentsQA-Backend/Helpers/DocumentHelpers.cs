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
