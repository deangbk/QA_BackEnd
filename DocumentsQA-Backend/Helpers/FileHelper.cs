using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using System.IO;

namespace DocumentsQA_Backend.Helpers {
	public static class FileHelpers {
		public static List<int> ReadIntListFromFile(Stream stream) {
			List<int> res = new();
			using (var reader = new StreamReader(stream, Encoding.ASCII)) {
				var lines = new List<string>();
				while (!reader.EndOfStream) {
					var line = reader.ReadLine();
					if (line != null && line.Length > 0)
						lines.Add(line);
				}

				foreach (var line in lines) {
					var data = line.Split(',').Select(x => int.Parse(x.Trim()));
					res.AddRange(data);
				}
			}
			return res;
		}
	}
}
