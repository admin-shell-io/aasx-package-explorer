namespace AasCore.Aas3_0_RC02.Attributes
{
    /// <summary>
    /// This attribute indicates, that the field / property shall be skipped for reflection
    /// in order to avoid cycles
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class SkipForReflection : System.Attribute
    {
    }
}
