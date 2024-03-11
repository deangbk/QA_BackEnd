using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;

using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Helpers {
	public static class ValueHelpers {
		public static IEnumerable<int> SplitIntString(string str) {
			return str.Split(',')
				.Select(x => int.Parse(x.Trim()));
		}

		public static string? CheckInvalidIds(IEnumerable<int> source, IEnumerable<int> mapped, string name) {
			if (source.Count() != mapped.Count()) {
				var invalidIds = source.Except(mapped).ToList();
				if (invalidIds.Count > 0) {
					return $"{name} not found: {invalidIds.ToStringEx()}";
				}
				else {
					return $"One or more duplicated {name} IDs";
				}
			}
			return null;
		}
	}
}
