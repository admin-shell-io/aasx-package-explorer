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
using System.Windows;
using System.Windows.Controls;
using AnyUi;

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
        void CloseFlyover(bool threadSafe = false);

        /// <summary>
        /// Start UserControl as modal flyout. The UserControl has to implement
        /// the interface IFlyoutControl
        /// </summary>
        void StartFlyoverModal(UserControl uc, Action closingAction = null);

        /// <summary>
        /// Start UserControl as modal flyout. The UserControl has to implement
        /// the interface IFlyoutControl
        /// </summary>
        Task StartFlyoverModalAsync(UserControl uc, Action closingAction = null);

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        AnyUiMessageBoxResult MessageBoxFlyoutShow(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image);

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        Task<AnyUiMessageBoxResult> MessageBoxFlyoutShowAsync(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image);

        /// <summary>
        /// Returns the window for advanced modal dialogues
        /// </summary>
        Window GetWin32Window();

        /// <summary>
        /// Gets the display context, e.g. to use UI-abstracted forms of dialogues
        /// </summary>
        AnyUiContextBase GetDisplayContext();
    }
}
