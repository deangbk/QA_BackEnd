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

		public int QuestionId { get; set; }			// FK to Question
		public virtual Question Question { get; set; } = null!;

		public int PostedById { get; set; }			// FK to User
		public virtual AppUser PostedBy { get; set; } = null!;
	}

	[PrimaryKey(nameof(Id))]
	public class Note {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int ProjectId { get; set; }					// FK to Project
		public virtual Project Project { get; set; } = null!;

		public int Num { get; set; }
		public string Text { get; set; } = null!;
		public string Description { get; set; } = null!;
		public string Category { get; set; } = null!;
		public DateTime DatePosted { get; set; }

		public bool Sticky { get; set; }

		public int PostedById { get; set; }					// FK to User
		public virtual AppUser PostedBy { get; set; } = null!;
	}
}
