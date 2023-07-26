using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class CreateProjectDTO {
		public string Name { get; set; } = null!;

		public string Company { get; set; } = null!;

		[BindProperty(Name = "DateStart")]
		public DateTime DateStart { get; set; }

		[BindProperty(Name = "DateEnd")]
		public DateTime DateEnd { get; set; }
	}
}
