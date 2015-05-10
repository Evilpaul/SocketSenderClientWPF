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
		private bool isRunning;
		private bool isLoaded;

		private List<node> list = new List<node>();

		public Sequence(IProgress<string> pr_str, IProgress<bool> pr_hmi, ref Client cl)
		{
			progress_str = pr_str;
			progress_hmi = pr_hmi;
			client = cl;
			isLoaded = false;
			isRunning = false;
		}

		public bool IsRunning()
		{
			return isRunning;
		}

		private void Set_IsRunning(bool value)
		{
			isRunning = value;
			progress_hmi.Report(isRunning);
		}

		public bool IsLoaded()
		{
			return isLoaded;
		}

		private void Set_IsLoaded(bool value)
		{
			isLoaded = value;
			progress_hmi.Report(isLoaded);
		}

		public async void Run()
		{
			await do_async();
		}

		private Task do_async()
		{
			return Task.Run(() =>
			{
				if (!isLoaded)
				{
					progress_str.Report("ERROR: No sequence loaded!");
					return;
				}

				Set_IsRunning(true);

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

				Set_IsRunning(false);
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
				Set_IsLoaded(true);

				progress_str.Report("File loaded : " + filePath);
			}
			catch (XmlSchemaValidationException ex)
			{
				Set_IsLoaded(false);
				progress_str.Report("Validation error: " + ex.Message);
			}
		}
	}
}