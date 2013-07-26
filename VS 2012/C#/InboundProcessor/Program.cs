using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

// Peter Ripley - July 2013
// Solution for Catalyst Development test
namespace InboundProcessor
{
    class Program
    {
        static void Main()
        {
            string inboundFileName = String.Format("{0}\\inbound.xml", Directory.GetCurrentDirectory());
            string validDocumentFileName = inboundFileName;
            int parsingAttempts = 0;
            
            // Loop while we have not attempted to parse a file more than twice (success, certain exceptions or user choice will cause a break from this loop).
            while(parsingAttempts++ < 2)
            {
                try
                {
                    Console.WriteLine(String.Format("Opening '{0}' for processing...\r\n", validDocumentFileName));
                    ProcessInboundFile(validDocumentFileName);
                    break;
                }
                catch (XmlException xmlEx)
                {
                    Console.WriteLine(String.Format("The XML document is not valid because it is not well-formed: {0}\r\n", xmlEx.Message));

                    // Try only once to create a valid document from an invalid one.
                    if (parsingAttempts == 1)
                    {
                        Console.Write("Create a valid document to process (y/n)? ");
                        string choice = Console.ReadLine();

                        if (choice.ToUpperInvariant() == "Y")
                        {
                            try
                            {
                                validDocumentFileName = CreateValidInboundFile(inboundFileName);

                                if (validDocumentFileName != null)
                                {
                                    Console.WriteLine(String.Format("\r\nA valid XML document was created and saved to the file '{0}'\r\n", validDocumentFileName));
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("\r\nA valid document could not be created from the original.");
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine(String.Format("The file '{0}' was not found in the executable root.", Path.GetFileName(validDocumentFileName)));
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(String.Format("An unexpected error occurred: {0}", ex.Message));
                    break;
                }
            }
            Console.Write("\r\nPress any key to exit...");
            Console.ReadKey();
        }

        // This method is specific to the file we assume we are dealing with.  It is not generic--it 'processes' the inbound XML document with knowledge of its structure.
        private static void ProcessInboundFile(String InboundFileName)
        {
            XDocument myXDocument = XDocument.Load(InboundFileName);
            string outputFileName = String.Format("{0}\\output.xml", Directory.GetCurrentDirectory());

            // Is it valid XML? How do you know? / Show the count of the element <row>  
            Console.WriteLine("This utility treats the XML document as valid because the DOM threw no exceptions while loading it.\r\n");
            Console.WriteLine(String.Format("There are {0:N0} <row> elements.\r\n", myXDocument.Descendants("row").Count()));

            // Convert the third <row> element from XML to key:value pairs. (I am assuming that 'the third <row> element' refers to ordinal position rather than Id.)
            XElement thirdRowNode = myXDocument.Descendants("row").ToArray()[2];
            Console.WriteLine("Third <row> element data:");
            Console.WriteLine(String.Format("Name: {0}", thirdRowNode.Element("name").Value));
            Console.WriteLine(String.Format("Department: {0}", thirdRowNode.Element("Department").Value));
            Console.WriteLine(String.Format("Joindate: {0:MM/dd/yyyy}", DateTime.Parse(thirdRowNode.Element("joindate").Value)));
            Console.WriteLine(String.Format("Salary: {0:###,###}\r\n", Convert.ToInt32(thirdRowNode.Element("salary").Value)));

            // Which employee has the highest salary? / Which employee has the lowest salary? / Which employee joined first?
            XElement myXElement = myXDocument.Descendants("row").OrderByDescending(x => (int)x.Element("salary")).First();
            Console.WriteLine(String.Format("{0} has the highest salary.", myXElement.Element("name").Value));

            myXElement = myXDocument.Descendants("row").OrderBy(x => (int)x.Element("salary")).First();
            Console.WriteLine(String.Format("{0} has the lowest salary.", myXElement.Element("name").Value));

            myXElement = myXDocument.Descendants("row").OrderBy(x => (DateTime)x.Element("joindate")).First();
            Console.WriteLine(String.Format("{0} joined the team first.", myXElement.Element("name").Value));

            // Add a new complete record for an employee (you must increment the element <row> counter).
            myXDocument.Element("EmployeeData").Add(new XElement("row", new XAttribute("id", myXDocument.Descendants("row").Count() + 1),
                new XElement("name", "Ishaan"),
                new XElement("Department", "Marketing"),
                new XElement("joindate", DateTime.Parse("06/01/12").ToString("yyyy-MM-dd")),
                new XElement("salary", "15000")));

            // When you are done, save your output as 'output.xml'.
            myXDocument.Save(outputFileName);
            Console.WriteLine(String.Format("\r\nAdded a record to document file '{0}'.", outputFileName));
        }

        // If the assumed file is referred to, a valid document will be created from best guessing the data it represents.
        private static string CreateValidInboundFile(string InvalidInboundFileName)
        {
            string fileText = null;
            string validInboundFileName = null;
            string seekString = null; 
            string replacementString = null;
            bool stringFound = true;
            int rowId = 0;

            using (StreamReader myStreamReader = new StreamReader(InvalidInboundFileName))
            {
                fileText = myStreamReader.ReadToEnd();
            }

            // The only fix we are doing is adding quotes around the 'id' attribute of the 'row' elements.  We are fixing known malformations.
            while (stringFound)
            {
                seekString = String.Format("<row id={0}",(++rowId));
                stringFound = fileText.Contains(seekString);

                if (stringFound)
                {
                    replacementString = String.Format(@"<row id=""{0}""", rowId);
                    fileText = fileText.Replace(seekString, replacementString);
                }
            }

            if (replacementString != null)
            {
                validInboundFileName = String.Format("{0}\\{1}.well_formed.xml", Path.GetDirectoryName(InvalidInboundFileName), Path.GetFileNameWithoutExtension(InvalidInboundFileName));
                using (StreamWriter myStremWriter = new StreamWriter(validInboundFileName))
                {
                    myStremWriter.Write(fileText);
                }
            }
            return validInboundFileName;
        }
    }
}
