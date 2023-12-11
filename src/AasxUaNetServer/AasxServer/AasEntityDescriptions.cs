/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AasOpcUaServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AasEntityDescriptions
    {
        private static Dictionary<string, string> keyToDescription = new Dictionary<string, string>();

        static AasEntityDescriptions()
        {
            AddDescription("AAS:Asset",
                @"An Asset describes meta data of an asset that is represented by an AAS. 
                The asset may either represent an asset type or an asset instance.
                The asset has a globally unique identifier plus – if needed – additional domain specific(proprietary)
                identifiers. ");

            AddDescription("AAS:Identifier",
                @"Used to uniquely identify an entity by using an identifier.");

            AddDescription("AAS:AdministrativeInformation",
                @"Administrative metainformation for an element like version information.");

            AddDescription("AAS:AssetAdministrationShell",
                @"An AssetAdministration Shell.");

            AddDescription("AAS:Submodel",
                @"A Submodel defines a specific aspect of the asset represented by the AAS.
				A submodel is used to structure the digital representation and technical functionality of an 
                Administration Shell into distinguishable parts. 
				Each submodel refers to a well-defined domain or subject matter. Submodels can become standardized 
                and thus become submodels types. Submodels can have different life-cycles. ");

            AddDescription("AAS:SubmodelElement",
                @"A data element is a submodel element that is not further composed out of other submodel elements. 
				A data element is a submodel element that has a value. The type of value differs for different 
                subtypes of data elements.");

            AddDescription("AAS:Property",
                @"A property is a data element that has a single value.");

            AddDescription("AAS:SubmodelElementCollection",
                @"A submodel element collection is a set or list of submodel elements.");

            AddDescription("AAS:SubmodelElementCollection",
                @"A submodel element collection is a set or list of submodel elements.");

            AddDescription("AAS:File",
                @"A File is a data element that represents an address to a file. The value is an URI that can 
                represent an absolute or relative path.");

            AddDescription("AAS:Blob",
                @"A BLOB is a data element that represents a file that is contained with its source code in the 
                value attribute.");

            AddDescription("AAS:ReferenceElement",
                @"A reference element is a data element that defines a logical reference to another element within 
                the same or another AAS or a reference to an external object or entity.");

            AddDescription("AAS:RelationshipElement",
                @"A relationship element is used to define a relationship between two referable elements.");

            AddDescription("AAS:OperationVariable",
                @"An operation variable is a submodel element that is used as input or output variable of an 
                operation.");

            AddDescription("AAS:Operation",
                @"An operation is a submodel element with input and output variables.");

            AddDescription("AAS:View",
                @"A view is a collection of referable elements w.r.t. to a specific viewpoint of one or more 
                stakeholders.");

            AddDescription("AAS:ConceptDictionary",
                @"A dictionary contains elements that can be reused.
				The concept dictionary contains concept descriptions.
				Typically a concept description dictionary of an AAS contains only concept descriptions of 
                elements used within submodels of the AAS");

            AddDescription("AAS:DataSpecification",
                @"A data specification template defines the additional attributes an element may or shall have.");

            AddDescription("AAS:DataSpecificationIEC61360",
                @"Data Specification Template conformant to IEC61360.");

            AddDescription("AAS:ConceptDescription",
                @"The semantics of a property or other elements that may have a semantic description is defined by a 
                concept description. The description of the concept should follow a standardized schema 
                (realized as data specification template).");

            AddDescription("AAS:Identifiable",
                @"An element that has a globally unique identifier. ");

            AddDescription("AAS:Referable",
                @"An element that is referable but has no globally unique. The id of the element is unique within the 
                name space of the element.");

        }

        static void AddDescription(string key, string description)
        {
            if (key == null)
                return;
            keyToDescription[key.Trim().ToLower()] = description;
        }

        public static string LookupDescription(string key)
        {
            if (key == null || !keyToDescription.ContainsKey(key.Trim().ToLower()))
                return null;
            var desc = keyToDescription[key.Trim().ToLower()];
            desc = Regex.Replace(desc, @"\s+", " ");
            return desc;
        }

    }
}
