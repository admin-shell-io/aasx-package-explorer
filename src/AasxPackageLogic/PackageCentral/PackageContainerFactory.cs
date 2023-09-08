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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;

namespace AasxPackageLogic.PackageCentral
{
    public class PackageContainerGuess
    {
        /// <summary>
        /// The location which was fed into the guessing
        /// </summary>
        public string Location;

        /// <summary>
        /// The type of PackageContainer, which is suitable for the location
        /// </summary>
        public Type GuessedType;

        /// <summary>
        /// The portion of the path, which goes from start to the beginnning of "/aas"
        /// </summary>
        public string HeadOfPath;

        /// <summary>
        /// The parsed AAS id; either index or idShort.
        /// </summary>
        public string AasId;

        /// <summary>
        /// Guess the container type based on <c>location</c> and parse necessary arguments
        /// </summary>
        /// <param name="location"></param>
        /// <param name="runtimeOptions"></param>
        /// <returns></returns>
        public static PackageContainerGuess FromLocation(
            string location,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // access
            if (location == null)
                return null;
            var ll = location.ToLower();

            // Log?
            runtimeOptions?.Log?.Info($"Trying to guess package container for {location} ..");

            // starts with user file scheme
            if (ll.StartsWith(PackageContainerUserFile.Scheme))
            {
                return new PackageContainerGuess()
                {
                    Location = location.Substring(PackageContainerUserFile.Scheme.Length),
                    GuessedType = typeof(PackageContainerUserFile)
                };
            }

            // starts with http ?
            if (ll.StartsWith("http://") || ll.StartsWith("https://"))
            {
                // direct evidence of /getaasx/
                var match = Regex.Match(ll, @"^(.*)/server/getaasx/([^/])+(/|$)");
                if (match.Success && match.Groups.Count >= 3)
                {
                    // care for the aasx file
                    runtimeOptions?.Log?.Info($".. deciding for networked HHTP file ..");

                    string aasId = match.Groups[2].ToString().Trim();
                    var split = ll.Split('/');
                    if (split.Length >= 3)
                        aasId = split[split.Length - 1];
                    return new PackageContainerGuess()
                    {
                        Location = location,
                        GuessedType = typeof(PackageContainerNetworkHttpFile),
                        HeadOfPath = match.Groups[1].ToString().Trim(),
                        AasId = aasId
                    };
                }

                runtimeOptions?.Log?.Info($".. no adequate HTTP option found!");
            }

            // check FileInfo for (possible?) local file
            FileInfo fi = null;
            try
            {
                fi = new FileInfo(location);
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            // if file, try to open (might throw exceptions!)
            if (fi != null)
                // seems to be a valid (possible) local file
                return new PackageContainerGuess()
                {
                    Location = location,
                    GuessedType = typeof(PackageContainerLocalFile)
                };

            // no??
            runtimeOptions?.Log?.Info($".. no any possible option for package container found");
            return null;
        }
    }

    public static class PackageContainerFactory
    {
        public static PackageContainerBase GuessAndCreateFor(
            PackageCentral packageCentral,
            string location,
            string fullItemLocation,
            bool overrideLoadResident,
            PackageContainerBase takeOver = null,
            PackageContainerListBase containerList = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            var task = Task.Run(() => GuessAndCreateForAsync(
                packageCentral, location, fullItemLocation, overrideLoadResident,
                takeOver, containerList, containerOptions, runtimeOptions));
            return task.Result;
        }

