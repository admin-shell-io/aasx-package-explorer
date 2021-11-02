/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// Implements a minimal stack for
    /// </summary>
    public class AasEventMsgStack
    {
        private List<AasEventMsgEnvelope> eventStack = new List<AasEventMsgEnvelope>();

        public void PushEvent(AasEventMsgEnvelope ev)
        {
            if (ev == null || this.eventStack == null)
                return;
            lock (this.eventStack)
            {
                this.eventStack.Add(ev);
            }
        }

        public AasEventMsgEnvelope PopEvent()
        {
            // result
            AasEventMsgEnvelope ev = null;

            // get?
            lock (this.eventStack)
            {
                if (this.eventStack.Count > 0)
                {
                    ev = this.eventStack[0];
                    this.eventStack.RemoveAt(0);
                }
            }

            // return if found or not ..
            return ev;
        }
    }
}
