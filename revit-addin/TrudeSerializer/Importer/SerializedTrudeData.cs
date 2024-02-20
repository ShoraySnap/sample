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
        public TrudeFurnitureObject Furniture;
        public Dictionary<string, TrudeMass> Masses;
        public Dictionary<string, Dictionary<string, TrudeMass>> RevitLinks;

        public FamilyTypes FamilyTypes;

        public SerializedTrudeData()
        {
            this.FamilyTypes = new FamilyTypes();
            this.Walls = new Dictionary<string, TrudeWall>();
            this.Furniture = new TrudeFurnitureObject();
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
}

class TrudeFurnitureObject
{
    public Dictionary<string, TrudeFurniture> Families;
    public Dictionary<string, TrudeInstance> Instances;

    public TrudeFurnitureObject()
    {
        this.Families = new Dictionary<string, TrudeFurniture>();
        this.Instances = new Dictionary<string, TrudeInstance>();
    }

    public bool HasFamily(string familyName)
    {
        return this.Families.ContainsKey(familyName);
    }

    public void AddFamily(string familyName, TrudeFurniture family)
    {
        if (this.HasFamily(familyName)) return;
        this.Families.Add(familyName, family);
    }

    public void AddInstance(string instanceId, TrudeInstance instance)
    {
        this.Instances.Add(instanceId, instance);
    }
}