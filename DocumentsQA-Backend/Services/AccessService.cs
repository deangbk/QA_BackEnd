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
		public int GetUserID();
		public bool UserHasRole(AppRole role);

		public bool IsValidUser();
		public bool IsNormalUser();
		public bool IsSuperUser();
		public bool IsAdmin();

		public bool AllowToProject(Project project);
		public bool AllowToTranche(Tranche tranche);
		public bool AllowManageProject(Project project);
		public bool AllowManageTranche(Tranche tranche);
	}

	public class AccessService : IAccessService {
		private readonly DataContext _dataContext;
		private readonly ILogger<AccessService> _logger;
		private readonly SignInManager<AppUser> _signinManager;

		private readonly IHttpContextAccessor _httpContextAccessor;

		public AccessService(DataContext dataContext, ILogger<AccessService> logger,
			SignInManager<AppUser> signinManager, IHttpContextAccessor httpContextAccessor) {

			_dataContext = dataContext;
			_logger = logger;
			_signinManager = signinManager;

			_httpContextAccessor = httpContextAccessor;
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

		public int GetUserID() {
			HttpContext ctx = _httpContextAccessor.HttpContext!;
			var claims = ctx.User;
			var idClaim = claims.FindFirst("id")?.Value;
			return _ParseUserID(idClaim);
		}
		public bool UserHasRole(AppRole role) {
			HttpContext ctx = _httpContextAccessor.HttpContext!;
			var claims = ctx.User;
			return claims.IsInRole(role.Name);
		}

		public bool IsValidUser() => GetUserID() >= 0;
		public bool IsNormalUser() => IsValidUser() && !IsSuperUser(); 
		public bool IsSuperUser() => UserHasRole(AppRole.Admin) || UserHasRole(AppRole.Manager);
		public bool IsAdmin() => UserHasRole(AppRole.Admin);

		public bool AllowToProject(Project project) {
			return AllowToProject(project, 
				id => ProjectHelpers.CanUserAccessProject(project, id));
		}
		public bool AllowToProject(Project project, Func<int, bool> userAllowPolicy) {
			var userId = GetUserID();

			// Admins can access everything
			if (UserHasRole(AppRole.Admin))
				return true;

			// Allow project managers
			if (UserHasRole(AppRole.Manager)) {
				// Un comment later once we have the system for assigning managers to projects

				//bool allowManager = project.UserManagers.Any(x => x.Id == userId);
				bool allowManager = true;
				if (allowManager)
					return true;
			}

			// Allow normal users
			if (UserHasRole(AppRole.User))
				return userAllowPolicy(userId);

			// Unknown role, refuse
			return false;
		}

		public bool AllowToTranche(Tranche tranche) {
			return AllowToTranche(tranche,
				id => tranche.UserAccesses.Any(x => x.Id == id));
		}
		public bool AllowToTranche(Tranche tranche, Func<int, bool> userAllowPolicy) {
			return AllowToProject(tranche.Project, userAllowPolicy);
		}

		public bool AllowManageProject(Project project) {
			// Use the normal AllowToProject, but always refuse normal users
			return AllowToProject(project, _ => false);
		}
		public bool AllowManageTranche(Tranche tranche) {
			return AllowManageProject(tranche.Project);
		}
	}

	public class AccessAllowAll : IAccessService {
		public int GetUserID() => -1;
		public bool UserHasRole(AppRole role) => true;

		public bool IsValidUser() => true;
		public bool IsNormalUser() => true;
		public bool IsSuperUser() => true;
		public bool IsAdmin() => true;

		public bool AllowToProject(Project project) => true;
		public bool AllowToTranche(Tranche tranche) => true;
		public bool AllowManageProject(Project project) => true;
		public bool AllowManageTranche(Tranche tranche) => true;
	}

	public class AuthorizationAllowAnonymous : IAuthorizationHandler {
		public Task HandleAsync(AuthorizationHandlerContext context) {
			foreach (var requirement in context.PendingRequirements.ToList())
				context.Succeed(requirement);
			return Task.CompletedTask;
		}
	}
}
