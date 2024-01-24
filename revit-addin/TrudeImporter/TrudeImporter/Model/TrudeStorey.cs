using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeStorey
    {
        public double Elevation { get; set; }
        public int LevelNumber { get; set; }
        public string Name { get; set; }
        public Level Level { get; set; }

        public TrudeStorey() { }
        public TrudeStorey(StoreyProperties storeyProps)
        {
            LevelNumber = storeyProps.LevelNumber;
            Name = storeyProps.Name;
            Elevation = storeyProps.Elevation;
        }
        public TrudeStorey(int levelNumber, double elevation, string name)
        {
            this.LevelNumber = levelNumber;
            this.Elevation = elevation;
            this.Name = name;
        }

        public Level CreateLevel(Document newDoc)
        {
            this.Level = Level.Create(newDoc, this.Elevation);
            this.Level.Name = (string.IsNullOrEmpty(Name)? ((LevelNumber > 0)? (LevelNumber-1).ToString(): LevelNumber.ToString()) : Name);

            this.CreateFloorPlan(newDoc);

            return Level;
        }

        private ViewPlan CreateFloorPlan(Document newDoc)
        {
            ViewFamilyType floorPlanType = new FilteredElementCollector(newDoc)
                                .OfClass(typeof(ViewFamilyType))
                                .Cast<ViewFamilyType>()
                                .FirstOrDefault(x => ViewFamily.FloorPlan == x.ViewFamily);

            ViewPlan floorPlan = ViewPlan.Create(newDoc, floorPlanType.Id, Level.Id);
            if (floorPlan.CanModifyViewDiscipline()) floorPlan.Discipline = ViewDiscipline.Architectural;

            return floorPlan;
        }
    }
}
