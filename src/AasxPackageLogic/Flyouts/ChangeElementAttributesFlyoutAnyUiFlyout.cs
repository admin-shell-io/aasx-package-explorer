/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;

namespace AasxPackageLogic
{
    public static class ChangeElementAttributesFlyoutAnyUiFlyout
	{
        public static AnyUiDialogueDataModalPanel CreateModelDialogue(
			AnyUiDialogueDataChangeElementAttributes innerDiaData)
        {
            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel(innerDiaData.Caption);
            uc.ActivateRenderPanel(innerDiaData,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(5, 4, new[] { "220:", "3*", "100:", "1*" },
                                padding: new AnyUiThickness(0, 5, 0, 5));
                    g.RowDefinitions[1].MinHeight = 16.0;
					g.RowDefinitions[3].MinHeight = 16.0;
					panel.Add(g);

                    // Row 0 : Attribute and language
                    helper.AddSmallLabelTo(g, 0, 0, content: "Attribute:", verticalCenter: true);
					AnyUiUIElement.SetIntFromControl(
						helper.Set(
							helper.AddSmallComboBoxTo(g, 0, 1,
								items: AnyUiDialogueDataChangeElementAttributes.AttributeNames,
								selectedIndex: (int)innerDiaData.AttributeToChange),
							minWidth: 400),
						(i) => { innerDiaData.AttributeToChange = (AnyUiDialogueDataChangeElementAttributes.AttributeEnum)i; });

					helper.AddSmallLabelTo(g, 0, 2, content: "Language:", verticalCenter: true);
                    AnyUiUIElement.SetStringFromControl(
						helper.Set(
							helper.AddSmallComboBoxTo(g, 0, 3,
								text: innerDiaData.AttributeLang,
								items: AasxLanguageHelper.GetLangCodes().ToArray(),
								isEditable: true),
							minWidth: 200),
						(s) => { innerDiaData.AttributeLang = s; });

					// Row 2 : Change pattern
					helper.AddSmallLabelTo(g, 2, 0, content: "Change pattern:", verticalCenter: true);
					AnyUiUIElement.SetStringFromControl(
                        helper.Set(
						    helper.AddSmallComboBoxTo(g, 2, 1,
							    text: innerDiaData.Pattern,
							    items: AnyUiDialogueDataChangeElementAttributes.PatternPresets,
							    isEditable: true),
							minWidth: 600,
                            colSpan: 3),
						(s) => { innerDiaData.Pattern = s; });

					// Row 4 : Help
					helper.AddSmallLabelTo(g, 4, 0, content: "Help:");
					helper.Set(
						helper.AddSmallLabelTo(g, 4, 1,
							margin: new AnyUiThickness(0, 2, 2, 2),
							content: string.Join(System.Environment.NewLine, AnyUiDialogueDataChangeElementAttributes.HelpLines),
							verticalAlignment: AnyUiVerticalAlignment.Top,
							verticalContentAlignment: AnyUiVerticalAlignment.Top,
							fontSize: 0.7,
							wrapping: AnyUiTextWrapping.Wrap),
						colSpan: 3,
                        minHeight: 100,
						horizontalAlignment: AnyUiHorizontalAlignment.Stretch);

                    // give back
                    return panel;
                });
            return uc;
        }
    }
}
