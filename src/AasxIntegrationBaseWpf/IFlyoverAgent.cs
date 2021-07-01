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
        /// The FlyoutStrady can decide to handle events from the outside
        /// </summary>
        void PushEvent(AdminShellEvents.AasEventMsgEnvelope ev);

        /// <summary>
        /// Event emitted by the Flyout in order to transmit an AAS event.
        /// </summary>
        event IFlyoutAgentPushAasEvent EventTriggered;
    }
}
