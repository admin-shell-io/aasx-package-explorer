/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase
{
    /// <summary>
    /// Implements a minimal stack for
    /// </summary>
    public class PluginEventStack
    {
        private List<AasxPluginResultEventBase> eventStack = new List<AasxPluginResultEventBase>();

        public void PushEvent(AasxPluginResultEventBase evt)
        {
            if (evt == null || this.eventStack == null)
                return;
            lock (this.eventStack)
            {
                this.eventStack.Add(evt);
            }
        }

        public AasxPluginResultEventBase PopEvent()
        {
            // result
            AasxPluginResultEventBase evt = null;

            // get?
            lock (this.eventStack)
            {
                if (this.eventStack.Count > 0)
                {
                    evt = this.eventStack[0];
                    this.eventStack.RemoveAt(0);
                }
            }

            // return if found or not ..
            return evt;
        }
    }
}
