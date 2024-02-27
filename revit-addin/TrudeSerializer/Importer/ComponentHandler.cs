using Autodesk.Revit.DB;
using System;
using TrudeImporter;
using TrudeSerializer.Components;
using TrudeCeiling = TrudeSerializer.Components.TrudeCeiling;
using TrudeColumn = TrudeSerializer.Components.TrudeColumn;
using TrudeDoor = TrudeSerializer.Components.TrudeDoor;
using TrudeFloor = TrudeSerializer.Components.TrudeFloor;
using TrudeMass = TrudeSerializer.Components.TrudeMass;
using TrudeWall = TrudeSerializer.Components.TrudeWall;

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
            else if(element is Floor)
            {
                return TrudeFloor.GetSerializedComponent(serializedData, element);
            }
            else if(element is Ceiling)
            {
                return TrudeCeiling.GetSerializedComponent(serializedData, element);
            }
            else if(TrudeColumn.IsColumnCategory(element))
            {
                return TrudeColumn.GetSerializedComponent(serializedData, element);
            }
            else if (TrudeFurniture.IsFurnitureCategory(element))
            {
                return TrudeFurniture.GetSerializedComponent(serializedData, element);
            }
            else if (TrudeDoor.IsDoor(element))
            {
                return TrudeDoor.GetSerializedComponent(serializedData, element);
            }

            return TrudeMass.GetSerializedComponent(serializedData, element);
        }
    }
}