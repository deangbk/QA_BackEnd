using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DocumentsQA_Backend.DTO {
	public class AuthResponse {
		public string Token { get; set; } = string.Empty;
		public DateTime Expiration { get; set; }
	}

	public class UserCredentials {
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		public string Password { get; set; } = string.Empty;

		public string DisplayName { get; set; } = string.Empty;
		public string Company { get; set; } = string.Empty;
	}
}
