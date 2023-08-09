/*
Copyright (c) 2022 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2022 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic.PackageCentral;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.Convert;
using AasxSignature;
using AdminShellNS;
using AnyUi;
using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Aas = AasCore.Aas3_0;

// ReSharper disable MethodHasAsyncOverload

namespace AasxPackageLogic
{
    /// <summary>
    /// This class takes menu action tickets with fully provided arguments and dispatches these
    /// to the functionality pieces provided by the "logic" class.
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
            VisualElementGeneric selectedItem,
            ListOfVisualElementBasic selectedItems,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (ticket == null)
                return;

            // basics
            var ve = selectedItem;
            if (ve != null)
            {
                ticket.MainDataObject = ve.GetMainDataObject();
                ticket.DereferencedMainDataObject = ve.GetDereferencedMainDataObject();
            }

            // more (only if requested)
            ticket.SelectedMainDataObjects = 
                selectedItems?.Select((ve) => ve.GetMainDataObject());
            ticket.SelectedDereferencedMainDataObjects = 
                selectedItems?.Select((ve) => ve.GetDereferencedMainDataObject());

            // selectedItem is null, if multiple selected items
            // Note: looks cumbersome, but intention is: specific ticket-Members for SME..
            // are only set, if selected items count == 1.
            var firstItem = selectedItems?.FirstOrDefault();

            // set
            if (firstItem is VisualElementEnvironmentItem veei)
            {
                ticket.Package = veei.thePackage;
                ticket.Env = veei.theEnv;
            }

            if (firstItem is VisualElementAdminShell veaas)
            {
                ticket.Package = veaas.thePackage;
                ticket.Env = veaas.theEnv;
                if (selectedItem != null)
                {
                    ticket.AAS = veaas.theAas;
                }
            }

            if (firstItem is VisualElementAsset veasset)
            {
                ticket.Env = veasset.theEnv;
                if (selectedItem != null)
                {
                    ticket.AssetInfo = veasset.theAsset;
                }
            }

            if (firstItem is VisualElementSubmodelRef vesmr)
            {
                ticket.Package = vesmr.thePackage;
                ticket.Env = vesmr.theEnv;
                if (selectedItem != null)
                {
                    ticket.Submodel = vesmr.theSubmodel;
                    ticket.SubmodelRef = vesmr.theSubmodelRef;
                }
            }

            if (firstItem is VisualElementSubmodel vesm)
            {
                ticket.Package = PackageCentral?.Main;
                ticket.Env = vesm.theEnv;
                if (selectedItem != null)
                {
                    ticket.Submodel = vesm.theSubmodel;
                }
            }

            if (firstItem is VisualElementSubmodelElement vesme)
            {
                ticket.Package = PackageCentral?.Main;
                ticket.Env = vesme.theEnv;
                if (selectedItem != null)
                {
                    ticket.SubmodelElement = vesme.theWrapper;
                }
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
                        var json = Jsonization.Serialize.ToJsonObject(ticket.Submodel).ToJsonString(new System.Text.Json.JsonSerializerOptions()
                        {
                            WriteIndented = true
                        });
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
                    if (smres.Kind == null || smres.Kind == Aas.ModellingKind.Instance)
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

            if (cmd == "newsubmodelfromknown")
            {
                //
                // start
                //

                // arguments
                if (ticket.Env == null
                || ticket.AAS == null
                || ticket.Submodel != null)
                {
                    LogErrorToTicket(ticket,
                        "New Submodel from known: No valid AAS-Env, AAS selected or individual " +
                        "Submodel selected!");
                    return;
                }

                // try to get tuple?
                var domainPart = ticket["Domain"] as string;

                // try to get full domain
                string domainFull = null;
                foreach (var dom in AasxPredefinedConcepts.DefinitionsPool.Static.GetDomains())
                    if (dom.Contains(domainPart, StringComparison.InvariantCultureIgnoreCase))
                    {
                        domainFull = dom;
                        break;
                    }
                if (domainFull == null)
                {
                    LogErrorToTicket(ticket,
                        "New Submodel from known: Full domain could not be identified. Aborting!");
                    return;
                }

                // try to get Referables to work on
                var listRfs = AasxPredefinedConcepts.DefinitionsPool.Static.GetEntitiesForDomain(domainFull)
                        .Where((o) => o is DefinitionsPoolReferableEntity)
                        .Cast<DefinitionsPoolReferableEntity>()
                        .Select((o) => o.Ref)
                        .Where((rf) => rf != null);

                var listSm = listRfs.Where((rf) => rf is Aas.ISubmodel);
                var listCd = listRfs.Where((rf) => rf is Aas.IConceptDescription);

                //
                // generate Submodels
                //

                int createdSms = 0;
                foreach (var knownSm in listSm)
                {
                    // individual instance
                    var smres = (knownSm as Aas.ISubmodel)?.Copy();
                    if (smres == null)
                        continue;

                    // need instance ids
                    if (smres.Kind == null || smres.Kind == Aas.ModellingKind.Instance)
                        smres.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelInstance);
                    else
                        smres.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelTemplate);

                    // add Submodel
                    var smref = smres.GetReference().Copy();
                    ticket.AAS.AddSubmodelReference(smref);
                    ticket.Env.Submodels.Add(smres);
                    createdSms++;
                }

                Log.Singleton.Info("New Submodel from known: {0} (empty) Submodels created.", createdSms);

                //
                // generate ConceptDescriptions
                //

                int createdCds = 0, foundCds = 0;
                foreach (var knownCd in listCd)
                {
                    // individual instance
                    var cdres = (knownCd as Aas.IConceptDescription)?.Copy();
                    if (cdres == null)
                        continue;

                    // check if already existing
                    var cdFound = ticket.Env.FindConceptDescriptionById(cdres.Id);
                    if (cdFound != null)
                    {
                        foundCds++;
                        continue;
                    }

                    // ok, add
                    ticket.Env.ConceptDescriptions.Add(cdres);
                    createdCds++;
                }

                Log.Singleton.Info("New Submodel from known: {0} ConceptDesciptions created, " +
                    "{1} ConceptDesciptions already found in Environment.", createdCds, foundCds);

#if __not_now

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
                    if (smres.Kind == null || smres.Kind == Aas.ModellingKind.Instance)
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
#endif
            }

            if (cmd == "missingcdsfromknown")
            {
                //
                // Step 1: analyze selected entity
                //

                var rfToCheck = new List<Aas.IReferable>();
                if (ticket.SelectedDereferencedMainDataObjects != null)
                    foreach (var mdo in ticket.SelectedDereferencedMainDataObjects)
                        if (mdo is Aas.AssetAdministrationShell mdoAas)
                            foreach (var sm in ticket.Env.FindAllSubmodelGroupedByAAS((aas, sm) => aas == mdoAas))
                                rfToCheck.Add(sm);
                        else if (mdo is Aas.Submodel mdoSm)
                            rfToCheck.Add(mdoSm);
                        else if (mdo is Aas.ISubmodelElement mdoSme)
                            rfToCheck.Add(mdoSme);

                if (rfToCheck.Count < 1)
                {
                    LogErrorToTicket(ticket, "No valid element selected to be checked for missing CDs.");
                    return;
                }

                //
                // Step 2: collect missing CDs
                //

                var cdsMissing = new List<string>();
                foreach (var rf in rfToCheck)
                    foreach (var x in rf.Descend())
                        if (x is Aas.ISubmodelElement sme && sme.SemanticId != null
                            && ticket.Env.FindConceptDescriptionByReference(sme.SemanticId) == null
                            && sme.SemanticId.IsValid() && sme.SemanticId.Count() == 1
                            && !cdsMissing.Contains(sme.SemanticId.Keys[0].Value))
                            cdsMissing.Add(sme.SemanticId.Keys[0].Value);

				if (cdsMissing.Count < 1)
				{
					LogErrorToTicket(ticket, "No missing CDs could be found for selected element. Aborting!");
					return;
				}

                //
                // Step 3: check, which CDs could be provided by pool
                //

                var cdsAvail = new List<Aas.ConceptDescription>();
                var duplicates = false;
                foreach (var cdm in cdsMissing)
                {
                    int count = 0;
                    foreach (var frf in AasxPredefinedConcepts.DefinitionsPool.Static
                                        .FindReferableByReference(cdm))
                        if (frf is Aas.ConceptDescription fcd)
                        {
                            var cpy = fcd.Copy();
                            count++;
                            if (count > 1)
                            {
                                cpy.Id += $"__{count:000}";
                                Log.Singleton.Error(
                                    $"Multiple CDs found for Id={cdm}. CD added with altered Id={cpy.Id}.");
                                duplicates = true;
                            }
                            cdsAvail.Add(cpy);
                        }
                }

				if (cdsAvail.Count < 1)
				{
					LogErrorToTicket(ticket, "No missing CDs could be found in pool of known. Aborting!");
					return;
				}

                //
                // Step 4: Ask
                //

                if (AnyUiMessageBoxResult.Yes != await DisplayContext.MessageBoxFlyoutShowAsync(
                    $"{cdsMissing.Count} CDs missing. {cdsAvail.Count} CDs available in pool of knonw. " +
                    "Add these available CDs to Environment?",
                    "Add CDs from pool of known",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                    return;

                //
                // Step 5: Go, add
                //

                foreach (var cd in cdsAvail)
                    ticket.Env.Add(cd);

                Log.Singleton.Info($"Added {cdsAvail.Count} missing CDs from pool of known.");
                if (duplicates)
                    Log.Singleton.Error("Duplicated CDs found while adding missing CDs from pool of known. " +
                        "See log.");
                ticket.Success = true;
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
                    object res = null;
                    if (plugin != null && plugin.HasAction("call-menu-item", useAsync: true))
                        res = await plugin.InvokeActionAsync("call-menu-item", cmd, ticket, DisplayContext, MainWindow?.GetEntityMasterPanel());

                    if (res is AasxPluginResultCallMenuItem aprcmi
                        && aprcmi.RenderWpfContent != null)
                    {
                        Log.Singleton.Info("Try displaying external entity control from plugin command..");
                        // MainWindow?.DisplayExternalEntity(aprcmi.RenderWpfContent);
                    }
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