using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	// Extends the AspNetUsers table
	public class EDUser : IdentityUser {
		public List<Project> ProjectAccesses { get; set; } = new();		// Reference navigation for many-to-many FK
		
		public int? FavouriteProjectId { get; set; }		// User's "favourite" proj, aka the proj the user get directed to when logging in
		public Project? FavouriteProject { get; set; }      // Reference navigation to FK

		public DateTime DateCreated { get; set; }
	}
}
