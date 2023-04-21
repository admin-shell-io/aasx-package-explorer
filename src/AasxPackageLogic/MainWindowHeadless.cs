﻿/*
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
using System.Windows.Controls;
using Microsoft.VisualBasic.Logging;

// ReSharper disable MethodHasAsyncOverload

namespace AasxPackageLogic
{
    /// <summary>
    /// This class takes menu action tickets with fully provided arguments and dispatches these
    /// to the functionality pieces provide by the "logic" class.
    /// It would be an entry point to headless, commandline only applications.
    /// </summary>
    public class MainWindowHeadless : MainWindowTools
    {
        /// <summary>
        /// Standard handler, if not given by ticket.
        /// </summary>
        public async Task<AnyUiMessageBoxResult> StandardInvokeMessageDelegate(bool error, string message)
        {
            if (error)
                Log.Singleton.Error(message);
            else
                Log.Singleton.Info(message);
            await Task.Yield();
            return AnyUiMessageBoxResult.Cancel;
        }

        public void FillSelectedItem(
            VisualElementGeneric selectedItem, AasxMenuActionTicket ticket = null)
        {
            // access
            if (ticket == null || selectedItem == null)
                return;

            // basics
            var ve = selectedItem;
            if (ve != null)
            {
                ticket.MainDataObject = ve.GetMainDataObject();
                ticket.DereferencedMainDataObject = ve.GetDereferencedMainDataObject();
            }

            // set
            if (selectedItem is VisualElementEnvironmentItem veei)
            {
                ticket.Package = veei.thePackage;
                ticket.Env = veei.theEnv;
            }

            if (selectedItem is VisualElementAdminShell veaas)
            {
                ticket.Package = veaas.thePackage;
                ticket.Env = veaas.theEnv;
                ticket.AAS = veaas.theAas;
            }

            if (selectedItem is VisualElementAsset veasset)
            {
                ticket.Env = veasset.theEnv;
                ticket.AssetInfo = veasset.theAsset;
            }

            if (selectedItem is VisualElementSubmodelRef vesmr)
            {
                ticket.Package = vesmr.thePackage;
                ticket.Env = vesmr.theEnv;
                ticket.Submodel = vesmr.theSubmodel;
                ticket.SubmodelRef = vesmr.theSubmodelRef;
            }

            if (selectedItem is VisualElementSubmodel vesm)
            {
                ticket.Package = PackageCentral?.Main;
                ticket.Env = vesm.theEnv;
                ticket.Submodel = vesm.theSubmodel;
            }

            if (selectedItem is VisualElementSubmodelElement vesme)
            {
                ticket.Package = PackageCentral?.Main;
                ticket.Env = vesme.theEnv;
                ticket.SubmodelElement = vesme.theWrapper;
            }

        }

#pragma warning disable CS1998
        // ReSharper disable CSharpWarnings::CS1998

        /// <summary>
        /// General dispatch for menu functions, which are not depending on any UI.
        /// </summary>
        public async Task CommandBinding_GeneralDispatchHeadless(
            string cmd,
            AasxMenuItemBase menuItem,
            AasxMenuActionTicket ticket)
        {
            //
            // Start
            //

            if (cmd == null || ticket == null)
                return;

            //
            // Dispatch NEW
            //

            //
            // Dispatch (Sign and Validate either on Submodel / AAS level)
            //

            if ((cmd == "sign" || cmd == "validatecertificate" || cmd == "encrypt"))
            {
                if (cmd == "sign"
                    && (ticket.Submodel != null || ticket.SubmodelElement != null))
                {
                    // arguments
                    if (!(ticket["UseX509"] is bool useX509))
                    {
                        LogErrorToTicket(ticket, "Sign: use of X509 not determined.");
                        return;
                    }

                    try
                    {
                        // refer to logic
                        if (Tool_Security_Sign(
                            ticket.Submodel, ticket.SubmodelElement, ticket.Env, useX509) != true)
                        {
                            LogErrorToTicket(ticket,
                                "Not able to execute tool for signing Submodel or SubmodelElement!");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogErrorToTicket(ticket, ex, "Signing Submodel/ SME");
                    }

                    // important to return here!
                    return;
                }

                if (cmd == "validatecertificate"
                    && (ticket.Submodel != null || ticket.SubmodelElement != null))
                {
                    try
                    {
                        // refer to logic
                        if (Tool_Security_ValidateCertificate(
                            ticket.Submodel, ticket.SubmodelElement, ticket.Env) != true)
                        {
                            Log.Singleton.Error("Not able to execute tool for validate certificate of " +
                                "Submodel or SubmodelElement!");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogErrorToTicket(ticket, ex, "Validating certificate Submodel/ SME");
                    }

                    // important to return here!
                    return;
                }

                // Porting (MIHO): this seems to be executed, if above functions are not engaged
                // suspecting: for whole AAS/ package or so ..

                if (cmd == "sign")
                {
                    // arguments
                    if (!(ticket["Source"] is string sourceFn)
                        || !(ticket["Certificate"] is string certFn))
                    {
                        LogErrorToTicket(ticket, "Sign: source package or certificate filename invalid.");
                        return;
                    }

                    // do
                    PackageHelper.SignAll(
                        sourceFn, certFn,
                        invokeMessage: (ticket.InvokeMessage == null)
                            ? StandardInvokeMessageDelegate : ticket.InvokeMessage);
                }

                if (cmd == "validatecertificate")
                {
                    // arguments
                    if (!(ticket["Source"] is string sourceFn))
                    {
                        LogErrorToTicket(ticket, "Validate: source package filename invalid.");
                        return;
                    }

                    // do
                    PackageHelper.Validate(sourceFn,
                        invokeMessage: (ticket.InvokeMessage == null)
                            ? StandardInvokeMessageDelegate : ticket.InvokeMessage);
                }

                if (cmd == "encrypt")
                {
                    // arguments
                    if (!(ticket["Source"] is string sourceFn)
                        || !(ticket["Certificate"] is string certFn)
                        || !(ticket["Target"] is string targetFn))
                    {
                        LogErrorToTicket(ticket,
                            "Encrypt: source or target package or certificate filename invalid.");
                        return;
                    }

                    // refer to logic
                    if (Tool_Security_PackageEncrpt(sourceFn, certFn, targetFn) != true)
                    {
                        LogErrorToTicket(ticket,
                            "Not able to execute tool for package encryption.");
                    }
                }

            }

            if (cmd == "decrypt")
            {
                // arguments
                if (!(ticket["Source"] is string sourceFn)
                    || !(ticket["Certificate"] is string certFn)
                    || !(ticket["Target"] is string targetFn))
                {
                    LogErrorToTicket(ticket,
                        "Encrypt: source or target package or certificate filename invalid.");
                    return;
                }

                // refer to logic
                if (Tool_Security_PackageDecrpt(sourceFn, certFn, targetFn) != true)
                {
                    LogErrorToTicket(ticket,
                        "Not able to execute tool for package decryption.");
                }
            }

            if (cmd == "opcread")
            {
                // arguments
                if (ticket.Submodel == null)
                {
                    LogErrorToTicket(ticket,
                        "OPC UA Client read: No valid Submodel selected");
                    return;
                }

                // do
                Tool_OpcUaClientRead(ticket.Submodel);
            }

            if (cmd == "submodelread")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Submodel Read: No valid Submodel, Env, source file selected");
                    return;
                }

                try
                {
                    Tool_ReadSubmodel(ticket.Submodel, ticket.Env, fn, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Read");
                }
            }

            if (cmd == "submodelwrite")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Submodel Write: No valid Submodel, Env, target file selected");
                    return;
                }

                try
                {
                    using (var s = new StreamWriter(fn))
                    {
                        var json = JsonConvert.SerializeObject(ticket.Submodel, Formatting.Indented);
                        s.WriteLine(json);
                    }
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Write");
                }

            }

            if (cmd == "submodelput")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null ||
                    !(ticket["URL"] is string url) || url.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Submodel Put: No valid Submodel, Env, URL selected");
                    return;
                }

                // execute
                Log.Singleton.Info($"Connecting to REST server {url} ..");

                try
                {
                    Tool_SubmodelPut(ticket.Submodel, url, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Put");
                }
            }

            if (cmd == "submodelget")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null ||
                    !(ticket["URL"] is string url) || url.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Submodel Get: No valid Submodel, Env, URL selected");
                    return;
                }

                // execute
                Log.Singleton.Info($"Connecting to REST server {url} ..");

                try
                {
                    Tool_SubmodelGet(ticket.Env, ticket.Submodel, url, ticket);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "Submodel Get");
                }
            }

            if (cmd == "rdfread")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "RDF Read: No valid Submodel, Env, source file selected");
                    return;
                }

                // do it
                try
                {
                    // do it
                    AasxBammRdfImExport.BAMMRDFimport.ImportInto(
                        fn, ticket?.Env, ticket?.Submodel, null);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing, an error occurred");
                }
            }

            if (cmd == "bmecatimport")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "BMEcat import: No valid Submodel, Env, source file selected");
                    return;
                }

                // do it
                try
                {
                    // do it
                    BMEcatTools.ImportBMEcatToSubModel(fn, ticket?.Env, ticket?.Submodel, null);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing BMEcat, an error occurred");
                }
            }

            if (cmd == "csvimport")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "BMEcat import: No valid Submodel, Env, source file selected");
                    return;
                }

                // do it
                try
                {
                    // do it
                    CSVTools.ImportCSVtoSubModel(fn, ticket?.Env, ticket?.Submodel);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing CSV, an error occurred");
                }
            }

            if (cmd == "submodeltdimport")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null || ticket.SubmodelRef == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "TD import: No valid Submodel, SubmodelEf, Env, source file selected");
                    return;
                }

                // do it
                try
                {
                    // do it
                    JObject importObject = TDJsonImport.ImportTDJsontoSubModel
                        (ticket["File"] as string, ticket.Env, ticket.Submodel, ticket.SubmodelRef);

                    // check result
                    foreach (var temp in (JToken)importObject)
                    {
                        JProperty importProperty = (JProperty)temp;
                        string key = importProperty.Name.ToString();
                        if (key == "error")
                        {
                            LogErrorToTicket(ticket, "Unable to import the JSON LD File");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing JSON LD for Thing Description, an error occurred");
                }
            }

            if (cmd == "submodeltdexport")
            {
                // arguments
                if (ticket.Submodel == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Thing Description (TD) export: No valid Submodel, source file selected");
                    return;
                }

                // do it
                try
                {
                    // do it
                    JObject exportData = TDJsonExport.ExportSMtoJson(ticket.Submodel);
                    if (exportData["status"].ToString() == "success")
                    {
                        using (var s = new StreamWriter(ticket["File"] as string))
                        {
                            string output = Newtonsoft.Json.JsonConvert.SerializeObject(exportData["data"],
                                Newtonsoft.Json.Formatting.Indented);
                            s.WriteLine(output);
                        }
                    }
                    else
                    {
                        LogErrorToTicket(ticket, "Unable to Export the JSON LD File");
                    }
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing BMEcat, an error occurred");
                }
            }

            if (cmd == "opcuaimportnodeset")
            {
                // arguments
                if (ticket.Submodel == null || ticket.Env == null || ticket.SubmodelRef == null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "OPC UA Nodeset import: No valid Submodel, SubmodelEf, Env, source file selected");
                    return;
                }

                // do it
                try
                {
                    // do it
                    OpcUaTools.ImportNodeSetToSubModel(ticket["File"] as string, ticket.Env, ticket.Submodel, ticket.SubmodelRef);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex,
                        "When importing OPC UA Nodeset, an error occurred");
                }
            }

            if (cmd == "importaml")
            {
                // arguments
                if (ticket.Env == null || ticket.Env == null
                    || ticket.Submodel != null || ticket.SubmodelElement != null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Import AML: No valid AAS-Env, package, source file selected or " +
                        "a single Submodel, SubmodelElement selected");
                    return;
                }

                // do it
                try
                {
                    AasxAmlImExport.AmlImport.ImportInto(ticket.Package, fn);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When importing AML, an error occurred");
                }
            }

            if (cmd == "exportaml")
            {
                // arguments
                if (ticket.Env == null || ticket.Env == null
                    || ticket.Submodel != null || ticket.SubmodelElement != null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Export AML: No valid AAS-Env, package, target file selected or " +
                        "a single Submodel, SubmodelElement selected");
                    return;
                }

                try
                {
                    var tryUseCompactProperties = false;
                    if (ticket["FilterIndex"] is int filterIndex)
                        tryUseCompactProperties = filterIndex == 2;

                    AasxAmlImExport.AmlExport.ExportTo(ticket.Package, fn, tryUseCompactProperties);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting AML, an error occurred");
                }
            }

            if (cmd == "exportjsonschema")
            {
                // arguments
                if (ticket.Env == null
                    || ticket.Submodel == null || ticket.SubmodelElement != null
                    || !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Import AML: No valid single Submodel selected");
                    return;
                }

                try
                {
                    var jsonSchemaExporter = new AasxSchemaExport.SubmodelTemplateJsonSchemaExporterV20();
                    var schema = jsonSchemaExporter.ExportSchema(ticket.Submodel);

                    using (var s = new StreamWriter(fn))
                    {
                        s.Write(schema);
                    }
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting JSON schema, an error occurred");
                }
            }

            if (cmd == "opcuai4aasexport")
            {
                // arguments
                if (ticket.Env == null || ticket.Env == null
                    || ticket.Submodel != null || ticket.SubmodelElement != null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Import i4AAS based OPC UA mapping: No valid AAS-Env, package, target file " +
                        "selected or a single Submodel, SubmodelElement selected");
                    return;
                }
#if TODO
                // try to access I4AAS export information
                AasxUANodesetImExport.UANodeSet InformationModel = null;
                try
                {
                    var xstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        "AasxPackageExplorer.Resources.i4AASCS.xml");

                    InformationModel = UANodeSetExport.getInformationModel(xstream);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when accessing i4AASCS.xml mapping types.");
                    return;
                }
                Log.Singleton.Info("Mapping types loaded.");

                // ReSharper enable PossibleNullReferenceException
                try
                {
                    UANodeSetExport.root = InformationModel.Items.ToList();

                    foreach (AdminShellV20.Asset ass in ticket.Env.Assets)
                    {
                        UANodeSetExport.CreateAAS(ass.idShort, ticket.Env);
                    }

                    InformationModel.Items = UANodeSetExport.root.ToArray();

                    using (var writer = new System.IO.StreamWriter(fn))
                    {
                        var serializer = new XmlSerializer(InformationModel.GetType());
                        serializer.Serialize(writer, InformationModel);
                        writer.Flush();
                    }

                    Log.Singleton.Info("i4AAS based OPC UA mapping exported: " + fn);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when exporting i4AAS based OPC UA mapping.");
                }
#endif
            }

            if (cmd == "opcuai4aasimport")
            {
                // arguments
                if (ticket.Env == null || ticket.Env == null
                    || ticket.Submodel != null || ticket.SubmodelElement != null ||
                    !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Import i4AAS based OPC UA mapping: No valid AAS-Env, package, target file " +
                        "selected or a single Submodel, SubmodelElement selected");
                    return;
                }
#if TODO
                // do
                try
                {
                    UANodeSet InformationModel = UANodeSetExport.getInformationModel(fn);

                    ticket.PostResults = new Dictionary<string, object>();
                    ticket.PostResults.Add("TakeOver", UANodeSetImport.Import(InformationModel));

                    Log.Singleton.Info("i4AAS based OPC UA mapping imported: " + fn);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "when importing i4AAS based OPC UA mapping.");
                }
#endif
            }

            if (cmd == "exportgenericforms")
            {
                // arguments
                if (ticket.Env == null || ticket.Env == null
                    || ticket.Submodel == null
                    || !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Export GenericForms: No valid AAS-Env, package, Submodel or target file selected");
                    return;
                }

                try
                {
                    Log.Singleton.Info("Exporting GenericForms: {0}", fn);
                    AasxIntegrationBase.AasForms.AasFormUtils.ExportAsGenericFormsOptions(
                        ticket.Env, ticket.Submodel, fn);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting GenericForms, an error occurred");
                }
            }

            if (cmd == "exportpredefineconcepts")
            {
                // arguments
                if (ticket.Env == null
                    || ticket.Submodel == null
                    || !(ticket["File"] is string fn) || fn.HasContent() != true)
                {
                    LogErrorToTicket(ticket,
                        "Export PredefinedConcepts: No valid AAS-Env, package, Submodel or target file selected");
                    return;
                }

                try
                {
                    Log.Singleton.Info("Exporting text snippets for PredefinedConcepts: {0}", fn);
                    AasxPredefinedConcepts.ExportPredefinedConcepts.Export(ticket.Env, ticket.Submodel, fn);
                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "When exporting PredefinedConcepts, an error occurred");
                }
            }

            if (cmd == "newsubmodelfromplugin")
            {
                // arguments
                if (ticket.Env == null
                || ticket.AAS == null
                || ticket.Submodel != null)
                {
                    LogErrorToTicket(ticket,
                        "New Submodel from plugin: No valid AAS-Env, AAS selected or individual " +
                        "Submodel selected!");
                    return;
                }

                // try to get tuple?
                var record = ticket["Record"] as Tuple<Plugins.PluginInstance, string>;

                // or search?
                if (record == null && ticket["Name"] is string name && name.HasContent())
                {
                    foreach (var rec in GetPotentialGeneratedSubmodels())
                        if (rec.Item2?.ToLower().Contains(name.ToLower()) == true)
                        {
                            record = rec;
                            break;
                        }
                }

                // found?
                if (record == null || record.Item1 == null
                    || record.Item2?.HasContent() != true)
                {
                    LogErrorToTicket(ticket, "New Submodel from plugin: " +
                        "No name or selection given to which Submodel shall be generated.");
                    return;
                }

                // try to invoke plugin to get submodel
                Aas.Submodel smres = null;
                List<Aas.ConceptDescription> cdres = null;
                try
                {
                    var res = record.Item1.InvokeAction("generate-submodel", record.Item2) as AasxPluginResultBase;
                    if (res is AasxPluginResultBaseObject rbo)
                    {
                        smres = rbo.obj as Aas.Submodel;
                    }
                    if (res is AasxPluginResultGenerateSubmodel rgsm)
                    {
                        smres = rgsm.sm;
                        cdres = rgsm.cds;
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

                // something
                if (smres == null)
                {
                    LogErrorToTicket(ticket,
                        "New Submodel from plugin: Error accessing plugins. Aborting.");
                    return;
                }

                try
                {
                    // Submodel needs an identification
                    smres.Id = "";
                    if (smres.Kind == null || smres.Kind == Aas.ModelingKind.Instance)
                        smres.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelInstance);
                    else
                        smres.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelTemplate);

                    // add Submodel
                    var smref = smres.GetReference().Copy();
                    ticket.AAS.AddSubmodelReference(smref);
                    ticket.Env.Submodels.Add(smres);

                    // add ConceptDescriptions?
                    if (cdres != null && cdres.Count > 0)
                    {
                        int nr = 0;
                        foreach (var cd in cdres)
                        {
                            if (cd == null || cd.Id == null)
                                continue;
                            var cdFound = ticket.Env.FindConceptDescriptionById(cd.Id);
                            if (cdFound != null)
                                continue;
                            // ok, add
                            var newCd = cd.Copy();
                            ticket.Env.ConceptDescriptions.Add(newCd);
                            nr++;
                        }
                        Log.Singleton.Info(
                            $"added {nr} ConceptDescritions for Submodel {smres.IdShort}.");
                    }

                    // give data bickt
                    ticket["SmRef"] = smref;
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "when adding Submodel to AAS");
                }
            }

            if (cmd == "convertelement")
            {
                // arguments
                var rf = ticket.DereferencedMainDataObject as Aas.IReferable;
                if (ticket.Package == null
                    || rf == null)
                {
                    LogErrorToTicket(ticket,
                        "Convert Referable: No valid AAS package nor AAS Referable selected!");
                    return;
                }

                // try to get tuple?
                var record = ticket["Record"] as ConvertOfferBase;

                // or search?
                if (record == null && ticket["Name"] is string name && name.HasContent())
                {
                    var offers = AasxPredefinedConcepts.Convert.ConvertPredefinedConcepts.CheckForOffers(rf);
                    if (offers != null)
                        foreach (var o in offers)
                            if (o.OfferDisplay.ToLower().Contains(name.Trim().ToLower()))
                                record = o;
                }

                // found?
                if (record == null)
                {
                    LogErrorToTicket(ticket, "Convert Referable: No name or selection given to " +
                        "find an adequate offer toconvert from any plugin.");
                    return;
                }

                // do
                try
                {
                    record?.Provider?.ExecuteOffer(
                        ticket.Package, rf, record, deleteOldCDs: true, addNewCDs: true);

                }
                catch (Exception ex)
                {
                    LogErrorToTicket(ticket, ex, "while doing user defined conversion");
                }
            }

            if (cmd == "filerepoquery")
            {
                ticket.StartExec();

                // access
                if (PackageCentral.Repositories == null || PackageCentral.Repositories.Count < 1)
                {
                    LogErrorToTicket(ticket,
                        "AASX File Repository: No repository currently available! Please open.");
                    return;
                }

                // make a lambda
                Action<PackageContainerRepoItem> lambda = (ri) =>
                {
                    var fr = PackageCentral.Repositories?.FindRepository(ri);

                    if (fr != null && ri?.Location != null)
                    {
                        // which file?
                        var loc = fr?.GetFullItemLocation(ri.Location);
                        if (loc == null)
                            return;

                        // start animation
                        fr.StartAnimation(ri,
                            PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                        try
                        {
                            // load
                            Log.Singleton.Info("Switching to AASX repository location {0} ..", loc);
                            MainWindow?.UiLoadPackageWithNew(
                                PackageCentral.MainItem, null, loc, onlyAuxiliary: false);
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(
                                ex, $"When switching to AASX repository location {loc}.");
                        }
                    }
                };

                // get the list of items
                var repoItems = PackageCentral.Repositories.EnumerateItems().ToList();

                // scripted?
                if (ticket["Index"] is int)
                {
                    var ri = (int)ticket["Index"];
                    if (ri < 0 || ri >= repoItems.Count)
                    {
                        LogErrorToTicket(ticket, "Repo Query: Index out of bounds");
                        return;
                    }
                    lambda(repoItems[ri]);
                }
                else
                if (ticket["AAS"] is string aasid)
                {
                    var ri = PackageCentral.Repositories.FindByAasId(aasid);
                    if (ri == null)
                    {
                        LogErrorToTicket(ticket, "Repo Query: AAS-Id not found");
                        return;
                    }
                    lambda(ri);
                }
                else
                if (ticket["Asset"] is string aid)
                {
                    var ri = PackageCentral.Repositories.FindByAssetId(aid);
                    if (ri == null)
                    {
                        LogErrorToTicket(ticket, "Repo Query: Asset-Id not found");
                        return;
                    }
                    lambda(ri);
                }
                else
                {
                    // dialogue
                    if (DisplayContext == null)
                    {
                        LogErrorToTicket(ticket, "Repo Query: No AnyUI context found. Could not display.");
                        return;
                    }

                    var uc = new AnyUiDialogueDataSelectFromRepository();
                    uc.Caption = "Select in repository";
                    uc.Items = repoItems;
                    if (DisplayContext.StartFlyoverModal(uc))
                    {
                        lambda(uc.ResultItem);
                    }
                }
            }

            //
            // Plugins
            //

            // check if a plugin is attached to the name
            if (menuItem is AasxMenuItem mi && mi.PluginToAction?.HasContent() == true)
            {
                try
                {
                    var plugin = Plugins.FindPluginInstance(mi.PluginToAction);
                    if (plugin != null && plugin.HasAction("call-menu-item", useAsync: true))
                        await plugin.InvokeActionAsync("call-menu-item", cmd, ticket, DisplayContext);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(
                        ex, $"When calling plugin {mi.PluginToAction} to call-menu-item {mi.Name}.");
                }
            }
        }

        public List<Tuple<Plugins.PluginInstance, string>>
            GetPotentialGeneratedSubmodels()
        {
            var res = new List<Tuple<Plugins.PluginInstance, string>>();
            foreach (var lpi in Plugins.LoadedPlugins.Values)
            {
                if (lpi.HasAction("get-list-new-submodel"))
                    try
                    {
                        var lpires = lpi.InvokeAction("get-list-new-submodel") as AasxPluginResultBaseObject;
                        if (lpires != null)
                        {
                            var lpireslist = lpires.obj as List<string>;
                            if (lpireslist != null)
                                foreach (var smname in lpireslist)
                                    res.Add(new Tuple<Plugins.PluginInstance, string>(lpi, smname));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogInternally.That.SilentlyIgnoredError(ex);
                    }
            }
            return res;
        }

    }
}