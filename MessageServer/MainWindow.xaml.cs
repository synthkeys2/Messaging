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

        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
			//LogToTextBox("Listening on " + IPAddresses.SelectedValue + " on port " + PortTextBox.Text);
			IPAddress IP = IPAddress.Parse(IPAddresses.SelectedValue.ToString());
			IPEndPoint localEndPoint = new IPEndPoint(IP, Convert.ToInt32(PortTextBox.Text));
			Thread t = new Thread(NewListenThread);
			t.Start(localEndPoint);
        }

		public void NewListenThread(object endPoint)
		{
			IPEndPoint localEndPoint = (IPEndPoint)endPoint;
			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			mServer = listener;

			// Bind the socket to the local endpoint and listen for incoming connections.
			try
			{
				listener.Bind(localEndPoint);
				LogToTextBox("Listening on " + listener.LocalEndPoint.ToString());
				Console.WriteLine("Listening on " + listener.LocalEndPoint.ToString());
				listener.Listen(100);

				while (true)
				{
					// Set the event to nonsignaled state.
					mConnectionFinished.Reset();

					// Start an asynchronous socket to listen for connections.
					LogToTextBox("Waiting for a connection");
					listener.BeginAccept(
						new AsyncCallback(AcceptCallback),
						listener);

					// Wait until a connection is made before continuing.
					mConnectionFinished.WaitOne();
				}
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}
		}

		public void AcceptCallback(IAsyncResult ar)
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

        public void ReadCallback(IAsyncResult ar)
        {
			mWindowClosing.Set();
            String content = String.Empty;

            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

			try
			{
				int bytesRead = handler.EndReceive(ar);

				if (bytesRead > 0)
				{
					state.sb.Clear();
					state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
					content = state.sb.ToString();
					LogToTextBox("Received message: " + content);

					Message m = new Message(content);
					if (m.mType == MessageType.Subscribe)
					{
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
						if (mIDToSubscribers.ContainsKey(m.mID))
						{
							mIDToSubscribers[m.mID].Remove(handler);
						}
					}
					if (m.mType == MessageType.UserDefined)
					{
						if (mIDToSubscribers.ContainsKey(m.mID))
						{
							foreach (Socket client in mIDToSubscribers[m.mID])
							{
								byte[] byteData = Encoding.ASCII.GetBytes(content);

								// Begin sending the data to the remote device.
								client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
							}
						}
					}
				}

				mWindowClosing.Reset();
				handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
				mWindowClosing.WaitOne();
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}
        }

		public void SendCallback(IAsyncResult ar)
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
				foreach (KeyValuePair<string, List<Socket>> pair in mIDToSubscribers)
				{
					pair.Value.Remove((Socket)ar.AsyncState);
				}
			}
		}

		delegate void LogToTextBoxDelegate(string message);

        public void LogToTextBox(string message)
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

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mWindowClosing.Set();
			mServer.Close();
			foreach (Socket client in mClients)
			{
				client.Close();
			}
		}

		private Socket mServer;
		private ManualResetEvent mConnectionFinished;
		private ManualResetEvent mWindowClosing;
		private List<Socket> mClients;
		private Dictionary<string, List<Socket>> mIDToSubscribers;
    }

    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}
