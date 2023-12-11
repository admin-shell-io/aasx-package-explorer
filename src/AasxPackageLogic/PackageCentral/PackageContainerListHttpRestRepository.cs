/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// AasxFileRepository, which is held synchronized with a AAS REST repository interface. 
    /// Just a deriative from <c>PackageContainerListBase</c>. Only small additions.
    /// </summary>
    public class PackageContainerListHttpRestRepository : PackageContainerListHttpRestBase
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

        public bool IsAspNetConnection { get; set; }

        //
        // Constructor
        //

        public PackageContainerListHttpRestRepository(string location)
        {
            // always have a location
            Endpoint = new Uri(location);

            // directly set endpoint
            _connector = new PackageConnectorHttpRest(null, Endpoint);


        }

        //
        // Outer funcs
        //

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
            if (true != _connector?.IsValid())
                return false;
            // try get a list of items from the connector
            var items = await _connector.GenerateRepositoryFromEndpointAsync();
            // just re-set
            FileMap.Clear();
            foreach (var fi in items)
                if (fi != null)
                {
                    FileMap.Add(fi);
                    fi.ContainerList = this;
                }

            // ok
            return true;
        }

        /// <summary>
        /// Retrieve the full location specification of the item w.r.t. to persistency container 
        /// (filesystem, HTTP, ..)
        /// </summary>
        /// <returns></returns>
        public override string GetFullItemLocation(string location)
        {
            // access
            if (location == null)
                return null;

            // there is a good chance, that fi.Location is already absolute
            var ll = location.Trim().ToLower();
            if (ll.StartsWith("http://") || ll.StartsWith("https://"))
                return location;

            // TODO (MIHO, 2021-01-08): check, how to make absolute
            throw new NotImplementedException("AasxFileRepoHttpRestRepository.GetFullItemLocation()");
        }

    }
}
