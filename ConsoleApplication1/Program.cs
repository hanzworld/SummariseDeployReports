using System;
using System.IO;
using System.Xml;
using CsvHelper;

namespace ConvertManyDeployReportsToCSV
{
	class Program
	{
		static void Main(string[] args)
		{
			#region Errors
			if (args.Length == 0 || args[0] == null)
			{
				Console.WriteLine("Please give me a folder path to your DeployReports!");
				return;
			}

			DirectoryInfo di = new DirectoryInfo(args[0]);

			if (!di.Exists)
			{
				Console.WriteLine("That directory path isn't any good sorry - something wrong with it?");
				return;
			}
			#endregion

			//figure out an output path
			var outputPath = Path.Combine(di.FullName, "combined-deploy-reports.csv");


			using (TextWriter writer = File.CreateText(outputPath))
			{
				using (CsvWriter csv = new CsvWriter(writer))
				{
					//write the CSV header line (you know, if we used the serialised objects you originally planned, this would be way easier)
					csv.WriteField("Source");
					csv.WriteField("Operation");
					csv.WriteField("Type");
					csv.WriteField("Object");
					csv.NextRecord();

					//now go through each file and spit out the results
					foreach (var file in di.EnumerateFiles("*.xml"))
					{
						XmlDocument xd = new XmlDocument();
						//stupid namespaces in SSDT files
						XmlNamespaceManager namespaces = new XmlNamespaceManager(xd.NameTable);
						namespaces.AddNamespace("ns", "http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02");
						xd.Load(file.FullName);

						//remember the name of the file we're processing so we can spit it out in the CSV file to distinguish the database
						var sourceFile = file.Name.Replace(file.Extension, "");

						//for each /DeploymentReport/Operations/Operation (grouped into Drop/Alter/Create)
						foreach (XmlNode operation in xd.SelectNodes("/ns:DeploymentReport/ns:Operations/ns:Operation", namespaces))
						{
							//remember for later to spit out in CSV
							var operationType = operation.SelectSingleNode("@Name").Value;

							//go through each actual item and find the informaton
							foreach (XmlNode item in operation.SelectNodes("ns:Item", namespaces))
							{
								csv.WriteField(sourceFile); // database
								csv.WriteField(operationType); // create/alter/drop
								csv.WriteField(item.SelectSingleNode("@Type").Value); // index, procedure, permission, table etc
								csv.WriteField(item.SelectSingleNode("@Value").Value); // actual name of schema object
								csv.NextRecord();
							}

						}

						//finish file
						Console.WriteLine("{0} converted", file.Name);
					}

					//finish job
					Console.WriteLine("CSV file complete. Take a look at {0}", outputPath);
				}
			}

		}
	}
}
