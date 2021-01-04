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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AdminShellNS;

namespace AasxWpfControlLibrary.PackageCentral
{
    public static class PackageContainerFactory
    {
        public static PackageContainerBase GuessAndCreateFor(
            PackageCentral packageCentral,
            string location, bool loadResident,
            bool stayConnected = false,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            var task = Task.Run(() => GuessAndCreateForAsync(
                packageCentral, location, loadResident, stayConnected, runtimeOptions));
            return task.Result;
        }

        public async static Task<PackageContainerBase> GuessAndCreateForAsync(
            PackageCentral packageCentral,
            string location, bool loadResident,
            bool stayConnected = false,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // access
            if (location == null)
                return null;
            var ll = location.ToLower();

            // Log?
            runtimeOptions?.Log?.Info($"Trying to guess package container for {location} ..");
            runtimeOptions?.Log?.Info($".. with loadResident = {loadResident}");

            // starts with http ?
            if (ll.StartsWith("http://") || ll.StartsWith("https://"))
            {
                // direct evidence of /getaasx/
                var match = Regex.Match(ll, @"^(.*)/server/getaasx/([^/])(/|$)");
                if (match.Success && match.Groups.Count >= 3)
                {
                    // care for the aasx file
                    runtimeOptions?.Log?.Info($".. deciding for networked HHTP file ..");
                    var cnt = await PackageContainerNetworkHttpFile.CreateAsync(
                                            packageCentral, location, loadResident, runtimeOptions);

                    // create an online connection?
                    var aasId = match.Groups[2].ToString().Trim();
                    var aasEndpoint = match.Groups[1].ToString().Trim() + "/aas/" + aasId;
                    if (stayConnected && aasId.HasContent())
                    {
                        cnt.ConnectorPrimary = new PackageConnectorHttpRest(cnt, new Uri(aasEndpoint));
                    }

                    // done
                    return cnt;
                }

                if (ll.Contains("/demo"))
                {
                    return await Demo(packageCentral, location, loadResident, runtimeOptions);
                }

                runtimeOptions?.Log?.Info($".. no adequate HTTP option found!");
            }

            // check FileInfo for (possible?) local file
            FileInfo fi = null;
            try
            {
                fi = new FileInfo(location);
            }
            catch { }

            // if file, try to open (might throw exceptions!)
            if (fi != null)
                // seems to be a valid (possible) file
                return new PackageContainerLocalFile(packageCentral, location, loadResident);

            // no??
            runtimeOptions?.Log?.Info($".. no any possible option for package container found .. Aborting!");
            return null;
        }

        public async static Task<PackageContainerBase> Demo(
            PackageCentral packageCentral,
            string location, bool loadResident,
            PackageContainerRuntimeOptions ro = null)
        {
            // Log location
            ro?.Log?.Info($"Perform Demo() for location {location}");

            // ask for a list
            var li1 = new List<SelectFromListFlyoutItem>();
            li1.Add(new SelectFromListFlyoutItem("AAAAAAAAAAAAAAAAAAAAAAAAAAA", "A"));
            li1.Add(new SelectFromListFlyoutItem("bbbbbbbbbbbb", "B"));
            li1.Add(new SelectFromListFlyoutItem("CCCCCCCCCCCCCCCCCC  CCCCC", "C"));

            var waitLi1 = new TaskCompletionSource<SelectFromListFlyoutItem>();
            ro?.AskForSelectFromList?.Invoke("Testselect", li1, waitLi1);
            var xx = await waitLi1.Task;
            ro?.Log?.Info($".. selected item is {"" + xx?.Text}");

            // ask for a list
            var li2 = new List<SelectFromListFlyoutItem>();
            li2.Add(new SelectFromListFlyoutItem("111111111", "A"));
            li2.Add(new SelectFromListFlyoutItem("222222222222222222222222", "B"));
            li2.Add(new SelectFromListFlyoutItem("3333333333333  3333", "C"));

            var waitLi2 = new TaskCompletionSource<SelectFromListFlyoutItem>();
            ro?.AskForSelectFromList?.Invoke("Testselect", li2, waitLi2);
            var xy = await waitLi2.Task;
            ro?.Log?.Info($".. selected item is {"" + xy?.Text}");

            // ask for credentials
            var waitCre = new TaskCompletionSource<PackageContainerCredentials>();
            ro?.AskForCredentials?.Invoke("Fill user credentials", waitCre);
            var xz = await waitCre.Task;
            ro?.Log?.Info($".. credentials are {"" + xz?.Username} and {"" + xz?.Password}");

            // debug some important blocks of text
            ro?.Log?.Info(StoredPrint.Color.Yellow, "Showing fingerprint:");
            var sum = "";
            for (int i = 0; i < 1000; i++)
                sum += $"{i} ";
            ro?.Log?.Info($"Showing fingerprint: {sum}");

            // done
            ro?.Log?.Info($".. demo loading from internet ..");
            return await PackageContainerNetworkHttpFile.CreateAsync(
                packageCentral,
                "http://admin-shell-io.com:51310/server/getaasx/0", 
                // "http://localhost:51310/server/getaasx/0",
                loadResident, ro);
        }
    }
}
