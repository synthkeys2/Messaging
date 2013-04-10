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
            IPAddress IP = IPAddress.Parse(IPTextBox.Text);
            int port = Convert.ToInt32(PortTextBox.Text);

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mConnectionFinished.Reset();
            s.BeginConnect(IP, port, new AsyncCallback(ConnectCallback), s);

            LogToTextBox("Attempting to connect to " + IPTextBox.Text);
 

            mConnectionFinished.WaitOne();

            LogToTextBox("Successfully connected");
        }

        public void LogToTextBox(string message)
        {
            LogTextBox.Text += "\n" + message + "...";
            LogTextBox.Focus();
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
            LogTextBox.ScrollToEnd();
        }

        public void ConnectCallback(IAsyncResult ar)
        {
            mConnectionFinished.Set();
            Socket s = (Socket)ar.AsyncState;
            s.EndConnect(ar);

            Console.WriteLine("Socket connected to " + s.RemoteEndPoint.ToString());
        }


        private ManualResetEvent mConnectionFinished;
    }
}
