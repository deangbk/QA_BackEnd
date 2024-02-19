using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	// Required properties must be nullable otherwise they'll be default-initialized if missing when used with FromBody

	public class CreateProjectDTO {
		public string Name { get; set; } = null!;

		public string Company { get; set; } = null!;

		[JsonPropertyName("date_start")]
		[Required] public DateTime? DateStart { get; set; }

		[JsonPropertyName("date_end")]
		[Required] public DateTime? DateEnd { get; set; }

		[JsonPropertyName("tranches")]
		public string InitialTranches { get; set; } = null!;
	}

	public class AddNoteDTO {
		public string Text { get; set; } = null!;

		public string? Description { get; set; }

		public string? Category { get; set; }

		public bool? Sticky { get; set; } = false;
	}
}
