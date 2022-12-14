namespace AasCore.Aas3_0_RC02.Attributes
{
    /// <summary>
    /// This attribute indicates, that the field / property is searchable
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class MetaModelName : System.Attribute
    {
        public string name;
        public MetaModelName(string name)
        {
            this.name = name;
        }
    }
}
