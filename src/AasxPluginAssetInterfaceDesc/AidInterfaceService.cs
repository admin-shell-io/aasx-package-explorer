/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBaseGdi;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginAssetInterfaceDescription
{
	/// <summary>
	/// This is thought as ONE service per ONE instance of AASX PE / BlazorExplorer
	/// to care for all (continous) interfacing.
	/// </summary>
	public class AidInterfaceService
    {
		//
		// Start of service
		//

		private LogInstance _log = new LogInstance();

		private const int _timerTickMs = 200;
		private System.Timers.Timer _dispatcherTimer = null;

		private AidAllInterfaceStatus _allInterfaceStatus = null;

        public void StartOperation(LogInstance log, AidAllInterfaceStatus allInterfaceStatus)
		{
			_log = log;
			_allInterfaceStatus = allInterfaceStatus;

			_dispatcherTimer = new System.Timers.Timer(_timerTickMs);
			_dispatcherTimer.Elapsed += DispatcherTimer_TickAsync;
			_dispatcherTimer.Enabled = true;
			_dispatcherTimer.Start();
		}

		//
		// Service
		//

		private bool _inDispatcherTimer = false;

		private async void DispatcherTimer_TickAsync(object sender, EventArgs e)
		{
			// access
			if (_allInterfaceStatus == null || _inDispatcherTimer)
				return;

			// block 
			_inDispatcherTimer = true;

            // call cyclic tasks
			try
			{
                // synchronous
                await _allInterfaceStatus.UpdateValuesContinousByTickAsyc();
			} catch (Exception ex)
			{
				;
			}

			// release mutex
			_inDispatcherTimer = false;
		}
	}
}
