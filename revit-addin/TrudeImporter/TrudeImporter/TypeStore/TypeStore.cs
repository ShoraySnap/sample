﻿using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public abstract class TypeStore<T_RawKey, T_Type> where T_Type : ElementType
    {
        public IDictionary<string, T_Type> Types = new Dictionary<string, T_Type>();
        public T_Type GetType(T_RawKey rawKey, Document doc, T_Type defaultType = null)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(T_Type));

            if (defaultType is null) defaultType = collector.First() as T_Type;

            return GetType(rawKey, defaultType);
        }
        public T_Type GetType(T_RawKey rawKey, T_Type defaultType)
        {
            string key = KeyAdapter(rawKey, defaultType);
            key = CleanKey(key);

            if (!Types.ContainsKey(key))
            {
                try
                {
                    T_Type newType = defaultType.Duplicate(key) as T_Type;
                    TypeModifier(rawKey, newType);
                    Types.Add(key, newType);
                }
                catch
                {
                    return defaultType;
                }
            }
            else if (!Types[key].IsValidObject)
            {
                Types.Remove(key);
                try
                {
                    T_Type newType = defaultType.Duplicate(key) as T_Type;
                    TypeModifier(rawKey, newType);
                    Types.Add(key, newType);
                }
                catch
                {
                    return defaultType;
                }
            }

            return Types[key];
        }

        public void Clear()
        {
            Types.Clear();
        }

        private string CleanKey(string key)
        {
            return key
                .Replace("{", "_")
                .Replace("}", "_")
                .Replace("[", "_")
                .Replace("]", "_")
                .Replace(";", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("?", "_")
                .Replace("`", "_")
                .Replace("~", "_")
                .Replace("\n", "_")
                .Replace("\\n", "_");
        }

        // Implement method to get key postfix
        abstract public string KeyAdapter(T_RawKey rawKey, T_Type defaultType);

        // Implement changes to be made to the type
        abstract public void TypeModifier(T_RawKey rawKey, T_Type type);
    }
}
