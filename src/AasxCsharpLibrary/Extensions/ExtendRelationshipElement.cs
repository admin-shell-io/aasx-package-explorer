/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AAS = AasCore.Aas3_0;

namespace Extensions
{
    public static class ExtendRelationshipElement
    {
        public static AAS.RelationshipElement Set(this AAS.RelationshipElement elem,
            AAS.Reference first, AAS.Reference second)
        {
            elem.First = first;
            elem.Second = second;
            return elem;
        }

        public static AAS.RelationshipElement UpdateFrom(
            this AAS.RelationshipElement elem, AAS.ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((AAS.ISubmodelElement)elem).UpdateFrom(source);

            if (source is AAS.ReferenceElement srcRef)
            {
                if (srcRef.Value != null)
                    elem.First = srcRef.Value.Copy();
            }

            if (source is AAS.AnnotatedRelationshipElement srcRelA)
            {
                if (srcRelA.First != null)
                    elem.First = srcRelA.First.Copy();
            }

            return elem;
        }
    }
}
