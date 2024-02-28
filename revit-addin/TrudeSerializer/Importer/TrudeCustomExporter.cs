using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Importer;
using TrudeSerializer.Components;
using TrudeSerializer.Types;

namespace TrudeSerializer
{
    internal class TrudeCustomExporter : IExportContext
    {
        private bool isRevitLink = false;
        private CurrentLink currentLink;
        private List<string> revitLinks = new List<string>();

        private Document doc;
        private Stack<Transform> transforms = new Stack<Transform>();
  
        private String currentMaterialId;
        private CurrentElement currentElement;
        public SerializedTrudeData serializedSnaptrudeData;
        

        public SerializedTrudeData GetExportData()
        {
            return serializedSnaptrudeData;
        }

        private Transform CurrentTransform
        {
            get
            {
                if (transforms.Count == 0)
                    return Transform.Identity;
                else
                    return transforms.Peek();
            }
        }

        private void ChangeCurrentDocument(Document doc)
        {
            this.doc = doc;
            GlobalVariables.CurrentDocument = doc;
        }

        

        public TrudeCustomExporter(Document doc)
        {
            this.doc = doc;
            transforms.Push(CurrentTransform);
            this.serializedSnaptrudeData = new SerializedTrudeData();
            GlobalVariables.CurrentDocument = doc;
        }

        bool IExportContext.Start()
        {
            Units units = doc.GetUnits();

            return true;
        }

        void IExportContext.Finish()
        {
            return;
        }

        bool IExportContext.IsCanceled()
        {
            return false;
        }

        RenderNodeAction IExportContext.OnViewBegin(ViewNode node)
        {
            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnViewEnd(ElementId elementId)
        {
            return;
        }

        RenderNodeAction IExportContext.OnLinkBegin(LinkNode node)
        {
            // implement link part
            Document doc = node.GetDocument();
            ChangeCurrentDocument(doc);
            isRevitLink = true;
            string name = doc.Title.Replace(".", "");
            string category = "RVT Links";
            currentLink = new CurrentLink(name, category);
            //currentLink.category = category;
            transforms.Push(CurrentTransform.Multiply(node.GetTransform()));

            // check for circular dependency
            if (revitLinks.Find(revitLink => revitLink == name) == null)
            {
                revitLinks.Add(name);
                serializedSnaptrudeData.RevitLinks.Add(name, new Dictionary<string, TrudeMass>());
            }
            else
            {
                isRevitLink = false;
                return RenderNodeAction.Skip;
            }

            if (isRevitLink)
            {
                // initialize serializedSnaptrudeData.revitLinks dictionary if needed
            }

            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnLinkEnd(LinkNode node)
        {
            ChangeCurrentDocument(GlobalVariables.Document);
            currentLink.name = "";
            currentLink.category = "";
            isRevitLink = false;
            transforms.Pop();
        }

        RenderNodeAction IExportContext.OnElementBegin(ElementId elementId)
        {
            Element element = doc.GetElement(elementId);
            TrudeComponent component = null;
            if (isRevitLink)
            {
                TrudeMass mass = TrudeMass.GetSerializedComponent(serializedSnaptrudeData, element);
                if (mass.elementId == "-1")
                {
                    return RenderNodeAction.Skip;
                }
                serializedSnaptrudeData.RevitLinks[currentLink.name].Add(elementId.ToString(), mass);
                component = mass;
            }
            else
            {
                try
                {
                    component = ComponentHandler.Instance.GetComponent(serializedSnaptrudeData, element);
                }catch(Exception e)
                {
                    return RenderNodeAction.Skip;
                }
                
                if (component?.elementId == "-1")
                {
                    return RenderNodeAction.Skip;
                }
                AddComponentToSerializedData(component);
            }

            if (component.IsParametric())
            {
                return RenderNodeAction.Skip;
            }

            this.currentElement = CurrentElement.SetCurrentElement(component);

            if (!component.isInstance) return RenderNodeAction.Proceed;

            TrudeComponent familyComponent = TrudeComponent.CurrentFamily;
            if (familyComponent == null)
            {
                return RenderNodeAction.Skip;
            }

            this.currentElement.component = familyComponent;

            return RenderNodeAction.Proceed;
        }

        private void AddComponentToSerializedData(TrudeComponent component)
        {
            if (component is TrudeWall)
            {
                serializedSnaptrudeData.AddWall(component as TrudeWall);
            }
            else if (component is TrudeLevel)
            {
                serializedSnaptrudeData.AddLevel(component as TrudeLevel);
            }
            else if (component is TrudeFloor)
            {
                serializedSnaptrudeData.AddFloor(component as TrudeFloor);
            }
            else if (component is TrudeMass)
            {
                serializedSnaptrudeData.AddMass(component as TrudeMass);
            }
            else if (component is TrudeCeiling)
            {
                serializedSnaptrudeData.AddCeiling(component as TrudeCeiling);
            }
            else if (component is TrudeFurniture)
            {
                serializedSnaptrudeData.AddFurnitureInstance(component.elementId, component as TrudeFurniture);
            }
            else if (component is TrudeDoor)
            {
                serializedSnaptrudeData.AddDoorInstance(component.elementId, component as TrudeDoor);
            }

            else if(component is TrudeRoof)
            {
                serializedSnaptrudeData.AddRoof(component as TrudeRoof);
            }
        }

        void IExportContext.OnElementEnd(ElementId elementId)
        {
            return;
        }

        RenderNodeAction IExportContext.OnInstanceBegin(InstanceNode node)
        {
            transforms.Push(CurrentTransform.Multiply(node.GetTransform()));
            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnInstanceEnd(InstanceNode node)
        {
            transforms.Pop();
        }

        RenderNodeAction IExportContext.OnFaceBegin(FaceNode node)
        {
            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnFaceEnd(FaceNode node)
        {
            return;
        }

        void IExportContext.OnLight(LightNode node)
        {
            return;
        }

        void IExportContext.OnRPC(RPCNode node)
        {
            return;
        }

        void IExportContext.OnMaterial(MaterialNode node)
        {
            String materialId = node.MaterialId.ToString();
            this.currentMaterialId = materialId;

            if (this.currentElement.HasMaterial(materialId)) return;

            Element material = doc.GetElement(node.MaterialId);

            TrudeMaterial trudeMaterial = TrudeMaterial.GetMaterial(material as Material);
            this.currentElement.AddMaterial(materialId);
            this.currentElement.component.SetMaterial(currentMaterialId, trudeMaterial);

            return;
        }

        void IExportContext.OnPolymesh(PolymeshTopology node)
        {
            String materialId = this.currentMaterialId;
            TrudeComponent component = this.currentElement.component;
            long size = component.geometries[materialId].vertices.Count / 3;

            for (int i = 0; i < node.NumberOfPoints; i++)
            {
                XYZ point = node.GetPoint(i);
                XYZ transformedPoint = CurrentTransform.OfPoint(point);
                if (component.category == "Doors")
                {
                    component.SetVertices(materialId, point.X, point.Z, point.Y);
                }
                else
                {
                    component.SetVertices(materialId, transformedPoint.X, transformedPoint.Z, transformedPoint.Y);
                }
            }

            for (int i = 0; i < node.NumberOfFacets; i++)
            {
                PolymeshFacet triangle = node.GetFacet(i);
                component.SetFaces(materialId, triangle.V1 + size, triangle.V2 + size, triangle.V3 + size);
            }

            for (int i = 0; i < node.NumberOfUVs; i++)
            {
                UV uv = node.GetUV(i);
                component.SetUVs(materialId, uv.U, uv.V);
            }

            return;
        }
    }
}