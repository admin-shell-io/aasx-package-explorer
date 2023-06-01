/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasxIntegrationBase;
using AasxPackageLogic;
using AnyUi;
using Extensions;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Webpki.JsonCanonicalizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This menu function 
    /// </summary>
    public class MenuFuncValidateSmt
    {
        //
        // Valdation and more
        //

        public async Task PerformDialogue(
                    string cmd,
                    AasxMenuItemBase menuItem,
                    AasxMenuActionTicket ticket,
                    AnyUiContextBase displayContext)
        {
            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel("Assess Submodel template ..");
            uc.ActivateRenderPanel(this,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(4, 2, new[] { "100:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(g);
                    g.RowDefinitions[0].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

                    // Row 1 : limiting of values im UML
                    helper.AddSmallLabelTo(g, 0, 0, content: "Limit values:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    // Output view
                    var tb = helper.AddSmallTextBoxTo(g, 0, 1);
                    tb.MultiLine = true;

                    // give back
                    return panel;
                });

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return;

        }
    }
}
