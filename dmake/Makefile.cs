// (c) 2018 by Tom van Dijkhuizen. All rights reserved.

// .NET namespaces
using System;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;

// Other namespaces
using UOSS;
using UOSS.Text;

// Typedefs
using i32 = System.Int32;

namespace dmake {
	// Contains variables and commands, as well as a target (i.e. command ID) for the program to default to in case none is supplied.
	public class Makefile : Node {
		public const String PublicTypeName = "makefile";

		public NamedCollection Commands {
			get;
			private set;
		}

		public String DefaultTarget {
			get;
			private set;
		}

		private NamedCollection m_Variables;

		private static bool TypeRegistered = false;
		public static void RegisterType() {
			Command.RegisterType();
			if(Makefile.TypeRegistered) return;

			TreeBuilder.RegisterType(typeof(Makefile),Makefile.PublicTypeName,(XElement el,Node Parent,Document owner) => {
				IEnumerable<XElement> Children = el.Elements();
				var ret = new Makefile(el.Name.ToString(),Parent,owner);
				foreach(XElement c in Children) {
					String Name = c.Name.ToString();
					if(Name.Equals("variables")) ret.m_Variables = (NamedCollection) TreeBuilder.InvokeParser(c,Parent,owner);
					else if(Name.Equals("commands")) ret.Commands = (NamedCollection) TreeBuilder.InvokeParser(c,Parent,owner);
				} // foreach

				IEnumerable<XAttribute> Attributes = el.Attributes();
				foreach(XAttribute a in Attributes) {
					String Name = a.Name.ToString();
					if(Name.Equals("DefaultTarget")) ret.DefaultTarget = a.Value;
				} // foreach

				return ret;
			},(Node N, i32 lvl) => {
				TreeBuilder.CheckChildCount(N,0,0);
				return ((Makefile) N).ToXML_internal(lvl);
			});

			Makefile.TypeRegistered = true;
		}
		
		private String ToXML_internal(i32 lvl) {
			var sb = new StringBuilderEx();
			sb.InsertTabs(lvl).AppendFormat("<{0} type=\"{1}\">",this.Name,this.TypeName).AppendLine();

			
			sb.InsertTabs(lvl + 1).Append(this.m_Variables.ToXML(lvl + 1));
			sb.InsertTabs(lvl + 1).Append(this.Commands.ToXML(lvl + 1));
			sb.InsertTabs(lvl).AppendFormat("</{0}>",this.Name).AppendLine();

			return sb.ToString();
		}

		public Makefile(Document owner) : base("obj",Makefile.PublicTypeName,typeof(Makefile),null,null,owner) {

		}

		public Makefile(String Name,Document owner) : base(Name,Makefile.PublicTypeName,typeof(Makefile),null,null,owner) {

		}

		public Makefile(String Name,Node Parent,Document owner) : base(Name,Makefile.PublicTypeName,typeof(Makefile),null,Parent,owner) {

		}

		public void AddVariables(NamedCollection nc) => this.AddVariables(nc,true);
		public void AddVariables(NamedCollection nc,bool OverrideExisting) {
			if(!OverrideExisting) {
				foreach(String i in nc) {
					if(this.m_Variables.ContainsKey(i)) continue;
					this.m_Variables.Add(nc[i]);
				} // foreach
			} else foreach(String i in nc) this.m_Variables.SetOrAdd(nc[i]);
		}

		public (ProcessStartInfo,bool)[] MakeCommands(String cmd) => this.MakeCommands(cmd,true);
		// Returns an array of ProcessStartInfo objects based on child nodes found in the <commands /> section.
		// Also asks PSI helper nodes to fill in variables for the ExeName and args members.
		// NB: dependencies will be inserted in the array BEFORE the ProcessStartInfo object that corresponds with cmd; this ensures that dependencies are run first.
		public (ProcessStartInfo,bool)[] MakeCommands(String cmd,bool IncludeDependencies) {
			var c = (Command) this.Commands[cmd];

			var ret = new List<(ProcessStartInfo,bool)>();
			if(IncludeDependencies) {
				i32 depc = c.DependencyCount;
				for(i32 i = 0; i < depc; i++) {
					String dep = c.GetDependency(i);
					ret.AddRange(this.MakeCommands(dep,true));
				} // for
			} // if

			i32 N = c.CommandCount;
			for(i32 i = 0; i < N; i++) {
				PsiHelperNode h = c.GetPsiHelper(i);
				ret.Add((h.GetPsi(this.m_Variables),h.CancelOnError));
			} // for
			return ret.ToArray();
		}

		public String GetVariable(String idx) {
			if(idx.Equals("wd")) {
				if(this.m_Variables.ContainsKey("OverrideWD")) {
					var OverrideWD = (String) this.m_Variables["OverrideWD"].Value;
					if(OverrideWD.ToLower().Equals("true")) {
						if(this.m_Variables.ContainsKey("wd")) return (String) this.m_Variables["wd"].Value;
						throw new Exception("If OverrideWD is \"true\", specify a working directory in the wd variable");
					} // if
				} // if
				return Environment.CurrentDirectory;
			} else return (String) this.m_Variables[idx].Value;
		}
	}
}
