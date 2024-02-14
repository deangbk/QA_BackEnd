using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DocumentsQA_Backend.Helpers {
	public static class ValueHelpers {
		public static IEnumerable<int> SplitIntString(string str) {
			return str.Split(',')
				.Select(x => int.Parse(x.Trim()));
		}
	}
}
