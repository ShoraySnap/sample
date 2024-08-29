using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using TrudeImporter;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SnaptrudeManager : IExternalCommand
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

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
                GlobalVariables.Document = doc;
                string name = doc.Title; // name of the current project
                string path = doc.PathName;
                string fileType = doc.IsFamilyDocument ? "rfa" : "rvt";

                log("Revit addin clicked");
                logger.Info("Revit addin clicked!");

                //WPFTODO: CHECKFORUPDATES
                var currentVersion = "2.1";
                var updateVersion = "2.2";


                writeAndClose();

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;

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

        // TODO: Move this to common assembly
        private string getAppDataPath(string fileName)
        {
            string snaptrudeManagerPath = "SnaptrudeManager";
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
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