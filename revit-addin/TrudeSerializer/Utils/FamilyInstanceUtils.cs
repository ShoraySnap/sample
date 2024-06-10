using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Components;

namespace TrudeSerializer.Utils
{
    internal class FamilyInstanceUtils
    {
        public static bool HasParentElement(Element element, bool ignoreAssemblies = false, bool ignoreModelGroups = false)
        {
            bool hasParentElement = false;

            if (element is FamilyInstance familyInstance)
            {
                Element superComponent = familyInstance.SuperComponent;
                if (superComponent != null)
                {
                    hasParentElement = true;
                }
            }

            if (!ignoreAssemblies)
            {
                ElementId assemblySuperComponent = element?.AssemblyInstanceId;

                bool HasAssemblySuperComponent = assemblySuperComponent != null && assemblySuperComponent.IntegerValue.ToString() != "-1";

                if (HasAssemblySuperComponent)
                {
                    hasParentElement = true;
                }
            }

            if (!ignoreModelGroups)
            {
                // Group (SuperComponent)
                ElementId groupId = element?.GroupId;
                bool hasGroupSuperComponent = groupId != null && groupId.IntegerValue.ToString() != "-1";

                if (hasGroupSuperComponent)
                {
                    hasParentElement = true;
                }
            }

            return hasParentElement;
        }

        public static List<string> GetSubComponentIds(Element element)
        {
            List<string> subComponentIds = new List<string> { };

            if (element is FamilyInstance)
            {
                ICollection<ElementId> subComponents = (element as FamilyInstance).GetSubComponentIds();
                if (subComponents.Count > 0)
                {
                    foreach (ElementId subComponent in subComponents)
                    {
                        subComponentIds.Add(subComponent.ToString());
                    }

                    return subComponentIds;
                }
            }

            if (element is AssemblyInstance || element is Group)
            {
                IList<ElementId> dependantElements = element.GetDependentElements(null);

                if (dependantElements.Count > 0)
                {
                    for (int i = 0; i < dependantElements.Count; i++)
                    {
                        Element dependantElement = GlobalVariables.CurrentDocument.GetElement(dependantElements[i]);
                        if (TrudeFurniture.IsFurnitureCategory(dependantElement))
                        {
                            subComponentIds.Add(dependantElement.Id.ToString());
                        }
                    }

                    return subComponentIds;
                }
            }

            return subComponentIds;
        }
    }
}