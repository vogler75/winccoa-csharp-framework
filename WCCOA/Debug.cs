using System;

namespace Roc.WCCOA
{
	static class Debug
	{
		public static DateTime last = DateTime.Now;

		public static void Write(string s)
		{
			DateTime t = DateTime.Now;
			Console.WriteLine (DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss.fff") + " " + s);
			last = t;
		}
	}
}

