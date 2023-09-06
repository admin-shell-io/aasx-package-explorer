// SOURCE: https://github.com/OPCFoundation/UA-.NETStandard/blob/1.4.355.26/SampleApplications/
// Samples/Opc.Ua.Sample/SampleServer.cs

/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

// ReSharper disable all

using System.Collections.Generic;
using System.Diagnostics;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Server;

namespace AasOpcUaServer
{
    /// <summary>
    /// A class which implements an instance of a UA server.
    /// </summary>
    public partial class SampleServer : StandardServer
    {
        private AdminShellPackageEnv thePackageEnv = null;
        private AasxUaServerOptions theServerOptions = null;

        public SampleServer(AdminShellPackageEnv env, AasxUaServerOptions serverOptions = null)
            : base()
        {
            thePackageEnv = env;
            theServerOptions = serverOptions;
        }

        /// <summary>
        /// Creates the node managers for the server.
        /// </summary>
        /// <remarks>
        /// This method allows the sub-class create any additional node managers which it uses. The SDK
        /// always creates a CoreNodeManager which handles the built-in nodes defined by the specification.
        /// Any additional NodeManagers are expected to handle application specific nodes.
        /// 
        /// Applications with small address spaces do not need to create their own NodeManagers and can add any
        /// application specific nodes to the CoreNodeManager. Applications should use custom NodeManagers when
        /// the structure of the address space is stored in another system or when the address space is too large
        /// to keep in memory.
        /// </remarks>
        protected override MasterNodeManager CreateMasterNodeManager(
            IServerInternal server, ApplicationConfiguration configuration)
        {
            Debug.WriteLine("Creating the Node Managers.");

            List<INodeManager> nodeManagers = new()
            {
                new AasNodeManager(server, configuration, thePackageEnv, theServerOptions)
            };

            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        /// <summary>
        /// Loads the non-configurable properties for the application.
        /// </summary>
        /// <remarks>
        /// These properties are exposed by the server but cannot be changed by administrators.
        /// </remarks>
        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new()
            {
                ManufacturerName = "OPC Foundation",
                ProductName = "OPC UA SDK Samples",
                ProductUri = "http://opcfoundation.org/UA/Samples/v1.0",
                SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
                BuildNumber = Utils.GetAssemblyBuildNumber(),
                BuildDate = Utils.GetAssemblyTimestamp()
            };

            return properties;
        }
    }
}
