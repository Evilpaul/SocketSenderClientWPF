using System.Windows;
using System.Windows.Documents;

namespace SocketSenderClientWPF
{
	/// <summary>
	/// Interaction logic for Dialog.xaml
	/// </summary>
	public partial class Dialog : Window
	{
		public Dialog(string title, string xml_ref)
		{
			InitializeComponent();

			this.Title = title;
			rtb.Document.Blocks.Clear();
			rtb.Document.Blocks.Add(new Paragraph(new Run(xml_ref)));
		}
	}
}
