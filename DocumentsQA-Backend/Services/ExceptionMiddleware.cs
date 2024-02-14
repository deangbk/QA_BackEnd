using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Newtonsoft.Json;

using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Services {
	using JsonTable = Dictionary<string, object>;

	public interface IControllerException {
		JsonTable GetFormattedResponse();
	}

	public class AccessUnauthorizedException : Exception, IControllerException {
		public AccessUnauthorizedException() : base("Unauthorized") { }
		public AccessUnauthorizedException(string message) : base(message) { }
		public AccessUnauthorizedException(string message, Exception inner) 
			: base(message, inner) { }

		public JsonTable GetFormattedResponse() {
			return new() {
				["title"] = Message,
				["errors"] = new List<string> { "Unauthorized" },
			};
		}
	}
	public class InvalidModelStateException : Exception, IControllerException {
		public ModelStateDictionary ModelState { get; set; }

		public InvalidModelStateException(ModelStateDictionary model) 
			: base("One or more validation errors occurred.")
		{
			ModelState = model;
		}

		public JsonTable GetFormattedResponse() {
			var errorTables = new JsonTable();
			foreach (var error in ModelState.GetErrors()) {
				errorTables[error.Key] = error.Errors;
			}

			return new() {
				["title"] = Message,
				["errors"] = errorTables,
			};
		}
	}

	public class ExceptionMiddleware {
		private readonly RequestDelegate _next;

		public ExceptionMiddleware(RequestDelegate next) {
			_next = next;
		}

		public async Task Invoke(HttpContext context) {
			try {
				await _next(context);
			}
			catch (Exception e) {
				context.Response.ContentType = "text/plain";

				HttpStatusCode code = HttpStatusCode.InternalServerError;
				switch (e) {
					case AccessUnauthorizedException _:
						code = HttpStatusCode.Unauthorized;		break;
					case InvalidModelStateException _:
						code = HttpStatusCode.BadRequest;		break;
				}
				context.Response.StatusCode = (int)code;

				if (e is IControllerException ece) {
					var resp = ece.GetFormattedResponse();
					resp["status"] = (int)code;

					await context.Response.WriteAsync(JsonConvert.SerializeObject(resp));
				}
				else {
					await context.Response.WriteAsync(e.Message);
				}
			}
		}
	}

	public class ModelValidationError {
		public string Key { get; set; } = null!;
		public List<string> Errors { get; set; } = new();

		public JsonTable ToTable() {
			return new() {
				[Key] = Errors,
			};
		}
	}
}
