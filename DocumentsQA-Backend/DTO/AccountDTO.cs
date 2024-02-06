using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class CreateAccountDTO {
		public string Tranche { get; set; } = null!;

		public int Number { get; set; }
		public string Name { get; set; } = null!;
	}

	public class EditAccountDTO {
		public string? Tranche { get; set; }

		public int? Number { get; set; }
		public string? Name { get; set; }
	}
}
