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
	public class ProjectAccessMiddleware {

		// List of controllers where a valid auth token is not required to access
		private static readonly string[] AllowUnauthRoutes = new[] {
			"UserAuth",
			"Unauthorised",
		};

		// -----------------------------------------------------

		private readonly RequestDelegate _next;

		public ProjectAccessMiddleware(RequestDelegate next) {
			_next = next;
		}

		// DataContext and IAccessService are scoped services, and cannot be injected into the middleware ctor
		public async Task Invoke(HttpContext context, DataContext dataContext, IAccessService access) {
			var controllerValue = context.GetRouteValue("controller");

			if (controllerValue != null) {
				string controller = controllerValue
					.ToString()!
					.ToLower();

				if (!AllowUnauthRoutes.Any(x => x.ToLower() == controller)) {
					Project? project = await Queries.GetProjectFromId(dataContext, access.GetProjectID());

					if (project == null) {
						context.Response.ContentType = "text/plain";
						context.Response.StatusCode = (int)HttpStatusCode.NotFound;

						await context.Response.WriteAsync("Project not found.");
						return;
					}

					if (!access.IsValidUser()) {
						context.Response.ContentType = "text/plain";
						context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

						await context.Response.WriteAsync("Invalid login token.");
						return;
					}
					if (!access.AllowToProject(project)) {
						context.Response.ContentType = "text/plain";
						context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

						await context.Response.WriteAsync("No permission to access project. "
							+ "Contact project staff if you think this is an error.");
						return;
					}
				}
			}

			// Everything OK, pass request to the next processor
			await _next.Invoke(context);
		}
	}
}
