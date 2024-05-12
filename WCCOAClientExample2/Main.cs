using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Roc.WCCOA;

namespace WCCOAClientExample2
{
	class MainClass
	{
		public static WCCOAClient client;
		static int counter = 0;
		static DateTime tStart;

		public static void Main (string[] args)
		{
			NetworkCredential CtrlConn = new NetworkCredential ("root", "");			
			string CtrlHost = "localhost";
			string ProxyHost = "localhost";

			if (args.Length >= 3) {
				CtrlHost = args [0];
				ProxyHost = args [1];
				CtrlConn = new NetworkCredential (args [2], args.Length == 3 ? "" : args [3]);
			}

			int ProxyClientPort = 8091;  // proxy tcp port for clients
			int ProxyRemotePort = 8092; // .net remoting
			
			// create server and client objects
			client = new WCCOAClient (ProxyHost, ProxyRemotePort, ProxyClientPort);
			
			// start and connect
			client.Start ();		
			client.Connect ();
			Thread.Sleep (1000); // wait until client id is registered
			
			client.DpQueryConnectSingle((object s, ArrayList a) => {
				WCCOABase.PrintArrayList(a);
				Statistics();
			}, "SELECT '_online.._value', '_online.._stime' FROM '*.**'");		

			/*
			client.DpConnect((object s, ArrayList a) => {
				WCCOABase.PrintArrayList(a);
			}, new string[] { "System1:ExampleDP_Arg1.", "System1:ExampleDP_Arg2." }, false);
			*/


			/*
			ArrayList dps = new ArrayList (new string[] { "System1:ExampleDP_Arg1.", "System1:ExampleDP_Arg2." });
			ArrayList val;
			while (true) {
				proxy.dpGet (dps, out val);
				WCCOABase.PrintArrayList(val);
				//val[0]=(Convert.ToInt32(val[0])+1).ToString();
				val[0] = ((double)val[0])+1;
				proxy.dpSet (dps, val);

				Thread.Sleep(1000);
			}
			*/
			
		}
		
		public static void Statistics()
		{
			if ( tStart == null )
				tStart = DateTime.Now;
			else
			{
				counter++;
				if ( DateTime.Now.Subtract(tStart).TotalSeconds >= 5 )
				{
					Console.WriteLine (counter / DateTime.Now.Subtract(tStart).TotalMilliseconds * 1000);
					tStart = DateTime.Now;
					counter = 0;
				}
			}
		}
	}
}
