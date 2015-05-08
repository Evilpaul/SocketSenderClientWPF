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

		private NodeType nodeType;
		public int delay_val;
		public string msg_val;

		public node(int delay)
		{
			nodeType = NodeType.Delay;
			delay_val = delay;
		}

		public node(string value)
		{
			nodeType = NodeType.Message;
			msg_val = value;
		}

		public NodeType getType()
		{
			return nodeType;
		}

		public int getDelay()
		{
			if (nodeType == NodeType.Delay)
				return delay_val;
			else
				throw new InvalidOperationException("This node has no delay value");
		}

		public string getValue()
		{
			if (nodeType == NodeType.Message)
				return msg_val;
			else
				throw new InvalidOperationException("This node has no message value");
		}
	}
}
