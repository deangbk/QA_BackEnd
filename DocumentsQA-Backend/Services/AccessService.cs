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
		public int GetProjectID();
		public int GetUserID();
		public AppRole? UserGetRole();
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
		private readonly ILogger<AccessService> _logger;

		private readonly IHttpContextAccessor _httpContextAccessor;

		private int? _projectId;
		private int? _userId;

		public AccessService(
			ILogger<AccessService> logger,
			IHttpContextAccessor httpContextAccessor)
		{
			_logger = logger;

			_httpContextAccessor = httpContextAccessor;
		}

		// -----------------------------------------------------

		private static int _ParseIntID(string? idClaim) {
			try {
				return int.Parse(idClaim!);		// Ignore null so ArgumentNullException would return -1
			}
			catch (Exception) {
				return -1;
			}
		}
		public int GetProjectID() {
			if (_projectId == null) {
				HttpContext ctx = _httpContextAccessor.HttpContext!;

				var claims = ctx.User;
				var idClaim = claims.FindFirst("proj")?.Value;

				_projectId =  _ParseIntID(idClaim);
			}
			return _projectId.Value;
		}
		public int GetUserID() {
			if (_userId == null) {
				HttpContext ctx = _httpContextAccessor.HttpContext!;

				var claims = ctx.User;
				var idClaim = claims.FindFirst("id")?.Value;

				_userId = _ParseIntID(idClaim);
			}
			return _userId.Value;
		}

		public AppRole? UserGetRole() {
			if (!IsValidUser()) return null;

			HttpContext ctx = _httpContextAccessor.HttpContext!;
			var claims = ctx.User;

			if (claims.IsInRole(AppRole.Admin.Name))
				return AppRole.Admin;
			else if (claims.IsInRole(AppRole.Manager.Name))
				return AppRole.Manager;
			return AppRole.User;
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
			return AllowToProject(id => ProjectHelpers.CanUserAccessProject(project, id));
			//return AllowToProject(id => GetProjectID() == project.Id);
		}
		public bool AllowToProject(Func<int, bool> userAllowPolicy) {
			if (!IsValidUser())
				return false;

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
			return AllowToTranche(id => tranche.UserAccesses.Any(x => x.Id == id));
		}
		public bool AllowToTranche(Func<int, bool> userAllowPolicy) {
			return AllowToProject(userAllowPolicy);
		}

		public bool AllowManageProject(Project project) {
			// Use the normal AllowToProject, but always refuse normal users
			return AllowToProject(_ => false);
		}
		public bool AllowManageTranche(Tranche tranche) {
			return AllowManageProject(tranche.Project);
		}
	}

	public class AccessAllowAll : IAccessService {
		public int GetProjectID() => -1;
		public int GetUserID() => -1;
		public AppRole UserGetRole() => AppRole.Admin;
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
