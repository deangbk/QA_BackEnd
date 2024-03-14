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
	public static class AdminHelpers {
		/// <summary>
		/// Grants a role to a user
		/// /// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public static async Task GrantUserRole(UserManager<AppUser> userManager, AppUser user, AppRole role) {
			var roleExists = await userManager.IsInRoleAsync(user, role.Name);
			if (!roleExists) {
				await userManager.AddClaimAsync(user, new Claim("role", role.Name));
				await userManager.AddToRoleAsync(user, role.Name);
			}
		}
		/// <summary>
		/// Grants a role to a user
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public static async Task RemoveUserRole(UserManager<AppUser> userManager, AppUser user, AppRole role) {
			var claims = await userManager.GetClaimsAsync(user);
			var roleClaims = claims
				.Where(x => x.ValueType == "role")
				.Where(x => x.Value == role.Name);

			await userManager.RemoveClaimsAsync(user, roleClaims);
			await userManager.RemoveFromRoleAsync(user, role.Name);
		}

		/// <summary>
		/// Removes tranche read access from users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public static void RemoveUsersTrancheAccess(
			DataContext dataContext,
			int trancheId, List<int> userIds)
		{
			var dbSetTranche = dataContext.Set<EJoinClass>("TrancheUserAccess");

			dbSetTranche.RemoveRange(
				dbSetTranche
					.Where(x => x.Id1 == trancheId)
					.Where(x => userIds.Any(y => y == x.Id2)));
		}

		/// <summary>
		/// Clears all tranche read access from users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public static void ClearUsersTrancheAccess(
			DataContext dataContext, 
			List<int> userIds)
		{
			var dbSetTranche = dataContext.Set<EJoinClass>("TrancheUserAccess");

			dbSetTranche.RemoveRange(
				dbSetTranche
					.Where(x => userIds.Any(y => y == x.Id2)));
		}

		/// <summary>
		/// Removes project manager rights from users
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public static void RemoveProjectManagers(
			DataContext dataContext,
			int projectId, List<int> userIds)
		{
			var dbSetManager = dataContext.Set<EJoinClass>("ProjectUserManage");

			dbSetManager.RemoveRange(
				dbSetManager
					.Where(x => x.Id2 == projectId)
					.Where(x => userIds.Any(y => y == x.Id1)));
		}

		/// <summary>
		/// Makes users the managers of a project
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public static async Task MakeProjectManagers(
			DataContext dataContext, UserManager<AppUser> userManager, 
			Project project, List<int> userIds)
		{
			var mapUsers = (await Queries.GetUsersMapFromIds(dataContext, userIds))!;

			// Verify user IDs
			{
				var err = ValueHelpers.CheckInvalidIds(
					userIds, mapUsers.Keys, "User");
				if (err != null) {
					throw new ArgumentException(err);
				}
			}

			await MakeProjectManagers(dataContext, userManager, project, mapUsers.Keys.ToList());
		}
		/// <summary>
		/// Makes users the managers of a project
		/// <para>Does not call SaveChangesAsync</para>
		/// </summary>
		public static async Task MakeProjectManagers(
			DataContext dataContext, UserManager<AppUser> userManager, 
			Project project, List<AppUser> users)
		{
			var userIds = users.Select(x => x.Id).ToList();

			// Make the users managers if they're not already one
			foreach (var user in users) {
				await GrantUserRole(userManager, user, AppRole.Manager);
			}

			// Grant elevated access (manager) to all tranches in the project
			{
				var dbSetTranche = dataContext.Set<EJoinClass>("TrancheUserAccess");
				var dbSetManager = dataContext.Set<EJoinClass>("ProjectUserManage");

				/*
				// Remove existing accesses for users
				dbSetTranche.RemoveRange(
					dbSetTranche
						.Where(x => userIds.Any(y => y == x.Id2)));
				*/

				// Remove existing manager rights for users
				dbSetManager.RemoveRange(
					dbSetManager
						.Where(x => userIds.Any(y => y == x.Id1)));

				/*
				// Add access for each tranche in project
				{
					var adds = project.Tranches.SelectMany(x => userIds, (t, u) => new EJoinClass {
						Id1 = t.Id,
						Id2 = u,
					});
					dbSetTranche.AddRange(adds);
				}
				*/

				// Add to project managers
				{
					var adds = userIds.Select(u => new EJoinClass {
						Id1 = u,
						Id2 = project.Id,
					});
					dbSetManager.AddRange(adds);
				}
			}
		}
	}
}
