using System;
using System.Collections;
using System.Collections.Generic;

namespace Roc.WCCOA
{
	public delegate void WCCOATagListAction(WCCOATagList TagList, bool FirstUpdate);

	//========================================================================================================================
	public class WCCOATagList
	{
		public WCCOAXmlRpc	Conn;
		
		public List<WCCOATag> TagList = new List<WCCOATag>();
		public List<WCCOATag> FetchList;
		public Hashtable DpNames = new Hashtable();
		
		public List<WCCOATag> TagListChanged = new List<WCCOATag>();
		public List<int>      TagIndxChanged = new List<int>();
		
		public DateTime UpdateTime = DateTime.MinValue;
		public bool UpdateChangedData = false;
		
		private List<WCCOATagListAction> ActionList = new List<WCCOATagListAction>();
		
		//------------------------------------------------------------------------------------------------------------------------
		public WCCOATagList(WCCOAXmlRpc Conn)
		{
			this.Conn = Conn;
			FetchList = TagList;
			UpdateTime = DateTime.MinValue;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		override public string ToString()
		{
			string s = "";
			foreach ( WCCOATag Tag in TagList )
				s+=Tag.ToString()+"\n";
			return s;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void Clear()
		{
			FetchList = TagList;
			UpdateTime = DateTime.MinValue;
			UpdateChangedData = false;
			TagList.Clear();
			TagListChanged.Clear();
			TagIndxChanged.Clear();
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// add a dp(name) to the taglist 
		public WCCOATag Add(string DpName)
		{
			WCCOATag tag = new WCCOATag(this.Conn, DpName);
			this.Add(tag);
			return tag;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// add a list of dp(names) to the taglist
		public void Add(string[] DpNames)
		{
			foreach ( string DpName in DpNames ) 
				this.Add(new WCCOATag(this.Conn, DpName));
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// add a tag to the taglist
		public void Add (WCCOATag Tag)
		{
			if (! TagList.Contains (Tag)) 
			{
                lock (TagList)
                {
                    TagList.Add(Tag);
                    DpNames.Add(Tag.DpName, Tag);
                }
			}
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// check if the taglist contains a tag
		public bool Contains(WCCOATag Tag)
		{
			return TagList.Contains(Tag);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public WCCOATag Contains(string DpName)
		{
			if ( DpNames.ContainsKey(DpName) )
				return DpNames[DpName] as WCCOATag;
			else
				return null;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// remove a dp(name) from the taglist
		public void Remove(string DpName)
		{
            for (int i = TagList.Count - 1; i >= 0; i--)
            {
                if (((WCCOATag)TagList[i]).DpName == DpName)
                {
                    lock ( TagList ) TagList.RemoveAt(i);
                }
            }
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// count of tags in taglist
		public int Count
		{
			get { return TagList.Count; }
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// fetch data of tag in taglist
		private bool FetchData(char what=' ')
		{
			return FetchDataMulti (what);
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// fetch the tags of the fetchlist one by one (one call per tag)
		private bool FetchDataSingle(char what=' ')
		{
			TagListChanged.Clear ();
			TagIndxChanged.Clear ();
			UpdateChangedData = false;
			
			DateTime UpdateTime = DateTime.Now;
			UpdateChangedData = false;
			
			for ( int i=0; i<FetchList.Count; i++ )
			{
				switch ( what )
				{
				case 'C': FetchList[i].FetchConfig(); break;
				case 'V': FetchList[i].FetchValue(); break;
				case 'A': FetchList[i].FetchAlert(); break;
				}
				if ( FetchList[i].UpdateChangedData ) 
				{
					UpdateChangedData = true;
					TagListChanged.Add (FetchList[i]);
					TagIndxChanged.Add (i);
				}
			}
			
			if ( UpdateChangedData ) 
			{
				UpdateData (UpdateTime);
			}
			
			return true;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// fetch the tags of the fetchlist with one call
		public bool FetchConfigs ()
		{
			return FetchDataMulti('C');
		}

		public bool FetchValues ()
		{
			return FetchDataMulti('V');
		}

		public bool FetchAlerts ()
		{
			return FetchDataMulti('A');
		}
		
		private bool FetchDataMulti(char what=' ')
		{
			int i;
			
			ArrayList input = new ArrayList();
			ArrayList dplist = new ArrayList();
			ArrayList output;
			
			DateTime UpdateTime = DateTime.Now;
			bool FirstUpdate = (this.UpdateTime==DateTime.MinValue ? true : false);
			UpdateChangedData = false;
			
			TagListChanged.Clear ();
			TagIndxChanged.Clear ();
			
			for ( i=0; i<FetchList.Count; i++ )
				dplist.Add (FetchList[i].DpName);
			
			input.Add (dplist);
			input.Add (what.ToString());
			
			if ( FetchList.Count > 0 && Conn.Call("xoa.getTags", input, out output))
			{
				for ( i=0; i<FetchList.Count; i++ )
				{
					FetchList[i].UpdateData((ArrayList)output[i], what, UpdateTime);
					if ( FetchList[i].UpdateChangedData ) 
					{
						UpdateChangedData = true;
						TagListChanged.Add (FetchList[i]);
						TagIndxChanged.Add (i);
					}
				}
			}
			
			if ( UpdateChangedData ) 
			{
				UpdateData (UpdateTime);
			}
			
			return true;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		// waits for new data of at least one tag of the fetchlist
		public bool WaitForValues ()
		{
			int i;
			
			ArrayList input = new ArrayList ();
			ArrayList dplist = new ArrayList ();
			ArrayList output;
			
			DateTime UpdateTime = DateTime.Now;
			bool FirstUpdate = (this.UpdateTime == DateTime.MinValue ? true : false);
			UpdateChangedData = false;
			
			TagListChanged.Clear ();
			TagIndxChanged.Clear ();
			
			for (i=0; i<FetchList.Count; i++)
				dplist.Add (FetchList [i].DpName);
			
			input.Add (dplist);
			
			if (FetchList.Count > 0 && Conn.Call ("xoa.waitForTags", input, out output)) {
				for (i=0; i<FetchList.Count; i++) {
					FetchList [i].UpdateData ((ArrayList)output [i], 'V', UpdateTime);
					if (FetchList [i].UpdateChangedData) {
						UpdateChangedData = true;
						TagListChanged.Add (FetchList [i]);
						TagIndxChanged.Add (i);
					}
				}
			}
			
			if (UpdateChangedData) {
				UpdateData (UpdateTime);
			}
			
			return true;
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void UpdateData(DateTime UpdateTime)
		{
			bool FirstUpdate = (this.UpdateTime==DateTime.MinValue ? true : false);
			this.UpdateTime = UpdateTime;

            foreach (WCCOATagListAction a in ActionList)
                a(this, FirstUpdate);
		}
		
		//------------------------------------------------------------------------------------------------------------------------
		public void AddDataChangedAction(WCCOATagListAction a)
		{
			ActionList.Add(a);
		}
		

	}
}

