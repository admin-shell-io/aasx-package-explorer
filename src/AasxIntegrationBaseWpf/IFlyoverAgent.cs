/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
    public delegate void IFlyoutAgentPushAasEvent(AdminShellEvents.AasEventMsgEnvelope ev);

    /// <summary>
    /// Marks a Flyout-Usercontrol, which can be "minimized" to run in parallel to the main window
    /// and is therefor a selfstanding agent.
    /// </summary>
    public interface IFlyoutAgent : IFlyoutControl
    {
        /// <summary>
        /// Event emitted by the Flyout in order to minimize the dialogue.
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global
        event IFlyoutControlAction ControlMinimize;

        /// <summary>
        /// The the (independent) model for the selfstanding functionality
        /// </summary>
        FlyoutAgentBase GetAgent();
    }

    /// <summary>
    /// Marks a user control which is a visual of the minimized agent
    /// </summary>
    public interface IFlyoutMini
    {
        /// <summary>
        /// The the (independent) model for the selfstanding functionality
        /// </summary>
        FlyoutAgentBase GetAgent();
    }

    public class FlyoutAgentBase
    {
        /// <summary>
        /// Event emitted by the Flyout in order to transmit an AAS event.
        /// </summary>
        public event IFlyoutAgentPushAasEvent EventTriggered;

        /// <summary>
        /// The Flyout can decide to handle events from the outside
        /// </summary>
        public virtual void PushEvent(AdminShellEvents.AasEventMsgEnvelope ev)
        {
            // default behavior: trigger event
            EventTriggered?.Invoke(ev);
        }

        /// <summary>
        /// If the Flyout is executed as Agent, minimized and then closed, the closing
        /// action needs to be retained.
        /// </summary>
        /// 
        // ReSharper disable once UnassignedField.Global
        public Action ClosingAction;

        /// <summary>
        /// If minimize button is triggered, this function will generate a <c>FlyoutMini</c>,
        /// which is visual for the selfstanding agent-
        /// </summary>
        public Func<IFlyoutMini> GenerateFlyoutMini;
    }
}
