// (c) 2018 by Tom van Dijkhuizen. All rights reserved.

// .NET namespaces
using System;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;

// Other namespaces
using UOSS;
using UOSS.Text;

// Typedefs
using i32 = System.Int32;

namespace dmake {
	// OBject that contains IDs of Command objects (i.e. dependencies) and command lines.
	public class Command : Node {
		public const String PublicTypeName = "command";
		private readonly List<String> m_Dependencies = new List<String>();
		
		public i32 CommandCount => this.RunIdx < 0 ? 0 : this.Children[this.RunIdx].Children.Count;
		public i32 DependencyCount => this.DepIdx < 0 ? 0 : this.Children[this.DepIdx].Children.Count;

		// Index of node in this.Children to find Run objects.
		private i32 RunIdx = -1;
		// Index of node in this.Children to find dependencies.
		private i32 DepIdx = -1;

		private static bool TypeRegistered = false;
		internal static void RegisterType() {
			PsiHelperNode.RegisterType();
			Array<PsiHelperNode>.RegisterType();
			if(Command.TypeRegistered) return;

			TreeBuilder.RegisterType(typeof(Command),Command.PublicTypeName,(XElement el,Node Parent,Document owner) => {
				IEnumerable<XElement> Children = el.Elements();
				var ret = new Command(el.Name.ToString(),Parent,owner);
				foreach(XElement c in Children) {
					String Name = c.Name.ToString();
					if(Name.Equals("dependencies")) {
						ret.DepIdx = ret.Children.Count;
						ret.Children.Add(TreeBuilder.InvokeParser(c,Parent,owner));
					} else if(Name.Equals("run")) {
						ret.RunIdx = ret.Children.Count;
						ret.Children.Add(TreeBuilder.InvokeParser(c,Parent,owner));
					} // if
				} // foreach
				return ret;
			},(Node N,i32 lvl) => {
				TreeBuilder.CheckChildCount(N,0,0);
				return ((Command) N).ToXML_internal(lvl);
			});

			Command.TypeRegistered = true;
		}

		private String ToXML_internal(i32 lvl) {
			var sb = new StringBuilderEx();
			sb.InsertTabs(lvl).AppendFormat("<{0} type=\"{1}\">",this.Name,this.TypeName).AppendLine();
			foreach(Node nd in this.Children) sb.AppendLine(TreeBuilder.GenerateNodeXML(lvl + 1,nd));
			sb.InsertTabs(lvl).AppendFormat("</{0}>",this.Name).AppendLine();
			return sb.ToString();
		}
		
		public PsiHelperNode GetPsiHelper(i32 idx) => (PsiHelperNode) this.Children[this.RunIdx].Children[idx];
		public String GetDependency(i32 idx) => (String) this.Children[this.DepIdx].Children[idx].Value;
		
		public Command(String Name,Node Parent,Document owner) : base(Name,Command.PublicTypeName,typeof(Command),null,Parent,owner) {
		}
	}
}
