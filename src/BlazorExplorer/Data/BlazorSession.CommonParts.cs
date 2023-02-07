/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using BlazorExplorer;
using BlazorExplorer.Shared;
using Microsoft.JSInterop;

namespace BlazorUI.Data
{
    /// <summary>
    /// This partial class holds parts which seem to be same with Package Explorer
    /// </summary>
    public partial class BlazorSession : IDisposable
    {
        /// <summary>
        /// Check for menu switch and flush events, if required.
        /// </summary>
        public void CheckIfToFlushEvents()
		{
			if (MainMenu?.IsChecked("CompressEvents") == true)
			{
				var evs = _eventCompressor?.Flush();
				if (evs != null)
					foreach (var ev in evs)
						PackageCentral?.PushEvent(ev);
			}
		}

        public void CommandExecution_RedrawAll()
        {
            // redraw everything
            RedrawAllAasxElements();
            RedrawElementView();
        }
    }

}
