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

namespace MessageClient
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
        }

		/// <summary>
		/// Connect to the server given the ip text box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
			LogToTextBox("Attempting to connect to " + IPTextBox.Text);

			ConnectInfo connectInfo;
			connectInfo.ip = IPAddress.Parse(IPTextBox.Text);
			connectInfo.port = Convert.ToInt32(PortTextBox.Text);
			Thread t = new Thread(NewConnectionThread);
			t.Start(connectInfo);
        }

		/// <summary>
		/// Spawn a new thread to connect so we don't block
		/// </summary>
		/// <param name="connectInfo">Holds the ip and port</param>
		private void NewConnectionThread(object connectInfo)
		{
			try
			{
				Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				mConnectionFinished.Reset();
				s.BeginConnect(((ConnectInfo)connectInfo).ip, ((ConnectInfo)connectInfo).port, new AsyncCallback(ConnectCallback), s);

				mConnectionFinished.WaitOne();
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}

			LogToTextBox("Successfully connected");
		}

		/// <summary>
		/// Called in response to a connection, start listening for receive.
		/// </summary>
		/// <param name="ar">Holds the socket</param>
		private void ConnectCallback(IAsyncResult ar)
        {
			try
			{
				mConnectionFinished.Set();
				Socket s = (Socket)ar.AsyncState;
				s.EndConnect(ar);
                mClient = s;
				LogToTextBox("Socket connected to " + s.RemoteEndPoint.ToString());

                StateObject state = new StateObject();
                state.workSocket = mClient;
                mClient.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}       
        }

		/// <summary>
		/// Called when a read is successful, then calls another read.
		/// </summary>
		/// <param name="ar"></param>
		private void ReadCallback(IAsyncResult ar)
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
					LogToTextBox(content);
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

		/// <summary>
		/// Called when the client wants to send a user defined message to the server.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            LogToTextBox("Attempting to send UserDefined message");

			Message m = new Message();
			m.mID = IDTextBox.Text;
			m.mType = MessageType.UserDefined;
			m.mValues.Add("Payload", PayloadTextBox.Text);

            SendInfo sendInfo;
            sendInfo.client = mClient;
            sendInfo.data = m.ToString();

            Thread t = new Thread(NewSendThread);
            t.Start(sendInfo);
        }

		/// <summary>
		/// Set up a subscribe message to the server.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SubscribeButton_Click(object sender, RoutedEventArgs e)
		{
			LogToTextBox("Attempting to send Subscribe message");

			Message m = new Message();
			m.mID = SubscribeIDTextBox.Text;
			m.mType = MessageType.Subscribe;

			SendInfo sendInfo;
			sendInfo.client = mClient;
			sendInfo.data = m.ToString();

			Thread t = new Thread(NewSendThread);
			t.Start(sendInfo);
		}

		/// <summary>
		/// Set up an unsubscribe message to the server.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UnsubscribeButton_Click(object sender, RoutedEventArgs e)
		{
			LogToTextBox("Attempting to send Unsubscribe message");

			Message m = new Message();
			m.mID = SubscribeIDTextBox.Text;
			m.mType = MessageType.Unsubscribe;

			SendInfo sendInfo;
			sendInfo.client = mClient;
			sendInfo.data = m.ToString();

			Thread t = new Thread(NewSendThread);
			t.Start(sendInfo);
		}

		/// <summary>
		///  The thread that is spawned for all sends.
		/// </summary>
		/// <param name="sendInfo">Contains a string to send and a client to send it to</param>
		private void NewSendThread(object sendInfo)
		{
			SendInfo info = (SendInfo)sendInfo;
			byte[] byteData = Encoding.ASCII.GetBytes(info.data);
			try
			{
				info.client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), info.client);
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
			}
		}

		/// <summary>
		/// The Thread spawned by a successful send call.
		/// </summary>
		/// <param name="ar">Holds the client socket</param>
		private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int bytesSent = client.EndSend(ar);
                LogToTextBox("Sent " + bytesSent + " bytes to the server");
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
		/// <param name="message"></param>
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
		/// Clean up the socket and end the read thread when the window closes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mWindowClosing.Set();
			if (mClient != null)
			{
				mClient.Close();
			}
		}

        private ManualResetEvent mConnectionFinished;
		private ManualResetEvent mWindowClosing;
        private Socket mClient;
    }

	/// <summary>
	/// Holds an ip and port for passing around through one parameter
	/// </summary>
	struct ConnectInfo
	{
		public IPAddress ip;
		public int port;
	}

	/// <summary>
	/// Holds a client and the data for sending 
	/// </summary>
    struct SendInfo
    {
        public Socket client;
        public String data;
    }

    /// <summary>
	/// State object for reading server data asynchronously
    /// </summary>
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
