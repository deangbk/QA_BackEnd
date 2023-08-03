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
		public Task<bool> AllowToProject(Project project);
		public Task<bool> AllowToTranche(Tranche tranche);
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
		public bool UserHasElevatedAccess() {
			HttpContext ctx = _httpContextAccessor.HttpContext!;
			var claims = ctx.User;
			return claims.IsInRole(AppRole.Admin.Name) 
				|| claims.IsInRole(AppRole.Manager.Name);
		}

		public Task<bool> AllowToProject(Project project) {
			return AllowToProject(project, 
				id => ProjectHelpers.CanUserAccessProject(project, id));
		}
		public Task<bool> AllowToProject(Project project, Func<int, bool> userAllowPolicy) {
			var userId = GetUserID();

			// Admins can access everything
			if (UserHasRole(AppRole.Admin))
				return Task.FromResult(true);

			// Allow project managers
			if (UserHasRole(AppRole.Manager)) {
				bool allowManager = project.UserManagers.Any(x => x.Id == userId);
				if (allowManager)
					return Task.FromResult(true);
			}

			// Allow normal users
			if (UserHasRole(AppRole.User))
				return Task.FromResult(userAllowPolicy(userId));

			// Unknown role, refuse
			return Task.FromResult(false);
		}

		public Task<bool> AllowToTranche(Tranche tranche) {
			return AllowToTranche(tranche,
				id => tranche.UserAccesses.Any(x => x.Id == id));
		}
		public Task<bool> AllowToTranche(Tranche tranche, Func<int, bool> userAllowPolicy) {
			return AllowToProject(tranche.Project, userAllowPolicy);
		}

		public Task<bool> AllowManageProject(Project project) {
			// Use the normal AllowToProject, but always refuse normal users
			return AllowToProject(project, _ => false);
		}
	}
}
