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
		[MaxLength(256)]
		public string? DisplayName { get; set; }

		[MaxLength(256)]
		public string CompanyName { get; set; } = null!;

		public string? Description { get; set; }

		[Url]
		public string? LogoUrl { get; set; }
		[Url]
		public string? BannerUrl { get; set; }

		public DateTime ProjectStartDate { get; set; }
		public DateTime ProjectEndDate { get; set; }

		public List<Tranche> Tranches { get; set; } = null!;			// One-to-many with Tranche
		public List<Question> Questions { get; set; } = null!;			// One-to-many with Tranche

		public List<AppUser> UserAccesses { get; set; } = new();
		public List<AppUser> UserManagers { get; set; } = new();
	}

	[PrimaryKey(nameof(Id))]
	public class Tranche {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int ProjectId { get; set; }					// FK to Project
		public Project Project { get; set; } = null!;		// Reference navigation to FK

		[MaxLength(16)]
		public string Name { get; set; } = null!;

		public List<AppUser> UserAccesses { get; set; } = new();
	}

	// Entity join class for Project <-> User
	[Keyless]
	[Table("UserProjectAccesses")]
	public class ProjectUserAccess {
		public int ProjectId { get; set; }		// FK to Project
		public int UserId { get; set; }			// FK to User

		public Project Project { get; set; } = null!;		// Reference navigation to FK
		public AppUser User { get; set; } = null!;			// Reference navigation to FK
	}

	// Entity join class for Project <-> User
	[Keyless]
	[Table("UserTrancheAccesses")]
	public class TrancheUserAccess {
		public int TrancheId { get; set; }		// FK to Tranche
		public int UserId { get; set; }			// FK to User

		public Tranche Tranche { get; set; } = null!;		// Reference navigation to FK
		public AppUser User { get; set; } = null!;			// Reference navigation to FK
	}

	// Entity join class for Project <-> User
	[Keyless]
	[Table("UserProjectManages")]
	public class ProjectUserManages {
		public int ProjectId { get; set; }		// FK to Project
		public int UserId { get; set; }			// FK to User

		public Project Project { get; set; } = null!;		// Reference navigation to FK
		public AppUser User { get; set; } = null!;			// Reference navigation to FK
	}
}
