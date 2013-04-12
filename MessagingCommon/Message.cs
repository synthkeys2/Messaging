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
		/// <summary>
		/// Default constructor
		/// </summary>
		public Message()
		{
			mValues = new Dictionary<string, object>();
		}

		/// <summary>
		/// Constructor that decomposes a string into its message parts.
		/// </summary>
		/// <param name="messageAsString"></param>
		public Message(string messageAsString)
		{
			mValues = new Dictionary<string, object>();

			string[] semiSplit = messageAsString.Split(';');

			foreach (string nameValue in semiSplit)
			{
				string[] equalSplit = nameValue.Split('=');
				if (equalSplit.Length == 2)
				{
					if (equalSplit[0] == "MessageType")
					{
						if (equalSplit[1] == "Subscribe")
						{
							mType = MessageType.Subscribe;
						}
						else if (equalSplit[1] == "Unsubscribe")
						{
							mType = MessageType.Unsubscribe;
						}
						else if (equalSplit[1] == "UserDefined")
						{
							mType = MessageType.UserDefined;
						}
					}
					else if (equalSplit[0] == "MessageID")
					{
						mID = equalSplit[1];
					}
					else
					{
						mValues.Add(equalSplit[0], equalSplit[1]);
					}
				}
			}
		}

		/// <summary>
		/// Takes a populated message object and prints it to a string.
		/// </summary>
		/// <returns>Message as string</returns>
		public string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("MessageType=");
			sb.Append(mType.ToString());
			sb.Append(";MessageID=");
			sb.Append(mID);
			sb.Append(";");

			foreach (KeyValuePair<string, object> entry in mValues)
			{
				sb.Append(entry.Key);
				sb.Append("=");
				sb.Append(entry.Value.ToString());
				sb.Append(";");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Compare 2 messages
		/// </summary>
		/// <param name="obj">Right hand side</param>
		/// <returns></returns>
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

		/// <summary>
		/// Classifies the message
		/// </summary>
		public string mID;

		/// <summary>
		/// Describes the type of message
		/// </summary>
		public MessageType mType;

		/// <summary>
		/// Holds variable payloads of a message
		/// </summary>
		public Dictionary<string, object> mValues;
	}
}
