using TrudeSerializer.Components;
using System;
using System.Collections.Generic;
using TrudeSerializer.Types;
using Newtonsoft.Json;
using System.Reflection;

namespace TrudeSerializer.Importer
{
    internal class SerializedTrudeData
    {
        public ProjectProperties ProjectProperties { get; set; }
        public Dictionary<string, TrudeWall> Walls { get; set; }
        public TrudeObject<TrudeFurniture> Furniture { get; set; }
        public TrudeObject<TrudeDoor> Doors { get; set; }
        public TrudeObject<TrudeWindow> Windows { get; set; }
        public Dictionary<string, TrudeFloor> Floors { get; set; }
        public Dictionary<string, TrudeMass> Masses { get; set; }
        public Dictionary<string, Dictionary<string, TrudeMass>> RevitLinks { get; set; }
        public FamilyTypes FamilyTypes { get; set; }
        public Dictionary<string, TrudeCeiling> Ceilings { get; set; }

        public SerializedTrudeData()
        {
            this.FamilyTypes = new FamilyTypes();
            this.Walls = new Dictionary<string, TrudeWall>();
            this.Furniture = new TrudeObject<TrudeFurniture>();
            this.Doors = new TrudeObject<TrudeDoor>();
            this.Windows = new TrudeObject<TrudeWindow>();
            this.Floors = new Dictionary<string, TrudeFloor>();
            this.Ceilings = new Dictionary<string, TrudeCeiling>();
            this.Masses = new Dictionary<string, TrudeMass>();
            this.RevitLinks = new Dictionary<string, Dictionary<string, TrudeMass>>();
            this.ProjectProperties = new ProjectProperties();
        }
        public void AddWall(TrudeWall wall)
        {
            if (this.Walls.ContainsKey(wall.elementId)) return;
            this.Walls.Add(wall.elementId, wall);
        }

        public void AddMass(TrudeMass mass)
        {
            if (this.Masses.ContainsKey(mass.elementId)) return;
            this.Masses.Add(mass.elementId, mass);
        }

        public void AddLevel(TrudeLevel level)
        {
            ProjectProperties.AddLevel(level);
        }

        public void AddFloor(TrudeFloor trudeFloor)
        {
            if (this.Floors.ContainsKey(trudeFloor.elementId)) return;
            this.Floors.Add(trudeFloor.elementId, trudeFloor);
        }

        public void AddCeiling(TrudeCeiling trudeCeiling)
        {
            if (this.Ceilings.ContainsKey(trudeCeiling.elementId)) return;
            this.Ceilings.Add(trudeCeiling.elementId, trudeCeiling);
        }

        public void SetProjectUnit(string unit)
        {
            ProjectProperties.SetProjectUnit(unit);
        }

        public void AddFurnitureFamily(string familyName, TrudeFamily family)
        {
            this.Furniture.AddFamily(familyName, family);
        }

        public void AddFurnitureInstance(string instanceId, TrudeFurniture instance)
        {
            this.Furniture.AddInstance(instanceId, instance);
        }

        public void AddDoorFamily(string familyName, TrudeFamily family)
        {
            this.Doors.AddFamily(familyName, family);
        }

        public void AddDoorInstance(string instanceId, TrudeDoor instance)
        {
            this.Doors.AddInstance(instanceId, instance);
        }

        public void AddWindowFamily(string familyName, TrudeFamily family)
        {
            this.Windows.AddFamily(familyName, family);
        }

        public void AddWindowInstance(string instanceId, TrudeWindow instance)
        {
            this.Windows.AddInstance(instanceId, instance);
        }

        public string SerializeProjectProperties()
        {
            string projectProperties = JsonConvert.SerializeObject(this.ProjectProperties);
            return projectProperties;
        }

        public Dictionary<string, string> GetSerializedObject()
        {
            Dictionary<string, string> serializedData = new Dictionary<string, string>();
            PropertyInfo[] properties = this.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                object propertyValue = property.GetValue(this);

                if (propertyValue != null)
                {
                    string serializedValue = JsonConvert.SerializeObject(propertyValue);
                    serializedData.Add(propertyName, serializedValue);
                }
            }

            return serializedData;
        }
    }

    internal class FamilyTypes
    {
        public Dictionary<String, TrudeWallType> WallTypes;
        public Dictionary<String, TrudeFloorType> FloorTypes;
        public Dictionary<String, TrudeCeilingType> CeilingTypes;

        public FamilyTypes()
        {
            this.WallTypes = new Dictionary<String, TrudeWallType>();
            this.FloorTypes = new Dictionary<String, TrudeFloorType>();
            this.CeilingTypes = new Dictionary<String, TrudeCeilingType>();
        }

        public bool HasFloorType(String floorTypeName)
        {
            return this.FloorTypes.ContainsKey(floorTypeName);
        }

        public bool HasWallType(String wallTypeName)
        {
            return this.WallTypes.ContainsKey(wallTypeName);
        }
        public bool HasCeilingType(String ceilingTypeName)
        {
            return this.CeilingTypes.ContainsKey(ceilingTypeName);
        }

        public void AddFloorType(String floorTypeName, TrudeFloorType floorType)
        {
            if (this.HasFloorType(floorTypeName)) return;
            this.FloorTypes.Add(floorTypeName, floorType);
        }

        public void AddWallType(String wallTypeName, TrudeWallType wallType)
        {
            if (this.HasWallType(wallTypeName)) return;
            this.WallTypes.Add(wallTypeName, wallType);
        }

        internal void AddCeilingType(String ceilingTypeName, TrudeCeilingType ceilingType)
        {
            if (this.HasCeilingType(ceilingTypeName)) return;
            this.CeilingTypes.Add(ceilingTypeName, ceilingType);
        }

    }

    internal class ProjectProperties
    {
        public Dictionary<string, TrudeLevel> Levels;
        public string ProjectUnit;

        public ProjectProperties()
        {
            this.Levels = new Dictionary<string, TrudeLevel>();
        }

        public void AddLevel(TrudeLevel level)
        {
            if (this.Levels.ContainsKey(level.elementId)) return;
            this.Levels.Add(level.elementId, level);
        }

        public void SetProjectUnit(string unit)
        {
            this.ProjectUnit = unit;
        }
    }

    internal class TrudeObject<TFamily>
    {
        public Dictionary<string, TrudeFamily> Families;
        public Dictionary<string, TFamily> Instances;

        public TrudeObject()
        {
            this.Families = new Dictionary<string, TrudeFamily>();
            this.Instances = new Dictionary<string, TFamily>();
        }

        public bool HasFamily(string familyName)
        {
            return this.Families.ContainsKey(familyName);
        }

        public void AddFamily(string familyName, TrudeFamily family)
        {
            if (this.HasFamily(familyName)) return;
            this.Families.Add(familyName, family);
        }

        public void AddInstance(string instanceId, TFamily instance)
        {
            this.Instances.Add(instanceId, instance);
        }
    }
}