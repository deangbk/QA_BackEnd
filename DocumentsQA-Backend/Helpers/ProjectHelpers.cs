﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;

namespace DocumentsQA_Backend.Helpers {
	public static class ProjectHelpers {
		public static bool CanUserAccessProject(Project project, AppUser user) {
			return CanUserAccessProject(project, user.Id);
		}
		public static bool CanUserAccessProject(Project project, int userID) {
			// User has project access if they have access to any of the project's tranches
			var tranches = project.Tranches;
			return tranches.Any(x => x.UserAccesses.Any(y => y.Id == userID));
		}

		public static List<Tranche> GetUserTrancheAccessesInProject(AppUser user, int pid) {
			return user.TrancheAccesses
				.Where(x => x.ProjectId == pid)
				.ToList();
		}
	}
}
