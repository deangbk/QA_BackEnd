using System;
using System.Collections.Generic;
using System.Linq;

using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Helpers {
	// Just in case
	// https://learn.microsoft.com/en-us/windows-server/identity/ad-fs/technical-reference/the-role-of-claims
	public static class ClaimTypeURI {
		public static readonly string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
		public static readonly string Name = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
		public static readonly string Role = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role";
	}
}
