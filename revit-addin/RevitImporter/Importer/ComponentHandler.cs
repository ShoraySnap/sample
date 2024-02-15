﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using TrudeSerializer.Components;

namespace TrudeSerializer.Importer
{
    internal class ComponentHandler
    {
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
        public TrudeComponent GetComponent(SerializedTrudeData serializedData, Element element)
        {
            if (element is Wall)
            {
                return TrudeWall.GetSerializedComponent(serializedData, element);
            }
            else if (element is Level)
            {
                return TrudeLevel.GetSerializedComponent(element);
            }
            else if (element is Stairs  || element is Railing)
            {
                return TrudeMass.GetSerializedComponent(serializedData, element);
            }
            else if (element is RevitLinkInstance)
            {
                return TrudeRevitLink.GetSerializedComponent(serializedData, element);
            }
            //else if (TrudeFurniture.IsFurnitureCategory(element))
            //{
            //    return TrudeFurniture.GetSerializedComponent(serializedData, element);
            //}

            return TrudeComponent.GetDefaultComponent();
        }
    }
}