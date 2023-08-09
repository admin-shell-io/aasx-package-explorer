/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;

namespace AasxIntegrationBaseGdi
{
    public static class AnyUiTimerHelper
    {
        public static object CreatePluginTimer(int intervalMs, EventHandler eventHandler)
        {
            {
                // Note: this timer shall work for all sorts of applications?
                // see: https://stackoverflow.com/questions/21041299/c-sharp-dispatchertimer-in-dll-application-never-triggered
                var _timer2 = new System.Timers.Timer(intervalMs);
                _timer2.Elapsed += (s, e) => eventHandler?.Invoke(s, e);
                _timer2.Enabled = true;
                _timer2.Start();
                return _timer2;
            }
        }
    }
}
