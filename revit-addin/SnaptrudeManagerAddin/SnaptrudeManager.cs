using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SnaptrudeManager : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            StringBuilder sb = new StringBuilder();
            string logFileName = @"revit.log";
            string logFilePath = getAppDataPath(logFileName);

            DateTime now = DateTime.Now;
            log(now.ToString("dddd, dd MMMM yyyy HH:mm:ss"));

            void log(string msg)
            {
                Console.WriteLine(msg);
                sb.Append(msg);
                sb.Append(Environment.NewLine);
            }

            void writeAndClose()
            {
                try
                {
                    File.AppendAllText(logFilePath, sb.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Can't write to file");
                }
                sb.Clear();
            }

            try
            {

                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                string name = doc.Title; // name of the current project
                string path = doc.PathName;
                string fileType = doc.PathName.Substring(doc.PathName.Length - 3);


                log("Revit addin clicked");

                string requestURL = "snaptrude://start?name=" + name + "&fileType=" + fileType;
                // System.Diagnostics.Process.Start("explorer", requestURL);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(requestURL) { UseShellExecute = true });


                var server = new NamedPipeServerStream("snaptrudeRevitPipe");

                log("Waiting for Connection");
                server.WaitForConnection();
                log("Connection Established");

                var ss = new StreamString(server);
                // Validate the server's signature string.
                var data = ss.ReadString();

                var REVIT_PIPE_MSG_BEGIN_IMPORT = "beginImport"; // 11 characters
                var REVIT_PIPE_MSG_BEGIN_EXPORT = "beginExport"; // 11 characters
                var REVIT_PIPE_MSG_STOP = "stopWaiting"; // 11 characters

                if (data == REVIT_PIPE_MSG_BEGIN_EXPORT)
                {
                    server.Close();

                    log("Calling revit importer");
                    writeAndClose();

                    TrudeSerializer.Command trudeSerializer = new TrudeSerializer.Command();
                    return trudeSerializer.Execute(commandData, ref message, elements);
                }
                else if (data == REVIT_PIPE_MSG_STOP)
                {
                    server.Close();

                    log("Manager closed or did not respond");
                    writeAndClose();

                    return Result.Failed;
                }
                else if (data == REVIT_PIPE_MSG_BEGIN_IMPORT)
                {
                    server.Close();

                    log("Calling snaptrude importer");
                    writeAndClose();

                    Command trudeImporter = new Command();
                    return trudeImporter.Execute(commandData, ref message, elements);

                }
                else
                {
                    server.Close();

                    log("Unknown response");
                    log(data);
                    writeAndClose();

                    return Result.Failed;
                }

            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void ShowFailedDialogue()
        {
            TaskDialog mainDialog = new TaskDialog("Snaptrude Export Status");
            mainDialog.MainInstruction = "Snaptrude Export Status";
            mainDialog.MainContent = "Failed to export the model to Snaptrude.";

            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();
        }

        private string getAppDataPath(string fileName)
        {
            string snaptrudeManagerPath = "snaptrude-manager";
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                snaptrudeManagerPath,
                fileName
            );

            return path;
        }

    }

    public class StreamString
    {
        private Stream ioStream;
        private UTF8Encoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UTF8Encoding();
        }

        public string ReadString()
        {
            /*int len;
            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();*/

            int len = 11;
            var inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}