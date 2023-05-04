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
