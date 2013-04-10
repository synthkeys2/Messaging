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
        }

        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
            LogToTextBox("Listening");
        }

        public void LogToTextBox(string message)
        {
            LogTextBox.Text += "\n" + message + "...";
            LogTextBox.Focus();
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
            LogTextBox.ScrollToEnd();
        }
    }
}
