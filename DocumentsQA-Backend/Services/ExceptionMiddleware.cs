using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.ExceptionServices;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using DocumentsQA_Backend.Extensions;
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend.Services {
	using JsonTable = Dictionary<string, object>;

	public interface IFormattableException {
		JsonTable GetFormattedResponse();
	}

	public class BadRequestException : Exception, IFormattableException {
		public BadRequestException() : base("Bad request") { }
		public BadRequestException(string message) : base(message) { }
		public BadRequestException(string message, Exception inner)
			: base(message, inner) { }

		public JsonTable GetFormattedResponse() {
			return new() {
				["status"] = HttpStatusCode.BadRequest,
				["title"] = "Bad Request",
				["errors"] = new List<string> { Message },
			};
		}
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
	public class CustomCodeException : Exception, IFormattableException {
		public HttpStatusCode Code { get; set; } = HttpStatusCode.InternalServerError;

		public CustomCodeException() : base("Unknown error") { }
		public CustomCodeException(string message) : base(message) { }
		public CustomCodeException(HttpStatusCode code, string message) 
			: base(message)
		{
			Code = code;
		}
		public CustomCodeException(HttpStatusCode code, string message, Exception inner)
			: base(message, inner)
		{
			Code = code;
		}

		public JsonTable GetFormattedResponse() {
			return new() {
				["status"] = Code,
				["title"] = "Error",
				["errors"] = new List<string> { Message },
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
				await HandleException(context, e);
			}
		}

		public static async Task HandleException(HttpContext context, Exception? e) {
			if (e is null)
				return;

			var code = e switch {
				BadRequestException				=> HttpStatusCode.BadRequest,
				AccessUnauthorizedException		=> HttpStatusCode.Unauthorized,
				AccessForbiddenException		=> HttpStatusCode.Forbidden,
				InvalidModelStateException		=> HttpStatusCode.BadRequest,
				CustomCodeException cce			=> cce.Code,
				_ => HttpStatusCode.InternalServerError,
			};
#if DEBUG
			if (code == HttpStatusCode.InternalServerError) {
				ExceptionDispatchInfo.Capture(e.InnerException!).Throw();
			}
#endif

			if (e is IFormattableException ece) {
				var resp = ece.GetFormattedResponse();
				//resp["status"] = (int)code;

				context.Response.ContentType = "application/json";
				await context.Response.SetHttpResponseError((int)code, JsonSerializer.Serialize(resp));
			}
			else {
				context.Response.ContentType = "text/plain";
				await context.Response.SetHttpResponseError((int)code, e.Message);
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
