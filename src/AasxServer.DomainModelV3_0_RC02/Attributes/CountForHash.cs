using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasCore.Aas3_0_RC02.Attributes
{
    /// <summary>
    /// This attribute indicates, that it should e.g. serialized in JSON.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class CountForHash : Attribute
    {
    }
}
