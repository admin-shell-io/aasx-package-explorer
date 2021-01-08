/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using AasxIntegrationBase;
using AasxWpfControlLibrary.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxWpfControlLibrary.AasxFileRepo
{
    /// <summary>
    /// AasxFileRepository, which is held synchronized with a AAS REST repository interface. 
    /// </summary>
    public class AasxFileRepoHttpRestRepository : AasxFileRepoBase
    {
        //
        // Member
        //

        private PackageConnectorHttpRest _connector;

        /// <summary>
        /// REST endpoint of the AAS repository, that is, without <c>/server/listaas</c>
        /// </summary>
        [JsonIgnore]
        public Uri Endpoint;

        //
        // Constructor
        //

        public AasxFileRepoHttpRestRepository(string location)
        {
            // always have a location
            Endpoint = new Uri(location);

            // directly set endpoint
            _connector = new PackageConnectorHttpRest(null, Endpoint);
        }

        //
        // Outer funcs
        //

        private class ListAasItem
        {
            public string Index, AasIdShort, AasId, Fn;
        }

        /// <summary>
        /// This functions asks the AAS REST repository on given location, which AasIds would be availble.
        /// Using the AasIds, details are retrieved for each inidivudal AAS and synchronized with the 
        /// repository.
        /// Note: for the time being, the list of file items is recreated, but not synchronized
        /// Note: due to the nature of long-lasting actions, this is by design async!
        /// </summary>
        /// <returns>If a successfull retrieval could be made</returns>
        public async Task<bool> SyncronizeFromServerAsync()
        {
            // access
            if (true != _connector?.IsValid())
                return false;

            // try get a list of items from the connector
            var items = await _connector.GenerateRepositoryFromEndpointAsync();

            // just re-set
            FileMap.Clear();
            foreach (var fi in items)
                FileMap.Add(fi);

            // ok
            return true;
        }

        /// <summary>
        /// Retrieve the full location specification of the item w.r.t. to persistency container 
        /// (filesystem, HTTP, ..)
        /// </summary>
        /// <returns></returns>
        public override string GetFullItemLocation(AasxFileRepoItem fi)
        {
            // access
            if (fi?.Location == null)
                return null;

            // there is a good chance, that fi.Location is already absolute
            var ll = fi.Location.Trim().ToLower();
            if (ll.StartsWith("http://") || ll.StartsWith("https://"))
                return fi.Location;

            // TODO (MIHO, 2021-01-08): check, how to make absolute
            throw new NotImplementedException("AasxFileRepoHttpRestRepository.GetFullItemLocation()");
        }

    }
}
