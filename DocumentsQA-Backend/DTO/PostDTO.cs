using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.DTO {
	// Required properties must be nullable otherwise they'll be default-initialized if missing when used with FromBody

	public class PaginateDTO {
		[JsonPropertyName("per_page")]
		[Range(1, int.MaxValue)]
		public int CountPerPage { get; set; } = 16;

		[JsonPropertyName("page")]
		[Range(0, int.MaxValue)]
		[Required] public int? Page { get; set; }
	}

	public class PostGetFilterDTO {
		[JsonPropertyName("search")]
		public string? SearchTerm { get; set; }

		public string? Tranche { get; set; }

		public int? Account { get; set; }

		[JsonPropertyName("id")]
		public int? TicketID { get; set; }

		[JsonPropertyName("post_by")]
		public int? PosterID { get; set; }

		[JsonPropertyName("date_from")]
		public DateTime? PostedFrom { get; set; }
		[JsonPropertyName("date_to")]
		public DateTime? PostedTo { get; set; }

		[JsonPropertyName("has_answer")]
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
		/// <item>account: gets only account questions, gets only from a specific account if Account is not null</item>
		/// </list>
		/// </summary>
		public string? Type { get; set; }

		public string? Category { get; set; }
	}
	public class PostGetFilterAndPaginateDTO {
		public PostGetFilterDTO Filter { get; set; } = null!;
		public PaginateDTO? Paginate { get; set; }
	}

	public class PostCreateDTO {
		[JsonPropertyName("account")]
		public int? AccountId { get; set; }

		public string Text { get; set; } = null!;
		public string? Category { get; set; }

		[JsonPropertyName("post_as")]
		public int? PostAs { get; set; }
		public bool? Approve { get; set; }

		[JsonPropertyName("date_sent")]
		public DateTime? DateSent { get; set; }
	}

	public class PostEditDTO {
		[Required] public int? Id { get; set; }

        [JsonPropertyName("q_text")]
		public string? Question { get; set; }
        [JsonPropertyName("a_text")]
		public string? Answer { get; set; }
        
		public string? Category { get; set; }
    }

	/// <summary>
	/// needed for the edit question page in front edit, the other edit dtos don't cover all the properties needed
	/// </summary>
    public class PostEditQuestionDTO
    {

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("q_text")]
        public string? Question { get; set; } = null!;
        [JsonPropertyName("a_text")]
        public string? Answer { get; set; } = null!;
        [JsonPropertyName("category")]
        public string? Category { get; set; } = null!;

        [JsonPropertyName("type")]
        public string? questionType { get; set; } = null!;

    }
	public class PostSetAnswerDTO {
		[Required] public int? Id { get; set; }
		public string Answer { get; set; } = null!;
	}

	public class PostSetApproveDTO {
		[Required] public bool? Approve { get; set; }

		public List<int> Questions { get; set; } = new();
	}

	public class PostAddCommentDTO {
		public string Text { get; set; } = null!;
	}

	// -----------------------------------------------------

	public class Unauth_PostCreateDTO : PostCreateDTO {
		[Required] public int? ProjectID { get; set; }

		public string Email { get; set; } = null!;
	}
}
