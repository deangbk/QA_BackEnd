using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocumentsQA_Backend.Helpers {
	public static class HttpHelpers {
		public static void AddApplicationError(this HttpResponse response, string message) {
			response.Headers.Add("Application-Error", message);
			response.Headers.Add("Access-Control-Expose-Headers", "Application-Error");
			response.Headers.Add("Access-Control-Allow-Orign", "*");
		}

		public static FileStreamResult StringToFileStreamResult(string data, string mimeType) {
			byte[] byteArray = Encoding.UTF8.GetBytes(data);
			var stream = new MemoryStream(byteArray);
			return new FileStreamResult(stream, mimeType);
		}
	}
}
