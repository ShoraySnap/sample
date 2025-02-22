using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

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
    public class CountDWFData : CountData
    {
        public int missingRFA = 0;
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
        public Dictionary<string, CountDWFData> doors = new Dictionary<string, CountDWFData>();
        public Dictionary<string, CountDWFData> windows = new Dictionary<string, CountDWFData>();
        public Dictionary<string, CountDWFData> furniture = new Dictionary<string, CountDWFData>();
        public Dictionary<string, CountData> roofs = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> genericModels = new Dictionary<string, CountData>();
        public Dictionary<string, CountData> unrecognizedComponents = new Dictionary<string, CountData>();

        public ComponentLogData()
        {
            walls.Add(TrudeExportLoggerHelper.BASIC_WALL_KEY, new CountData());
            floors.Add(TrudeExportLoggerHelper.BASIC_FLOOR_KEY, new CountData());
            ceilings.Add(TrudeExportLoggerHelper.BASIC_CEILING_KEY, new CountData());
            //curtainWalls.Add(TrudeExportLoggerHelper.MULLIONS_KEY, new CountData());
            //curtainWalls.Add(TrudeExportLoggerHelper.PANELS_KEY, new CountData());
            columns.Add(TrudeExportLoggerHelper.BASIC_COLUMN_KEY, new CountData());
            beams.Add(TrudeExportLoggerHelper.BASIC_BEAM_KEY, new CountData());
            doors.Add(TrudeExportLoggerHelper.BASIC_DOOR_KEY, new CountDWFData());
            windows.Add(TrudeExportLoggerHelper.BASIC_WINDOW_KEY, new CountDWFData());
            furniture.Add(TrudeExportLoggerHelper.BASIC_FURNITURE_KEY, new CountDWFData());
            genericModels.Add(TrudeExportLoggerHelper.GENERIC_MODELS_KEY, new CountData());
            unrecognizedComponents.Add(TrudeExportLoggerHelper.MASSES_KEY, new CountData());
            roofs.Add(TrudeExportLoggerHelper.BASIC_ROOF_KEY, new CountData());
        }
    }

    public class ExportStatus 
    {
      public double timeTaken = 0;
      public string status = "";
      public string type = "";
    }

    public class ExportIdentifier
    {
        public string email;
        public string units;
        public string floorkey;
        public Dictionary<string,string> team;
        public string env;
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
        public string errorType;
        public string message;
        public int? objectId;
    }

    public class LogDataContainer
    {
        public SnaptrudeData snaptrude = new SnaptrudeData();
        public RevitData revit = new RevitData();
        public List<LogError> errors = new List<LogError>();
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
        List<LogError> errorData;

        public void Init()
        {
            TrudeExportLogger.Instance = this;
            fullData = new FullLogData();
            snaptrudeData = fullData.data.snaptrude;
            revitData = fullData.data.revit;
            errorData = fullData.data.errors;
        }

        public void Save()
        {
            string serializedLog = JsonConvert.SerializeObject(fullData);
            string snaptrudeManagerPath = "SnaptrudeManager";
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                snaptrudeManagerPath,
                "exportlogger_august.json"
            );

            File.WriteAllText(filePath, serializedLog);
        }

        private void CountComponent(Dictionary<string, CountData> componentDict, string subComponent, bool isParametric, string type)
        {
            CountData countData = componentDict[subComponent];
            if (componentDict.ContainsKey(subComponent))
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
        }

        private void CountComponent(Dictionary<string, CountDWFData> componentDict, string subComponent, bool isParametric, string type)
        {
            CountData countData = componentDict[subComponent];
            if (componentDict.ContainsKey(subComponent))
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
                }
            }
        }

        public void CountOutputElements(string subComponent, bool isParametric, string type)
        {
            if (subComponent == null) return;

            if (subComponent == TrudeExportLoggerHelper.BASIC_WALL_KEY)
            {
                CountComponent(revitData.components.walls, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_FLOOR_KEY)
            {
                CountComponent(revitData.components.floors, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_CEILING_KEY)
            {
                CountComponent(revitData.components.ceilings, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_COLUMN_KEY)
            {
                CountComponent(revitData.components.columns, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_BEAM_KEY)
            {
                CountComponent(revitData.components.beams, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_SLAB_KEY)
            {
                CountComponent(revitData.components.slabs, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_DOOR_KEY)
            {
                CountComponent(revitData.components.doors, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_WINDOW_KEY)
            {
                CountComponent(revitData.components.windows, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.MASSES_KEY)
            {
                CountComponent(revitData.components.unrecognizedComponents, subComponent, isParametric, type);
            }
            else if (subComponent == TrudeExportLoggerHelper.BASIC_FURNITURE_KEY)
            {
                CountComponent(revitData.components.furniture, subComponent, isParametric, type);
            }
        }
        
        public void LogError(string type, string message, int? id)
        {
            LogError errorLog = new LogError();
            errorLog.errorType = type;
            errorLog.message = message;
            errorLog.objectId = id;

            errorData.Add(errorLog);
        }

        public void LogError(LogError errorLog)
        {
            errorData.Add(errorLog);
        }

        public void LogMissingRFA(string type, int count)
        {
            if (type == "door") revitData.components.doors["basic door"].missingRFA = count;
            else if (type == "window") revitData.components.windows["basic window"].missingRFA = count;
            else if (type == "furniture") revitData.components.furniture["basic furniture"].missingRFA = count;
        }
        public void CountInputElements(ComponentLogData logs)
        {
            snaptrudeData.components = logs;
        }

        public void LogExportStatus(double timeTaken, string status, string type, string from)
        {
            if (from == "snaptrude")
            {
                snaptrudeData.trudeGeneration.status = status;
                snaptrudeData.trudeGeneration.timeTaken = timeTaken;
                snaptrudeData.trudeGeneration.type = type;
            }
            else
            {
                revitData.reconcile.status = status;
                revitData.reconcile.timeTaken = timeTaken;
                revitData.reconcile.type = type;
            }
        }

        public void LogExportStatus(ExportStatus data, string from)
        {
            if (from == "snaptrude")
            {
                snaptrudeData.trudeGeneration = data;
            }
            else
            {
                revitData.reconcile = data;
            }
        }

        public string GetSerializedObject()
        {
            var serializedLog = JsonConvert.SerializeObject(fullData.data, Formatting.Indented);
            return serializedLog;
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