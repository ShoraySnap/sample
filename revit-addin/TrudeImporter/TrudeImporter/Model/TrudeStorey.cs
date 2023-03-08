using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeStorey
    {
        public string id { get; set; }
        public double basePosition { get; set; }
        public int levelNumber { get; set; }
        public Level level { get; set; }
        public string name { get; set; }

        public TrudeStorey() { }
        public TrudeStorey(JToken storeyData)
        {
            id = storeyData["id"].ToString();
            levelNumber = (int)storeyData["value"];
            name = storeyData["name"].ToString();
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
            this.level.Name = (string.IsNullOrEmpty(name)? ((levelNumber > 0)? (levelNumber-1).ToString(): levelNumber.ToString()) : name);
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
