using System;
using System.Collections;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Roc.WCCOA
{
	//------------------------------------------------------------------------------------------------------------------------
	public delegate void WCCOACallback(object sender, ArrayList args);

	public class WCCOAClient
	{
		int ProxyTcpPort;
		TcpClient tcp;

		string ProxyHost;
		WCCOAProxyRemote Proxy;
		
		public int ConnectId = 0;

		WCCOAConnection Client;
		WCCOAClientWorker Worker;		
				
		//------------------------------------------------------------------------------------------------------------------------
		public WCCOAClient (string ProxyHost, int ProxyRemotePort, int ProxyTcpPort = 8091)
		{
			this.ProxyHost = ProxyHost;
			this.ProxyTcpPort = ProxyTcpPort;

			Worker = new WCCOAClientWorker();		

			// .net remoting to proxy
			TcpChannel chan = new TcpChannel ();
			ChannelServices.RegisterChannel (chan, false);

			// create remote proxy object
			Proxy = (WCCOAProxyRemote)Activator.GetObject (
				typeof(WCCOAProxyRemote), "tcp://" + ProxyHost + ":" + ProxyRemotePort + "/WCCOAProxyRemote");
		}

		//------------------------------------------------------------------------------------------------------------------------
		public void Start ()
		{
			tcp = new TcpClient();
			tcp.Connect(ProxyHost, ProxyTcpPort);
			Client = new WCCOAConnection(tcp, Worker, true);
		}


		//------------------------------------------------------------------------------------------------------------------------
		public bool Connect ()
		{
			ArrayList i = new ArrayList ();
			Console.WriteLine (DateTime.Now + " xoa.AddClient...");
			if ( (ConnectId = Proxy.AddClient()) > 0 ) {
				Console.WriteLine (DateTime.Now + " xoa.AddClient " + ConnectId);
				i.Add(ConnectId);
				Client.ConnectId = ConnectId;
				Client.AddWork ("ClientConnectCB", i); // send id to server (from tcp connection)			
				return true; 
			} else {
				Console.WriteLine (DateTime.Now + " xoa.AddClient BAD");
				return false;
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public bool Disconnect (int ConnectId = -1)
		{
			int id = (ConnectId == -1 ? this.ConnectId : ConnectId);
			if (Proxy.DelClient(id)) {
				if ( id == this.ConnectId )
				{
					ArrayList i = new ArrayList ();
					i.Add (id);
					Client.AddWork("ClientDisconnectCB", i); // send id to server (from tcp connection)
					this.ConnectId = -1;
				}
				Console.WriteLine (DateTime.Now + " xoa.DelClient " + id);
				return true;
			}
			else {
				return false;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int dpGet (string[] dps, out ArrayList val)
		{
			return Proxy.dpGet(dps, out val);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int dpSet (string[] dps, ArrayList val)
		{
			return Proxy.dpSet(dps, val);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int DpConnect (WCCOACallback cb, string[] dps, bool answer = false)
		{
			int key = dps.GetHashCode ();
			
			if (Worker.DpConnects.ContainsKey (key)) {
				Worker.DpConnects [key].Callbacks += cb;
				return 2;
			} else {		
				if ( Proxy.DpConnect(this.ConnectId, key, dps, answer) > 0 ) {
					Worker.DpConnects.Add (key, new DpConnectItem (dps, cb, Client));
					return 1; 
				} else {
					return -1;
				}
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public int DpQueryConnectSingle (WCCOACallback cb, string query, bool answer = false)
		{
			int key = query.GetHashCode ();
			
			if (Worker.DpQueryConnects.ContainsKey (key)) {
				Worker.DpQueryConnects [key].Callbacks += cb;
				return 2;
			} else {		
				if ( Proxy.DpQueryConnectSingle(this.ConnectId, key, query, answer) > 0 ) {
					Worker.DpQueryConnects.Add (key, new DpQueryConnectItem (query, cb, Client));
					return 1; 
				} else {
					return -1;
				}
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int TagQueryConnectSingle (WCCOATagList list, string query, bool answer = false)
		{
			int key = query.GetHashCode ();

			if (Worker.TagQueryConnects.ContainsKey (key)) {
				Worker.TagQueryConnects [key].Lists.Add(list);
				return 2;
			} else {
				if ( Proxy.TagQueryConnectSingle(this.ConnectId, key, query, answer) > 0 ) {
					Worker.TagQueryConnects.Add (key, new TagQueryConnectItem(query, list, Client));
					return 1;
				} else {
					return -1;
				}
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public int TagQueryDisconnect (WCCOATagList list, string query)
		{
			int key = query.GetHashCode ();
			
			if (Worker.TagQueryConnects.ContainsKey (key)) {
				if (Worker.TagQueryConnects [key].Lists.Contains (list))
					Worker.TagQueryConnects [key].Lists.Remove (list);
				if (Worker.TagQueryConnects [key].Lists.Count > 0) {
					return 2;
				} else {
					if ( Proxy.TagQueryDisconnect(this.ConnectId, key, query) > 0 ) {
						Worker.TagQueryConnects.Remove (key);
						return 1;
					} else {
						return -1;
					}
				} 
			} else {
				return -2;
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public int TagConnect (WCCOATagList list, bool answer = false)
		{
			int key = list.GetHashCode ();

			if (Worker.TagConnects.ContainsKey (key)) {
				Worker.TagConnects [key].Lists.Add (list);
				return 2;
			} else {		
				int i=0;
				string[] dps = new string[list.TagList.Count*4];
				foreach ( WCCOATag tag in list.TagList ) {
					dps[i++]=(tag.DpName + ":_online.._value");
					dps[i++]=(tag.DpName + ":_online.._stime");
					dps[i++]=(tag.DpName + ":_online.._invalid");
					dps[i++]=(tag.DpName + ":_online.._default");
				}
				if ( Proxy.TagConnect(this.ConnectId, key, dps, answer) > 0 ) {
					Worker.TagConnects.Add (key, new TagConnectItem (list, Client));
					return 1; 
				} else {
					return -1;
				}
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public int TagDisconnect (WCCOATagList list)
		{
			int key = list.GetHashCode ();

			if (Worker.TagConnects.ContainsKey (key)) {
				if (Worker.TagConnects [key].Lists.Contains (list))
					Worker.TagConnects [key].Lists.Remove (list);
				if (Worker.TagConnects [key].Lists.Count > 0) {
					return 2;
				} else {
					int i=0;
					string[] dps = new string[list.TagList.Count*4];
					foreach ( WCCOATag tag in list.TagList ) {
						dps[i++]=(tag.DpName + ":_online.._value");
						dps[i++]=(tag.DpName + ":_online.._stime");
						dps[i++]=(tag.DpName + ":_online.._invalid");
						dps[i++]=(tag.DpName + ":_online.._default");
					}
					if ( Proxy.TagDisconnect(this.ConnectId, key, dps) > 0 ) {
						Worker.TagConnects.Remove (key);
						return 1; 
					} else {
						return -1;
					}
				}
			} else {
				return -2;
			}
		}
	}
}

