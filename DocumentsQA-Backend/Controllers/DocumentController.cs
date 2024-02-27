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

		public DocumentController(DataContext dataContext, ILogger<DocumentController> logger, IAccessService access) {
			_dataContext = dataContext;
			_logger = logger;

			_access = access;

			if (!_access.IsValidUser())
				throw new AccessUnauthorizedException();
		}

		// -----------------------------------------------------

		private bool AllowDocumentAccess(Document document) {
			// NullReferenceException purposely not guarded against here, handle it in caller code
			switch (document.Type) {
				case DocumentType.General: {
					var project = document.Project;
					if (_access.AllowToProject(project!))
						return true;
					break;
				}
				case DocumentType.Question: {
					var project = document.AssocQuestion!.Project;
					if (_access.AllowToProject(project))
						return true;
					break;
				}
				case DocumentType.Account: {
					var tranche = document.AssocAccount!.Tranche;
					if (_access.AllowToTranche(tranche))
						return true;
					break;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets document information
		/// </summary>
		[HttpGet("info/{id}")]
		public async Task<IActionResult> GetDocumentInfo(int id, [FromQuery] int details = 0) {
			Document? document = await Queries.GetDocumentFromId(_dataContext, id);
			if (document == null)
				return BadRequest("Document not found");

			try {
				if (!AllowDocumentAccess(document))
					return Unauthorized();
			}
			catch (NullReferenceException) {
				return BadRequest("Data error");
			}

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

			try {
				if (!AllowDocumentAccess(document))
					return Unauthorized();
			}
			catch (NullReferenceException) {
				return BadRequest("Data error");
			}

			byte[] fileBytes;
			try {
				using HttpClient client = new();
				fileBytes = await client.GetByteArrayAsync(document.FileUrl);
			}
			catch (Exception) {
				return NotFound(document.FileName);
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
				return Unauthorized();

			var listDocuments = await _dataContext.Documents
				.Where(x => x.ProjectId == id)
				.Where(x => x.Type == DocumentType.General)
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
				return Unauthorized();

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
			if (!_access.AllowToProject(account.Project))
				return Unauthorized();

			var listDocuments = account.Documents
				.OrderBy(x => x.DateUploaded);
			var listDocumentTables = listDocuments.Select(x => x.ToJsonTable(details));

			return Ok(listDocumentTables);
		}

		/// <summary>
		/// Get all recently uploaded documents
		/// <para>Ordered by upload date</para>
		/// </summary>
		[HttpGet("recent/{id}")]
		public async Task<IActionResult> GetDocuments_Recent(int id, [FromQuery] int details = 0) {
			Project? project = await Queries.GetProjectFromId(_dataContext, id);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowToProject(project))
				return Unauthorized();

			var baseQuery = _dataContext.Documents
				.Where(x => x.ProjectId == id)
				.OrderBy(x => x.DateUploaded);
			var listDocuments = await baseQuery.ToListAsync();

			// Allow staff to everything, but filter based on access for regular users
			if (!_access.IsSuperUser()) {
				var mapAccounts = await baseQuery
					.Where(x => x.AssocAccountId != null)
					.GroupBy(x => (int)x.AssocAccountId!)
					.ToDictionaryAsync(x => x.Key, x => _access.AllowToProject(x.First().Project));
				var mapPosts = await baseQuery
					.Where(x => x.AssocQuestionId != null)
					.GroupBy(x => (int)x.AssocAccountId!)
					.ToDictionaryAsync(x => x.Key, x => _access.AllowToProject(x.First().Project));

				listDocuments = listDocuments
					.Where(x => x.Type switch {
						DocumentType.Question => mapAccounts[(int)x.AssocQuestionId!],
						DocumentType.Account => mapPosts[(int)x.AssocAccountId!],
						_ => true,
					})
					.ToList();
			}

			var listDocumentTables = listDocuments.Select(x => x.ToJsonTable(details));

			return Ok(listDocumentTables);
		}

		// -----------------------------------------------------

		private async Task<(bool, Document)> _DocumentFromUploadDTO(int projectId, DocumentUploadDTO upload) {
			string fileName = $"{projectId}_{Path.GetFileName(upload.Url)}";
			string fileExt = Path.GetExtension(fileName)[1..];		// substr to remove the dot

			int uploaderId = _access.GetUserID();

			var document = new Document {
				FileUrl = upload.Url,
				FileName = fileName,
				FileType = fileExt,
				Description = upload.Description,

				Hidden = upload.Hidden ?? false,
				AllowPrint = upload.Printable ?? false,

				UploadedById = uploaderId,
				ProjectId = projectId,

				DateUploaded = DateTime.Now,
			};

			var bNameAlreadyExists = await _dataContext.Documents
				.Where(x => x.ProjectId == projectId)
				.Where(x => x.FileName == fileName)
				.AnyAsync();

			return (!bNameAlreadyExists, document);
		}

		/// <summary>
		/// Uploads a general document, attached to a project
		/// </summary>
		[HttpPost("upload/project/{id}")]
		public async Task<IActionResult> UploadDocument_General(int id, [FromBody] DocumentUploadDTO upload) {
			Project? project = await Queries.GetProjectFromId(_dataContext, id);
			if (project == null)
				return BadRequest("Project not found");
			if (!_access.AllowManageProject(project))
				return Unauthorized();

			var (bValid, document) = await _DocumentFromUploadDTO(id, upload);
			if (!bValid)
				return BadRequest($"File {document.FileName} already exists");

			document.Type = DocumentType.General;

			_dataContext.Documents.Add(document);
			await _dataContext.SaveChangesAsync();

			return Ok(document.Id);
		}

		/// <summary>
		/// Uploads a document, attached to a specific question
		/// </summary>
		[HttpPost("upload/post/{id}")]
		public async Task<IActionResult> UploadDocument_Post(int id, [FromBody] DocumentUploadDTO upload) {
			Question? question = await Queries.GetQuestionFromId(_dataContext, id);
			if (question == null)
				return BadRequest("Question not found");
			Project project = question.Project;

			if (!_access.AllowManageProject(project))
				return Unauthorized();

			var (bValid, document) = await _DocumentFromUploadDTO(id, upload);
			if (!bValid)
				return BadRequest($"File {document.FileName} already exists");

			document.Type = DocumentType.Question;
			document.AssocQuestionId = id;

			_dataContext.Documents.Add(document);
			await _dataContext.SaveChangesAsync();

			return Ok(document.Id);
		}

		/// <summary>
		/// Uploads a document, attached to a specific account
		/// </summary>
		[HttpPost("upload/account/{id}")]
		public async Task<IActionResult> UploadDocument_Account(int id, [FromBody] DocumentUploadDTO upload) {
			Account? account = await Queries.GetAccountFromId(_dataContext, id);
			if (account == null)
				return BadRequest("Account not found");

			if (!_access.AllowManageProject(account.Project))
				return Unauthorized();

			var (bValid, document) = await _DocumentFromUploadDTO(id, upload);
			if (!bValid)
				return BadRequest($"File {document.FileName} already exists");

			document.Type = DocumentType.Account;
			document.AssocAccountId = id;

			_dataContext.Documents.Add(document);
			await _dataContext.SaveChangesAsync();

			return Ok(document.Id);
		}

		// -----------------------------------------------------

		/// <summary>
		/// Edits document info
		/// </summary>
		[HttpPut("edit/{id}")]
		public async Task<IActionResult> EditDocument(int id, [FromBody] DocumentEditDTO dto) {
			Document? document = await Queries.GetDocumentFromId(_dataContext, id);
			if (document == null)
				return BadRequest("Document not found");

			if (!_access.AllowManageProject(document.Project))
				return Unauthorized();

			if (dto.Description != null) {
				document.Description = dto.Description;
			}
			if (dto.Url != null) {
				string fileName = $"{document.ProjectId}_{Path.GetFileName(dto.Url)}";
				string fileExt = Path.GetExtension(fileName)[1..];		// substr to remove the dot

				document.FileUrl = dto.Url;
				document.FileName = fileName;
				document.FileType = fileExt;
			}
			if (dto.Hidden != null) {
				document.Hidden = dto.Hidden.Value;
			}
			if (dto.Printable != null) {
				document.AllowPrint = dto.Printable.Value;
			}

			return Ok();
		}
	}
}
