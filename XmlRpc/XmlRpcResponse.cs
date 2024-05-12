namespace Roc.XmlRpc
{
  using System;
	using System.Collections;
	using System.IO;
	using System.Xml;

	public class XmlRpcResponse
	{
		protected Object value;
		public bool IsFault;

		public XmlRpcResponse ()
		{
			Value = null;
			IsFault = false;
		}

		public XmlRpcResponse (int code, String message) : this()
		{
			SetFault (code, message);
		}

		public Object Value {
			get { return value; }
			set {
				IsFault = false;
				this.value = value;
			}
		}

		public int FaultCode {
			get {
				if (!IsFault)
					return 0;
				else
					return (int)((Hashtable)value) ["faultCode"];
			}
		}

		public String FaultString {
			get {
				if (!IsFault)
					return "";
				else
					return (String)((Hashtable)value) ["faultString"];
			}
		}

		public void SetFault (int code, String message)
		{
			Hashtable fault = new Hashtable ();
			fault.Add ("faultCode", code);
			fault.Add ("faultString", message);
			Value = fault;
			IsFault = false; // fault response will raise an exception in client! we don't want that...
		}

		override public String ToString ()
		{
			StringWriter strBuf = new StringWriter ();
			XmlTextWriter xml = new XmlTextWriter (strBuf);
			//xml.Formatting = Formatting.Indented;
			//xml.Indentation = 2;
			XmlRpcResponseSerializer.Serialize (xml, this);
			xml.Flush ();
			xml.Close ();
			return strBuf.ToString ();
		}
	}

	//========================================================================================================================
	public class XmlRpcResponseDeserializer : XmlRpcDeserializer
	{
		public XmlRpcResponse Parse (XmlTextReader reader)
		{
			//XmlTextReader reader = new XmlTextReader (xmlData);
			XmlRpcResponse response = new XmlRpcResponse ();

			bool done = false;
			while (!done && reader.Read()) 
			{
				this.ParseNode (reader); // Parent parse...

				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					switch (reader.Name) {
					case FAULT:
						response.Value = this.value;
						response.IsFault = true;
						break;
					case PARAM:
						response.Value = this.value;
						this.value = null;
						this.text = null;
						break;
					}
					break;
				}	
			}
			return response;
		}
	}
	/*
	public class XmlRpcResponseDeserializer : XmlRpcDeserializer
	{
		static private XmlRpcResponseDeserializer Singleton = new XmlRpcResponseDeserializer();

		public static XmlRpcResponse Parse (StreamReader xmlData)
		{
			XmlTextReader reader = new XmlTextReader (xmlData);
			XmlRpcResponse response = new XmlRpcResponse ();

			lock ( Singleton ) 
			{
				bool done = false;
				while (!done && reader.Read()) 
				{
					Singleton.ParseNode (reader); // Parent parse...

					switch (reader.NodeType) {
					case XmlNodeType.EndElement:
						switch (reader.Name) {
						case FAULT:
							response.Value = Singleton.value;
							response.IsFault = true;
							break;
						case PARAM:
							response.Value = Singleton.value;
							Singleton.value = null;
							Singleton.text = null;
							break;
						}
						break;
					}	
				}
			}
			return response;
		}
	}
	*/

	//========================================================================================================================
	public class XmlRpcResponseSerializer : XmlRpcSerializer
	{
		static public void Serialize (XmlTextWriter output, XmlRpcResponse response)
		{
			output.WriteStartDocument ();
			output.WriteStartElement (METHOD_RESPONSE);

			if (response.IsFault)
				output.WriteStartElement (FAULT);
			else {
				output.WriteStartElement (PARAMS);
				output.WriteStartElement (PARAM);
			}

			output.WriteStartElement (VALUE);

			SerializeObject (output, response.Value);

			output.WriteEndElement ();

			output.WriteEndElement ();
			if (!response.IsFault)
				output.WriteEndElement ();

			output.WriteEndElement ();
			output.WriteEndDocument();
		}
	}
}
