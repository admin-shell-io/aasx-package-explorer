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
using System.Threading;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

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

		private LogInstance _log = null;
		protected PluginEventStack _eventStack = null;

        private const int _timerTickMs = 200;
		private System.Timers.Timer _dispatcherTimer = null;

		private AidAllInterfaceStatus _allInterfaceStatus = null;

        public void StartOperation(
			LogInstance log, 
			PluginEventStack eventStack, 
			AidAllInterfaceStatus allInterfaceStatus)
		{
			_log = log;
			_eventStack = eventStack;
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

		private int _counter = 0;

		private async void DispatcherTimer_TickAsync(object sender, EventArgs e)
		{
			// access
			if (_allInterfaceStatus == null || _inDispatcherTimer)
				return;

            // block 
            _inDispatcherTimer = true;

#if hardcore_debug
			_counter++;
			if (_counter > 100)
			{
                _counter = 0;

				try
				{
					Console.WriteLine("Hello, World!");
					var client = new AasOpcUaClient("opc.tcp://MMT-HOMI2-N1:4840", true, "", "");
					await client.DirectConnect();
					Console.WriteLine("Running..");
					while (client.StatusCode == AasOpcUaClientStatus.Starting
						|| client.StatusCode == AasOpcUaClientStatus.Running)
					{
						Thread.Sleep(500);
						Console.Write(".");
						var nid = client.CreateNodeId(5, 2);
						var x = client.ReadNodeId(nid);
						Console.WriteLine("" + x);
					}
				} catch (Exception ex)
				{
					;
				}
			}
#endif

			// call cyclic tasks
			try
			{
                await _allInterfaceStatus.UpdateValuesContinousByTickAsyc();
			} catch (Exception ex)
			{
				;
			}

			// check if to send an event
			var potEvt = new AasxPluginResultEventPushSomeEvents();
			lock (_allInterfaceStatus.AnimatedSingleValueChange)
			{
				if (_allInterfaceStatus.AnimatedSingleValueChange != null
					&& _allInterfaceStatus.AnimatedSingleValueChange.Count >= 0)
				{
					potEvt.AnimateSingleEvents = new List<Aas.ISubmodelElement>();
					potEvt.AnimateSingleEvents.AddRange(_allInterfaceStatus.AnimatedSingleValueChange);
					_allInterfaceStatus.AnimatedSingleValueChange.Clear();
                }
			}
			if (potEvt.AnimateSingleEvents != null)
			{
				_eventStack?.PushEvent(potEvt);
			}

            // release mutex
            _inDispatcherTimer = false;
		}
	}
}
