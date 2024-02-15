using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Importer;
using TrudeSerializer.Components;

namespace TrudeSerializer
{
    class TrudeCustomExporter : IExportContext
    {
        Document doc;
        Document primaryDoc;
        Stack<Transform> transforms = new Stack<Transform>();
        private object familyData;
        private object creationData;
        private String currentMaterialId;
        private FamilyElement currentFamilyElement;
        public SerializedTrudeData serializedSnaptrudeData;
        private TrudeComponent currentComponent;

        public SerializedTrudeData GetExportData()
        {
            return serializedSnaptrudeData;
        }

        Transform CurrentTransform
        {
            get
            {
                if (transforms.Count == 0)
                    return Transform.Identity;
                else
                    return transforms.Peek();
            }
        }

        public TrudeCustomExporter(Document doc)
        {
            this.doc = doc;
            this.primaryDoc = doc;
            transforms.Push(CurrentTransform);
            this.familyData = new Object();
            this.creationData = new Object();
            this.serializedSnaptrudeData = new SerializedTrudeData();
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
            doc = node.GetDocument();
            currentFamilyElement.name = doc.Title.Replace(".", "");
            transforms.Push(CurrentTransform.Multiply(node.GetTransform()));

            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnLinkEnd(LinkNode node)
        {
            // implement link part
            doc = primaryDoc;
            transforms.Pop();
        }

        RenderNodeAction IExportContext.OnElementBegin(ElementId elementId)
        {
            Element element = doc.GetElement(elementId);
            TrudeComponent component = ComponentHandler.Instance.GetComponent(serializedSnaptrudeData, element);
            if (component.elementId == "-1")
            {
                return RenderNodeAction.Skip;
            }

            AddComponentToSerializedData(component);
            currentComponent = component;

            if (component.IsParametric())
            {
                return RenderNodeAction.Skip;
            }

            this.currentFamilyElement = FamilyElement.SetCurrentFamilyElement(component);

            return RenderNodeAction.Proceed;
        }

        void AddComponentToSerializedData(TrudeComponent component)
        {
            if (component is TrudeWall)
            {
                serializedSnaptrudeData.AddWall(component as TrudeWall);
            }
            else if (component is TrudeLevel)
            {
                serializedSnaptrudeData.AddLevel(component as TrudeLevel);
            }
            else if (component is TrudeMass)
            {
                serializedSnaptrudeData.AddMass(component as TrudeMass);
            }
            else if (component is TrudeRevitLink)
            {
                serializedSnaptrudeData.AddRevitLink(component as TrudeRevitLink);
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

            if (this.currentFamilyElement.HasMaterial(materialId)) return;

            Element material = doc.GetElement(node.MaterialId);

            TrudeMaterial trudeMaterial = TrudeMaterial.GetMaterial(material as Material);
            currentComponent.SetMaterial(currentMaterialId, trudeMaterial);

            return;
        }

        void IExportContext.OnPolymesh(PolymeshTopology node)
        {
            String materialId = this.currentMaterialId;

            long size = currentComponent.geometries[materialId].Vertices.Count / 3;

            for (int i = 0; i < node.NumberOfPoints; i++)
            {
                XYZ vertex = node.GetPoint(i);
                XYZ transformedPoint = CurrentTransform.OfPoint(vertex);
                currentComponent.SetVertices(materialId, transformedPoint.X, transformedPoint.Z, transformedPoint.Y);
            }

            for (int i = 0; i < node.NumberOfFacets; i++)
            {
                PolymeshFacet triangle = node.GetFacet(i);
                currentComponent.SetFaces(materialId, triangle.V1 + size, triangle.V2 + size, triangle.V3 + size);
            }

            for (int i = 0; i < node.NumberOfUVs; i++)
            {
                UV uv = node.GetUV(i);
                currentComponent.SetUVs(materialId, uv.U, uv.V);
            }

            return;
        }
    }
}