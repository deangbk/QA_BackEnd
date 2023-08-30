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
		public QuestionType Type { get; set; }

		public int ProjectId { get; set; }					// FK to Project
		public virtual Project Project { get; set; } = null!;

		public int? AccountId { get; set; }					// FK to Account, null if general question
		public virtual Account? Account { get; set; }

		public string QuestionText { get; set; } = null!;
		public string? QuestionAnswer { get; set; }

		public int PostedById { get; set; }					// FK to User
		public virtual AppUser PostedBy { get; set; } = null!;
		public int? AnsweredById { get; set; }				// FK to User
		public virtual AppUser? AnsweredBy { get; set; }

		public int? QuestionApprovedById { get; set; }		// FK to User
		public virtual AppUser? QuestionApprovedBy { get; set; }
		public int? AnswerApprovedById { get; set; }		// FK to User
		public virtual AppUser? AnswerApprovedBy { get; set; }

		public int LastEditorId { get; set; }				// FK to User
		public virtual AppUser LastEditor { get; set; } = null!;

		public DateTime DatePosted { get; set; }
		public DateTime? DateAnswered { get; set; }
		public DateTime? DateQuestionApproved { get; set; }
		public DateTime? DateAnswerApproved { get; set; }
		public DateTime DateLastEdited { get; set; }

		public virtual List<Document> Attachments { get; set; } = new();	// One-to-many with Document
		public virtual List<Comment> Comments { get; set; } = new();		// One-to-many with Comment
	}

	public enum QuestionType {
		General,	// General question
		Account,	// Associated with a specific account
	}
}
