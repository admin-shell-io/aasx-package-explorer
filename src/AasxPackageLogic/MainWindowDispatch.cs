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
using AasxSignature;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class takes menu action tickets with fully provided arguments and dispatches these
    /// to the functionality pieces provide by the "logic" class
    /// </summary>
    public class MainWindowDispatch : MainWindowLogic
    {
        /// <summary>
        /// Standard handler, if not given by ticket.
        /// </summary>
        public AnyUiMessageBoxResult StandardInvokeMessageDelegate(bool error, string message)
        {
            if (error)
                Log.Singleton.Error(message);
            else
                Log.Singleton.Info(message);
            return AnyUiMessageBoxResult.Cancel;
        }

        public async Task CommandBinding_GeneralDispatch(
            string cmd,
            AasxMenuActionTicket ticket)
        {
            //
            // Start
            //

            if (cmd == null || ticket == null)
                return;

            var scriptmode = ticket.ScriptMode == true;

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
                        fn, ticket?.Env, ticket?.Submodel);
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
                    BMEcatTools.ImportBMEcatToSubModel(fn, ticket?.Env, ticket?.Submodel);
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

        }
    }
}