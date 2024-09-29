using System;
using System.Collections.Generic;

namespace TrudeSerializer.Utils
{
    internal class TrudeCategoryUtils
    {
        private static readonly Dictionary<string, TrudeCategory> StringToCategoryMap = new Dictionary<string, TrudeCategory>(StringComparer.OrdinalIgnoreCase)
    {
        { "Wall", TrudeCategory.Wall },
        { "Floor", TrudeCategory.Floor },
        { "Roof", TrudeCategory.Roof },
        { "Furniture", TrudeCategory.Furniture },
        { "Column", TrudeCategory.Column },
        { "Ceiling", TrudeCategory.Ceiling },
        { "Mass", TrudeCategory.Mass },
        { "GenericModel", TrudeCategory.GenericModel },
        { "Window", TrudeCategory.Window },
        { "Door", TrudeCategory.Door },
        { "CurtainWall", TrudeCategory.CurtainWall }
    };

        private static readonly Dictionary<TrudeCategory, string> CategoryToStringMap = new Dictionary<TrudeCategory, string>
    {
        { TrudeCategory.Wall, "Wall" },
        { TrudeCategory.Floor, "Floor" },
        { TrudeCategory.Roof, "Roof" },
        { TrudeCategory.Furniture, "Furniture" },
        { TrudeCategory.Column, "Column" },
        { TrudeCategory.Ceiling, "Ceiling" },
        { TrudeCategory.Mass, "Mass" },
        { TrudeCategory.GenericModel, "GenericModel" },
        { TrudeCategory.Window, "Window" },
        { TrudeCategory.Door, "Door" },
        { TrudeCategory.CurtainWall, "CurtainWall" }
    };

        static public TrudeCategory MapStringToSubCategory(string subCategory)
        {
            return StringToCategoryMap.TryGetValue(subCategory, out var category) ? category : TrudeCategory.Mass;
        }

        static public string MapSubCategoryToString(TrudeCategory category)
        {
            return CategoryToStringMap.TryGetValue(category, out var subCategory) ? subCategory : "Mass";
        }
    }

    public enum TrudeCategory
    {
        Wall,
        Floor,
        Roof,
        Furniture,
        Column,
        Ceiling,
        Mass,
        GenericModel,
        Window,
        Door,
        CurtainWall,
        Default
    }
}