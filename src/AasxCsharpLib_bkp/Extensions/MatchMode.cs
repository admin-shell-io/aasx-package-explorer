using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public enum MatchMode
    {
        Strict,  //may be not needed in future, as no local flag in V3
        Relaxed, //should be as default
        Identification
    }
}
