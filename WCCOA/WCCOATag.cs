using System;
using System.Collections;
using System.Collections.Generic;

namespace Roc.WCCOA
{
	public class oa_config 
	{
		public string		Desc;
		public string 		Unit;
		public string 		Format;
		public int 			RangeType;
		public double 		RangeMin;
		public double 		RangeMax;

		override public string ToString()
		{
			return String.Format("{0} {1} {2} {3} {4}", Desc, Unit, RangeType, RangeMin, RangeMax);
		}
	}

	public class oa_value		
	{
		public object 		Value;
		public object		OldValue;
		public DateTime 	Time;
		public bool 		Invalid;
		public bool 		Default;
		public bool 		Uncertain;

		override public string ToString()
		{
			return String.Format("{0} {1} {2} {3} {4}", Value, Time, Invalid, Default, Uncertain);
		}
	}

	public class oa_alert
	{
		public string 		Text0;
		public string		Text1;
		public int    		State;
		public string		Text;
		public string		Color;
		public int			Priority;

		public bool Ackable { get { return State == 1 || State == 3 || State == 4; } }


		override public string ToString()
		{
			return String.Format("{0} {1} {2} ", Text0, Text1, State);
		}
	}

	public class oa_tag
	{
		public string		 DpName;
		public oa_config	 Config;
		public oa_value		 Value;
		public oa_alert		 Alert;
		public Hashtable     Attr;

		public oa_tag(string DpName)
		{
			this.DpName = DpName;
			this.Config = new oa_config();
			this.Value = new oa_value();
			this.Alert = new oa_alert();
		}

		override public string ToString()
		{
			return String.Format ("{0} {1} {2} {3}", DpName, Config, Value, Alert);
		}
	}

	public delegate void WCCOATagAction(WCCOATag Tag, bool FirstUpdate);

	//========================================================================================================================
	public class WCCOATag : oa_tag
	{
		public WCCOAXmlRpc 	Conn;  

		public DateTime		UpdateTime;
		public bool			UpdateChangedData; // update (fetch data) changed values 

		private List<WCCOATagAction> ActionList = new List<WCCOATagAction>();

		//------------------------------------------------------------------------------------------------------------------------
		public WCCOATag (WCCOAXmlRpc Conn, string DpName) : base(DpName)
		{
			this.Conn = Conn;
			this.UpdateTime = DateTime.MinValue;
		}

		//------------------------------------------------------------------------------------------------------------------------
		override public string ToString()
		{
			return String.Format ("{0} {1}>> {2}",
			                      UpdateTime, 
			                      UpdateChangedData,
			                      base.ToString());
		}

		public bool FetchConfig ()
		{
			return FetchData ('C');
		}

		public bool FetchValue ()
		{
			return FetchData ('V');
		}

		public bool FetchAlert ()
		{
			return FetchData ('A');
		}	

