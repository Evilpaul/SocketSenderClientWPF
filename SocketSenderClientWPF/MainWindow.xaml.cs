using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace SocketSenderClientWPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private IProgress<string> progress_str;
		private IProgress<Boolean> progress_hmi;

		private Client client;
		private Sequence sequence;
		private Data data;

		private IReadOnlyList<Key> allowedPortKeys = new List<Key> { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.Back, Key.Delete, Key.Left, Key.Up, Key.Down, Key.Right };
		private IReadOnlyList<Key> allowedIpKeys = new List<Key> { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.Back, Key.Delete, Key.Left, Key.Up, Key.Down, Key.Right, Key.OemPeriod };
		private IReadOnlyList<Key> allowedHexKeys = new List<Key> { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.Back, Key.Delete, Key.Left, Key.Up, Key.Down, Key.Right };

		public MainWindow()
		{
			InitializeComponent();

			progress_str = new Progress<string>(status =>
			{
				OutputLog.Items.Add(status);
				OutputLog.UpdateLayout();
				OutputLog.ScrollIntoView(OutputLog.Items[OutputLog.Items.Count - 1]);
			});

			progress_hmi = new Progress<Boolean>(status =>
			{
				updateUI();
			});

			ServerIpBox.Text = GetIP();
			PortNoBox.Text = "5050";

			progress_hmi.Report(false);

			client = new Client(progress_str, progress_hmi);
			sequence = new Sequence(progress_str, progress_hmi, ref client);
			data = new Data(progress_str, ref MessageList);

			data.LoadDefaults();
		}

		private void updateUI()
		{
			if (client.isSocketOpen())
			{
				StatusBar.Content = "Socket open";
			}
			else
			{
				StatusBar.Content = "Socket closed";
			}

			if(sequence.IsRunning())
			{
				StatusBar.Content += ", Sequence running";
			}
			else if (sequence.IsLoaded())
			{
				StatusBar.Content += ", Sequence loaded";
			}

			ServerIpBox.IsEnabled = !client.isSocketOpen();
			PortNoBox.IsEnabled = !client.isSocketOpen();
			RunMenuItem.IsEnabled = client.isSocketOpen() && sequence.IsLoaded() && !sequence.IsRunning();
			StopMenuItem.IsEnabled = client.isSocketOpen();
			MsgBox.IsEnabled = client.isSocketOpen() && !sequence.IsRunning();
			MessageList.IsEnabled = client.isSocketOpen();
			UpdateOpenBtnStatus();
			UpdateSendBtnStatus();
		}

		private string GetIP()
		{
			String strHostName = Dns.GetHostName();

			// Find host by name
			IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);

			// Grab the first IP addresses
			String IPStr = "";
			foreach (IPAddress ipaddress in iphostentry.AddressList)
			{
				if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
				{
					IPStr = ipaddress.ToString();
					break;
				}
			}
			return IPStr;
		}

		private void UpdateOpenBtnStatus()
		{
			if (ServerIpBox != null && !String.IsNullOrEmpty(ServerIpBox.Text) && PortNoBox != null && !String.IsNullOrEmpty(PortNoBox.Text))
			{
				Match matchIp = Regex.Match(ServerIpBox.Text, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$");
				Match matchPort = Regex.Match(PortNoBox.Text, @"\d");

				if (matchIp.Success && matchPort.Success && client != null && !client.isSocketOpen())
				{
					StartMenuItem.IsEnabled = true;
				}
				else
				{
					StartMenuItem.IsEnabled = false;
				}
			}
			else
			{
				StartMenuItem.IsEnabled = false;
			}
		}

		private void UpdateSendBtnStatus()
		{
			SendMsgButton.IsEnabled = !String.IsNullOrEmpty(MsgBox.Text) && client  != null && client.isSocketOpen() && sequence != null && !sequence.IsRunning();
		}

		private IPAddress parseIpAddr()
		{
			try
			{
				IPAddress ip = IPAddress.Parse(ServerIpBox.Text);
				return ip;
			}
			catch (Exception e)
			{
				progress_str.Report("ERROR: Could not parse server IP!");
				progress_str.Report(e.ToString());
			}

			return null;
		}

		private int parsePortNo()
		{
			int port = -1;

			try
			{
				port = System.Convert.ToInt16(PortNoBox.Text);
			}
			catch (Exception e)
			{
				progress_str.Report("ERROR: Could not parse port number!");
				progress_str.Report(e.ToString());
			}

			return port;
		}

		private void ExampleData_Click(object sender, RoutedEventArgs e)
		{
			Dialog popup = new Dialog("Example messages XML", Properties.Resources.messages_example);
			popup.ShowDialog();
		}

		private void ExampleSequence_Click(object sender, RoutedEventArgs e)
		{
			Dialog popup = new Dialog("Example sequence XML", Properties.Resources.sequence_example);
			popup.ShowDialog();
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void StopMenuItem_Click(object sender, RoutedEventArgs e)
		{
			client.closeSocket();
			progress_hmi.Report(false);
		}

		private void StartMenuItem_Click(object sender, RoutedEventArgs e)
		{
			StartMenuItem.IsEnabled = false;

			// See if we have text on the IP and Port text fields
			if (ServerIpBox.Text == "" || PortNoBox.Text == "")
			{
				progress_str.Report("IP Address and Port Number are required to connect to the Server");
				progress_hmi.Report(false);
				return;
			}

			client.openSocket(parseIpAddr(), parsePortNo());
		}

		private void LoadData_Click(object sender, RoutedEventArgs e)
		{
			// Show the dialog and get result.
			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.CheckFileExists = true;
			openDialog.DefaultExt = "*.xml";
			openDialog.Filter = "XML Files|*.xml";
			openDialog.Multiselect = false;
			openDialog.ReadOnlyChecked = true;

			if (openDialog.ShowDialog().Value) // Test result.
			{
				data.Load(openDialog.FileName);
			}
		}

		private void LoadSequence_Click(object sender, RoutedEventArgs e)
		{
			// Show the dialog and get result.
			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.CheckFileExists = true;
			openDialog.DefaultExt = "*.xml";
			openDialog.Filter = "XML Files|*.xml";
			openDialog.Multiselect = false;
			openDialog.ReadOnlyChecked = true;

			if (openDialog.ShowDialog().Value) // Test result.
			{
				sequence.Load(openDialog.FileName);
				RunMenuItem.IsEnabled = client.isSocketOpen() && sequence.IsLoaded();
			}
		}

		private void RunMenuItem_Click(object sender, RoutedEventArgs e)
		{
			sequence.Run();
		}

		private void SendMsgButton_Click(object sender, RoutedEventArgs e)
		{
			if (MsgBox.Text.Length % 2 == 1)
			{
				progress_str.Report("Error: Send data cannot have an odd number of digits");
			}
			else
			{
				client.sendMessage(MsgBox.Text);
			}
		}

		private void PortNoBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!allowedPortKeys.Contains(e.Key))
			{
				e.Handled = true;
			}
		}

		private void ServerIpBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!allowedIpKeys.Contains(e.Key))
			{
				e.Handled = true;
			}
		}

		private void Input_PastingHandler(object sender, DataObjectPastingEventArgs e)
		{
			e.CancelCommand();
		}

		private void Input_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			UpdateOpenBtnStatus();
		}

		private void MsgBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			UpdateSendBtnStatus();
		}

		private void MsgBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!allowedHexKeys.Contains(e.Key))
			{
				e.Handled = true;
			}
		}

		private void MessageList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			MyItem mi = (MyItem)MessageList.SelectedItem;
			MsgBox.Text = mi.Data;
		}
	}
}
