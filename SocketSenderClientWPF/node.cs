using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketSenderClientWPF
{
	class node
	{
		public enum NodeType
		{
			Message,
			Delay
		}

		public NodeType nodeType { get; private set; }
		public int Delay { get; private set; }
		public string Message { get; private set; }

		public node(int delay)
		{
			nodeType = NodeType.Delay;
			Delay = delay;
			Message = "";
		}

		public node(string value)
		{
			nodeType = NodeType.Message;
			Message = value;
			Delay = 0;
		}
	}
}
