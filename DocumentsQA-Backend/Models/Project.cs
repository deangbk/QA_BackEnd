using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Project {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public string Name { get; set; } = null!;
		public string? DisplayName { get; set; }

		public string CompanyName { get; set; } = null!;

		public string? Description { get; set; }

		public string? LogoUrl { get; set; }
		public string? BannerUrl { get; set; }

		public List<Tranche> Tranches { get; set; } = null!;

		public DateTime ProjectStartDate { get; set; }
		public DateTime ProjectEndDate { get; set; }

		public List<AppUser> UserAccesses { get; set; } = new();     // Reference navigation for many-to-many FK
	}

	[Keyless]
	public class ProjectUserAccess {
		public int ProjectId { get; set; }		// FK to Project
		public int UserId { get; set; }			// FK to User
	}

	[PrimaryKey(nameof(Id))]
	public class Tranche {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int ProjectId { get; set; }		// FK to Project

		[MaxLength(16)]
		public string Name { get; set; } = null!;
	}
}
