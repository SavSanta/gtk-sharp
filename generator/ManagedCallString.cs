// GtkSharp.Generation.ManagedCallString.cs - The ManagedCallString Class.
//
// Author: Mike Kestner <mkestner@speakeasy.net>
//
// Copyright (c) 2003 Mike Kestner
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the GNU General Public
// License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.


namespace GtkSharp.Generation {

	using System;
	using System.Collections;
	using System.IO;

	public class ManagedCallString {
		
		ArrayList parms = new ArrayList ();
		ArrayList special = new ArrayList ();
		bool has_error;

		public ManagedCallString (Parameters parms)
		{
			for (int i = 1; i < parms.Count; i ++) {
				Parameter p = parms [i];
				if (p.IsLength && parms [i-1].IsString) 
					continue;
				else if (p.Scope == "notified")
					i += 2;
					
				else if (p is ErrorParameter) {
					has_error = true;
					continue;
				}
				this.parms.Add (p);

				if (p.PassAs != String.Empty && (p.Name != p.FromNative (p.Name)))
					this.special.Add (true);
				else if (p.Generatable is CallbackGen)
					this.special.Add (true);
				else
					this.special.Add (false);
			}
		}

		public string Setup (string indent)
		{
			string ret = "";

			if (has_error)
				ret = indent + "error = IntPtr.Zero;\n";

			for (int i = 0; i < parms.Count; i ++) {
				if ((bool)special[i] == false)
					continue;

				Parameter p = parms [i] as Parameter;
				IGeneratable igen = p.Generatable;

				if (igen is CallbackGen) {
					ret += indent + String.Format ("{0} {1}_invoker = new {0} ({1});\n", (igen as CallbackGen).InvokerName, p.Name);
				} else {
					ret += indent + igen.QualifiedName + " my" + p.Name;
					if (p.PassAs == "ref")
						ret += " = " + p.FromNative (p.Name);
					ret += ";\n";
				}
			}

			return ret;
		}

		public override string ToString ()
		{
			if (parms.Count < 1)
				return "";

			string[] result = new string [parms.Count];

			for (int i = 0; i < parms.Count; i ++) {
				Parameter p = parms [i] as Parameter;
				result [i] = p.PassAs == "" ? "" : p.PassAs + " ";
				if (p.Generatable is CallbackGen)
					result [i] += p.Name + "_invoker.Handler";
				else
					result [i] += ((bool)special[i]) ? "my" + p.Name : p.FromNative (p.Name);
			}

			return String.Join (", ", result);
		}

		public string Finish (string indent)
		{
			string ret = "";

			for (int i = 0; i < parms.Count; i ++) {
				if ((bool)special[i] == false)
					continue;

				Parameter p = parms [i] as Parameter;
				IGeneratable igen = p.Generatable;

				if (igen is CallbackGen)
					continue;
				else if (igen is StructBase || igen is ByRefGen)
					ret += indent + String.Format ("if ({0} != IntPtr.Zero) System.Runtime.InteropServices.Marshal.StructureToPtr (my{0}, {0}, false);\n", p.Name);
				else
					ret += indent + p.Name + " = " + igen.ToNativeReturn ("my" + p.Name) + ";\n";
			}

			return ret;
		}
	}
}
