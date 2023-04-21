using AasCore.Aas3_0_RC02;
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

        public KeyTypes? KeyType { get; set; }

        public AasSubmodelElements? SmeType { get; set; }

        public AasElementSelfDescription(string aasElementName, string elementAbbreviation,
            KeyTypes? keyType, AasSubmodelElements? smeType)
        {
            AasElementName = aasElementName;
            ElementAbbreviation = elementAbbreviation;
            KeyType = keyType;
            SmeType = smeType;
        }
    }
}
