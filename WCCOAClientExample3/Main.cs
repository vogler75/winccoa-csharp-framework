using System;
using Gtk;
using Roc.WCCOA;

namespace WCCOAClientExample3
{
	class MainClass
	{
		static WCCOAClient client;

		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();


		}
	}
}
