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

		public bool QuestionApproved { get; set; } = false;
		public bool AnswerApproved { get; set; } = false;
		public DateTime DateQuestionApproved { get; set; }
		public DateTime DateAnswerApproved { get; set; }

		public string PostedById { get; set; } = null!;
		public EDUser PostedBy { get; set; } = null!;			// Reference navigation to FK
		public string? AnsweredById { get; set; }
		public EDUser? AnsweredBy { get; set; } = null!;		// Reference navigation to FK

		public string? QuestionApprovedById { get; set; }
		public EDUser? QuestionApprovedBy { get; set; } = null!;		// Reference navigation to FK
		public string? AnswerApprovedById { get; set; }
		public EDUser? AnswerApprovedBy { get; set; } = null!;			// Reference navigation to FK
		
		public string LastEditorId { get; set; } = null!;
		public EDUser LastEditor { get; set; } = null!;			// Reference navigation to FK
		public DateTime DateLastEdited { get; set; }

		public bool DailyEmailSent { get; set; } = false;
	}
}
