using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Helpers {
	// Just in case
	// https://learn.microsoft.com/en-us/windows-server/identity/ad-fs/technical-reference/the-role-of-claims
	public static class ClaimTypeURI {
		public static readonly string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
		public static readonly string Name = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
		public static readonly string Role = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role";
	}

	// -----------------------------------------------------
	// https://stackoverflow.com/a/77744171

	public static class ControllerExt {
		public static void ValidateModel(this ControllerBase controller, ILogger logger) {
			if (!controller.ModelState.IsValid) {
				logger.LogInvalidModelState(controller.ModelState);
				throw new InvalidModelStateException(controller.ModelState);
			}
		}
	}

	public static class LoggerExt {
		/// <summary>
		/// Logs information about an invalid model state.
		/// </summary>
		/// <param name="source">The logger.</param>
		/// <param name="modelState">The model state.</param>
		public static void LogInvalidModelState(this ILogger source, ModelStateDictionary modelState) {
			var modelStateEntries = new List<ModelStateEntry>();
			var errorMessages = new List<string>();

			Visit(modelState.Root, modelStateEntries);

			foreach (var modelStateEntry in modelStateEntries) {
				foreach (var error in modelStateEntry.Errors)
					errorMessages.Add(error.ErrorMessage);
			}

			source.LogError("Invalid model state: {ErrorMessages}", errorMessages);
		}

		/// <summary>
		/// Adds all non-container nodes of an <see cref="ModelStateEntry"/> to the provided collection.
		/// </summary>
		/// <param name="modelStateEntry">The current model state entry.</param>
		/// <param name="modelStateEntries">A collection of model state entries.</param>
		private static void Visit(ModelStateEntry modelStateEntry, ICollection<ModelStateEntry> modelStateEntries) {
			if (modelStateEntry.Children != null) {
				foreach (var child in modelStateEntry.Children)
					Visit(child, modelStateEntries);
			}

			if (!modelStateEntry.IsContainerNode)
				modelStateEntries.Add(modelStateEntry);
		}
	}
}
