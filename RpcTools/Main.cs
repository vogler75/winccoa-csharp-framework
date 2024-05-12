using System;
using System.Collections;
//using System.Net;
//using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Net;
using Roc.XmlRpc;

namespace RpcTools
{
	class RpcTools
	{
		private int Port;

		//private Hashtable HttpProxyList = new Hashtable();

		//------------------------------------------------------------------------------------------------------------------------
		public RpcTools(int Port = 8081)
		{
			this.Port = Port;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public void Start()
		{
			// xmlrpc listener/server
			XmlRpcServer rpc = new XmlRpcServer (this.Port);
			rpc.Add ("toa", this);
			rpc.Start ();
			Console.WriteLine ("WCCOA XMLRPC Server running on port {0} ... ", Port);
		}


		//------------------------------------------------------------------------------------------------------------------------
		public static void PrintArrayList(ArrayList Par, string x)
		{
			Console.WriteLine (x + "C:" + Par.Count);
			for ( int i = 0; i<Par.Count; i++ )
			{
				if ( Par[i] is ArrayList ) 
					PrintArrayList(Par[i] as ArrayList, x + "  ");
				else
					Console.WriteLine (x + "  V[ " + i + "]:" + Par[i]);
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public ArrayList echo (ArrayList arg)
		{
			Console.WriteLine ("echo " + arg.Count);
			//PrintArrayList(arg, " ");
			return arg;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public string RequestSerializer (string MethodName, ArrayList Params)
		{
			StringWriter strBuf = new StringWriter ();
			XmlTextWriter xml = new XmlTextWriter (strBuf);
			XmlRpcRequestSerializer.Serialize (xml, MethodName, Params);
			xml.Flush ();
			xml.Close ();
			return strBuf.ToString();
		}

		//------------------------------------------------------------------------------------------------------------------------
		public object ResponseDeserializer (string xmlstr)
		{
			byte[] byteArray = Encoding.ASCII.GetBytes( xmlstr );
			var stream = new MemoryStream( byteArray );
			var sr = new StreamReader(stream);
			var des = new XmlRpcResponseDeserializer();
			var xmlr = new XmlTextReader(sr);
			XmlRpcResponse resp = des.Parse(xmlr);
			return resp.Value;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public ArrayList HttpGet(string url)
		{
			ArrayList res = new ArrayList();
			WebRequest request = WebRequest.Create(url);
			try 
			{
				Stream str = request.GetResponse().GetResponseStream();
				StreamReader strr = new StreamReader(str);

				for ( string s=""; (s=strr.ReadLine())!=null; res.Add(s) );
					;
			}
			catch ( Exception e )
			{
				res.Add(e.Message);
			}
			return res;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public ArrayList forward (string id, string MethodName, ArrayList Params, int MsgId)
		{
			ArrayList ret = new ArrayList();
			Console.WriteLine("forward:" + id +" " + MethodName + " " + MsgId);
			ret.Add(0);
			return ret;
		}

		//------------------------------------------------------------------------------------------------------------------------
		private byte[] StringToByteArray(string str)
		{
		    System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
		    return enc.GetBytes(str);
		}

		private string ByteArrayToString(byte[] arr)
		{
		    System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
		    return enc.GetString(arr);
		}

		//------------------------------------------------------------------------------------------------------------------------
//		public void HttpProxyStart (int port, string desthost)
//		{
//			string key = port+"/"+desthost;
//			if ( HttpProxyList.Contains(key) )
//				return;
//			else
//			{
//				Console.WriteLine ("HttpProxyStart: "+port+" => "+desthost);
//		        HttpListener listener = new HttpListener();
//		        listener.Prefixes.Add("http://*:"+port+"/");
//		        listener.Start();
//				new Thread (delegate () { HttpProxyListener(listener, desthost); }).Start();
//				HttpProxyList.Add(key, listener);
//			}
//		}
//
//		public void HttpProxyStop(int port, string desthost)
//		{
//			string key = port+"/"+desthost;
//			if ( HttpProxyList.Contains(key) )
//			{
//				HttpListener listener = (HttpListener)HttpProxyList[key];
//				listener.Stop();
//			}
//		}
//
//		private void HttpProxyListener (HttpListener listener, string desthost)
//		{
//	        Console.WriteLine("Listening...");
//			bool ok = true;
//	        while ( ok )
//	        {
//          		HttpListenerContext ctx = listener.GetContext();
//				try {
//					new Thread (delegate () { HttpProxyProcessRequest(ctx, desthost); }).Start();				
//				}
//				catch ( HttpListenerException e )
//				{
//					Console.WriteLine ("Listener Exception: " + e.Message);
//					ok = false;
//				}
//				//HttpProxyProcessRequest(ctx, desthost);
//	        }
//			Console.WriteLine ("Listening...done");
//		}
//
//		private void HttpProxyProcessRequest (HttpListenerContext ctx, string desthost)
//		{
//			HttpListenerRequest src_req = ctx.Request;
//			HttpListenerResponse src_res = ctx.Response;
//
//	        Console.WriteLine(src_req.HttpMethod + " " + src_req.Url + " ==> " + desthost);
//
//			Uri uri_in = src_req.Url;
//			UriBuilder uri_out = new UriBuilder(uri_in);
//			uri_out.Host = desthost;
//
//			try {
//		        WebRequest dst_req = HttpWebRequest.Create(uri_out.Uri);
//		        
//				dst_req.ContentType = src_req.ContentType;
//		        dst_req.Method = src_req.HttpMethod;
//				//dst_req.Proxy = new WebProxy("http://ww300\atw121y4:av1130xY+6@proxyfarm-at.3dns.netz.sbs.de:84");
//
//				byte[] byteArray = new byte[1024];
//
//				if ( src_req.HttpMethod == "GET" )
//				{
//					WebResponse wr = dst_req.GetResponse();
//
//					Stream str_in = wr.GetResponseStream();
//
//					Stream str_out = src_res.OutputStream;
//
//					try
//					{
//						Console.WriteLine ("read " + wr.ContentLength + " bytes...");
//						int c;
//						for ( long sum=0; sum < wr.ContentLength && (c=str_in.Read(byteArray, 0, 1024)) > 0; sum+=c )
//						{
//							str_out.Write(byteArray, 0, c);
//						}
//						Console.WriteLine ("wrote...done");
//					}
//					catch ( Exception e )
//					{
//						Console.WriteLine("read/write exception: " + e.Message);
//					}
//					Console.WriteLine ("done");
//					str_out.Flush();
//					str_out.Close();
//
//					wr.Close();
//				}
//				else
//				if ( src_req.HttpMethod == "POST" )
//				{
//					Console.WriteLine ("POST not implemented!");
//				}
//			}
//			catch ( Exception e )
//			{
//				Console.WriteLine ("Exception: " + e.Message);
//			}
//
//		}

		public static void Main (string[] args)
		{
			var srv = new RpcTools();
			srv.Start();
		}
	}
}
