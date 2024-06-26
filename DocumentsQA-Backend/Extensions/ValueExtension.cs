﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DocumentsQA_Backend.Extensions {
	public static class ValueExtension {
		public static List<object> ToList(this ITuple tuple) {
			if (tuple == null)
				throw new ArgumentNullException(nameof(tuple));

			var result = new List<object>(tuple.Length);
			for (int i = 0; i < tuple.Length; i++) {
				result.Add(tuple[i]!);
			}
			return result;
		}

		public static int AddRange<TKey, TValue>(
			this Dictionary<TKey, TValue> dict, 
			params (TKey, TValue)[] values) where TKey : notnull
		{
			foreach (var (k, v) in values) {
				dict.Add(k, v);
			}
			return dict.Count;
		}

		public static string ToStringEx<T>(this IEnumerable<T> e,
			string delim = ", ", string before = "[", string after = "]")
		{
			StringBuilder sb = new();

			sb.Append(before);
			sb.AppendJoin(delim, e);
			sb.Append(after);

			return sb.ToString();
		}
	}

	public static class StringExtension {
		public static string Truncate(this string str, int maxLength) {
			if (str == null) throw new ArgumentNullException(str);

			if (string.IsNullOrEmpty(str))
				return str;
			return str.Length < maxLength ? str : str.Substring(0, maxLength);
		}

		public static Stream ToStream(this string str) {
			if (str == null) throw new ArgumentNullException(str);

			MemoryStream stream = new();
			using (var writer = new StreamWriter(stream)) {
				writer.Write(str);
			}
			stream.Position = 0;
			return stream;
		}

		public static List<string> SplitLines(this string str) {
			if (str == null) throw new ArgumentNullException(str);

			var lines = new List<string>();
			using (var sr = new StringReader(str)) {
				while (true) {
					var line = sr.ReadLine();

					if (line == null)
						break;
					else if (line.Length > 0)
						lines.Add(line);
				}
			}

			return lines;
		}

		public static string ReplaceAt(this string str, int i, char ch) {
			if (str == null) throw new ArgumentNullException(str);

			char[] chars = str.ToCharArray();
			chars[i] = ch;
			return new string(chars);
		}
	}
}
