using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace TrudeSerializer.Utils
{
    internal class FamilyInstanceUtils
    {
        public static bool HasParentElement(Element element, bool ignoreModelGroupAndAssemblies = false)
        {
            bool hasParentElement = false;

            if (element is FamilyInstance familyInstance)
            {
                Element superComponent = familyInstance.SuperComponent;
                if (superComponent != null)
                {
                    hasParentElement = true;
                }

                if (ignoreModelGroupAndAssemblies)
                {
                    return hasParentElement;
                }
            }

            ElementId assemblySuperComponent = element?.AssemblyInstanceId;
#if (REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023)
            bool HasAssemblySuperComponent = assemblySuperComponent != null && assemblySuperComponent.IntegerValue.ToString() != "-1";
#else
            bool HasAssemblySuperComponent = assemblySuperComponent != null && assemblySuperComponent.Value.ToString() != "-1";
#endif

            if (HasAssemblySuperComponent)
            {
                hasParentElement = true;
            }

            ElementId groupId = element?.GroupId;
#if (REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023)
            bool hasGroupSuperComponent = groupId != null && groupId.IntegerValue.ToString() != "-1";
#else
            bool hasGroupSuperComponent = groupId != null && groupId.Value.ToString() != "-1";
#endif

            if (hasGroupSuperComponent)
            {
                hasParentElement = true;
            }

            return hasParentElement;
        }

        public static List<string> GetSubComponentIds(Element element)
        {
            List<string> subComponentIds = new List<string> { };

            IList<ElementId> dependantElements = element.GetDependentElements(null);

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

            if (element is AssemblyInstance)
            {
                if (dependantElements.Count > 0)
                {
                    subComponentIds = dependantElements.Select(dependantElement => dependantElement.ToString()).ToList();
                    return subComponentIds;
                }
            }

            if (element is Group)
            {
                if (dependantElements.Count > 0)
                {
                    subComponentIds = dependantElements.Select(dependantElement => dependantElement.ToString()).ToList();
                    return subComponentIds;
                }
            }

            return subComponentIds;
        }
    }
}