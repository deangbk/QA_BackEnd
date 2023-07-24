using System.Net;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[Table("EventLog_Login")]
	[PrimaryKey(nameof(Timestamp), nameof(UserId))]
	public class LogInEvent {
		public DateTime Timestamp { get; set; }

		public int UserId { get; set; }					// FK to User
		public AppUser User { get; set; } = null!;		// Reference navigation to FK

		public IPAddress IPAddress { get; set; } = null!;
	}

	[Table("EventLog_View")]
	[PrimaryKey(nameof(Timestamp), nameof(UserId))]
	public class ViewEvent {
		public DateTime Timestamp { get; set; }

		public int UserId { get; set; }							// FK to User
		public AppUser User { get; set; } = null!;				// Reference navigation to FK

		public int? ViewedAccountId { get; set; }				// FK to Account
		public Account? ViewedAccount { get; set; } = null!;	// Reference navigation to FK

		public int? ViewedTrancheId { get; set; }				// FK to User
		public Tranche? ViewedTranche { get; set; } = null!;	// Reference navigation to FK
	}
}
