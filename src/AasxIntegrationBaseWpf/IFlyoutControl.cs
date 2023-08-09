/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AnyUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AasxIntegrationBase
{
    public delegate void IFlyoutControlAction();

    /// <summary>
    /// Marks an user control, which is superimposed on top of the application
    /// </summary>
    public interface IFlyoutControl
    {
        /// <summary>
        /// Event emitted by the Flyout in order to end the dialogue.
        /// </summary>
        event IFlyoutControlAction ControlClosed;

        /// <summary>
        /// Called by the main window immediately after start
        /// </summary>
        void ControlStart();

        /// <summary>
        /// Called by main window, as soon as a keyboard input is avilable
        /// </summary>
        /// <param name="e"></param>
        void ControlPreviewKeyDown(KeyEventArgs e);

        /// <summary>
        /// Called by the main window immediately to hand over a selected range
        /// of lambda actions.
        /// </summary>
        void LambdaActionAvailable(AnyUiLambdaActionBase la);
    }
}
