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
using AasxIntegrationBase;
using AasxIntegrationBaseWpf;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxPackageLogic.PackageCentral.AasxFileServerInterface;
using AasxSignature;
using AnyUi;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains the necessary scripting support for the UI application
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider, IAasxScriptRemoteInterface
    {
        protected string _currentScriptText = "";
        protected AasxScript _aasxScript = null;

        public async Task CommandBinding_ScriptEditLaunch(string cmd, AasxMenuItemBase menuItem)
        {
            // REFACTOR: SAME
            if (cmd == "scripteditlaunch")
            {
                // trivial things
                if (!PackageCentral.MainAvailable)
                {
                    await DisplayContext.MessageBoxFlyoutShowAsync(
                        "An AASX package needs to be available", "Error", 
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                    return;
                }

                // trivial things
                if (_aasxScript?.IsExecuting == true)
                {
                    if (AnyUiMessageBoxResult.No == await DisplayContext.MessageBoxFlyoutShowAsync(
                        "An AASX script is already executed! Continue anyway?", "Warning",
                        AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                        return;
                    else
                        // brutal
                        _aasxScript = null;
                }

                // prompt for the script
                var uc = new AnyUiDialogueDataTextEditor("Edit script to be launched ..");
                uc.MimeType = "application/csharp";
                uc.Presets = Options.Curr.ScriptPresets;
                uc.Text = _currentScriptText;

                // context menu
#if feature_not_available
                uc.ContextMenuCreate = () =>
                {
                    var cm = DynamicContextMenu.CreateNew(
                        new AasxMenu()
                            .AddAction("Clip", "Copy JSON to clipboard", "\U0001F4CB"));
                    return cm;
                };

                uc.ContextMenuAction = (cmd, mi, ticket) =>
                {
                    if (cmd == "clip")
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
#endif

                // execute
                await DisplayContext.StartFlyoverModalAsync(uc);

                // always remember script
                _currentScriptText = uc.Text;

                // execute?
                if (uc.Result && uc.Text.HasContent())
                {
                    try
                    {
                        // create first
                        if (_aasxScript == null)
                            _aasxScript = new AasxScript();

                        // executing
                        _aasxScript.StartEnginBackground(
                            uc.Text, Options.Curr.ScriptLoglevel,
                            MainMenu?.Menu, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "when executing script");
                    }
                }
            }

            // REFACTOR: SAME
            for (int i = 0; i < 9; i++)
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
                        if (AnyUiMessageBoxResult.No == await DisplayContext.MessageBoxFlyoutShowAsync(
                            "An AASX script is already executed! Continue anyway?", "Warning",
                            AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                            return;
                        else
                            // brutal
                            _aasxScript = null;
                    }

                    // prompting
                    if (!Options.Curr.ScriptLaunchWithoutPrompt)
                    {
                        if (AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
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
                            MainMenu?.Menu, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "when executing script");
                    }
                }
        }


        public enum ScriptSelectRefType { None = 0, This, AAS, SM, SME, CD };

        public enum ScriptSelectAdressMode { None = 0, First, Next, Prev, idShort, semanticId };
        protected static string[] _allowedSelectAdressMode = {
            "First", "Next", "Prev", "idShort", "semanticId"
        };

        protected Tuple<Aas.IReferable, object> SelectEvalObject(
            ScriptSelectRefType refType, ScriptSelectAdressMode adrMode)
        {
            //
            // Try gather some selection states
            //

            // something to select
            var pm = PackageCentral?.Main?.AasEnv;
            if (pm == null)
            {
                Log.Singleton.Error("Script: Select: No main package AAS environment available!");
                return null;
            }

            // available elements in the environment
            var firstAas = pm.AssetAdministrationShells.FirstOrDefault();

            Aas.Submodel firstSm = null;
            if (firstAas != null && firstAas.Submodels != null && firstAas.Submodels.Count > 0)
                firstSm = pm.FindSubmodel(firstAas.Submodels[0]);

            Aas.ISubmodelElement firstSme = null;
            if (firstSm != null && firstSm.SubmodelElements != null && firstSm.SubmodelElements.Count > 0)
                firstSme = firstSm.SubmodelElements[0];

            // TODO (MIHO, 2022-12-16): Some cases are not implemented

            // selected items by user
            var siThis = DisplayElements.SelectedItem;
            var siSM = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementSubmodelRef, includeThis: true) as VisualElementSubmodelRef;
            var siAAS = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementAdminShell, includeThis: true) as VisualElementAdminShell;
#if later
            var siSME = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementSubmodelElement, includeThis: true);
            var siCD = siThis?.FindFirstParent(
                    (ve) => ve is VisualElementConceptDescription, includeThis: true);
#endif

            //
            // This
            //

            if (refType == ScriptSelectRefType.This)
            {
                // just return as Referable
                return new Tuple<Aas.IReferable, object>(
                    siThis?.GetDereferencedMainDataObject() as Aas.IReferable,
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
                    return new Tuple<Aas.IReferable, object>(firstAas, firstAas);
                }

                if (refType == ScriptSelectRefType.SM)
                {
                    if (siAAS?.theAas != null)
                    {
                        var smr = siAAS.theAas.Submodels.FirstOrDefault();
                        var sm = pm.FindSubmodel(smr);
                        if (sm == null)
                        {
                            Log.Singleton.Error("Script: AAS selected, but no Submodel found!");
                            return null;
                        }
                        return new Tuple<Aas.IReferable, object>(sm, smr);
                    }

                    if (firstAas != null)
                    {
                        var smr = firstAas.Submodels.FirstOrDefault();
                        var sm = pm.FindSubmodel(smr);
                        if (sm == null)
                        {
                            Log.Singleton.Error("Script: first AAS taken, but no Submodel found!");
                            return null;
                        }
                        return new Tuple<Aas.IReferable, object>(sm, smr);
                    }
                }

                if (refType == ScriptSelectRefType.SME)
                {
                    if (siSM?.theSubmodel?.SubmodelElements != null
                        && siSM?.theSubmodel?.SubmodelElements.Count > 0)
                    {
                        var sme = siSM?.theSubmodel?.SubmodelElements.FirstOrDefault();
                        if (sme != null)
                            return new Tuple<Aas.IReferable, object>(sme, sme);
                    }

                    if (firstSme != null)
                    {
                        return new Tuple<Aas.IReferable, object>(firstSme, firstSme);
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
                    var idx = pm?.AssetAdministrationShells?.IndexOf(siAAS?.theAas);
                    if (siAAS?.theAas == null || idx == null
                        || idx.Value < 0 || idx.Value >= pm.AssetAdministrationShells.Count - 1)
                    {
                        Log.Singleton.Error("Script: For next AAS, the selected AAS is unknown " +
                            "or no next AAS can be determined!");
                        return null;
                    }
                    var aas = pm?.AssetAdministrationShells[idx.Value + 1];
                    return new Tuple<Aas.IReferable, object>(aas, aas);
                }

                if (refType == ScriptSelectRefType.SM)
                {
                    var idx = siAAS?.theAas?.Submodels?.IndexOf(siSM?.theSubmodelRef);
                    if (siAAS?.theAas?.Submodels == null
                        || siSM?.theSubmodel == null
                        || siSM?.theSubmodelRef == null
                        || idx == null
                        || idx.Value < 0 || idx.Value >= siAAS.theAas.Submodels.Count)
                    {
                        // complain
                        Log.Singleton.Error("Script: For next SM, the selected AAS/ SM is unknown " +
                            "or no next SM can be determined!");
                        return null;
                    }
                    if (idx.Value >= siAAS.theAas.Submodels.Count - 1)
                    {
                        // return null without error, as this is "expected" behaviour
                        return null;
                    }

                    // make the step
                    var smr = siAAS.theAas.Submodels[idx.Value + 1];
                    var sm = pm.FindSubmodel(smr);
                    if (sm == null)
                    {
                        Log.Singleton.Error("Script: For next SM, a SubmodelRef does not have a SM!");
                        return null;
                    }
                    return new Tuple<Aas.IReferable, object>(sm, smr);
                }
            }

            //
            // Prev
            //

            if (adrMode == ScriptSelectAdressMode.Prev)
            {
                if (refType == ScriptSelectRefType.AAS)
                {
                    var idx = pm?.AssetAdministrationShells?.IndexOf(siAAS?.theAas);
                    if (siAAS?.theAas == null || idx == null
                        || idx.Value <= 0 || idx.Value >= pm.AssetAdministrationShells.Count)
                    {
                        Log.Singleton.Error("Script: For previos AAS, the selected AAS is unknown " +
                            "or no previous AAS can be determined!");
                        return null;
                    }
                    var aas = pm?.AssetAdministrationShells[idx.Value - 1];
                    return new Tuple<Aas.IReferable, object>(aas, aas);
                }

                if (refType == ScriptSelectRefType.SM)
                {
                    var idx = siAAS?.theAas?.Submodels?.IndexOf(siSM?.theSubmodelRef);
                    if (siAAS?.theAas?.Submodels == null
                        || siSM?.theSubmodel == null
                        || siSM?.theSubmodelRef == null
                        || idx == null
                        || idx.Value < 0 || idx.Value >= siAAS.theAas.Submodels.Count)
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
                    var smr = siAAS.theAas.Submodels[idx.Value - 1];
                    var sm = pm.FindSubmodel(smr);
                    if (sm == null)
                    {
                        Log.Singleton.Error("Script: For prev SM, a SubmodelRef does not have a SM!");
                        return null;
                    }
                    return new Tuple<Aas.IReferable, object>(sm, smr);
                }
            }

            // Oops!
            return null;
        }

        Aas.IReferable IAasxScriptRemoteInterface.Select(object[] args)
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

            switch (refTypeName.Trim().ToLower())
            {
                case "this":
                    refType = ScriptSelectRefType.This;
                    break;
                case "aas":
                case "assetadministrationshell":
                    refType = ScriptSelectRefType.AAS;
                    break;
                case "sm":
                case "submodel":
                    refType = ScriptSelectRefType.SM;
                    break;
                case "sme":
                case "submodelelement":
                    refType = ScriptSelectRefType.SME;
                    break;
                case "cd":
                case "conceptdescription":
                    refType = ScriptSelectRefType.CD;
                    break;
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
            var foundMenu = MainMenu.Menu;
            var mi = foundMenu.FindName(toolName);
            if (mi == null)
            {
                foundMenu = _dynamicMenu.Menu;
                mi = foundMenu.FindName(toolName);
            }
            if (mi == null)
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
