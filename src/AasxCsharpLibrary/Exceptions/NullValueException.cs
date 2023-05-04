using System;

namespace AdminShellNS.Exceptions
{
    public class NullValueException : Exception
    {
        public NullValueException(string field) : base($"The field {field} is null!!")
        {

        }
    }
}
