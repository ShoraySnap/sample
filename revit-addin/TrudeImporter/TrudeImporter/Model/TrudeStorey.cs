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
        public string RevitName {  get; set; }

        public TrudeStorey() { }
        public TrudeStorey(StoreyProperties storeyProps)
        {
            LevelNumber = storeyProps.LevelNumber;
            Name = storeyProps.Name;
            Elevation = storeyProps.Elevation;
            RevitName = string.IsNullOrEmpty(Name) ? ((LevelNumber > 0) ? (LevelNumber - 1).ToString() : LevelNumber.ToString()) : Name;
        }
        public TrudeStorey(int levelNumber, double elevation, string name)
        {
            LevelNumber = levelNumber;
            Elevation = elevation;
            Name = name;
            RevitName = string.IsNullOrEmpty(Name) ? ((LevelNumber > 0) ? (LevelNumber - 1).ToString() : LevelNumber.ToString()) : Name;
        }

        public Level CreateLevel(Document newDoc)
        {
            Level = Level.Create(newDoc, Elevation);
            Level.Name = RevitName;
            CreateFloorPlan(newDoc);

            return Level;
        }

        public static string getLevelName(int storey)
        {
            // How to handle case when two storey have same name (since levels are using that name).
            return ((storey > 0) ? (storey - 1).ToString() : storey.ToString());
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
