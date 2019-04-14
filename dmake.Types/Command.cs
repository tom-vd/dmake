// (c) 2018 by Tom van Dijkhuizen. All rights reserved.

// .NET namespaces
using System;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;

// Other namespaces
using XOSS;
using XOSS.Text;

// Typedefs
using i32 = System.Int32;

namespace dmake {
	namespace Types {
		// OBject that contains IDs of Command objects (i.e. dependencies) and command lines.
		public class Command : Node_base {
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
				XOSS.Types.Array<PsiHelperNode>.RegisterType();
				if(Command.TypeRegistered) return;

				TreeBuilder.RegisterType<Command>(Command.PublicTypeName,(XElement el,Node_base Parent,Document owner) => {
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
				},(Node_base N,i32 lvl,StringBuilderEx sb) => {
					TreeBuilder.CheckChildCount(N,0,0);
					((Command) N).ToXML(lvl,sb);
				});

				Command.TypeRegistered = true;
			}

			public override void ToXML(i32 lvl,StringBuilderEx sb) {
				sb.InsertTabs(lvl).AppendFormat("<{0} type=\"{1}\">",this.Name,this.TypeName).AppendLine();
				foreach(Node_base nd in this.Children) nd.ToXML(lvl + 1,sb);
				sb.InsertTabs(lvl).AppendFormat("</{0}>",this.Name).AppendLine();
			}

			public PsiHelperNode GetPsiHelper(i32 idx) => (PsiHelperNode) this.Children[this.RunIdx].Children[idx];
			public String GetDependency(i32 idx) => ((Node<String>) this.Children[this.DepIdx].Children[idx]).Value;

			public Command(String Name,Node_base Parent,Document owner) {
				this.Name = Name;
				this.Parent = Parent;
				this.Owner = owner;
			}
		}
	}
}
