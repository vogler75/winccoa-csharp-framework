using System;
using System.Collections;
using System.Net;
using Roc.XmlRpc;

namespace Roc.WCCOA
{
	public class WCCOABase : WCCOAXmlRpc
	{

		public static DateTime ToDateTime(string s)
		{
			return Convert.ToDateTime(s);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public WCCOABase() 
			:base("localhost", 8080, "RPC", null, 10000)
		{
		}

		public WCCOABase(string Host, int Port, string RpcPath, NetworkCredential Credentials = null, int Timeout = 10000)
			:base(Host, Port, RpcPath, Credentials, Timeout)
		{
		}
			
		//------------------------------------------------------------------------------------------------------------------------
		public bool getValues(string[] dpNames, out ArrayList values)
		{
			ArrayList par = new ArrayList(dpNames);
			ArrayList input = new ArrayList(par);
			return Call ("pvss.db.getValues", input, out values);
		}			

		//------------------------------------------------------------------------------------------------------------------------
		public bool getValuesAsync(string[] dpNames, XmlRpcAsyncCallback func)
		{
			ArrayList par = new ArrayList(dpNames);
			ArrayList input = new ArrayList(par);
			return CallAsync("pvss.db.getValues", input, func);
		}

		//------------------------------------------------------------------------------------------------------------------------
        public bool setValues(string[] dpNames, ArrayList values)
        {
			ArrayList input = new ArrayList();
			input.Add(new ArrayList(dpNames));
			input.Add(values);
			ArrayList output;
			if ( Call ("pvss.db.setValues", input, out output) ) 
				return true;
			else 
				return false;
        }

		//------------------------------------------------------------------------------------------------------------------------
		public bool getValue(string dpName, out object value)
		{
			ArrayList values;
			if ( getValues (new string[] { dpName }, out values) && values is ArrayList ) 
			{
				value = values[0];
				return true;
			}
			else
			{
				value = null;
				return false;
			}
		}	

		//------------------------------------------------------------------------------------------------------------------------
		public bool getValueAsync(string dpName,  XmlRpcAsyncCallback func)
		{
			return getValuesAsync (new string[] { dpName }, func);
		}	

		//------------------------------------------------------------------------------------------------------------------------
		public bool setValue(string dpName, object value)
		{
			ArrayList values = new ArrayList();
			values.Add(value);
			return setValues (new string[] { dpName }, values);
		}

		//------------------------------------------------------------------------------------------------------------------------
        public bool getDouble (string dpName, out double output) 
        {
			object value;
			if ( getValue (dpName, out value) )
			{
				output=Convert.ToDouble(value);
				return true;
			}
		    else
			{
				output=0;
				return false;
			}
        }

		//------------------------------------------------------------------------------------------------------------------------
		public bool setDouble(string dpName, double value)
		{
			return setValue (dpName, value);
		}

		//------------------------------------------------------------------------------------------------------------------------
        public bool getString (string dpName, out string output) 
        {
			object value;
			if ( getValue (dpName, out value) )
			{
				output=value.ToString();
				return true;
			}
		    else
			{
				output="";
				return false;
			}
        }

		//------------------------------------------------------------------------------------------------------------------------
		public bool setString(string dpName, string value)
		{
			return setValue (dpName, value);
		}

		//------------------------------------------------------------------------------------------------------------------------
		public string[] dpNames(string dpPattern, string dpType="")
		{
			var input = new ArrayList() { dpPattern, dpType };
			var output = new ArrayList();

			if ( Call ("pvss.db.getDpNames", input, out output) )
			{
				string[] ret = new string[output.Count];
				for ( int i=0; i<output.Count; i++ )
					ret[i] = (string)output[i];
				return ret;
			}
			else
			{
				Console.WriteLine (LastErrorMsg);
				return new string[0];
			}
		}
	}
}


