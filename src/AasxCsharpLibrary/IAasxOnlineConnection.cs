/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxIntegrationBase
{
    /// <summary>
    /// This interface describes a connection for a server resource of AASX contents, such as OPC UA or REST
    /// </summary>
    public interface IAasxOnlineConnection
    {
        bool IsValid();
        bool IsConnected();
        string GetInfo();
        Stream GetThumbnailStream();
        string UpdatePropertyValue(
            AdminShell.AdministrationShellEnv env, AdminShell.Submodel submodel, AdminShell.SubmodelElement sme);
    }
}
