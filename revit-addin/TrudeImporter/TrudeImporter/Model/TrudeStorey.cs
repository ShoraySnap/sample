using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeStorey
    {
        public double Elevation { get; set; }
        public int levelNumber { get; set; }
        public string name { get; set; }

        public Level level { get; set; }

        public TrudeStorey() { }
        public TrudeStorey(StoreyProperties props)
        {
            levelNumber = props.LevelNumber;
            name = props.Name;
            Elevation = props.Elevation;
        }
        public TrudeStorey(int levelNumber, double elevation, string name)
        {
            this.levelNumber = levelNumber;
            this.Elevation = elevation;
            this.name = name;
        }

        public Level CreateLevel(Document newDoc)
        {
            this.level = Level.Create(newDoc, this.Elevation);
            this.level.Name = (string.IsNullOrEmpty(name)? ((levelNumber > 0)? (levelNumber-1).ToString(): levelNumber.ToString()) : name);

            this.CreateFloorPlan(newDoc);

            return level;
        }

        private ViewPlan CreateFloorPlan(Document newDoc)
        {
            ViewFamilyType floorPlanType = new FilteredElementCollector(newDoc)
                                .OfClass(typeof(ViewFamilyType))
                                .Cast<ViewFamilyType>()
                                .FirstOrDefault(x => ViewFamily.FloorPlan == x.ViewFamily);

            ViewPlan floorPlan = ViewPlan.Create(newDoc, floorPlanType.Id, level.Id);
            if (floorPlan.CanModifyViewDiscipline()) floorPlan.Discipline = ViewDiscipline.Architectural;

            return floorPlan;
        }
    }
}
