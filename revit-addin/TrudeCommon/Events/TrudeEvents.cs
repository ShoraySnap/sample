﻿using System;
using System.Collections.Generic;
using System.Text;

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

        // REVIT PLUGIN EVENTS
        REVIT_PLUGIN_VIEW_3D,
        REVIT_PLUGIN_VIEW_OTHER,
        REVIT_CLOSED,
        REVIT_PLUGIN_IMPORT_TO_REVIT_START,
        REVIT_PLUGIN_PROGRESS_UPDATE,
        REVIT_PLUGIN_IMPORT_TO_REVIT_SUCCESS,

        // COMMUNICATION EVENTS
        DATA_FROM_PLUGIN,
        DATA_FROM_MANAGER_UI,
    }

    public static class TrudeEventUtils
    {
        public static string GetEventName(TRUDE_EVENT type)
        {
            return Enum.GetName(typeof(TRUDE_EVENT), type);
        }
    }
}
