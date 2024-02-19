using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	// Required properties must be nullable otherwise they'll be default-initialized if missing when used with FromBody

	public class CreateAccountDTO {
		public string Tranche { get; set; } = null!;

		[Required] public int? Number { get; set; }
		public string Name { get; set; } = null!;
	}

	public class EditAccountDTO {
		public string? Tranche { get; set; }

		public int? Number { get; set; }
		public string? Name { get; set; }
	}
}
