using System;
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
		public static List<Tranche> GetUserTrancheAccessesInProject(AppUser user, int pid) {
			return user.TrancheAccesses
				.Where(x => x.ProjectId == pid)
				.ToList();
		}
	}
}
