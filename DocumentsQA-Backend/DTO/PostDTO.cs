using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DocumentsQA_Backend.DTO {
	public class PostGetDTO {
		[BindProperty(Name = "cnt.page")]
		public int PostsPerPage { get; set; } = 16;

		[BindProperty(Name = "page")]
		public int Page { get; set; }

		[BindProperty(Name = "details")]
		public int DetailsLevel { get; set; } = 0;
	}

	public class PostGetFilterDTO {
		[BindProperty(Name = "search")]
		public string? SearchTerm { get; set; }

		[BindProperty(Name = "tranche")]
		public string? Tranche { get; set; }

		[BindProperty(Name = "t.id")]
		public int? TicketID { get; set; }

		[BindProperty(Name = "u.id")]
		public int? PosterID { get; set; }

		[BindProperty(Name = "d.from")]
		public DateTime? PostedFrom { get; set; }
		[BindProperty(Name = "d.to")]
		public DateTime? PostedTo { get; set; }

		[BindProperty(Name = "answer")]
		public bool? OnlyAnswered { get; set; }
	}
}
