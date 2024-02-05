using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

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

		[BindProperty(Name = "Account")]
		public int? Account { get; set; }

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

		/// <summary>
		/// <list type="bullet">
		/// <item>null:  gets everything</item>
		/// <item>true:  gets only approved</item>
		/// <item>false: gets only unapproved</item>
		/// </list>
		/// </summary>
		public bool? Approved { get; set; } = null;

		/// <summary>
		/// <list type="bullet">
		/// <item>null:    don't care, gets everything</item>
		/// <item>general: gets only general questions</item>
		/// <item>account: gets only questions tied to a specific account, Account must then not be null</item>
		/// </list>
		/// </summary>
		public string? Type { get; set; } = null;

		public QuestionCategory? Category { get; set; } = null;
	}

	public class PostCreateDTO {
		[BindProperty(Name = "account")]
		public int? AccountId { get; set; } = null;
		public string Text { get; set; } = null!;
		public QuestionCategory? Category { get; set; } = null;
	}

	public class PostEditDTO {
		public string Text { get; set; } = null!;
		public QuestionCategory? Category { get; set; } = null;
	}
	public class PostSetAnswerDTO {
		public string Answer { get; set; } = null!;
	}

	public class PostSetApproveDTO {
		public bool Approve { get; set; }
		public List<int> Questions { get; set; } = new();
	}
}
