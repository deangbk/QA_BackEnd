using System.Net;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[Table("EventLog_Login")]
	[PrimaryKey(nameof(Id))]
	public class LogInEvent {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public DateTime Timestamp { get; set; }

		public int? UserId { get; set; }						// FK to User
		public virtual AppUser? User { get; set; }				// Reference navigation to FK

		public int ProjectId { get; set; }						// FK to Project
		public virtual Project Project { get; set; } = null!;	// Reference navigation to FK

		public IPAddress IPAddress { get; set; } = null!;
	}

	public enum ViewType : int {
		Question,
		Account,
		Tranche,
		Document,
	}

	[Table("EventLog_View")]
	[PrimaryKey(nameof(Id))]
	public class ViewEvent {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public DateTime Timestamp { get; set; }

		public int? UserId { get; set; }						// FK to User
		public virtual AppUser? User { get; set; }				// Reference navigation to FK

		public int ProjectId { get; set; }						// FK to Project
		public virtual Project Project { get; set; } = null!;	// Reference navigation to FK

		public ViewType Type { get; set; }
		public int ViewId { get; set; }							// Polymorphic association foreign key depending on Type
	}
}
