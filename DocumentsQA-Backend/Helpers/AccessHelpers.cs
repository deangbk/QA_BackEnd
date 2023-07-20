using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using System.Security.Claims;
using System.Security.Cryptography;

namespace DocumentsQA_Backend.Helpers {
	public static class AccessHelpers {
		public static async Task<bool> AllowProject(DataContext dataContext, HttpContext httpContext, int pid) {
			Project? project = await Queries.GetProjectFromId(dataContext, pid);
			if (project == null)
				return false;
			return AllowProject(httpContext, project);
		}
		public static bool AllowProject(HttpContext httpContext, Project project) {
			var claimsPrincipal = httpContext.User;

			var userIdClaim = claimsPrincipal.FindFirst("id")?.Value;
			var userRoleClaim = claimsPrincipal.FindFirst("role")?.Value;

			// Admins can access everything
			if (userRoleClaim == "admin")
				return true;

			int userId = 0;
			try {
				userId = int.Parse(userIdClaim!);	// Ignore null so ArgumentNullException would return false
			}
			catch (Exception) {
				return false;
			}

			return project.UserAccesses.Exists(x => x.Id == userId);
		}
	}
}
