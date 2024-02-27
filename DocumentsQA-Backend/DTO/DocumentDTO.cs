using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DocumentsQA_Backend.DTO {
	public class DocumentUploadDTO {
		[Url]
		public string Url { get; set; } = null!;

		public string? Description { get; set; }

		public bool? Hidden { get; set; } = false;
		public bool? Printable { get; set; } = false;
	}

	public class DocumentEditDTO {
		public string? Description { get; set; }

		[Url]
		public string? Url { get; set; }

		public bool? Hidden { get; set; }
		public bool? Printable { get; set; }
	}
}
