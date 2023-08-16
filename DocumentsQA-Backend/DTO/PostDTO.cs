using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DocumentsQA_Backend.DTO {
	public class PaginateDTO {
		[BindProperty(Name = "Count")]
		public int CountPerPage { get; set; } = 16;

		[BindProperty(Name = "Page")]
		public int Page { get; set; }
	}

	public class PostGetFilterDTO {
		[BindProperty(Name = "Search")]
		public string? SearchTerm { get; set; }

		[BindProperty(Name = "Tranche")]
		public string? Tranche { get; set; }

		[BindProperty(Name = "Ticket")]
		public int? TicketID { get; set; }

		[BindProperty(Name = "Poster")]
		public int? PosterID { get; set; }

		[BindProperty(Name = "DateFrom")]
		public DateTime? PostedFrom { get; set; }
		[BindProperty(Name = "DateTo")]
		public DateTime? PostedTo { get; set; }

		[BindProperty(Name = "Answered")]
		public bool? OnlyAnswered { get; set; }
	}
}
