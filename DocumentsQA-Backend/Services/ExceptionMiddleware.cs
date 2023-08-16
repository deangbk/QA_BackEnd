﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using DocumentsQA_Backend.Helpers;
using Microsoft.AspNetCore.Diagnostics;

namespace DocumentsQA_Backend.Services {
	public class AccessUnauthorizedException : Exception {
		public AccessUnauthorizedException() : base("Unauthorized") { }
		public AccessUnauthorizedException(string message) : base(message) { }
		public AccessUnauthorizedException(string message, Exception inner) : base(message, inner) { }
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
				}
				context.Response.StatusCode = (int)code;

				await context.Response.WriteAsync(e.Message);
			}
		}
	}
}
