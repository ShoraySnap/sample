using System;
using System.Collections.Generic;

namespace TrudeSerializer.Importer
{
    internal class FamilyElement
    {
        public String name;
        public String category;
        public List<String> materials;

        public FamilyElement()
        {
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
    }
}