using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TrudeCommon.DataTransfer;

namespace TrudeCommon.Events
{
    public enum TRUDE_EVENT
    {
        // MANAGER UI EVENTS
        MANAGER_UI_OPEN,
        MANAGER_UI_CLOSE,
        MANAGER_UI_MAIN_WINDOW_RMOUSE,
        MANAGER_UI_REQ_IMPORT_TO_REVIT,
        MANAGER_UI_REQ_ABORT_IMPORT,
        MANAGER_UI_REQ_EXPORT_TO_SNAPTRUDE,
        MANAGER_UI_REQ_ABORT_EXPORT,

        // REVIT PLUGIN EVENTS
        REVIT_PLUGIN_DOCUMENT_OPENED,
        REVIT_PLUGIN_DOCUMENT_CLOSED,

        REVIT_PLUGIN_VIEW_3D,
        REVIT_PLUGIN_VIEW_OTHER,
        REVIT_CLOSED,
        REVIT_PLUGIN_PROGRESS_UPDATE,
        REVIT_PLUGIN_IMPORT_TO_REVIT_START,
        REVIT_PLUGIN_IMPORT_TO_REVIT_SUCCESS,
        REVIT_PLUGIN_IMPORT_TO_REVIT_FAILED,
        REVIT_PLUGIN_IMPORT_TO_REVIT_ABORTED,
        REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_START,
        REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_SUCCESS,
        REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_FAILED,
        REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_ABORTED,

        // COMMUNICATION EVENTS
        DATA_FROM_PLUGIN,
        DATA_FROM_MANAGER_UI,
        REVIT_PLUGIN_PROJECTNAME_AND_FILETYPE,

        // BROWSER EVENTS
        BROWSER_LOGIN_CREDENTIALS,
    }

    public static class TrudeEventUtils
    {
        public static string GetEventName(TRUDE_EVENT type)
        {
            return Enum.GetName(typeof(TRUDE_EVENT), type);
        }

        public static bool IsEventGlobal(TRUDE_EVENT type)
        {
            // GLOBAL EVENTS THAT DON'T CHECK HANDSHAKE
            switch(type)
            {
                case TRUDE_EVENT.BROWSER_LOGIN_CREDENTIALS:
                    return true;
            }

            return false;
        }
    }


    public static class HandshakeManager
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        private static DataChannel _channel = new DataChannel("PID_HANDSHAKE", 1024);

        public static void SetHandshakeName(string a, string b)
        {
            string data = $"{a};{b}";
            _channel.WriteString(data);
            logger.Debug("Written handshake data: {0}", data);
        }

        public static (string, string) GetHandshakeName()
        {
            var data = _channel.ReadString();
            if(data == null || data.Length == 0)
            {
                logger.Warn("No handshake data present!");
            }
            else
            {
                string[] content = data.Split(';');
                if(content.Length == 2)
                {
                    string a = content[0];
                    string b = content[1];
                    return (a, b);
                }
            }

            return ("NONE", "NONE");
        }

        public static bool IsHandshakeValid()
        {
            (string a,string b) = HandshakeManager.GetHandshakeName();
            string pid = Process.GetCurrentProcess().Id.ToString();

            bool flag = pid == a || pid == b;
            if(!flag)
            {
                logger.Warn("Handshake ID: {0} <-> {1} does not match with receiver pid {2}",
                    a, b, pid);
            }
            return flag;
        }
    }
}
