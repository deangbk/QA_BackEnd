using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Comment {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int CommentNum { get; set; }
		public string CommentText { get; set; } = null!;
		public DateTime DatePosted { get; set; }

		public int QuestionId { get; set; }					// FK to Question
		public Question Question { get; set; } = null!;		// Reference navigation to FK

		public int PostedById { get; set; }					// FK to User
		public AppUser PostedBy { get; set; } = null!;		// Reference navigation to FK
	}
}
