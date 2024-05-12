using System;
using System.Collections.Generic;

namespace Roc.WCCOA
{
	//------------------------------------------------------------------------------------------------------------------------
	public abstract class WCCOAConnectItem 
	{
		public List<WCCOAConnection> Clients;
		
		public WCCOAConnectItem() { 
			Clients = new List<WCCOAConnection>();
		}
		
		public WCCOAConnectItem(WCCOAConnection Client) : this ( ) {
			Add(Client);
		}
		
		public bool Add (WCCOAConnection Client)
		{
			if (! Clients.Contains (Client)) {
				Clients.Add (Client);
				Client.Connects.Add(this);
				return true;
			} else {
				return false;
			}
		}
		
		public bool Remove (WCCOAConnection Client)
		{
			if (Clients.Contains (Client)) {
				Clients.Remove (Client);
				return true;
			} else {
				return false;
			}
		}
		
		public abstract void Disconnect();

		public override abstract string ToString();
		
		public void Check()
		{
			if ( Clients.Count == 0 )
				Disconnect();
		}
	}
}

