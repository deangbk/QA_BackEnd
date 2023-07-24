using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DocumentsQA_Backend.Models {
	[PrimaryKey(nameof(Id))]
	public class Account {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int TrancheId { get; set; }					// FK to Tranche
		public Tranche Tranche { get; set; } = null!;		// Reference navigation to FK

		public int AccountNo { get; set; }
		public string? AccountName { get; set; }
	}
}
