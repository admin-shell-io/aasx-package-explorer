/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using BlazorExplorer;
using Microsoft.JSInterop;

namespace BlazorUI.Data
{
    /// <summary>
    /// This partial class holds the bindings to the menu commands of a session.
    /// </summary>
    public partial class BlazorSession : IDisposable
    {
        private async Task CommandBinding_GeneralDispatch(
                    string cmd,
                    AasxMenuItemBase menuItem,
                    AasxMenuActionTicket ticket)
        {
            //
            // Start
            //

            if (cmd == null || ticket == null)
                return;

            // change of edit field display

            if (cmd == "editmenu" || cmd == "editkey"
                || cmd == "hintsmenu" || cmd == "hintskey"
                || cmd == "showirimenu" || cmd == "showirikey")
            {
                // start
                ticket.StartExec();

                if (ticket.ScriptMode && cmd == "editmenu" && ticket["Mode"] is bool editMode)
                {
                    MainMenu?.SetChecked("EditMenu", editMode);
                }

                if (ticket.ScriptMode && cmd == "hintsmenu" && ticket["Mode"] is bool hintsMode)
                {
                    MainMenu?.SetChecked("HintsMenu", hintsMode);
                }

                // try to remember current selected data object
                object currMdo = null;
                if (DisplayElements.SelectedItem != null)
                    currMdo = DisplayElements.SelectedItem.GetMainDataObject();

                // in session, set these all important settings
                this.EditMode = MainMenu?.IsChecked("EditMenu") == true;
                this.HintMode = MainMenu?.IsChecked("HintMenu") == true;

                // edit mode affects the total element view
                RedrawAllAasxElements();

                // signalNewData should be sufficient:
                // this.StateHasChanged(); 
                Program.signalNewData(Program.DataRedrawMode.RebuildTreeKeepOpen, this.SessionId);

                // fake selection
                // RedrawElementView();
                // select last object
                if (currMdo != null)
                {
                    DisplayElements.TrySelectMainDataObject(currMdo, wishExpanded: true);
                }
            }
        }
    }
}
