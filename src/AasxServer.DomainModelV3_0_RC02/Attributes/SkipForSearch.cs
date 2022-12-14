namespace AasCore.Aas3_0_RC02.Attributes
{
    /// <summary>
    /// This attribute indicates, that the field / property shall be skipped for searching, because it is not
    /// directly displayed in Package Explorer
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class SkipForSearch : System.Attribute
    {
    }
}
