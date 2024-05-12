using System;
using System.Collections;
using System.Collections.Generic;

namespace Roc.WCCOA
{
	internal class ProxyDpQueryConnectItem : WCCOAConnectItem
	{
		WCCOAProxy Proxy;
		int Key;
		string Query;
		public bool Tag;
		
		public ProxyDpQueryConnectItem(WCCOAProxy Proxy, int Key, string Query, bool Tag, WCCOAConnection Client) : base(Client) 
		{
			this.Proxy = Proxy;
			this.Key = Key;
			this.Query = Query;
			this.Tag = Tag;
		}
		public override void Disconnect ()
		{
			Console.WriteLine ("DpQueryConnectItem " + Key + " disconnect.");
			int ret = Proxy.DpQueryDisconnect(0, Key, Query, Tag);
			Console.WriteLine ("DpQueryConnectItem " + Key + " disconnect. " + ret);
			Query = null;
			Clients.Clear();
		}
		public override string ToString ()
		{
			return Query + " " + this.Clients.Count;
		}
	}
	
	internal class ProxyDpConnectItem : WCCOAConnectItem
	{
		WCCOAProxy Proxy;
		int Key;
		string[] Dps;
		public bool Tag;
		
		public ProxyDpConnectItem(WCCOAProxy Proxy, int Key, string[] Dps, bool Tag, WCCOAConnection Client) : base(Client) 
		{
			this.Proxy = Proxy;
			this.Key = Key;
			this.Dps = Dps;
			this.Tag = Tag;
		}
		public override void Disconnect ()
		{
			Console.WriteLine ("DpConnectItem " + Key + " disconnect.");
			int ret = Proxy.DpDisconnect(0, Key, Dps, Tag);
			Console.WriteLine ("DpConnectItem " + Key + " disconnect. " + ret);
			Dps = null;
			Clients.Clear();
		}
		public override string ToString ()
		{
			return "TagConnectItem" + " " + this.Clients.Count;
		}
	}	

	//------------------------------------------------------------------------------------------------------------------------
	public class WCCOAProxyWorker
	{
		internal Dictionary<int, WCCOAConnection> Clients; 
		
		internal Dictionary<int, ProxyDpQueryConnectItem> DpQueryConnects;
		internal Dictionary<int, ProxyDpConnectItem> DpConnects;	
			
		//------------------------------------------------------------------------------------------------------------------------
		public WCCOAProxyWorker()
		{
			this.Clients = new Dictionary<int, WCCOAConnection>();
			this.DpQueryConnects = new Dictionary<int, ProxyDpQueryConnectItem>();
			this.DpConnects = new Dictionary<int, ProxyDpConnectItem>();	
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// TCP Callbacks
		//------------------------------------------------------------------------------------------------------------------------
		
		//------------------------------------------------------------------------------------------------------------------------
		public bool Alive (object sender, int id, string msg)
		{
			bool alive;
			if (Clients.ContainsKey (id) && ((WCCOAConnection)Clients[id]).IsAlive ()) 
				alive = true;
			else {
				alive = false;
			}
			Console.WriteLine(DateTime.Now + " Alive " + id + " " + msg + " => " + alive);
			return alive;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void ClientConnectCB (object sender, int id)
		{
			Console.WriteLine ("ClientConnectionCB: " + id);
			WCCOAConnection cc = sender as WCCOAConnection;
			if ( cc != null ) 
			{
				Console.WriteLine ("ClientConnectionCB: " + id + " found client connection!");
				if ( Clients.ContainsKey(id) )
				{
					cc.Stop();
					Clients.Remove(id);
				}
				Clients.Add(id, cc);
				cc.ConnectId = id;
			}
			else
				Console.WriteLine ("ClientConnectionCB: sender is not a WCCOAClientConnection!");
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void ClientDisconnectCB (object sender, int id)
		{
			WCCOAConnection cc = sender as WCCOAConnection;
			if ( cc != null ) 
			{
				if ( Clients.ContainsKey(id) )
					Clients.Remove(id);
			}
			else
				Console.WriteLine ("ClientDisconnectionCB: sender is not a WCCOAClientConnection!");
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void DpQueryConnectCB (object sender, int id, int key, ArrayList dps)
		{
			QueryConnectCB (sender, id, key, dps, false);
		}

		public void TagQueryConnectCB (object sender, int id, int key, ArrayList dps)
		{
			QueryConnectCB (sender, id, key, dps, true);
		}

		private void QueryConnectCB (object sender, int id, int key, ArrayList dps, bool tag)
		{
			//Console.WriteLine ("TagQueryConnectSingleCB", id, key);
			if (DpQueryConnects.ContainsKey (key)) {
				ArrayList Params = new ArrayList ();
				Params.Add (id);
				Params.Add (key);
				Params.Add (dps);
				
				//Console.WriteLine ("TagQueryConnectgSingle");
				//WCCOABase.PrintArrayList(dps);
				string cb = tag ? "TagQueryConnectCB" : "DpQueryConnectCB";
				foreach ( WCCOAConnection cc in DpQueryConnects[key].Clients ) {
					cc.AddWork (new WCCOAMethod (cb, Params));
				}
			} else {
				Console.WriteLine (DateTime.Now + " query hot link, but no client connected to id => remove connect (TODO).");
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void DpConnectCB (object sender, int id, int key, ArrayList dps, ArrayList val)
		{
			ConnectCB (sender, id, key, dps, val, false);
		}

		public void TagConnectCB (object sender, int id, int key, ArrayList dps, ArrayList val)
		{
			ConnectCB (sender, id, key, dps, val, true);
		}
	
		private void ConnectCB (object sender, int id, int key, ArrayList dps, ArrayList val, bool tag=false)
		{
			//Console.WriteLine ("TagConnectCB", id, key);
			if (DpConnects.ContainsKey (key)) {
				ArrayList Params = new ArrayList ();
				Params.Add (id);
				Params.Add (key);
				Params.Add (dps);
				Params.Add (val);
				
				//Console.WriteLine ("TagConnectCB");
				//WCCOABase.PrintArrayList(dps);
				//WCCOABase.PrintArrayList(val);
				string cb = tag ? "TagConnectCB" : "DpConnectCB";
				foreach ( WCCOAConnection cc in DpConnects[key].Clients ) {
					cc.AddWork (new WCCOAMethod (cb, Params));
				}
			} else {
				Console.WriteLine (DateTime.Now + " query hot link, but no client connected to id => remove connect (TODO).");
			}
		}
	}	
}

