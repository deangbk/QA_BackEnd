using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DocumentsQA_Backend.Services {
	public class ModelValidationActionFilter : IActionFilter {
		public void OnActionExecuting(ActionExecutingContext context) {
			if (!context.ModelState.IsValid) {
				//throw new InvalidModelStateException(context.ModelState);
			}
		}

		public void OnActionExecuted(ActionExecutedContext context) {
		}
	}
}
