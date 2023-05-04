using System;
using System.Collections.Generic;
using static AasCore.Aas3_0.Reporting;

namespace AdminShellNS.Exceptions
{
    public class MetamodelVerificationException : Exception
    {
        public List<Error> ErrorList { get; }

        public MetamodelVerificationException(List<Error> errorList) : base($"The request body not conformant with the metamodel. Found {errorList.Count} errors !!")
        {
            ErrorList = errorList;
        }


    }
}
