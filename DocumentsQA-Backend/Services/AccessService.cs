using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;

namespace DocumentsQA_Backend.Services {
	public interface IAccessService {
		public bool AllowToProject(HttpContext ctx, Project project);
		public bool AllowToTranche(HttpContext ctx, Tranche tranche);
	}

	public class AccessService : IAccessService {
		private readonly ILogger<AccessService> _logger;

		public AccessService(ILogger<AccessService> logger) {
			_logger = logger;
		}

		// -----------------------------------------------------

		private static int _ParseUserID(string? idClaim) {
			try {
				return int.Parse(idClaim!);		// Ignore null so ArgumentNullException would return -1
			}
			catch (Exception) {
				return -1;
			}
		}

		public bool AllowToProject(HttpContext ctx, Project project) {
			var claimsPrincipal = ctx.User;

			var userIdClaim = claimsPrincipal.FindFirst("id")?.Value;
			var userRoleClaim = claimsPrincipal.FindFirst("role")?.Value;

			// Admins can access everything
			if (userRoleClaim == AppRole.Admin)
				return true;

			int userId = _ParseUserID(userIdClaim);

			bool allowProject = project.UserAccesses.Any(x => x.Id == userId);
			return allowProject;
		}

		public bool AllowToTranche(HttpContext ctx, Tranche tranche) {
			var claimsPrincipal = ctx.User;

			var userIdClaim = claimsPrincipal.FindFirst("id")?.Value;
			var userRoleClaim = claimsPrincipal.FindFirst("role")?.Value;

			// Admins can access everything
			if (userRoleClaim == AppRole.Admin)
				return true;

			int userId = _ParseUserID(userIdClaim);

			// Managers of a project can view any of the project's tranches
			if (userRoleClaim == AppRole.Manager) {
				bool allowManager = tranche.Project.UserManagers.Any(x => x.Id == userId);
				if (allowManager)
					return true;
			}

			bool allowProject = tranche.Project.UserAccesses.Any(x => x.Id == userId);
			return allowProject;
		}
	}
}
