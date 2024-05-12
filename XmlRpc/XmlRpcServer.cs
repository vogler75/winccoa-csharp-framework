namespace Roc.XmlRpc
{
  using System;
	using System.Collections;
	using System.Diagnostics;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;
	//using System.Threading.Tasks;
	using System.Xml;

	public class XmlRpcServer : IEnumerable
	{
		private int 			port;
		private Hashtable 		handlers;

		private TcpListener 	listener;
		private Thread 			listener_thread;

		private bool 			threaded;

		//------------------------------------------------------------------------------------------------------------------------
		//The constructor which make the TcpListener start listening on the given port. 
		public XmlRpcServer (int port, bool threaded = true)
		{
			this.port = port;
			this.handlers = new Hashtable ();
			this.threaded = threaded;
			this.listener = new TcpListener (System.Net.IPAddress.Any, this.port);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public void Start ()
		{
			try {
				listener.Start ();
				listener_thread = new Thread (new ThreadStart (Listener));
				listener_thread.Start ();
			} catch (Exception e) {
				Debug.Write ("An Exception Occurred while Listening :" + e.ToString ());
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		///This method Accepts new connections and dispatches them when appropriate.
		public void Listener ()
		{
			while (true) {
				TcpClient client = listener.AcceptTcpClient ();
				if ( threaded ) 
				{
					var client_thread = new Thread (delegate () { XmlRpcClient(client); });
					client_thread.Start ();
				}
				else
				{
					XmlRpcClient (client);
				}
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		private void XmlRpcClient(TcpClient client)
		{			
			Debug.Write ("XmlRpcClient:thread...");
			XmlRpcServerRequest req = new XmlRpcServerRequest (client);
			while ( req.Read() ) 
			{
				if ( req.HttpMethod == "POST") {
					try {
						//StreamReader tr = httpReq.Input;
						//Console.WriteLine (tr.ReadToEnd());
						XmlRpcCall (req);
					} catch (Exception e) {
						Debug.Write ("XmlRpcClient:Failed on XmlRpcCall: " + e);
					}
				} else {
					Debug.Write  ("XmlRpcClient:Only POST methods are supported: " + req.HttpMethod + " ignored");
				}
			}
			req.Close ();
			client.Close ();
			Debug.Write ("XmlRpcClient:closed.");
		}

		//------------------------------------------------------------------------------------------------------------------------
		private void XmlRpcCall (XmlRpcServerRequest req)
		{
			//XmlRpcRequest rpc = XmlRpcRequestDeserializer.Parse(req.Input);
			var des = new XmlRpcRequestDeserializer();
			var xmlr = new XmlTextReader(req.Input);
			XmlRpcRequest rpc = des.Parse(xmlr);

			//Console.WriteLine(rpc.ToString());

			XmlRpcResponse resp = new XmlRpcResponse ();
			Object target = handlers [rpc.MethodNameObject];
	
			if (target == null) {
				resp.SetFault (-1, "XmlRpcCall.Method " + rpc.MethodNameObject + " not registered.");
			} else {
				try {
					resp.Value = rpc.Invoke (target);
				} catch (XmlRpcException e1) {
					resp.SetFault(e1.Code, e1.Message);
					Debug.Write ("XmlRpcCall.Invoke.E1: " + e1.Message);
				} catch (Exception e2) {
					resp.SetFault(-1, e2.Message);
					Debug.Write ("XmlRpcCall.Invoke.E2: " + e2.Message);
				}
			}

			StringWriter strw = new StringWriter ();
			XmlTextWriter xml = new XmlTextWriter (strw);
			XmlRpcResponseSerializer.Serialize (xml, resp);
			xml.Close();

			SendHeader (req.Protocol, "text/xml", strw.ToString().Length, " 200 OK", req.Output);
			req.Output.Write(strw);

			req.Flush();
		}

		//------------------------------------------------------------------------------------------------------------------------
		/// This function send the Header Information to the client (Browser)
		/// <param name="sHttpVersion">HTTP Version</param>
		/// <param name="sMIMEHeader">Mime Type</param>
		/// <param name="iTotBytes">Total Bytes to be sent in the body</param>
		/// <param name="sStatusCode"></param>
		/// <param name="output">Socket reference</param>
		public void SendHeader (string sHttpVersion, string sMIMEHeader, long iTotBytes, string sStatusCode, TextWriter output)
		{
			String sBuffer = "";
			
			// if Mime type is not provided set default to text/html
			if (sMIMEHeader.Length == 0) {
				sMIMEHeader = "text/html";  // Default Mime Type is text/html
			}

			sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
			sBuffer = sBuffer + "Connection: close\r\n";
			if (iTotBytes > 0)
				sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n";
			sBuffer = sBuffer + "Server: XmlRpcServer \r\n";
			sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";

			sBuffer = sBuffer + "\r\n";

			output.Write (sBuffer);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator ()
		{
			return handlers.GetEnumerator ();
		}

		//------------------------------------------------------------------------------------------------------------------------
		public Object this [String name] {
			get { return handlers [name]; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		///Add an XML-RPC handler object by name.
		public void Add (String name, Object obj)
		{
			handlers.Add (name, obj);
		}
	}

	//========================================================================================================================
	public class XmlRpcServerRequest
	{
		private String httpMethod = null;
		private String protocol;
		private String filePathFile = null;
		private String filePathDir = null;
		private String filePathVar;

		private TcpClient client;
		private StreamReader input;
		private StreamWriter output;
		private Hashtable headers;

		//------------------------------------------------------------------------------------------------------------------------
		public XmlRpcServerRequest (TcpClient client)
		{
			this.client = client;
			this.output = new StreamWriter (client.GetStream ());
			this.input = new StreamReader (client.GetStream ());
		}

		//------------------------------------------------------------------------------------------------------------------------
		public bool Read()
		{
			if ( GetRequestMethod () ) 
			{
				GetRequestHeaders ();
				return true;
			}
			else
			{
				return false;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public StreamWriter Output {
			get { return output; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		public StreamReader Input {
			get { return input; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		public TcpClient Client {
			get { return client; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		public String FilePath {
			get { return filePathVar; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		public String HttpMethod {
			get { return httpMethod; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		public String Protocol {
			get { return protocol; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		public String FilePathFile {
			get {
				if (filePathFile != null)
					return filePathFile;

				int i = FilePath.LastIndexOf ("/");

				if (i == -1)
					return "";
	    
				i++;
				filePathFile = FilePath.Substring (i, FilePath.Length - i);
				return filePathFile;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public String FilePathDir {
			get {
				if (filePathDir != null)
					return filePathDir;

				int i = FilePath.LastIndexOf ("/");

				if (i == -1)
					return "";
	    
				i++;
				filePathDir = FilePath.Substring (0, i);
				return filePathDir;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		private String filePath {
			get { return filePathVar; }
			set {
				filePathVar = value;
				filePathDir = null;
				filePathFile = null;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		private bool GetRequestMethod ()
		{
			string req;
			try {
				req = input.ReadLine ();
			}
			catch ( Exception e )
			{
				Debug.Write ("XmlRpcServer.GetRequestMethod:Exception: " + e.Message);
				return false;
			}

			if (req == null) return false;	//throw new ApplicationException ("Void request.");

			if (req.Length >= 4 && 0 == String.Compare ("GET ", req.Substring (0, 4), true))
				httpMethod = "GET";
			else if (req.Length >= 5 && 0 == String.Compare ("POST ", req.Substring (0, 5), true))
				httpMethod = "POST";
			else
			{
				//throw new InvalidOperationException ("Unrecognized method in query: " + req);
				Debug.Write("Unrecognized method in query: " + req);
			}

			req = req.TrimEnd ();
			int idx = req.IndexOf (' ') + 1;
			if (idx >= req.Length) return false; //throw new ApplicationException ("What do you want?");

			string page_protocol = req.Substring (idx);
			int idx2 = page_protocol.IndexOf (' ');
			if (idx2 == -1)
				idx2 = page_protocol.Length;
		
			filePath = page_protocol.Substring (0, idx2).Trim ();
			protocol = page_protocol.Substring (idx2).Trim ();

			return true;
		}

		//------------------------------------------------------------------------------------------------------------------------
		private void GetRequestHeaders ()
		{
			String line;
			int idx;

			headers = new Hashtable ();

			while ((line = input.ReadLine ()) != "") {
				if (line == null) {
					break;
				}

				idx = line.IndexOf (':');
				if (idx == -1 || idx == line.Length - 1) {
					Debug.Write ("Malformed header line: " + line);
					continue;
				}

				String key = line.Substring (0, idx);
				String value = line.Substring (idx + 1);

				try {
					headers.Add (key, value);
				} catch (Exception) {
					Debug.Write ("Duplicate header key in line: " + line);
				}
			}
		}
    
		//------------------------------------------------------------------------------------------------------------------------
		override public String ToString ()
		{
			return HttpMethod + " " + FilePath + " " + Protocol;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public void Flush()
		{
			output.Flush();
		}


		//------------------------------------------------------------------------------------------------------------------------
		public void Close ()
		{
			output.Close ();
			input.Close ();
		}
	}


}
