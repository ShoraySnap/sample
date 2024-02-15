using System;
using System.Collections.Generic;
using TrudeSerializer.Components;

namespace TrudeSerializer.Importer
{
    internal class CurrentElement
    {
        public String name;
        public String category;
        public List<String> materials;
        public TrudeComponent component;

        public CurrentElement()
        {
            this.materials = new List<String>();
        }
        public CurrentElement(String name, String category)
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

        public void SetComponent(TrudeComponent component)
        {
            this.component = component;
        }
        public void AddMaterial(String subMaterialId)
        {
            this.materials.Add(subMaterialId);
        }

        public bool HasMaterial(String subMaterialId)
        {
            return this.materials.Contains(subMaterialId);
        }

        public static CurrentElement SetCurrentElement(TrudeComponent component)
        {
            CurrentElement currentElement = new CurrentElement();
            currentElement.SetComponent(component);
            currentElement.AddName(component.elementId);
            currentElement.AddCategory(component.category);
            return currentElement;
        }
    }
}