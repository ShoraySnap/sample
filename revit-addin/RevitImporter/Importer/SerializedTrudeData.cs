using RevitImporter.Components;
using System;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Types;

namespace TrudeSerializer.Importer
{
    internal class SerializedTrudeData
    {
        public object families;
        public ProjectProperties ProjectProperties;
        public Dictionary<string, TrudeWall> Walls;
        public Dictionary<string, TrudeFloor> Floors;
        public TrudeFurnitureObject Furniture;

        public FamilyTypes FamilyTypes;

        public SerializedTrudeData()
        {
            this.families = new Object();
            this.FamilyTypes = new FamilyTypes();
            this.Walls = new Dictionary<string, TrudeWall>();
            this.Floors = new Dictionary<string, TrudeFloor>();
            this.Furniture = new TrudeFurnitureObject();
            this.ProjectProperties = new ProjectProperties();
        }

        public void AddWall(TrudeWall wall)
        {
            if (this.Walls.ContainsKey(wall.elementId)) return;
            this.Walls.Add(wall.elementId, wall);
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

        public void AddFurnitureFamily(string familyName, TrudeFurniture family)
        {
            this.Furniture.AddFamily(familyName, family);
        }
        public void AddFurnitureInstance(string instanceId, TrudeInstance instance)
        {
            this.Furniture.AddInstance(instanceId, instance);
        }

    }

    class FamilyTypes
    {
        public Dictionary<String, TrudeWallType> wallTypes;
        public Dictionary<String, TrudeFloorType> floorTypes;

        public FamilyTypes()
        {
            this.wallTypes = new Dictionary<String, TrudeWallType>();
            this.floorTypes = new Dictionary<string, TrudeFloorType>();
        }


        public bool HasFloorType(String floorTypeName)
        {
            return this.floorTypes.ContainsKey(floorTypeName);
        }

        public bool HasWallType(String wallTypeName)
        {
            return this.wallTypes.ContainsKey(wallTypeName);
        }

        public void AddFloorType(String floorTypeName, TrudeFloorType floorType)
        {
            if (this.HasFloorType(floorTypeName)) return;
            this.floorTypes.Add(floorTypeName, floorType);
        }

        public void AddWallType(String wallTypeName, TrudeWallType wallType)
        {
            if (this.HasWallType(wallTypeName)) return;
            this.wallTypes.Add(wallTypeName, wallType);
        }
    }

    class ProjectProperties
    {
        public Dictionary<string, TrudeLevel> levels;
        public string projectUnit;

        public ProjectProperties()
        {
            this.levels = new Dictionary<string, TrudeLevel>();
        }

        public void AddLevel(TrudeLevel level)
        {
            if (this.levels.ContainsKey(level.elementId)) return;
            this.levels.Add(level.elementId, level);
        }

        public void SetProjectUnit(string unit)
        {
            this.projectUnit = unit;
        }
    }
}

class TrudeFurnitureObject
{
    Dictionary<string, TrudeFurniture> families;
    Dictionary<string, TrudeInstance> instances;

    public TrudeFurnitureObject()
    {
        this.families = new Dictionary<string, TrudeFurniture>();
        this.instances = new Dictionary<string, TrudeInstance>();
    }

    public bool HasFamily(string familyName)
    {
        return this.families.ContainsKey(familyName);
    }

    public void AddFamily(string familyName, TrudeFurniture family)
    {
        if (this.HasFamily(familyName)) return;
        this.families.Add(familyName, family);
    }

    public void AddInstance(string instanceId, TrudeInstance instance)
    {
        this.instances.Add(instanceId, instance);
    }
}