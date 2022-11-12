/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasxPackageLogic;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;
using Org.Webpki.JsonCanonicalizer;
using System.IO;
using System.Windows;
using AasxIntegrationBase;
using Jose;
using System.Threading;
using AasxPackageLogic.PackageCentral;
using Newtonsoft.Json.Serialization;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This class contains pure logic elements of a typical "main window" of Package Explorer or
    /// similar application.
    /// </summary>
    public class MainWindowLogic
    {
        /// <summary>
        /// If in scriptmode, set ticket result and exception to error.
        /// Add also to log.
        /// </summary>
        public void LogErrorToTicket(
            AasxMenuActionTicket ticket,
            string message)
        {
            if (ticket != null)
            {
                ticket.Exception = message;
                ticket.Result = false;
            }

            Log.Singleton.Error(message);
        }

        /// <summary>
        /// Only in scriptmode, set ticket result and exception to error.
        /// Add also to log.
        /// Do nothing, if not in scriptmode
        /// </summary>
        public void LogErrorToTicketOrSilent(
            AasxMenuActionTicket ticket,
            string message)
        {
            if (ticket?.ScriptMode != true)
                return;

            if (ticket != null)
            {
                ticket.Exception = message;
                ticket.Result = false;
            }

            Log.Singleton.Error(message);
        }

        public void LogErrorToTicket(
            AasxMenuActionTicket ticket,
            Exception ex,
            string where)
        {
            if (ticket != null)
            {
                ticket.Exception = $"Error {ex?.Message} in {where}.";
                ticket.Result = false;
            }

            Log.Singleton.Error(ex, where);
        }

        /// <summary>
        /// String manipulation to form a Json LD (linked data) from an input json.
        /// </summary>
        public static string makeJsonLD(string json, int count)
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

        /// <summary>
        /// Performs a signing of a Submodel or SubmodelElement
        /// </summary>
        public bool Tool_Security_Sign(
            AdminShell.Submodel rootSm,
            AdminShell.SubmodelElement rootSme,
            AdminShell.AdministrationShellEnv env,
            bool useX509)
        {
            // access
            if (env == null || (rootSm == null && rootSme == null))
                return false;

            // ported from MainWindow_CommandBindings
            AdminShell.Submodel sm = null;
            AdminShell.SubmodelElementCollection smc = null;
            AdminShell.SubmodelElementCollection smcp = null;
            if (rootSm != null)
            {
                sm = rootSm;
            }
            if (rootSme != null)
            {
                var smee = rootSme;
                if (smee is AdminShell.SubmodelElementCollection)
                {
                    smc = smee as AdminShell.SubmodelElementCollection;
                    var p = smee.parent;
                    if (p is AdminShell.Submodel)
                        sm = p as AdminShell.Submodel;
                    if (p is AdminShell.SubmodelElementCollection)
                        smcp = p as AdminShell.SubmodelElementCollection;
                }
            }
            if (sm == null && smcp == null)
                return false;

            List<AdminShell.SubmodelElementCollection> existing = new List<AdminShellV20.SubmodelElementCollection>();
            if (smc == null)
            {
                for (int i = 0; i < sm.submodelElements.Count; i++)
                {
                    var sme = sm.submodelElements[i];
                    var len = "signature".Length;
                    var idShort = sme.submodelElement.idShort;
                    if (sme.submodelElement is AdminShell.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(sme.submodelElement as AdminShell.SubmodelElementCollection);
                        sm.Remove(sme.submodelElement);
                        i--; // check next
                    }
                }
            }
            else
            {
                for (int i = 0; i < smc.value.Count; i++)
                {
                    var sme = smc.value[i];
                    var len = "signature".Length;
                    var idShort = sme.submodelElement.idShort;
                    if (sme.submodelElement is AdminShell.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(sme.submodelElement as AdminShell.SubmodelElementCollection);
                        smc.Remove(sme.submodelElement);
                        i--; // check next
                    }
                }
            }

            if (useX509)
            {
                AdminShell.SubmodelElementCollection smec = AdminShell.SubmodelElementCollection.CreateNew("signature");
                AdminShell.Property json = AdminShellV20.Property.CreateNew("submodelJson");
                AdminShell.Property canonical = AdminShellV20.Property.CreateNew("submodelJsonCanonical");
                AdminShell.Property subject = AdminShellV20.Property.CreateNew("subject");
                AdminShell.SubmodelElementCollection x5c = AdminShell.SubmodelElementCollection.CreateNew("x5c");
                AdminShell.Property algorithm = AdminShellV20.Property.CreateNew("algorithm");
                AdminShell.Property sigT = AdminShellV20.Property.CreateNew("sigT");
                AdminShell.Property signature = AdminShellV20.Property.CreateNew("signature");
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
                json.value = s;
                JsonCanonicalizer jsonCanonicalizer = new JsonCanonicalizer(s);
                string result = jsonCanonicalizer.GetEncodedString();
                canonical.value = result;
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
                    subject.value = certificate.Subject;

                    X509Chain ch = new X509Chain();
                    ch.Build(certificate);

                    //// string[] X509Base64 = new string[ch.ChainElements.Count];

                    int j = 1;
                    foreach (X509ChainElement element in ch.ChainElements)
                    {
                        AdminShell.Property c = AdminShellV20.Property.CreateNew("certificate_" + j++);
                        c.value = Convert.ToBase64String(element.Certificate.GetRawCertData());
                        x5c.Add(c);
                    }

                    try
                    {
                        using (RSA rsa = certificate.GetRSAPrivateKey())
                        {
                            algorithm.value = "RS256";
                            byte[] data = Encoding.UTF8.GetBytes(result);
                            byte[] signed = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                            signature.value = Convert.ToBase64String(signed);
                            sigT.value = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
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
                AdminShell.SubmodelElementCollection smec = AdminShell.SubmodelElementCollection.CreateNew("signature");
                AdminShell.Property json = AdminShellV20.Property.CreateNew("submodelJson");
                AdminShell.Property jsonld = AdminShellV20.Property.CreateNew("submodelJsonLD");
                AdminShell.Property vc = AdminShellV20.Property.CreateNew("vc");
                AdminShell.Property epvc = AdminShellV20.Property.CreateNew("endpointVC");
                AdminShell.Property algorithm = AdminShellV20.Property.CreateNew("algorithm");
                AdminShell.Property sigT = AdminShellV20.Property.CreateNew("sigT");
                AdminShell.Property proof = AdminShellV20.Property.CreateNew("proof");
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
                json.value = s;
                s = makeJsonLD(s, 0);
                jsonld.value = s;

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
                    epvc.value = "https://nameplate.h2894164.stratoserver.net";
                    string requestPath = epvc.value + "/demo/sign?create_as_verifiable_presentation=false";

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
                        vc.value = s;

                        var parsed = JObject.Parse(s);

                        try
                        {
                            var p = parsed.SelectToken("proof").Value<JObject>();
                            if (p != null)
                                proof.value = JsonConvert.SerializeObject(p, Formatting.Indented);
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
                    algorithm.value = "VC";
                    sigT.value = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
                }
            }

            // ok??!
            return true;
        }

        /// <summary>
        /// Validates a certificate
        /// </summary>
        public bool Tool_Security_ValidateCertificate(
            AdminShell.Submodel rootSm,
            AdminShell.SubmodelElement rootSme,
            AdminShell.AdministrationShellEnv env,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (env == null || (rootSm == null && rootSme == null))
                return false;

            var resOk = true;

            // ported from MainWindow_CommandBindings
            List<AdminShell.SubmodelElementCollection> existing = new List<AdminShellV20.SubmodelElementCollection>();
            List<AdminShell.SubmodelElementCollection> validate = new List<AdminShellV20.SubmodelElementCollection>();
            AdminShell.Submodel sm = null;
            AdminShell.SubmodelElementCollection smc = null;
            AdminShell.SubmodelElementCollection smcp = null;
            bool smcIsSignature = false;
            if (rootSm == null)
            {
                sm = rootSm;
            }
            if (rootSme != null)
            {
                var smee = rootSme;
                if (smee is AdminShell.SubmodelElementCollection)
                {
                    smc = smee as AdminShell.SubmodelElementCollection;
                    var len = "signature".Length;
                    var idShort = smc.idShort;
                    if (idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        smcIsSignature = true;
                    }
                    var p = smc.parent;
                    if (smcIsSignature && p is AdminShell.Submodel)
                        sm = p as AdminShell.Submodel;
                    if (smcIsSignature && p is AdminShell.SubmodelElementCollection)
                        smcp = p as AdminShell.SubmodelElementCollection;
                    if (!smcIsSignature)
                        smcp = smc;
                }
            }
            if (sm == null && smcp == null)
                return false;

            if (sm != null)
            {
                foreach (var sme in sm.submodelElements)
                {
                    var smee = sme.submodelElement;
                    var len = "signature".Length;
                    var idShort = smee.idShort;
                    if (smee is AdminShell.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(smee as AdminShell.SubmodelElementCollection);
                    }
                }
            }
            if (smcp != null)
            {
                foreach (var sme in smcp.value)
                {
                    var len = "signature".Length;
                    var idShort = sme.submodelElement.idShort;
                    if (sme.submodelElement is AdminShell.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(sme.submodelElement as AdminShell.SubmodelElementCollection);
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
                    AdminShell.SubmodelElementCollection x5c = null;
                    AdminShell.Property subject = null;
                    AdminShell.Property algorithm = null;
                    AdminShell.Property digest = null; // legacy
                    AdminShell.Property signature = null;

                    foreach (var sme in smec.value)
                    {
                        var smee = sme.submodelElement;
                        switch (smee.idShort)
                        {
                            case "x5c":
                                if (smee is AdminShell.SubmodelElementCollection)
                                    x5c = smee as AdminShell.SubmodelElementCollection;
                                break;
                            case "subject":
                                subject = smee as AdminShell.Property;
                                break;
                            case "algorithm":
                                algorithm = smee as AdminShell.Property;
                                break;
                            case "digest":
                                digest = smee as AdminShell.Property;
                                break;
                            case "signature":
                                signature = smee as AdminShell.Property;
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
                        JsonCanonicalizer jsonCanonicalizer = new JsonCanonicalizer(s);
                        string result = jsonCanonicalizer.GetEncodedString();

                        X509Store storeCA = new X509Store("CA", StoreLocation.CurrentUser);
                        storeCA.Open(OpenFlags.ReadWrite);
                        X509Certificate2Collection xcc = new X509Certificate2Collection();
                        X509Certificate2 x509 = null;
                        bool valid = false;

                        try
                        {
                            for (int i = 0; i < x5c.value.Count; i++)
                            {
                                var p = x5c.value[i].submodelElement as AdminShell.Property;
                                var cert = new X509Certificate2(Convert.FromBase64String(p.value));
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
                            LogErrorToTicket(ticket,
                                $"While checking '{smec.idShort}' " +
                                $"Invalid certificate chain: '{subject.value}' occured!");
                            resOk = false;
                        }
                        if (valid)
                        {
                            valid = false;

                            if (algorithm.value == "RS256")
                            {
                                try
                                {
                                    using (RSA rsa = x509.GetRSAPublicKey())
                                    {
                                        string value = null;
                                        if (signature != null)
                                            value = signature.value;
                                        if (digest != null)
                                            value = digest.value;
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
                                    LogErrorToTicket(ticket,
                                        $"While checking '{smec.idShort}': " +
                                        $"Invalid signature '{subject.value}' found!");
                                    resOk = false;
                                }
                                if (valid)
                                {
                                    Log.Singleton.Info(StoredPrint.Color.Blue,
                                        $"While checking '{smec.idShort}': " +
                                        $"Signature is valid: '{subject.value}'");
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

            // ok??!
            return resOk;
        }

        /// <summary>
        /// Encrypts a full package
        /// </summary>
        public bool Tool_Security_PackageEncrpty(
            string sourceFn,
            string certFn,
            string targetFn,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (sourceFn?.HasContent() != true || certFn?.HasContent() != true || targetFn?.HasContent() != true)
                return false;

            try
            {
                X509Certificate2 x509 = new X509Certificate2(certFn);
                var publicKey = x509.GetRSAPublicKey();

                Byte[] binaryFile = File.ReadAllBytes(sourceFn);
                string binaryBase64 = Convert.ToBase64String(binaryFile);

                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                string fileToken = Jose.JWT.Encode(
                    payload, publicKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);
                Byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileToken);

                File.WriteAllBytes(targetFn, fileBytes);
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, $"encrypting AASX file {sourceFn} with certificate {certFn}");
                return false;
            }

            // ok?
            return true;
        }

        /// <summary>
        /// Decrypts a full package
        /// </summary>
        public bool Tool_Security_PackageDecrpt(
            string sourceFn,
            string certFn,
            string targetFn,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (sourceFn?.HasContent() != true || certFn?.HasContent() != true || targetFn?.HasContent() != true)
                return false;

            try
            {
                X509Certificate2 x509 = new X509Certificate2(certFn, "i40");
                var privateKey = x509.GetRSAPrivateKey();

                Byte[] binaryFile = File.ReadAllBytes(sourceFn);
                string fileString = System.Text.Encoding.UTF8.GetString(binaryFile);

                string fileString2 = Jose.JWT.Decode(
                    fileString, privateKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);

                var parsed0 = JObject.Parse(fileString2);
                string binaryBase64_2 = parsed0.SelectToken("file").Value<string>();

                Byte[] fileBytes2 = Convert.FromBase64String(binaryBase64_2);

                File.WriteAllBytes(targetFn, fileBytes2);
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, $"decrypting AASX2 file {sourceFn} with certificate {certFn}");
                return false;
            }

            // ok?
            return true;
        }

        /// <summary>
        /// Populates an existingSubmodel with values from OPC UA.
        /// </summary>
        public void Tool_OpcUaClientRead(
            AdminShell.Submodel sm,
            AasxMenuActionTicket ticket = null)
        {
            try
            {
                // Durch das Submodel iterieren
                {
                    int count = sm.qualifiers.Count;
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
                            var p = sm.qualifiers[i];

                            switch (i)
                            {
                                case 0: // URL
                                    if (p.type == "OPCURL")
                                    {
                                        URL = p.value;
                                    }
                                    break;
                                case 1: // Username
                                    if (p.type == "OPCUsername")
                                    {
                                        Username = p.value;
                                    }
                                    break;
                                case 2: // Password
                                    if (p.type == "OPCPassword")
                                    {
                                        Password = p.value;
                                    }
                                    break;
                                case 3: // Namespace
                                    if (p.type == "OPCNamespace")
                                    {
                                        Namespace = int.Parse(p.value);
                                    }
                                    break;
                                case 4: // Path
                                    if (p.type == "OPCPath")
                                    {
                                        Path = p.value;
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
                        count = sm.submodelElements.Count;
                        i = 0;
                        while (i < count)
                        {
                            if (sm.submodelElements[i].submodelElement is AdminShell.Property)
                            {
                                // access data
                                var p = sm.submodelElements[i].submodelElement as AdminShell.Property;
                                var nodeName = "" + Path + p?.idShort;

                                // do read() via plug-in
                                var resValue = pi.InvokeAction(
                                    "read-sme-value", resClient.obj,
                                    nodeName, Namespace) as AasxPluginResultBaseObject;

                                // set?
                                if (resValue != null && resValue.obj != null && resValue.obj is string)
                                {
                                    var value = (string)resValue.obj;
                                    p?.Set(p.valueType, value);
                                }
                            }
                            i++;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex, "executing OPC UA client");
            }

        }

        /// <summary>
        /// This function reads a Submodel from JSON and add/ replaces it in a AAS / environment
        /// Note: check if there is a business case for this
        /// </summary>
        public void Tool_ReadSubmodel(
            AdminShell.Submodel sm,
            AdminShell.AdministrationShellEnv env,
            string sourceFn,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (sm == null || env == null)
            {
                LogErrorToTicket(ticket, "Read Submodel: invalid Submodel or Environment.");
                return;
            }

            try
            {
                // locate AAS?
                var aas = env.FindAASwithSubmodel(sm.identification);

                // de-serialize Submodel
                AdminShell.Submodel submodel = null;

                using (StreamReader file = System.IO.File.OpenText(sourceFn))
                {
                    ITraceWriter tw = new MemoryTraceWriter();
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TraceWriter = tw;
                    serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                    submodel = (AdminShell.Submodel)serializer.Deserialize(file, typeof(AdminShell.Submodel));
                }

                // need id for idempotent behaviour
                if (submodel == null || submodel.identification == null)
                {
                    LogErrorToTicket(ticket, 
                        "Submodel Read: Identification of SubModel is (null).");
                    return;
                }

                // datastructure update
                if (env.Assets == null)
                {
                    LogErrorToTicket(ticket,
                        "Submodel Read: Error accessing internal data structures.");
                    return;
                }

                // add Submodel
                var existingSm = env.FindSubmodel(submodel.identification);
                if (existingSm != null)
                    env.Submodels.Remove(existingSm);
                env.Submodels.Add(submodel);

                // add SubmodelRef to AAS
                // access the AAS
                var newsmr = AdminShell.SubmodelRef.CreateNew(
                    AdminShell.Key.Submodel, true, submodel.identification.idType, submodel.identification.id);
                var existsmr = aas.HasSubmodelRef(newsmr);
                if (!existsmr)
                    aas.AddSubmodelRef(newsmr);
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex,
                    "Submodel Read: Can not read SubModel.");
                return;
            }
        }
    }
}