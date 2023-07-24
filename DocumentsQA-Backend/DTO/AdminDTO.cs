using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class CreateProjectDTO {
		[BindProperty(Name = "name")]
		public string Name { get; set; } = null!;

		[BindProperty(Name = "company")]
		public string Company { get; set; } = null!;

		[BindProperty(Name = "d.start")]
		public DateTime DateStart { get; set; }

		[BindProperty(Name = "d.end")]
		public DateTime DateEnd { get; set; }
	}
}
