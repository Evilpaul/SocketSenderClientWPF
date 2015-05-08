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
		private IProgress<bool> progress_seq;
		private Client client;
		private bool is_file_loaded = false;

		private List<node> list = new List<node>();

		public Sequence(IProgress<string> pr_str, IProgress<bool> pr_seq, ref Client cl)
		{
			progress_str = pr_str;
			progress_seq = pr_seq;
			client = cl;
			is_file_loaded = false;
		}

		public bool isLoaded()
		{
			return is_file_loaded;
		}

		public async void Run()
		{
			progress_seq.Report(true);
			await do_async();
			progress_seq.Report(false);
		}

		private Task do_async()
		{
			return Task.Run(() =>
			{
				if (!is_file_loaded)
				{
					progress_str.Report("ERROR: No sequence loaded!");
					return;
				}

				foreach (node n in list)
				{
					if (!client.isSocketOpen())
						break;

					if (n.getType() == node.NodeType.Delay)
					{
						progress_str.Report("Sleeping for " + n.getDelay() + " milliseconds");
						Thread.Sleep(n.getDelay());
					}
					else
					{
						client.sendMessage(n.getValue());
					}
				}
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
				is_file_loaded = true;

				progress_str.Report("File loaded : " + filePath);
			}
			catch(XmlSchemaValidationException ex)
			{
				is_file_loaded = false;
				progress_str.Report("Validation error: " + ex.Message);
			}
		}
	}
}