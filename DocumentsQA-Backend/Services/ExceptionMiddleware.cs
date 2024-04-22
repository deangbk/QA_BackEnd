using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Services {
	using JsonTable = Dictionary<string, object>;

	public interface IFormattableException {
		JsonTable GetFormattedResponse();
	}

	public class AccessUnauthorizedException : Exception, IFormattableException {
		public AccessUnauthorizedException() : base("Unauthorized access, please add a valid credentials token.") { }
		public AccessUnauthorizedException(string message) : base(message) { }
		public AccessUnauthorizedException(string message, Exception inner) 
			: base(message, inner) { }

		public JsonTable GetFormattedResponse() {
			return new() {
				["status"] = HttpStatusCode.Unauthorized,
				["title"] = "Unauthorized",
				["errors"] = new List<string> { Message },
			};
		}
	}
	public class AccessForbiddenException : Exception, IFormattableException {
		public AccessForbiddenException() : base("Insufficient credentials for action.") { }
		public AccessForbiddenException(string message) : base(message) { }
		public AccessForbiddenException(string message, Exception inner)
			: base(message, inner) { }

		public JsonTable GetFormattedResponse() {
			return new() {
				["status"] = HttpStatusCode.Forbidden,
				["title"] = "Forbidden",
				["errors"] = new List<string> { Message },
			};
		}
	}
	public class InvalidModelStateException : Exception, IFormattableException {
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
				["status"] = HttpStatusCode.BadRequest,
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
				await _next.Invoke(context);
			}
			catch (Exception e) {
				context.Response.ContentType = "text/plain";

				HttpStatusCode code = HttpStatusCode.InternalServerError;
				switch (e) {
					case AccessUnauthorizedException _:
						code = HttpStatusCode.Unauthorized;		break;
					case AccessForbiddenException _:
						code = HttpStatusCode.Forbidden;		break;
					case InvalidModelStateException _:
						code = HttpStatusCode.BadRequest;		break;
				}
				context.Response.StatusCode = (int)code;

			if (e is IFormattableException ece) {
					var resp = ece.GetFormattedResponse();
					//resp["status"] = (int)code;

					await context.Response.WriteAsync(JsonSerializer.Serialize(resp));
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
