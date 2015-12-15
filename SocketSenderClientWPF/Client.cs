using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketSenderClientWPF
{
	class Client
	{
		private IProgress<string> progress_str;
		private IProgress<Boolean> progress_hmi;

		private UdpClient sender;

		private Mutex mut;

		public bool IsOpen { get { return (sender != null); } }

		public Client(IProgress<string> pr_str, IProgress<Boolean> pr_hmi)
		{
			progress_str = pr_str;
			progress_hmi = pr_hmi;
			mut = new Mutex();
		}

		public void openSocket(IPAddress ip, int port)
		{
			progress_str.Report("Opening Socket...");
			sender = new UdpClient(ip.ToString(), port);
			sender.BeginReceive(DataReceived, sender);

			progress_hmi.Report(true);
		}

		public void sendMessage(string msg)
		{
			mut.WaitOne();
			try
			{
				if (sender != null)
				{
					byte[] byData = StringToByteArray(msg);
					progress_str.Report("Sending Msg : " + BitConverter.ToString(byData).Replace("-", string.Empty));
					sender.Send(byData, byData.Length);
				}
			}
			catch (SocketException se)
			{
				progress_str.Report(se.Message);
				closeSocket();
			}
			mut.ReleaseMutex();
		}

		public void closeSocket()
		{
			if (sender != null)
			{
				progress_str.Report("Closing Socket...");
				sender.Close();
				sender = null;
			}
		}

		private static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}

		private void DataReceived(IAsyncResult ar)
		{
			try
			{
				UdpClient c = (UdpClient)ar.AsyncState;
				IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
				Byte[] receivedBytes = c.EndReceive(ar, ref receivedIpEndPoint);

				// Convert data to ASCII and print in console
				string receivedText = BitConverter.ToString(receivedBytes).Replace("-", string.Empty);
				progress_str.Report(receivedIpEndPoint + ": " + receivedText + Environment.NewLine);

				// Restart listening for udp data packages
				c.BeginReceive(DataReceived, ar.AsyncState);
			}
			catch (ObjectDisposedException)
			{
				progress_str.Report("DataReceived: Socket has been closed");
				closeSocket();
				progress_hmi.Report(false);
			}
			catch (SocketException se)
			{
				progress_str.Report(se.Message);
				closeSocket();
				progress_hmi.Report(false);
			}
		}
	}
}
