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
using AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxPackageLogic.PackageCentral.AasxFileServerInterface;
// OZOZ using AasxSignature;
// OZOZ using AasxUANodesetImExport;
using AdminShellNS;
using AdminShellNS.Extenstions;
using AnyUi;
using Extensions;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Crypto;
// OZOZ using Org.Webpki.JsonCanonicalizer;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains all command bindings, such as for the main menu, in order to reduce the
    /// complexity of MainWindow.xaml.cs
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider
    {
        private string lastFnForInitialDirectory = null;

        public void RememberForInitialDirectory(string fn)
        {
            this.lastFnForInitialDirectory = fn;
        }

        public string DetermineInitialDirectory(string existingFn = null)
        {
            string res = null;

            if (existingFn != null)
                try
                {
                    res = System.IO.Path.GetDirectoryName(existingFn);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            // may be can used last?
            if (res == null && lastFnForInitialDirectory != null)
                try
                {
                    res = System.IO.Path.GetDirectoryName(lastFnForInitialDirectory);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            return res;
        }

        private void CommandExecution_RedrawAll()
        {
            // redraw everything
            RedrawAllAasxElements();
            RedrawElementView();
        }

        private static string makeJsonLD(string json, int count)
        {
            int total = json.Length;
            string header = "";
            string jsonld = "";
            string name = "";
            int state = 0;
            int identification = 0;
            string id = "idNotFound";

            for (int i = 0; i < total; i++)
            {
                var c = json[i];
                switch (state)
                {
                    case 0:
                        if (c == '"')
                        {
                            state = 1;
                        }
                        else
                        {
                            jsonld += c;
                        }
                        break;
                    case 1:
                        if (c == '"')
                        {
                            state = 2;
                        }
                        else
                        {
                            name += c;
                        }
                        break;
                    case 2:
                        if (c == ':')
                        {
                            bool skip = false;
                            string pattern = ": null";
                            if (i + pattern.Length < total)
                            {
                                if (json.Substring(i, pattern.Length) == pattern)
                                {
                                    skip = true;
                                    i += pattern.Length;
                                    // remove last "," in jsonld if character after null is not ","
                                    int j = jsonld.Length - 1;
                                    while (Char.IsWhiteSpace(jsonld[j]))
                                    {
                                        j--;
                                    }
                                    if (jsonld[j] == ',' && json[i] != ',')
                                    {
                                        jsonld = jsonld.Substring(0, j) + "\r\n";
                                    }
                                    else
                                    {
                                        jsonld = jsonld.Substring(0, j + 1) + "\r\n";
                                    }
                                    while (json[i] != '\n')
                                        i++;
                                }
                            }

                            if (!skip)
                            {
                                if (name == "identification")
                                    identification++;
                                if (name == "id" && identification == 1)
                                {
                                    id = "";
                                    int j = i;
                                    while (j < json.Length && json[j] != '"')
                                    {
                                        j++;
                                    }
                                    j++;
                                    while (j < json.Length && json[j] != '"')
                                    {
                                        id += json[j];
                                        j++;
                                    }
                                }
                                count++;
                                name += "__" + count;
                                if (header != "")
                                    header += ",\r\n";
                                header += "  \"" + name + "\": " + "\"aio:" + name + "\"";
                                jsonld += "\"" + name + "\":";
                            }
                        }
                        else
                        {
                            jsonld += "\"" + name + "\"" + c;
                        }
                        state = 0;
                        name = "";
                        break;
                }
            }

            string prefix = "  \"aio\": \"https://admin-shell-io.com/ns#\",\r\n";
            prefix += "  \"I40GenericCredential\": \"aio:I40GenericCredential\",\r\n";
            prefix += "  \"__AAS\": \"aio:__AAS\",\r\n";
            header = prefix + header;
            header = "\"context\": {\r\n" + header + "\r\n},\r\n";
            int k = jsonld.Length - 2;
            while (k >= 0 && jsonld[k] != '}' && jsonld[k] != ']')
            {
                k--;
            }
            #pragma warning disable format
            jsonld = jsonld.Substring(0, k+1);
            jsonld += ",\r\n" + "  \"id\": \"" + id + "\"\r\n}\r\n";
            jsonld = "\"doc\": " + jsonld;
            jsonld = "{\r\n\r\n" + header + jsonld + "\r\n\r\n}\r\n";
            #pragma warning restore format

            return jsonld;
        }

        private async void CommandBinding_GeneralDispatch(string cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException($"Unexpected null {nameof(cmd)}");
            }

            if (cmd == "new")
            {
                if (AnyUiMessageBoxResult.Yes == MessageBoxFlyoutShow(
                    "Create new Adminshell environment? This operation can not be reverted!", "AAS-ENV",
                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                {
                    try
                    {
                        // clear
                        ClearAllViews();
                        // create new AASX package
                        _packageCentral.MainItem.New();
                        // redraw
                        CommandExecution_RedrawAll();
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "When creating new AASX, an error occurred");
                        return;
                    }
                }
            }

            if (cmd == "open" || cmd == "openaux")
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.Main?.Filename);
                dlg.Filter =
                    "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|" +
                    "AAS JSON file (*.json)|*.json|All files (*.*)|*.*";

                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);

                    switch (cmd)
                    {
                        case "open":
                            UiLoadPackageWithNew(
                                _packageCentral.MainItem, null, dlg.FileName, onlyAuxiliary: false,
                                storeFnToLRU: dlg.FileName);
                            break;
                        case "openaux":
                            UiLoadPackageWithNew(
                                _packageCentral.AuxItem, null, dlg.FileName, onlyAuxiliary: true);
                            break;
                        default:
                            throw new InvalidOperationException($"Unexpected {nameof(cmd)}: {cmd}");
                    }
                }
            }

            if (cmd == "save")
            {
                // open?
                if (!_packageCentral.MainStorable)
                {
                    MessageBoxFlyoutShow(
                        "No open AASX file to be saved.",
                        "Save", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);
                    return;
                }

                try
                {
                    // save
                    await _packageCentral.MainItem.SaveAsAsync(runtimeOptions: _packageCentral.CentralRuntimeOptions);

                    // backup
                    if (Options.Curr.BackupDir != null)
                        _packageCentral.MainItem.Container.BackupInDir(
                            System.IO.Path.GetFullPath(Options.Curr.BackupDir),
                            Options.Curr.BackupFiles,
                            PackageContainerBase.BackupType.FullCopy);

                    // may be was saved to index
                    if (_packageCentral?.MainItem?.Container?.Env?.AasEnv != null)
                        _packageCentral.MainItem.Container.SignificantElements
                            = new IndexOfSignificantAasElements(_packageCentral.MainItem.Container.Env.AasEnv);

                    // may be was saved to flush events
                    CheckIfToFlushEvents();

                    // as saving changes the structure of pending supplementary files, re-display
                    RedrawAllAasxElements(keepFocus: true);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "When saving AASX, an error occurred");
                    return;
                }
                Log.Singleton.Info("AASX saved successfully: {0}", _packageCentral.MainItem.Filename);
            }

            if (cmd == "saveas")
            {
                // open?
                if (!_packageCentral.MainAvailable || _packageCentral.MainItem.Container == null)
                {
                    MessageBoxFlyoutShow(
                        "No open AASX file to be saved.",
                        "Save", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);
                    return;
                }

                // shall be a local file?!
                var isLocalFile = _packageCentral.MainItem.Container is PackageContainerLocalFile;
                if (!isLocalFile)
                    if (AnyUiMessageBoxResult.Yes != MessageBoxFlyoutShow(
                        "Current AASX file is not a local file. Proceed and convert to local AASX file?",
                        "Save", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Hand))
                        return;

                // where
                var dlg = new Microsoft.Win32.SaveFileDialog();
                if (isLocalFile)
                {
                    dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
                    dlg.FileName = _packageCentral.MainItem.Filename;
                }
                else
                {
                    dlg.FileName = "copy";
                }

                dlg.DefaultExt = "*.aasx";
                dlg.Filter =
                    "AASX package files (*.aasx)|*.aasx|AASX package files w/ JSON (*.aasx)|*.aasx|" +
                    (!isLocalFile ? "" : "AAS XML file (*.xml)|*.xml|AAS JSON file (*.json)|*.json|") +
                    "All files (*.*)|*.*";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
                if (res == true)
                {
                    // save
                    try
                    {
                        // if not local, do a bit of voodoo ..
                        if (!isLocalFile)
                        {
                            // establish local
                            if (!await _packageCentral.MainItem.Container.SaveLocalCopyAsync(
                                dlg.FileName,
                                runtimeOptions: _packageCentral.CentralRuntimeOptions))
                            {
                                // Abort
                                MessageBoxFlyoutShow(
                                    "Not able to copy current AASX file to local file. Aborting!",
                                    "Save", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);
                                return;
                            }

                            // re-load
                            UiLoadPackageWithNew(
                                _packageCentral.MainItem, null, dlg.FileName, onlyAuxiliary: false,
                                storeFnToLRU: dlg.FileName);
                            return;
                        }

                        //
                        // ELSE .. already local
                        //

                        // preferred format
                        var prefFmt = AdminShellPackageEnv.SerializationFormat.None;
                        if (dlg.FilterIndex == 1)
                            prefFmt = AdminShellPackageEnv.SerializationFormat.Xml;
                        if (dlg.FilterIndex == 2)
                            prefFmt = AdminShellPackageEnv.SerializationFormat.Json;

                        // save 
                        RememberForInitialDirectory(dlg.FileName);
                        await _packageCentral.MainItem.SaveAsAsync(dlg.FileName, prefFmt: prefFmt);

                        // backup (only for AASX)
                        if (dlg.FilterIndex == 0)
                            if (Options.Curr.BackupDir != null)
                                _packageCentral.MainItem.Container.BackupInDir(
                                    System.IO.Path.GetFullPath(Options.Curr.BackupDir),
                                    Options.Curr.BackupFiles,
                                    PackageContainerBase.BackupType.FullCopy);
                        // as saving changes the structure of pending supplementary files, re-display
                        RedrawAllAasxElements();
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "When saving AASX, an error occurred");
                        return;
                    }
                    Log.Singleton.Info("AASX saved successfully as: {0}", dlg.FileName);

                    // LRU?
                    // record in LRU?
                    try
                    {
                        var lru = _packageCentral?.Repositories?.FindLRU();
                        if (lru != null)
                            lru.Push(_packageCentral?.MainItem?.Container as PackageContainerRepoItem, dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"When managing LRU files");
                        return;
                    }
                }
            }

            if (cmd == "close" && _packageCentral?.Main != null)
            {
                if (AnyUiMessageBoxResult.Yes == MessageBoxFlyoutShow(
                    "Do you want to close the open package? Please make sure that you have saved before.",
                    "Close Package?", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                    try
                    {
                        _packageCentral.MainItem.Close();
                        RedrawAllAasxElements();
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "When closing AASX, an error occurred");
                    }
            }

            if ((cmd == "sign" || cmd == "validatecertificate" || cmd == "encrypt") && _packageCentral?.Main != null)
            {
                VisualElementSubmodelRef el = null;
                VisualElementSubmodelElement els = null;

                if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                    el = DisplayElements.SelectedItem as VisualElementSubmodelRef;

                if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelElement)
                    els = DisplayElements.SelectedItem as VisualElementSubmodelElement;

                if (cmd == "sign"
                    && ((el != null && el.theEnv != null && el.theSubmodel != null)
                            || (els != null && els.theEnv != null && els.theWrapper != null)))
                {
                    Submodel sm = null;
                    SubmodelElementCollection smc = null;
                    SubmodelElementCollection smcp = null;
                    if (el != null)
                    {
                        sm = el.theSubmodel;
                    }
                    if (els != null)
                    {
                        var smee = els.theWrapper;
                        if (smee is SubmodelElementCollection)
                        {
                            smc = smee as SubmodelElementCollection;
                            var p = smee.Parent;
                            if (p is Submodel)
                                sm = p as Submodel;
                            if (p is SubmodelElementCollection)
                                smcp = p as SubmodelElementCollection;
                        }
                    }
                    if (sm == null && smcp == null)
                        return;

                    bool useX509 = (AnyUiMessageBoxResult.Yes == MessageBoxFlyoutShow(
                        "Use X509 (yes) or Verifiable Credential (No)?",
                        "X509 or VerifiableCredential", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Hand));

                    List<SubmodelElementCollection> existing = new List<SubmodelElementCollection>();
                    if (smc == null)
                    {
                        for (int i = 0; i < sm.SubmodelElements.Count; i++)
                        {
                            var sme = sm.SubmodelElements[i];
                            var len = "signature".Length;
                            var idShort = sme.IdShort;
                            if (sme is SubmodelElementCollection &&
                                    idShort.Length >= len &&
                                    idShort.Substring(0, len).ToLower() == "signature")
                            {
                                existing.Add(sme as SubmodelElementCollection);
                                sm.Remove(sme);
                                i--; // check next
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < smc.Value.Count; i++)
                        {
                            var sme = smc.Value[i];
                            var len = "signature".Length;
                            var idShort = sme.IdShort;
                            if (sme is SubmodelElementCollection &&
                                    idShort.Length >= len &&
                                    idShort.Substring(0, len).ToLower() == "signature")
                            {
                                existing.Add(sme as SubmodelElementCollection);
                                smc.Remove(sme);
                                i--; // check next
                            }
                        }
                    }

                    if (useX509)
                    {
                        SubmodelElementCollection smec = new SubmodelElementCollection(idShort:"signature");
                        Property json = new Property(DataTypeDefXsd.String,idShort:"submodelJson");
                        Property canonical = new Property(DataTypeDefXsd.String, idShort:"submodelJsonCanonical");
                        Property subject = new Property(DataTypeDefXsd.String, idShort: "subject");
                        SubmodelElementCollection x5c = new SubmodelElementCollection(idShort:"x5c");
                        Property algorithm = new Property(DataTypeDefXsd.String, idShort: "algorithm");
                        Property sigT = new Property(DataTypeDefXsd.String, idShort: "sigT");
                        Property signature = new Property(DataTypeDefXsd.String, idShort: "signature");
                        smec.Add(json);
                        smec.Add(canonical);
                        smec.Add(subject);
                        smec.Add(x5c);
                        smec.Add(algorithm);
                        smec.Add(sigT);
                        smec.Add(signature);
                        string s = null;
                        if (smc == null)
                        {
                            s = JsonConvert.SerializeObject(sm, Formatting.Indented);
                        }
                        else
                        {
                            s = JsonConvert.SerializeObject(smc, Formatting.Indented);
                        }
                        json.Value = s;
                        // OZOZ JsonCanonicalizer jsonCanonicalizer = new JsonCanonicalizer(s);
                        // OZOZ string result = jsonCanonicalizer.GetEncodedString();
                        string result = "";
                        canonical.Value = result;
                        if (smc == null)
                        {
                            foreach (var e in existing)
                            {
                                sm.Add(e);
                            }
                            sm.Add(smec);
                        }
                        else
                        {
                            foreach (var e in existing)
                            {
                                smc.Add(e);
                            }
                            smc.Add(smec);
                        }

                        X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                        X509Certificate2Collection collection = store.Certificates;
                        X509Certificate2Collection fcollection = collection.Find(
                            X509FindType.FindByTimeValid, DateTime.Now, false);

                        X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection,
                            "Test Certificate Select",
                            "Select a certificate from the following list to get information on that certificate",
                            X509SelectionFlag.SingleSelection);
                        if (scollection.Count != 0)
                        {
                            var certificate = scollection[0];
                            subject.Value = certificate.Subject;

                            X509Chain ch = new X509Chain();
                            ch.Build(certificate);

                            //// string[] X509Base64 = new string[ch.ChainElements.Count];

                            int j = 1;
                            foreach (X509ChainElement element in ch.ChainElements)
                            {
                                Property c = new Property(DataTypeDefXsd.String,idShort:"certificate_" + j++);
                                c.Value = Convert.ToBase64String(element.Certificate.GetRawCertData());
                                x5c.Add(c);
                            }

                            try
                            {
                                using (RSA rsa = certificate.GetRSAPrivateKey())
                                {
                                    algorithm.Value = "RS256";
                                    byte[] data = Encoding.UTF8.GetBytes(result);
                                    byte[] signed = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                                    signature.Value = Convert.ToBase64String(signed);
                                    sigT.Value = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
                                }
                            }
                            // ReSharper disable EmptyGeneralCatchClause
                            catch
                            {
                            }
                            // ReSharper enable EmptyGeneralCatchClause
                        }
                    }
                    else // Verifiable Credential
                    {
                        SubmodelElementCollection smec = new SubmodelElementCollection(idShort: "signature");
                        Property json = new Property(DataTypeDefXsd.String, idShort: "submodelJson");
                        Property jsonld = new Property(DataTypeDefXsd.String, idShort: "submodelJsonLD");
                        Property vc = new Property(DataTypeDefXsd.String, idShort: "vc");
                        Property epvc = new Property(DataTypeDefXsd.String, idShort: "endpointVC");
                        Property algorithm = new Property(DataTypeDefXsd.String, idShort: "algorithm");
                        Property sigT = new Property(DataTypeDefXsd.String, idShort: "sigT");
                        Property proof = new Property(DataTypeDefXsd.String, idShort: "proof");
                        smec.Add(json);
                        smec.Add(jsonld);
                        smec.Add(vc);
                        smec.Add(epvc);
                        smec.Add(algorithm);
                        smec.Add(sigT);
                        smec.Add(proof);
                        string s = null;
                        if (smc == null)
                        {
                            s = JsonConvert.SerializeObject(sm, Formatting.Indented);
                        }
                        else
                        {
                            s = JsonConvert.SerializeObject(smc, Formatting.Indented);
                        }
                        json.Value = s;
                        s = makeJsonLD(s, 0);
                        jsonld.Value = s;

                        if (smc == null)
                        {
                            foreach (var e in existing)
                            {
                                sm.Add(e);
                            }
                            sm.Add(smec);
                        }
                        else
                        {
                            foreach (var e in existing)
                            {
                                smc.Add(e);
                            }
                            smc.Add(smec);
                        }

                        if (s != null && s != "")
                        {
                            epvc.Value = "https://nameplate.h2894164.stratoserver.net";
                            string requestPath = epvc.Value + "/demo/sign?create_as_verifiable_presentation=false";

                            var handler = new HttpClientHandler();
                            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

                            var client = new HttpClient(handler);
                            client.Timeout = TimeSpan.FromSeconds(60);

                            bool error = false;
                            HttpResponseMessage response = new HttpResponseMessage();
                            try
                            {
                                var content = new StringContent(s, System.Text.Encoding.UTF8, "application/json");
                                var task = Task.Run(async () =>
                                {
                                    response = await client.PostAsync(
                                        requestPath, content);
                                });
                                task.Wait();
                                error = !response.IsSuccessStatusCode;
                            }
                            catch
                            {
                                error = true;
                            }
                            if (!error)
                            {
                                s = response.Content.ReadAsStringAsync().Result;
                                vc.Value = s;

                                var parsed = JObject.Parse(s);

                                try
                                {
                                    var p = parsed.SelectToken("proof").Value<JObject>();
                                    if (p != null)
                                        proof.Value = JsonConvert.SerializeObject(p, Formatting.Indented);
                                }
                                catch
                                {
                                    error = true;
                                }
                            }
                            else
                            {
                                string r = "ERROR POST; " + response.StatusCode.ToString();
                                r += " ; " + requestPath;
                                if (response.Content != null)
                                    r += " ; " + response.Content.ReadAsStringAsync().Result;
                                Console.WriteLine(r);
                                s = r;
                            }
                            algorithm.Value = "VC";
                            sigT.Value = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
                        }
                    }
                    RedrawAllAasxElements();
                    RedrawElementView();
                    return;
                }

                if (cmd == "validatecertificate"
                    && ((el != null && el.theEnv != null && el.theSubmodel != null)
                            || (els != null && els.theEnv != null && els.theWrapper != null)))
                {
                    List<SubmodelElementCollection> existing = new List<SubmodelElementCollection>();
                    List<SubmodelElementCollection> validate = new List<SubmodelElementCollection>();
                    Submodel sm = null;
                    SubmodelElementCollection smc = null;
                    SubmodelElementCollection smcp = null;
                    bool smcIsSignature = false;
                    if (el != null)
                    {
                        sm = el.theSubmodel;
                    }
                    if (els != null)
                    {
                        var smee = els.theWrapper;
                        if (smee is SubmodelElementCollection)
                        {
                            smc = smee as SubmodelElementCollection;
                            var len = "signature".Length;
                            var idShort = smc.IdShort;
                            if (idShort.Length >= len &&
                                    idShort.Substring(0, len).ToLower() == "signature")
                            {
                                smcIsSignature = true;
                            }
                            var p = smc.Parent;
                            if (smcIsSignature && p is Submodel)
                                sm = p as Submodel;
                            if (smcIsSignature && p is SubmodelElementCollection)
                                smcp = p as SubmodelElementCollection;
                            if (!smcIsSignature)
                                smcp = smc;
                        }
                    }
                    if (sm == null && smcp == null)
                        return;

                    if (sm != null)
                    {
                        foreach (var sme in sm.SubmodelElements)
                        {
                            var smee = sme;
                            var len = "signature".Length;
                            var idShort = smee.IdShort;
                            if (smee is SubmodelElementCollection &&
                                    idShort.Length >= len &&
                                    idShort.Substring(0, len).ToLower() == "signature")
                            {
                                existing.Add(smee as SubmodelElementCollection);
                            }
                        }
                    }
                    if (smcp != null)
                    {
                        foreach (var sme in smcp.Value)
                        {
                            var len = "signature".Length;
                            var idShort = sme.IdShort;
                            if (sme is SubmodelElementCollection &&
                                    idShort.Length >= len &&
                                    idShort.Substring(0, len).ToLower() == "signature")
                            {
                                existing.Add(sme as SubmodelElementCollection);
                            }
                        }
                    }

                    if (smcIsSignature)
                    {
                        validate.Add(smc);
                    }
                    else
                    {
                        validate = existing;
                    }

                    if (validate.Count != 0)
                    {
                        X509Store root = new X509Store("Root", StoreLocation.CurrentUser);
                        root.Open(OpenFlags.ReadWrite);
                        List<X509Certificate2> rootList = new List<X509Certificate2>();

                        System.IO.DirectoryInfo ParentDirectory = new System.IO.DirectoryInfo(".");

                        // Add additional trusted root certificates temporarilly
                        if (Directory.Exists("./root"))
                        {
                            foreach (System.IO.FileInfo f in ParentDirectory.GetFiles("./root/*.cer"))
                            {
                                X509Certificate2 cert = new X509Certificate2("./root/" + f.Name);

                                try
                                {
                                    if (!root.Certificates.Contains(cert))
                                    {
                                        root.Add(cert);
                                        rootList.Add(cert);
                                    }
                                }
                                // ReSharper disable EmptyGeneralCatchClause
                                catch
                                {
                                }
                                // ReSharper enable EmptyGeneralCatchClause
                            }
                        }

                        if (smcp == null)
                        {
                            foreach (var e in existing)
                            {
                                sm.Remove(e);
                            }
                        }
                        else
                        {
                            foreach (var e in existing)
                            {
                                smcp.Remove(e);
                            }
                        }
                        foreach (var smec in validate)
                        {
                            SubmodelElementCollection x5c = null;
                            Property subject = null;
                            Property algorithm = null;
                            Property digest = null; // legacy
                            Property signature = null;

                            foreach (var sme in smec.Value)
                            {
                                var smee = sme;
                                switch (smee.IdShort)
                                {
                                    case "x5c":
                                        if (smee is SubmodelElementCollection)
                                            x5c = smee as SubmodelElementCollection;
                                        break;
                                    case "subject":
                                        subject = smee as Property;
                                        break;
                                    case "algorithm":
                                        algorithm = smee as Property;
                                        break;
                                    case "digest":
                                        digest = smee as Property;
                                        break;
                                    case "signature":
                                        signature = smee as Property;
                                        break;
                                }
                            }
                            if (smec != null && x5c != null && subject != null && algorithm != null &&
                                (signature != null || digest != null))
                            {
                                string s = null;
                                if (smcp == null)
                                {
                                    s = JsonConvert.SerializeObject(sm, Formatting.Indented);
                                }
                                else
                                {
                                    s = JsonConvert.SerializeObject(smcp, Formatting.Indented);
                                }
                                // OZOZ JsonCanonicalizer jsonCanonicalizer = new JsonCanonicalizer(s);
                                // OZOZ string result = jsonCanonicalizer.GetEncodedString();
                                string result = "";

                                X509Store storeCA = new X509Store("CA", StoreLocation.CurrentUser);
                                storeCA.Open(OpenFlags.ReadWrite);
                                X509Certificate2Collection xcc = new X509Certificate2Collection();
                                X509Certificate2 x509 = null;
                                bool valid = false;

                                try
                                {
                                    for (int i = 0; i < x5c.Value.Count; i++)
                                    {
                                        var p = x5c.Value[i] as Property;
                                        var cert = new X509Certificate2(Convert.FromBase64String(p.Value));
                                        if (i == 0)
                                        {
                                            x509 = cert;
                                        }
                                        if (cert.Subject != cert.Issuer)
                                        {
                                            xcc.Add(cert);
                                            storeCA.Add(cert);
                                        }
                                        if (cert.Subject == cert.Issuer)
                                        {
                                            try
                                            {
                                                if (!root.Certificates.Contains(cert))
                                                {
                                                    root.Add(cert);
                                                    rootList.Add(cert);
                                                }
                                            }
                                            // ReSharper disable EmptyGeneralCatchClause
                                            catch
                                            {
                                            }
                                            // ReSharper enable EmptyGeneralCatchClause
                                        }
                                    }

                                    if (x509 != null)
                                    {
                                        X509Chain c = new X509Chain();
                                        c.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                                        valid = c.Build(x509);
                                    }

                                    //// storeCA.RemoveRange(xcc);
                                }
                                catch
                                {
                                    x509 = null;
                                    valid = false;
                                }

                                if (!valid)
                                {
                                    System.Windows.MessageBox.Show(
                                        this, "Invalid certificate chain: " + subject.Value, "Check " + smec.IdShort,
                                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                                if (valid)
                                {
                                    valid = false;

                                    if (algorithm.Value == "RS256")
                                    {
                                        try
                                        {
                                            using (RSA rsa = x509.GetRSAPublicKey())
                                            {
                                                string value = null;
                                                if (signature != null)
                                                    value = signature.Value;
                                                if (digest != null)
                                                    value = digest.Value;
                                                byte[] data = Encoding.UTF8.GetBytes(result);
                                                byte[] h = Convert.FromBase64String(value);
                                                valid = rsa.VerifyData(data, h, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                                            }
                                        }
                                        catch
                                        {
                                            valid = false;
                                        }
                                        if (!valid)
                                        {
                                            System.Windows.MessageBox.Show(
                                                this, "Invalid signature: " + subject.Value, "Check " + smec.IdShort,
                                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                        }
                                        if (valid)
                                        {
                                            System.Windows.MessageBox.Show(
                                                this, "Signature is valid: " + subject.Value, "Check " + smec.IdShort,
                                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                        }
                                    }
                                }
                            }
                        }
                        if (smcp == null)
                        {
                            foreach (var e in existing)
                            {
                                sm.Add(e);
                            }
                        }
                        else
                        {
                            foreach (var e in existing)
                            {
                                smcp.Add(e);
                            }
                        }

                        // Delete additional trusted root certificates immediately
                        foreach (var cert in rootList)
                        {
                            try
                            {
                                root.Remove(cert);
                            }
                            // ReSharper disable EmptyGeneralCatchClause
                            catch
                            {
                            }
                            // ReSharper enable EmptyGeneralCatchClause
                        }
                    }
                    return;
                }

                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "AASX package files (*.aasx)|*.aasx";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res == true)
                {
                    if (cmd == "sign")
                    {
                        // OZOZ packageHelper.SignAll(dlg.FileName);
                    }
                    if (cmd == "validatecertificate")
                    {
                        // OZOZ PackageHelper.Validate(dlg.FileName);
                    }
                    if (cmd == "encrypt")
                    {
                        var dlg2 = new Microsoft.Win32.OpenFileDialog();
                        dlg2.Filter = ".cer files (*.cer)|*.cer";
                        if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                        res = dlg2.ShowDialog();
                        if (Options.Curr.UseFlyovers) this.CloseFlyover();

                        if (res == true)
                        {
                            try
                            {
                                X509Certificate2 x509 = new X509Certificate2(dlg2.FileName);
                                var publicKey = x509.GetRSAPublicKey();

                                Byte[] binaryFile = System.IO.File.ReadAllBytes(dlg.FileName);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                                string fileToken = Jose.JWT.Encode(
                                    payload, publicKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);
                                Byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileToken);

                                var dlg3 = new Microsoft.Win32.SaveFileDialog();
                                dlg3.Filter = "AASX2 package files (*.aasx2)|*.aasx2";
                                dlg3.FileName = dlg.FileName + "2";
                                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                                res = dlg3.ShowDialog();
                                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                                if (res == true)
                                {
                                    System.IO.File.WriteAllBytes(dlg3.FileName, fileBytes);
                                }
                            }
                            catch
                            {
                                System.Windows.MessageBox.Show(
                                    this, "Can not encrypt with " + dlg2.FileName, "Decrypt .AASX2",
                                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                    }
                }
            }
            if ((cmd == "decrypt") && _packageCentral.Main != null)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "AASX package files (*.aasx2)|*.aasx2";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res == true)
                {
                    if (cmd == "decrypt")
                    {
                        var dlg2 = new Microsoft.Win32.OpenFileDialog();
                        dlg2.Filter = ".pfx files (*.pfx)|*.pfx";
                        if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                        res = dlg2.ShowDialog();
                        if (Options.Curr.UseFlyovers) this.CloseFlyover();

                        if (res == true)
                        {
                            try
                            {
                                X509Certificate2 x509 = new X509Certificate2(dlg2.FileName, "i40");
                                var privateKey = x509.GetRSAPrivateKey();

                                Byte[] binaryFile = System.IO.File.ReadAllBytes(dlg.FileName);
                                string fileString = System.Text.Encoding.UTF8.GetString(binaryFile);

                                string fileString2 = Jose.JWT.Decode(
                                    fileString, privateKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);

                                var parsed0 = JObject.Parse(fileString2);
                                string binaryBase64_2 = parsed0.SelectToken("file").Value<string>();

                                Byte[] fileBytes2 = Convert.FromBase64String(binaryBase64_2);

                                var dlg4 = new Microsoft.Win32.SaveFileDialog();
                                dlg4.Filter = "AASX package files (*.aasx)|*.aasx";
                                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                                res = dlg4.ShowDialog();
                                if (Options.Curr.UseFlyovers) this.CloseFlyover();
                                if (res == true)
                                {
                                    System.IO.File.WriteAllBytes(dlg4.FileName, fileBytes2);
                                }
                            }
                            catch
                            {
                                System.Windows.MessageBox.Show(
                                    this, "Can not decrypt with " + dlg2.FileName, "Decrypt .AASX2",
                                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                    }
                }
            }

            if (cmd == "closeaux" && _packageCentral.AuxAvailable)
                try
                {
                    _packageCentral.AuxItem.Close();
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "When closing auxiliary AASX, an error occurred");
                }

            if (cmd == "exit")
                System.Windows.Application.Current.Shutdown();

            if (cmd == "connectopcua")
                MessageBoxFlyoutShow(
                    "In future versions, this feature will allow connecting to an online Administration Shell " +
                    "via OPC UA or similar.",
                    "Connect", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);

            if (cmd == "about")
            {
                var ab = new AboutBox(_pref);
                ab.ShowDialog();
            }

            if (cmd == "helpgithub")
            {
                ShowHelp();
            }

            if (cmd == "faqgithub")
            {
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell-io/questions-and-answers/blob/master/README.md");
            }

            if (cmd == "helpissues")
            {
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell-io/aasx-package-explorer/issues");
            }

            if (cmd == "helpoptionsinfo")
            {
                var st = Options.ReportOptions(Options.ReportOptionsFormat.Markdown, Options.Curr);
                var dlg = new MessageReportWindow(st,
                    windowTitle: "Report on active and possible options");
                dlg.ShowDialog();
            }

            if (cmd == "editkey")
                MenuItemWorkspaceEdit.IsChecked = !MenuItemWorkspaceEdit.IsChecked;

            if (cmd == "hintskey")
                MenuItemWorkspaceHints.IsChecked = !MenuItemWorkspaceHints.IsChecked;

            if (cmd == "showirikey")
                MenuItemOptionsShowIri.IsChecked = !MenuItemOptionsShowIri.IsChecked;

            if (cmd == "editmenu" || cmd == "editkey"
                || cmd == "hintsmenu" || cmd == "hintskey"
                || cmd == "showirimenu" || cmd == "showirikey")
            {
                // try to remember current selected data object
                object currMdo = null;
                if (DisplayElements.SelectedItem != null)
                    currMdo = DisplayElements.SelectedItem.GetMainDataObject();

                // edit mode affects the total element view
                RedrawAllAasxElements();
                // fake selection
                RedrawElementView();
                // select last object
                if (currMdo != null)
                {
                    DisplayElements.TrySelectMainDataObject(currMdo, wishExpanded: true);
                }
            }

            if (cmd == "test")
            {
                DisplayElements.Test();
            }

            if (cmd == "bufferclear")
            {
                DispEditEntityPanel.ClearPasteBuffer();
                Log.Singleton.Info("Internatl copy/ paste buffer cleared. Pasting of external JSON elements " +
                    "enabled.");
            }

            if (cmd == "exportsmd")
                CommandBinding_ExportSMD();

            if (cmd == "printasset")
                CommandBinding_PrintAsset();

            if (cmd.StartsWith("filerepo"))
                await CommandBinding_FileRepoAll(cmd);

            if (cmd == "opcread")
                CommandBinding_OpcUaClientRead();

            if (cmd == "submodelread")
                CommandBinding_SubmodelRead();

            if (cmd == "submodelwrite")
                CommandBinding_SubmodelWrite();

            if (cmd == "rdfread")
                CommandBinding_RDFRead();

            if (cmd == "submodelput")
                CommandBinding_SubmodelPut();

            if (cmd == "submodelget")
                CommandBinding_SubmodelGet();

            if (cmd == "bmecatimport")
                CommandBinding_BMEcatImport();

            if (cmd == "csvimport")
                CommandBinding_CSVImport();

            if (cmd == "tdimport")
                CommandBinding_TDImport();

            if (cmd == "submodeltdexport")
                CommandBinding_SubmodelTDExport();

            if (cmd == "opcuaimportnodeset")
                CommandBinding_OpcUaImportNodeSet();

            if (cmd == "importsubmodel")
                CommandBinding_ImportSubmodel();

            if (cmd == "importsubmodelelements")
                CommandBinding_ImportSubmodelElements();

            if (cmd == "importaml")
                CommandBinding_ImportAML();

            if (cmd == "exportaml")
                CommandBinding_ExportAML();

            if (cmd == "opcuai4aasexport")
                CommandBinding_ExportOPCUANodeSet();

            if (cmd == "opcuai4aasimport")
                CommandBinding_ImportOPCUANodeSet();

            if (cmd == "opcuaexportnodesetuaplugin")
                CommandBinding_ExportNodesetUaPlugin();

            if (cmd == "serverrest")
                CommandBinding_ServerRest();

            if (cmd == "mqttpub")
                CommandBinding_MQTTPub();

            if (cmd == "connectintegrated")
                CommandBinding_ConnectIntegrated();

            if (cmd == "connectsecure")
                CommandBinding_ConnectSecure();

            if (cmd == "connectrest")
                CommandBinding_ConnectRest();

            if (cmd == "copyclipboardelementjson")
                CommandBinding_CopyClipboardElementJson();

            if (cmd == "exportgenericforms")
                CommandBinding_ExportGenericForms();

            if (cmd == "exportpredefineconcepts")
                CommandBinding_ExportPredefineConcepts();

            if (cmd == "exporttable")
                CommandBinding_ExportImportTableUml(import: false);

            if (cmd == "importtable")
                CommandBinding_ExportImportTableUml(import: true);

            if (cmd == "exportuml")
                CommandBinding_ExportImportTableUml(exportUml: true);

            if (cmd == "importtimeseries")
                CommandBinding_ExportImportTableUml(importTimeSeries: true);

            if (cmd == "serverpluginemptysample")
                CommandBinding_ExecutePluginServer(
                    "EmptySample", "server-start", "server-stop", "Empty sample plug-in.");

            if (cmd == "serverpluginopcua")
                CommandBinding_ExecutePluginServer(
                    "AasxPluginUaNetServer", "server-start", "server-stop", "Plug-in for OPC UA Server for AASX.");

            if (cmd == "serverpluginmqtt")
                CommandBinding_ExecutePluginServer(
                    "AasxPluginMqttServer", "MQTTServer-start", "server-stop", "Plug-in for MQTT Server for AASX.");

            if (cmd == "newsubmodelfromplugin")
                CommandBinding_NewSubmodelFromPlugin();

            if (cmd == "convertelement")
                CommandBinding_ConvertElement();

            if (cmd == "toolsfindtext" || cmd == "toolsfindforward" || cmd == "toolsfindbackward")
                CommandBinding_ToolsFind(cmd);

            if (cmd == "checkandfix")
                CommandBinding_CheckAndFix();

            if (cmd == "eventsresetlocks")
            {
                Log.Singleton.Info($"Event interlocking reset. Status was: " +
                    $"update-value-pending={_eventHandling.UpdateValuePending}");

                _eventHandling.Reset();
            }

            if (cmd == "eventsshowlogkey")
                MenuItemWorkspaceEventsShowLog.IsChecked = !MenuItemWorkspaceEventsShowLog.IsChecked;

            if (cmd == "eventsshowlogkey" || cmd == "eventsshowlogmenu")
            {
                PanelConcurrentSetVisibleIfRequired(PanelConcurrentCheckIsVisible());
            }
        }

        public void CommandBinding_TDImport()
        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel is selected.", "Unable to import TD JSON LD Document",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
            dlg.Filter = "JSON files (*.JSONLD)|*.jsonld";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    JObject importObject = TDJsonImport.ImportTDJsontoSubModel
                        (dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    foreach (var temp in (JToken)importObject)
                    {
                        JProperty importProperty = (JProperty)temp;
                        string key = importProperty.Name.ToString();
                        if (key == "error")
                        {
                            MessageBoxFlyoutShow(
                            "Unable to Import the JSON LD File", "Check the log"
                            ,
                            AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                            Log.Singleton.Error(importProperty.Value.ToString(), "When importing the jsonld document");
                        }
                        else
                        {
                            // redisplay
                            RedrawAllAasxElements();
                            RedrawElementView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "When importing the jsonld document");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }
        public bool PanelConcurrentCheckIsVisible()
        {
            return MenuItemWorkspaceEventsShowLog.IsChecked;
        }

        public void PanelConcurrentSetVisibleIfRequired(
            bool targetState, bool targetAgents = false, bool targetEvents = false)
        {
            if (!targetState)
            {
                RowDefinitionConcurrent.Height = new GridLength(0);
            }
            else
            {
                if (RowDefinitionConcurrent.Height.Value < 1.0)
                {
                    var desiredH = Math.Max(140.0, this.Height / 3.0);
                    RowDefinitionConcurrent.Height = new GridLength(desiredH);
                }

                if (targetEvents)
                    TabControlConcurrent.SelectedItem = TabItemConcurrentEvents;

                if (targetAgents)
                    TabControlConcurrent.SelectedItem = TabItemConcurrentAgents;
            }
        }

        public void CommandBinding_CheckAndFix()
        {
            // work on package
            var msgBoxHeadline = "Check, validate and fix ..";
            var env = _packageCentral.Main?.AasEnv;
            if (env == null)
            {
                MessageBoxFlyoutShow(
                    "No package/ environment open. Aborting.", msgBoxHeadline,
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // try to get results
            AasValidationRecordList recs = null;
            try
            {
                // validate (logically)
                recs = env.ValidateAll();

                // validate as XML
                var ms = new MemoryStream();
                _packageCentral.Main.SaveAs("noname.xml", true, AdminShellPackageEnv.SerializationFormat.Xml, ms,
                    saveOnlyCopy: true);
                ms.Flush();
                ms.Position = 0;
                AasSchemaValidation.ValidateXML(recs, ms);
                ms.Close();

                // validate as JSON
                var ms2 = new MemoryStream();
                _packageCentral.Main.SaveAs("noname.json", true, AdminShellPackageEnv.SerializationFormat.Json, ms2,
                    saveOnlyCopy: true);
                ms2.Flush();
                ms2.Position = 0;
                AasSchemaValidation.ValidateJSONAlternative(recs, ms2);
                ms2.Close();
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Checking model contents");
                MessageBoxFlyoutShow(
                    "Error while checking model contents. Aborting.", msgBoxHeadline,
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // could be nothing
            if (recs.Count < 1)
            {
                MessageBoxFlyoutShow(
                   "No issues found. Done.", msgBoxHeadline,
                   AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                return;
            }

            // prompt for this list
            var uc = new ShowValidationResultsFlyout();
            uc.ValidationItems = recs;
            this.StartFlyoverModal(uc);
            if (uc.FixSelected)
            {
                // fix
                var fixes = recs.FindAll((r) =>
                {
                    var res = uc.DoHint && r.Severity == AasValidationSeverity.Hint
                        || uc.DoWarning && r.Severity == AasValidationSeverity.Warning
                        || uc.DoSpecViolation && r.Severity == AasValidationSeverity.SpecViolation
                        || uc.DoSchemaViolation && r.Severity == AasValidationSeverity.SchemaViolation;
                    return res;
                });

                int done = 0;
                try
                {
                    done = env.AutoFix(fixes);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Fixing model contents");
                    MessageBoxFlyoutShow(
                        "Error while fixing issues. Aborting.", msgBoxHeadline,
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // info
                MessageBoxFlyoutShow(
                   $"Corresponding {done} issues were fixed. Please check the changes and consider saving " +
                   "with a new filename.", msgBoxHeadline,
                   AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);

                // redraw
                CommandExecution_RedrawAll();
            }
        }

        public async Task CommandBinding_FileRepoAll(string cmd)
        {
            if (cmd == "filereponew")
            {
                if (AnyUiMessageBoxResult.OK != MessageBoxFlyoutShow(
                        "Create new (empty) file repository? It will be added to list of repos on the lower/ " +
                        "left of the screen.",
                        "AASX File Repository",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                    return;

                this.UiAssertFileRepository(visible: true);
                _packageCentral.Repositories.AddAtTop(new PackageContainerListLocal());
            }

            if (cmd == "filerepoopen")
            {
                // ask for the file
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                dlg.Filter = "JSON files (*.JSON)|*.json|All files (*.*)|*.*";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res == true)
                {
                    var fr = this.UiLoadFileRepository(dlg.FileName);
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral.Repositories.AddAtTop(fr);
                }
            }

            if (cmd == "filerepoconnectrepository")
            {
                // read server address
                var uc = new TextBoxFlyout("REST endpoint (without \"/server/listaas\"):",
                            AnyUiMessageBoxImage.Question);
                uc.Text = "" + Options.Curr.DefaultConnectRepositoryLocation;
                this.StartFlyoverModal(uc);
                if (!uc.Result)
                    return;

                if (uc.Text.Contains("asp.net"))
                {
                    var fileRepository = new PackageContainerAasxFileRepository(uc.Text);
                    fileRepository.GeneratePackageRepository();
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral.Repositories.AddAtTop(fileRepository);
                }
                else
                {
                    var fr = new PackageContainerListHttpRestRepository(uc.Text);
                    await fr.SyncronizeFromServerAsync();
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral.Repositories.AddAtTop(fr);
                }
            }

            if (cmd == "filerepoquery")
            {
                // access
                if (_packageCentral.Repositories == null || _packageCentral.Repositories.Count < 1)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently available! Please open.",
                        "AASX File Repository",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);

                    return;
                }

                // dialogue
                var uc = new SelectFromRepositoryFlyout();
                uc.Margin = new Thickness(10);
                if (uc.LoadAasxRepoFile(items: _packageCentral.Repositories.EnumerateItems()))
                {
                    uc.ControlClosed += () =>
                    {
                        var fi = uc.ResultItem;
                        var fr = _packageCentral.Repositories?.FindRepository(fi);

                        if (fr != null && fi?.Location != null)
                        {
                            // which file?
                            var loc = fr?.GetFullItemLocation(fi.Location);
                            if (loc == null)
                                return;

                            // start animation
                            fr.StartAnimation(fi,
                                PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                            try
                            {
                                // load
                                Log.Singleton.Info("Switching to AASX repository location {0} ..", loc);
                                UiLoadPackageWithNew(
                                    _packageCentral.MainItem, null, loc, onlyAuxiliary: false);
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"When switching to AASX repository location {loc}.");
                            }
                        }

                    };
                    this.StartFlyover(uc);
                }
            }

            if (cmd == "filerepocreatelru")
            {
                if (AnyUiMessageBoxResult.OK != MessageBoxFlyoutShow(
                        "Create new (empty) \"Last Recently Used (LRU)\" list? " +
                        "It will be added to list of repos on the lower/ left of the screen. " +
                        "It will be saved under \"last-recently-used.json\" in the binaries folder. " +
                        "It will replace an existing LRU list w/o prompt!",
                        "Last Recently Used AASX Packages",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                    return;

                var lruFn = PackageContainerListLastRecentlyUsed.BuildDefaultFilename();
                try
                {
                    this.UiAssertFileRepository(visible: true);
                    var lruExist = _packageCentral?.Repositories?.FindLRU();
                    if (lruExist != null)
                        _packageCentral.Repositories.Remove(lruExist);
                    var lruNew = new PackageContainerListLastRecentlyUsed();
                    lruNew.Header = "Last Recently Used";
                    lruNew.SaveAs(lruFn);
                    this.UiAssertFileRepository(visible: true);
                    _packageCentral?.Repositories?.AddAtTop(lruNew);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"while initializing last recently used file in {lruFn}.");
                }
            }

            // Note: rest of the commands migrated to AasxRepoListControl
        }

        public void CommandBinding_ConnectSecure()
        {
            // make dialgue flyout
            var uc = new SecureConnectFlyout();
            uc.LoadPresets(Options.Curr.SecureConnectPresets);

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
            });

            // succss?
            if (uc.Result == null)
                return;
            var preset = uc.Result;

            // make listing flyout
            var logger = new LogInstance();
            var uc2 = new LogMessageFlyout("Secure connecting ..", "Start secure connect ..", () =>
            {
                return logger.PopLastShortTermPrint();
            });
            uc2.EnableLargeScreen();

            // do some statistics
            Log.Singleton.Info("Start secure connect ..");
            Log.Singleton.Info("Protocol: {0}", preset.Protocol.Value);
            Log.Singleton.Info("AuthorizationServer: {0}", preset.AuthorizationServer.Value);
            Log.Singleton.Info("AasServer: {0}", preset.AasServer.Value);
            Log.Singleton.Info("CertificateFile: {0}", preset.CertificateFile.Value);
            Log.Singleton.Info("Password: {0}", preset.Password.Value);

            logger.Info("Protocol: {0}", preset.Protocol.Value);
            logger.Info("AuthorizationServer: {0}", preset.AuthorizationServer.Value);
            logger.Info("AasServer: {0}", preset.AasServer.Value);
            logger.Info("CertificateFile: {0}", preset.CertificateFile.Value);
            logger.Info("Password: {0}", preset.Password.Value);

            // start CONNECT as a worker (will start in the background)
            var worker = new BackgroundWorker();
            AdminShellPackageEnv envToload = null;
            worker.DoWork += (s1, e1) =>
            {
                for (int i = 0; i < 15; i++)
                {
                    var sb = new StringBuilder();
                    for (double j = 0; j < 1; j += 0.0025)
                        sb.Append($"{j}");
                    logger.Info("The output is: {0} gives {1} was {0}", i, sb.ToString());
                    logger.Info(StoredPrint.Color.Blue, "This is blue");
                    logger.Info(StoredPrint.Color.Red, "This is red");
                    logger.Error("This is an error!");
                    logger.InfoWithHyperlink(0, "This is an link", "(Link)", "https://www.google.de");
                    logger.Info("----");
                    Thread.Sleep(2134);
                }

                envToload = null;
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc2, closingAction: () =>
            {
                // clean up
            });

            // commit Package
            if (envToload != null)
            {
            }

            // done
            Log.Singleton.Info("Secure connect done.");
        }

        public void CommandBinding_ConnectIntegrated()
        {
            // make dialogue flyout
            var uc = new IntegratedConnectFlyout(
                _packageCentral,
                initialLocation: "" /* "http://admin-shell-io.com:51310/server/getaasx/0" */,
                logger: new LogInstance());
            uc.LoadPresets(Options.Curr.IntegratedConnectPresets);

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
            });

            // execute
            if (uc.Result && uc.ResultContainer != null)
            {
                Log.Singleton.Info($"For integrated connection, trying to take over " +
                    $"{uc.ResultContainer.ToString()} ..");
                try
                {
                    UiLoadPackageWithNew(
                        _packageCentral.MainItem, null, takeOverContainer: uc.ResultContainer, onlyAuxiliary: false);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When opening {uc.ResultContainer.ToString()}");
                }
            }
        }

        public void CommandBinding_PrintAsset()
        {
            AssetInformation asset = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementAsset)
            {
                var ve = DisplayElements.SelectedItem as VisualElementAsset;
                if (ve != null && ve.theAsset != null)
                    asset = ve.theAsset;
            }

            if (asset == null)
            {
                MessageBoxFlyoutShow(
                    "No asset selected for printing code sheet.", "Print code sheet",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            try
            {
                var id = asset.GlobalAssetId.GetAsIdentifier();
                if (id != null)
                {
                    //AasxPrintFunctions.PrintSingleAssetCodeSheet(id, asset.fakeIdShort); //TODO:jtikekar fakeIdShort?
                    AasxPrintFunctions.PrintSingleAssetCodeSheet(id, "AssetInformation");
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When printing, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ServerRest()
        {
            // OZOZ
            /*
            // make a logger
            var logger = new AasxRestServerLibrary.GrapevineLoggerToListOfStrings();

            // make listing flyout
            var uc = new LogMessageFlyout("AASX REST Server", "Starting REST server ..", () =>
            {
                var st = logger.Pop();
                return (st == null) ? null : new StoredPrint(st);
            });

            // start REST as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.DoWork += (s1, e1) =>
            {
                AasxRestServerLibrary.AasxRestServer.Start(
                    _packageCentral.Main, Options.Curr.RestServerHost, Options.Curr.RestServerPort, logger);
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
                AasxRestServerLibrary.AasxRestServer.Stop();
            });
            */
        }

        public class FlyoutAgentMqttPublisher : FlyoutAgentBase
        {
            // OZOZ 
            /*
            public AasxMqttClient.AnyUiDialogueDataMqttPublisher DiaData;
            public AasxMqttClient.GrapevineLoggerToStoredPrints Logger;
            public AasxMqttClient.MqttClient Client;
            public BackgroundWorker Worker;
            */
        }

        public void CommandBinding_MQTTPub()
        {
            //OZOZ 
            /*
            // make an agent
            var agent = new FlyoutAgentMqttPublisher();

            // ask for preferences
            agent.DiaData = AasxMqttClient.AnyUiDialogueDataMqttPublisher.CreateWithOptions("AASQ MQTT publisher ..",
                        jtoken: Options.Curr.MqttPublisherOptions);
            var uc1 = new MqttPublisherFlyout(agent.DiaData);
            this.StartFlyoverModal(uc1);
            if (!uc1.Result)
                return;

            // make a logger
            agent.Logger = new AasxMqttClient.GrapevineLoggerToStoredPrints();

            // make listing flyout
            var uc2 = new LogMessageFlyout("AASX MQTT Publisher", "Starting MQTT Client ..", () =>
            {
                var sp = agent.Logger.Pop();
                return sp;
            });
            uc2.Agent = agent;

            // start MQTT Client as a worker (will start in the background)
            agent.Client = new AasxMqttClient.MqttClient();
            agent.Worker = new BackgroundWorker();
            agent.Worker.DoWork += async (s1, e1) =>
            {
                try
                {
                    await agent.Client.StartAsync(_packageCentral.Main, agent.DiaData, agent.Logger);
                }
                catch (Exception e)
                {
                    agent.Logger.Error(e);
                }
            };
            agent.Worker.RunWorkerAsync();

            // wire events
            agent.EventTriggered += (ev) =>
            {
                // trivial
                if (ev == null)
                    return;

                // safe
                try
                {
                    // potentially expensive .. get more context for the event source
                    ReferableRootInfo foundRI = null;
                    if (_packageCentral != null && ev.Source?.Keys != null)
                        foreach (var pck in _packageCentral.GetAllPackageEnv())
                        {
                            var ri = new ReferableRootInfo();
                            var res = pck?.AasEnv?.FindReferableByReference(ev.Source.Keys, rootInfo: ri);
                            if (res != null && ri.IsValid)
                                foundRI = ri;
                        }

                    // publish
                    agent.Client?.PublishEvent(ev, foundRI);
                }
                catch (Exception e)
                {
                    agent.Logger.Error(e);
                }
            };

            agent.GenerateFlyoutMini = () =>
            {
                var storedAgent = agent;
                var mini = new LogMessageMiniFlyout("AASX MQTT Publisher", "Executing minimized ..", () =>
                {
                    var sp = storedAgent.Logger.Pop();
                    return sp;
                });
                mini.Agent = agent;
                return mini;
            };

            // modal dialogue
            this.StartFlyoverModal(uc2, closingAction: () => { });
            */
        }

        static string lastConnectInput = "";
        public async void CommandBinding_ConnectRest()
        {
            // OZOZ 
            /*
            var uc = new TextBoxFlyout("REST server adress:", AnyUiMessageBoxImage.Question);
            if (lastConnectInput == "")
            {
                uc.Text = "http://" + Options.Curr.RestServerHost + ":" + Options.Curr.RestServerPort;
            }
            else
            {
                uc.Text = lastConnectInput;
            }
            this.StartFlyoverModal(uc);
            if (uc.Result)
            {
                string value = "";
                string input = uc.Text.ToLower();
                lastConnectInput = input;
                if (!input.StartsWith("http://localhost:1111"))
                {
                    string tag = "";
                    bool connect = false;

                    if (input.Contains("/getaasxbyassetid/")) // get by AssetID
                    {
                        if (_packageCentral.MainAvailable)
                            _packageCentral.MainItem.Close();
                        File.Delete(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx");

                        var handler = new HttpClientHandler();
                        handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                        //// handler.AllowAutoRedirect = false;

                        string dataServer = new Uri(input).GetLeftPart(UriPartial.Authority);

                        var client = new HttpClient(handler)
                        {
                            BaseAddress = new Uri(dataServer)
                        };
                        input = input.Substring(dataServer.Length, input.Length - dataServer.Length);
                        client.DefaultRequestHeaders.Add("Accept", "application/aas");
                        var response2 = await client.GetAsync(input);

                        // ReSharper disable PossibleNullReferenceException
                        var contentStream = await response2?.Content?.ReadAsStreamAsync();
                        if (contentStream == null)
                            return;
                        // ReSharper enable PossibleNullReferenceException

                        string outputDir = ".";
                        Console.WriteLine("Writing file: " + outputDir + "\\" + "download.aasx");
                        using (var file = new FileStream(outputDir + "\\" + "download.aasx",
                            FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(file);
                        }

                        if (File.Exists(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx"))
                            UiLoadPackageWithNew(
                                _packageCentral.MainItem,
                                null,
                                AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx", onlyAuxiliary: false);
                        return;
                    }
                    else
                    {
                        tag = "http";
                        tag = input.Substring(0, tag.Length);
                        if (tag == "http")
                        {
                            connect = true;
                            tag = "openid ";
                            value = input;
                        }
                        else
                        {
                            tag = "openid1";
                            tag = input.Substring(0, tag.Length);
                            if (tag == "openid " || tag == "openid1" || tag == "openid2" || tag == "openid3")
                            {
                                connect = true;
                                value = input.Substring(tag.Length);
                            }
                        }
                    }

                    if (connect)
                    {
                        if (_packageCentral.MainAvailable)
                            _packageCentral.MainItem.Close();
                        File.Delete(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx");
                        await AasxOpenIdClient.OpenIDClient.Run(tag, value);

                        if (File.Exists(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx"))
                            UiLoadPackageWithNew(
                                _packageCentral.MainItem,
                                null,
                                AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx", onlyAuxiliary: false);
                    }
                }
                else
                {
                    var url = uc.Text;
                    Log.Singleton.Info($"Connecting to REST server {url} ..");

                    try
                    {
                        var client = new AasxRestServerLibrary.AasxRestClient(url);
                        theOnlineConnection = client;
                        var pe = client.OpenPackageByAasEnv();
                        if (pe != null)
                            UiLoadPackageWithNew(_packageCentral.MainItem, pe, info: uc.Text, onlyAuxiliary: false);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"Connecting to REST server {url}");
                    }
                }
            }
            */
        }

        public void CommandBinding_BMEcatImport()
        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected for BMEcat information.", "BMEcat import",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
            dlg.Filter = "BMEcat XML files (*.bmecat)|*.bmecat|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    BMEcatTools.ImportBMEcatToSubModel(dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "When importing BMEcat, an error occurred");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_CSVImport()
        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "CSV import", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
            dlg.Filter = "CSV files (*.CSV)|*.csv|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    CSVTools.ImportCSVtoSubModel(dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "When importing CSV, an error occurred");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_OpcUaImportNodeSet()
        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "Import", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
            dlg.Filter = "OPC UA NodeSet XML files (*.XML)|*.XML|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    OpcUaTools.ImportNodeSetToSubModel(dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "When importing, an error occurred");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        private void CommandBinding_ExecutePluginServer(
            string pluginName, string actionName, string stopName, string caption, string[] additionalArgs = null)
        {
            // check
            var pi = Plugins.FindPluginInstance(pluginName);
            if (pi == null || !pi.HasAction(actionName) || !pi.HasAction(stopName))
            {
                var res = MessageBoxFlyoutShow(
                        $"This function requires a binary plug-in file named '{pluginName}', " +
                        $"which needs to be added to the command line, with an action named '{actionName}'. " +
                        "Press 'OK' to show help page on GitHub.",
                        "Plug-in not present",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand);
                if (res == AnyUiMessageBoxResult.OK)
                {
                    ShowHelp();
                }
                return;
            }

            // activate server via plugin
            // make listing flyout
            var uc = new LogMessageFlyout(caption, $"Starting plug-in {pluginName}, action {actionName} ..", () =>
            {
                return this.FlyoutLoggingPop();
            });

            this.FlyoutLoggingStart();

            uc.ControlCloseWarnTime = 10000;
            uc.ControlWillBeClosed += () =>
            {
                uc.LogMessage("Initiating closing (wait at max 10sec) ..");
                pi.InvokeAction(stopName);
            };
            uc.AddPatternError(new Regex(@"^\[1\]"));

            // start server as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (s1, e1) =>
            {
                try
                {
                    // total argument list
                    var totalArgs = new List<string>();
                    if (pi.args != null)
                        totalArgs.AddRange(pi.args);
                    if (additionalArgs != null)
                        totalArgs.AddRange(additionalArgs);

                    // invoke
                    pi.InvokeAction(actionName, _packageCentral.Main, totalArgs.ToArray());

                }
                catch (Exception ex)
                {
                    uc.LogMessage("Exception in plug-in: " + ex.Message + " in " + ex.StackTrace);
                    uc.LogMessage("Stopping...");
                    Thread.Sleep(5000);
                }
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
                // in any case, close flyover
                this.FlyoutLoggingStop();
                uc.LogMessage("Completed.");
                uc.CloseControlExplicit();
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
#if FALSE
                if (false && worker.IsBusy)
                    try
                    {
                        worker.CancelAsync();
                        worker.Dispose();
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
#endif
            });
        }

        public void CommandBinding_SubmodelWrite()
        {
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "Submodel Write",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }
            var obj = ve1.theSubmodel;

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
            dlg.FileName = "Submodel_" + obj.IdShort + ".json";
            dlg.Filter = "JSON files (*.JSON)|*.json|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
            {
                RememberForInitialDirectory(dlg.FileName);
                using (var s = new StreamWriter(dlg.FileName))
                {
                    var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    s.WriteLine(json);
                }
            }
            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_SubmodelRead()
        {
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "Submodel Read",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }
            var obj = ve1.theSubmodel;

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.FileName = "Submodel_" + obj.IdShort + ".json";
            dlg.Filter = "JSON files (*.JSON)|*.json|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();

            if (res == true)
            {
                var aas = _packageCentral.Main.AasEnv.FindAasWithSubmodelId(obj.Id);

                // de-serialize Submodel
                Submodel submodel = null;

                try
                {
                    RememberForInitialDirectory(dlg.FileName);
                    using (StreamReader file = System.IO.File.OpenText(dlg.FileName))
                    {
                        ITraceWriter tw = new MemoryTraceWriter();
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.TraceWriter = tw;
                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        submodel = (Submodel)serializer.Deserialize(file, typeof(Submodel));
                    }
                }
                catch (Exception)
                {
                    MessageBoxFlyoutShow(
                        "Can not read SubModel.", "Submodel Read",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // need id for idempotent behaviour
                if (submodel == null || submodel.Id == null)
                {
                    MessageBoxFlyoutShow(
                        "Identification of SubModel is (null).", "Submodel Read",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // datastructure update
                if (_packageCentral.Main?.AasEnv?.AssetAdministrationShells == null)
                {
                    MessageBoxFlyoutShow(
                        "Error accessing internal data structures.", "Submodel Read",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }


                // add Submodel
                var existingSm = _packageCentral.Main.AasEnv.FindSubmodelById(submodel.Id);
                if (existingSm != null)
                    _packageCentral.Main.AasEnv.Submodels.Remove(existingSm);
                _packageCentral.Main.AasEnv.Submodels.Add(submodel);

                // add SubmodelRef to AAS
                // access the AAS
                var newsmr = new Reference(ReferenceTypes.GlobalReference, new List<AasCore.Aas3_0_RC02.Key>() { new AasCore.Aas3_0_RC02.Key(KeyTypes.Submodel, submodel.Id)});

                var existsmr = aas.HasSubmodelReference(newsmr);
                if (!existsmr)
                {
                    aas.AddSubmodelReference(newsmr);
                }
                RedrawAllAasxElements();
                RedrawElementView();
            }
            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        static string PUTURL = "http://???:51310";

        public void CommandBinding_SubmodelPut()
        {
            // OZOZ 
            /*
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "PUT Submodel",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                return;
            }

            var input = new TextBoxFlyout("REST server adress:", AnyUiMessageBoxImage.Question);
            input.Text = PUTURL;
            this.StartFlyoverModal(input);
            if (!input.Result)
            {
                return;
            }
            PUTURL = input.Text;
            Log.Singleton.Info($"Connecting to REST server {PUTURL} ..");

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "PUT Submodel",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }
            var obj = ve1.theSubmodel;
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);

            try
            {
                var client = new AasxRestServerLibrary.AasxRestClient(PUTURL);
                client.PutSubmodelAsync(json);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"Connecting to REST server {PUTURL}");
            }
            */
        }

        static string GETURL = "http://???:51310";

        public void CommandBinding_SubmodelGet()
        {
            // OZOZ 
            /*
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "GET Submodel",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                return;
            }

            var input = new TextBoxFlyout("REST server adress:", AnyUiMessageBoxImage.Question);
            input.Text = GETURL;
            this.StartFlyoverModal(input);
            if (!input.Result)
            {
                return;
            }
            GETURL = input.Text;
            Log.Singleton.Info($"Connecting to REST server {GETURL} ..");

            var obj = ve1.theSubmodel;
            var sm = "";
            try
            {
                var client = new AasxRestServerLibrary.AasxRestClient(GETURL);
                sm = client.GetSubmodel(obj.IdShort);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"Connecting to REST server {GETURL}");
            }

            {
                var aas = _packageCentral.Main.AasEnv.FindAASwithSubmodel(obj.identification);

                // de-serialize Submodel
                Submodel submodel = null;

                try
                {
                    using (TextReader reader = new StringReader(sm))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        submodel = (Submodel)serializer.Deserialize(reader, typeof(Submodel));
                    }
                }
                catch (Exception)
                {
                    MessageBoxFlyoutShow(
                        "Can not read SubModel.", "Submodel Read",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // need id for idempotent behaviour
                if (submodel == null || submodel.identification == null)
                {
                    MessageBoxFlyoutShow(
                        "Identification of SubModel is (null).", "Submodel Read",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // datastructure update
                if (_packageCentral.Main?.AasEnv?.Assets == null)
                {
                    MessageBoxFlyoutShow(
                        "Error accessing internal data structures.", "Submodel Read",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // add Submodel
                var existingSm = _packageCentral.Main.AasEnv.FindSubmodel(submodel.identification);
                if (existingSm != null)
                    _packageCentral.Main.AasEnv.Submodels.Remove(existingSm);
                _packageCentral.Main.AasEnv.Submodels.Add(submodel);

                // add SubmodelRef to AAS
                // access the AAS
                var newsmr = SubmodelRef.CreateNew(
                    "Submodel", true, submodel.identification.idType, submodel.identification.Id);
                var existsmr = aas.HasSubmodelRef(newsmr);
                if (!existsmr)
                {
                    aas.AddSubmodelRef(newsmr);
                }
                RedrawAllAasxElements();
                RedrawElementView();
            }
            */
        }

        public void CommandBinding_OpcUaClientRead()
        {
            // OZ
            {
                VisualElementSubmodelRef ve1 = null;
                if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                    ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

                if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
                {
                    MessageBoxFlyoutShow(
                        "No valid SubModel selected for OPC import.", "OPC import",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                try
                {

                    // Durch das Submodel iterieren
                    {
                        int count = ve1.theSubmodel.Qualifiers.Count;
                        if (count != 0)
                        {
                            int stopTimeout = Timeout.Infinite;
                            bool autoAccept = true;
                            // Variablen aus AAS Qualifiern
                            string Username = "";
                            string Password = "";
                            string URL = "";
                            int Namespace = 0;
                            string Path = "";

                            int i = 0;


                            while (i < 5 && i < count) // URL, Username, Password, Namespace, Path
                            {
                                var p = ve1.theSubmodel.Qualifiers[i];

                                switch (i)
                                {
                                    case 0: // URL
                                        if (p.Type == "OPCURL")
                                        {
                                            URL = p.Value;
                                        }
                                        break;
                                    case 1: // Username
                                        if (p.Type == "OPCUsername")
                                        {
                                            Username = p.Value;
                                        }
                                        break;
                                    case 2: // Password
                                        if (p.Type == "OPCPassword")
                                        {
                                            Password = p.Value;
                                        }
                                        break;
                                    case 3: // Namespace
                                        if (p.Type == "OPCNamespace")
                                        {
                                            Namespace = int.Parse(p.Value);
                                        }
                                        break;
                                    case 4: // Path
                                        if (p.Type == "OPCPath")
                                        {
                                            Path = p.Value;
                                        }
                                        break;
                                }
                                i++;
                            }

                            if (URL == "" || Username == "" || Password == "" || Namespace == 0 || Path == "")
                            {
                                return;
                            }

                            // find OPC plug-in
                            var pi = Plugins.FindPluginInstance("AasxPluginOpcUaClient");
                            if (pi == null || !pi.HasAction("create-client") || !pi.HasAction("read-sme-value"))
                            {
                                Log.Singleton.Error(
                                    "No plug-in 'AasxPluginOpcUaClient' with appropriate " +
                                    "actions 'create-client()', 'read-sme-value()' found.");
                                return;
                            }

                            // create client
                            // ReSharper disable ConditionIsAlwaysTrueOrFalse
                            var resClient =
                                pi.InvokeAction(
                                    "create-client", URL, autoAccept, stopTimeout,
                                    Username, Password) as AasxPluginResultBaseObject;
                            // ReSharper enable ConditionIsAlwaysTrueOrFalse
                            if (resClient == null || resClient.obj == null)
                            {
                                Log.Singleton.Error(
                                    "Plug-in 'AasxPluginOpcUaClient' cannot create client access!");
                                return;
                            }

                            // over all SMEs
                            count = ve1.theSubmodel.SubmodelElements.Count;
                            i = 0;
                            while (i < count)
                            {
                                if (ve1.theSubmodel.SubmodelElements[i] is Property)
                                {
                                    // access data
                                    var p = ve1.theSubmodel.SubmodelElements[i] as Property;
                                    var nodeName = "" + Path + p?.IdShort;

                                    // do read() via plug-in
                                    var resValue = pi.InvokeAction(
                                        "read-sme-value", resClient.obj,
                                        nodeName, Namespace) as AasxPluginResultBaseObject;

                                    // set?
                                    if (resValue != null && resValue.obj != null && resValue.obj is string)
                                    {
                                        var value = (string)resValue.obj;
                                        p.Value = value;
                                    }
                                }
                                i++;
                            }
                        }

                        RedrawAllAasxElements();
                        RedrawElementView();
                    }

                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "executing OPC UA client");
                }
            }

        }

        public void CommandBinding_ImportSubmodel()
        {
            AasCore.Aas3_0_RC02.Environment env = _packageCentral.Main.AasEnv;
            AssetAdministrationShell aas = null;
            if (DisplayElements.SelectedItem != null)
            {
                if (DisplayElements.SelectedItem is VisualElementAdminShell aasItem)
                {
                    // AAS is selected --> import into AAS
                    env = aasItem.theEnv;
                    aas = aasItem.theAas;
                }
                else if (DisplayElements.SelectedItem is VisualElementEnvironmentItem envItem &&
                        envItem.theItemType == VisualElementEnvironmentItem.ItemType.EmptySet)
                {
                    // Empty environment is selected --> create new AAS
                    env = envItem.theEnv;
                }
                else
                {
                    // Other element is selected --> error
                    MessageBoxFlyoutShow("Please select the administration shell for the submodel import.",
                        "Submodel Import", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }
            }

#if !DoNotUseAasxDictionaryImport
            var dataChanged = false;
            try
            {
                // OZOZ dataChanged = AasxDictionaryImport.Import.ImportSubmodel(this, env, Options.Curr.DictImportDir, aas);
            }
            catch (Exception e)
            {
                Log.Singleton.Error(e, "An error occurred during the submodel import.");
            }

            if (dataChanged)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                RestartUIafterNewPackage();
                Mouse.OverrideCursor = null;
            }
#endif
        }

        public void CommandBinding_ImportSubmodelElements()
        {
            AasCore.Aas3_0_RC02.Environment env = null;
            Submodel submodel = null;
            if (DisplayElements.SelectedItem is VisualElementSubmodel ves)
            {
                env = ves.theEnv;
                submodel = ves.theSubmodel;
            }
            else if (DisplayElements.SelectedItem is VisualElementSubmodelRef vesr)
            {
                env = vesr.theEnv;
                submodel = vesr.theSubmodel;
            }
            else
            {
                MessageBoxFlyoutShow("Please select the submodel for the submodel element import.",
                    "Submodel Element Import", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

#if !DoNotUseAasxDictionaryImport
            var dataChanged = false;
            try
            {
                // OZOZ dataChanged = AasxDictionaryImport.Import.ImportSubmodelElements(this, env, Options.Curr.DictImportDir,
                // OZOZ submodel);
            }
            catch (Exception e)
            {
                Log.Singleton.Error(e, "An error occurred during the submodel element import.");
            }

            if (dataChanged)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                RestartUIafterNewPackage();
                Mouse.OverrideCursor = null;
            }
#endif
        }

        public void CommandBinding_ImportAML()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select AML file to be imported";
            dlg.Filter = "AutomationML files (*.aml)|*.aml|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    AasxAmlImExport.AmlImport.ImportInto(_packageCentral.Main, dlg.FileName);
                    this.RestartUIafterNewPackage();
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When importing AML, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_RDFRead()

        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "Import", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
            dlg.Title = "Select RDF file to be imported";
            dlg.Filter = "BAMM files (*.ttl)|*.ttl|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    // OZOZ AasxBammRdfImExport.BAMMRDFimport.ImportInto(
                    // OZOZ dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "When importing, an error occurred");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }



        public void CommandBinding_ExportAML()
        {
            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select AML file to be exported";
            dlg.FileName = "new.aml";
            dlg.DefaultExt = "*.aml";
            dlg.Filter =
                "AutomationML files (*.aml)|*.aml|AutomationML files (*.aml) (compact)|" +
                "*.aml|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    AasxAmlImExport.AmlExport.ExportTo(
                        _packageCentral.Main, dlg.FileName, tryUseCompactProperties: dlg.FilterIndex == 2);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When exporting AML, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ExportNodesetUaPlugin()
        {
            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select Nodeset2.XML file to be exported";
            dlg.FileName = "new.xml";
            dlg.DefaultExt = "*.xml";
            dlg.Filter = "OPC UA Nodeset2 files (*.xml)|*.xml|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    CommandBinding_ExecutePluginServer(
                        "AasxPluginUaNetServer",
                        "server-start",
                        "server-stop",
                        "Export Nodeset2 via OPC UA Server...",
                        new[] { "-export-nodeset", dlg.FileName }
                        );
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, "When exporting UA nodeset via plug-in, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_CopyClipboardElementJson()
        {
            // get the selected element
            var ve = DisplayElements.SelectedItem;

            // allow only some elements
            if (!(ve is VisualElementConceptDescription
                || ve is VisualElementSubmodelElement
                || ve is VisualElementAdminShell
                || ve is VisualElementAsset
                || ve is VisualElementOperationVariable
                || ve is VisualElementSubmodel
                || ve is VisualElementSubmodelRef))
                ve = null;

            // need to have business object
            var mdo = ve?.GetMainDataObject();

            if (ve == null || mdo == null)
            {
                MessageBoxFlyoutShow(
                    "No valid element selected.", "Copy selected elements",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // ok, for Serialization we just want the plain element with no BLOBs..
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new AdminShellConverters.AdaptiveFilterContractResolver(
                deep: false, complete: false);
            var jsonStr = JsonConvert.SerializeObject(mdo, Formatting.Indented, settings);

            // copy to clipboard
            if (jsonStr != null && jsonStr != "")
            {
                System.Windows.Clipboard.SetText(jsonStr);
                Log.Singleton.Info("Copied selected element to clipboard.");
            }
            else
            {
                Log.Singleton.Info("No JSON text could be generated for selected element.");
            }
        }

        public void CommandBinding_ExportGenericForms()
        {
            // trivial things
            if (!_packageCentral.MainStorable)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                return;
            }

            // a SubmodelRef shall be exported
            VisualElementSubmodelRef ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected for exporting options file for GenericForms.", "Generic Forms",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select options file for GenericForms to be exported";
            dlg.FileName = "new.add-options.json";
            dlg.DefaultExt = "*.add-options.json";
            dlg.Filter = "options file for GenericForms (*.add-options.json)|*.add-options.json|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    Log.Singleton.Info(
                        "Exporting add-options file to GenericForm: {0}", dlg.FileName);
                    RememberForInitialDirectory(dlg.FileName);
                    AasxIntegrationBase.AasForms.AasFormUtils.ExportAsGenericFormsOptions(
                        ve1.theEnv, ve1.theSubmodel, dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, "When exporting options file for GenericForms, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ExportPredefineConcepts()
        {
            // trivial things
            if (!_packageCentral.MainAvailable)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                return;
            }

            // a SubmodelRef shall be exported
            VisualElementSubmodelRef ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected for exporting snippets.", "Snippets for PredefinedConcepts",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select text file for PredefinedConcepts to be exported";
            dlg.FileName = "new.txt";
            dlg.DefaultExt = "*.txt";
            dlg.Filter = "Text file for PredefinedConcepts (*.txt)|*.txt|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    Log.Singleton.Info(
                        "Exporting text snippets for PredefinedConcepts: {0}", dlg.FileName);
                    AasxPredefinedConcepts.ExportPredefinedConcepts.Export(
                        _packageCentral.Main.AasEnv, ve1.theSubmodel, dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, "When exporting text snippets for PredefinedConcepts, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ConvertElement()
        {
            // trivial things
            if (!_packageCentral.MainStorable)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open for storage", "Error",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                return;
            }

            // a Referable shall be exported
            IReferable rf = null;
            object bo = null;
            if (DisplayElements.SelectedItem != null)
            {
                bo = DisplayElements.SelectedItem.GetMainDataObject();
                rf = DisplayElements.SelectedItem.GetDereferencedMainDataObject() as IReferable;
            }

            if (rf == null)
            {
                MessageBoxFlyoutShow(
                    "No valid Referable selected for conversion.", "Convert Referable",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // try to get offers
            var offers = AasxPredefinedConcepts.Convert.ConvertPredefinedConcepts.CheckForOffers(rf);
            if (offers == null || offers.Count < 1)
            {
                MessageBoxFlyoutShow(
                    "No valid conversion offers found for this Referable. Aborting.", "Convert Referable",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // convert these to list items
            var fol = new List<AnyUiDialogueListItem>();
            foreach (var o in offers)
                fol.Add(new AnyUiDialogueListItem(o.OfferDisplay, o));

            // show a list
            // prompt for this list
            var uc = new SelectFromListFlyout();
            uc.DiaData.Caption = "Select Conversion action to be executed ..";
            uc.DiaData.ListOfItems = fol;
            this.StartFlyoverModal(uc);
            if (uc.DiaData.ResultItem != null && uc.DiaData.ResultItem.Tag != null &&
                uc.DiaData.ResultItem.Tag is AasxPredefinedConcepts.Convert.ConvertOfferBase)
                try
                {
                    {
                        var offer = uc.DiaData.ResultItem.Tag as AasxPredefinedConcepts.Convert.ConvertOfferBase;
                        offer?.Provider?.ExecuteOffer(
                            _packageCentral.Main, rf, offer, deleteOldCDs: true, addNewCDs: true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Executing user defined conversion");
                }

            // redisplay
            // add to "normal" event quoue
            DispEditEntityPanel.AddWishForOutsideAction(new AnyUiLambdaActionRedrawAllElements(bo));
        }

        public void CommandBinding_ExportImportTableUml(
            bool import = false, bool exportUml = false, bool importTimeSeries = false)
        {
            // trivial things
            if (!_packageCentral.MainAvailable)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                return;
            }

            // a SubmodelRef shall be exported/ imported
            VisualElementSubmodelRef ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid Submodel selected for exporting/ importing.", "Export table/ UML/ time series",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // check, if required plugin can be found
            var pluginName = "AasxPluginExportTable";
            var actionName = (!import) ? "export-submodel" : "import-submodel";
            if (exportUml)
                actionName = "export-uml";
            if (importTimeSeries)
                actionName = "import-time-series";
            var pi = Plugins.FindPluginInstance(pluginName);
            if (pi == null || !pi.HasAction(actionName))
            {
                var res = MessageBoxFlyoutShow(
                        $"This function requires a binary plug-in file named '{pluginName}', " +
                        $"which needs to be added to the command line, with an action named '{actionName}'. " +
                        $"Press 'OK' to show help page on GitHub.",
                        "Plug-in not present",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand);
                if (res == AnyUiMessageBoxResult.OK)
                {
                    ShowHelp();
                }
                return;
            }

            // try activate plugin
            pi.InvokeAction(actionName, this, ve1.theEnv, ve1.theSubmodel);

            // redraw
            CommandExecution_RedrawAll();
        }

        public void CommandBinding_SubmodelTDExport()
        {
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel is selected.", "Unable to create TD JSON LD document",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }
            var obj = ve1.theSubmodel;

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(_packageCentral.MainItem.Filename);
            dlg.FileName = "Submodel_" + obj.IdShort + ".jsonld";
            dlg.Filter = "JSON files (*.JSONLD)|*.jsonld";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
            {
                JObject exportData = TDJsonExport.ExportSMtoJson(ve1.theSubmodel);
                if (exportData["status"].ToString() == "success")
                {
                    RememberForInitialDirectory(dlg.FileName);
                    using (var s = new StreamWriter(dlg.FileName))
                    {
                        string output = Newtonsoft.Json.JsonConvert.SerializeObject(exportData["data"],
                            Newtonsoft.Json.Formatting.Indented);
                        s.WriteLine(output);
                    }
                }
                else
                {
                    MessageBoxFlyoutShow(
                            "Unable to Import the JSON LD File", exportData["data"].ToString(),
                            AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                }

            }
            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_NewSubmodelFromPlugin()
        {
            // trivial things
            if (!_packageCentral.MainStorable)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open for storage", "Error"
                    , AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                return;
            }

            // an AAS needs to be selected
            VisualElementAdminShell ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementAdminShell)
                ve1 = DisplayElements.SelectedItem as VisualElementAdminShell;

            if (ve1 == null || ve1.theAas == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid AAS selected for creating a new Submodel.", "New Submodel from plugins",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // create a list of plugins, which are capable of generating Submodels
            var listOfSm = new List<AnyUiDialogueListItem>();
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
                                    listOfSm.Add(new AnyUiDialogueListItem(
                                        "" + lpi.name + " | " + "" + smname,
                                        new Tuple<Plugins.PluginInstance, string>(lpi, smname)
                                        ));
                        }
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
            }

            // could be nothing
            if (listOfSm.Count < 1)
            {
                MessageBoxFlyoutShow(
                    "No plugins generating Submodels found. Aborting.", "New Submodel from plugins",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                return;
            }

            // prompt for this list
            var uc = new SelectFromListFlyout();
            uc.DiaData.Caption = "Select Plug-in and Submodel to be generated ..";
            uc.DiaData.ListOfItems = listOfSm;
            this.StartFlyoverModal(uc);
            if (uc.DiaData.ResultItem != null && uc.DiaData.ResultItem.Tag != null &&
                uc.DiaData.ResultItem.Tag is Tuple<Plugins.PluginInstance, string>)
            {
                // get result arguments
                var TagTuple = uc.DiaData.ResultItem.Tag as Tuple<Plugins.PluginInstance, string>;
                var lpi = TagTuple?.Item1;
                var smname = TagTuple?.Item2;
                if (lpi == null || smname == null || smname.Length < 1)
                {
                    MessageBoxFlyoutShow(
                        "Error accessing plugins. Aborting.", "New Submodel from plugins",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                // try to invoke plugin to get submodel
                Submodel smres = null;
                List<ConceptDescription> cdres = null;
                try
                {
                    var res = lpi.InvokeAction("generate-submodel", smname) as AasxPluginResultBase;
                    if (res is AasxPluginResultBaseObject rbo)
                    {
                        smres = rbo.obj as Submodel;
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
                    MessageBoxFlyoutShow(
                        "Error accessing plugins. Aborting.", "New Submodel from plugins",
                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Submodel needs an identification
                    smres.Id = "";
                    if (smres.Kind == null || smres.Kind == ModelingKind.Instance)
                        smres.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelInstance);
                    else
                        smres.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelTemplate);

                    // add Submodel
                    var smref = smres.GetReference().Copy();
                    ve1.theAas.Submodels.Add(smref);
                    _packageCentral.Main.AasEnv.Submodels.Add(smres);

                    // add ConceptDescriptions?
                    if (cdres != null && cdres.Count > 0)
                    {
                        int nr = 0;
                        foreach (var cd in cdres)
                        {
                            if (cd == null || cd.Id == null)
                                continue;
                            var cdFound = ve1.theEnv.FindConceptDescriptionById(cd.Id);
                            if (cdFound != null)
                                continue;
                            // ok, add
                            var newCd = cd.Copy();
                            ve1.theEnv.ConceptDescriptions.Add(newCd);
                            nr++;
                        }
                        Log.Singleton.Info(
                            $"added {nr} ConceptDescritions for Submodel {smres.IdShort}.");
                    }

                    // redisplay
                    // add to "normal" event quoue
                    DispEditEntityPanel.AddWishForOutsideAction(new AnyUiLambdaActionRedrawAllElements(smref));
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "when adding Submodel to AAS");
                }
            }
        }

        public void CommandBinding_ToolsFind(string cmd)
        {
            // access
            if (ToolsGrid == null || TabControlTools == null || TabItemToolsFind == null || ToolFindReplace == null)
                return;

            if (cmd == "toolsfindtext")
            {
                // make panel visible
                ToolsGrid.Visibility = Visibility.Visible;
                TabControlTools.SelectedItem = TabItemToolsFind;

                // set the link to the AAS environment
                // Note: dangerous, as it might change WHILE the find tool is opened!
                ToolFindReplace.TheAasEnv = _packageCentral.Main?.AasEnv;

                // cursor
                ToolFindReplace.FocusFirstField();
            }

            if (cmd == "toolsfindforward")
                ToolFindReplace.FindForward();

            if (cmd == "toolsfindbackward")
                ToolFindReplace.FindBackward();
        }

        public void CommandBinding_ImportOPCUANodeSet()
        {
            // OZOZ 
            /*
            //choose File to import to
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML File (.xml)|*.xml|Text documents (.txt)|*.txt"; // Filter files by extension

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var result = dlg.ShowDialog();

            if (result == true)
            {
                RememberForInitialDirectory(dlg.FileName);
                UANodeSet InformationModel = UANodeSetExport.getInformationModel(dlg.FileName);
                _packageCentral.MainItem.TakeOver(UANodeSetImport.Import(InformationModel));
                RestartUIafterNewPackage();
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
            */
        }

        public void CommandBinding_ExportOPCUANodeSet()
        {
            // OZOZ 
            /*
            // try to access I4AAS export information
            UANodeSet InformationModel = null;
            try
            {
                var xstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "AasxPackageExplorer.Resources.i4AASCS.xml");

                InformationModel = UANodeSetExport.getInformationModel(xstream);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "when accessing i4AASCS.xml mapping types.");
                return;
            }
            Log.Singleton.Info("Mapping types loaded.");

            // ReSharper enable PossibleNullReferenceException
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                dlg.Title = "Select Nodeset file to be exported";
                dlg.FileName = "new.xml";
                dlg.DefaultExt = "*.xml";
                dlg.Filter = "XML File (.xml)|*.xml|Text documents (.txt)|*.txt";

                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = true == dlg.ShowDialog(this);
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
                if (!res)
                    return;

                RememberForInitialDirectory(dlg.FileName);

                UANodeSetExport.root = InformationModel.Items.ToList();

                foreach (Asset ass in _packageCentral.Main.AasEnv.Assets)
                {
                    UANodeSetExport.CreateAAS(ass.IdShort, _packageCentral.Main.AasEnv);
                }

                InformationModel.Items = UANodeSetExport.root.ToArray();

                using (var writer = new System.IO.StreamWriter(dlg.FileName))
                {
                    var serializer = new XmlSerializer(InformationModel.GetType());
                    serializer.Serialize(writer, InformationModel);
                    writer.Flush();
                }

                Log.Singleton.Info("i4AAS based OPC UA mapping exported: " + dlg.FileName);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "when exporting i4AAS based OPC UA mapping.");
            }
            */
        }

        public void CommandBinding_ExportSMD()
        {
            // OZOZ 
            /*
            // trivial things
            if (!_packageCentral.MainStorable)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                return;
            }
            // check, if required plugin can be found
            var pluginName = "AasxPluginSmdExporter";
            var actionName = "generate-SMD";
            var pi = Plugins.FindPluginInstance(pluginName);
            if (pi == null || !pi.HasAction(actionName))
            {
                var res = MessageBoxFlyoutShow(
                        $"This function requires a binary plug-in file named '{pluginName}', " +
                        $"which needs to be added to the command line, with an action named '{actionName}'." +
                        $"Press 'OK' to show help page on GitHub.",
                        "Plug-in not present",
                        AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand);
                if (res == AnyUiMessageBoxResult.OK)
                {
                    ShowHelp();
                }
                return;
            }
            //-----------------------------------
            // make a logger
            var logger = new AasxRestServerLibrary.GrapevineLoggerToListOfStrings();

            AasxRestServerLibrary.AasxRestServer.Start(_packageCentral.Main,
                                                        Options.Curr.RestServerHost,
                                                        Options.Curr.RestServerPort,
                                                        logger);

            Queue<string> stack = new Queue<string>();

            // Invoke Plugin
            var ret = pi.InvokeAction(actionName,
                                      this,
                                      stack,
                                      $"http://{Options.Curr.RestServerHost}:{Options.Curr.RestServerPort}/",
                                      "");

            if (ret == null) return;

            // make listing flyout
            var uc = new LogMessageFlyout("SMD Exporter", "Generating SMD ..", () =>
            {
                string st;
                if (stack.Count != 0)
                    st = stack.Dequeue();
                else
                    st = null;
                return (st == null) ? null : new StoredPrint(st);
            });

            this.StartFlyoverModal(uc, closingAction: () =>
            {
                AasxRestServerLibrary.AasxRestServer.Stop();
            });
            //--------------------------------
            // Redraw for changes to be visible
            RedrawAllAasxElements();
            //-----------------------------------
            */
        }
    }
}
