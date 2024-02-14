using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Mvc.ModelBinding;

using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Extensions {
	public static class ModelExtension {
		public static List<ModelValidationError> GetErrors(this ModelStateDictionary modelState) {
			var errors = new List<ModelValidationError>();

			foreach (var key in modelState.Keys) {
				var stateValue = modelState[key]!;

				if (stateValue.Errors.Count > 0) {
					errors.Add(new ModelValidationError {
						Key = key,
						Errors = stateValue.Errors.Select(x => x.ErrorMessage).ToList(),
					});
				}
			}

			return errors;
		}
	}
}
