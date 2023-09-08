/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;


namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Exceptions thrown when handling PackageConnectors
    /// </summary>
    public class PackageConnectorException : Exception
    {
        public PackageConnectorException() { }
        public PackageConnectorException(string message) : base(message) { }
        public PackageConnectorException(string message, Exception innerException) : base(message, innerException) { }
    }

    public interface IPackageConnectorManageEvents
    {
        bool PushEvent(AasEventMsgEnvelope ev);
    }

    /// <summary>
    /// A package connector create a "live" link to a package residing on another server.
    /// The "live" link coulbe be e.g. events based, but also be a OPC UA connection, a 
    /// file watcher instance, a ODBC connection or somethink else.
    /// The thinking is, that a FULL package is being retrieved by a PackageConnector,
    /// while parts are being retrieved or updated (in both directions) by a PackageConnector.
    /// Therefore, the PackageConnector is seen as an extension, ALWAYS maintaining the link
    /// to an underlying <c>PackageContainerBase.</c>
    /// </summary>
    public class PackageConnectorBase : IPackageConnectorManageEvents
    {
        /// <summary>
        /// Link to container; as a (test-wise) design constraint only set by constrction ..
        /// </summary>
        private PackageContainerBase _container;

        /// <summary>
        /// Link to the underlying / fundamental PackageContainer.
        /// </summary>
        public PackageContainerBase Container { get { return _container; } }

        /// <summary>
        /// Shortcut to PackageContainer's Environment.
        /// </summary>
        public AdminShellPackageEnv Env { get { return _container?.Env; } }

        //
        // Constructors
        //

        /// <summary>
        /// By design, a PackageConnector is based on a PackageContainer.
        /// </summary>
        public PackageConnectorBase(PackageContainerBase container)
        {
            _container = container;
        }

        public PackageConnectorBase()
        {
        }

        //
        // Event handling
        //

        /// <summary>
        /// PackageCentral pushes an AAS event message down to the connector.
        /// Return true, if the event shall be consumed and PackageCentral shall not
        /// push anything further.
        /// </summary>
        /// <param name="ev">The event message</param>
        /// <returns>True, if consume event</returns>
        public virtual bool PushEvent(AasEventMsgEnvelope ev) { return false; }
    }
}
