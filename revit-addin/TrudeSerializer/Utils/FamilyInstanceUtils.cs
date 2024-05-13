using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

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

            if(!ignoreModelGroups)
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