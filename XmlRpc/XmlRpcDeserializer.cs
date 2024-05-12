namespace Roc.XmlRpc
{
  using System;
	using System.Collections;
	using System.IO;
	using System.Xml;
	using System.Diagnostics;
	using System.Globalization;

	public class XmlRpcDeserializer : XmlRpcXmlTokens
	{
		private static 	DateTimeFormatInfo	dateFormat = new DateTimeFormatInfo ();
		protected 	String 			text;
		protected 	Object 			value;
		protected 	Object 			name;
		private 	Object 			container;
		private 	Stack 			containerStack;

		public XmlRpcDeserializer ()
		{
			this.container = null;
			this.containerStack = new Stack ();
			dateFormat.FullDateTimePattern = ISO_DATETIME;
		}

		public void ParseNode (XmlTextReader reader)
		{
			switch (reader.NodeType) {
			case XmlNodeType.Element:
				//Console.WriteLine (new String(' ', reader.Depth)+"<" + reader.Name + ">");

				switch (reader.Name) {
				case VALUE:
					this.value = null;
					this.text = null;
					break;
				case STRUCT:
					if (this.container != null)
						this.containerStack.Push (this.container);
					this.container = new Hashtable ();
					break;
				case ARRAY:
					if (this.container != null)
						this.containerStack.Push (this.container);
					this.container = new ArrayList ();
					break;
				}
				break;
			case XmlNodeType.EndElement:
				//Console.WriteLine (new String(' ', reader.Depth)+"</" + reader.Name + ">");
				switch (reader.Name) {
				case BASE64:
					this.value = Convert.FromBase64String (this.text);
					break;
				case BOOLEAN:
					int val = Int16.Parse (this.text);
					if (val == 0)
						this.value = false;
					else if (val == 1)
						this.value = true;
					break;
				case STRING:
					this.value = this.text;
					//Console.WriteLine (new String(' ', reader.Depth)+"Value: " + this.text + "[" + (this.text == null ? "NULL" : "OK") + "]");

					break;
				case DOUBLE:
					this.value = Double.Parse (this.text, CultureInfo.InvariantCulture);
					break;
				case INT:
				case ALT_INT:
					this.value = Int32.Parse (this.text);
					break;
				case DATETIME:
					this.value = DateTime.ParseExact (this.text, "F", dateFormat);
					break;
				case NAME:
					this.name = this.text;
					break;
				case VALUE:
					if (this.value == null)
						this.value = this.text; // some kits don't use <string> tag, they just do <value>

					if ((this.container != null) && (this.container is ArrayList)) // in an array?  If so add value to it.
						((ArrayList)this.container).Add (this.value);

					//Console.WriteLine (new String(' ', reader.Depth)+"Value: " + this.text + "[" + (this.text == null ? "NULL" : "OK") + "]");

					break;
				case MEMBER:
					if ((this.container != null) && (this.container is Hashtable)) // in an struct?  If so add value to it.
						((Hashtable)this.container).Add (this.name, this.value);
					break;
				case ARRAY:
				case STRUCT:
					this.value = this.container;
					this.container = (this.containerStack.Count == 0) ? null : this.containerStack.Pop ();
					break;
				}
				break;
			case XmlNodeType.Text:
				//Console.WriteLine (new String(' ', reader.Depth)+"Text: " + reader.Value);
				this.text = reader.Value;
				break;
			default:
				break;
			}	

			//Debug.Write  ("Text now: " + this.text);
			//Debug.Write  ("Value now: " + id (this.value));
			//Debug.Write  ("Container now: " + id (this.container));
		}

		private String id (Object x)
		{
			if (x == null)
				return "null";

			return x.GetType ().Name + "[" + x.GetHashCode () + "]";
		}
	}
}


