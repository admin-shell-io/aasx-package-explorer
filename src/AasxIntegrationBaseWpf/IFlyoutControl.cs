using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxIntegrationBase
{
    public delegate void IFlyoutControlClosed();

    public interface IFlyoutControl
    {
        /// <summary>
        /// Event emitted by the Flyout in order to end the dialogue.
        /// </summary>
        event IFlyoutControlClosed ControlClosed;

        /// <summary>
        /// ´Called by the main window immediately after start
        /// </summary>
        void ControlStart();

        /// <summary>
        /// Called by main window, as soon as a keyboard input is avilable
        /// </summary>
        /// <param name="e"></param>
        void ControlPreviewKeyDown(KeyEventArgs e);
    }
}
