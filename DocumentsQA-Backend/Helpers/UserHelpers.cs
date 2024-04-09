using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Extensions;
using DocumentsQA_Backend.Data;

namespace DocumentsQA_Backend.Helpers {
	public static class UserHelpers {
		public static async Task<AppRole?> GetHighestRole(UserManager<AppUser> userManager, AppUser user) {
			var roles = await userManager.GetRolesAsync(user);
			var highestRole = roles
				.Select(x => AppRole.FromString(x))
				.Max();
			return highestRole;
		}
		public static async Task<bool> HasRole(UserManager<AppUser> userManager, AppUser user, AppRole role) {
			var roles = await userManager.GetRolesAsync(user);
			return roles.FirstOrDefault(x => x == role.Name) != null;
		}
	}
}
