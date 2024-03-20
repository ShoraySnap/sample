using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Markup;
using TrudeSerializer.Components;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;
using TrudeSerializer.Debug;

namespace TrudeSerializer.Debug
{
	internal class ErrorData
	{
		public string callStack = "";
		public string revitId = "";
		public string message = "";
	}

	internal class ProcessStageLogData
	{
		public const string STATUS_DONE = "DONE";
		public const string STATUS_FAILED = "FAILED";
		public string status = "NONE";
	}
	internal class CountData
	{
		public int parametric = 0;
		public int nonParametric = 0;
		public int total = 0;
	}
	internal class ComponentLogData
	{
		public const string BASIC_WALL_KEY = "BasicWall";
		public const string BASIC_FLOOR_KEY = "BasicFloor";
		public const string BASIC_CEILING_KEY = "BasicCeiling";
		public const string BASIC_COLUMN_KEY = "BasicColumn";
		public const string BASIC_DOOR_KEY = "BasicDoor";
		public const string BASIC_WINDOW_KEY = "BasicWindow";
		public const string BASIC_FURNITURE_KEY = "BasicFurniture";
		public const string MULLIONS_KEY = "mullions";
		public const string PANELS_KEY = "panels";
		public const string MASSES_KEY = "masses";
		public const string GENERIC_MODELS_KEY = "generic";
		public const string BASIC_ROOF_KEY = "BasicRoof";
		public Dictionary<string, CountData> walls = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> floors = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> ceilings = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> curtainWalls = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> columns = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> doors = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> windows = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> furniture = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> revitLinks = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> roofs = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> genericModels = new Dictionary<string, CountData>();
		public Dictionary<string, CountData> unrecognizedComponents = new Dictionary<string, CountData>();

		public ComponentLogData() {
			walls.Add(BASIC_WALL_KEY, new CountData());
			floors.Add(BASIC_FLOOR_KEY, new CountData());
			ceilings.Add(BASIC_CEILING_KEY, new CountData());
			curtainWalls.Add(MULLIONS_KEY, new CountData());
			curtainWalls.Add(PANELS_KEY, new CountData());
			columns.Add(BASIC_COLUMN_KEY, new CountData());
			doors.Add(BASIC_DOOR_KEY, new CountData());
			windows.Add(BASIC_WINDOW_KEY, new CountData());
			furniture.Add(BASIC_FURNITURE_KEY, new CountData());
			genericModels.Add(GENERIC_MODELS_KEY, new CountData());
			unrecognizedComponents.Add(MASSES_KEY, new CountData());
			roofs.Add(BASIC_ROOF_KEY, new CountData());
		}
	}

	internal class TrudeLogData
	{
		public ProcessStageLogData serialize = new ProcessStageLogData();
		public ProcessStageLogData upload = new ProcessStageLogData();
		public ComponentLogData components = new ComponentLogData();
	}

	internal class FullLogData {
		public TrudeLogData revit = new TrudeLogData();
		public TrudeLogData snaptrude = new TrudeLogData();
	}

    internal class TrudeLogger
    {
		public static TrudeLogger Instance = new TrudeLogger();		
		FullLogData fullData;
        TrudeLogData data;

		private static string currentKey = "";
		public void Init()
		{
			TrudeLogger.Instance = this;
			fullData = new FullLogData();
			data = fullData.revit;
		}

		public void UploadDone(bool ok)
		{
			if (ok)
				data.upload.status = ProcessStageLogData.STATUS_DONE;
			else
				data.upload.status = ProcessStageLogData.STATUS_FAILED;
		}
		public void SerializeDone(bool ok)
		{
			if (ok)
				data.serialize.status = ProcessStageLogData.STATUS_DONE;
			else
				data.serialize.status = ProcessStageLogData.STATUS_FAILED;
		}
		public void Save()
		{
			var serializedLog = JsonConvert.SerializeObject(fullData);
			TrudeDebug.StoreData(serializedLog, "log.json");
		}

		private void CountInputComponent(Dictionary<string, CountData> componentDict, string key)
		{
			if(componentDict.ContainsKey(key))
			{
				componentDict[key].total += 1;
			}
			else
			{
				componentDict[key] = new CountData();
				componentDict[key].total += 1;
			}

			currentKey = key;
		}

