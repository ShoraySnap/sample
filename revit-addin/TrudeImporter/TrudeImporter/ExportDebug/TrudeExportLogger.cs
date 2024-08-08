using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using TrudeCommon.Utils;

namespace TrudeImporter
{
    public class ErrorData
    {
        public string callStack = "";
        public string revitId = "";
        public string message = "";
    }

    public class ProcessStageLogData
    {
        public const string STATUS_DONE = "DONE";
        public const string STATUS_FAILED = "FAILED";
        public string status = "NONE";
    }
    public class ModifiedCountType
    {
        public int parametric = 0;
        public int nonParametric = 0;
    }
    public class DeletedCountType
    {
        public int total = 0;
    }
    public class CountData
    {
        public ModifiedCountType added = new ModifiedCountType();
        public ModifiedCountType updated = new ModifiedCountType();
        public DeletedCountType deleted = new DeletedCountType();
    }
    public class ComponentLogData
    {
        public Dictionary<string, CountData> walls = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> floors = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> ceilings = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> curtainWalls = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> columns = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> beams = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> slabs = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> doors = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> windows = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> furniture = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> roofs = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> genericModels = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> unrecognizedComponents = new Dictionary<string, CountData>();

        public ComponentLogData()
        {
            walls.Add(TrudeExportLoggerHelper.BASIC_WALL_KEY, new CountData());
            floors.Add(TrudeExportLoggerHelper.BASIC_FLOOR_KEY, new CountData());
            ceilings.Add(TrudeExportLoggerHelper.BASIC_CEILING_KEY, new CountData());
            curtainWalls.Add(TrudeExportLoggerHelper.MULLIONS_KEY, new CountData());
            curtainWalls.Add(TrudeExportLoggerHelper.PANELS_KEY, new CountData());
            columns.Add(TrudeExportLoggerHelper.BASIC_COLUMN_KEY, new CountData());
            beams.Add(TrudeExportLoggerHelper.BASIC_BEAM_KEY, new CountData());
            doors.Add(TrudeExportLoggerHelper.BASIC_DOOR_KEY, new CountData());
            windows.Add(TrudeExportLoggerHelper.BASIC_WINDOW_KEY, new CountData());
            furniture.Add(TrudeExportLoggerHelper.BASIC_FURNITURE_KEY, new CountData());
            genericModels.Add(TrudeExportLoggerHelper.GENERIC_MODELS_KEY, new CountData());
            unrecognizedComponents.Add(TrudeExportLoggerHelper.MASSES_KEY, new CountData());
            roofs.Add(TrudeExportLoggerHelper.BASIC_ROOF_KEY, new CountData());
        }
    }

    public class ExportStatus 
    {
      public int timeTaken = 0;
      public string status = "";
      public string type = "";
    }

    public class ExportIdentifier
    {
      // TODO: taufiq will add these meta data 
    }

    public class SnaptrudeData
    {
        public ExportStatus trudeGeneration = new ExportStatus();
        public ComponentLogData components = new ComponentLogData();
    }

    public class RevitData
    {
        public ExportStatus reconcile = new ExportStatus();
        public ComponentLogData components = new ComponentLogData();
    }

    public class LogError
    {
      // TODO: to add later - all catch blocks will add to this
    }

    public class LogDataContainer
    {
        public SnaptrudeData snaptrude = new SnaptrudeData();
        public RevitData revit = new RevitData();
        public LogError errors = new LogError();
    }

    public class FullLogData
    {
        public ExportIdentifier identifier = new ExportIdentifier();
        public LogDataContainer data = new LogDataContainer();
    }

    public class TrudeExportLogger
    {
        public static TrudeExportLogger Instance = new TrudeExportLogger();
        FullLogData fullData;
        SnaptrudeData snaptrudeData;
        RevitData revitData;

        private static string currentKey = "";
        public void Init()
        {
            TrudeExportLogger.Instance = this;
            fullData = new FullLogData();
            snaptrudeData = fullData.data.snaptrude;
            revitData = fullData.data.revit;
        }

        public void Save()
        {
            string serializedLog = JsonConvert.SerializeObject(fullData);
            string snaptrudeManagerPath = "snaptrude-manager";
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                snaptrudeManagerPath,
                "exportlogger_august.json"
            );

            File.WriteAllText(filePath, serializedLog);
        }

        private void CountComponent(Dictionary<string, CountData> componentDict, bool isParametric, string type)
        {
            CountData countData = componentDict[currentKey];
            if (componentDict.ContainsKey(currentKey))
            {
                if (type == "added")
                {
                    if (isParametric) countData.added.parametric += 1;
                    else countData.added.nonParametric += 1;
                }
                else if (type == "updated")
                {
                    if (isParametric) countData.updated.parametric += 1;
                    else countData.updated.nonParametric += 1;
                }
                else if (type == "deleted")
                {
                    countData.deleted.total += 1;
                    // if (isParametric) countData.deleted.parametric += 1;
                    // else countData.deleted.nonParametric += 1;
                }
            }
            currentKey = "";
        }

        public void CountOutputElements(string subComponent, bool isParametric, string type)
        {
            if (subComponent == null) return;

            if (subComponent == TrudeExportLoggerHelper.BASIC_WALL_KEY)
            {
                CountComponent(revitData.components.walls, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_FLOOR_KEY)
            {
                CountComponent(revitData.components.floors, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_CEILING_KEY)
            {
                CountComponent(revitData.components.ceilings, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_COLUMN_KEY)
            {
                CountComponent(revitData.components.columns, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_BEAM_KEY)
            {
                CountComponent(revitData.components.beams, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_SLAB_KEY)
            {
                CountComponent(revitData.components.slabs, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_DOOR_KEY)
            {
                CountComponent(revitData.components.doors, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_WINDOW_KEY)
            {
                CountComponent(revitData.components.windows, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.MASSES_KEY)
            {
                CountComponent(revitData.components.unrecognizedComponents, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_FURNITURE_KEY)
            {
                CountComponent(revitData.components.furniture, isParametric, type);
            }
        }
        
        public void CountInputElements(ComponentLogData logs)
        {
            snaptrudeData.components = logs;
        }

        public string GetSerializedObject()
        {
            return "";
            //var serializedLog = JsonConvert.SerializeObject(fullData);
            //return serializedLog;
        }

        public static bool ToIgnoreUnrecognizedCategories(Element element)
        {
          return false;
          string elementCategory = element?.Category?.Name;
          if (elementCategory == null) return true;
          string[] ignoreCategories = new string[] { "Cameras", "Levels" };
          return Array.Exists(ignoreCategories, elementCategory.Contains);
        }
    }
}