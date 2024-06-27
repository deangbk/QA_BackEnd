using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	// Required properties must be nullable otherwise they'll be default-initialized if missing when used with FromBody

	public class CreateProjectDTO {
		[MaxLength(256)]
		[MinLength(4)]
		[RegularExpression(@"[a-zA-Z0-9_-]+")]
		public string Name { get; set; } = null!;

		[MaxLength(256)]
		[JsonPropertyName("display_name")]
		public string DisplayName { get; set; } = null!;

		[MaxLength(256)]
		public string Company { get; set; } = null!;

		[JsonPropertyName("date_start")]
		[Required] public DateTime? DateStart { get; set; }

		[JsonPropertyName("date_end")]
		[Required] public DateTime? DateEnd { get; set; }

		[JsonPropertyName("tranches")]
		public List<string> InitialTranches { get; set; } = new();

		public List<AddUserDTO>? Users { get; set; }
	}
	

	public class EditProjectDTO {
		[MaxLength(256)]
		[MinLength(4)]
		[RegularExpression(@"[a-zA-Z0-9_-]+")]
		public string? Name { get; set; }

		[JsonPropertyName("display_name")] 
		public string? DisplayName { get; set; }

		public string? Description { get; set; }

		public string? Company { get; set; }

		[JsonPropertyName("date_start")]
		public DateTime? DateStart { get; set; }
		[JsonPropertyName("date_end")]
		public DateTime? DateEnd { get; set; }
	}

	public class AddNoteDTO {
		public string Text { get; set; } = null!;

		public string? Description { get; set; }

		public string? Category { get; set; }

		public bool? Sticky { get; set; } = false;
	}
}
