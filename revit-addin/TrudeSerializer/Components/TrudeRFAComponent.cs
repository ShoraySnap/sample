using System;
using System.Collections.Generic;
using System.Text;

namespace TrudeSerializer.Components
{
    internal class TrudeRFAComponent: TrudeComponent
    {
        public string type;
        public TrudeRFAComponent(string elementId, string category, string family, string type) : base(elementId, category, family, "-1")
        {
            this.type = type; 
        }
    }
}
