﻿using System.ComponentModel.DataAnnotations;
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

		[MaxLength(256)]
		public string Name { get; set; } = null!;
		[MaxLength(256)]
		public string? DisplayName { get; set; }

		[MaxLength(256)]
		public string CompanyName { get; set; } = null!;

		public string? Description { get; set; }

		[Url]
		[MaxLength(256)]
		public string? LogoUrl { get; set; }
		[Url]
		[MaxLength(256)]
		public string? BannerUrl { get; set; }

		public DateTime ProjectStartDate { get; set; }
		public DateTime ProjectEndDate { get; set; }

		public virtual List<Tranche> Tranches { get; set; } = new();			// One-to-many with Tranche
		public virtual List<Question> Questions { get; set; } = new();			// One-to-many with Question

		public virtual List<AppUser> UserAccesses { get; set; } = new();		// Reference navigation for many-to-many FK
		public virtual List<AppUser> UserManagers { get; set; } = new();		// Reference navigation for many-to-many FK
	}

	[PrimaryKey(nameof(Id))]
	public class Tranche {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int ProjectId { get; set; }		// FK to Project
		public virtual Project Project { get; set; } = null!;

		[MaxLength(16)]
		public string Name { get; set; } = null!;

		public virtual List<AppUser> UserAccesses { get; set; } = new();		// Reference navigation for many-to-many FK
	}

	// Multipurpose entity join class
	public class EJoinClass {
		public int Id1 { get; set; }
		public int Id2 { get; set; }
	}
}
