using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace DocumentsQA_Backend.DTO {
	public class AuthResponse {
		public string Token { get; set; } = string.Empty;
		public DateTime Expiration { get; set; }
	}

	/*
	public class UserCreateDTO {
		[EmailAddress]
		public string Email { get; set; } = null!;

		public string Password { get; set; } = null!;

		public string DisplayName { get; set; } = null!;
		public string Company { get; set; } = null!;
	}
	*/

	public class LoginDTO {
		public string Email { get; set; } = null!;

		public string Password { get; set; } = null!;
	}

	public class PasswordChangeDTO {
		public string Old { get; set; } = null!;
		public string New { get; set; } = null!;
	}
	public class PasswordResetDTO {
		public string? Project { get; set; }
		public string Email { get; set; } = null!;
	}
	public class PasswordResetTokenDTO {
		public string Token { get; set; } = null!;
		public string Password { get; set; } = null!;
	}
}
