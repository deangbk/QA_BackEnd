using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Data;

namespace DocumentsQA_Backend.Models {
	// Extends the AspNetUsers table, also changes the ID to be an int
	public class AppUser : IdentityUser<int> {
		public List<Project> ProjectAccesses { get; set; } = new();		// Reference navigation for many-to-many FK
		
		public int? FavouriteProjectId { get; set; }		// User's "favourite" proj, aka the proj the user gets directed to when logging in
		public Project? FavouriteProject { get; set; }		// Reference navigation to FK

		public string DisplayName { get; set; } = null!;
		public DateTime DateCreated { get; set; }

		public List<Document> Documents { get; set; } = new();
	}

	// -----------------------------------------------------
	// Override the default ASP.NET stuff to use int IDs instead

	// https://stackoverflow.com/a/35521154

	public class AppRole : IdentityRole<int> {
		public AppRole() { }
		public AppRole(string name) { Name = name; }
	}
}
