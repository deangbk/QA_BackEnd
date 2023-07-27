using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class CreateProjectDTO {
		public string Name { get; set; } = null!;

		public string Company { get; set; } = null!;

		public DateTime DateStart { get; set; }

		public DateTime DateEnd { get; set; }

		[BindProperty(Name = "Tranches")]
		public string InitialTranches { get; set; } = null!;
	}
}
