namespace Roc.XmlRpc
{
  using System;

	public class XmlRpcException : Exception
	{
		private int code;
  
		public XmlRpcException (int code, String message) : base(message)
		{
			this.code = code;
		}

		public int Code {
			get { return this.code; }
		}

		override public String ToString ()
		{
			return "Code: " + code + " Message: " + ((Exception)this).ToString ();
		}
	}
}
