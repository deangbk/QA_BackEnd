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

namespace DocumentsQA_Backend.Helpers {
	public static class AuthHelpers {
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
	}
}
