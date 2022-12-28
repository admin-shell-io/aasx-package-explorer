﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AAS = AasCore.Aas3_0_RC02;
using AdminShellNS.Extenstions;

namespace Extensions
{
    public static class ExtendRelationshipElement
    {
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