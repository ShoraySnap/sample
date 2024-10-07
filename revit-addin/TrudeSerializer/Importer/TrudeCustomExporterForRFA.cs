using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;

namespace TrudeSerializer
{
    internal class TrudeCustomExporterForRFA : IExportContext
    {
        private readonly Document doc;
        private readonly Stack<Transform> transforms = new Stack<Transform>();
        private string currentMaterialId;
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

        public TrudeCustomExporterForRFA(Document doc)
        {
            this.doc = doc;
            transforms.Push(CurrentTransform);
            this.serializedSnaptrudeData = new SerializedTrudeData();
            GlobalVariables.CurrentDocument = doc;
        }

        bool IExportContext.Start()
        {
            Units units = doc.GetUnits();

            TrudeComponent component = new TrudeRFAComponent("0", "RFA", "RFA_FAMILY", "RFA_TYPE");

            currentElement = CurrentElement.SetCurrentElement(component);
            TrudeRFAComponent rfaComponent = component as TrudeRFAComponent;
            rfaComponent.type = doc.FamilyManager.CurrentType.Name;
            rfaComponent.category = "RFA";
            rfaComponent.family = doc.Title;
            if (rfaComponent.family.EndsWith(".rfa")) {
                rfaComponent.family = rfaComponent.family.Substring(0, rfaComponent.family.Length - 4);
            }
            serializedSnaptrudeData.ProjectProperties.SetRFA(true);

            serializedSnaptrudeData.AddRFAComponent(rfaComponent);

            return true;
        }

        void IExportContext.Finish()
        {
            return;
        }

        bool IExportContext.IsCanceled()
        {
#if !DIRECT_IMPORT

            return ExportToSnaptrudeEEH.IsImportAborted();
            #else
            return false;
            #endif
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
            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnLinkEnd(LinkNode node)
        {
            return;
        }

        RenderNodeAction IExportContext.OnElementBegin(ElementId elementId)
        {
            return RenderNodeAction.Proceed;
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

                component.SetVertices(materialId, transformedPoint.X, transformedPoint.Z, transformedPoint.Y);
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