        public static async Task<PackageContainerBase> GuessAndCreateForAsync(
            PackageCentral packageCentral,
            string location,
            string fullItemLocation,
            bool overrideLoadResident,
            PackageContainerBase takeOver = null,
            PackageContainerListBase containerList = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // access
            if (location == null)
                return null;
            var ll = location.ToLower();

            // guess
            runtimeOptions?.Log?.Info($"Trying to guess package container for {location} ..");
            var guess = PackageContainerGuess.FromLocation(location, runtimeOptions);
            if (guess == null)
            {
                runtimeOptions?.Log?.Info("Aborting");
                return null;
            }

            // start
            runtimeOptions?.Log?.Info($".. with containerOptions = {containerOptions?.ToString()}");

            // TODO (MIHO, 2021-02-01): check, if demo option is still required
            if (ll.Contains("/demo"))
            {
                return await Demo(packageCentral, location,
                    overrideLoadResident, containerOptions, runtimeOptions);
            }

            // starts with http ?
            if (guess.GuessedType == typeof(PackageContainerNetworkHttpFile))
            {
                var cnt = await PackageContainerNetworkHttpFile.CreateAndLoadAsync(
                            packageCentral, location, fullItemLocation,
                            overrideLoadResident, takeOver, containerList,
                            containerOptions, runtimeOptions);

                if (cnt.ContainerOptions.StayConnected
                    && guess.AasId.HasContent()
                    && guess.HeadOfPath.HasContent())
                {
                    cnt.ConnectorPrimary = new PackageConnectorHttpRest(cnt,
                        new Uri(guess.HeadOfPath + "/aas/" + guess.AasId));
                }

                return cnt;
            }

            if (guess.GuessedType == typeof(PackageContainerUserFile))
            {
                var cnt = await PackageContainerUserFile.CreateAndLoadAsync(
                            packageCentral, location, fullItemLocation,
                            overrideLoadResident, takeOver,
                            containerOptions, runtimeOptions);
                return cnt;
            }

            // check FileInfo for (possible?) local file
            FileInfo fi = null;
            try
            {
                fi = new FileInfo(location);
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            // if file, try to open (might throw exceptions!)
            if (fi != null)
                // seems to be a valid (possible) file
                return await PackageContainerLocalFile.CreateAndLoadAsync(
                    packageCentral, location, fullItemLocation, overrideLoadResident, takeOver, containerOptions);

            // no??
            runtimeOptions?.Log?.Info($".. no any possible option for package container found .. Aborting!");
            return null;
        }

        public static async Task<PackageContainerBase> Demo(
            PackageCentral packageCentral,
            string location,
            bool overrideLoadResident,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions ro = null)
        {
            // Log location
            ro?.Log?.Info($"Perform Demo() for location {location}");

            // ask for a list
            var li1 = new List<AnyUiDialogueListItem>();
            li1.Add(new AnyUiDialogueListItem("AAAAAAAAAAAAAAAAAAAAAAAAAAA", "A"));
            li1.Add(new AnyUiDialogueListItem("bbbbbbbbbbbb", "B"));
            li1.Add(new AnyUiDialogueListItem("CCCCCCCCCCCCCCCCCC  CCCCC", "C"));

            var waitLi1 = new TaskCompletionSource<AnyUiDialogueListItem>();
            ro?.AskForSelectFromList?.Invoke("Testselect", li1, waitLi1);
            var xx = await waitLi1.Task;
            ro?.Log?.Info($".. selected item is {"" + xx?.Text}");

            // ask for a list
            var li2 = new List<AnyUiDialogueListItem>();
            li2.Add(new AnyUiDialogueListItem("111111111", "A"));
            li2.Add(new AnyUiDialogueListItem("222222222222222222222222", "B"));
            li2.Add(new AnyUiDialogueListItem("3333333333333  3333", "C"));

            var waitLi2 = new TaskCompletionSource<AnyUiDialogueListItem>();
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
            return await PackageContainerNetworkHttpFile.CreateAndLoadAsync(
                packageCentral,
                "http://admin-shell-io.com:51310/server/getaasx/0",
                "http://admin-shell-io.com:51310/server/getaasx/0",
                // "http://localhost:51310/server/getaasx/0",
                overrideLoadResident, null, null, containerOptions, ro);
        }

    }

}
