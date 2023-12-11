using System;

namespace AasCore.Aas3_0.Attributes
{
    /// <summary>
    /// This attribute indicates, that it should e.g. serialized in JSON.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class CountForHash : Attribute
    {
    }
}
