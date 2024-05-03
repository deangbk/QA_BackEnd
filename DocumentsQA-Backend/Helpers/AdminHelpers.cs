using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

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
	public class AdminHelpers {
		private readonly DataContext _dataContext;

		private readonly UserManager<AppUser> _userManager;

		public AdminHelpers(
			DataContext dataContext,
			UserManager<AppUser> userManager)
		{
			_dataContext = dataContext;

			_userManager = userManager;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Grants a role to a user
		/// </summary>
		public async Task GrantUserRole(AppUser user, AppRole role) {
			var roleExists = await _userManager.IsInRoleAsync(user, role.Name);
			if (!roleExists) {
				await _userManager.AddClaimAsync(user, new Claim("role", role.Name));
				await _userManager.AddToRoleAsync(user, role.Name);
			}
		}
		/// <summary>
		/// Grants a role to a user
		/// </summary>
		public async Task RemoveUserRole(AppUser user, AppRole role) {
			var claims = await _userManager.GetClaimsAsync(user);
			var roleClaims = claims
				.Where(x => x.ValueType == "role")
				.Where(x => x.Value == role.Name);

			await _userManager.RemoveClaimsAsync(user, roleClaims);
			await _userManager.RemoveFromRoleAsync(user, role.Name);
		}

		/// <summary>
		/// Grants tranche read access to users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public async Task GrantUsersTrancheAccess(Tranche tranche, List<int> userIds) {
			var mapUsers = await Queries.GetUsersMapFromIds(_dataContext, userIds);

			GrantUsersTrancheAccess(tranche, mapUsers.Values.ToList());
		}
		/// <summary>
		/// Grants tranche read access to users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public void GrantUsersTrancheAccess(Tranche tranche, List<AppUser> users) {
			// Remove users who can already access the tranche
			var adding = users.Where(
				x => tranche.UserAccesses.Find(y => x.Id == y.Id) == null);

			// Add to tranche users
			tranche.UserAccesses.AddRange(adding);
		}

		/// <summary>
		/// Removes tranche read access from users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public void RemoveUsersTrancheAccess(Tranche tranche, List<int> userIds) {
			tranche.UserAccesses.RemoveAll(x => userIds.Any(y => y == x.Id));
		}

		/// <summary>
		/// Clears all tranche read access from users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public void ClearUsersTrancheAccess(Project project, List<int> userIds) {
			foreach (var tranche in project.Tranches)
				RemoveUsersTrancheAccess(tranche, userIds);
		}

		/// <summary>
		/// Removes project manager rights from users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public void RemoveProjectManagers(Project project, List<int> userIds) {
			project.UserManagers.RemoveAll(x => userIds.Any(y => y == x.Id));
		}

		/// <summary>
		/// Makes users the managers of a project
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public async Task MakeProjectManagers(Project project, List<int> userIds) {
			var mapUsers = await Queries.GetUsersMapFromIds(_dataContext, userIds);

			await MakeProjectManagers(project, mapUsers.Values.ToList());
		}
		/// <summary>
		/// Makes users the managers of a project
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public async Task MakeProjectManagers(Project project, List<AppUser> users) {
			// Make the users managers if they're not already one
			foreach (var user in users) {
				await GrantUserRole(user, AppRole.Manager);
			}

			// Grant elevated access (manager) to all tranches in the project
			{
				// Remove existing managers
				var adding = users.Where(
					x => project.UserManagers.Find(y => x.Id == y.Id) == null);

				// Add to project managers
				project.UserManagers.AddRange(adding);
			}
		}
	}
}
