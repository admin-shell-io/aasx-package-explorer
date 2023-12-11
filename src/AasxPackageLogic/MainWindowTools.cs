﻿/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasxIntegrationBase;
using AasxPackageLogic;
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
    /// This class contains pure functionality pieces of a typical "main window" of Package Explorer or
    /// similar application.
    /// </summary>
    public class MainWindowTools : MainWindowLogic
    {
        /// <summary>
        /// String manipulation to form a Json LD (linked data) from an input json.
        /// </summary>
        public static string SubTool_makeJsonLD(string json, int count)
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
            Aas.ISubmodel rootSm,
            Aas.ISubmodelElement rootSme,
            Aas.IEnvironment env,
            bool useX509)
        {
            // access
            if (env == null || (rootSm == null && rootSme == null))
                return false;

            // ported from MainWindow_CommandBindings
            Aas.ISubmodel sm = null;
            Aas.SubmodelElementCollection smc = null;
            Aas.SubmodelElementCollection smcp = null;
            if (rootSm != null)
            {
                sm = rootSm;
            }
            if (rootSme != null)
            {
                var smee = rootSme;
                if (smee is Aas.SubmodelElementCollection)
                {
                    smc = smee as Aas.SubmodelElementCollection;
                    var p = smee.Parent;
                    if (p is Aas.Submodel)
                        sm = p as Aas.Submodel;
                    if (p is Aas.SubmodelElementCollection)
                        smcp = p as Aas.SubmodelElementCollection;
                }
            }
            if (sm == null && smcp == null)
                return false;

            List<Aas.SubmodelElementCollection> existing = new List<Aas.SubmodelElementCollection>();
            if (smc == null)
            {
                for (int i = 0; i < sm.SubmodelElements.Count; i++)
                {
                    var sme = sm.SubmodelElements[i];
                    var len = "signature".Length;
                    var idShort = sme.IdShort;
                    if (sme is Aas.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(sme as Aas.SubmodelElementCollection);
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
                    if (sme is Aas.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(sme as Aas.SubmodelElementCollection);
                        smc.Remove(sme);
                        i--; // check next
                    }
                }
            }

            if (useX509)
            {
                Aas.SubmodelElementCollection smec = new Aas.SubmodelElementCollection(idShort: "signature");
                Aas.Property json = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "submodelJson");
                Aas.Property canonical = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "submodelJsonCanonical");
                Aas.Property subject = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "subject");
                Aas.SubmodelElementCollection x5c = new Aas.SubmodelElementCollection(idShort: "x5c");
                Aas.Property algorithm = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "algorithm");
                Aas.Property sigT = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "sigT");
                Aas.Property signature = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "signature");
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
                JsonCanonicalizer jsonCanonicalizer = new JsonCanonicalizer(s);
                string result = jsonCanonicalizer.GetEncodedString();
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
                        Aas.Property c = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "certificate_" + j++);
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
                Aas.SubmodelElementCollection smec = new Aas.SubmodelElementCollection(idShort: "signature");
                Aas.Property json = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "submodelJson");
                Aas.Property jsonld = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "submodelJsonLD");
                Aas.Property vc = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "vc");
                Aas.Property epvc = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "endpointVC");
                Aas.Property algorithm = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "algorithm");
                Aas.Property sigT = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "sigT");
                Aas.Property proof = new Aas.Property(Aas.DataTypeDefXsd.String, idShort: "proof");
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
                s = SubTool_makeJsonLD(s, 0);
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

            // ok??!
            return true;
        }

        /// <summary>
        /// Validates a certificate
        /// </summary>
        public bool Tool_Security_ValidateCertificate(
            Aas.ISubmodel rootSm,
            Aas.ISubmodelElement rootSme,
            Aas.Environment env,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (env == null || (rootSm == null && rootSme == null))
                return false;

            var resOk = true;

            // ported from MainWindow_CommandBindings
            List<Aas.SubmodelElementCollection> existing = new List<Aas.SubmodelElementCollection>();
            List<Aas.SubmodelElementCollection> validate = new List<Aas.SubmodelElementCollection>();
            Aas.ISubmodel sm = null;
            Aas.SubmodelElementCollection smc = null;
            Aas.SubmodelElementCollection smcp = null;
            bool smcIsSignature = false;
            if (rootSm != null)
            {
                sm = rootSm;
            }
            if (rootSme != null)
            {
                var smee = rootSme;
                if (smee is Aas.SubmodelElementCollection)
                {
                    smc = smee as Aas.SubmodelElementCollection;
                    var len = "signature".Length;
                    var idShort = smc.IdShort;
                    if (idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        smcIsSignature = true;
                    }
                    var p = smc.Parent;
                    if (smcIsSignature && p is Aas.Submodel)
                        sm = p as Aas.Submodel;
                    if (smcIsSignature && p is Aas.SubmodelElementCollection)
                        smcp = p as Aas.SubmodelElementCollection;
                    if (!smcIsSignature)
                        smcp = smc;
                }
            }
            if (sm == null && smcp == null)
                return false;

            if (sm != null)
            {
                foreach (var smee in sm.SubmodelElements)
                {
                    var len = "signature".Length;
                    var idShort = smee.IdShort;
                    if (smee is Aas.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(smee as Aas.SubmodelElementCollection);
                    }
                }
            }
            if (smcp != null)
            {
                foreach (var sme in smcp.Value)
                {
                    var len = "signature".Length;
                    var idShort = sme.IdShort;
                    if (sme is Aas.SubmodelElementCollection &&
                            idShort.Length >= len &&
                            idShort.Substring(0, len).ToLower() == "signature")
                    {
                        existing.Add(sme as Aas.SubmodelElementCollection);
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
                    Aas.SubmodelElementCollection x5c = null;
                    Aas.Property subject = null;
                    Aas.Property algorithm = null;
                    Aas.Property digest = null; // legacy
                    Aas.Property signature = null;

                    foreach (var sme in smec.Value)
                    {
                        var smee = sme;
                        switch (smee.IdShort)
                        {
                            case "x5c":
                                if (smee is Aas.SubmodelElementCollection)
                                    x5c = smee as Aas.SubmodelElementCollection;
                                break;
                            case "subject":
                                subject = smee as Aas.Property;
                                break;
                            case "algorithm":
                                algorithm = smee as Aas.Property;
                                break;
                            case "digest":
                                digest = smee as Aas.Property;
                                break;
                            case "signature":
                                signature = smee as Aas.Property;
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
                            for (int i = 0; i < x5c.Value.Count; i++)
                            {
                                var p = x5c.Value[i] as Aas.Property;
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
                            LogErrorToTicket(ticket,
                                $"While checking '{smec.IdShort}' " +
                                $"Invalid certificate chain: '{subject.Value}' occured!");
                            resOk = false;
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
                                    LogErrorToTicket(ticket,
                                        $"While checking '{smec.IdShort}': " +
                                        $"Invalid signature '{subject.Value}' found!");
                                    resOk = false;
                                }
                                if (valid)
                                {
                                    Log.Singleton.Info(StoredPrint.Color.Blue,
                                        $"While checking '{smec.IdShort}': " +
                                        $"Signature is valid: '{subject.Value}'");
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
        public bool Tool_Security_PackageEncrpt(
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

                Byte[] binaryFile = System.IO.File.ReadAllBytes(sourceFn);
                string binaryBase64 = Convert.ToBase64String(binaryFile);

                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                string fileToken = Jose.JWT.Encode(
                    payload, publicKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);
                Byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileToken);

                System.IO.File.WriteAllBytes(targetFn, fileBytes);
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex, $"encrypting AASX file {sourceFn} with certificate {certFn}");
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

                Byte[] binaryFile = System.IO.File.ReadAllBytes(sourceFn);
                string fileString = System.Text.Encoding.UTF8.GetString(binaryFile);

                string fileString2 = Jose.JWT.Decode(
                    fileString, privateKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);

                var parsed0 = JObject.Parse(fileString2);
                string binaryBase64_2 = parsed0.SelectToken("file").Value<string>();

                Byte[] fileBytes2 = Convert.FromBase64String(binaryBase64_2);

                System.IO.File.WriteAllBytes(targetFn, fileBytes2);
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex, $"decrypting AASX2 file {sourceFn} with certificate {certFn}");
                return false;
            }

            // ok?
            return true;
        }

        /// <summary>
        /// Populates an existingSubmodel with values from OPC UA.
        /// </summary>
        public void Tool_OpcUaClientRead(
            Aas.ISubmodel sm,
            AasxMenuActionTicket ticket = null)
        {
            try
            {
                // Durch das Submodel iterieren
                {
                    int count = sm.Qualifiers.Count;
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
                            var p = sm.Qualifiers[i];

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
                        count = sm.SubmodelElements.Count;
                        i = 0;
                        while (i < count)
                        {
                            if (sm.SubmodelElements[i] is Aas.Property)
                            {
                                // access data
                                var p = sm.SubmodelElements[i] as Aas.Property;
                                var nodeName = "" + Path + p?.IdShort;

                                // do read() via plug-in
                                var resValue = pi.InvokeAction(
                                    "read-sme-value", resClient.obj,
                                    nodeName, Namespace) as AasxPluginResultBaseObject;

                                // set?
                                if (resValue != null && resValue.obj != null && resValue.obj is string)
                                {
                                    var value = (string)resValue.obj;
                                    p?.Set(p.ValueType, value);
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
        /// Reads a Submodel from JSON and add/ replaces it in a AAS / environment
        /// Note: check if there is a business case for this
        /// </summary>
        public void Tool_ReadSubmodel(
            Aas.ISubmodel sm,
            Aas.Environment env,
            string sourceFn,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (sm == null || env == null)
            {
                LogErrorToTicket(ticket, "Read Aas.Submodel: invalid Submodel or Environment.");
                return;
            }

            try
            {
                // locate AAS?
                var aas = env.FindAasWithSubmodelId(sm.Id);

                // de-serialize Submodel
                Aas.Submodel submodel = null;

                using (var file = System.IO.File.OpenRead(sourceFn))
                {
                    var node = System.Text.Json.Nodes.JsonNode.Parse(file);
                    submodel = Aas.Jsonization.Deserialize.SubmodelFrom(node);
                }

                // need id for idempotent behaviour
                if (submodel == null || submodel.Id == null)
                {
                    LogErrorToTicket(ticket,
                        "Submodel Read: Identification of SubModel is (null).");
                    return;
                }

                // add Submodel
                var existingSm = env.FindSubmodelById(submodel.Id);
                if (existingSm != null)
                    env.Submodels.Remove(existingSm);
                env.Submodels.Add(submodel);

                // add SubmodelRef to AAS
                // access the AAS
                var newsmr = ExtendReference.CreateFromKey(new Aas.Key(Aas.KeyTypes.Submodel, submodel.Id));
                var existsmr = aas.HasSubmodelReference(newsmr);
                if (!existsmr)
                    aas.AddSubmodelReference(newsmr);
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex,
                    "Submodel Read: Can not read SubModel.");
            }
        }

        /// <summary>
        /// Writes a Submodel to JSON.
        /// Note: check if there is a business case for this
        /// </summary>
        public void Tool_SubmodelWrite(
            Aas.Submodel sm,
            string targetFn,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (sm == null)
            {
                LogErrorToTicket(ticket, "Write Aas.Submodel: invalid Submodel.");
                return;
            }

            try
            {
                using (var s = new StreamWriter(targetFn))
                {
                    var json = JsonConvert.SerializeObject(sm, Formatting.Indented);
                    s.WriteLine(json);
                }
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex,
                    "Submodel Read: Can not read SubModel.");
            }
        }

        /// <summary>
        /// Puts a Submodel to URL.
        /// Note: check if there is a business case for this
        /// </summary>
        public void Tool_SubmodelPut(
            Aas.ISubmodel sm,
            string url,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (sm == null)
            {
                LogErrorToTicket(ticket, "Put Aas.Submodel: invalid Submodel.");
                return;
            }

            try
            {
                var json = JsonConvert.SerializeObject(sm, Formatting.Indented);
#if TODO
                var client = new AasxRestServerLibrary.AasxRestClient(url);
                client.PutSubmodelAsync(json);
#endif
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex,
                    $"Submodel Put: Can not put SubModel to url '{url}'.");
            }
        }

        /// <summary>
        /// Gets a Submodel to URL.
        /// Note: check if there is a business case for this
        /// </summary>
        public void Tool_SubmodelGet(
            Aas.Environment env,
            Aas.ISubmodel sm,
            string url,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (sm == null || env == null)
            {
                LogErrorToTicket(ticket, "Get Aas.Submodel: invalid Submodel or Environment.");
                return;
            }

#if TODO
            var smJson = "";
            try
            {
                var client = new AasxRestServerLibrary.AasxRestClient(url);
                smJson = client.GetSubmodel(sm.IdShort);
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex, $"Connecting to REST server {url}");
                return;
            }

            var aas = env.FindAasWithSubmodelId(sm.Id);

            // de-serialize Submodel
            Aas.Submodel submodel = null;

            try
            {
                using (var file = System.IO.File.OpenRead(smJson))
                {
                    var node = System.Text.Json.Nodes.JsonNode.Parse(file);
                    submodel = Jsonization.Deserialize.SubmodelFrom(node);
                }
            }
            catch (Exception ex)
            {
                LogErrorToTicket(ticket, ex, "Submodel Get: Can not read SubModel.");
                return;
            }

            // need id for idempotent behaviour
            if (submodel == null || submodel.Id == null)
            {
                LogErrorToTicket(ticket, "Submodel Get: Identification of SubModel is (null).");
                return;
            }

            // add Submodel
            var existingSm = env.FindSubmodelById(submodel.Id);
            if (existingSm != null)
                env.Submodels.Remove(existingSm);
            env.Submodels.Add(submodel);

            // add SubmodelRef to AAS
            // access the AAS
            var newsmr = ExtendReference.CreateFromKey(new Key(Aas.KeyTypes.Submodel, submodel.Id));
            var existsmr = aas.HasSubmodelReference(newsmr);
            if (!existsmr)
            {
                aas.AddSubmodelReference(newsmr);
            }
#endif
        }

    }
}