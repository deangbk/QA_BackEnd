using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.DTO {
	// Explicit [Required] used here because we're manually validating them in UploadDocument
	public class DocumentUploadDTO {
		[Required]
		public string Type { get; set; } = null!;

		[JsonPropertyName("with_post")]
		public int? AssocQuestion { get; set; }

		[JsonPropertyName("with_account")]
		public int? AssocAccount { get; set; }

		[Required]
		public string Name { get; set; } = null!;
		public string? Description { get; set; }

		[Url]
		public string? Url { get; set; }

		public bool? Hidden { get; set; } = false;
		public bool? Printable { get; set; } = false;
	}
	public class DocumentUploadWithFileDTO {
		public List<IFormFile> Files { get; set; } = null!;

		[BindProperty(Name = "descs")]
		public string DescsJson { get; set; } = null!;
	}

	public class DocumentEditDTO {
		[Required]
		public int? Id { get; set; }

		public string? Name { get; set; }
		public string? Description { get; set; }

		[Url]
		public string? Url { get; set; }

		public bool? Hidden { get; set; }
		public bool? Printable { get; set; }
	}

	public class DocumentFilterDTO {
		[JsonPropertyName("search")]
		public string? SearchTerm { get; set; }

		public string? Category { get; set; }

		[JsonPropertyName("upload_by")]
		public int? UploaderID { get; set; }

		[JsonPropertyName("date_from")]
		public DateTime? PostedFrom { get; set; }
		[JsonPropertyName("date_to")]
		public DateTime? PostedTo { get; set; }

		[JsonPropertyName("printable")]
		public bool? AllowPrint { get; set; }

		[JsonPropertyName("in_post")]
		public int? AssocQuestion { get; set; }

		[JsonPropertyName("in_tranche")]
		public string? AssocTranche { get; set; }

		[JsonPropertyName("in_account")]
		public int? AssocAccount { get; set; }
	}
    public class fileUploadDTO
    {
        public int QuestionID { get; set; }
        public string? upType{ get; set; }
		public string? Account { get; set; }
        public int AccountId { get; set; }
    }

    public class DocumentGetDTO {
		public DocumentFilterDTO? Filter { get; set; }
		public PaginateDTO? Paginate { get; set; }
	}
}
