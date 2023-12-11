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
    public class PackageContainerOptionsBase
    {
        /// <summary>
        /// If true, then PackageContainer will try to automatically load the contents of the package
        /// on application level.
        /// </summary>
        public bool LoadResident;

        /// <summary>
        /// If true, the container will try to stay connected (that is, receive AAS events) via an
        /// appropriate connector.
        /// </summary>
        public bool StayConnected;

        /// <summary>
        /// If staying connected, the desired period of update value events in [ms].
        /// Zero means off.
        /// </summary>
        public int UpdatePeriod;

        //
        // Constructor
        //

        public static PackageContainerOptionsBase CreateDefault(
            OptionsInformation opts,
            bool loadResident = false)
        {
            // first act
            var res = new PackageContainerOptionsBase();
            res.LoadResident = loadResident;
            if (opts == null)
                return res;

            // now take some user options, as well
            res.StayConnected = opts.DefaultStayConnected;
            res.UpdatePeriod = Math.Max(OptionsInformation.MinimumUpdatePeriod, opts.DefaultUpdatePeriod);

            // ok
            return res;
        }

        //
        // Serialization
        //

        public override string ToString()
        {
            return $"LoadResident={LoadResident} StayConnected={StayConnected} UpdatePeriod={UpdatePeriod}";
        }

    }
}
