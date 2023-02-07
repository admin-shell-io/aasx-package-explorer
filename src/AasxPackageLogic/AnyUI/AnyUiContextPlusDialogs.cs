/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AnyUi;
using AasxIntegrationBase;

namespace AnyUi
{
    /// <summary>
    /// This class extends the base context with more functions for handling 
    /// convenience dialogues.
    /// </summary>
    public class AnyUiContextPlusDialogs : AnyUiContextBase
    {
        private string lastFnForInitialDirectory = null;
        public void RememberForInitialDirectory(string fn)
        {
            this.lastFnForInitialDirectory = fn;
        }

        /// <summary>
        /// Selects a filename to read either from user or from ticket.
        /// </summary>
        /// <returns>The dialog data containing the filename or <c>null</c></returns>
        public virtual async Task<AnyUiDialogueDataOpenFile> MenuSelectOpenFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            await Task.Yield();
            return null;
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public virtual async Task<bool> MenuSelectOpenFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// Selects a filename to write either from user or from ticket.
        /// </summary>
        /// <returns>The dialog data containing the filename or <c>null</c></returns>
        public virtual async Task<AnyUiDialogueDataSaveFile> MenuSelectSaveFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            await Task.Yield();
            return null;
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public virtual async Task<bool> MenuSelectSaveFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            string argFilterIndex = null)
        {
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public virtual async Task<AnyUiDialogueDataTextBox> MenuSelectTextAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            await Task.Yield();
            return null;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public virtual async Task<bool> MenuSelectTextToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// The display context tells, if user files are allowable for the application
        /// </summary>
        public virtual bool UserFilesAllowed()
        {
            return false;
        }

        /// <summary>
        /// The display context tells, if web browser services are allowable for the application.
        /// These are: download files, open new browser window ..
        /// </summary>
        public virtual bool WebBrowserServicesAllowed()
        {
            return false;
        }

        /// <summary>
        /// Initiate a download within the web browser
        /// </summary>
        public virtual async Task WebBrowserDisplayOrDownloadFile(string fn, string mimeType = null)
        {
            await Task.Yield();
            return;
        }
    }
}
