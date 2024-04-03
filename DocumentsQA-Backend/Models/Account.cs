using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Account {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int ProjectId { get; set; }			// FK to Project
		public virtual Project Project { get; set; } = null!;

		public int TrancheId { get; set; }			// FK to Tranche
		public virtual Tranche Tranche { get; set; } = null!;

		public int AccountNo { get; set; }
		[MaxLength(256)]
		public string? AccountName { get; set; }

		public virtual List<Document> Documents { get; set; } = new();      // One-to-many with Document

		// NOTE: Also available as dbfunc "ufnGetAccountIdentifierName"
		// Get the format "A_001", "C_032", "D_999" and the such
		public string GetIdentifierName() => string.Format("{0}_{1:D3}", Tranche.Name, AccountNo);
	}
}
