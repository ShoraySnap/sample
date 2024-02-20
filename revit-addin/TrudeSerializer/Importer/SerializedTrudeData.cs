using TrudeSerializer.Components;
using System;
using System.Collections.Generic;
using TrudeSerializer.Types;

namespace TrudeSerializer.Importer
{
    internal class SerializedTrudeData
    {
        public ProjectProperties ProjectProperties;
        public Dictionary<string, TrudeWall> Walls;
        public TrudeObject<TrudeFurniture> Furniture;
        public TrudeObject<TrudeDoor> Doors;
        public Dictionary<string, TrudeFloor> Floors;
        public Dictionary<string, TrudeMass> Masses;
        public Dictionary<string, Dictionary<string, TrudeMass>> RevitLinks;

        public FamilyTypes FamilyTypes;

        public SerializedTrudeData()
        {
            this.FamilyTypes = new FamilyTypes();
            this.Walls = new Dictionary<string, TrudeWall>();
            this.Furniture = new TrudeObject<TrudeFurniture>();
            this.Doors = new TrudeObject<TrudeDoor>();
            this.Floors = new Dictionary<string, TrudeFloor>();
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
            if(this.Floors.ContainsKey(trudeFloor.elementId)) return; 
            this.Floors.Add(trudeFloor.elementId, trudeFloor);
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
    }

    class FamilyTypes
    {
        public Dictionary<String, TrudeWallType> WallTypes;
        public Dictionary<string, TrudeFloorType> FloorTypes;

        public FamilyTypes()
        {
            this.WallTypes = new Dictionary<String, TrudeWallType>();
            this.FloorTypes = new Dictionary<String, TrudeFloorType>();
        }


        public bool HasFloorType(String floorTypeName)
        {
            return this.FloorTypes.ContainsKey(floorTypeName);
        }

        public bool HasWallType(String wallTypeName)
        {
            return this.WallTypes.ContainsKey(wallTypeName);
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
    }

    class ProjectProperties
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

     class TrudeObject<TFamily>
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

