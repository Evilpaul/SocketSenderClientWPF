using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace SocketSenderClientWPF
{
	class Sequence
	{
		private IProgress<string> progress_str;
		private IProgress<bool> progress_hmi;
		private Client client;
		private bool _isRunning;
		private bool _isLoaded;
		public bool IsRunning { get { return _isRunning; } private set { _isRunning = value; progress_hmi.Report(value); } }
		public bool IsLoaded { get { return _isLoaded; } private set { _isLoaded = value; progress_hmi.Report(value); } }

		private List<node> list = new List<node>();

		public Sequence(IProgress<string> pr_str, IProgress<bool> pr_hmi, ref Client cl)
		{
			progress_str = pr_str;
			progress_hmi = pr_hmi;
			client = cl;
			_isLoaded = false;
			_isRunning = false;
		}

		public async void Run()
		{
			await do_async();
		}

		private Task do_async()
		{
			return Task.Run(() =>
			{
				if (!IsLoaded)
				{
					progress_str.Report("ERROR: No sequence loaded!");
					return;
				}

				IsRunning = true;

				foreach (node n in list)
				{
					if (!client.isSocketOpen())
						break;

					if (n.nodeType == node.NodeType.Delay)
					{
						progress_str.Report("Sleeping for " + n.Delay + " milliseconds");
						Thread.Sleep(n.Delay);
					}
					else
					{
						client.sendMessage(n.Message);
					}
				}

				IsRunning = false;
			});
		}

		public void Load(string filePath)
		{
			// load the XSD (schema) from the assembly's embedded resources and add it to schema set
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlSchema schema;
			using (StreamReader streamReader = new StreamReader(assembly.GetManifestResourceStream("SocketSenderClientWPF.Resources.sequence.xsd")))
			{
				schema = XmlSchema.Read(streamReader, null);
			}

			// set the validation settings
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.ValidationType = ValidationType.Schema;
			readerSettings.Schemas = new XmlSchemaSet();
			readerSettings.Schemas.Add(schema);

			// create an XmlReader from the passed XML string. Use the reader settings just created
			try
			{
				XmlReader reader = XmlReader.Create(filePath, readerSettings);
				reader.MoveToContent();

				list.Clear();

				do
				{
					if ((reader.Name == "message") && (reader.NodeType == XmlNodeType.Element))
					{
						reader.MoveToAttribute("name");
						reader.ReadAttributeValue();
						string name = reader.Value.ToString();

						reader.MoveToElement();
						list.Add(new node(reader.ReadElementContentAsString()));
					}
					else if ((reader.Name == "delay") && (reader.NodeType == XmlNodeType.Element))
					{
						reader.MoveToElement();
						list.Add(new node(reader.ReadElementContentAsInt()));
					}
				} while (reader.Read());

				reader.Close();
				IsLoaded = true;

				progress_str.Report("File loaded : " + filePath);
			}
			catch (XmlSchemaValidationException ex)
			{
				IsLoaded = false;
				progress_str.Report("Validation error: " + ex.Message);
			}
		}
	}
}