/*
Copyright (c) 2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2022 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic.PackageCentral;
using AasxPredefinedConcepts.Convert;
using AasxSignature;
using AnyUi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using System.Windows;
using Microsoft.VisualBasic.Logging;
using AasCore.Aas3_0_RC02;

// ReSharper disable MethodHasAsyncOverload

namespace AasxPackageLogic
{
    /// <summary>
    /// This class sits above the abstract dialogs and attaches scripting the abstract functions.
    /// </summary>
    public class MainWindowScripting : MainWindowAnyUiDialogs, IAasxScriptRemoteInterface
    {
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
            var siThis = MainWindow?.GetDisplayElements()?.GetSelectedItem();
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
                MainWindow?.GetDisplayElements()?.ClearSelection();
                MainWindow?.GetDisplayElements()?.TrySelectMainDataObject(selEval.Item2, wishExpanded: true);
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
            var foundMenu = MainWindow?.GetMainMenu();
            var mi = foundMenu.FindName(toolName);
            if (mi == null)
            {
                foundMenu = MainWindow?.GetDynamicMenu();
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
                MainWindow.AddWishForToplevelAction(ticket.UiLambdaAction);
            }

            return 0;
        }

        async Task<bool> IAasxScriptRemoteInterface.Location(object[] args)
        {
            // access
            if (args == null || args.Length < 1 || !(args[0] is string cmd))
                return false;

            // delegate
            await CommandBinding_GeneralDispatchAnyUiDialogs(
                "location" + cmd.Trim().ToLower(),
                null,
                ticket: new AasxMenuActionTicket());
            return true;
        }

    }
}