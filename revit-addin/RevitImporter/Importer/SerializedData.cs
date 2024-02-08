using Autodesk.Revit.DB;
using RevitImporter.Components;
using RevitImporter.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace RevitImporter.Importer
{
    internal class SerializedData
    {
        public object families;
        public Dictionary<string, TrudeWall> walls;
        public Dictionary<string, SnaptrudeLevel> levels;
        public FamilyTypes familyTypes;

        public SerializedData()
        {
            this.families = new Object();
            this.familyTypes = new FamilyTypes();
            this.walls = new Dictionary<string, TrudeWall>();
            this.levels = new Dictionary<string, SnaptrudeLevel>();
        }

        public void AddWall(TrudeWall wall)
        {
            if (this.walls.ContainsKey(wall.elementId)) return;
            this.walls.Add(wall.elementId, wall);
        }

        public void AddLevel(SnaptrudeLevel level)
        {
            if (this.levels.ContainsKey(level.elementId)) return;
            this.levels.Add(level.elementId, level);
        }
    }

    class FamilyTypes
    {
        public Dictionary<String, TrudeWallType> wallTypes;

        public FamilyTypes()
        {
            this.wallTypes = new Dictionary<String, TrudeWallType>();
        }

        public bool HasWallType(String wallTypeName)
        {
            return this.wallTypes.ContainsKey(wallTypeName);
        }

        public void AddWallType(String wallTypeName, TrudeWallType wallType)
        {
            if (this.HasWallType(wallTypeName)) return;
            this.wallTypes.Add(wallTypeName, wallType);
        }


    }


    //class families
    //{
    //    public
    //}
}
