using AdminShellNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
