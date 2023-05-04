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
