using Autodesk.Revit.DB;
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
            else if (element is Floor)
            {
                return TrudeFloor.GetSerializedComponent(serializedData, element);
            }
            else if (element is Ceiling)
            {
                return TrudeCeiling.GetSerializedComponent(serializedData, element);
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