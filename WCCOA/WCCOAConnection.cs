using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using Roc.XmlRpc;

namespace Roc.WCCOA
{
	//------------------------------------------------------------------------------------------------------------------------
	public class WCCOAMethod {
		public string MethodName = null;
		public ArrayList Params = null;

		public WCCOAMethod (string MethodName, ArrayList Params)
		{
			this.MethodName = MethodName;
			this.Params = Params;
		}
	}	

	//------------------------------------------------------------------------------------------------------------------------
	public class WCCOAConnection {
		public int ConnectId;
		public List<WCCOAConnectItem> Connects;

		TcpClient tcp;
		WCCOAXmlTcp xmltcp;

		bool tcpok;
		DateTime lasttime;
		AutoResetEvent writesig = new AutoResetEvent(false);

		List<WCCOAMethod> queue;
		Thread thread;

		Object rpc;

		bool client; // am i a client? (false=i am a server)

		//------------------------------------------------------------------------------------------------------------------------
		public WCCOAConnection (TcpClient tcp, Object rpc, bool client)
		{
			this.ConnectId = -1;
			this.Connects = new List<WCCOAConnectItem>();

			this.tcp = tcp;
			this.rpc = rpc;
			this.client = client;

			this.queue= new List<WCCOAMethod>();

			this.xmltcp = new WCCOAXmlTcp (rpc, tcp, client);

			this.tcpok = true;
			this.lasttime = DateTime.MinValue;

			this.thread = new Thread (new ThreadStart (WorkThread));
			this.thread.Start ();
		}

		//------------------------------------------------------------------------------------------------------------------------
		public bool IsAlive ()
		{
			return thread.IsAlive;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public void Stop ()
		{
			tcpok=false;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public bool AddWork (string MethodName, ArrayList Params)
		{
			return AddWork(new WCCOAMethod(MethodName, Params));
		}


		//------------------------------------------------------------------------------------------------------------------------
		public bool AddWork (WCCOAMethod w)
		{
			if ( ! thread.IsAlive ) return false;
			lock (queue) {
				//Console.WriteLine ("AddWorkLoad " + w.MethodName);
				queue.Add(w);
			}
			writesig.Set();
			return true;
		}

		//------------------------------------------------------------------------------------------------------------------------
		private void ReadWriteThread ()
		{
			int ret, i, j;
			DateTime t;
			XmlRpcRequest request;
			
			while (tcpok) 
			{
				i=0;
				t=DateTime.Now;
				do
				{
					ret=xmltcp.ReadRequest (out request, 100);
					if ( ret == 1 )
					{
						i++;
						//Console.WriteLine ("Read");
						//WCCOABase.PrintArrayList(request.Params);
						WCCOAXmlTcp.InvokeRequest (this, rpc, request.MethodName, request.Params);
						lasttime = DateTime.Now;
					}
					if ( ret == -2 )
						tcpok = false;
				}
				while ( ret == 1 && DateTime.Now.Subtract(t).TotalSeconds <= 1 );

				if (writesig.WaitOne (100) || queue.Count > 0 ) 
				{
					if ( queue.Count == 0 ) break;

					i=0;
					j=queue.Count;
					t=DateTime.Now;
					do
					{
						//Console.WriteLine("Write");
						//WCCOABase.PrintArrayList(queue [i].Params);
						ret = xmltcp.WriteRequest (queue [i].MethodName, queue [i].Params);
						//Debug.Write ("WriteThread " + queue [i].MethodName + " => " + ret);
						switch ( ret )
						{
						case 1: 
							i++;
							lasttime = DateTime.Now;
							break;
						case -1:
							tcpok=false;
							break;
						}
					}
					while ( i<j && ret == 1 && DateTime.Now.Subtract(t).TotalSeconds <= 1);

					lock (queue) 
					{
						if (i > 0) 
							queue.RemoveRange (0, i);
					}
				}
			}
		}


		//------------------------------------------------------------------------------------------------------------------------
		private void WorkThread ()
		{
			int i;
			Console.WriteLine ("ClientWorkThread");

			tcpok = true;
			Thread rw = new Thread (new ThreadStart (ReadWriteThread));
			rw.Start ();

			var param = new ArrayList(); 
			param.Add (ConnectId);
			param.Add ("");
			var alive = new WCCOAMethod("Alive", param);
			while (tcpok) {
				Thread.Sleep(1000);
				if ( client && DateTime.Now.Subtract(lasttime).TotalSeconds > 3 && queue.Count == 0 )
				{
					param[0] = ConnectId;
					param[1] = Dns.GetHostName() + "/" + Process.GetCurrentProcess().ProcessName + "/" + DateTime.Now.ToString();
					AddWork(alive);
				}
			}
			Console.WriteLine ("ClientWorkThread: write failed, ended! " + Connects.Count + " " + tcpok);

			for ( i=0; i<Connects.Count; i++ ) {
				Connects[i].Remove(this);
				Connects[i].Check();
			}
			Console.WriteLine ("ClientWorkThread: removed connects from client!");
		}
	}
}

