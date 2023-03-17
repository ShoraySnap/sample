using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Dynamo.Applications;
using Newtonsoft.Json;
using System.IO;
using System.IO.Pipes;
using SnaptrudeManagerAddin;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DynamoExport : IExternalCommand
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
                // return OpenDynamo(commandData, ref message, elements);

                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                string name = doc.Title; // name of the current project
                string path = doc.PathName;


                log("Revit addin clicked");
        
                string requestURL = "snaptrude://start?name=" + name;
                // System.Diagnostics.Process.Start("explorer", requestURL);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(requestURL) { UseShellExecute = true });

                // System.Diagnostics.Process.Start("explorer", "snaptrude://start");

                var server = new NamedPipeServerStream("snaptrudeRevitPipe");

                log("Waiting for Connection");
                server.WaitForConnection();
                log("Connection Established");

                var ss = new StreamString(server);
                // Validate the server's signature string.
                var data = ss.ReadString();

                // Console.WriteLine(data);

                var REVIT_PIPE_MSG_BEGIN_IMPORT = "beginImport"; // 11 characters
                var REVIT_PIPE_MSG_BEGIN_EXPORT = "beginExport"; // 11 characters
                var REVIT_PIPE_MSG_STOP = "stopWaiting"; // 11 characters

                if (data == REVIT_PIPE_MSG_BEGIN_EXPORT)
                // if (!String.IsNullOrEmpty(data))
                {
                    server.Close();

                    log("Calling dynamo");
                    writeAndClose();

                    return OpenDynamo(commandData, ref message, elements);
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

                    SnaptrudeManagerAddin.Command trudeImporter = new SnaptrudeManagerAddin.Command();
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

        public Result OpenDynamo(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get application and documnet objects and start transaction
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            string version = uiapp.Application.VersionNumber;

            string dynamoFileName;

            if (version == "2019")
            {
                dynamoFileName = @"revit-snaptrude-2019.dyn";
            }
            else if (version == "2020")
            {
                dynamoFileName = @"revit-snaptrude-2020.dyn";
            }
            else if (version == "2021" || version == "2022")
            {
                dynamoFileName = @"revit-snaptrude.dyn";
            }
            else
            {
                return Result.Failed;
            }

           
            string journalDynamoPath = getAppDataPath(dynamoFileName);

            DynamoRevit dynamoRevit = new DynamoRevit();

            DynamoRevitCommandData dynamoRevitCommandData = new DynamoRevitCommandData();
            dynamoRevitCommandData.Application = commandData.Application;

              
            List<Dictionary<string, string>> ModelNodesInfo = new List<Dictionary<string, string>>();

            /*Dictionary<string, string> StreamURL = new Dictionary<string, string>();
            StreamURL.Add(Dynamo.Applications.JournalNodeKeys.Id, "68c382ea19a94e49b6ed3c3fd9e50a78");
            StreamURL.Add(Dynamo.Applications.JournalNodeKeys.Name, "StreamURL");
            StreamURL.Add(Dynamo.Applications.JournalNodeKeys.Value, "https://speckle.xyz/streams/f17e6a081a");

                
            ModelNodesInfo.Add(StreamURL);*/

            // the above config should work, but not working

            IDictionary<string, string> journalData = new Dictionary<string, string>
            {
                { Dynamo.Applications.JournalKeys.ShowUiKey, true.ToString() }, // don't show DynamoUI at runtime
                { Dynamo.Applications.JournalKeys.AutomationModeKey, false.ToString() }, //run journal automatically
                { Dynamo.Applications.JournalKeys.DynPathKey, journalDynamoPath }, //run node at this file path
                { Dynamo.Applications.JournalKeys.DynPathExecuteKey, true.ToString() }, // The journal file can specify if the Dynamo workspace opened from DynPathKey will be executed or not. If we are in automation mode the workspace will be executed regardless of this key.
                { Dynamo.Applications.JournalKeys.ForceManualRunKey, true.ToString() }, // don't run in manual mode
                { Dynamo.Applications.JournalKeys.ModelShutDownKey, true.ToString() },
                { Dynamo.Applications.JournalKeys.ModelNodesInfo, JsonConvert.SerializeObject(ModelNodesInfo) }

            };


            dynamoRevitCommandData.JournalData = journalData;
            Result externalCommandResult = dynamoRevit.ExecuteCommand(dynamoRevitCommandData);
            return externalCommandResult;
            // return Result.Succeeded;
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