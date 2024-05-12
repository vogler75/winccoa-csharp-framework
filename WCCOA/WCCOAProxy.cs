using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Roc.XmlRpc;

namespace Roc.WCCOA
{

	//------------------------------------------------------------------------------------------------------------------------
	public class WCCOAProxy
	{
		private string ProxyHost;

		private int ProxyTcpPort;   // tcp connections
		private int ProxySrvPort;   // incoming data from ctrl(OA server)

		private int ClientIdSeq = 0;

		private TcpListener ListenerServer;
		private TcpListener ListenerClients;
		
		private Thread		ThreadServer;
		private Thread      ThreadClients;	
		
		private int         ConnectId;

		private WCCOAXmlRpc XmlRpc;
		private WCCOAProxyWorker Worker;

		//------------------------------------------------------------------------------------------------------------------------
		public WCCOAProxy(WCCOAXmlRpc XmlRpc, int ProxySrvPort = 8090, int ProxyTcpPort = 8091, int ProxyRemotePort = 8092, string ProxyHost="")
		{
			this.ProxyHost = ProxyHost;

			this.ProxySrvPort = ProxySrvPort;
			this.ProxyTcpPort = ProxyTcpPort;
								
			this.XmlRpc = XmlRpc;

			this.Worker = new WCCOAProxyWorker();

			// .net remoting for remote proxy
			IChannel channel = new TcpChannel (ProxyRemotePort);
			ChannelServices.RegisterChannel (channel, false);			
			RemotingConfiguration.RegisterWellKnownServiceType (typeof(WCCOAProxyRemote), "WCCOAProxyRemote", WellKnownObjectMode.Singleton);
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void Start()
		{		
			try {
				// tcp listener/server wccoa
				ListenerServer = new TcpListener (System.Net.IPAddress.Any, this.ProxySrvPort);
				ListenerServer.Start ();
				
				ThreadServer = new Thread (new ThreadStart (TcpListenerServer));
				ThreadServer.Start ();
				Console.WriteLine ("WCCOA TCP Proxy Server running on port {0} ... ", this.ProxySrvPort);	
				
				// tcp listener/server clients
				ListenerClients = new TcpListener (System.Net.IPAddress.Any, this.ProxyTcpPort);
				ListenerClients.Start ();
				
				ThreadClients = new Thread (new ThreadStart (TcpListenerClients));
				ThreadClients.Start ();
				Console.WriteLine ("WCCOA TCP Proxy Clients running on port {0} ... ", this.ProxyTcpPort);	

				// xmlrpc server
				/*
				RpcServer = new XmlRpcServer (this.ProxyRpcPort);
				RpcServer.Add ("xoa", this);
				RpcServer.Start ();
				Console.WriteLine ("WCCOA XMLRPC Server running on port {0} ... ", ProxyRpcPort);
				*/
			} catch (Exception e) {
				Console.WriteLine ("An Exception Occurred while Listening Client:" + e.ToString ());
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public void Stop ()
		{
			ThreadServer.Abort();
			ListenerServer.Stop();
			
			ThreadClients.Abort();
			ListenerClients.Stop();
		}

		//------------------------------------------------------------------------------------------------------------------------
		public bool Connect ()
		{
			ArrayList i = new ArrayList ();
			ArrayList o;			

			i.Add (this.ProxyHost=="" ? "*" : this.ProxyHost); // host
			i.Add (this.ProxySrvPort); // port
			i.Add (true); // init

			if (XmlRpc.Call ("xoa.addClient", i, out o)) {
				ConnectId = (o.Count > 0 ? (int)o[0] : -1);
				Console.WriteLine (DateTime.Now + " xoa.addClient " + ConnectId);
				return true; 
			}
			else
				return false;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public bool Disconnect (int ConnectId = -1)
		{
			int id = (ConnectId == -1 ? this.ConnectId : ConnectId);
			ArrayList i = new ArrayList ();
			ArrayList o;
			i.Add (id);			
			if (XmlRpc.Call ("xoa.delClient", i, out o)) {
				if ( id == this.ConnectId )
					this.ConnectId = -1;
				Console.WriteLine (DateTime.Now + " xoa.delClient " + id);
				return true;
			}
			else {
				return false;
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		private void TcpListenerServer ()
		{
			while (true) 
			{
				TcpClient tcp = ListenerServer.AcceptTcpClient ();
				Thread thread = new Thread ( new ThreadStart(() => { ServerThread(tcp); } ));
				thread.Start();
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		private void TcpListenerClients ()
		{
			while (true) 
			{
				TcpClient tcp = ListenerClients.AcceptTcpClient ();
				new WCCOAConnection(tcp, this.Worker, false);
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		private void ServerThread (TcpClient tcp)
		{
			int ret;
			Console.WriteLine ("ServerRequests...");
			WCCOAXmlTcp xml = new WCCOAXmlTcp (this, tcp, false);
			XmlRpcRequest request;
			while ((ret=xml.ReadRequest(out request, 10000))>0) {
				if ( ret == 1 )
					WCCOAXmlTcp.InvokeRequest(this, this.Worker, request.MethodName, request.Params);
			};
			Console.WriteLine ("ServerRequests...ended");
			xml.Close();
		}
				
		//------------------------------------------------------------------------------------------------------------------------

		public int AddClient ()
		{
			return ++ClientIdSeq;			
		}
		
		public bool DelClient (int id)
		{
			if (Worker.Clients.ContainsKey (id)) {
				WCCOAConnection cc = Worker.Clients [id] as WCCOAConnection;
				cc.Stop ();
				return true;
			} else {
				return false;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		// Tag Functions
		//------------------------------------------------------------------------------------------------------------------------
		
		//------------------------------------------------------------------------------------------------------------------------
		public int DpQueryConnectSingle (int id, int key, string query, bool answer, bool tag)
		{
			Console.WriteLine ("DpQueryConnectSingle " + id + " " + key + " " + query + " " + tag);

			WCCOAConnection cc;
			if (Worker.Clients.ContainsKey (id)) {
				cc = Worker.Clients [id];
			} else {
				Console.WriteLine ("DpQueryConnectSingle client not connected!");
				return -1; // client not connected
			}

			if (Worker.DpQueryConnects.ContainsKey (key)) {
				// add client to connect
				Worker.DpQueryConnects[key].Add(cc);
				return 2;
			} else 	{
				// new connect
				ArrayList i = new ArrayList ();
				ArrayList o;			
				i.Add (ConnectId);
				i.Add (key);
				i.Add (tag ? "SELECT '_online.._value', '_online.._stime', '_online.._invalid', '_online.._default' " + query : query);
				i.Add (answer);
				
				if ( XmlRpc.Call ("xoa." + (tag?"tag":"dp") + "QueryConnectSingle", i, out o) ) {
					// add new connection item to query connects
					Worker.DpQueryConnects.Add (key, new ProxyDpQueryConnectItem(this, key, query, tag, cc));
					return 1; 
				} else {
					return -2;
				}
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int DpQueryDisconnect (int id, int key, string query, bool tag)
		{
			Console.WriteLine ("DpQueryDisconnect " + id + " " + key + " " + query + " " + tag);

			WCCOAConnection cc = null;
			ProxyDpQueryConnectItem ci = null;

			if (id != 0) {
				if (Worker.Clients.ContainsKey (id)) {
					cc = Worker.Clients [id]; // client not connected
				} else {
					return -1;
				}
			}

			if (Worker.DpQueryConnects.ContainsKey (key)) {
				ci = Worker.DpQueryConnects [key];
			} else {
				return -2; // query not connected
			}

			if ( cc == null || ci.Clients.Contains(cc) ) {
				ci.Remove(cc); // remove client from connect list
				if ( ci.Clients.Count == 0 ) { // last client? then remove connect				
					ArrayList i = new ArrayList ();
					ArrayList o;			
					i.Add (ConnectId);
					i.Add (key);
					i.Add (tag ? "SELECT '_online.._value', '_online.._stime', '_online.._invalid', '_online.._default' " + query : query);

					if (XmlRpc.Call ("xoa." + (tag?"tag":"dp") + "QueryDisconnect", i, out o)) {
						Worker.DpQueryConnects.Remove(key); // remove connect
						return 1;
					} else {				
						return -3;
					}
				} else {
					return 2;
				}
			} 
			return 0;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int DpConnect (int id, int key, string[] dps, bool answer, bool tag)
		{
			WCCOAConnection cc;
			if (Worker.Clients.ContainsKey (id)) {
				cc = Worker.Clients [id];
			} else {
				return -1; // client not connected
			}
			
			if (Worker.DpConnects.ContainsKey (key)) {
				// add client to connect
				Worker.DpConnects [key].Add (cc);
				return 2;
			} else {
				ArrayList i = new ArrayList ();
				ArrayList o;			
				i.Add (ConnectId);
				i.Add (key);
				i.Add (dps);
				i.Add (answer);
				
				if (XmlRpc.Call ("xoa." + (tag?"tag":"dp") + "Connect", i, out o)) {
					// add new connection item to query connects
					Worker.DpConnects.Add (key, new ProxyDpConnectItem(this, key, dps, tag, cc));
					return 1; 
				} else {
					return -2;
				}
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public int DpDisconnect (int id, int key, string[] dps, bool tag)
		{
			WCCOAConnection cc = null;
			ProxyDpConnectItem ci;

			if (id != 0) {
				if (Worker.Clients.ContainsKey (id)) {
					cc = Worker.Clients [id]; // client not connected
				} else {
					return -1;
				}
			}

			if (Worker.DpConnects.ContainsKey (key)) {
				ci = Worker.DpConnects [key];
			} else {
				return -2; // query not connected
			}
			
			if ( cc == null || ci.Clients.Contains(cc) ) {
				ci.Remove(cc); // remove client from connect list
				if ( ci.Clients.Count == 0 ) { // last client? then remove connect				
					ArrayList i = new ArrayList ();
					ArrayList o;			
					i.Add (ConnectId);
					i.Add (key);
					i.Add (dps);
					if (XmlRpc.Call ("xoa." + (tag?"tag":"dp") + "Disconnect", i, out o)) {
						Worker.DpConnects.Remove(key); // remove connect
						return 1;
					} else {
						return -3;
					}
				} else {
					return 2;
				}
			} 
			return 0;
		}

		//------------------------------------------------------------------------------------------------------------------------
		// DP Functions
		//------------------------------------------------------------------------------------------------------------------------

		//------------------------------------------------------------------------------------------------------------------------
		public int dpGet (string[] dps, out ArrayList val)
		{
			ArrayList i = new ArrayList ();
			i.Add (dps);
			if (XmlRpc.Call ("pvss.db.getValues", i, out val)) {
				return 1;
			} else {
				return -1;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int dpSet (string[] dps, ArrayList val)
		{
			ArrayList i = new ArrayList ();
			i.Add (dps);
			i.Add (val);
			ArrayList o;
			if (XmlRpc.Call ("pvss.db.setValues", i, out o)) {
				WCCOABase.PrintArrayList(o);
				return 1;
			} else {
				return -1;
			}			
		}
	}	
}

