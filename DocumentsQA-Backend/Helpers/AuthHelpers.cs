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

namespace DocumentsQA_Backend.Helpers {
	public static class AuthHelpers {
		public static string ComposeUsername(int projectId, string name) {
			return $"{projectId}:{name}";
		}
		public static (int, string) DecomposeUsername(string name) {
			int sep = name.IndexOf(':');
			if (sep == -1) {
				return (-1, "");
			}

			int project = -1;
			try {
				project = int.Parse(name[..sep]);
			}
			catch {}

			return (project, name[(sep + 1)..]);
		}

		/// <summary>
		/// Throws an AccessForbiddenException if the requested details level is higher than the maximum allowed.
		/// <para>Does nothing if the user has manager access.</para>
		/// </summary>
		public static void GuardDetailsLevel(IAccessService access, Project project, int details, int maxDetails) {
			if (details >= maxDetails) {
				if (!access.AllowManageProject(project))
					throw new AccessForbiddenException($"Insufficient credentials to get details level {details}");
			}
		}

		public static string GeneratePassword(Random rand, int length) {
			const string passwordChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			
			var pass = string.Concat(Enumerable.Range(0, length)
				.Select(x => passwordChars[rand.Next() % passwordChars.Length]));

			// Must contain at least 1 digit
			if (!pass.Any(x => Char.IsDigit(x))) {
				pass = pass.ReplaceAt(0, '0');
			}

			return pass;
		}


	}
}
