using System;
using System.Collections;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Xml;
using System.Reflection;
using Roc.XmlRpc;

namespace Roc.WCCOA
{
	public class WCCOAXmlTcp
	{
		const byte SOH = 1;  // start of heading
		const byte STX = 2;  // start of text
		const byte ETX = 3;  // end of text
		const byte ACK = 6;  // acknowledge
		const byte NAK = 15; // neg. ack
		const byte SYN = 22; // synchronize

		// Buffer to store the response bytes.
		byte[] rawd = new byte[4096];			
		byte[] data = new byte[4096*256];

		object rpc;
		TcpClient tcp;
		NetworkStream net;
		bool client; 

		public WCCOAXmlTcp (object rpc, TcpClient tcp, bool client)
		{
			this.rpc = rpc;
			this.tcp = tcp;
			this.client = client;
			this.net = tcp.GetStream ();
		}

		public void Close()
		{
			tcp.Close();
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int ReadByte (int timeout)
		{
			net.ReadTimeout=timeout;

			int ret = net.ReadByte();
			switch ( ret ) 
			{
			case -1 : return -1; // end of stream
			case ACK: return ACK;
			case NAK: return NAK;
			case SOH: return SOH;
			case STX: return STX; 
			case ETX: return ETX;
			default: 
				Debug.Write ("ReadByte: read unknown byte: " + ret);
				return -2;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		private void ReadBlock (NetworkStream stream, byte[] rawd, byte[] data, out int bytes, out bool overflow, int timeout)
		{
			bool stx;
			bool etx;
			int bytesBlock;
			
			stx = false;
			etx = false;
			bytes = 0;
			overflow = false;

			net.ReadTimeout=timeout;
			
			while ( !etx )  
			{
				bytesBlock = stream.Read (rawd, 0, rawd.Length);
				if ( bytesBlock == 0 ) continue;
				
				if ( rawd[0] == STX )
				{
					if ( rawd[bytesBlock-1] == ETX )
					{
						//Console.WriteLine("[STX]+[ETX]");
						stx=etx=true;
						if ( !(overflow=(bytes+bytesBlock-2 > data.Length)) )
							Array.Copy (rawd, 1, data, bytes, bytesBlock-2);
						bytes+=bytesBlock-1;
					}
					else
					{
						//Console.Write("[STX]");
						stx=true;
						if ( !(overflow=(bytes+bytesBlock-1 > data.Length)) )
							Array.Copy (rawd, 1, data, bytes, bytesBlock-1);
						bytes+=bytesBlock-1;
					}
				}
				else if ( stx )
				{
					if ( rawd[bytesBlock-1] == ETX )
					{
						//Console.WriteLine("[ETX]");
						etx=true;
						if ( !(overflow=(bytes+bytesBlock-1 > data.Length)) )
							Array.Copy (rawd, 0, data, bytes, bytesBlock-1);
						bytes+=bytesBlock-1;
					}
					else
					{
						//Console.Write("*");
						if ( !(overflow=(bytes+bytesBlock > data.Length)) )
							Array.Copy (rawd, 0, data, bytes, bytesBlock);
						bytes+=bytesBlock;
					}
				}
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public bool WriteByte (byte b, int timeout)
		{
			net.WriteTimeout=timeout;
			net.WriteByte(b);
			net.Flush();
			return true;
		}

		//------------------------------------------------------------------------------------------------------------------------
		public bool WriteBlock (string MethodName, ArrayList Params, int timeout)
		{
			//Console.WriteLine ("WriteRequest: " + MethodName);
			try {
				WriteByte (STX, 100); 

				/* Debug-Write
				StringWriter strBuf = new StringWriter ();
				var xmlw_temp = new XmlTextWriter (strBuf);
				XmlRpcRequestSerializer.Serialize (xmlw_temp, MethodName, Params);
				Console.WriteLine (strBuf);
				*/

				var strw = new StreamWriter (net);			
				var xmlw = new XmlTextWriter (strw);
				XmlRpcRequestSerializer.Serialize (xmlw, MethodName, Params);
				strw.Flush ();

				WriteByte (ETX, 100);

				return true;
			} catch (Exception e) {
				Debug.Write ("WriteBlock: write exception: " + e.Message);
				return false;
			}
		}	

		//------------------------------------------------------------------------------------------------------------------------
		public int ReadRequest (out XmlRpcRequest request, int timeout)
		{
			int b, bytes;
			bool overflow;

			request = null;			
			try 
			{
				b = ReadByte (timeout);
				switch ( b ) 
				{
				case SOH: 
					WriteByte (ACK, 100);
					ReadBlock (net, rawd, data, out bytes, out overflow, 1000);
					if ( ! overflow )
					{
						//string s = Encoding.ASCII.GetString (data, 0, bytes);
						//Console.WriteLine ("Received: {0}", s);

						if ( DecodeRequest (rpc, data, bytes, out request) )
						{
							WriteByte(ACK, 100); 
							return 1;
						}
						else
						{
							Debug.Write ("ReadRequest: DecodeRequest error! ");
							WriteByte(NAK, 100); 
							return 2;
						}
					}
					else
					{
						Debug.Write ("ReadRequest: buffer overflow! " + bytes);
						WriteByte (NAK, 100);
						return 3;
					}
				case -1:
					Debug.Write ("ReadRequest: got -1 from ReadByte");
					return -2;
				default: 
					Debug.Write ("ReadRequest: got no SOH! " + b);
					return 4;
				}
			}
			catch ( Exception )
			{
				//Debug.Write ("ReadRequest: Exception!");
				return -1;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public int WriteRequest(string MethodName, ArrayList Params)
		{
			int a1, a2, ret;
			try {
				do {
					WriteByte (SOH, 1000); 
					a1 = ReadByte(1000);
					switch ( a1 )
					{
					case ACK: 
						if ( WriteBlock (MethodName, Params, 1000) )
						{
							if ( (a2=ReadByte (1000)) == ACK )
								ret = 1;
							else
							{
								Debug.Write ("WriteRequest: got no ACK after BLCOCK. " + a2);
								ret = 2;
							}
						}							
						else
						{
							ret = 3;
						}
						break;
					case NAK: 
						Debug.Write ("WriteRequest: got NAK for SOH.");
						ret = 4;
						break;
					case SOH: 
						if ( client ) 
						{
							Debug.Write ("WriteRequest: client got SOH...retry.");
							ret = 0;
						}
						else
						{
							Debug.Write ("WriteRequest: server got SOH...stop.");
							ret = 5;
						}
						break;
					default:
						Debug.Write ("WriteRequest: got unknow answer " + a1);
						ret = 6;
						break;
					}
				} while ( ret == 0 );
				return ret;
			} catch (Exception e) {
				Console.WriteLine ("WriteRequest: exception: " + e.Message);
				return -1;
			}
		}
	

		//------------------------------------------------------------------------------------------------------------------------
		private bool DecodeRequest (object rpc, Byte[] data, int len, out XmlRpcRequest request)
		{
			try {
				//byte[] data = Encoding.ASCII.GetBytes( xml );
				var mstr = new MemoryStream (data, 0, len);
				var strr = new StreamReader (mstr);
				var xmlr = new XmlTextReader (strr);
				
				var des = new XmlRpcRequestDeserializer ();
				request = des.Parse (xmlr);

				//Console.WriteLine ( requ.ToString() );
				return true;
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
				Console.WriteLine ("Received: {0}", Encoding.ASCII.GetString (data, 0, len));
				request = null;
				return false;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		public static object InvokeRequest (object sender, object rpc, string MethodName, ArrayList Params)
		{
			//WCCOABase.PrintArrayList (Params, " ");
			
			//			switch (MethodName) {
			//			case "RPC_Alive":
			//				break;
			//			case "RPC_TagConnectCB":
			//				TagConnectCB ((ArrayList)Params [0]);
			//				break;
			//			case "RPC_TagQueryConnectSingleCB":
			//				TagQueryConnectSingleCB ((ArrayList)Params [0]);
			//				break;
			//			}
			
			Type type = rpc.GetType ();
			MethodInfo method = type.GetMethod (MethodName);
			
			if (method == null) {
				Console.WriteLine ("Method " + MethodName + " not found.");
				return null;
			}
			
			if (Params.Count == 1 && Params [0] is ArrayList) {
				Params = (ArrayList)Params [0];
			}
			Object[] args = new Object[Params.Count+1];
			args[0] = sender;
			for (int i = 0; i < Params.Count; i++)
				args [i+1] = Params [i];			
			//Console.WriteLine ("InvokeRequest: " + MethodName + " " + args.Length);
			return method.Invoke (rpc, args);
		}
	}
}

