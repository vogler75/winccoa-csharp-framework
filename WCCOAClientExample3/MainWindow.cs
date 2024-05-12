using System;
using Gtk;
using System.Collections;
using System.Net;
using System.Threading;

using Roc.WCCOA;

public partial class MainWindow: Gtk.Window
{	
	static WCCOAClient client;

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		NetworkCredential CtrlConn = new NetworkCredential ("root", "");			
		string ProxyHost = "localhost";
		
		int ProxyClientPort = 8091;  // proxy tcp port for clients
		int ProxyRemotePort = 8092; // .net remoting
		
		// create server and client objects
		client = new WCCOAClient (ProxyHost, ProxyRemotePort, ProxyClientPort);
		
		// start and connect
		client.Start ();		
		client.Connect ();
		Thread.Sleep (1000); // wait until client id is registered

		/*
		client.DpQueryConnectSingle((object s, ArrayList a) => {
			WCCOABase.PrintArrayList(a);
		}, "SELECT '_online.._value', '_online.._stime' FROM '*.**'");	
		*/

		client.DpConnect((object s, ArrayList a) => {
			Gtk.Application.Invoke(delegate {
			spinbutton1.Text = ((double)((ArrayList)a[1])[0]).ToString();
			});
		}, new string[] {"ExampleDP_Arg1."}, true);
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}
}
