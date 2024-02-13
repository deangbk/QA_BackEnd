using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class AddNoteDTO {
		public string Text { get; set; } = null!;

		public string? Description { get; set; }

		public string? Category { get; set; }

		public bool? Sticky { get; set; } = false;
	}
}
