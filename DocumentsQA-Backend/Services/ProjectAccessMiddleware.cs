using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

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
	public class ProjectAccessRequirement : IAuthorizationRequirement {
		public bool Manager { get; set; }

		public ProjectAccessRequirement() => Manager = false;
		public ProjectAccessRequirement(bool manager) => Manager = manager;
	}
	public class ProjectAccessPolicyHandler : AuthorizationHandler<ProjectAccessRequirement> {
		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		public ProjectAccessPolicyHandler(DataContext dataContext, IAccessService access) {
			_dataContext = dataContext;
			_access = access;
		}

		protected override async Task HandleRequirementAsync(
			AuthorizationHandlerContext context, ProjectAccessRequirement requirement)
		{
			if (!_access.IsValidUser()) {
				context.Fail(new AuthorizationFailureReason(this, "Invalid credentials"));
				return;
			}

			Project? project = await Queries.GetProjectFromId(_dataContext, _access.GetProjectID());
			if (project == null) {
				context.Fail(new AuthorizationFailureReason(this, "Project not found"));
				return;
			}
			else {
				bool bAllow = requirement.Manager ?
					_access.AllowManageProject(project) :
					_access.AllowToProject(project);

				if (!bAllow) {
					context.Fail(new AuthorizationFailureReason(this, "No permission to access project"));
					return;
				}
			}

			context.Succeed(requirement);
		}
	}

	public class RoleRequirement : IAuthorizationRequirement {
		public AppRole Role { get; set; }

		public RoleRequirement(AppRole role) => Role = role;
	}
	public class RolePolicyHandler : AuthorizationHandler<RoleRequirement> {
		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		public RolePolicyHandler(DataContext dataContext, IAccessService access) {
			_dataContext = dataContext;
			_access = access;
		}

		protected override Task HandleRequirementAsync(
			AuthorizationHandlerContext context, RoleRequirement requirement)
		{
			var role = _access.UserGetRole();
			if (role != null && role.CompareTo(requirement.Role) >= 0) {
				context.Succeed(requirement);
			}
			else {
				context.Fail(new AuthorizationFailureReason(this, "Forbidden"));
			}
			return Task.CompletedTask;
		}
	}
}
