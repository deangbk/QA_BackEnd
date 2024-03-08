using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class AddUserDTO {
		public string Email { get; set; } = null!;
		public string Name { get; set; } = null!;
		public string? Company { get; set; }
		public List<string>? Tranches { get; set; }
	}
}
