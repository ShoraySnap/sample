using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Components;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;
using TrudeSerializer.Utils;

namespace TrudeSerializer
{
    /*! Snaptrude's Custom Exporter class.
     *  This class allows custom exporting 3D views of a Revit model.
     *  It inherits from the IExportContext interface, which defines methods for export process pipeline.
     */
    internal class TrudeCustomExporter : IExportContext
    {
        private bool isRevitLink = false; /*!< Flag to check if the current element is a Revit link. */
        private CurrentLink currentLink; /*!< Used to track the name of current RevitLink. */
        private List<string> revitLinks = new List<string>(); /*!< Used to track the RevitLink files to avoid circular dependency */

        private Document doc; /*!< Used to track the active document, which changes in case of RevitLinks */
        private Stack<Transform> transforms = new Stack<Transform>();

        private String currentMaterialId; /*!< Used to track the current MaterialId. */
        private CurrentElement currentElement; /*!< Used to track the current element. */
        private List<string> elementIds = new List<string>(); /*!< Tracks all elements to avoid duplication, as in the case of some Generic Models. */
        public SerializedTrudeData serializedSnaptrudeData;  /*!< The main SerializedTrudeData object, which stores the serialized data.  */

        /*!
        *  @return serializedSnaptrudeData
        */
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

        /*!
        * Used to change the current document in case of RevitLinks
        * @param doc new document to track
        */
        private void ChangeCurrentDocument(Document doc)
        {
            this.doc = doc;
            GlobalVariables.CurrentDocument = doc;
        }

        /*!
        * Constructor for TrudeCustomExporter class.
        * @param doc new document to track
        */
        public TrudeCustomExporter(Document doc)
        {
            this.doc = doc;
            transforms.Push(CurrentTransform);
            this.serializedSnaptrudeData = new SerializedTrudeData();
            GlobalVariables.CurrentDocument = doc;
        }

        /*!
        * Called when the export process starts.
        * The first step of export pipeline. Only called once in the export process.
        * @return bool
        */
        bool IExportContext.Start()
        {
            UnitConversion.GetUnits(doc, serializedSnaptrudeData);
            return true;
        }

        /*!
        * Called when the export process finishes.
        * The last step of export pipeline. Only called once in the export process.
        */
        void IExportContext.Finish()
        {
            if(ExportToSnaptrudeEEH.ExportInterrupted)
            {
                ExportToSnaptrudeEEH.InterruptWithAbort();
            }
            ComponentHandler.Instance.AddLevelsToSerializedData(serializedSnaptrudeData, doc);
            return;
        }

        bool IExportContext.IsCanceled()
        {
            return ExportToSnaptrudeEEH.IsImportAborted();
        }

        RenderNodeAction IExportContext.OnViewBegin(ViewNode node)
        {
            return RenderNodeAction.Proceed;
        }

        void IExportContext.OnViewEnd(ElementId elementId)
        {
            return;
        }

