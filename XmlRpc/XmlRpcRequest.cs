namespace Roc.XmlRpc
{
  using System;
	using System.Collections;
	using System.IO;
	using System.Xml;
	using System.Net;
	using System.Text;
	using System.Reflection;
	using System.Threading;
	using System.Globalization;
	using System.Diagnostics;

	public delegate void XmlRpcAsyncCallback(XmlRpcResponse output);
  
	public class XmlRpcRequest
	{
		public String MethodName;
		public ArrayList Params;

		private HttpWebRequest request;

		private Encoding encoding = null; //new ASCIIEncoding();

		//------------------------------------------------------------------------------------------------------------------------
		public XmlRpcRequest ()
		{
			Params = new ArrayList ();
		}

		//------------------------------------------------------------------------------------------------------------------------
		public XmlRpcRequest (String Url, NetworkCredential Credentials = null, int Timeout = 1000) : this()
		{
			request = (HttpWebRequest)WebRequest.Create(Url);

			request.Method = "POST";
			request.ContentType = "text/xml";
			request.Headers.Add (HttpRequestHeader.AcceptEncoding, "gzip,deflate");
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			if ( Credentials != null ) 
			{
				request.Credentials = Credentials;
				request.PreAuthenticate = true;
			}

			request.Timeout = Timeout;

		}

		//------------------------------------------------------------------------------------------------------------------------
		public String MethodNameObject {
			get {
				int index = MethodName.IndexOf (".");

				if (index == -1)
					return MethodName;

				return MethodName.Substring (0, index);
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public String MethodNameMethod {
			get {
				int index = MethodName.IndexOf (".");

				if (index == -1)
					return MethodName;

				return MethodName.Substring (index + 1, MethodName.Length - index - 1);
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public XmlRpcResponse Send ()
		{	
			Stream stream = request.GetRequestStream ();
			XmlTextWriter xmlw = new XmlTextWriter (stream, encoding);
			XmlRpcRequestSerializer.Serialize (xmlw, this.MethodName, this.Params);
			xmlw.Flush ();
			xmlw.Close ();

			HttpWebResponse response = (HttpWebResponse)request.GetResponse ();

			StreamReader input = new StreamReader (response.GetResponseStream ());
			var des = new XmlRpcResponseDeserializer();
			var xmlr = new XmlTextReader(input);

			XmlRpcResponse resp = des.Parse (xmlr);

			xmlr.Close();
			//response.Close ();

			return resp;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public void SendAsync (XmlRpcAsyncCallback func)
		{
			Stream stream = request.GetRequestStream ();

			XmlTextWriter xml = new XmlTextWriter (stream, encoding);
			XmlRpcRequestSerializer.Serialize (xml, this.MethodName, this.Params);
			xml.Flush ();
			xml.Close ();

			request.BeginGetResponse(new AsyncCallback(FinishAsync), new object[] { request, func });
		}

		//------------------------------------------------------------------------------------------------------------------------
		private void FinishAsync(IAsyncResult result)
		{
			HttpWebRequest request = (result.AsyncState as object[])[0] as HttpWebRequest;
			XmlRpcAsyncCallback func = (result.AsyncState as object[])[1] as XmlRpcAsyncCallback;

			HttpWebResponse response = request.EndGetResponse(result) as HttpWebResponse;

			StreamReader input = new StreamReader (response.GetResponseStream ());

			var des = new XmlRpcResponseDeserializer();
			var xmlr = new XmlTextReader(input);
			XmlRpcResponse resp = des.Parse(xmlr);
			xmlr.Close();

			//response.Close ();

			func(resp);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public Object Invoke (Object target)
		{
			Type type = target.GetType ();
			MethodInfo method = type.GetMethod (MethodNameMethod);

			if (method == null)
				throw new XmlRpcException (-2, "Method " + MethodNameMethod + " not found.");

			//Debug.Write ("XmlRpcRequest:Invoke:Parameter: "+ Params.Count);

			Object[] args = new Object[Params.Count];

			for (int i = 0; i < Params.Count; i++)
			{
				args [i] = Params [i];			
				//if ( args[i] is ArrayList )
				//	Debug.Write ("XmlRpcRequest:Invoke:Parameter["+i+"]:A:"+ (args[i] as ArrayList).Count);
				//else
				//	Debug.Write ("XmlRpcRequest:Invoke:Parameter["+i+"]:V:"+ args[i]);
			}

			return method.Invoke (target, args);
		}

		//------------------------------------------------------------------------------------------------------------------------
		override public String ToString ()
		{
			StringWriter strBuf = new StringWriter ();
			XmlTextWriter xml = new XmlTextWriter (strBuf);
			xml.Formatting = Formatting.Indented;
			xml.Indentation = 2;
			XmlRpcRequestSerializer.Serialize (xml, this.MethodName, this.Params);
			xml.Flush ();
			xml.Close ();
			return strBuf.ToString ();
		}
	}


	//========================================================================================================================
	public class XmlRpcRequestSerializer : XmlRpc.XmlRpcSerializer
	{
		static public void Serialize (XmlTextWriter output, string MethodName, ArrayList Params)
		{
			output.WriteStartDocument ();
			output.WriteStartElement (METHOD_CALL);
			output.WriteElementString (METHOD_NAME, MethodName);
			output.WriteStartElement (PARAMS);
			foreach (Object p in Params) {
				output.WriteStartElement (PARAM);
				output.WriteStartElement (VALUE);
				SerializeObject (output, p);
				output.WriteEndElement ();
				output.WriteEndElement ();
			}
			output.WriteEndElement ();
			output.WriteEndElement ();
			output.WriteEndDocument ();
		}
	}

	//========================================================================================================================
	public class XmlRpcRequestDeserializer : XmlRpcDeserializer
	{
		public XmlRpcRequest Parse (XmlTextReader reader)
		{
			//XmlTextReader reader = new XmlTextReader (xmlData);
			XmlRpcRequest request = new XmlRpcRequest ();

			bool done = false;
			while (!done && reader.Read()) 
			{
				this.ParseNode (reader); // Parent parse...

				switch (reader.NodeType) {
				case XmlNodeType.EndElement:
					switch (reader.Name) {
					case METHOD_NAME:
						request.MethodName = this.text;
						break;
					case METHOD_CALL:
						done = true;
						break;
					case PARAM:
						request.Params.Add (this.value);
						this.text = null;
						break;
					}
					break;
				}	
			}
			return request;
		}
	}
}
