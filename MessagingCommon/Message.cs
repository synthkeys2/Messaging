using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessagingCommon
{
	public enum MessageType 
	{
		Subscribe,
		Unsubscribe,
		UserDefined
	}
	public class Message : IComparable
	{
		Message()
		{

		}

		public string ToString()
		{

			return "string";
		}

		public int CompareTo(object obj)
		{
			if (obj == null) return 1;

			Message otherMessage = obj as Message;
			if (otherMessage != null)
			{
				return mID.CompareTo(otherMessage.mID);
			}
			else throw new ArgumentException("Object is not a Message");
		}

		public string mID;
		public MessageType mType;
		public Dictionary<string, object> mValues;
		public int port;
	}
}
