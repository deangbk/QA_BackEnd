using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.ModelBinding;

using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Extensions {
	public static class LoggerExtension {
		/// <summary>
		/// Logs information about an invalid model state.
		/// </summary>
		/// <param name="source">The logger.</param>
		/// <param name="modelState">The model state.</param>
		public static void LogInvalidModelState(this ILogger source, ModelStateDictionary modelState) {
			var errorMessages = modelState.GetErrors();

			if (errorMessages.Count > 0) {
				string msg = "Invalid model state: " + errorMessages.ToStringEx();
				source.LogError(msg);
			}
		}
	}
}
