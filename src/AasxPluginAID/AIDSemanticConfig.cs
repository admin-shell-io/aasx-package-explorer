/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using AasxPluginAID;
using Microsoft.VisualBasic;
using AnyUi;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using AasCore.Aas3_0;
using ImageMagick;
using JetBrains.Annotations;

namespace AasxPluginAID
{
    public class AIDSemanticConfig

    {
        public Aas.Key SemIdReferencedObject = null;
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
        public static FormDescSubmodelElement BuildUIFormElem(JToken formElem)
        {
            JObject elemJObject = JObject.FromObject(formElem);

            string elem = elemJObject["formtext"].ToString();
            string elemType = elemJObject["AasElementType"].ToString();
            string presetIdShort = elemJObject["presetIdShort"].ToString();

            FormMultiplicity multiplicity = idtaDef.GetFormMultiplicity(elemJObject["multiplcity"].ToString());
            Aas.Key semanticReferenceKey = idtaDef.ConstructKey(KeyTypes.GlobalReference, elemJObject["semanticReference"].ToString());
            
            LangStringTextType description = new Aas.LangStringTextType("en", elemJObject["description"].ToString());
            
            if (elemType == "Property")
            {
                string valueType = elemJObject["valueType"].ToString();
                FormDescProperty _propertyFrom = new FormDescProperty(elem,
                        multiplicity,semanticReferenceKey, presetIdShort,
                        valueType: valueType);
                _propertyFrom.PresetDescription.Add(description);
                
                return _propertyFrom;
            }
            else if (elemJObject["AasElementType"].ToString() == "SubmodelElementCollection")
            {
                FormDescSubmodelElementCollection _formCollection = new FormDescSubmodelElementCollection(elem,
                        multiplicity, semanticReferenceKey, presetIdShort
                        );
                _formCollection.PresetDescription.Add(description);
                foreach (var childElem in elemJObject["childs"])
                {
                    _formCollection.Add(BuildUIFormElem(childElem));
                }
                return _formCollection;
            }
            else if (elemJObject["AasElementType"].ToString() == "Range")
            {
                string valueType = elemJObject["valueType"].ToString();
                FormDescRange _rangeFrom = new FormDescRange(elem,
                        multiplicity, semanticReferenceKey, presetIdShort,
                        valueType: valueType);
                _rangeFrom.PresetDescription.Add(new Aas.LangStringTextType("en", elemJObject["description"].ToString()));

                return _rangeFrom;
            }
            else if (elemJObject["AasElementType"].ToString() == "ReferenceElement")
            {
                FormDescReferenceElement _referenceElementFrom = new FormDescReferenceElement(elem,
                        multiplicity, semanticReferenceKey, presetIdShort
                        );
                _referenceElementFrom.PresetDescription.Add(new Aas.LangStringTextType("en", elemJObject["description"].ToString()));

                return _referenceElementFrom;
            }
            else if (elemJObject["AasElementType"].ToString() == "File")
            {
                FormDescFile _fileElementForm = new FormDescFile(elem,
                        multiplicity, semanticReferenceKey, presetIdShort
                        );

                return _fileElementForm;
            }
            return null;
        }        
        public static FormDescSubmodelElementCollection CreateAssetInterfaceDescription()
        {
            try
            {
                FormDescSubmodelElementCollection interfaceDescription = BuildUIFormElem(idtaDef.EndpointMetadataJObject["interface"]) as FormDescSubmodelElementCollection;

                return interfaceDescription;
            }
            catch(Exception ex) {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
        
    }
}