		private void CountOutputComponent(Dictionary<string, CountData> componentDict, bool isParametric)
		{
			if (componentDict.ContainsKey(currentKey)) {
				if(isParametric)
				{
                    componentDict[currentKey].parametric += 1;
				}
				else
				{
					componentDict[currentKey].nonParametric += 1;
				}
			}

			currentKey = "";
		}


		public void CountInput(Element element)
		{
			if (element == null) return;

			if(TrudeWall.IsValidWall(element))
			{
				CountInputComponent(data.components.walls, ComponentLogData.BASIC_WALL_KEY);
			}
			else if(element is Floor floor)
			{
				CountInputComponent(data.components.floors, ComponentLogData.BASIC_FLOOR_KEY);
			}
			else if(element is Ceiling ceiling)
			{
				CountInputComponent(data.components.ceilings, ComponentLogData.BASIC_CEILING_KEY);
			}
			else if(TrudeCurtainWall.IsCurtainWallComponent(element))
			{
				if(TrudeCurtainWall.IsCurtainWallMullion(element))
				{
					CountInputComponent(data.components.curtainWalls, ComponentLogData.MULLIONS_KEY);
				}
				else if(TrudeCurtainWall.IsCurtainWallPanel(element))
				{
					CountInputComponent(data.components.curtainWalls, ComponentLogData.PANELS_KEY);
				}
			}
			else if(TrudeColumn.IsColumnCategory(element))
			{
				CountInputComponent(data.components.columns, ComponentLogData.BASIC_COLUMN_KEY);
			}
			else if(TrudeDoor.IsDoor(element))
			{
				CountInputComponent(data.components.doors, ComponentLogData.BASIC_DOOR_KEY);
			}
			else if(TrudeWindow.IsWindow(element))
			{
				CountInputComponent(data.components.windows, ComponentLogData.BASIC_WINDOW_KEY);
			}
			else if(TrudeFurniture.IsFurnitureCategory(element))
			{
				CountInputComponent(data.components.furniture, ComponentLogData.BASIC_FURNITURE_KEY);
			}
			else if(TrudeGenericModel.IsGenericModel(element))
			{
				CountInputComponent(data.components.genericModels, ComponentLogData.GENERIC_MODELS_KEY);
			}
			else if(element is RoofBase)
			{
				CountInputComponent(data.components.roofs, ComponentLogData.BASIC_ROOF_KEY);
			}
			else
			{
				CountInputComponent(data.components.unrecognizedComponents, ComponentLogData.MASSES_KEY);
			}
		}

		public void CountInputRevitLink(string key)
		{
			CountInputComponent(data.components.revitLinks, key); // Count revit links
		}
		public void CountOutputRevitLink()
		{
			CountOutputComponent(data.components.revitLinks, false); // Always non-parametric for now	
		}
		public void CountOutput(TrudeComponent component)
		{
			if(component == null) return;

			if(component is TrudeWall wall)
			{
				CountOutputComponent(data.components.walls, component.isParametric);
			}
			else if(component is TrudeFloor floor)
			{
				CountOutputComponent(data.components.floors, component.isParametric);
			}
			else if(component is TrudeCeiling ceiling)
			{
				CountOutputComponent(data.components.ceilings, component.isParametric);
			}
			else if(component is TrudeCurtainWall)
			{
				CountOutputComponent(data.components.curtainWalls, component.isParametric);
			}
			else if (component is TrudeColumn column)
			{
				CountOutputComponent(data.components.columns, component.isParametric);
			}
			else if(component is TrudeDoor)
			{
				CountOutputComponent(data.components.doors, component.isParametric);
			}
			else if(component is TrudeWindow)
			{
				CountOutputComponent(data.components.windows, component.isParametric);
			}
			else if(component is TrudeGenericModel)
			{
				CountOutputComponent(data.components.genericModels, component.isParametric);
			}
			else if(component is TrudeRoof)
			{
				CountOutputComponent(data.components.roofs, component.isParametric);
			}
			else if(component is TrudeMass)
			{
				CountOutputComponent(data.components.unrecognizedComponents, component.isParametric);
			}
			else if(component is TrudeFurniture)
			{
				CountOutputComponent(data.components.furniture, component.isParametric);
			}
		}

        public string GetSerializedObject()
        {
			var serializedLog = JsonConvert.SerializeObject(fullData);

            return serializedLog;
        }

		public void CleanupUpdateMasses(CountData newMassCount)
		{
			data.components.unrecognizedComponents["masses"] = newMassCount;
		}
    }
}