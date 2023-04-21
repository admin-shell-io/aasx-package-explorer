using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminShellNS.Exceptions
{
    public class NullValueException : Exception
    {
        public NullValueException(string field) : base($"The field {field} is null!!")
        {

        }
    }
}
