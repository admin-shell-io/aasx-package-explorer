using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public class AasElementSelfDescription
    {
        public string AasElementName { get; set; }

        public string ElementAbbreviation { get; set; }

        public AasElementSelfDescription(string aasElementName, string elementAbbreviation)
        {
            AasElementName = aasElementName;
            ElementAbbreviation = elementAbbreviation;
        }
    }
}
