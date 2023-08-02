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

		public int GetUserID(HttpContext ctx) {
			var claimsPrincipal = ctx.User;
			var idClaim = claimsPrincipal.FindFirst("id")?.Value;
			return _ParseUserID(idClaim);
		}
		public AppRole GetUserRole(HttpContext ctx) {
			var claimsPrincipal = ctx.User;
			var roleClaim = claimsPrincipal.FindFirst("role")?.Value;
			return AppRole.FromString(roleClaim ?? "");
		}

		public Task<bool> AllowToProject(HttpContext ctx, Project project) {
			return AllowToProject(ctx, project, 
				id => ProjectHelpers.CanUserAccessProject(project, id));
		}
		public Task<bool> AllowToProject(HttpContext ctx, Project project, Func<int, bool> userAllowPolicy) {
			var userId = GetUserID(ctx);
			var userRole = GetUserRole(ctx);

			// Admins can access everything
			if (userRole == AppRole.Admin)
				return Task.FromResult(true);

			// Allow project managers
			if (userRole == AppRole.Manager) {
				bool allowManager = project.UserManagers.Any(x => x.Id == userId);
				if (allowManager)
					return Task.FromResult(true);
			}

			// Allow normal users
			if (userRole == AppRole.User)
				return Task.FromResult(userAllowPolicy(userId));

			// Unknown role, refuse
			return Task.FromResult(false);
		}

		public Task<bool> AllowToTranche(HttpContext ctx, Tranche tranche) {
			return AllowToTranche(ctx, tranche,
				id => tranche.UserAccesses.Any(x => x.Id == id));
		}
		public Task<bool> AllowToTranche(HttpContext ctx, Tranche tranche, Func<int, bool> userAllowPolicy) {
			var userId = GetUserID(ctx);
			var userRole = GetUserRole(ctx);

			// Admins can access everything
			if (userRole == AppRole.Admin)
				return Task.FromResult(true);

			// Allow project managers
			if (userRole == AppRole.Manager) {
				bool allowManager = tranche.Project.UserManagers.Any(x => x.Id == userId);
				if (allowManager)
					return Task.FromResult(true);
			}

			// Allow normal users
			if (userRole == AppRole.User)
				return Task.FromResult(userAllowPolicy(userId));

			// Unknown role, refuse
			return Task.FromResult(false);
		}

		public Task<bool> AllowManageProject(HttpContext ctx, Project project) {
			// Use the normal AllowToProject, but always refuse normal users
			return AllowToProject(ctx, project, _ => false);
		}
	}
}
