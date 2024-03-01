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
				return Forbid();

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
				return Forbid();

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
				return Forbid();

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
					DocumentType typeMatch = filterDTO.Category.ToLower() switch {
						"question" or "post"
									=> DocumentType.Question,
						"account"	=> DocumentType.Account,
						_ => DocumentType.General,
					};
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

		private async Task<(bool, Document)> _DocumentFromUploadDTO(int projectId, DocumentUploadDTO upload) {
			string fileName;
			if (upload.Name == null)
				fileName = Path.GetFileName(upload.Url);
			else fileName = upload.Name;

			string fileExt = Path.GetExtension(upload.Url)[1..];		// substr to remove the dot

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
				return Forbid();

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
				return Forbid();

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
				return Forbid();

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

			// Only staff can do this
			if (!_access.AllowManageProject(document.Project))
				return Forbid();

			if (dto.Name != null) {
				var bNameAlreadyExists = await _dataContext.Documents
					.Where(x => x.ProjectId == document.ProjectId)
					.Where(x => x.FileName == dto.Name)
					.AnyAsync();
				if (bNameAlreadyExists)
					return BadRequest($"File {dto.Name} already exists");

				document.FileName = dto.Name;
			}
			if (dto.Description != null) {
				document.Description = dto.Description;
			}
			if (dto.Url != null) {
				string fileExt = Path.GetExtension(dto.Url)[1..];		// substr to remove the dot

				document.FileUrl = dto.Url;
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
