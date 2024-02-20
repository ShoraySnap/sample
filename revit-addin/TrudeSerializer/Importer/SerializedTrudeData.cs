using System;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Types;

namespace TrudeSerializer.Importer
{
    internal class SerializedTrudeData
    {
        public ProjectProperties ProjectProperties;
        public Dictionary<string, TrudeWall> Walls;
        public TrudeObject<TrudeFurniture> Furniture;
        public TrudeObject<TrudeDoor> Doors;

        public FamilyTypes FamilyTypes;

        public SerializedTrudeData()
        {
            this.FamilyTypes = new FamilyTypes();
            this.Walls = new Dictionary<string, TrudeWall>();
            this.Furniture = new TrudeObject<TrudeFurniture>();
            this.Doors = new TrudeObject<TrudeDoor>();
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

        public FamilyTypes()
        {
            this.WallTypes = new Dictionary<String, TrudeWallType>();
        }

        public bool HasWallType(String wallTypeName)
        {
            return this.WallTypes.ContainsKey(wallTypeName);
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

