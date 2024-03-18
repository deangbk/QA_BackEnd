using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/document")]
	//[Authorize]
	public class DocumentController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<DocumentController> _logger;

		private readonly IAccessService _access;

		private readonly IFileManagerService _fileManager;

		public DocumentController(
			DataContext dataContext, ILogger<DocumentController> logger,
			IAccessService access,
			IWebHostEnvironment env,
			IFileManagerService fileManager)
		{
			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			_fileManager = fileManager;

			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();
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
		[HttpGet("with/project/{id}")]
		public async Task<IActionResult> GetDocuments_General(int id, [FromQuery] int details = 0) {
			Project? project = await Queries.GetProjectFromId(_dataContext, id);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();
			AuthHelpers.GuardDetailsLevel(_access, project, details, 4);

			var listDocuments = await _dataContext.Documents
				.Where(x => x.ProjectId == id)
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
		[HttpPost("recent/{id}")]
		public async Task<IActionResult> GetDocuments_Recent(int id, [FromBody] DocumentGetDTO dto, [FromQuery] int details = 0) {
			Project? project = await Queries.GetProjectFromId(_dataContext, id);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowToProject(project))
				return Forbid();
			AuthHelpers.GuardDetailsLevel(_access, project, details, 4);

			var baseQuery = _dataContext.Documents
				.Where(x => x.ProjectId == id);

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
				var trancheAccesses = ProjectHelpers.GetUserTrancheAccessesInProject(user, id);

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
		/// Uploads a document
		/// </summary>
		[HttpPost("upload/file/{pid}")]
		public async Task<IActionResult> UploadDocument(int pid, [FromForm] DocumentUploadDTO dto, [FromForm] IFormFile file) {
			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			var document = DocumentHelpers.CreateFromDTO(pid, dto);
			document.UploadedById = _access.GetUserID();

			if (await DocumentHelpers.CheckDuplicate(_dataContext, document))
				return BadRequest($"File {document.FileName} already exists");

			{
				var validateRes = _ValidateDocumentType(dto, document);
				if (validateRes != null) {
					return BadRequest(validateRes);
				}
			}

			using (var transaction = _dataContext.Database.BeginTransaction()) {
				_dataContext.Documents.Add(document);
				bool success = (await _dataContext.SaveChangesAsync()) > 0;

				if (success) {
					try {
						string path = DocumentHelpers.GetDocumentFileRoute(document);

						using var ms = new MemoryStream();
						await file.CopyToAsync(ms);
						ms.Position = 0;

						await _fileManager.CreateFile(path, ms);
					}
					catch (Exception e) {
						return StatusCode(500, e.Message);
					}
				}

				await transaction.CommitAsync();
			}
			

			return Ok(document.Id);
		}

		/// <summary>
		/// Uploads a document by adding a new entry to the system. No file is created anywhere
		/// </summary>
		[HttpPost("upload/{pid}")]
		public async Task<IActionResult> UploadDocumentEntryOnly(int pid, [FromBody] DocumentUploadDTO dto) {
			if (dto.Url == null) {
				ModelState.AddModelError("Url", "Url must not be null");
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			Project? project = await Queries.GetProjectFromId(_dataContext, pid);
			if (project == null)
				return BadRequest("Project not found");

			if (!_access.AllowManageProject(project))
				return Forbid();

			var document = DocumentHelpers.CreateFromDTO(pid, dto);
			document.UploadedById = _access.GetUserID();

				if (await DocumentHelpers.CheckDuplicate(_dataContext, document))
				return BadRequest($"File {document.FileName} already exists");

			{
				var validateRes = _ValidateDocumentType(dto, document);
				if (validateRes != null) {
					return BadRequest(validateRes);
				}
			}

			_dataContext.Documents.Add(document);
			await _dataContext.SaveChangesAsync();

			return Ok(document.Id);
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
		[HttpPut("bulk/edit/{id}")]
		public async Task<IActionResult> EditDocuments(int id, [FromBody] List<DocumentEditDTO> dtos) {
			Project? project = await Queries.GetProjectFromId(_dataContext, id);
			if (project == null)
				return BadRequest("Project not found");

			// Only staff can do this
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

		[HttpDelete("{id}/{docId}")]
		public async Task<IActionResult> DeleteDocument(int id, int docId) {
			Project? project = await Queries.GetProjectFromId(_dataContext, id);
			if (project == null)
				return BadRequest("Project not found");

			// Only staff can do this
			if (!_access.AllowManageProject(project))
				return Forbid();

			Document? document = await Queries.GetDocumentFromId(_dataContext, docId);
			if (document == null)
				return BadRequest("Document not found");

			await _fileManager.DeleteFile(
				DocumentHelpers.GetDocumentFileRoute(document));

			_dataContext.Documents.Remove(document);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		[HttpPost("bulk/delete/{id}")]
		public async Task<IActionResult> DeleteDocumentMultiple(int id, [FromBody] List<int> docIds) {
			Project? project = await Queries.GetProjectFromId(_dataContext, id);
			if (project == null)
				return BadRequest("Project not found");

			// Only staff can do this
			if (!_access.AllowManageProject(project))
				return Forbid();

			var mapDocuments = await _GetDocumentsMapAndCheckAccess(project, docIds);

			_dataContext.Documents.RemoveRange(mapDocuments.Values.ToArray());

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}
	}
}
