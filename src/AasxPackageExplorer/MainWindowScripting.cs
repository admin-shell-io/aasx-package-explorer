/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using AasxDictionaryImport;
using AasxIntegrationBase;
using AasxIntegrationBaseWpf;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxPackageLogic.PackageCentral.AasxFileServerInterface;
using AasxSignature;
using AasxUANodesetImExport;
using AdminShellNS;
using AnyUi;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains the necessary scripting support for the UI application
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider, IAasxScriptRemoteInterface
    {
        protected string _currentScriptText = "";
        protected AasxScript _aasxScript = null;

        public void CommandBinding_ScriptEditLaunch(string cmd, AasxMenuItemBase menuItem)
        {
            if (cmd == "scripteditlaunch")
            {
                // trivial things
                if (!_packageCentral.MainAvailable)
                {
                    MessageBoxFlyoutShow(
                        "An AASX package needs to be available", "Error"
                        , AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                    return;
                }

                // trivial things
                if (_aasxScript?.IsExecuting == true)
                {
                    if (AnyUiMessageBoxResult.No == MessageBoxFlyoutShow(
                        "An AASX script is already executed! Continue anyway?", "Warning"
                        , AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                        return;
                    else
                        // brutal
                        _aasxScript = null;
                }

                // prompt for the script
                var uc = new TextEditorFlyout();
                uc.DiaData.MimeType = "application/csharp";
                uc.DiaData.Caption = "Edit script to be launched ..";
                uc.DiaData.Presets = Options.Curr.ScriptPresets;
                uc.DiaData.Text = _currentScriptText;

                // context menu
                uc.ContextMenuCreate = () =>
                {
                    var cm = DynamicContextMenu.CreateNew();
                    cm.Add(new DynamicContextItem(
                        "CLIP", "\U0001F4CB", "Copy JSON to clipboard"));
                    return cm;
                };

                uc.ContextMenuAction = (tag, obj) =>
                {
                    if (tag == "CLIP")
                    {
                        var text = uc.DiaData.Text;
                        var lines = text?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var sb = new StringBuilder();
                        sb.AppendLine("[");
                        if (lines != null)
                            foreach (var ln in lines)
                            {
                                var ln2 = ln.Replace("\"", "\\\"");
                                ln2 = ln2.Replace("\t", "    ");
                                sb.AppendLine($"\"{ln2}\",");
                            }
                        sb.AppendLine("]");
                        var jsonStr = sb.ToString();
                        System.Windows.Clipboard.SetText(jsonStr);
                        Log.Singleton.Info("Copied JSON to clipboard.");
                    }
                };
                
                // execute
                this.StartFlyoverModal(uc);
                _currentScriptText = uc.DiaData.Text;
                if (uc.DiaData.Result && uc.DiaData.Text.HasContent())
                {
                    try
                    {
                        // create first
                        if (_aasxScript == null)
                            _aasxScript = new AasxScript();

                        // executing
                        _aasxScript.StartEnginBackground(
                            uc.DiaData.Text, Options.Curr.ScriptLoglevel,
                            _mainMenu?.Menu, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "when executing script");
                    }
                }
            }

            for (int i=0;i<9; i++)
                if (cmd == $"launchscript{i}" 
                    && Options.Curr.ScriptPresets != null)
                {
                    // order in human sense
                    var scriptIndex = (i == 0) ? 9 : (i - 1);
                    if (scriptIndex >= Options.Curr.ScriptPresets.Count
                        || Options.Curr.ScriptPresets[scriptIndex]?.Text?.HasContent() != true)
                        return;

                    // still running?
                    if (_aasxScript?.IsExecuting == true)
                    {
                        if (AnyUiMessageBoxResult.No == MessageBoxFlyoutShow(
                            "An AASX script is already executed! Continue anyway?", "Warning"
                            , AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                            return;
                        else
                            // brutal
                            _aasxScript = null;
                    }

                    // prompting
                    if (!Options.Curr.ScriptLaunchWithoutPrompt)
                    {
                        if (AnyUiMessageBoxResult.Yes != MessageBoxFlyoutShow(
                            $"Executing script preset #{1 + scriptIndex} " +
                            $"'{Options.Curr.ScriptPresets[scriptIndex].Name}'. \nContinue?", 
                            "Question", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                            return;
                    }

                    // execute
                    try
                    {
                        // create first
                        if (_aasxScript == null)
                            _aasxScript = new AasxScript();

                        // executing
                        _aasxScript.StartEnginBackground(
                            Options.Curr.ScriptPresets[scriptIndex].Text, Options.Curr.ScriptLoglevel,
                            _mainMenu?.Menu, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "when executing script");
                    }
                }
        }


        public enum ScriptSelectRefType { None = 0, This, AAS, SM, SME, CD };
        protected static AdminShell.Referable[] _allowedSelectRefType = {
            new AdminShell.AdministrationShell(),
            new AdminShell.Submodel(),
            new AdminShell.SubmodelElement(),
            new AdminShell.ConceptDescription()
        };

        public enum ScriptSelectAdressMode { None = 0, First, Next, Prev, idShort, semanticId };
        protected static string[] _allowedSelectAdressMode = {
            "First", "Next", "Prev", "idShort", "semanticId"
        };

        protected Tuple<AdminShell.Referable, object> SelectEvalObject(
            ScriptSelectRefType refType, ScriptSelectAdressMode adrMode)
        {
            //
            // Try gather some selection states
            //

            // something to select
            var pm = _packageCentral?.Main?.AasEnv;
            if (pm == null)
                if (adrMode == ScriptSelectAdressMode.None)
                {
                    Log.Singleton.Error("Script: Select: No main package environment available!");
                    return null;
                }

            // available elements in the environment
            var firstAas = pm.AdministrationShells.FirstOrDefault();
            
            AdminShell.Submodel firstSm = null;
            if (firstAas != null && firstAas.submodelRefs != null && firstAas.submodelRefs.Count > 0)
                firstSm = pm.FindSubmodel(firstAas.submodelRefs[0]);

            AdminShell.SubmodelElement firstSme = null;
            if (firstSm != null && firstSm.submodelElements != null && firstSm.submodelElements.Count > 0)
                firstSme = firstSm.submodelElements[0]?.submodelElement;

            // selected items by user
            var siThis = DisplayElements.SelectedItem;
            var siSME = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementSubmodelElement, includeThis: true);
            var siSM = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementSubmodelRef, includeThis: true) as VisualElementSubmodelRef;
            var siAAS = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementAdminShell, includeThis: true) as VisualElementAdminShell;
            var siCD = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementConceptDescription, includeThis: true);

            //
            // This
            //

            if (refType == ScriptSelectRefType.This)
            {
                // just return as Referable
                return new Tuple<AdminShell.Referable, object>(
                    siThis?.GetDereferencedMainDataObject() as AdminShell.Referable, 
                    siThis?.GetMainDataObject()
                );
            }

            //
            // First
            //

            if (adrMode == ScriptSelectAdressMode.First)
            {
                if (refType == ScriptSelectRefType.AAS)
                {
                    if (firstAas == null)
                    {
                        Log.Singleton.Error("Script: Select: No AssetAdministrationShells available!");
                        return null;
                    }
                    return new Tuple<AdminShell.Referable, object>(firstAas, firstAas);
                }

                if (refType == ScriptSelectRefType.SM)
                {
                    if (siAAS?.theAas != null)
                    {
                        var smr = siAAS.theAas.submodelRefs.FirstOrDefault();
                        var sm = pm.FindSubmodel(smr);
                        if (sm == null)
                        {
                            Log.Singleton.Error("Script: AAS selected, but no Submodel found!");
                            return null;
                        }
                        return new Tuple<AdminShell.Referable, object>(sm, smr);
                    }

                    if (firstAas != null)
                    {
                        var smr = firstAas.submodelRefs.FirstOrDefault();
                        var sm = pm.FindSubmodel(smr);
                        if (sm == null)
                        {
                            Log.Singleton.Error("Script: first AAS taken, but no Submodel found!");
                            return null;
                        }
                        return new Tuple<AdminShell.Referable, object>(sm, smr);
                    }
                }

                if (refType == ScriptSelectRefType.SME)
                {
                    if (siSM?.theSubmodel?.submodelElements != null
                        && siSM?.theSubmodel?.submodelElements.Count > 0)
                    {
                        var sme = siSM?.theSubmodel?.submodelElements.FirstOrDefault()?.submodelElement;
                        if (sme != null)
                            return new Tuple<AdminShell.Referable, object>(sme, sme);
                    }

                    if (firstSme != null)
                    {
                        return new Tuple<AdminShell.Referable, object>(firstSme, firstSme);
                    }
                }
            }

            //
            // Next
            //

            if (adrMode == ScriptSelectAdressMode.Next)
            {
                if (refType == ScriptSelectRefType.AAS)
                {
                    var idx = pm?.AdministrationShells?.IndexOf(siAAS?.theAas);
                    if (siAAS?.theAas == null || idx == null 
                        || idx.Value < 0 || idx.Value >= pm.AdministrationShells.Count - 1)
                    {
                        Log.Singleton.Error("Script: For next AAS, the selected AAS is unknown " +
                            "or no next AAS can be determined!");
                        return null;
                    }
                    var aas = pm?.AdministrationShells[idx.Value + 1];
                    return new Tuple<AdminShell.Referable, object>(aas, aas);
                }

                if (refType == ScriptSelectRefType.SM)
                {
                    var idx = siAAS?.theAas.submodelRefs?.IndexOf(siSM?.theSubmodelRef);
                    if (siAAS?.theAas?.submodelRefs == null 
                        || siSM?.theSubmodel == null
                        || siSM?.theSubmodelRef == null
                        || idx == null
                        || idx.Value < 0 || idx.Value >= siAAS.theAas.submodelRefs.Count)
                    {
                        // complain
                        Log.Singleton.Error("Script: For next SM, the selected AAS/ SM is unknown " +
                            "or no next SM can be determined!");
                        return null;
                    }
                    if (idx.Value >= siAAS.theAas.submodelRefs.Count - 1)
                    {
                        // return null without error, as this is "expected" behaviour
                        return null;
                    }

                    // make the step
                    var smr = siAAS.theAas.submodelRefs[idx.Value + 1];
                    var sm = pm.FindSubmodel(smr);
                    if (sm == null)
                    {
                        Log.Singleton.Error("Script: For next SM, a SubmodelRef does not have a SM!");
                        return null;
                    }
                    return new Tuple<AdminShell.Referable, object>(sm, smr);
                }
            }

            //
            // Prev
            //

            if (adrMode == ScriptSelectAdressMode.Prev)
            {
                if (refType == ScriptSelectRefType.AAS)
                {
                    var idx = pm?.AdministrationShells?.IndexOf(siAAS?.theAas);
                    if (siAAS?.theAas == null || idx == null
                        || idx.Value <= 0 || idx.Value >= pm.AdministrationShells.Count)
                    {
                        Log.Singleton.Error("Script: For previos AAS, the selected AAS is unknown " +
                            "or no previous AAS can be determined!");
                        return null;
                    }
                    var aas = pm?.AdministrationShells[idx.Value - 1];
                    return new Tuple<AdminShell.Referable, object>(aas, aas);
                }

                if (refType == ScriptSelectRefType.SM)
                {
                    var idx = siAAS?.theAas.submodelRefs?.IndexOf(siSM?.theSubmodelRef);
                    if (siAAS?.theAas?.submodelRefs == null
                        || siSM?.theSubmodel == null
                        || siSM?.theSubmodelRef == null
                        || idx == null
                        || idx.Value < 0 || idx.Value >= siAAS.theAas.submodelRefs.Count)
                    {
                        // complain
                        Log.Singleton.Error("Script: For prev SM, the selected AAS/ SM is unknown " +
                            "or no prev SM can be determined!");
                        return null;
                    }
                    if (idx.Value <= 0)
                    {
                        // return null without error, as this is "expected" behaviour
                        return null;
                    }

                    // make the step
                    var smr = siAAS.theAas.submodelRefs[idx.Value - 1];
                    var sm = pm.FindSubmodel(smr);
                    if (sm == null)
                    {
                        Log.Singleton.Error("Script: For prev SM, a SubmodelRef does not have a SM!");
                        return null;
                    }
                    return new Tuple<AdminShell.Referable, object>(sm, smr);
                }
            }

            // Oops!
            return null;
        }

        AdminShellV20.Referable IAasxScriptRemoteInterface.Select(object[] args)
        {
            // access
            if (args == null || args.Length < 1
                || !(args[0] is string refTypeName))
            {
                Log.Singleton.Error("Script: Select: Referable type missing!");
                return null;
            }

            // check if Referable Type is ok
            ScriptSelectRefType refType = ScriptSelectRefType.None;
            if (refTypeName.Trim().ToLower() == "this")
                refType = ScriptSelectRefType.This;
            for (int i = 0; i < _allowedSelectRefType.Length; i++)
            {
                var sd = _allowedSelectRefType[i].GetSelfDescription();
                if ((sd?.ElementName.Trim().ToLower() == refTypeName.Trim().ToLower())
                    || (sd?.ElementAbbreviation.Trim().ToLower() == refTypeName.Trim().ToLower()))
                    refType = ScriptSelectRefType.AAS + i;
            }
            if (refType == ScriptSelectRefType.None)
            {
                Log.Singleton.Error("Script: Select: Referable type invalid!");
                return null;
            }

            // check adress mode is ok
            ScriptSelectAdressMode adrMode = ScriptSelectAdressMode.None;

            if (refType != ScriptSelectRefType.This)
            {
                if (args.Length < 2
                    || !(args[1] is string adrModeName))
                {
                    Log.Singleton.Error("Script: Select: Adfress mode missing!");
                    return null;
                }

                for (int i = 0; i < _allowedSelectAdressMode.Length; i++)
                    if (_allowedSelectAdressMode[i].ToLower().Trim() == adrModeName.Trim().ToLower())
                        adrMode = ScriptSelectAdressMode.First + i;
                if (adrMode == ScriptSelectAdressMode.None)
                {
                    Log.Singleton.Error("Script: Select: Adressing mode invalid!");
                    return null;
                }
            }

            // evaluate next item
            var selEval = SelectEvalObject(refType, adrMode);

            // well-defined result?
            if (selEval != null && selEval.Item1 != null && selEval.Item2 != null)
            {
                DisplayElements.ClearSelection();
                DisplayElements.TrySelectMainDataObject(selEval.Item2, wishExpanded: true);
                return selEval.Item1;
            }            

            // nothing found
            return null;
        }

            
        public async Task<int> Tool(object[] args)
        {
            if (args == null || args.Length < 1 || !(args[0] is string toolName))
            {
                Log.Singleton.Error("Script: Invoke Tool: Toolname missing");
                return -1;
            }

            // name of tool, find it
            var foundMenu = _mainMenu.Menu;
            var mi = foundMenu.FindName(toolName);
            if (mi == null)
            {
                foundMenu = _dynamicMenu.Menu;
                mi = foundMenu.FindName(toolName);
            }

            if (foundMenu == null || mi == null)
            {
                Log.Singleton.Error($"Script: Invoke Tool: Toolname invalid: {toolName}");
                return -1;
            }

            // create a ticket
            var ticket = new AasxMenuActionTicket()
            {
                MenuItem = mi,
                ScriptMode = true,
                ArgValue = new AasxMenuArgDictionary()
            };

            // go thru the remaining arguments and find arg names and values
            var argi = 1;
            while (argi < args.Length)
            {
                // get arg name
                if (!(args[argi] is string argname))
                {
                    Log.Singleton.Error($"Script: Invoke Tool: Argument at index {argi} is " +
                        $"not string type for argument name.");
                    return -1;
                }

                // find argname?
                var ad = mi.ArgDefs?.Find(argname);
                if (ad == null)
                {
                    Log.Singleton.Error($"Script: Invoke Tool: Argument at index {argi} is " +
                        $"not valid argument name.");
                    return -1;
                }

                // create arg value (not available is okay)
                object av = null;
                if (argi + 1 < args.Length)
                    av = args[argi + 1];

                // into ticket
                ticket.ArgValue.Add(ad, av);

                // 2 forward!
                argi += 2;
            }

            // invoke action
            await foundMenu.ActivateAction(mi, ticket);

            // perform UI updates if required
            if (ticket.UiLambdaAction != null && !(ticket.UiLambdaAction is AnyUiLambdaActionNone))
            {
                // add to "normal" event quoue
                DispEditEntityPanel.AddWishForOutsideAction(ticket.UiLambdaAction);
            }

            return 0;
        }

        bool IAasxScriptRemoteInterface.Location(object[] args)
        {
            // access
            if (args == null || args.Length < 1 || !(args[0] is string cmd))
                return false;

            // delegate
            CommandBinding_GeneralDispatch(
                "location" + cmd.Trim().ToLower(),
                null,
                ticket: new AasxMenuActionTicket()).GetAwaiter().GetResult();
            return true;
        }
    }
}
