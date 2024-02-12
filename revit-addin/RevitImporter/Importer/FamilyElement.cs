using System;
using System.Collections.Generic;
using TrudeSerializer.Components;

namespace TrudeSerializer.Importer
{
    internal class FamilyElement
    {
        public String name;
        public String category;
        public List<String> materials;

        public FamilyElement()
        {
            this.materials = new List<String>();
        }
        public FamilyElement(String name, String category)
        {
            this.name = name;
            this.category = category;
            this.materials = new List<String>();
        }

        public void AddName(String name)
        {
            this.name = name;
        }
        public void AddCategory(String category) { this.category = category; }
        public void AddMaterial(String subMaterialId)
        {
            this.materials.Add(subMaterialId);
        }

        public bool HasMaterial(String subMaterialId)
        {
            return this.materials.Contains(subMaterialId);
        }

        public static FamilyElement SetCurrentFamilyElement(TrudeComponent component)
        {
            FamilyElement familyElement = new FamilyElement();
            familyElement.AddName(component.elementId);
            familyElement.AddCategory(component.category);
            return familyElement;
        }
    }
}