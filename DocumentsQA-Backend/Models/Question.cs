using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Question {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int QuestionNum { get; set; }
		public int Type { get; set; }

		public int ProjectId { get; set; }					// FK to Project
		public Project Project { get; set; } = null!;		// Reference navigation to FK

		public DateTime DatePosted { get; set; }

		public string QuestionText { get; set; } = null!;
		public string? QuestionAnswer { get; set; }

		public int AccountId { get; set; }					// FK to Account
		public Account Account { get; set; } = null!;		// Reference navigation to FK

		public int PostedById { get; set; }					// FK to User
		public AppUser PostedBy { get; set; } = null!;		// Reference navigation to FK
		public int? AnsweredById { get; set; }				// FK to User
		public AppUser? AnsweredBy { get; set; }			// Reference navigation to FK

		public int? QuestionApprovedById { get; set; }		// FK to User
		public AppUser? QuestionApprovedBy { get; set; }	// Reference navigation to FK
		public int? AnswerApprovedById { get; set; }		// FK to User
		public AppUser? AnswerApprovedBy { get; set; }		// Reference navigation to FK

		public DateTime? DateQuestionApproved { get; set; }
		public DateTime? DateAnswerApproved { get; set; }

		public int LastEditorId { get; set; }				// FK to User
		public AppUser LastEditor { get; set; } = null!;	// Reference navigation to FK
		public DateTime DateLastEdited { get; set; }

		public bool DailyEmailSent { get; set; } = false;

		public List<Document> Attachments { get; set; } = new();
		public List<Comment> Comments { get; set; } = new();
	}
}
