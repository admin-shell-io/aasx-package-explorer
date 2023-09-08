/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
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
