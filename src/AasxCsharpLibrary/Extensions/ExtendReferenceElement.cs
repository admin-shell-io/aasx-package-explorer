/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AAS = AasCore.Aas3_0;

namespace Extensions
{
    public static class ExtendReferenceElement
    {
        public static AAS.ReferenceElement Set(this AAS.ReferenceElement elem,
            Reference rf)
        {
            elem.Value = rf;
            return elem;
        }

        public static AAS.ReferenceElement UpdateFrom(
            this AAS.ReferenceElement elem, AAS.ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((AAS.ISubmodelElement)elem).UpdateFrom(source);

            if (source is AAS.RelationshipElement srcRel)
            {
                if (srcRel.First != null)
                    elem.Value = srcRel.First.Copy();
            }

            if (source is AAS.AnnotatedRelationshipElement srcRelA)
            {
                if (srcRelA.First != null)
                    elem.Value = srcRelA.First.Copy();
            }

            return elem;
        }
    }
}
