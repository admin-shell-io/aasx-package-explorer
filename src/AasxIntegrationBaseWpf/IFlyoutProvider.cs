using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxIntegrationBase
{
    public interface IFlyoutProvider
    {
        /// <summary>
        /// Returns true, if already an flyout is shown
        /// </summary>
        /// <returns></returns>
        bool IsInFlyout();

        /// <summary>
        /// Starts an Flyout based on an instantiated UserControl. The UserControl has to implement
        /// the interface IFlyoutControl
        /// </summary>
        /// <param name="uc"></param>
        void StartFlyover(UserControl uc);

        /// <summary>
        /// Initiate closing an existing flyout
        /// </summary>
        void CloseFlyover();

        /// <summary>
        /// Start UserControl as modal flyout. The UserControl has to implement
        /// the interface IFlyoutControl
        /// </summary>
        /// <param name="uc"></param>
        void StartFlyoverModal(UserControl uc, Action closingAction = null);

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        MessageBoxResult MessageBoxFlyoutShow(string message, string caption, MessageBoxButton buttons, MessageBoxImage image);

        /// <summary>
        /// Returns the window for advanced modal dialogues
        /// </summary>
        Window GetWin32Window();
    }
}
