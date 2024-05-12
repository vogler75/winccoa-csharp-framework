using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Roc.WCCOA;



namespace WCCOANetServer
{
	class MainClass
	{		
		public static void Main (string[] args)
		{
			NetworkCredential CtrlLogon = new NetworkCredential ("root", "");

			string CtrlHost = "127.0.0.1";
			string ProxyHost = "127.0.0.1";

			if (args.Length >= 3) {
				CtrlHost = args[0];
				ProxyHost = args[1];
				CtrlLogon = new NetworkCredential(args[2], args.Length==3 ? "" : args[3]);
			}

			int CtrlPort = 8080;  // oa ctrl xmlrpc

			int ProxyServerPort = 8090;  // proxy tcp port for server
			int ProxyClientPort = 8091;  // proxy tcp port for clients
			int ProxyRemotePort = 8092; // .net remoting

			// create proxy server
			WCCOAProxyServer.proxy = new WCCOAProxy (
				new WCCOAXmlRpc (CtrlHost, CtrlPort, "RPC2", CtrlLogon), 
				ProxyServerPort, ProxyClientPort, ProxyRemotePort, ProxyHost );

			// start proxy server
			WCCOAProxyServer.proxy.Start ();
			WCCOAProxyServer.proxy.Connect ();
		}	
	}
}
