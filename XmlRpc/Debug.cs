namespace Roc.XmlRpc
{
using System;
	using System.Diagnostics;

	static class Debug
	{
		public static DateTime last = DateTime.Now;
		public static int counter = 0;

		public static void Write(string s)
		{
			DateTime t = DateTime.Now;
			Console.WriteLine (DateTime.Now.ToString("dd.MM.yy hh:mm:ss.fff") + " " + s);
			last = t;
		}
	}
}
