using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.DTO {
	public class PaginateDTO {
		[BindProperty(Name = "per.page")]
		public int CountPerPage { get; set; } = 16;

		[BindProperty(Name = "page")]
		public int Page { get; set; }
	}

	public class PostGetFilterDTO {
		[BindProperty(Name = "search")]
		public string? SearchTerm { get; set; }

		public string? Tranche { get; set; }

		public int? Account { get; set; }

		public int? TicketID { get; set; }

		[BindProperty(Name = "post.by")]
		public int? PosterID { get; set; }

		[BindProperty(Name = "date.from")]
		public DateTime? PostedFrom { get; set; }
		[BindProperty(Name = "date.to")]
		public DateTime? PostedTo { get; set; }

		[BindProperty(Name = "has.answer")]
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

	public class PostCreateMultipleDTO {
		public List<PostCreateDTO> Posts { get; set; } = null!;
	}
	public class PostEditMultipleDTO {
		public class Inner {
			public int Id { get; set; }
			public string Text { get; set; } = null!;
			public QuestionCategory? Category { get; set; } = null;
		}

		public List<Inner> Posts { get; set; } = null!;
	}

	public class PostSetAnswerMultipleDTO {
		public class Inner {
			public int Id { get; set; }
			public string Answer { get; set; } = null!;
		}

		public List<Inner> Answers { get; set; } = null!;
	}
}
