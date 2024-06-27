using System.Data;
using System.Security.Claims;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Data;

namespace DocumentsQA_Backend.Models {
	// Extends the AspNetUsers table, also changes the ID to be an int
	public class AppUser : IdentityUser<int> {
		public virtual List<Project> ProjectManages { get; set; } = new();		// Reference navigation for many-to-many FK
		public virtual List<Tranche> TrancheAccesses { get; set; } = new();     // Reference navigation for many-to-many FK

		[MaxLength(256)]
		public string Company { get; set; } = null!;

		[MaxLength(256)]
		public string DisplayName { get; set; } = null!;
		public DateTime DateCreated { get; set; }

		public int? ProjectId { get; set; }					// FK to Project
		public virtual Project? Project { get; set; }
	}
}
