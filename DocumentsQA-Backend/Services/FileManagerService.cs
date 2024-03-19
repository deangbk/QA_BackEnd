using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;


using DocumentsQA_Backend.Controllers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Services {
	public interface IFileManagerService {
		public Task CreateFile(string path, Stream dataStream);
		public Task ReadFile(string path, Stream outStream);
		public Task DeleteFile(string path);
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

			RootPath = _env.ContentRootPath;
		}

		// -----------------------------------------------------
		public async Task CreateFile(string path, Stream dataStream) {
			string? dir = Path.GetDirectoryName(path);
			if (dir != null && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			string finalPath = Path.Combine(RootPath, path);
			using var fs = File.OpenWrite(finalPath);

			await dataStream.CopyToAsync(fs);
		}

		public async Task ReadFile(string path, Stream outStream) {
			string finalPath = Path.Combine(RootPath, path);
			if (File.Exists(finalPath)) {
				using var fs = File.OpenRead(finalPath);

				await fs.CopyToAsync(outStream);
			}
		}

		public Task DeleteFile(string path) {
			string finalPath = Path.Combine(RootPath, path);
			File.Delete(finalPath);

			return Task.CompletedTask;
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
		public Task CreateFile(string path, Stream dataStream) {
			throw new NotImplementedException();
		}

		public async Task ReadFile(string path, Stream outStream) {
			using HttpClient client = new();
			outStream = await client.GetStreamAsync(path);
		}

		public Task DeleteFile(string path) {
			throw new NotImplementedException();
		}
	}
}
