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
		public virtual List<Tranche> TrancheAccesses { get; set; } = new();		// Reference navigation for many-to-many FK
		
		/*
		public int? FavouriteProjectId { get; set; }		// User's "favourite" proj, aka the proj the user gets directed to when logging in
		public virtual Project? FavouriteProject { get; set; }
		*/

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
		private AppRole(string name) { Name = name; }

		// -----------------------------------------------------

		public static readonly AppRole User = new("user");
		public static readonly AppRole Manager = new("manager");
		public static readonly AppRole Admin = new("admin");
		public static readonly AppRole Empty = new();

		public bool IsStaff() => this.Equals(Manager) || this.Equals(Admin);

		public static AppRole FromString(string name) => name switch {
			"user" => User,
			"manager" => Manager,
			"admin" => Admin,
			_ => Empty,
		};
		public override string ToString() => this.Name;
		public override bool Equals(object? obj) {
			if (obj is AppRole role)
				return this.Name.Equals(role.Name);
			return false;
		}
		public override int GetHashCode() => this.Name.GetHashCode();

		// -----------------------------------------------------

		public static async Task AddRoleToUser(UserManager<AppUser> userManager, AppUser user, AppRole role) {
			await userManager.AddClaimAsync(user, new Claim("role", role.Name));
			await userManager.AddToRoleAsync(user, role.Name);
		}
	}
}
