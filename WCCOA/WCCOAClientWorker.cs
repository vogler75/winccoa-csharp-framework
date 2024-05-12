using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Roc.WCCOA
{

	internal class TagQueryConnectItem : WCCOAConnectItem
	{
		private string Query;
		public List<WCCOATagList> Lists;
		public TagQueryConnectItem(string Query, WCCOATagList List, WCCOAConnection Client) : base(Client) 
		{
			this.Query = Query;
			this.Lists = new List<WCCOATagList>();
			this.Lists.Add(List);
		}
		public override void Disconnect ()
		{
			Lists.Clear();
		}
		public override string ToString ()
		{
			return Query + " " + this.Clients.Count;
		}
	}
	
	internal class TagConnectItem : WCCOAConnectItem
	{
		public List<WCCOATagList> Lists;
		public TagConnectItem(WCCOATagList List, WCCOAConnection Client) : base(Client) 
		{
			this.Lists = new List<WCCOATagList>();
			this.Lists.Add(List);
		}
		public override void Disconnect ()
		{
			Lists.Clear();
		}
		public override string ToString ()
		{
			return "TagConnectItem" + " " + this.Clients.Count;
		}
	}	
	
	internal class DpQueryConnectItem : WCCOAConnectItem
	{
		private string Query;
		public event WCCOACallback Callbacks;
		
		public DpQueryConnectItem(string Query, WCCOACallback Callback, WCCOAConnection Client) : base(Client) 
		{
			this.Query = Query;
			this.Callbacks += Callback;
		}
		public override void Disconnect ()
		{
			Callbacks = null;
		}
		public override string ToString ()
		{
			return Query + " " + this.Clients.Count;
		}
		public void Changed (ArrayList val)
		{
			Callbacks(this, val);
		}
	}
	
	internal class DpConnectItem : WCCOAConnectItem
	{
		private string[] Dps;			
		public event WCCOACallback Callbacks;
		
		public DpConnectItem(string[] Dps, WCCOACallback Callback, WCCOAConnection Client) : base(Client) 
		{
			this.Dps = Dps;
			this.Callbacks += Callback;
		}
		public override void Disconnect ()
		{
			Callbacks = null;
		}
		public override string ToString ()
		{
			return "DpConnectItem" + " " + this.Clients.Count;
		}
		public void Changed (ArrayList val)
		{
			ArrayList args = new ArrayList();
			args.Add(Dps);
			args.Add(val);
			Callbacks(this, args);
		}
	}	

	public class WCCOAClientWorker
	{
		internal Dictionary<int, TagQueryConnectItem> TagQueryConnects;
		internal Dictionary<int, TagConnectItem> TagConnects;
		
		internal Dictionary<int, DpConnectItem> DpConnects;
		internal Dictionary<int, DpQueryConnectItem> DpQueryConnects;

		public WCCOAClientWorker ()
		{
			this.TagQueryConnects = new Dictionary<int, TagQueryConnectItem>();
			this.TagConnects = new Dictionary<int, TagConnectItem>();
			
			this.DpConnects = new Dictionary<int, DpConnectItem>();
			this.DpQueryConnects = new Dictionary<int, DpQueryConnectItem>();
		}


		//------------------------------------------------------------------------------------------------------------------------
		public bool Alive (object sender, int id, string msg)
		{
			Console.WriteLine(DateTime.Now + " Alive " + id + " " + msg);
			return true;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void TagQueryConnectSingleCB (object sender, int id, int key, ArrayList dps)
			/*
		   C:2
		     V[ 0]:-496764408
		     C:12
		       C:5
		         V[ 0]:
		         V[ 1]::_online.._value
		         V[ 2]::_online.._stime
		         V[ 3]::_online.._invalid
		         V[ 4]::_online.._default
		       C:5
		         V[ 0]:System1:_Stat_event_0.SndTotal
		         V[ 1]:85
		         V[ 2]:2012.12.26 13:48:25.529
		         V[ 3]:False
		         V[ 4]:False
		       C:5
		         V[ 0]:System1:_Stat_event_0.RcvTotal
		         V[ 1]:78
		         V[ 2]:2012.12.26 13:48:25.529
		         V[ 3]:False
		         V[ 4]:False
				...
		 */
		{
			//WCCOABase.PrintArrayList(dps, " ");
			if (TagQueryConnects.ContainsKey (key)) {	
				foreach ( WCCOATagList list in TagQueryConnects[key].Lists ) {
					lock ( list.TagIndxChanged ) {
						list.TagListChanged.Clear();
						for (int i=1; i<dps.Count; i++) {
							ArrayList val = dps [i] as ArrayList;
							string dp = (String)val [0];
							
							WCCOATag tag = list.Contains (dp);
							if (tag == null)
								tag = list.Add (dp);
							
							tag.UpdateValue (val [1], ref tag.Value.Value);
							tag.UpdateValue (val [2], ref tag.Value.Time);
							tag.UpdateValue (val [3], ref tag.Value.Invalid);
							tag.UpdateValue (val [4], ref tag.Value.Default);
							
							list.TagListChanged.Add(tag);					
							tag.UpdateData (null, '-', DateTime.Now);
						}
					}
					list.UpdateData (DateTime.Now);
				}
			} else {
				Console.WriteLine (DateTime.Now + " TagQueryConnectSingleCB with unkown key " + key + "!");
			}
		}		
		
		//------------------------------------------------------------------------------------------------------------------------
		public void TagConnectCB (object sender, int id, int key, ArrayList dps, ArrayList val)
			/*
			 C:1
			   C:2
			     C:1
			       V[ 0]:System1:ExampleDP_Trend1
			     C:4
			       V[ 0]:2
			       V[ 1]:2012.12.26 11:02:32.974
			       V[ 2]:False
			       V[ 3]:False
		 */
		{
			//WCCOABase.PrintArrayList(dps, " ");
			//WCCOABase.PrintArrayList(val, " ");
			if (TagConnects.ContainsKey (key)) {							
				foreach ( WCCOATagList list in TagConnects[key].Lists ) 
				{
					lock ( list.TagListChanged )
					{
						list.TagListChanged.Clear();
						int j = 0;
						for (int i=0; i<dps.Count; i++) {
							string dp = (String)dps [i];
							
							WCCOATag tag = list.Contains (dp);
							
							if (tag == null)
								tag = list.Add (dp);
							
							tag.UpdateValue (val [j + 0], ref tag.Value.Value);
							tag.UpdateValue (val [j + 1], ref tag.Value.Time);
							tag.UpdateValue (val [j + 2], ref tag.Value.Invalid);
							tag.UpdateValue (val [j + 3], ref tag.Value.Default);
							j += 4;
							
							list.TagListChanged.Add(tag);					
							tag.UpdateData (null, '-', DateTime.Now);
						}
					}
					list.UpdateData (DateTime.Now);
				}
			} else {
				Console.WriteLine (DateTime.Now + " TagConnectCB with unkown key " + key + "!");
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void DpQueryConnectCB (object sender, int id, int key, ArrayList dps)
			/*
		   C:2
		     V[ 0]:-496764408
		     C:12
		       C:5
		         V[ 0]:
		         V[ 1]::_online.._value
		         V[ 2]::_online.._stime
		         V[ 3]::_online.._invalid
		         V[ 4]::_online.._default
		       C:5
		         V[ 0]:System1:_Stat_event_0.SndTotal
		         V[ 1]:85
		         V[ 2]:2012.12.26 13:48:25.529
		         V[ 3]:False
		         V[ 4]:False
		       C:5
		         V[ 0]:System1:_Stat_event_0.RcvTotal
		         V[ 1]:78
		         V[ 2]:2012.12.26 13:48:25.529
		         V[ 3]:False
		         V[ 4]:False
			 ...
		 */
		{
			//WCCOABase.PrintArrayList(dps, " ");
			if (DpQueryConnects.ContainsKey (key)) {	
				DpQueryConnects[key].Changed(dps);
			} else {
				Console.WriteLine (DateTime.Now + " TagQueryConnectSingleCB with unkown key " + key + "!");
			}
		}	
		
		//------------------------------------------------------------------------------------------------------------------------
		public void DpConnectCB (object sender, int id, int key, ArrayList dps, ArrayList val)
			/*
			 C:1
			   C:2
			     C:1
			       V[ 0]:System1:ExampleDP_Trend1
			     C:4
			       V[ 0]:2
			       V[ 1]:2012.12.26 11:02:32.974
			       V[ 2]:False
			       V[ 3]:False
		 */
		{
			//WCCOABase.PrintArrayList(dps, " ");
			//WCCOABase.PrintArrayList(val, " ");
			if (DpConnects.ContainsKey (key)) {	
				DpConnects[key].Changed(val);
			} else {
				Console.WriteLine (DateTime.Now + " DpConnectCB with unkown key " + key + "!");
			}
		}
	}
}

