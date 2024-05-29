using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/project")]
	[Authorize]
	public class ProjectController : Controller {
		private readonly ILogger<ProjectController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IFileManagerService _fileManager;

		private readonly DocumentHelpers _documentHelpers;
		private readonly IProjectRepository _repoProject;

		public ProjectController(
			ILogger<ProjectController> logger, 
			DataContext dataContext, IAccessService access,
			IFileManagerService fileManager, 
			DocumentHelpers documentHelpers,
			IProjectRepository repoProject) 
		{
			_logger = logger;

			_dataContext = dataContext;
			_access = access;

			_fileManager = fileManager;

			_documentHelpers = documentHelpers;
			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets project information
		/// </summary>
		[HttpGet("all")]
		public async Task<IActionResult> GetAllProjects() {
			if (!_access.IsAdmin())
				return Forbid();

			var projects = await _dataContext.Projects
				.OrderBy(x => x.Id)
				.ToListAsync();

			return Ok(projects.Select(x => x.ToJsonTable(2)));
		}

		/// <summary>
		/// Gets project information
		/// </summary>
		[HttpGet("")]
		public async Task<IActionResult> GetProjectInfo() {
			var project = await _repoProject.GetProjectAsync();

			return Ok(project.ToJsonTable(2));
		}

		/// <summary>
		/// Gets project tranches information
		/// </summary>
		[HttpGet("tranches")]
		public async Task<IActionResult> GetProjectTranches() {
			var project = await _repoProject.GetProjectAsync();

			List<Tranche> tranches = new();
			if (!_access.IsSuperUser()) {
				var user = await Queries.GetUserFromId(_dataContext, _access.GetUserID());
				if (user != null) {
					tranches = ProjectHelpers.GetUserTrancheAccessesInProject(user, project.Id);
				}
			}
			else {
				tranches = project.Tranches;
			}

			var resTable = tranches.Select(x => x.ToJsonTable(1));
			return Ok(resTable);
		}

		/// <summary>
		/// Gets list of all users with project read access
		/// <para>Admins are not included</para>
		/// </summary>
		[HttpGet("users")]
		public async Task<IActionResult> GetProjectUsers([FromQuery] int details = -1) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			// Exclude managers
			var listUsers = project.Users
				.Where(x => !project.UserManagers.Any(y => x.Id == y.Id));

			if (details >= 0) {
				// TODO: Maybe optimize these

				var trancheUsersMap = ProjectHelpers.GetTrancheUserAccessesMap(project);
				var trancheDataMap = project.Tranches
					.ToDictionary(x => x.Id, x => x.ToJsonTable(0));

				var mapUsers = listUsers.ToDictionary(x => x.Id, x => x);
				var listRes = mapUsers
					.Select(x => {
						var tableBase = x.Value.ToJsonTable(details);
						tableBase["tranches"] = trancheUsersMap
							.Where(y => y.Value.Contains(x.Key))
							.Select(y => trancheDataMap[y.Key.Id])
							.ToList();
						return tableBase;
					});

				return Ok(listRes);
			}
			else {
				return Ok(listUsers.Select(x => x.Id).ToList());
			}
		}

		/// <summary>
		/// Gets list of all users with project management access
		/// <para>Admins are not included</para>
		/// </summary>
		[HttpGet("managers")]
		public async Task<IActionResult> GetProjectManagers([FromQuery] int details = -1) {
			var project = await _repoProject.GetProjectAsync();

			var listManagerIds = project.UserManagers
				.Select(x => x.Id)
				.ToList();

			if (details >= 0) {
				var trancheUsersMap = ProjectHelpers.GetTrancheUserAccessesMap(project);
				var trancheDataMap = project.Tranches
					.ToDictionary(x => x.Id, x => x.ToJsonTable(0));

				var mapUsers = await Queries.GetUsersMapFromIds(_dataContext, listManagerIds);
				var listRes = mapUsers
					.Select(x => {
						var tableBase = x.Value.ToJsonTable(details);
						tableBase["tranches"] = trancheUsersMap
							.Where(y => y.Value.Contains(x.Key))
							.Select(y => trancheDataMap[y.Key.Id])
							.ToList();
						return tableBase;
					});

				return Ok(listRes);
			}
			else {
				return Ok(listManagerIds);
			}
		}

		[HttpGet("content")]
		public async Task<IActionResult> CountContent() {
			var project = await _repoProject.GetProjectAsync();

			int countGeneralPosts = 0;
			int countAccountPosts = 0;
			int countDocuments = 0;

			if (_access.IsSuperUser()) {
				var queryPosts = Queries.GetProjectQuestions(_dataContext, project.Id);

				countGeneralPosts = await queryPosts
					.Where(x => x.Type == QuestionType.General)
					.CountAsync();
				countAccountPosts = await queryPosts
					.Where(x => x.Type == QuestionType.Account)
					.CountAsync();
				countDocuments = await _dataContext.Documents
					.Where(x => x.ProjectId == project.Id)
					.CountAsync();
			}
			else {
				int userId = _access.GetUserID();
				var user = await Queries.GetUserFromId(_dataContext, userId);

				var tranchesAccess = ProjectHelpers.GetUserTrancheAccessesInProject(user!, project.Id)
					.Select(x => x.Id)
					.ToList();

				var listPosts = await Queries.GetApprovedQuestionsQuery(_dataContext, project.Id)
					.ToListAsync();
				var listAllowPosts = PostHelpers.FilterUserReadPost(_access, listPosts);

				countGeneralPosts = listAllowPosts
					.Where(x => x.Type == QuestionType.General)
					.Count();
				countAccountPosts = listAllowPosts
					.Where(x => x.Type == QuestionType.Account)
					.Count();
				countDocuments = (await _documentHelpers.GetDocuments(new())).Count;
			}

			return Ok(new JsonTable {
				["gen_posts"] = countGeneralPosts,
				["acc_posts"] = countAccountPosts,
				["documents"] = countDocuments,
			});
		}

		// -----------------------------------------------------

		[HttpPost("create")]
		public async Task<IActionResult> CreateProject([FromBody] CreateProjectDTO dto) {
			if (_access.IsAdmin())
				return Forbid();

			var project = new Project {
				Name = dto.Name,
				DisplayName = dto.DisplayName,
				CompanyName = dto.Company,
				ProjectStartDate = dto.DateStart!.Value,
				ProjectEndDate = dto.DateEnd!.Value,
				LastEmailSentDate = DateTime.MinValue,
			};

			List<string> tranches;
			try {
				tranches = dto.InitialTranches
					.Split(",")
					.Select(x => x.Trim().Truncate(16))
					.Where(x => x.Length > 0)
					.ToList();
			}
			catch (Exception) {
				return BadRequest("Tranches: incorrect input format");
			}

			// Wrap all operations in a transaction so failure would revert the entire thing
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				_dataContext.Projects.Add(project);
				await _dataContext.SaveChangesAsync();

				project.Tranches = tranches.Select(x => new Tranche {
					ProjectId = project.Id,
					Name = x,
				}).ToList();
				await _dataContext.SaveChangesAsync();

				await transaction.CommitAsync();
			}

			return Ok(project.Id);
		}

		[HttpPut("edit")]
		public async Task<IActionResult> EditProject([FromBody] EditProjectDTO dto) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.IsAdmin())
				return Forbid();

			int projectId = project.Id;

			if (dto.Name != null) {
				project.Name = dto.Name;
			}
			if (dto.DisplayName != null) {
				project.DisplayName = dto.DisplayName;
			}
			if (dto.Description != null) {
				var sanitizer = new Ganss.Xss.HtmlSanitizer();

				project.Description = sanitizer.Sanitize(dto.Description);
			}
			if (dto.Company != null) {
				project.CompanyName = dto.Company;
			}
			if (dto.DateStart != null) {
				var date = dto.DateStart.Value;
				project.ProjectStartDate = dto.DateStart.Value;
			}
			if (dto.DateEnd != null) {
				var date = dto.DateEnd.Value;
				if (date <= DateTime.Now.AddHours(0.5))
					return BadRequest("End date cannot be in the past");

				project.ProjectEndDate = dto.DateEnd.Value;
			}

			{
				if (project.ProjectStartDate >= project.ProjectEndDate)
					return BadRequest("Start date must be before end date");

				bool duplicate = await _dataContext.Projects
					.Where(x => x.Id != projectId)
					.AnyAsync(x => x.Name == dto.Name);
				if (duplicate)
					return BadRequest("Duplicated name");
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		public static async Task<FileStreamResult> GetImage(IFileManagerService fileManager, string? url) {
			if (url == null)
				throw new FileNotFoundException();

			byte[] fileBytes = await FileHelpers.GetFileBytes(fileManager, url);

			var extProvider = new FileExtensionContentTypeProvider();
			extProvider.TryGetContentType(url, out string? mediaType);

			return new FileStreamResult(new MemoryStream(fileBytes), mediaType ?? "application/octet-stream");
		}

		[HttpGet("logo")]
		public async Task<IActionResult> GetLogo() {
			var project = await _repoProject.GetProjectAsync();

			FileStreamResult streamResult;
			try {
				streamResult = await GetImage(_fileManager, project.LogoUrl);
			}
			catch (FileNotFoundException) {
				return NotFound();
			}
			catch (Exception e) {
				return StatusCode(500, e.Message);
			}

			return streamResult;
		}
		[HttpGet("banner")]
		public async Task<IActionResult> GetBanner() {
			var project = await _repoProject.GetProjectAsync();

			FileStreamResult streamResult;
			try {
				streamResult = await GetImage(_fileManager, project.BannerUrl);
			}
			catch (FileNotFoundException) {
				return NotFound();
			}
			catch (Exception e) {
				return StatusCode(500, e.Message);
			}

			return streamResult;
		}

		private async Task<string> _UploadImage(Project project, IFormFile file, string prefix) {
			string baseDir = FileHelpers.GetResourceDirectory(project.Id, "Icons");
			string path = Path.Combine(baseDir, $"{prefix}_{file.FileName}");

			{
				// If duplicate name, generate random string and add it to the name

				string pathNoExt = Path.GetFileNameWithoutExtension(file.FileName);
				string pathExt = Path.GetExtension(file.FileName);

				while (await _fileManager.Exists(path)) {
					string randH = AuthHelpers.GeneratePassword(new Random(GetHashCode()), 4, false);

					path = Path.Combine(baseDir, $"{prefix}_{pathNoExt}_{randH}{pathExt}");
				}
			}

			// Upload new image
			{
				var stream = file.OpenReadStream();

				await _fileManager.CreateFile(path, stream);
			}

			return path;
		}

		[HttpPut("logo/{type}")]
		public async Task<IActionResult> EditLogo([FromRoute] string type, [FromForm] IFormFile file) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.IsAdmin())
				return Forbid();

			// TODO: Throw error if file is not an image

			switch (type) {
				case "logo": {
					var pathNewImage = await _UploadImage(project, file, "logo");

					if (project.LogoUrl != null) {
						// Remove previous image
						await _fileManager.DeleteFile(project.LogoUrl);
					}
					project.LogoUrl = pathNewImage;

					break;
				}
				case "banner": {
					var pathNewImage = await _UploadImage(project, file, "banner");

					if (project.BannerUrl != null) {
						// Remove previous image
						await _fileManager.DeleteFile(project.BannerUrl);
					}
					project.BannerUrl = pathNewImage;

					break;
				}
				default:
					return BadRequest("Unknown type");
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}
	}
}
