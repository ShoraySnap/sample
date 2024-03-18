using Autodesk.Revit.DB;
using System.Linq;
using TrudeSerializer.Components;
using TrudeSerializer.Debug;
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
        public enum FamilyFolder
        {
            Doors,
            Windows,
            GenericModel,
            Furniture,
        }
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

        public void AddLevelsToSerializedData(SerializedTrudeData serializedData, Document doc)
        {
            foreach (Level level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
            {
                if (level == null)
                {
                    continue;
                }
                TrudeLevel levelComponent = TrudeLevel.GetSerializedComponent(level);
                if (levelComponent.elementId != "-1")
                {
                    serializedData.AddLevel(levelComponent);
                }
            }
        }
        public TrudeComponent GetComponent(SerializedTrudeData serializedData, Element element)
        {
            TrudeLogger.Instance.CountInput(element);
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
            else if (TrudeColumn.IsColumnCategory(element))
            {
                return TrudeColumn.GetSerializedComponent(serializedData, element);
            }
            else if (TrudeCurtainWall.IsCurtainWallComponent(element))
            {
                return TrudeCurtainWall.GetSerializedComponent(serializedData, element);
            }
            else if (TrudeFurniture.IsFurnitureCategory(element))
            {
                return TrudeFurniture.GetSerializedComponent(serializedData, element);
            }
            else if (TrudeDoor.IsDoor(element))
            {
                return TrudeDoor.GetSerializedComponent(serializedData, element);
            }
            else if (TrudeWindow.IsWindow(element))
            {
                return TrudeWindow.GetSerializedComponent(serializedData, element);
            }
            else if (TrudeGenericModel.IsGenericModel(element))
            {
                return TrudeGenericModel.GetSerializedComponent(serializedData, element);
            }
            else if (element is RoofBase)
            {
                return TrudeRoof.GetSerializedComponent(serializedData, element);
            }

            return TrudeMass.GetSerializedComponent(serializedData, element);
        }

        public void AddComponent(SerializedTrudeData serializedData, TrudeComponent component)
        {
            string instanceId = component.elementId.ToString();
            if (component is TrudeWall)
            {
                TrudeWall wall = component as TrudeWall;
                serializedData.AddWall(wall);
            }
            else if (component is TrudeMass)
            {
                TrudeMass mass = component as TrudeMass;
                serializedData.AddMass(mass);
            }
            else if (component is TrudeLevel)
            {
                TrudeLevel level = component as TrudeLevel;
                serializedData.AddLevel(level);
            }
            else if (component is TrudeFloor)
            {
                TrudeFloor floor = component as TrudeFloor;
                serializedData.AddFloor(floor);
            }
            else if (component is TrudeCeiling)
            {
                TrudeCeiling ceiling = component as TrudeCeiling;
                serializedData.AddCeiling(ceiling);
            }
            else if (component is TrudeColumn)
            {
                TrudeColumn column = component as TrudeColumn;
                serializedData.AddColumn(column);
            }
            else if (component is TrudeCurtainWall)
            {
                TrudeCurtainWall curtainWall = component as TrudeCurtainWall;
                serializedData.AddCurtainWall(curtainWall);
            }
            else if (component is TrudeRoof)
            {
                TrudeRoof roof = component as TrudeRoof;
                serializedData.AddRoof(roof);
            }
            else if (component is TrudeFurniture)
            {
                TrudeFurniture furnitureInstance = component as TrudeFurniture;
                serializedData.AddFurnitureInstance(instanceId, furnitureInstance);
            }
            else if (component is TrudeGenericModel)
            {
                TrudeGenericModel genericModelInstance = component as TrudeGenericModel;
                serializedData.AddGenericModelInstance(instanceId, genericModelInstance);
            }
            else if (component is TrudeDoor)
            {
                TrudeDoor doorInstance = component as TrudeDoor;
                serializedData.AddDoorInstance(instanceId, doorInstance);
            }
            else if (component is TrudeWindow)
            {
                TrudeWindow windowInstance = component as TrudeWindow;
                serializedData.AddWindowInstance(instanceId, windowInstance);
            }

            TrudeLogger.Instance.CountOutput(component);
        }

        public void AddComponent(SerializedTrudeData serializedData, TrudeMass mass, string revitLink, string elementId)
        {
            serializedData.RevitLinks[revitLink].Add(elementId, mass);
        }

        public void AddFamily(SerializedTrudeData serializedData, FamilyFolder folder, string familyName, TrudeFamily family)
        {

            switch (folder)
            {
                case FamilyFolder.Furniture:
                    serializedData.AddFurnitureFamily(familyName, family);
                    break;
                case FamilyFolder.GenericModel:
                    serializedData.AddGenericModelFamily(familyName, family);
                    break;
                case FamilyFolder.Doors:
                    serializedData.AddDoorFamily(familyName, family);
                    break;
                case FamilyFolder.Windows:
                    serializedData.AddWindowFamily(familyName, family);
                    break;
            }
        }

        public void RemoveFamily(SerializedTrudeData serializedData, FamilyFolder folder, string familyName)
        {
            switch (folder)
            {
                case FamilyFolder.Furniture:
                    serializedData.Furniture.RemoveFamily(familyName);
                    break;
                case FamilyFolder.GenericModel:
                    serializedData.GenericModel.RemoveFamily(familyName);
                    break;
                case FamilyFolder.Doors:
                    serializedData.Doors.RemoveFamily(familyName);
                    break;
                case FamilyFolder.Windows:
                    serializedData.Windows.RemoveFamily(familyName);
                    break;
            }
        }

        public void SetProjectUnit(SerializedTrudeData serializedData, string unit)
        {
            serializedData.SetProjectUnit(unit);
        }

        public void CleanSerializedData(SerializedTrudeData serializedData)
        {
            serializedData.CleanSerializedData();
        }
    }
}