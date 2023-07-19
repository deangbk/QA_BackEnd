using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Document {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public string? Description { get; set; }

		public string FileUrl { get; set; } = null!;
		public string? FileType { get; set; }

		public int UploadedById { get; set; }						// FK to User
		public AppUser UploadedBy { get; set; } = null!;			// Reference navigation to FK
		public DateTime DateUploaded { get; set; }

		public DocumentType Type { get; set; } = DocumentType.General;

		public int? AssocQuestionId { get; set; }					// FK to Question
		public Question? AssocQuestion { get; set; } = null!;		// Reference navigation to FK

		public int? AssocUserId { get; set; }						// FK to User
		public AppUser? AssocUser { get; set; } = null!;			// Reference navigation to FK

		public bool Hidden { get; set; } = false;
		public bool AllowPrint { get; set; } = false;
	}

	public enum DocumentType {
		General,		// No association
		Question,		// Associated with a specific question
		Account,		// Associated with a specific user
	}
}
