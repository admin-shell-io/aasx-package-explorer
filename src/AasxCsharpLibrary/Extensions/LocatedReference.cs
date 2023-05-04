namespace Extensions
{
    public class LocatedReference
    {
        public IIdentifiable Identifiable;
        public IReference Reference;

        public LocatedReference() { }
        public LocatedReference(IIdentifiable identifiable, IReference reference)
        {
            Identifiable = identifiable;
            Reference = reference;
        }
    }
}
