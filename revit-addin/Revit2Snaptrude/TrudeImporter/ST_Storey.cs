﻿using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Snaptrude
{
    public class ST_Storey
    {
        public string id { get; set; }
        public double basePosition { get; set; }
        public int levelNumber { get; set; }
        public Level level { get; set; }

        public ST_Storey() { }
        public ST_Storey(JToken storeyData)
        {
            id = storeyData["id"].ToString();
            levelNumber = (int)storeyData["value"];
            basePosition = UnitsAdapter.convertToRevit((double)storeyData["base"]);
        }

        /// <summary>
        /// Wrapper for creating a level or storey.
        /// </summary>
        /// <param name="newDoc">Revit document in operation.</param>
        /// <returns>Ref to the created level.</returns>
        public Level CreateLevel(Document newDoc)
        {
            this.level = Level.Create(newDoc, this.basePosition);

            ViewFamilyType floorPlanType = new FilteredElementCollector(newDoc)
                                .OfClass(typeof(ViewFamilyType))
                                .Cast<ViewFamilyType>()
                                .FirstOrDefault<ViewFamilyType>(x => ViewFamily.FloorPlan == x.ViewFamily);

            ViewPlan floorPlan = ViewPlan.Create(newDoc, floorPlanType.Id, level.Id);
            if (floorPlan.CanModifyViewDiscipline()) floorPlan.Discipline = ViewDiscipline.Architectural;

            return level;
        }
    }
}
