using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

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

		// -----------------------------------------------------

		public static ObjectResult ResultWithMessage(this ControllerBase controller, int code, string message) {
			return new ObjectResult(message) { StatusCode = code };
		}

		public static ObjectResult ForbidWithMessage(this ControllerBase controller, string message) {
			return controller.ResultWithMessage((int)HttpStatusCode.Forbidden, message);
		}
		public static ObjectResult ForbidStaff(this ControllerBase controller, string role = "Manager") {
			throw new AccessForbiddenException($"{role} access required");
		}
		public static ObjectResult ForbidWithMessage(this ControllerBase controller) {
			/*
			var e = new AccessForbiddenException();
			var resp = e.GetFormattedResponse();
			return controller.ResultWithMessage((int)HttpStatusCode.Forbidden, JsonSerializer.Serialize(resp));
			*/
			throw new AccessForbiddenException();
		}
	}
}
