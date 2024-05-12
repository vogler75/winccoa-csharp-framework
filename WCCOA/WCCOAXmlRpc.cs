using System;
using System.Collections;
using System.Net;

using Roc.XmlRpc;

namespace Roc.WCCOA
{
	public class WCCOAXmlRpc
	{
		protected volatile int    lastErrorNr; 
		protected volatile string lastErrorMsg = "";
		
		protected string 				host;
		protected int 				port;
		protected string 				rpcpath;
		
		protected string 				url;
		protected NetworkCredential 	credentials;
		protected int 				timeout;
		
		public bool 				CountData = true; // overhead!! xml will be parsed a second time!
		
		public int					SentData = 0;
		public int					RecvData = 0;
		
		public int 					SentDataSum = 0;
		public int					RecvDataSum = 0;
		
		//------------------------------------------------------------------------------------------------------------------------
		public WCCOAXmlRpc() 
		{
			this.host = "localhost";
			this.port = 8080;
			this.rpcpath = "RPC";
			CreateUrl ();
			this.credentials = null;
			this.timeout = 10000;
		}
		
		public WCCOAXmlRpc(string Host, int Port, string RpcPath, NetworkCredential Credentials = null, int Timeout = 10000)
		{
			this.host = Host;
			this.port = Port;
			this.rpcpath  = RpcPath;
			CreateUrl();	
			this.credentials = Credentials;
			this.timeout = Timeout;
		}
		
		public string Host { get { return host; } set { host=value; CreateUrl(); } }
		public int    Port { get { return port; } set { port=value; CreateUrl(); } }
		public string RpcPath  { get { return rpcpath ; } set { rpcpath =value; CreateUrl(); } }
		
		private void CreateUrl()
		{
			this.url = "http://" + this.host + ":" + this.port + "/" + this.rpcpath;
		}
		
		public NetworkCredential Credentials
		{
			get { return this.credentials; }
			set { this.credentials = value; }
		}
		
		public int Timeout 
		{
			get { return this.timeout; }
			set { this.timeout = value; }
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public int LastErrorNr
		{
			get { return this.lastErrorNr; }
			set { this.lastErrorNr = value; if ( value == 0 ) this.lastErrorMsg=""; }
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public string LastErrorMsg 
		{
			get { return this.lastErrorMsg; }
			set { this.lastErrorMsg = value; if ( value == "" ) this.lastErrorNr=0; }
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public static void PrintArrayList(ArrayList Par, string x = " ")
		{
			Console.WriteLine (x + "A:" + Par.Count);
			for ( int i=0; i<Par.Count; i++ )
			{
				if ( Par[i] is ArrayList ) 
					PrintArrayList(Par[i] as ArrayList, x + "  ");
				else if ( Par[i] is String[] )
				{
					Console.WriteLine (x + "  S:" + ((string[])Par[i]).Length );
					for ( int j=0; j<((string[])Par[i]).Length; j ++)
						Console.WriteLine (x + "    [" + j +"]:" + (string)((Par[i] as string[])[j]));
				}
				else
				{
					Console.WriteLine (x + "  [" + i + "]:" + Par[i] + " (" + (Par[i]==null ? "null" : Par[i].GetType().ToString())+")");
				}
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public virtual bool Call(string method, ArrayList input, out ArrayList output)
		{
			try 
			{
				Debug.Write ("WCCOABase:Call:request " + method);
				XmlRpcRequest request = new XmlRpcRequest(this.url, this.credentials, this.timeout);
				XmlRpcResponse response;
				
				request.MethodName = method;
				request.Params.Clear();
				
				for ( int i=0; i<input.Count; i++ )
					request.Params.Add(input[i]);
				
				Debug.Write ("WCCOABase:Call:" + request.ToString());
				
				response = request.Send();
				
				//Debug.Write ("WCCOABase:Call:request sent");
				
				//-------------------------------------------
				//Console.WriteLine (request);
				//-------------------------------------------
				if ( CountData ) SentDataSum += (SentData = request.ToString().Length);
				
				if ( !response.IsFault)
				{
					if ( response.Value is ArrayList ) 
					{
						output = (ArrayList)response.Value;
						LastErrorNr = 0;
						//-------------------------------------------
						Console.WriteLine (response);
						//-------------------------------------------
						if ( CountData ) RecvDataSum += (RecvData = response.ToString().Length);
						Debug.Write ("WCCOABase:Call:got response. ok.");
						return true;
					}
					else
					{
						output = new ArrayList();
						LastErrorNr = -2;
						LastErrorMsg = "return value is not an array!";
						Debug.Write ("WCCOABase:Call:got response is not an array!");
						return false;
					}
				}
				else
				{
					output = new ArrayList();
					LastErrorNr = response.FaultCode;
					LastErrorMsg = response.FaultString;
					Debug.Write ("WCCOABase:Call:got response is faulty!");
					return false;
				}
			}
			catch ( Exception ex )
			{
				output = new ArrayList();
				LastErrorNr = -1;
				LastErrorMsg = ex.Message;
				Debug.Write ("WCCOABase:Call:exception! " + ex.Message);
				return false;
			}
		}	
		
		//------------------------------------------------------------------------------------------------------------------------
		public virtual bool CallAsync(string method, ArrayList input, XmlRpcAsyncCallback func)
		{
			XmlRpcRequest request = new XmlRpcRequest(this.url, this.credentials, this.timeout);
			
			request.MethodName = method;
			request.Params.Clear();
			
			for ( int i=0; i<input.Count; i++ )
				request.Params.Add(input[i]);
			
			try {
				request.SendAsync(func);
				LastErrorNr = 0;
				return true;
			}
			catch ( Exception ex )
			{
				LastErrorNr = -1;
				LastErrorMsg = ex.Message;
				return false;
			}	
		}
	}
}

