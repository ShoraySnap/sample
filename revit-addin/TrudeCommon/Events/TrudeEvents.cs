using System;
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

        // REVIT PLUGIN EVENTS
        REVIT_PLUGIN_VIEW_3D,
        REVIT_PLUGIN_VIEW_OTHER,
    }

    public static class TrudeEventUtils
    {
        public static string GetEventName(TRUDE_EVENT type)
        {
            switch(type)
            {
                case TRUDE_EVENT.MANAGER_UI_OPEN: return "MANAGER_UI_OPEN";
                case TRUDE_EVENT.MANAGER_UI_CLOSE: return "MANAGER_UI_CLOSE";
                case TRUDE_EVENT.MANAGER_UI_MAIN_WINDOW_RMOUSE: return "MANAGER_UI_MAIN_WINDOW_RMOUSE";
                case TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D: return "REVIT_PLUGIN_VIEW_3D";
                case TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER: return "REVIT_PLUGIN_VIEW_OTHER";
            }

            return "UNKNOWN_EVENT";
        }
    }
}
