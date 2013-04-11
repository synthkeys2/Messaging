﻿using System;
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
			IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());

			foreach (IPAddress ip in ips)
			{
				IPAddresses.Items.Add(ip.ToString());
			}
			IPAddresses.Items.Add(IPAddress.Parse("127.0.0.1"));
			IPAddresses.SelectedIndex = ips.Length;
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
			// Get the socket that handles the client request.
			Socket listener = (Socket)ar.AsyncState;
			Socket client = listener.EndAccept(ar);

			mClients.Add(client);
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
		private List<Socket> mClients;
		private Dictionary<string, List<Socket>> mIDToSubscribers;
    }
}
