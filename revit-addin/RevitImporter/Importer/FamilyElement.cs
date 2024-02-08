using System;
using System.Collections.Generic;
using System.Text;

namespace RevitImporter.Importer
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

        public void addName(String name)
        {
            this.name = name;
        }
        public void addCategory(String category) { this.category = category; }
        public void addMaterial(String subMaterialId)
        {
            this.materials.Add(subMaterialId);
        }

        public bool hasMaterial(String subMaterialId)
        {
            return this.materials.Contains(subMaterialId);
        }
    }
}
