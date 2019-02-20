// (c) 2018 by Tom van Dijkhuizen. All rights reserved.

// .NET namespaces
using System;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;

// Other namespaces
using XOSS;
using XOSS.Text;
using XOSS.Types;

// Typedefs
using i32 = System.Int32;

namespace dmake {
	// Helper object that handles parsing/serialising ProcessStartInfo objects.
	public class PsiHelperNode : Node_base {
		public const String PublicTypeName = "ProcessStartInfo";
		private String ExeName_stub;
		private String Arguments_stub;
		
		public bool CancelOnError {
			get;
			private set;
		}

		public ProcessStartInfo Psi {
			get;
			private set;
		} = new ProcessStartInfo();

		public PsiHelperNode(String Name,Node_base Parent,Document owner) {
			this.Name = Name;
			this.Parent = Parent;
			this.Owner = owner;
		}

		private static bool TypeRegistered = false;

		internal static void RegisterType() {
			if(PsiHelperNode.TypeRegistered) return;

			TreeBuilder.RegisterType<PsiHelperNode>(PsiHelperNode.PublicTypeName,(XElement el,Node_base Parent,Document owner) => {
				IEnumerable<XElement> Children = el.Elements();
				var ret = new PsiHelperNode(el.Name.ToString(),Parent,owner);
				foreach(XElement c in Children) {
					String Name = c.Name.ToString();
					if(Name.Equals("ExeName")) ret.ExeName_stub = c.Value;
					else if(Name.Equals("args")) ret.Arguments_stub = c.Value;
					else if(Name.Equals("CancelOnError")) ret.CancelOnError = c.Value.Equals("true");
				} // foreach
				return ret;
			},(Node_base N,i32 lvl,StringBuilderEx sb) => {
				TreeBuilder.CheckChildCount(N,0,0);
				((PsiHelperNode) N).ToXML(lvl,sb);
			});

			PsiHelperNode.TypeRegistered = true;
		}

		public override void ToXML(i32 lvl,StringBuilderEx sb) {
			sb.InsertTabs(lvl).AppendFormat("<{0} type=\"{1}\">",this.Name,this.TypeName).AppendLine();
			sb.InsertTabs(lvl + 1).AppendFormat("<ExeName type=\"string\">{0}</ExeName>",this.ExeName_stub).AppendLine();
			sb.InsertTabs(lvl + 1).AppendFormat("<args type=\"string\">{0}</args>",this.Psi.Arguments).AppendLine();
			sb.InsertTabs(lvl).AppendFormat("</{1}>",this.Name).AppendLine();
		}
		
		public String BuildCommandString(NamedCollection Variables) => this.BuildString(this.ExeName_stub,Variables);
		public String BuildArgumentsString(NamedCollection Variables) => this.BuildString(this.Arguments_stub,Variables);

		// Fills in variables in a string.
		// In order to use a variable, its name must be surrounded by exclamation points.
		// For example !wd! will return the value of the variable named wd.
		private String BuildString(String str,NamedCollection Variables) {
			String prev = "";
			while(!prev.Equals(str)) {
				prev = str;
				foreach(String i in Variables.Keys) {
					String rep = String.Format("!{0}!",i);
					str = str.Replace(rep,Variables.GetValue<String>(i));
				} // foreach
			} // while
			return str;
		}

		// Fills in the ProcessStartInfo object that belongs to this node using the Variables collection.
		public ProcessStartInfo GetPsi(NamedCollection Variables) {
			ProcessStartInfo ret = this.Psi;

			ret.FileName = this.BuildCommandString(Variables);
			ret.Arguments = this.BuildArgumentsString(Variables);

			return ret;
		}
	}
}
