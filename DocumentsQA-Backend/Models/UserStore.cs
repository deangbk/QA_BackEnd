using System.Data;
using System.Security.Claims;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Data;

namespace DocumentsQA_Backend.Models {
	// Override the default ASP.NET stuff to use int IDs instead

	// https://stackoverflow.com/a/35521154

	public class AppRole : IdentityRole<int>, IComparable {
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

		public int Rank {
			get => ToString() switch {
				"admin" => 999999,
				"manager" => 10,
				"user" => 1,
				_ => -1,
			};
		}

		public override int GetHashCode() => this.Name.GetHashCode();

		public override bool Equals(object? obj) {
			if (obj is AppRole role)
				return this.Name.Equals(role.Name);
			return false;
		}
		public int CompareTo(object? obj) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			if (obj is AppRole other) {
				int selfLevel = this.Rank;
				int otherLevel = other.Rank;

				return selfLevel.CompareTo(otherLevel);
			}
			else {
				throw new ArgumentException("obj is not AppRole", nameof(obj));
			}
		}
	}
}
