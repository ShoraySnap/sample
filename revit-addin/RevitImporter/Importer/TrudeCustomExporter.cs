﻿using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Importer;


namespace TrudeSerializer
{
    class TrudeCustomExporter : IExportContext
    {
        Document doc;
        Stack<Transform> transforms = new Stack<Transform>();
        private object familyData;
        private object creationData;
        private String currentMaterialId;
        private FamilyElement currentFamilyElement;
        public SerializedTrudeData serializedSnaptrudeData;

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
            transforms.Push(CurrentTransform);
            this.familyData = new Object();
            this.creationData = new Object();
            this.serializedSnaptrudeData = new SerializedTrudeData();
            this.currentFamilyElement = new FamilyElement("", "");
        }

        bool IExportContext.Start()
        {
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
            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnLinkEnd(LinkNode node)
        {
            // implement link part
        }

        RenderNodeAction IExportContext.OnElementBegin(ElementId elementId)
        {
            Element element = doc.GetElement(elementId);
            ComponentHandler.Instance.SetData(serializedSnaptrudeData, element);
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
            String name = this.currentFamilyElement.name;
            String category = this.currentFamilyElement.category;

            if (!this.currentFamilyElement.HasMaterial(materialId))
            {
                Element material = doc.GetElement(node.MaterialId);

                if (material != null && material.IsValidObject)
                {
                }
                else
                {
                    Color nodeColor = node.Color;
                }
            }

            return;
        }

        void IExportContext.OnPolymesh(PolymeshTopology node)
        {
            String name = this.currentFamilyElement.name;
            String category = this.currentFamilyElement.category;
            String materialId = this.currentMaterialId;

            return;
        }
    }
}