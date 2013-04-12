using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using MessagingCommon;

namespace MessageServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

			mConnectionFinished = new ManualResetEvent(false);
			mWindowClosing = new ManualResetEvent(false);
			IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());

			foreach (IPAddress ip in ips)
			{
				IPAddresses.Items.Add(ip.ToString());
			}
			IPAddresses.Items.Add(IPAddress.Parse("127.0.0.1"));
			IPAddresses.SelectedIndex = ips.Length;

			mClients = new List<Socket>();
			mIDToSubscribers = new Dictionary<string, List<Socket>>();
        }

		/// <summary>
		/// Spawn a listener thread on a listen button click
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
			// Grab the ip and port from the text box and wrap it up in an EndPoint
			IPAddress IP = IPAddress.Parse(IPAddresses.SelectedValue.ToString());
			IPEndPoint localEndPoint = new IPEndPoint(IP, Convert.ToInt32(PortTextBox.Text));
			
			// Spawn the listener on a different thread
			Thread t = new Thread(NewListenThread);
			t.Start(localEndPoint);
        }

		/// <summary>
		/// Listener thread that listens and spawns accept threads
		/// </summary>
		/// <param name="endPoint">Holds the IP and port in an IPEndPoint object</param>
		private void NewListenThread(object endPoint)
		{
			// Get the localEndPoint parameter and create a listener
			IPEndPoint localEndPoint = (IPEndPoint)endPoint;
			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			mServer = listener;

			// Bind the socket to the local endpoint and listen for incoming connections.
			try
			{
				listener.Bind(localEndPoint);
				LogToTextBox("Listening on " + listener.LocalEndPoint.ToString());
				listener.Listen(20);

				while (true)
				{
					// Set the event to nonsignaled state.
					mConnectionFinished.Reset();

					// Start an asynchronous socket to listen for connections.
					LogToTextBox("Waiting for a connection");
					listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

					// Wait until a connection is made before continuing.
					mConnectionFinished.WaitOne();
				}
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}
		}

		/// <summary>
		/// Called when the listener finds a client to accept
		/// </summary>
		/// <param name="ar">Holds the listener socket</param>
		private void AcceptCallback(IAsyncResult ar)
		{
			mConnectionFinished.Set();
			LogToTextBox("Received a connection");

			try
			{
				// Get the socket that handles the client request.
				Socket listener = (Socket)ar.AsyncState;
				Socket client = listener.EndAccept(ar);

				mClients.Add(client);

				StateObject state = new StateObject();
				state.workSocket = client;
				client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}
		}

		/// <summary>
		/// Called when the server gets a callback from BeginReceive
		/// </summary>
		/// <param name="ar">Holds state information like the client socket and buffer size</param>
        private void ReadCallback(IAsyncResult ar)
        {
			// Unblock any previous read calls
			mWindowClosing.Set();

			// Set up state data
            String content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

			try
			{
				int bytesRead = handler.EndReceive(ar);

				if (bytesRead > 0)
				{
					// Get the data that was read
					state.sb.Clear();
					state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
					content = state.sb.ToString();
					LogToTextBox("Received message: " + content);

					// Construct a new message from the received data
					Message m = new Message(content);
					if (m.mType == MessageType.Subscribe)
					{
						// If subscribe, just add the client to the subscribers list
						if (mIDToSubscribers.ContainsKey(m.mID))
						{
							mIDToSubscribers[m.mID].Add(handler);
						}
						else
						{
							List<Socket> clients = new List<Socket>();
							clients.Add(handler);
							mIDToSubscribers.Add(m.mID, clients);
						}
					}
					if (m.mType == MessageType.Unsubscribe)
					{
						// If unsubscribe, just remove the client from the subscribers list.
						if (mIDToSubscribers.ContainsKey(m.mID))
						{
							mIDToSubscribers[m.mID].Remove(handler);
						}
					}
					if (m.mType == MessageType.UserDefined)
					{
						// If Userdefined, distribute it to all client subscribers
						if (mIDToSubscribers.ContainsKey(m.mID))
						{
							List<Socket> socketsToRemove = new List<Socket>();

							foreach (Socket client in mIDToSubscribers[m.mID])
							{
								byte[] byteData = Encoding.ASCII.GetBytes(content);
								try
								{
									client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
								}
								catch (Exception ex)
								{
									LogToTextBox(ex.ToString());
									socketsToRemove.Add(client);
								}
							}
							foreach (Socket client in socketsToRemove)
							{
								foreach (KeyValuePair<string, List<Socket>> pair in mIDToSubscribers)
								{
									pair.Value.Remove(client);
								}

								mClients.Remove(client);
							}

							socketsToRemove.Clear();
						}
					}
				}

				// Start another read
				mWindowClosing.Reset();
				handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
				mWindowClosing.WaitOne();
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}
        }

		/// <summary>
		/// Called when the server needs to distribute a message to subscribers
		/// </summary>
		/// <param name="ar">Holds the client socket</param>
		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				Socket client = (Socket)ar.AsyncState;
				int bytesSent = client.EndSend(ar);
				LogToTextBox("Sent " + bytesSent + " bytes to " + client.ToString());
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}
		}

		delegate void LogToTextBoxDelegate(string message);

		/// <summary>
		/// Logs info to the text box on screen
		/// </summary>
		/// <param name="message">String to log</param>
        private void LogToTextBox(string message)
        {
			if (LogTextBox.Dispatcher.CheckAccess())
			{
				LogTextBox.Text += "\n" + message + "...";
				LogTextBox.Focus();
				LogTextBox.CaretIndex = LogTextBox.Text.Length;
				LogTextBox.ScrollToEnd();
			}
			else
			{
				LogTextBox.Dispatcher.Invoke(new LogToTextBoxDelegate(LogToTextBox), message);
			}
        }

		/// <summary>
		/// Clean up the sockets and end the threads when the window closes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mWindowClosing.Set();

			if (mServer != null)
			{
				mServer.Close();
				foreach (Socket client in mClients)
				{
					client.Close();
				}
			}
		}

		private Socket mServer;
		private ManualResetEvent mConnectionFinished;
		private ManualResetEvent mWindowClosing;
		private List<Socket> mClients;
		private Dictionary<string, List<Socket>> mIDToSubscribers;
    }

	/// <summary>
	/// State object for reading client data asynchronously
	/// </summary> 
    public class StateObject
    {
        /// Client  socket.
        public Socket workSocket = null;
        /// Size of receive buffer.
        public const int BufferSize = 1024;
        /// Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        /// Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}
