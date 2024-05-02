using System;
using TrudeSerializer.Components;

namespace TrudeSerializer.Types
{
    internal class CurrentLink
    {
        public string name;
        public string category;

        public CurrentLink(string name, string category)
        {
            this.name = name;
            this.category = category;
        }
    }
}