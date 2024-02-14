using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Extensions {
	public static class ControllerExtension {
		public static void ValidateModel(this ControllerBase controller, ILogger logger) {
			if (!controller.ModelState.IsValid) {
				logger.LogInvalidModelState(controller.ModelState);
				throw new InvalidModelStateException(controller.ModelState);
			}
		}
	}
}
