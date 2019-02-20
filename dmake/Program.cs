// (c) 2018 by Tom van Dijkhuizen. All rights reserved.

//#define CONFIRM_EXIT

// .NET namespaces
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

// Other namespaces
using XOSS;
using XOSS.Types;

// Typedefs
using i32 = System.Int32;
using u64 = System.UInt64;

namespace dmake {
	public static class Program {
		private static String CacheDirectory {
			get;
			set;
		}

		public static i32 Main(String[] args) {
			// Default argument values.
			// NB: these are strings ONLY, conversion to appropriate types is done ad hoc.
			var dic = new Dictionary<String,String> {
				["target"] = null,
				["filename"] = "makefile.xml",
				["redirect"] = "false",
				["cancel-on-error"] = "true",
				["var-str"] = ""
			};

			if(args.Length == 1 && args[0].Equals("love")) Program.WriteLine("Not war");
			Program.ParseArguments(args,dic);

			String filename = dic["filename"];
			if(!File.Exists(filename)) return Program.ExitMessage(1,"ERROR - file not found: {0}",filename);
			String target = dic["target"];

			// Prepare XOSS for Makefile type and its dependencies.
			Array<Node<String>>.RegisterType();
			NamedCollection.RegisterType();
			Makefile.RegisterType();

			// Parse the file
			Document d = Document.FromFile(filename);
			var file = (Makefile) d.Root;
			file.AddVariables(Program.ToNamedCollection(dic["var-str"]));

			if(String.IsNullOrWhiteSpace(target)) {
				target = file.DefaultTarget;
				Program.WriteLine($"No target specified; defaulting to {target}");
			} // if

			if(!file.Commands.ContainsKey(target)) return Program.ExitMessage(2,"ERROR - command not found: {0}",target);
			Program.CacheDirectory = Program.FindCacheDirectory();
			bool redirect = dic["redirect"].Equals("true");
			//bool cancel_on_error = dic["cancel-on-error"].Equals("true");

			(ProcessStartInfo, bool)[] res = file.MakeCommands(target);
			for(i32 i = 0; i < res.Length; i++) {
				(ProcessStartInfo, bool) current = res[i];
				ProcessStartInfo psi = current.Item1;
				bool cancel_on_error = current.Item2;
				Program.WriteLine($"{psi.FileName} {psi.Arguments}");
				try {
					psi.WorkingDirectory = file.GetVariable("wd");
					psi.RedirectStandardError = redirect;
					psi.RedirectStandardOutput = redirect;
					// If redirecting, UseShellExecute == true will throw an exception
					psi.UseShellExecute = !redirect;

					Process p = Process.Start(psi);
					p.WaitForExit();
					if(redirect) {
						Console.WriteLine(p.StandardOutput.ReadToEnd());
						Console.WriteLine(p.StandardError.ReadToEnd());
					} // if

					if(p.ExitCode != 0) {
						Program.Write($"{psi.FileName} exited with code {p.ExitCode}... ");
						if(cancel_on_error) {
							Program.WriteLine("exiting!");
							return Program.ExitMessage(p.ExitCode);
						} else Program.WriteLine("ignoring");
					} // if
				} catch(Exception e) {
					Program.WriteLine($"Error: {e.ToString()}");
				} // try
			} // for

			return Program.ExitMessage(0);
		}

		[DebuggerStepThrough]
		private static void WriteLine(String l) => Console.WriteLine("[DMAKE] {0}",l);
		[DebuggerStepThrough]
		private static void Write(String l) => Console.Write("[DMAKE] {0}",l);

		// Converts a string representing a Dictionary<String,String> to a NamedCollection.
		private static NamedCollection ToNamedCollection(String v) {
			Dictionary<String,String> dic = Util.ParseDictionary(Util.Unescape(v));
			var ret = new NamedCollection("nc");

			foreach(String i in dic.Keys) {
				//Node.Create<String>(i,dic[i],ret);
				var nd = new Node<String>(i,dic[i],ret);
				ret.Add(nd);
			} // foreach

			return ret;
		}

		private static String FindCacheDirectory() {
			String BaseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			return $"{BaseDir}\\cache";
		}

		private static void ParseArguments(String[] args,Dictionary<String,String> dic) {
			for(i32 i = 0; i < args.Length; i++) {
				String current = args[i];
				if(String.IsNullOrWhiteSpace(current)) continue;
				if(current.Equals("--var-str") || current.Equals("--var.str")) dic["var-str"] = args[++i];
				else if(current.Equals("--filename")) dic["filename"] = args[++i];
				else if(current.Equals("--target")) dic["target"] = args[++i];
				else if(current.Equals("--redirect")) dic["redirect"] = args[++i];
				else if(current.Equals("--cancel-on-error")) dic["cancel-on-error"] = args[++i];
				else if(Program.IsTarget(i,current)) dic["target"] = args[i];
				else Program.WriteLine($"Ignoring unknown command line option \"{current}\"");
			} // for
		}

		// FIXME: need better criteria
		private static bool IsTarget(i32 i,String s) => i == 0;

		private static i32 ExitMessage(i32 ExitCode,String fmt,params Object[] args) {
			Program.WriteLine(String.Format(fmt,args));
			return Program.ExitMessage(ExitCode);
		}

		private static i32 ExitMessage(i32 ExitCode) {
#if CONFIRM_EXIT
			Program.Write("Done; press [ENTER] to exit... ");
			Program.ReadLine();
#else
			Program.WriteLine("Done");
#endif
			return ExitCode;
		}
	}
}
