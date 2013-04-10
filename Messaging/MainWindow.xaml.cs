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

namespace Messaging
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
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
			LogToTextBox("Attempting to connect to " + IPTextBox.Text);

			ConnectInfo connectInfo;
			connectInfo.ip = IPAddress.Parse(IPTextBox.Text);
			connectInfo.port = Convert.ToInt32(PortTextBox.Text);
			Thread t = new Thread(NewConnectionThread);
			t.Start(connectInfo);
        }

		public void NewConnectionThread(object connectInfo)
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

        public void ConnectCallback(IAsyncResult ar)
        {
			try
			{
				mConnectionFinished.Set();
				Socket s = (Socket)ar.AsyncState;
				s.EndConnect(ar);
				LogToTextBox("Socket connected to " + s.RemoteEndPoint.ToString());
			}
			catch (Exception ex)
			{
				LogToTextBox(ex.ToString());
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

        private ManualResetEvent mConnectionFinished;
    }

	struct ConnectInfo
	{
		public IPAddress ip;
		public int port;
	}
}
