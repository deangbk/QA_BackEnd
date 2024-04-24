using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class CreateTrancheDTO {
		[MaxLength(16)]
		public string Name { get; set; } = null!;
	}

	public class EditTrancheDTO {
		[MaxLength(16)]
		public string? Name { get; set; }
	}
}
