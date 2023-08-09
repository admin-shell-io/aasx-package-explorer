/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;

namespace AasxIntegrationBase
{
    /// <summary>
    /// Implements a minimal stack for plugin events
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

#if __for_future_use_prepared_but_not_required_yet
        /// <summary>
        /// This feature allows any function to subscribe for the next return event.
        /// Precondition: the plugins needs to manually call <c>HandleEventReturn</c>.
        /// This action is removed automatically, after being called.
        /// </summary>
        public Action<AasxPluginEventReturnBase> SubscribeForNextEventReturn = null;

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            if (SubscribeForNextEventReturn != null)
            {
                // delete first
                var tempLambda = SubscribeForNextEventReturn;
                SubscribeForNextEventReturn = null;

                // execute
                tempLambda(evtReturn);
            }
        }
#endif
    }
}
