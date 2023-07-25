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

		public virtual List<AppUser> UserAccesses { get; set; } = new();
		public virtual List<AppUser> UserManagers { get; set; } = new();
	}

	[PrimaryKey(nameof(Id))]
	public class Tranche {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int ProjectId { get; set; }		// FK to Project
		public virtual Project Project { get; set; } = null!;

		[MaxLength(16)]
		public string Name { get; set; } = null!;

		public virtual List<AppUser> UserAccesses { get; set; } = new();
	}

	// Entity join class for Project <-> User
	[Keyless]
	[Table("UserProjectAccesses")]
	public class ProjectUserAccess {
		public int ProjectId { get; set; }		// FK to Project
		public int UserId { get; set; }			// FK to User

		public virtual Project Project { get; set; } = null!;
		public virtual AppUser User { get; set; } = null!;
	}

	// Entity join class for Project <-> User
	[Keyless]
	[Table("UserTrancheAccesses")]
	public class TrancheUserAccess {
		public int TrancheId { get; set; }		// FK to Tranche
		public int UserId { get; set; }			// FK to User

		public virtual Tranche Tranche { get; set; } = null!;
		public virtual AppUser User { get; set; } = null!;
	}

	// Entity join class for Project <-> User
	[Keyless]
	[Table("UserProjectManages")]
	public class ProjectUserManages {
		public int ProjectId { get; set; }		// FK to Project
		public int UserId { get; set; }			// FK to User

		public virtual Project Project { get; set; } = null!;
		public virtual AppUser User { get; set; } = null!;
	}
}
