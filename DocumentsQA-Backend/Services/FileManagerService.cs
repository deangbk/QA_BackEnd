using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Controllers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Services {
	public interface IFileManagerService {
		public Task CreateFile(string path, Stream dataStream);
		public Task ReadFile(string path, Stream outStream);
	}

	public class InProjectRootFileManager : IFileManagerService {
		private readonly ILogger<InProjectRootFileManager> _logger;

		private readonly IWebHostEnvironment _env;

		public string RootPath { get; private set; }

		public InProjectRootFileManager(
			ILogger<InProjectRootFileManager> logger, IWebHostEnvironment env)
		{
			_logger = logger;
			_env = env;

			RootPath = _env.ContentRootPath + "/";
		}

		// -----------------------------------------------------
		public async Task CreateFile(string path, Stream dataStream) {
			using var fs = new FileStream(RootPath + path, FileMode.Create);

			await dataStream.CopyToAsync(fs);
		}

		public async Task ReadFile(string path, Stream outStream) {
			using var fs = new FileStream(RootPath + path, FileMode.Open, FileAccess.Read);

			await fs.CopyToAsync(outStream);
		}
	}

	public class FromHttpFileManager : IFileManagerService {
		private readonly ILogger<InProjectRootFileManager> _logger;

		public FromHttpFileManager(
			ILogger<InProjectRootFileManager> logger)
		{
			_logger = logger;
		}

		// -----------------------------------------------------
		public async Task CreateFile(string path, Stream dataStream) {
			throw new NotImplementedException();
		}

		public async Task ReadFile(string path, Stream outStream) {
			using HttpClient client = new();
			outStream = await client.GetStreamAsync(path);
		}
	}
}
