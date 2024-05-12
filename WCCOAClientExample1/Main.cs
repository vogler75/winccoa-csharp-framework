using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Roc.WCCOA;

namespace WCCOANetClient
{
	class MainClass
	{
		static WCCOAXmlRpc xmlrpc;
		static WCCOAClient client;

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
			
			int CtrlPort = 8080;  // oa ctrl xmlrpc
			
			int ProxyClientPort = 8091;  // proxy tcp port for clients
			int ProxyRemotePort = 8092; // .net remoting
	
			// create server and client objects
			xmlrpc = new WCCOAXmlRpc (CtrlHost, CtrlPort, "RPC2", CtrlConn);
			client = new WCCOAClient (ProxyHost, ProxyRemotePort, ProxyClientPort);

			// start and connect
			client.Start ();		
			client.Connect ();
			Thread.Sleep (1000); // wait until client id is registered

			//Console.WriteLine ("press: q...query connect, t...tag connect, x...both");
			//ConsoleKeyInfo c=Console.ReadKey();

			TagConnect ();
			//QueryConnect ();
		}

		public static void TagConnect()
		{
			// create a tag 
			WCCOATagList list = new WCCOATagList(xmlrpc);
			
			// add tag to the  list
			var t1 = new WCCOATag(xmlrpc, "System1:ExampleDP_Trend1.");
			var t2 = new WCCOATag(xmlrpc, "System1:ExampleDP_Arg1.");
			list.Add(t1);
			list.Add(t2);
			
			// callbacks
			list.AddDataChangedAction((l, f) => {
				Console.WriteLine ("TC-ListCB: " + l.TagList.Count);
				for ( int i=0; i<l.TagList.Count; i++ )
					Console.WriteLine ("TC-ListCB: " + l.TagList[i].ToString());
			});

			/*
			t1.AddDataChangedAction((t, f) => { 
				Console.WriteLine ("TC-TagCB1: " + t.DpName + " => " + t.Value.Value.ToString()); 
			});
			
			t2.AddDataChangedAction((t, f) => { 
				Console.WriteLine ("TC-TagCB2: " + t.DpName + " => " + t.Value.Value.ToString()); 
			});
			*/
			
			// connect
			int ret = client.TagConnect(list, false);
			Console.WriteLine("TagConnect " + ret);		
		}
		
		public static void QueryConnect()
		{
			string query="FROM '*.**'";
			
			WCCOATagList list = new WCCOATagList(xmlrpc);
			//var t1 = new WCCOATag(server, "System1:ExampleDP_Trend1.");
			//list.Add(t1);
			
			//t1.AddDataChangedAction((t, f) => { 
			//	Console.WriteLine ("QC-TagCB: " + t.DpName + " => " + t.Value.Value.ToString()); 
			//});
			
			list.AddDataChangedAction((l, f) => { 
				//Console.WriteLine ("QC-ListCB: " + l.TagList.Count); 
				lock ( l.TagListChanged )
				if (l.TagListChanged.Count > 0)
				{

					//Console.WriteLine(l.TagListChanged[0].ToString());
					//Console.WriteLine ("QC-ListCB: " + l.TagListChanged.Count + " " + l.TagListChanged[0].ToString());
				}
			});
			
			// query connect single to tag
			int ret = client.TagQueryConnectSingle(list, query, false);
			Console.WriteLine("TagQueryConnectSingle " + ret);		
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
