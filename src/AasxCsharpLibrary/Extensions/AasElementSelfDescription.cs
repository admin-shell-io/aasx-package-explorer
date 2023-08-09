/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
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