        /*!
        * Init for RevitLink element export - sets current doc and currentLink, checks for circular dependency.
        * Part of the export pipeline, called after onElementBegin at the start each time a linked project element is encountered.
        * @param node the current LinkNode
        * @return RenderNodeAction Proceed or Skip
        */
        RenderNodeAction IExportContext.OnLinkBegin(LinkNode node)
        {
            ExportToSnaptrudeEEH.InterruptsReset();

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

        /*!
        * Last step of RevitLink element export - resets current doc and currentLink
        * @param node
        */
        void IExportContext.OnLinkEnd(LinkNode node)
        {
            ChangeCurrentDocument(GlobalVariables.Document);
            currentLink.name = "";
            currentLink.category = "";
            isRevitLink = false;
            transforms.Pop();
        }

        /*!
        * Adds element to serializedSnaptrudeData and skips the flow for invalid elements.
        * Part of the export pipeline, called after onViewBegin for each element encountered.
        * @param elementId
        * @return RenderNodeAction Proceed or Skip
        */
        RenderNodeAction IExportContext.OnElementBegin(ElementId elementId)
        {
            ExportToSnaptrudeEEH.InterruptsReset();

            Element element = doc.GetElement(elementId);
            string category = element?.Category?.Name;
            if (category == "Levels")
            {
                return RenderNodeAction.Skip;
            }

            TrudeComponent component = null;
            if (isRevitLink)
            {
                component = AddLinkedComponentToSerializedData(elementId, element);
                if (component == null)
                {
                    return RenderNodeAction.Skip;
                }
            }
            else
            {
                try
                {
                    if (isDuplicateElement(element))
                    {
                        return RenderNodeAction.Skip;
                    }
                    component = ComponentHandler.Instance.GetComponent(serializedSnaptrudeData, element);
                    if (component?.elementId == "-1")
                    {
                        return RenderNodeAction.Skip;
                    }

                    ComponentHandler.Instance.AddComponent(serializedSnaptrudeData, component, element);
                }
                catch (Exception)
                {
                    return RenderNodeAction.Skip;
                }
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

        /*!
        * Checks if an element has appeared before.
        * @param element
        * @return bool true if element is duplicate, false otherwise
        */
        private bool isDuplicateElement(Element element)
        {
            string elementId = element.Id.ToString();
            if (elementIds.Contains(elementId))
            {
                return true;
            }
            elementIds.Add(elementId);
            return false;
        }

        /*!
        * Adds revitLink element to serializedSnaptrudeData.
        * Also checks of undesirable elements like Levels, Model Groups, Divisions
        * @param elementId
        * @param element
        * @return TrudeMass the RevitLink element converted to TrudeMass
        */
        private TrudeComponent AddLinkedComponentToSerializedData(ElementId elementId, Element element)
        {
            TrudeMass mass = TrudeMass.GetSerializedComponent(serializedSnaptrudeData, element);
            if (mass.elementId == "-1" || mass.subType == "Levels" || mass.subType == "Model Groups" || mass.subType == "Divisions")
            {
                return null;
            }

            ComponentHandler.Instance.AddComponent(serializedSnaptrudeData, mass, currentLink.name, elementId.ToString());

            return mass;
        }

        /*!
        * Part of the export pipeline, called at the end of each element export.
        * @param elementId
        */
        void IExportContext.OnElementEnd(ElementId elementId)
        {
            return;
        }

        /*!
        * Part of the export pipeline, called if the element has a family instance.
        * @param node InstanceNode
        * @return RenderNodeAction Proceed
        */
        RenderNodeAction IExportContext.OnInstanceBegin(InstanceNode node)
        {
            ExportToSnaptrudeEEH.InterruptsReset();
            transforms.Push(CurrentTransform.Multiply(node.GetTransform()));
            return RenderNodeAction.Proceed;
        }

        /*!
        * Part of the export pipeline, called after all faces are done for an element.
        * @param node InstanceNode
        * @return RenderNodeAction void
        */
        void IExportContext.OnInstanceEnd(InstanceNode node)
        {
            transforms.Pop();
        }

        RenderNodeAction IExportContext.OnFaceBegin(FaceNode node)
        {
            ExportToSnaptrudeEEH.InterruptsReset();
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

        /*!
        * Used to extract material from the face.
        * Part of the export pipeline, called after onElementBegin / onInstanceBegin for each element encountered.
        * @param node MaterialNode
        */
        void IExportContext.OnMaterial(MaterialNode node)
        {
            String materialId = node.MaterialId.ToString();
            this.currentMaterialId = materialId;
            TrudeCategory category = CurrentElement.GetCategory(currentElement);

            if (this.currentElement.HasMaterial(materialId)) return;

            Element material = doc.GetElement(node.MaterialId);

            TrudeMaterial trudeMaterial = TrudeMaterial.GetMaterial(material as Material, category);
            this.currentElement.AddMaterial(materialId);
            this.currentElement.component.SetMaterial(currentMaterialId, trudeMaterial);

            return;
        }

        /*!
        * Used to extract vertices, faces and uvs for the current face.
        * Part of the export pipeline, called after assigning material to the elemnt.
        * @param node PolymeshTopology
        */
        void IExportContext.OnPolymesh(PolymeshTopology node)
        {
            String materialId = this.currentMaterialId;
            TrudeComponent component = this.currentElement.component;
            long size = component.geometries[materialId].vertices.Count / 3;

            for (int i = 0; i < node.NumberOfPoints; i++)
            {
                XYZ point = node.GetPoint(i);
                XYZ transformedPoint = CurrentTransform.OfPoint(point);
                if (component.category == "Doors" || component.category == "Windows")
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