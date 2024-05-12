using System;
using System.Collections;
using System.Collections.Generic;

namespace Roc.WCCOA
{
	public class WCCOAProxyRemote : MarshalByRefObject
	{
		public int AddClient ()
		{
			return WCCOAProxyServer.proxy.AddClient();
		}
		
		public bool DelClient (int id)
		{
			return WCCOAProxyServer.proxy.DelClient(id);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int TagQueryConnectSingle (int id, int key, string query, bool answer = false)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! TagQueryConnectSingle" + id + " " + key + " " + query);
			return WCCOAProxyServer.proxy.DpQueryConnectSingle(id, key, query, answer, true);
		}
		
		public int TagQueryDisconnect (int id, int key, string query)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! TagQueryDisconnect" + id + " " + key + " " + query);
			return WCCOAProxyServer.proxy.DpQueryDisconnect(id, key, query, true);
		}
		
		public int TagConnect (int id, int key, string[] dps, bool answer = false)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! TagConnect" + id + " " + key);
			return WCCOAProxyServer.proxy.DpConnect(id, key, dps, answer, true);
		}
		
		public int TagDisconnect (int id, int key, string[] dps)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! TagDisconnect" + id + " " + key);
			return WCCOAProxyServer.proxy.DpDisconnect(id, key, dps, true);
		}
	
		//------------------------------------------------------------------------------------------------------------------------
		public int DpQueryConnectSingle (int id, int key, string query, bool answer = false)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! DpQueryConnectSingle" + id + " " + key + " " + query);
			return WCCOAProxyServer.proxy.DpQueryConnectSingle(id, key, query, answer, false);
		}
		
		public int DpQueryDisconnect (int id, int key, string query)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! DpQueryDisconnect" + id + " " + key + " " + query);
			return WCCOAProxyServer.proxy.DpQueryDisconnect(id, key, query, false);
		}

		public int DpConnect (int id, int key, string[] dps, bool answer)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! dpConnect" + id + " " + key);
			return WCCOAProxyServer.proxy.DpConnect(id, key, dps, answer, false);
		}

		public int DpDisconnect (int id, int key, string[] dps)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! TagDisconnect" + id + " " + key);
			return WCCOAProxyServer.proxy.DpDisconnect(id, key, dps, false);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int dpGet (string[] dps, out ArrayList val)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! dpGet");
			return WCCOAProxyServer.proxy.dpGet(dps, out val);
		}

		public int dpSet (string[] dps, ArrayList val)
		{
			Console.WriteLine(DateTime.Now + " ProxyRemote! dpSet");
			return WCCOAProxyServer.proxy.dpSet(dps, val);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public WCCOAProxyRemote ()
		{
			Console.WriteLine ("WCCOAProxyRemote");
		}
	}
}

