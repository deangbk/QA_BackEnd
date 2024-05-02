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
	public class AuthHelpers {
		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly UserManager<AppUser> _userManager;

		public AuthHelpers(
			DataContext dataContext, IAccessService access, 
			UserManager<AppUser> userManager)
		{
			_dataContext = dataContext;
			_access = access;

			_userManager = userManager;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Throws an AccessForbiddenException if the requested details level is higher than the maximum allowed.
		/// <para>Does nothing if the user has manager access.</para>
		/// </summary>
		public void GuardDetailsLevel(int details, int maxDetails) {
			if (details >= maxDetails && !_access.IsSuperUser()) {
				throw new AccessForbiddenException($"Insufficient credentials to get details level {details}");
			}
		}

		public static string GeneratePassword(Random rand, int length, bool requireDigit = true) {
			const string passwordChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			
			var pass = string.Concat(Enumerable.Range(0, length)
				.Select(x => passwordChars[rand.Next() % passwordChars.Length]));

			if (requireDigit) {
				// Must contain at least 1 digit
				if (!pass.Any(x => Char.IsDigit(x))) {
					pass = pass.ReplaceAt(0, '0');
				}
			}

			return pass;
		}

		/// <summary>
		/// Returns true if the current user is able to "manage" the specified user
		/// </summary>
		public async Task<bool> CanManageUser(int target) {
			AppUser? user = await Queries.GetUserFromId(_dataContext, target);
			if (user == null)
				return false;

			return await CanManageUser(user);
		}
		/// <summary>
		/// Returns true if the current user is able to "manage" the specified user
		/// </summary>
		public async Task<bool> CanManageUser(AppUser target) {
			var selfRole = _access.UserGetRole() ?? AppRole.Empty;
			var targetRole = (await UserHelpers.GetHighestRole(_userManager, target))
				?? AppRole.Empty;

			// Self must have higher privilege than target user
			if (selfRole.CompareTo(targetRole) <= 0)
				return false;

			var targetProject = target.Project;
			if (targetProject == null)
				return false;
			
			return _access.AllowManageProject(targetProject);
		}
	}
}
