// (c) 2018 by Tom van Dijkhuizen. All rights reserved.

// .NET namespaces
using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

// Typedefs
using i32 = System.Int32;

namespace dmake {
	public static class Util {
		public static String Escape(String str) => WebUtility.UrlEncode(str);
		public static String Unescape(String str) => WebUtility.UrlDecode(str);

		// No default values specified -> use empty Dictionary<String,String>.
		public static Dictionary<String,String> ParseDictionary(String str) => Util.ParseDictionary(str,new Dictionary<String,String>());
		// Format: key=value;key=value; etc.
		// This function does not unescape keys or values; leading or trailing spaces are trimmed and should therefore be escaped.
		public static Dictionary<String,String> ParseDictionary(String str,Dictionary<String,String> DefaultVaues) {
			var ret = new Dictionary<String,String>();
			// Individual key-value pairs separated by the '=' sign.
			String[] parts = str.Split(new String[] { ";" },StringSplitOptions.RemoveEmptyEntries);

			for(i32 i = 0; i < parts.Length; i++) {
				String current = parts[i];
				String[] pair = current.Split('=');
				if(pair.Length == 1) {
					// If the format is wrong, keys or values may be invalid, so skip the current pair.
					Console.WriteLine("Warning. Invalid format for dictionary entry: {0}",current);
					Console.WriteLine("Format should be: key=value");
					continue;
				} else if(pair.Length > 2) pair[1] += Util.FixValue(pair,2,'='); // This chould happen if the value includes an '=' sign.
				
				// Leading or trailing spaces should be escaped!
				String key = pair[0].Trim();
				String val = pair[1].Trim();
				Console.WriteLine("{0}={1}",key,val);
				ret.Add(key,val);
			} // for

			// Add default values iff they don't exist yet in the return value.
			foreach(String i in DefaultVaues.Keys) {
				if(ret.ContainsKey(i)) continue;
				ret[i] = DefaultVaues[i];
			} // foreach

			return ret;
		}

		// Utility function for Util.ParseDictionary. Repairs the RHS of a key-value pair if it includes an '=' sign.
		private static String FixValue(String[] values,i32 start,char delim) {
			var sb = new StringBuilder();
			i32 N = values.Length;
			for(i32 i = start; i < N; i++) sb.AppendFormat("{0}{1}",delim,values[i]);
			return sb.ToString();
		}

		public static String SerialiseDictionary(Dictionary<String,String> dic) {
			var sb = new StringBuilder();
			foreach(String i in dic.Keys) sb.AppendFormat("{0}={1};",i,Util.Escape(dic[i]));
			// FIXME: remove?
			sb.Append(";");
			return sb.ToString();
		}
	}
}
