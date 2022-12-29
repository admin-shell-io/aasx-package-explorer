using AasCore.Aas3_0_RC02;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Extensions
{
    public static class ExtendKeyTypes
    {
        public static bool IsSME(this KeyTypes keyType)
        {
            foreach (var kt in Constants.AasSubmodelElementsAsKeys)
                if (kt.HasValue && kt.Value == keyType)
                    return true;
            return false;
        }
    }
}
