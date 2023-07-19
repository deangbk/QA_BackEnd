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

		public string Tranche { get; set; } = null!;

		public DateTime DatePosted { get; set; }

		public string QuestionText { get; set; } = null!;
		public string? QuestionAnswer { get; set; }

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
	}
}
