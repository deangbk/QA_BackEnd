using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Controllers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Services {
	public interface IAccessService {
		public Task<bool> AllowToProject(HttpContext ctx, Project project);
		public Task<bool> AllowToTranche(HttpContext ctx, Tranche tranche);
	}

	public class AccessService : IAccessService {
		private readonly DataContext _dataContext;
		private readonly ILogger<AccessService> _logger;
		private readonly SignInManager<AppUser> _signinManager;

		public AccessService(DataContext dataContext, ILogger<AccessService> logger,
			SignInManager<AppUser> signinManager) {

			_dataContext = dataContext;
			_logger = logger;
			_signinManager = signinManager;
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

		public Task<bool> AllowToProject(HttpContext ctx, Project project) {
			var claimsPrincipal = ctx.User;

			var userIdClaim = claimsPrincipal.FindFirst("id")?.Value;
			var userRoleClaim = claimsPrincipal.FindFirst("role")?.Value;

			// Admins can access everything
			if (userRoleClaim == AppRole.Admin)
				return Task.FromResult(true);

			int userId = _ParseUserID(userIdClaim);

			// Allow project managers
			if (userRoleClaim == AppRole.Manager) {
				bool allowManager = project.UserManagers.Any(x => x.Id == userId);
				if (allowManager)
					return Task.FromResult(true);
			}

			// Allow normal users with access
			bool allowProject = ProjectHelpers.CanUserAccessProject(project, userId);
			return Task.FromResult(allowProject);
		}
		public Task<bool> AllowToTranche(HttpContext ctx, Tranche tranche) {
			var claimsPrincipal = ctx.User;

			var userIdClaim = claimsPrincipal.FindFirst("id")?.Value;
			var userRoleClaim = claimsPrincipal.FindFirst("role")?.Value;

			// Admins can access everything
			if (userRoleClaim == AppRole.Admin)
				return Task.FromResult(true);

			int userId = _ParseUserID(userIdClaim);

			// Managers of a project can view any of the project's tranches
			if (userRoleClaim == AppRole.Manager) {
				bool allowManager = tranche.Project.UserManagers.Any(x => x.Id == userId);
				if (allowManager)
					return Task.FromResult(true);
			}

			// Allow normal users with access
			bool allowProject = tranche.UserAccesses.Any(x => x.Id == userId);
			return Task.FromResult(allowProject);
		}

		public Task<bool> AllowManageProject(HttpContext ctx, Project project) {
			var claimsPrincipal = ctx.User;

			var userIdClaim = claimsPrincipal.FindFirst("id")?.Value;
			var userRoleClaim = claimsPrincipal.FindFirst("role")?.Value;

			// Admins can access everything
			if (userRoleClaim == AppRole.Admin)
				return Task.FromResult(true);

			int userId = _ParseUserID(userIdClaim);

			// Allow project managers
			if (userRoleClaim == AppRole.Manager) {
				bool allowManager = project.UserManagers.Any(x => x.Id == userId);
				if (allowManager)
					return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}
		public Task<bool> AllowManageTranche(HttpContext ctx, Tranche tranche) {
			var allow = AllowManageProject(ctx, tranche.Project);
			return allow;
		}
	}
}
