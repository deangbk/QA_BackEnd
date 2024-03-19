using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;

using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;
using DocumentsQA_Backend.Repository;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/document")]
	[Authorize]
	public class DocumentController : Controller {
		private readonly ILogger<DocumentController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IFileManagerService _fileManager;
		private readonly IProjectRepository _repoProject;

		public DocumentController(
			ILogger<DocumentController> logger,
			DataContext dataContext, 
			IAccessService access,

			IFileManagerService fileManager,
			IProjectRepository repoProject)
		{
			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			_fileManager = fileManager;
			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets document information
		/// </summary>
		[HttpGet("info/{id}")]
		public async Task<IActionResult> GetDocumentInfo(int id, [FromQuery] int details = 0) {
			Document? document = await Queries.GetDocumentFromId(_dataContext, id);
			if (document == null)
				return BadRequest("Document not found");

			var project = document.Project;

			if (!_access.AllowToProject(project))
				return Forbid();
			AuthHelpers.GuardDetailsLevel(_access, project, details, 4);

			return Ok(document.ToJsonTable(details));
		}

		/// <summary>
		/// Gets document file stream, encrypted with a basic, non-cryptographically-secure method
		/// </summary>
		[HttpGet("stream/{id}")]
		public async Task<IActionResult> GetDocument(int id) {
			Document? document = await Queries.GetDocumentFromId(_dataContext, id);
			if (document == null)
				return BadRequest("Document not found");

			if (!_access.AllowToProject(document.Project))
				return Forbid();

			byte[] fileBytes;
			try {
				string path = DocumentHelpers.GetDocumentFileRoute(document);

				using var ms = new MemoryStream();
				await _fileManager.ReadFile(path, ms);

				fileBytes = ms.ToArray();
			}
			catch (FileNotFoundException) {
				return NotFound(document.FileName);
			}
			catch (Exception e) {
				return StatusCode(500, e.Message);
			}

			{
				// Basic encryption on the bytes, no need to be cryptographically secure
				// Just enough so people couldn't just open the browser console and export it

				int userId = _access.GetUserID();
				var hashKey = MD5.HashData(Encoding.ASCII.GetBytes(userId.ToString()));
				for (int i = 0; i < fileBytes.Length; ++i) {
					fileBytes[i] ^= hashKey[i % hashKey.Length];
				}
			}

			if (true) {
				// Return as base64
				string bytesEncode = Convert.ToBase64String(fileBytes);
				return HttpHelpers.StringToFileStreamResult(bytesEncode, "application/octet-stream");
			}
			else {
				// Return as raw binary
				var stream = new MemoryStream(fileBytes);
				return new FileStreamResult(stream, "application/octet-stream");
			}
		}

		// -----------------------------------------------------

		/// <summary>
		/// Get all general documents in the project
		/// <para>Ordered by upload date</para>
		/// </summary>
		[HttpGet("with/project")]
		public async Task<IActionResult> GetDocuments_General([FromQuery] int details = 0) {
			var project = await _repoProject.GetProjectAsync();

			AuthHelpers.GuardDetailsLevel(_access, project, details, 4);

			var listDocuments = await _dataContext.Documents
				.Where(x => x.ProjectId == project.Id)
				.Where(x => x.Type == DocumentType.Transaction || x.Type == DocumentType.Bid)
				.OrderBy(x => x.DateUploaded)
				.ToListAsync();
			var listDocumentTables = listDocuments.Select(x => x.ToJsonTable(details));

			return Ok(listDocumentTables);
		}

		/// <summary>
		/// Get all documents attached to the post
		/// <para>Ordered by upload date</para>
		/// </summary>
		[HttpGet("with/post/{id}")]
		public async Task<IActionResult> GetDocuments_Post(int id, [FromQuery] int details = 0) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");

			if (!_access.AllowToProject(question.Project))
				return Forbid();
			if (!PostHelpers.AllowUserReadPost(_access, question))
				return Forbid();

			AuthHelpers.GuardDetailsLevel(_access, question.Project, details, 4);

			var listDocuments = question.Attachments
				.OrderBy(x => x.DateUploaded);
			var listDocumentTables = listDocuments.Select(x => x.ToJsonTable(details));

			return Ok(listDocumentTables);
		}

		/// <summary>
		/// Get all documents related to the account
		/// <para>Ordered by upload date</para>
		/// </summary>
		[HttpGet("with/account/{id}")]
		public async Task<IActionResult> GetDocuments_Account(int id, [FromQuery] int details = 0) {
			Account? account = await Queries.GetAccountFromId(_dataContext, id);
			if (account == null)
				return BadRequest("Account not found");

			if (!_access.AllowToTranche(account.Tranche))
				return Forbid();
			AuthHelpers.GuardDetailsLevel(_access, account.Project, details, 4);

			var listDocuments = account.Documents
				.OrderBy(x => x.DateUploaded);
			var listDocumentTables = listDocuments.Select(x => x.ToJsonTable(details));

			return Ok(listDocumentTables);
		}

		/// <summary>
		/// Get all recently uploaded documents
		/// <para>Ordered by upload date</para>
		/// </summary>
		[HttpPost("recent")]
		public async Task<IActionResult> GetDocuments_Recent([FromBody] DocumentGetDTO dto, [FromQuery] int details = 0) {
			var project = await _repoProject.GetProjectAsync();
			var projectId = project.Id;

			AuthHelpers.GuardDetailsLevel(_access, project, details, 4);

			var baseQuery = _dataContext.Documents
				.Where(x => x.ProjectId == projectId);

			var filterDTO = dto.Filter;
			var pageDTO = dto.Paginate;

			if (filterDTO != null) {
				if (filterDTO.SearchTerm != null) {
					baseQuery = baseQuery.Where(x => EF.Functions.Contains(x.FileName, filterDTO.SearchTerm));
				}
				if (filterDTO.UploaderID != null) {
					baseQuery = baseQuery.Where(x => x.UploadedById == filterDTO.UploaderID);
				}
				if (filterDTO.PostedFrom is not null) {
					baseQuery = baseQuery.Where(x => x.DateUploaded >= filterDTO.PostedFrom);
				}
				if (filterDTO.PostedTo is not null) {
					baseQuery = baseQuery.Where(x => x.DateUploaded < filterDTO.PostedTo);
				}
				if (filterDTO.AllowPrint != null) {
					baseQuery = baseQuery.Where(x => x.AllowPrint == filterDTO.AllowPrint);
				}

				if (filterDTO.Category != null) {
					var typeMatch = DocumentHelpers.ParseDocumentType(filterDTO.Category)
						?? DocumentType.Bid;
					baseQuery = baseQuery.Where(x => x.Type == typeMatch);
				}
				if (filterDTO.AssocQuestion != null) {
					baseQuery = baseQuery.Where(x => x.Type == DocumentType.Question
						&& x.AssocQuestionId == filterDTO.AssocQuestion);
				}
				if (filterDTO.AssocAccount != null) {
					baseQuery = baseQuery.Where(x => x.Type == DocumentType.Account
						&& x.AssocAccountId == filterDTO.AssocAccount);
				}
				if (filterDTO.AssocTranche != null) {
					// TODO: Test this
					try {
						int trancheId = await _dataContext.Tranches
							.Where(x => x.Name == filterDTO.AssocTranche)
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
				AppUser user = (await Queries.GetUserFromId(_dataContext, _access.GetUserID()))!;
				var trancheAccesses = ProjectHelpers.GetUserTrancheAccessesInProject(
					user, projectId);

				var listAllowedAccounts = trancheAccesses
					.SelectMany(x => x.Accounts.Select(x => x.Id))
					.ToHashSet();

				listDocuments = listDocuments
					.Where(x => x.Type switch {
						DocumentType.Account => listAllowedAccounts.Contains((int)x.AssocAccountId!),
						DocumentType.Question => x.AssocQuestion!.AccountId == null
							|| listAllowedAccounts.Contains((int)x.AssocQuestion!.AccountId),
						_ => true,
					})
					.ToList();
			}

			// Paginate result; but return everything if paginate DTO doesn't exist
			if (pageDTO != null) {
				int countPerPage = pageDTO.CountPerPage;
				int maxPages = (int)Math.Ceiling(listDocuments.Count / (double)countPerPage);

				listDocuments = listDocuments
					.Skip(pageDTO.Page!.Value * countPerPage)
					.Take(countPerPage)
					.ToList();
			}

			var listDocumentTables = listDocuments.Select(x => x.ToJsonTable(details));

			return Ok(listDocumentTables);
		}

		// -----------------------------------------------------

		private string? _ValidateDocumentType(DocumentUploadDTO dto, Document document) {
			var docType = DocumentHelpers.ParseDocumentType(dto.Type);
			if (docType == null) {
				return ("Invalid document type: " + dto.Type);
			}

			switch (docType) {
				case DocumentType.Question: {
					if (dto.AssocQuestion == null) {
						ModelState.AddModelError("with_post", "with_post must not be null");
						throw new InvalidModelStateException(ModelState);
					}

					document.Type = DocumentType.Question;
					document.AssocQuestionId = dto.AssocQuestion!.Value;

					break;
				}
				case DocumentType.Account: {
					if (dto.AssocAccount == null) {
						ModelState.AddModelError("with_account", "with_account must not be null");
						throw new InvalidModelStateException(ModelState);
					}

					document.Type = DocumentType.Account;
					document.AssocAccountId = dto.AssocAccount!.Value;

					break;
				}
				case DocumentType.Bid:
					document.Type = DocumentType.Bid;
					break;
				case DocumentType.Transaction:
					document.Type = DocumentType.Transaction;
					break;
			}
			return null;
		}

		/// <summary>
		/// Uploads documents
		/// </summary>
		[HttpPost("upload/file")]
		public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadWithFileDTO dto) {
			List<DocumentUploadDTO> descs;
			{
				var parse = JsonSerializer.Deserialize<List<DocumentUploadDTO>>(dto.DescsJson, 
					new JsonSerializerOptions {
						PropertyNameCaseInsensitive = true
					});
				if (parse == null) {
					return BadRequest("descs must not be null");
				}
				descs = parse;

				// TODO: Might not work, test later
				var result = new List<ValidationResult>();
				foreach (var desc in descs) {
					var ctx = new ValidationContext(desc);
					if (!Validator.TryValidateObject(desc, ctx, result)) {
						return BadRequest(result);
					}
				}
			}

			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			List<Document> documents = new();

			foreach (var docDto in descs) {
				var document = DocumentHelpers.CreateFromDTO(project.Id, docDto);
				document.UploadedById = _access.GetUserID();

				if (await DocumentHelpers.CheckDuplicate(_dataContext, document))
					return BadRequest($"File {document.FileName} already exists");

				{
					var validateRes = _ValidateDocumentType(docDto, document);
					if (validateRes != null) {
						return BadRequest(validateRes);
					}
				}

				documents.Add(document);
			}

			{
				List<string> uploaded = new();

				try {
					foreach (var (doc, file) in documents.Zip(dto.Files)) {
						string path = DocumentHelpers.GetDocumentFileRoute(doc);

						var stream = file.OpenReadStream();

						await _fileManager.CreateFile(path, stream);
						uploaded.Add(path);
					}

					_dataContext.Documents.AddRange(documents);
					await _dataContext.SaveChangesAsync();
				}
				catch (Exception e) {
					// If failed, revert all successful file uploads
					foreach (var dp in uploaded) {
						await _fileManager.DeleteFile(dp);
					}

					return StatusCode(500, e.Message);
				}
			}

			return Ok(documents.Select(x => x.Id).ToList());
		}

		/// <summary>
		/// Uploads documents by adding new entries to the system. No file is created anywhere
		/// </summary>
		[HttpPost("upload")]
		public async Task<IActionResult> UploadDocumentEntryOnly([FromBody] List<DocumentUploadDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			List<Document> documents = new();

			foreach (var dto in dtos) {
				if (dto.Url == null) {
					ModelState.AddModelError("Url", "Url must not be null");
					return BadRequest(new ValidationProblemDetails(ModelState));
				}

				var document = DocumentHelpers.CreateFromDTO(project.Id, dto);
				document.UploadedById = _access.GetUserID();

				if (await DocumentHelpers.CheckDuplicate(_dataContext, document))
					return BadRequest($"File {document.FileName} already exists");

				{
					var validateRes = _ValidateDocumentType(dto, document);
					if (validateRes != null) {
						return BadRequest(validateRes);
					}
				}

				documents.Add(document);
			}

			_dataContext.Documents.AddRange(documents);
			await _dataContext.SaveChangesAsync();

			return Ok(documents.Select(x => x.Id).ToList());
		}

		// -----------------------------------------------------

		private async Task<Dictionary<int, Document>> _GetDocumentsMapAndCheckAccess(Project project, List<int> ids) {
			int projectId = project.Id;

			var mapDocuments = (await Queries.GetDocumentsMapFromIds(_dataContext, ids))!;

			{
				// Check if the DTO contains documents from other projects
				var invalidProjects = mapDocuments
					.Select(x => x.Value.ProjectId)
					.Where(x => x != projectId)
					.ToList();
				if (invalidProjects.Count > 0)
					throw new InvalidOperationException("Invalid document IDs: " + invalidProjects.ToStringEx());

				// Detect invalid IDs
				if (mapDocuments.Count != ids.Count) {
					var invalidIds = ids.Except(mapDocuments.Keys);
					throw new InvalidOperationException("Documents not found: " + invalidIds.ToStringEx());
				}
			}

			return mapDocuments;
		}

		/// <summary>
		/// Bulk edits documents info
		/// </summary>
		[HttpPut("bulk/edit")]
		public async Task<IActionResult> EditDocuments([FromBody] List<DocumentEditDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			var ids = dtos.Select(x => x.Id!.Value).ToList();
			var mapDocuments = await _GetDocumentsMapAndCheckAccess(project, ids);

			{
				// Detect any duplicate file names

				var fileNames = dtos
					.Where(x => x.Name != null)
					.Select(x => x.Name!)
					.ToList();

				var duplicateNames = await _dataContext.Documents
					.Where(x => x.ProjectId == project.Id)
					.Where(x => fileNames.Any(y => y == x.FileName))
					.ToListAsync();
				if (duplicateNames.Count > 0)
					return BadRequest("Duplicate file names: " + duplicateNames.ToStringEx());
			}

			foreach (var d in dtos) {
				var document = mapDocuments[d.Id!.Value];

				if (d.Name != null) {
					document.FileName = d.Name;
				}
				if (d.Description != null) {
					document.Description = d.Description;
				}
				if (d.Url != null) {
					string fileExt = Path.GetExtension(d.Url)[1..];       // substr to remove the dot

					document.FileUrl = d.Url;
					document.FileType = fileExt;
				}
				if (d.Hidden != null) {
					document.Hidden = d.Hidden.Value;
				}
				if (d.Printable != null) {
					document.AllowPrint = d.Printable.Value;
				}
			}

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}

		// -----------------------------------------------------

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteDocument(int id) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			Document? document = await Queries.GetDocumentFromId(_dataContext, id);
			if (document == null)
				return BadRequest("Document not found");

			await _fileManager.DeleteFile(
				DocumentHelpers.GetDocumentFileRoute(document));

			_dataContext.Documents.Remove(document);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		[HttpPost("bulk/delete")]
		public async Task<IActionResult> DeleteDocumentMultiple([FromBody] List<int> docIds) {
			var project = await _repoProject.GetProjectAsync();
			if (!_access.AllowManageProject(project))
				return Forbid();

			var mapDocuments = await _GetDocumentsMapAndCheckAccess(project, docIds);

			_dataContext.Documents.RemoveRange(mapDocuments.Values.ToArray());

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}
	}
}
