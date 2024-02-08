using Autodesk.Revit.DB;
using RevitImporter.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RevitImporter.Importer
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
        public void SetData(SerializedData importData, Element element)
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
