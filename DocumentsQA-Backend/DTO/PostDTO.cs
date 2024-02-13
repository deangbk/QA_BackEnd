using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.DTO {
	public class PaginateDTO {
		[JsonPropertyName("per.page")]
		public int CountPerPage { get; set; } = 16;

		[JsonPropertyName("page")]
		public int Page { get; set; }
	}

	public class PostGetFilterDTO {
		[JsonPropertyName("search")]
		public string? SearchTerm { get; set; }

		public string? Tranche { get; set; }

		public int? Account { get; set; }

		public int? TicketID { get; set; }

		[JsonPropertyName("post.by")]
		public int? PosterID { get; set; }

		[JsonPropertyName("date.from")]
		public DateTime? PostedFrom { get; set; }
		[JsonPropertyName("date.to")]
		public DateTime? PostedTo { get; set; }

		[JsonPropertyName("has.answer")]
		public bool? OnlyAnswered { get; set; }

		/// <summary>
		/// <list type="bullet">
		/// <item>null:  gets everything</item>
		/// <item>true:  gets only approved</item>
		/// <item>false: gets only unapproved</item>
		/// </list>
		/// </summary>
		public bool? Approved { get; set; }

		/// <summary>
		/// <list type="bullet">
		/// <item>null:    don't care, gets everything</item>
		/// <item>general: gets only general questions</item>
		/// <item>account: gets only questions tied to a specific account, Account must then not be null</item>
		/// </list>
		/// </summary>
		public string? Type { get; set; }

		public string? Category { get; set; }
	}

	public class PostCreateDTO {
		[JsonPropertyName("account")]
		public int? AccountId { get; set; }
		public string Text { get; set; } = null!;
		public string? Category { get; set; }
	}

	public class PostEditDTO {
		public string Text { get; set; } = null!;
		public string? Category { get; set; }
	}
	public class PostSetAnswerDTO {
		public string Answer { get; set; } = null!;
	}

	public class PostSetApproveDTO {
		public bool Approve { get; set; }
		public List<int> Questions { get; set; } = new();
	}

	public class PostEditMultipleDTO {
		public int Id { get; set; }
		public string Text { get; set; } = null!;
		public string? Category { get; set; }
	}

	public class PostSetAnswerMultipleDTO {
		public int Id { get; set; }
		public string Answer { get; set; } = null!;
	}

	public class PostAddCommentDTO {
		public string Text { get; set; } = null!;
	}

	// -----------------------------------------------------

	public class Unauth_PostCreateDTO : PostCreateDTO {
		[JsonPropertyName("user.id")]
		public int ProjectID { get; set; }

		public string Email { get; set; } = null!;
	}
}
