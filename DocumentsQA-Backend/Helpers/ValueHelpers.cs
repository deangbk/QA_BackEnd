using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DocumentsQA_Backend.Helpers {
	public static class ValueHelpers {
		public static List<object> TupleToList(ITuple tuple) {
			if (tuple == null)
				throw new ArgumentNullException(nameof(tuple));

			var result = new List<object>(tuple.Length);
			for (int i = 0; i < tuple.Length; i++) {
				result.Add(tuple[i]!);
			}
			return result;
		}

		public static IEnumerable<int> SplitIntString(string str) {
			return str.Split(',')
				.Select(x => int.Parse(x.Trim()));
		}

		public static int AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dict, params (TKey, TValue)[] values)
			where TKey : notnull {

			foreach (var (k, v) in values) {
				dict.Add(k, v);
			}
			return dict.Count;
		}
	}
}
