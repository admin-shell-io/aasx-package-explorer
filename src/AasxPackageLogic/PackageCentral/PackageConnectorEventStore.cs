/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Holds a memory of event messages delivered by PackageCentral.
    /// </summary>
    public class PackageConnectorEventStore : PackageConnectorBase
    {
        private ObservableCollection<AasEventMsgEnvelope> _eventStore = new ObservableCollection<AasEventMsgEnvelope>();

        //
        // Constructors
        //

        public PackageConnectorEventStore(PackageContainerBase container)
            : base(container)
        {
        }

        //
        // Helper
        //

        public override string ToString()
        {
            lock (_eventStore)
                return $"EVENT-STORE connector with {"" + _eventStore?.Count} events";
        }

        //
        // Interface
        //

        /// <summary>
        /// PackageCentral pushes an AAS event message down to the connector.
        /// Return true, if the event shall be consumed and PackageCentral shall not
        /// push anything further.
        /// </summary>
        /// <param name="ev">The event message</param>
        /// <returns>True, if consume event</returns>
        public override bool PushEvent(AasEventMsgEnvelope ev)
        {
            // add
            if (_eventStore != null)
            {
                lock (_eventStore)
                    _eventStore.Insert(0, ev);
            }

            // do not consume, just want to listen!
            return false;
        }

        /// <summary>
        /// Pops the oldest event ..
        /// </summary>
        public AasEventMsgEnvelope PopEvent()
        {
            if (_eventStore == null)
                return null;

            lock (_eventStore)
            {
                if (_eventStore.Count < 1)
                    return null;
                var res = _eventStore[0];
                _eventStore.RemoveAt(0);
                return res;
            }
        }
    }
}
