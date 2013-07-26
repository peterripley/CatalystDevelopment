Imports System
Imports System.IO
Imports System.Linq
Imports System.Xml
Imports System.Xml.Linq

Module Program
    Class InboundProcessor
        Shared Sub Main()
            Dim inboundFileName As String = String.Format("{0}\inbound.xml", Directory.GetCurrentDirectory())
            Dim validDocumentFileName As String = inboundFileName

            ' Loop while we have not attempted to parse a file more than twice (success, certain exceptions or user choice will cause a break from this loop).
            For ParsingAttempts As Integer = 1 To 2
                Try
                    Console.WriteLine(String.Format("Opening '{0}' for processing..." & VbCrLf, validDocumentFileName))
                    ProcessInboundFile(validDocumentFileName)
                    Exit For
                Catch xmlEx As XmlException
                    Console.WriteLine(String.Format("The XML document is not valid because it is not well-formed: {0}" & VbCrLf, xmlEx.Message))

                    ' Try only once to create a valid document from an invalid one.
                    If ParsingAttempts = 1 Then
                        Console.Write("Create a valid document to process (y/n)? ")
                        Dim choice As String = Console.ReadLine()

                        If choice.ToUpperInvariant() = "Y" Then
                            Try
                                validDocumentFileName = CreateValidInboundFile(inboundFileName)

                                If validDocumentFileName IsNot Nothing Then
                                    Console.WriteLine(String.Format(vbCr & vbLf & "A valid XML document was created and saved to the file '{0}'" & VbCrLf, validDocumentFileName))
                                Else
                                    Throw New Exception()
                                End If
                            Catch generatedExceptionName As Exception
                                Console.WriteLine(vbCr & vbLf & "A valid document could not be created from the original.")
                                Exit For
                            End Try
                        Else
                            Exit For
                        End If
                    End If
                Catch generatedExceptionName As FileNotFoundException
                    Console.WriteLine(String.Format("The file '{0}' was not found in the executable root.", Path.GetFileName(validDocumentFileName)))
                    Exit For
                Catch ex As Exception
                    Console.WriteLine(String.Format("An unexpected error occurred: {0}", ex.Message))
                    Exit For
                End Try
            Next ParsingAttempts

            Console.Write(vbCr & vbLf & "Press any key to exit...")
            Console.ReadKey()
        End Sub

        ' This method is specific to the file we assume we are dealing with.  It is not generic--it 'processes' the inbound XML document with knowledge of its structure.
        Private Shared Sub ProcessInboundFile(InboundFileName As [String])
            Dim myXDocument As XDocument = XDocument.Load(InboundFileName)
            Dim outputFileName As String = String.Format("{0}\output.xml", Directory.GetCurrentDirectory())

            ' Is it valid XML? How do you know? / Show the count of the element <row>  
            Console.WriteLine("This utility treats the XML document as valid because the DOM threw no exceptions while loading it." & VbCrLf)
            Console.WriteLine(String.Format("There are {0:N0} <row> elements." & VbCrLf, myXDocument.Descendants("row").Count()))

            ' Convert the third <row> element from XML to key:value pairs. (I am assuming that 'the third <row> element' refers to ordinal position rather than Id.)
            Dim thirdRowNode As XElement = myXDocument.Descendants("row").ToArray()(2)
            Console.WriteLine("Third <row> element data:")
            Console.WriteLine(String.Format("Name: {0}", thirdRowNode.Element("name").Value))
            Console.WriteLine(String.Format("Department: {0}", thirdRowNode.Element("Department").Value))
            Console.WriteLine(String.Format("Joindate: {0:MM/dd/yyyy}", DateTime.Parse(thirdRowNode.Element("joindate").Value)))
            Console.WriteLine(String.Format("Salary: {0:###,###}" & VbCrLf, Convert.ToInt32(thirdRowNode.Element("salary").Value)))

            ' Which employee has the highest salary? / Which employee has the lowest salary? / Which employee joined first?
            Dim myXElement As XElement = myXDocument.Descendants("row").OrderByDescending(Function(x) CInt(x.Element("salary"))).First()
            Console.WriteLine(String.Format("{0} has the highest salary.", myXElement.Element("name").Value))

            myXElement = myXDocument.Descendants("row").OrderBy(Function(x) CInt(x.Element("salary"))).First()
            Console.WriteLine(String.Format("{0} has the lowest salary.", myXElement.Element("name").Value))

            myXElement = myXDocument.Descendants("row").OrderBy(Function(x) CDate(x.Element("joindate"))).First()
            Console.WriteLine(String.Format("{0} joined the team first.", myXElement.Element("name").Value))

            ' Add a new complete record for an employee (you must increment the element <row> counter).
            myXDocument.Element("EmployeeData").Add(New XElement("row", New XAttribute("id", myXDocument.Descendants("row").Count() + 1), New XElement("name", "Ishaan"), New XElement("Department", "Marketing"), New XElement("joindate", DateTime.Parse("06/01/12").ToString("yyyy-MM-dd")), New XElement("salary", "15000")))

            ' When you are done, save your output as 'output.xml'.
            myXDocument.Save(outputFileName)
            Console.WriteLine(String.Format(vbCr & vbLf & "Added a record to document file '{0}'.", outputFileName))
        End Sub

        ' If the assumed file is referred to, a valid document will be created from best guessing the data it represents.
        Private Shared Function CreateValidInboundFile(InvalidInboundFileName As String) As String
            Dim fileText As String = Nothing
            Dim validInboundFileName As String = Nothing
            Dim seekString As String = Nothing
            Dim replacementString As String = Nothing
            Dim stringFound As Boolean = True
            Dim rowId As Integer = 0

            Using myStreamReader As New StreamReader(InvalidInboundFileName)
                fileText = myStreamReader.ReadToEnd()
            End Using

            ' The only fix we are doing is adding quotes around the 'id' attribute of the 'row' elements.  We are fixing known malformations.
            While stringFound
                seekString = String.Format("<row id={0}", (System.Threading.Interlocked.Increment(rowId)))
                stringFound = fileText.Contains(seekString)

                If stringFound Then
                    replacementString = String.Format("<row id=""{0}""", rowId)
                    fileText = fileText.Replace(seekString, replacementString)
                End If
            End While

            If replacementString IsNot Nothing Then
                validInboundFileName = String.Format("{0}\{1}.well_formed.xml", Path.GetDirectoryName(InvalidInboundFileName), Path.GetFileNameWithoutExtension(InvalidInboundFileName))
                Using myStremWriter As New StreamWriter(validInboundFileName)
                    myStremWriter.Write(fileText)
                End Using
            End If
            Return validInboundFileName
        End Function
    End Class
End Module
