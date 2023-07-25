using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Account {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int TrancheId { get; set; }			// FK to Tranche
		public virtual Tranche Tranche { get; set; } = null!;

		public int AccountNo { get; set; }
		[MaxLength(256)]
		public string? AccountName { get; set; }
	}
}
