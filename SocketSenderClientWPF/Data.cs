using System;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Schema;

namespace SocketSenderClientWPF
{
	class Data
	{
		private IProgress<string> progress_str;
		private ListView MessageList;

		public Data(IProgress<string> pr_str, ref ListView msgList)
		{
			progress_str = pr_str;
			MessageList = msgList;
		}

		public void LoadDefaults()
		{
			XmlReaderSettings readerSettings = getXmlReadSettings();

			// create an XmlReader from the passed XML string. Use the reader settings just created
			Assembly assembly = Assembly.GetExecutingAssembly();
			StreamReader streamReader = new StreamReader(assembly.GetManifestResourceStream("SocketSenderClientWPF.Resources.messages-default.xml"));
			XmlReader reader = XmlReader.Create(streamReader, readerSettings);

			parseXml(reader);
			reader.Close();
		}

		public void Load(string filePath)
		{
			XmlReaderSettings readerSettings = getXmlReadSettings();

			// create an XmlReader from the passed XML string. Use the reader settings just created
			XmlReader reader = XmlReader.Create(filePath, readerSettings);

			parseXml(reader);
			reader.Close();
		}

		private XmlReaderSettings getXmlReadSettings()
		{
			// load the XSD (schema) from the assembly's embedded resources and add it to schema set
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlSchema schema;
			using (StreamReader streamReader = new StreamReader(assembly.GetManifestResourceStream("SocketSenderClientWPF.Resources.messages.xsd")))
			{
				schema = XmlSchema.Read(streamReader, null);
			}

			// set the validation settings
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.ValidationType = ValidationType.Schema;
			readerSettings.Schemas = new XmlSchemaSet();
			readerSettings.Schemas.Add(schema);

			return readerSettings;
		}

		private void parseXml(XmlReader reader)
		{
			string name, value;

			MessageList.Items.Clear();

			try
			{
				reader.MoveToContent();

				do
				{
					if ((reader.Name == "message") && (reader.NodeType == XmlNodeType.Element))
					{
						reader.MoveToAttribute("name");
						reader.ReadAttributeValue();
						name = reader.Value.ToString();

						reader.MoveToContent();
						value = reader.ReadContentAsString();

						MessageList.Items.Add(new MyItem { Description = name, Data = value });
					}
				} while (reader.Read());
			}
			catch (XmlSchemaValidationException ex)
			{
				progress_str.Report("Validation error: " + ex.Message);
			}
		}
	}
}