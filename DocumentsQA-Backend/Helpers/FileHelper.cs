using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Helpers {
	public static class FileHelpers {
		public static async Task<string> ReadIFormFile(IFormFile file) {
			using var stream = new MemoryStream();
			{
				await file.CopyToAsync(stream);
				stream.Position = 0;
			}

			string contents;
			using (var reader = new StreamReader(stream)) {
				contents = reader.ReadToEnd();
			}

			return contents;
		}

		public static string GetResourceDirectory(int projectId, string resourceType) {
			return $"{resourceType}/{projectId}/";
		}

		public static async Task<byte[]> GetFileBytes(IFileManagerService fileManager, string resourcePath) {
			using var ms = new MemoryStream();
			await fileManager.ReadFile(resourcePath, ms);

			return ms.ToArray();
		}
	}
}
