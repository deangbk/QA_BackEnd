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
	[Authorize(Policy = "Project_Access")]
	public class DocumentController : Controller {
		private readonly ILogger<DocumentController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IFileManagerService _fileManager;

		private readonly AuthHelpers _authHelper;
		private readonly DocumentHelpers _documentHelpers;
		private readonly IProjectRepository _repoProject;

		public DocumentController(
			ILogger<DocumentController> logger,
			DataContext dataContext, 
			IAccessService access,

			IFileManagerService fileManager,

			AuthHelpers authHelper,
			DocumentHelpers documentHelpers,
			IProjectRepository repoProject)
		{
			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			_fileManager = fileManager;

			_authHelper = authHelper;
			_documentHelpers = documentHelpers;
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
			_authHelper.GuardDetailsLevel(details, 4);

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
				fileBytes = await FileHelpers.GetFileBytes(_fileManager, path);
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

			_authHelper.GuardDetailsLevel(details, 4);

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

			_authHelper.GuardDetailsLevel(details, 4);

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
			_authHelper.GuardDetailsLevel(details, 4);

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

			_authHelper.GuardDetailsLevel(details, 4);

			var documents = await _documentHelpers.GetDocuments(dto);

			var listDocumentTables = documents.Select(x => x.ToJsonTable(details));
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
		[Authorize(Policy = "Project_Manage")]
		public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadWithFileDTO dto) {
			var project = await _repoProject.GetProjectAsync();

			List<Document> documents = new();

			{
				List<string> uploaded = new();

				try {
					foreach (var file in dto.Files) {
						var docDesc = new DocumentUploadDTO {
							Type = dto.Type,
							AssocQuestion = dto.AssocQuestion,
							AssocAccount = dto.AssocAccount,
							Description = dto.Description,
							Hidden = dto.Hidden,
							Printable = dto.Printable,
						};

						string fileName = file.FileName;
						docDesc.Url = fileName;

						var document = DocumentHelpers.CreateFromDTO(project.Id, docDesc, fileName);
						document.UploadedById = _access.GetUserID();

						{
							// If duplicate name, generate random string and add it to the name
							{
								string nameNoExt = Path.GetFileNameWithoutExtension(fileName);

								while (await _documentHelpers.CheckDuplicate(document)) {
									string randH = AuthHelpers.GeneratePassword(new Random(GetHashCode()), 4, false);
									document.FileUrl = $"{nameNoExt}_{randH}.{document.FileType}";
								}
							}

							{
								var validateRes = _ValidateDocumentType(docDesc, document);
								if (validateRes != null) {
									return BadRequest(validateRes);
								}
							}

							documents.Add(document);
						}

						string path = DocumentHelpers.GetDocumentFileRoute(document);

						var stream = file.OpenReadStream();

						await _fileManager.CreateFile(path, stream);
						uploaded.Add(path);
					}

					_dataContext.Documents.AddRange(documents);
					await _dataContext.SaveChangesAsync();
				}
				catch (Exception e) {
					// If any failed, revert all successful file uploads
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
		[Authorize(Policy = "Project_Manage")]
		public async Task<IActionResult> UploadDocumentEntryOnly([FromBody] List<DocumentUploadDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();

			List<Document> documents = new();

			foreach (var dto in dtos) {
				var document = DocumentHelpers.CreateFromDTO(project.Id, dto, dto.Url);
				document.UploadedById = _access.GetUserID();

				{
					string nameNoExt = Path.GetFileNameWithoutExtension(document.FileName);

					while (await _documentHelpers.CheckDuplicate(document)) {
						string randH = AuthHelpers.GeneratePassword(new Random(GetHashCode()), 4);
						document.FileUrl = $"{nameNoExt}_{randH}.{document.FileType}";
					}
				}

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
		[Authorize(Policy = "Project_Manage")]
		public async Task<IActionResult> EditDocuments([FromBody] List<DocumentEditDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();

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
		[Authorize(Policy = "Project_Manage")]
		public async Task<IActionResult> DeleteDocument(int id) {
			var project = await _repoProject.GetProjectAsync();

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
		[Authorize(Policy = "Project_Manage")]
		public async Task<IActionResult> DeleteDocumentMultiple([FromBody] List<int> docIds) {
			var project = await _repoProject.GetProjectAsync();

			var mapDocuments = await _GetDocumentsMapAndCheckAccess(project, docIds);

			_dataContext.Documents.RemoveRange(mapDocuments.Values.ToArray());

			var count = await _dataContext.SaveChangesAsync();
			return Ok(count);
		}
	}
}
