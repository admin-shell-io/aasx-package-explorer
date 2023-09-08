/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AnyUi;
using System;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This class contains is the base class of abstracting main window functions
    /// over different UIs such WPF/ Blazor.
    /// </summary>
    public class MainWindowLogic
    {
        /// <summary>
        /// This instance of <c>PackageCentral</c> shall be accessile for 
        /// all abstracted functionality. UI-Windows may link to this.
        /// </summary>
        public PackageCentral PackageCentral = new PackageCentral();

        /// <summary>
        /// Will be initialized by the specific main window providing
        /// access to UI-specific functions like modal dialogs or
        /// clipboard handling.
        /// </summary>
        public AnyUiContextBase DisplayContext = null;

        /// <summary>
        /// Via this interface, important state changes for the user
        /// can be triggered, e.g. loading.
        /// </summary>
        public IMainWindow MainWindow = null;

        /// <summary>
        /// Currently edited script text. Survives multiple start of dialog.
        /// </summary>
        protected string _currentScriptText = "";

        /// <summary>
        /// Script engine
        /// </summary>
        protected AasxScript _aasxScript = null;

        /// <summary>
        /// If in scriptmode, set ticket result and exception to error.
        /// Add also to log.
        /// </summary>
        public void LogErrorToTicket(
            AasxMenuActionTicket ticket,
            string message)
        {
            if (ticket != null)
            {
                ticket.Exception = message;
                ticket.Success = false;
            }

            Log.Singleton.Error(message);
        }

        /// <summary>
        /// Only in scriptmode, set ticket result and exception to error.
        /// Add also to log.
        /// Do nothing, if not in scriptmode
        /// </summary>
        public static void LogErrorToTicketOrSilentStatic(
            AasxMenuActionTicket ticket,
            string message)
        {
            if (ticket?.ScriptMode != true)
                return;

            ticket.Exception = message;
            ticket.Success = false;

            Log.Singleton.Error(message);
        }

        public static void LogErrorToTicketStatic(
            AasxMenuActionTicket ticket,
            Exception ex,
            string where)
        {
            if (ticket != null)
            {
                ticket.Exception = $"Error {ex?.Message} in {where}.";
                ticket.Success = false;
            }

            Log.Singleton.Error(ex, where);
        }

        /// <summary>
        /// Only in scriptmode, set ticket result and exception to error.
        /// Add also to log.
        /// Do nothing, if not in scriptmode
        /// </summary>
        public void LogErrorToTicketOrSilent(
            AasxMenuActionTicket ticket,
            string message)
        {
            MainWindowLogic.LogErrorToTicketOrSilentStatic(ticket, message);
        }

        public void LogErrorToTicket(
            AasxMenuActionTicket ticket,
            Exception ex,
            string where)
        {
            MainWindowLogic.LogErrorToTicketStatic(ticket, ex, where);
        }

        //
        // Scripting
        //

        public void StartScriptFile(string fn, AasxMenu menu, IAasxScriptRemoteInterface remote)
        {
            if (_aasxScript == null)
                _aasxScript = new AasxScript();
            var script = System.IO.File.ReadAllText(Options.Curr.ScriptFn);
            _aasxScript.StartEnginBackground(
                script, Options.Curr.ScriptLoglevel,
                menu, remote);
        }

        public void StartScriptCommand(string command, AasxMenu menu, IAasxScriptRemoteInterface remote)
        {
            if (_aasxScript == null)
                if (_aasxScript == null)
                    _aasxScript = new AasxScript();
            _aasxScript.StartEnginBackground(
                command, Options.Curr.ScriptLoglevel,
                menu, remote);
        }

    }
}