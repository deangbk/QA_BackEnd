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
		public virtual List<Tranche> TrancheAccesses { get; set; } = new();		// Reference navigation for many-to-many FK
		
		public int? FavouriteProjectId { get; set; }		// User's "favourite" proj, aka the proj the user gets directed to when logging in
		public virtual Project? FavouriteProject { get; set; }

		[MaxLength(256)]
		public string Company { get; set; } = null!;

		[MaxLength(256)]
		public string DisplayName { get; set; } = null!;
		public DateTime DateCreated { get; set; }
	}

	// -----------------------------------------------------
	// Override the default ASP.NET stuff to use int IDs instead

	// https://stackoverflow.com/a/35521154

	public class AppRole : IdentityRole<int> {
		public AppRole() { }
		public AppRole(string name) { Name = name; }

		// -----------------------------------------------------

		public static readonly string User = "user";
		public static readonly string Manager = "manager";
		public static readonly string Admin = "admin";
	}
}
