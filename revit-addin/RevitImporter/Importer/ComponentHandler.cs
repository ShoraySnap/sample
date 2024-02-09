using Autodesk.Revit.DB;
using System;
using TrudeSerializer.Components;

namespace TrudeSerializer.Importer
{
    internal class ComponentHandler
    {
        String[] SUPPORTED_PARAMETRIC = new String[] { "Wall" };

        private ComponentHandler() { }
        private static ComponentHandler instance = null;
        public static ComponentHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ComponentHandler();
                }
                return instance;
            }
        }
        public void SetData(SerializedTrudeData importData, Element element)
        {
            if (element is Wall)
            {
                TrudeWall.SetImportData(importData, element);
                return;
            }
            if (element is Level)
            {
                SnaptrudeLevel.SetImportData(importData, element);
                return;
            }

            return;
        }
    }
}