		//------------------------------------------------------------------------------------------------------------------------
		// desc: fetches data of tag
		// what: ' '  all, 'C' Config, 'V' Values, 'X' Values+Alerts
		private bool FetchData(char what=' ')
		{
			ArrayList input = new ArrayList();
			ArrayList output;

			input.Add (this.DpName); // first parameter dp name
			input.Add (what.ToString()); // second parameter what to fetch 

			if ( Conn.Call("xoa.getTag", input, out output))
			{
				//bool FirstUpdate = (UpdateTime == DateTime.MinValue ? true : false);

				// update data in data structure
				UpdateData(output, what, DateTime.Now);

				return true;
			}
			else
			{
				return false;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		// add an action/delegate to the action list (when data has changed)
		public void AddDataChangedAction(WCCOATagAction a)
		{
			ActionList.Add(a);
		}	

		public void CallDataChangedActions (bool FirstUpdate = false)
		{
			foreach (WCCOATagAction a in ActionList) {
				a (this, FirstUpdate);
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		// updates the data structure with the data got from source
		public void UpdateData(ArrayList data, char what, DateTime time)
		{
			//Console.WriteLine ("Tag.UpdateData " + data.Count + " " + what + " " + time); 

			UpdateChangedData = false;
			bool FirstUpdate = (UpdateTime == DateTime.MinValue ? true : false);
			UpdateTime = time;

			if ( data != null ) 
			{
				int i = -1;
				ArrayList x;
			
				//WCCOABase.PrintArrayList(data, "  ");

				if ( what==' ' || what=='C' ) 
				{
					x = (ArrayList)(data[++i]);
					UpdateValue (x[0], ref Config.Desc);
					UpdateValue (x[1], ref Config.Unit);
					UpdateValue (x[2], ref Config.Format);
					UpdateValue (x[3], ref Config.RangeType);
					UpdateValue (x[4], ref Config.RangeMin);
					UpdateValue (x[5], ref Config.RangeMax);
				}

				if ( what==' ' || what=='V' || what == 'X' ) 
				{
					x = (ArrayList)(data[++i]);
					Value.OldValue = Value.Value;
					UpdateValue (x[0], ref Value.Value);
					UpdateValue (x[1], ref Value.Time);
					UpdateValue (x[2], ref Value.Invalid);
					UpdateValue (x[3], ref Value.Default);
					UpdateValue (x[4], ref Value.Uncertain);
					//Console.WriteLine ("Tag.Update " + UpdateChangedData + " " + Value.Value.ToString() + " " + Value.Time.ToString() + " " + Value.Invalid + Value.Default + Value.Uncertain);
				}

				if ( what==' ' || what=='A' || what == 'X' ) 
				{
					x = (ArrayList)(data[++i]);
					UpdateValue (x[0], ref Alert.Text0);
					UpdateValue (x[1], ref Alert.Text1);
					UpdateValue (x[2], ref Alert.State);
					UpdateValue (x[3], ref Alert.Text);
					UpdateValue (x[4], ref Alert.Color);
					UpdateValue (x[5], ref Alert.Priority);
				}
			}
			else
				UpdateChangedData = true;

			// check if at least one data value has changed and execute delegates 
			if ( UpdateChangedData )
			{
				CallDataChangedActions(FirstUpdate);
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		// updates a single value with old/new comparison
		// if a value has changed then "UpdateChangedData" will be set to true
		public void UpdateValue(object s, ref object d)
		{
			if ( d is bool )
			{
				bool t = (bool)d;
				UpdateValue (s, ref t);
				d=t;
			}
			else if ( d is int )
			{
				int t = (int)d;
				UpdateValue (s, ref t);
				d=t;
			}
			else if ( d is double ) 
			{
				double t = (double)d;
				UpdateValue (s, ref t);
				d=t;
			}
			else if ( d is string ) 
			{
				string t = (string)d;
				UpdateValue (s, ref t);
				d=t;
			}
			else if ( d is DateTime )
			{
				DateTime t = (DateTime)d;
				UpdateValue(s, ref t);
				d=t;
			}
			else
			{
				d = s;
				UpdateChangedData = true;
				//Console.WriteLine ("obj");
			}
		}

		// update value for datetime
		public void UpdateValue(object s, ref DateTime d)
		{
			if ( s is DateTime ) 
			{
				if ( d == (DateTime)s ) return;
				else d = (DateTime)s;
			}
			else if ( s is string ) 
			{
				if ( d == WCCOABase.ToDateTime((string)s) ) return;
				else d = WCCOABase.ToDateTime((string)s);
			}
			UpdateChangedData = true;
			//Console.WriteLine ("datetime");
		}

		// update value for string
		public void UpdateValue(object s, ref string d)
		{
			if ( (string)d == (string)s ) return;
			else d = (string)s;
			UpdateChangedData = true;
			//Console.WriteLine ("string");
		}

		// update value for bool
		public void UpdateValue(object s, ref bool d)
		{
			if ( s is bool ) 
			{
				if ( d == (bool)s ) return;
				else d = (bool)s;
			}
			else 
			{
				bool t;
				if ( s is string ) 
					t = ((string)s=="TRUE" ? true : false);
				else if ( s is int || s is double ) 
					t = ((int)s != 0 ? true : false);
				else 
					t = false;

				if ( d == t ) return;
				else d = t;			
			}
			UpdateChangedData = true;
			//Console.WriteLine ("bool");
		}

		// update value for int
		public void UpdateValue(object s, ref int d)
		{
			if ( d == (int)s ) return;
			else d = (int)s;
			UpdateChangedData=true;
			//Console.WriteLine ("int");
		}

		// update value for double
		public void UpdateValue(object s, ref double d)
		{
			if ( d == (double)s ) return;
			else d = (double)s;
			UpdateChangedData=true;
			//Console.WriteLine ("double");
		}
	}
}