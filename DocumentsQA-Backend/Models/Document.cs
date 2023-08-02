﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Document {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[MaxLength(256)]
		public string FileName { get; set; } = null!;
		public string? Description { get; set; }

		[Url]
		[MaxLength(256)]
		public string FileUrl { get; set; } = null!;
		[MaxLength(64)]
		public string? FileType { get; set; }

		public int UploadedById { get; set; }			// FK to User
		public virtual AppUser UploadedBy { get; set; } = null!;
		public DateTime DateUploaded { get; set; }

		public DocumentType Type { get; set; } = DocumentType.General;

		public int? AssocQuestionId { get; set; }		// FK to Question
		public virtual Question? AssocQuestion { get; set; } = null!;

		public int? AssocAccountId { get; set; }		// FK to Account
		public virtual Account? AssocAccount { get; set; } = null!;

		public bool Hidden { get; set; } = false;
		public bool AllowPrint { get; set; } = false;
	}

	public enum DocumentType {
		General,		// No association
		Question,		// Associated with a specific question
		Account,		// Associated with a specific account
	}
}
