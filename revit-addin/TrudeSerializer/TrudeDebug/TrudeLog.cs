using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using TrudeSerializer.Components;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Debug
{
    internal class TrudeLog
    {
        public long Walls { get; set; }
        public long Furniture { get; set; }
        public long Doors { get; set; }
        public long Windows { get; set; }
        public long Floors { get; set; }
        public long Ceilings { get; set; }
        public Dictionary<String, long> Masses { get; set; }
        public long RevitLinks { get; set; }

        private void HandleMasses(Dictionary<string, TrudeMass> trudeMasses)
        {
            foreach (var trudeMass in trudeMasses)
            {
                if (Masses.ContainsKey(trudeMass.Value.subCategory))
                {
                    Masses[trudeMass.Value.subCategory] += 1;
                }
                else
                {
                    Masses.Add(trudeMass.Value.subCategory, 1);
                }
            }
        }

        private void HandleObjects<T>(Dictionary<string, TrudeObject<T>> trudeComponents)
        {
            if (typeof(T) == typeof(TrudeWall))
            {
                Walls = trudeComponents.Count;
            }
            else if (typeof(T) == typeof(TrudeFloor))
            {
                Floors = trudeComponents.Count;
            }
            else if (typeof(T) == typeof(TrudeCeiling))
            {
                Ceilings = trudeComponents.Count;
            }
        }

        private void HandleFamilyObject<T>(TrudeObject<T> trudeObject)
        {
            Dictionary<string, T> instances = trudeObject.Instances;

            foreach (var instance in instances)
            {
                if (typeof(T) == typeof(TrudeFurniture))
                {
                    Furniture += !(instance.Value as TrudeFurniture).hasParentElement ? 1 : 0;
                }
                else if (typeof(T) == typeof(TrudeDoor))
                {
                    Doors += (instance.Value as TrudeDoor).hasParentElement ? 1 : 0;
                }
                else if (typeof(T) == typeof(TrudeWindow))
                {
                    Windows += (instance.Value as TrudeWindow).hasParentElement ? 1 : 0;
                }
            }
        }

        private void HandleRevitLinks(Dictionary<string, Dictionary<string, TrudeMass>> revitLinks)
        {
            RevitLinks = revitLinks.Count;
        }

        public static void LogElementCount(SerializedTrudeData serializedTrudeData)
        {
            PropertyInfo[] properties = serializedTrudeData.GetType().GetProperties();

            TrudeLog trudeLog = new TrudeLog();

            Dictionary<string, Action<object>> propertyHandlers = new Dictionary<string, Action<object>>
            {
              { "Masses", pv => trudeLog.HandleMasses(pv as Dictionary<string, TrudeMass>) },
              { "Walls", pv => trudeLog.HandleObjects(pv as Dictionary<string, TrudeObject<TrudeWall>>) },
              { "Furniture", pv => trudeLog.HandleFamilyObject(pv as TrudeObject<TrudeFurniture>) },
              { "Floors", pv => trudeLog.HandleObjects(pv as Dictionary<string, TrudeObject<TrudeFloor>>) },
              { "Ceilings", pv => trudeLog.HandleObjects(pv as Dictionary<string, TrudeObject<TrudeCeiling>>) },
              { "Doors", pv => trudeLog.HandleFamilyObject(pv as TrudeObject<TrudeDoor>) },
              { "Windows", pv => trudeLog.HandleFamilyObject(pv as TrudeObject<TrudeWindow>) },
              { "RevitLinks", pv => trudeLog.HandleRevitLinks(pv as Dictionary<string, Dictionary<string, TrudeMass>>)}
            };

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                object propertyValue = property.GetValue(serializedTrudeData);

                if (propertyHandlers.ContainsKey(propertyName))
                {
                    propertyHandlers[propertyName].Invoke(propertyValue);
                }
            }
            //foreach (PropertyInfo property in properties)
            //{
            //    string propertyName = property.Name;
            //    object propertyValue = property.GetValue(serializedTrudeData);

            //    if (propertyName == "Masses")
            //    {
            //        trudeLog.HandleMasses(propertyValue as Dictionary<string, TrudeMass>);
            //    }
            //    else if (propertyName == "Walls")
            //    {
            //        trudeLog.HandleObjects(propertyValue as Dictionary<string, TrudeObject<TrudeWall>>);
            //    }
            //    else if (propertyName == "Furniture")
            //    {
            //        trudeLog.HandleFamilyObject(propertyValue as TrudeObject<TrudeFurniture>);
            //    }
            //    else if (propertyName == "Floors")
            //    {
            //        trudeLog.HandleObjects(propertyValue as Dictionary<string, TrudeObject<TrudeFloor>>);
            //    }
            //    else if (propertyName == "Ceilings")
            //    {
            //        trudeLog.HandleObjects(propertyValue as Dictionary<string, TrudeObject<TrudeCeiling>>);
            //    }
            //    else if (propertyName == "Doors")
            //    {
            //        trudeLog.HandleFamilyObject(propertyValue as TrudeObject<TrudeDoor>);
            //    }
            //    else if (propertyName == "Windows")
            //    {
            //        trudeLog.HandleFamilyObject(propertyValue as TrudeObject<TrudeWindow>);
            //    }
            //    else if (propertyName == "RevitLinks")
            //    {
            //        trudeLog.HandleRevitLinks(propertyValue as Dictionary<string, Dictionary<string, TrudeMass>>);
            //    }
            //}

            string serializedObject = JsonConvert.SerializeObject(trudeLog);
            JObject trudeLogJsonObject = JObject.Parse(serializedObject);
            string formattedJson = trudeLogJsonObject.ToString(Formatting.Indented);
            string filePath = "trudeLog.txt";

            TrudeDebug.StoreData(formattedJson, filePath);
        }
    }
}