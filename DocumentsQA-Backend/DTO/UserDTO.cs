using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class EditUserDTO {

		[JsonPropertyName("display_name")]
		public string? DisplayName { get; set; }

		public List<string>? Tranches { get; set; }
	}
}
