/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdminShellNS
{
    /// <summary>
    /// This class lets an outer functionality keep track on the supplementary files, which are in or
    /// are pending to be added or deleted to an Package.
    /// </summary>
    public class AdminShellPackageSupplementaryFile : AdminShell.Referable
    {
        public delegate byte[] SourceGetByteChunk();

        public enum LocationType { InPackage, AddPending, DeletePending }

        public enum SpecialHandlingType { None, EmbedAsThumbnail }

        public readonly Uri Uri = null;

        public readonly string UseMimeType = null;

        public readonly string SourceLocalPath = null;
        public readonly SourceGetByteChunk SourceGetBytesDel = null;

        public LocationType Location;
        public readonly SpecialHandlingType SpecialHandling;

        public AdminShellPackageSupplementaryFile(
            Uri uri, string sourceLocalPath = null, LocationType location = LocationType.InPackage,
            SpecialHandlingType specialHandling = SpecialHandlingType.None,
            SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            Uri = uri;
            UseMimeType = useMimeType;
            SourceLocalPath = sourceLocalPath;
            SourceGetBytesDel = sourceGetBytesDel;
            Location = location;
            SpecialHandling = specialHandling;
        }

        // class derives from Referable in order to provide GetElementName
        public override string GetElementName()
        {
            return "File";
        }

    }

    public class ListOfAasSupplementaryFile : List<AdminShellPackageSupplementaryFile>
    {
        public AdminShellPackageSupplementaryFile FindByUri(string path)
        {
            if (path == null)
                return null;

            return this.FirstOrDefault(
                x => x?.Uri?.ToString().Trim() == path.Trim());
        }
    }

    /// <summary>
    /// Provides (static?) helpers for serializing AAS..
    /// </summary>
    public static class AdminShellSerializationHelper
    {

        public static string TryReadXmlFirstElementNamespaceURI(Stream s)
        {
            string res = null;
            try
            {
                var xr = System.Xml.XmlReader.Create(s);
                int i = 0;
                while (xr.Read())
                {
                    // limit amount of read
                    i++;
                    if (i > 99)
                        // obviously not found
                        break;

                    // find element
                    if (xr.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        res = xr.NamespaceURI;
                        break;
                    }
                }
                xr.Close();
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            // return to zero pos
            s.Seek(0, SeekOrigin.Begin);

            // give back
            return res;
        }

        /// <summary>
        /// De-serialize an open stream into AdministrationShellEnv. Does version/ compatibility management.
        /// </summary>
        /// <param name="s">Open for read stream</param>
        /// <returns></returns>
        public static AdminShell.AdministrationShellEnv DeserializeXmlFromStreamWithCompat(Stream s)
        {
            // not sure
            AdminShell.AdministrationShellEnv res = null;

            // try get first element
            var nsuri = TryReadXmlFirstElementNamespaceURI(s);

            // read V1.0?
            if (nsuri != null && nsuri.Trim() == "http://www.admin-shell.io/aas/1/0")
            {
#if !DoNotUseAasxCompatibilityModels
                XmlSerializer serializer = new XmlSerializer(
                    typeof(AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv),
                    "http://www.admin-shell.io/aas/1/0");
                var v10 = serializer.Deserialize(s) as AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv;
                res = new AdminShellV20.AdministrationShellEnv(v10);
                return res;
#else
                throw (new Exception("Cannot handle AAS file format http://www.admin-shell.io/aas/1/0 !"));
#endif
            }

            // read V2.0?
            if (nsuri != null && nsuri.Trim() == "http://www.admin-shell.io/aas/2/0")
            {
                XmlSerializer serializer = new XmlSerializer(
                    typeof(AdminShell.AdministrationShellEnv), "http://www.admin-shell.io/aas/2/0");
                res = serializer.Deserialize(s) as AdminShell.AdministrationShellEnv;
                return res;
            }

            // nope!
            return null;
        }

        public static JsonSerializer BuildDefaultAasxJsonSerializer()
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(
                new AdminShellConverters.JsonAasxConverter(
                    "modelType", "name"));
            return serializer;
        }

        public static T DeserializeFromJSON<T>(TextReader textReader) where T : AdminShell.Referable
        {
            var serializer = BuildDefaultAasxJsonSerializer();
            var rf = (T)serializer.Deserialize(textReader, typeof(T));
            return rf;
        }

        public static T DeserializeFromJSON<T>(JToken obj) where T : AdminShell.Referable
        {
            if (obj == null)
                return null;
            var serializer = BuildDefaultAasxJsonSerializer();
            var rf = obj.ToObject<T>(serializer);
            return rf;
        }

    }

    /// <summary>
    /// This class encapsulates an AdminShellEnvironment and supplementary files into an AASX Package.
    /// Specifically has the capability to load, update and store .XML, .JSON and .AASX packages.
    /// </summary>
    public class AdminShellPackageEnv : IDisposable
    {
        private string _fn = "New Package";

        private string _tempFn = null;

        private AdminShell.AdministrationShellEnv _aasEnv = new AdminShell.AdministrationShellEnv();
        private Package _openPackage = null;
        private readonly ListOfAasSupplementaryFile _pendingFilesToAdd = new ListOfAasSupplementaryFile();
        private readonly ListOfAasSupplementaryFile _pendingFilesToDelete = new ListOfAasSupplementaryFile();

        public AdminShellPackageEnv() { }

        public AdminShellPackageEnv(AdminShell.AdministrationShellEnv env)
        {
            if (env != null)
                _aasEnv = env;
        }

        public AdminShellPackageEnv(string fn, bool indirectLoadSave = false)
        {
            Load(fn, indirectLoadSave);
        }

        public bool IsOpen
        {
            get
            {
                return _openPackage != null;
            }
        }

        public string Filename
        {
            get
            {
                return _fn;
            }
        }

        public AdminShell.AdministrationShellEnv AasEnv
        {
            get
            {
                return _aasEnv;
            }
        }

        private static AdminShell.AdministrationShellEnv LoadXml(string fn)
        {
            try
            {
                using (var reader = new StreamReader(fn))
                {
                    var aasEnv = AdminShellSerializationHelper.DeserializeXmlFromStreamWithCompat(
                        reader.BaseStream);

                    if (aasEnv == null)
                        throw new Exception("Type error for XML file");

                    return aasEnv;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"While reading AAS {fn} at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        private static AdminShell.AdministrationShellEnv LoadJson(string fn)
        {
            try
            {
                using (StreamReader file = System.IO.File.OpenText(fn))
                {
                    // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                    var serializer = new JsonSerializer();
                    serializer.Converters.Add(
                        new AdminShellConverters.JsonAasxConverter(
                            "modelType", "name"));

                    var aasEnv = (AdminShell.AdministrationShellEnv)serializer.Deserialize(
                        file, typeof(AdminShell.AdministrationShellEnv));

                    return aasEnv;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"While reading AAS {fn} at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        /// <remarks><paramref name="fn"/> is unequal <paramref name="fnToLoad"/> if indirectLoadSave is used.</remarks>
        private static (AdminShell.AdministrationShellEnv, Package) LoadPackageAasx(string fn, string fnToLoad)
        {
            AdminShell.AdministrationShellEnv aasEnv;
            Package openPackage = null;

            Package package;
            try
            {
                package = Package.Open(fnToLoad, FileMode.Open);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    fn == fnToLoad
                        ? $"While opening the package to read AASX {fn} " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}"
                        : $"While opening the package to read AASX {fn} indirectly from {fnToLoad} " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }

            try
            {
                // get the origin from the package
                PackagePart originPart = null;
                var xs = package.GetRelationshipsByType(
                    "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                foreach (var x in xs)
                    if (x.SourceUri.ToString() == "/")
                    {
                        originPart = package.GetPart(x.TargetUri);
                        break;
                    }

                if (originPart == null)
                    throw (new Exception("Unable to find AASX origin. Aborting!"));

                // get the specs from the package
                PackagePart specPart = null;
                xs = originPart.GetRelationshipsByType("http://www.admin-shell.io/aasx/relationships/aas-spec");
                foreach (var x in xs)
                {
                    specPart = package.GetPart(x.TargetUri);
                    break;
                }

                if (specPart == null)
                    throw (new Exception("Unable to find AASX spec(s). Aborting!"));

                // open spec part to read
                try
                {
                    if (specPart.Uri.ToString().ToLower().Trim().EndsWith("json"))
                    {
                        using (var s = specPart.GetStream(FileMode.Open))
                        {
                            using (var file = new StreamReader(s))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                serializer.Converters.Add(
                                    new AdminShellConverters.JsonAasxConverter(
                                        "modelType", "name"));

                                aasEnv = (AdminShell.AdministrationShellEnv)serializer.Deserialize(
                                    file, typeof(AdminShell.AdministrationShellEnv));
                            }
                        }
                    }
                    else
                    {
                        using (var s = specPart.GetStream(FileMode.Open))
                        {
                            // own catch loop to be more specific
                            aasEnv = AdminShellSerializationHelper.DeserializeXmlFromStreamWithCompat(s);
                            openPackage = package;

                            if (aasEnv == null)
                                throw new Exception("Type error for XML file!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        fn == fnToLoad
                            ? $"While reading spec from the AASX {fn} " +
                              $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}"
                            : $"While reading spec from the {fn} (and indirectly over {fnToLoad}) " +
                              $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    fn == fnToLoad
                        ? $"While reading the AASX {fn} " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}"
                        : $"While reading the {fn} (and indirectly over {fnToLoad}) " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
            finally
            {
                if (openPackage == null)
                {
                    package.Close();
                }
            }

            return (aasEnv, openPackage);
        }

        public void Load(string fn, bool indirectLoadSave = false)
        {
            _fn = fn;
            _openPackage?.Close();
            _openPackage = null;

            string extension = Path.GetExtension(fn).ToLower();
            switch (extension)
            {
                case ".xml":
                    {
                        _aasEnv = LoadXml(fn);
                        break;
                    }
                case ".json":
                    {
                        _aasEnv = LoadJson(fn);
                        break;
                    }
                case ".aasx":
                    {
                        var fnToLoad = fn;
                        _tempFn = null;
                        if (indirectLoadSave)
                        {
                            try
                            {
                                _tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".aasx");
                                System.IO.File.Copy(fn, _tempFn);
                                fnToLoad = _tempFn;

                            }
                            catch (Exception ex)
                            {
                                throw new Exception(
                                    $"While copying AASX {fn} for indirect load to {fnToLoad} " +
                                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                            }
                        }

                        // load package AASX
                        (_aasEnv, _openPackage) = LoadPackageAasx(fn, fnToLoad);
                        break;
                    }
                default:
                    throw new Exception(
                        $"Does not know how to handle the extension {extension} of the file: {fn}");
            }
        }

        public void LoadFromAasEnvString(string content)
        {
            try
            {
                using (var file = new StringReader(content))
                {
                    // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                    _aasEnv = (AdminShell.AdministrationShellEnv)serializer.Deserialize(
                        file, typeof(AdminShell.AdministrationShellEnv));
                }
            }
            catch (Exception ex)
            {
                throw (new Exception(
                    string.Format("While reading AASENV string {0} gave: {1}",
                        AdminShellUtil.ShortLocation(ex), ex.Message)));
            }
        }

        public enum SerializationFormat { None, Xml, Json };

        public static XmlSerializerNamespaces GetXmlDefaultNamespaces()
        {
            var nss = new XmlSerializerNamespaces();
            nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
            nss.Add("aas", "http://www.admin-shell.io/aas/2/0");
            nss.Add("IEC", "http://www.admin-shell.io/IEC61360/2/0");
            nss.Add("abac", "http://www.admin-shell.io/aas/abac/2/0");
            return nss;
        }

        public bool SaveAs(string fn, bool writeFreshly = false, SerializationFormat prefFmt = SerializationFormat.None,
                MemoryStream useMemoryStream = null, bool saveOnlyCopy = false)
        {
            if (fn.ToLower().EndsWith(".xml"))
            {
                // save only XML
                if (!saveOnlyCopy)
                    _fn = fn;
                try
                {
                    Stream s = (useMemoryStream != null)
                        ? (Stream)useMemoryStream
                        : File.Open(fn, FileMode.Create, FileAccess.Write);

                    try
                    {
                        // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                        var serializer = new XmlSerializer(typeof(AdminShell.AdministrationShellEnv));
                        var nss = GetXmlDefaultNamespaces();
                        serializer.Serialize(s, _aasEnv, nss);
                        s.Flush();
                    }
                    finally
                    {
                        // close?
                        if (useMemoryStream == null)
                            s.Close();
                    }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While writing AAS {0} at {1} gave: {2}",
                            fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            if (fn.ToLower().EndsWith(".json"))
            {
                // save only JSON
                // This functionality is an initial test.
                if (!saveOnlyCopy)
                    _fn = fn;
                try
                {
                    Stream s = (useMemoryStream != null) ? (Stream)useMemoryStream
                        : File.Open(fn, FileMode.Create, FileAccess.Write);

                    try
                    {
                        // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                        JsonSerializer serializer = new JsonSerializer()
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                            Formatting = Newtonsoft.Json.Formatting.Indented
                        };

                        using (var sw = new StreamWriter(s))
                        {
                            using (var writer = new JsonTextWriter(sw))
                            {
                                serializer.Serialize(writer, _aasEnv);
                                writer.Flush();
                                sw.Flush();
                                s.Flush();
                            }
                        }
                    }
                    finally
                    {
                        // close?
                        if (useMemoryStream == null)
                            s.Close();
                    }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While writing AAS {0} at {1} gave: {2}",
                            fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            if (fn.ToLower().EndsWith(".aasx"))
            {
                // save package AASX
                try
                {
                    // We want existing contents to be preserved, but do not want to allow the change of the file name.
                    // Therefore: copy the file to a new name, then re-open.
                    // fn could be changed, therefore close "old" package first
                    if (_openPackage != null)
                    {
                        try
                        {
                            _openPackage.Close();
                            if (!writeFreshly)
                            {
                                if (_tempFn != null)
                                    System.IO.File.Copy(_tempFn, fn);
                                else
                                {
                                    /* TODO (MIHO, 2021-01-02): check again.
                                     * Revisiting this code after a while, and after
                                     * the code has undergo some changes by MR, the following copy command needed
                                     * to be amended with a if to protect against self-copy. */
                                    if (_fn != fn)
                                        System.IO.File.Copy(_fn, fn);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogInternally.That.SilentlyIgnoredError(ex);
                        }
                        _openPackage = null;
                    }

                    // approach is to utilize the existing package, if possible. If not, create from scratch
                    Package package = null;
                    if (useMemoryStream != null)
                    {
                        package = Package.Open(
                            useMemoryStream, (writeFreshly) ? FileMode.Create : FileMode.OpenOrCreate);
                    }
                    else
                    {
                        package = Package.Open(
                            (_tempFn != null) ? _tempFn : fn,
                            (writeFreshly) ? FileMode.Create : FileMode.OpenOrCreate);
                    }
                    _fn = fn;

                    // get the origin from the package
                    PackagePart originPart = null;
                    var xs = package.GetRelationshipsByType(
                        "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                    foreach (var x in xs)
                        if (x.SourceUri.ToString() == "/")
                        {
                            originPart = package.GetPart(x.TargetUri);
                            break;
                        }
                    if (originPart == null)
                    {
                        // create, as not existing
                        originPart = package.CreatePart(
                            new Uri("/aasx/aasx-origin", UriKind.RelativeOrAbsolute),
                            System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Maximum);
                        using (var s = originPart.GetStream(FileMode.Create))
                        {
                            var bytes = System.Text.Encoding.ASCII.GetBytes("Intentionally empty.");
                            s.Write(bytes, 0, bytes.Length);
                        }
                        package.CreateRelationship(
                            originPart.Uri, TargetMode.Internal,
                            "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                    }

                    // get the specs from the package
                    PackagePart specPart = null;
                    PackageRelationship specRel = null;
                    xs = originPart.GetRelationshipsByType("http://www.admin-shell.io/aasx/relationships/aas-spec");
                    foreach (var x in xs)
                    {
                        specRel = x;
                        specPart = package.GetPart(x.TargetUri);
                        break;
                    }

                    // check, if we have to change the spec part
                    if (specPart != null && specRel != null)
                    {
                        var name = System.IO.Path.GetFileNameWithoutExtension(
                            specPart.Uri.ToString()).ToLower().Trim();
                        var ext = System.IO.Path.GetExtension(specPart.Uri.ToString()).ToLower().Trim();
                        if ((ext == ".json" && prefFmt == SerializationFormat.Xml)
                             || (ext == ".xml" && prefFmt == SerializationFormat.Json)
                             || (name.StartsWith("aasenv-with-no-id")))
                        {
                            // try kill specpart
                            try
                            {
                                originPart.DeleteRelationship(specRel.Id);
                                package.DeletePart(specPart.Uri);
                            }
                            catch (Exception ex)
                            {
                                LogInternally.That.SilentlyIgnoredError(ex);
                            }
                            finally { specPart = null; specRel = null; }
                        }
                    }

                    if (specPart == null)
                    {
                        // create, as not existing
                        var frn = "aasenv-with-no-id";
                        if (_aasEnv.AdministrationShells.Count > 0)
                            frn = _aasEnv.AdministrationShells[0].GetFriendlyName() ?? frn;
                        var aas_spec_fn = "/aasx/#/#.aas";
                        if (prefFmt == SerializationFormat.Json)
                            aas_spec_fn += ".json";
                        else
                            aas_spec_fn += ".xml";
                        aas_spec_fn = aas_spec_fn.Replace("#", "" + frn);
                        specPart = package.CreatePart(
                            new Uri(aas_spec_fn, UriKind.RelativeOrAbsolute),
                            System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
                        originPart.CreateRelationship(
                            specPart.Uri, TargetMode.Internal,
                            "http://www.admin-shell.io/aasx/relationships/aas-spec");
                    }

                    // now, specPart shall be != null!
                    if (specPart.Uri.ToString().ToLower().Trim().EndsWith("json"))
                    {
                        using (var s = specPart.GetStream(FileMode.Create))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.NullValueHandling = NullValueHandling.Ignore;
                            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                            serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                            using (var sw = new StreamWriter(s))
                            {
                                using (JsonWriter writer = new JsonTextWriter(sw))
                                {
                                    serializer.Serialize(writer, _aasEnv);
                                }
                            }
                        }
                    }
                    else
                    {
                        using (var s = specPart.GetStream(FileMode.Create))
                        {
                            var serializer = new XmlSerializer(typeof(AdminShell.AdministrationShellEnv));
                            var nss = GetXmlDefaultNamespaces();
                            serializer.Serialize(s, _aasEnv, nss);
                        }
                    }

                    // there might be pending files to be deleted (first delete, then add,
                    // in case of identical files in both categories)
                    foreach (var psfDel in _pendingFilesToDelete)
                    {
                        // try find an existing part for that file ..
                        var found = false;

                        // normal files
                        xs = specPart.GetRelationshipsByType("http://www.admin-shell.io/aasx/relationships/aas-suppl");
                        foreach (var x in xs)
                            if (x.TargetUri == psfDel.Uri)
                            {
                                // try to delete
                                specPart.DeleteRelationship(x.Id);
                                package.DeletePart(psfDel.Uri);
                                found = true;
                                break;
                            }

                        // thumbnails
                        xs = package.GetRelationshipsByType(
                            "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                        foreach (var x in xs)
                            if (x.TargetUri == psfDel.Uri)
                            {
                                // try to delete
                                package.DeleteRelationship(x.Id);
                                package.DeletePart(psfDel.Uri);
                                found = true;
                                break;
                            }

                        if (!found)
                            throw (new Exception(
                                $"Not able to delete pending file {psfDel.Uri} in saving package {fn}"));
                    }

                    // after this, there are no more pending for delete files
                    _pendingFilesToDelete.Clear();

                    // write pending supplementary files
                    foreach (var psfAdd in _pendingFilesToAdd)
                    {
                        // make sure ..
                        if ((psfAdd.SourceLocalPath == null && psfAdd.SourceGetBytesDel == null) ||
                            psfAdd.Location != AdminShellPackageSupplementaryFile.LocationType.AddPending)
                            continue;

                        // normal file?
                        if (psfAdd.SpecialHandling == AdminShellPackageSupplementaryFile.SpecialHandlingType.None ||
                            psfAdd.SpecialHandling ==
                                AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                        {

                            // try find an existing part for that file ..
                            PackagePart filePart = null;
                            if (psfAdd.SpecialHandling == AdminShellPackageSupplementaryFile.SpecialHandlingType.None)
                            {
                                xs = specPart.GetRelationshipsByType(
                                    "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                                foreach (var x in xs)
                                    if (x.TargetUri == psfAdd.Uri)
                                    {
                                        filePart = package.GetPart(x.TargetUri);
                                        break;
                                    }
                            }
                            if (psfAdd.SpecialHandling ==
                                AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                            {
                                xs = package.GetRelationshipsByType(
                                    "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                                foreach (var x in xs)
                                    if (x.SourceUri.ToString() == "/" && x.TargetUri == psfAdd.Uri)
                                    {
                                        filePart = package.GetPart(x.TargetUri);
                                        break;
                                    }
                            }

                            if (filePart == null)
                            {
                                // determine mimeType
                                var mimeType = psfAdd.UseMimeType;
                                // reconcile mime
                                if (mimeType == null && psfAdd.SourceLocalPath != null)
                                    mimeType = AdminShellPackageEnv.GuessMimeType(psfAdd.SourceLocalPath);
                                // still null?
                                if (mimeType == null)
                                    // see: https://stackoverflow.com/questions/6783921/
                                    // which-mime-type-to-use-for-a-binary-file-thats-specific-to-my-program
                                    mimeType = "application/octet-stream";

                                // create new part and link
                                filePart = package.CreatePart(psfAdd.Uri, mimeType, CompressionOption.Maximum);
                                if (psfAdd.SpecialHandling ==
                                    AdminShellPackageSupplementaryFile.SpecialHandlingType.None)
                                    specPart.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                                if (psfAdd.SpecialHandling ==
                                    AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                                    package.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        "http://schemas.openxmlformats.org/package/2006/" +
                                        "relationships/metadata/thumbnail");
                            }

                            // now should be able to write
                            using (var s = filePart.GetStream(FileMode.Create))
                            {
                                if (psfAdd.SourceLocalPath != null)
                                {
                                    var bytes = System.IO.File.ReadAllBytes(psfAdd.SourceLocalPath);
                                    s.Write(bytes, 0, bytes.Length);
                                }

                                if (psfAdd.SourceGetBytesDel != null)
                                {
                                    var bytes = psfAdd.SourceGetBytesDel();
                                    if (bytes != null)
                                        s.Write(bytes, 0, bytes.Length);
                                }
                            }
                        }
                    }

                    // after this, there are no more pending for add files
                    _pendingFilesToAdd.Clear();

                    // flush, but leave open
                    package.Flush();
                    _openPackage = package;

                    // if in temp fn, close the package, copy to original fn, re-open the package
                    if (_tempFn != null)
                        try
                        {
                            package.Close();
                            System.IO.File.Copy(_tempFn, _fn, overwrite: true);
                            _openPackage = Package.Open(_tempFn, FileMode.OpenOrCreate);
                        }
                        catch (Exception ex)
                        {
                            throw (new Exception(
                                string.Format("While write AASX {0} indirectly at {1} gave: {2}",
                                fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                        }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While write AASX {0} at {1} gave: {2}",
                        fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            // Don't know to handle
            throw new Exception($"Does not know how to handle the file: {fn}");
        }

        /// <summary>
        /// Temporariyl saves & closes package and executes lambda. Afterwards, the package is re-opened
        /// under the same file name
        /// </summary>
        /// <param name="lambda"></param>
        public void TemporarilySaveCloseAndReOpenPackage(Action lambda)
        {
            // access 
            if (!this.IsOpen)
                throw (new Exception(
                    string.Format("Could not temporarily close and re-open AASX {0}, because package" +
                    "not open as expected!", Filename)));

            try
            {
                // save (it will be open, still)
                SaveAs(this.Filename);

                // close
                _openPackage.Flush();
                _openPackage.Close();

                // execute lambda
                lambda?.Invoke();
            }
            catch (Exception ex)
            {
                throw (new Exception(
                    string.Format("While temporarily close and re-open AASX {0} at {1} gave: {2}",
                    Filename, AdminShellUtil.ShortLocation(ex), ex.Message)));
            }
            finally
            {
                // even after failing of the lambda, the package shall be re-opened
                _openPackage = Package.Open(Filename, FileMode.OpenOrCreate);
            }
        }

        private int BackupIndex = 0;

        public void BackupInDir(string backupDir, int maxFiles)
        {
            // access
            if (backupDir == null || maxFiles < 1)
                return;

            // we do it not caring on any errors
            try
            {
                // get index in form
                if (BackupIndex == 0)
                {
                    // do not always start at 0!!
                    var rnd = new Random();
                    BackupIndex = rnd.Next(maxFiles);
                }
                var ndx = BackupIndex % maxFiles;
                BackupIndex += 1;

                // build a filename
                var bdfn = Path.Combine(backupDir, $"backup{ndx:000}.xml");

                // raw save
                using (var s = new StreamWriter(bdfn))
                {
                    var serializer = new XmlSerializer(typeof(AdminShell.AdministrationShellEnv));
                    var nss = new XmlSerializerNamespaces();
                    nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                    nss.Add("aas", "http://www.admin-shell.io/aas/2/0");
                    nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/2/0");
                    serializer.Serialize(s, _aasEnv, nss);
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
        }

        public Stream GetStreamFromUriOrLocalPackage(string uriString,
            FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.Read)
        {
            // local
            if (IsLocalFile(uriString))
                return GetLocalStreamFromPackage(uriString, mode);

            // no ..
            return File.Open(uriString, mode, access);
        }

        public byte[] GetByteArrayFromUriOrLocalPackage(string uriString)
        {
            try
            {
                using (var input = GetStreamFromUriOrLocalPackage(uriString))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        input.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return null;
            }
        }

        public bool IsLocalFile(string uriString)
        {
            // access
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));
            if (uriString == null || uriString == "" || !uriString.StartsWith("/"))
                return false;

            // check
            var isLocal = _openPackage.PartExists(new Uri(uriString, UriKind.RelativeOrAbsolute));
            return isLocal;
        }

        public Stream GetLocalStreamFromPackage(string uriString, FileMode mode = FileMode.Open)
        {
            // access
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));

            // gte part
            var part = _openPackage.GetPart(new Uri(uriString, UriKind.RelativeOrAbsolute));
            if (part == null)
                throw (new Exception(
                    string.Format($"Cannot access URI {uriString} in {_fn} not opened. Aborting!")));
            return part.GetStream(mode);
        }

        public long GetStreamSizeFromPackage(string uriString)
        {
            long res = 0;
            try
            {
                if (_openPackage == null)
                    return 0;
                var part = _openPackage.GetPart(new Uri(uriString, UriKind.RelativeOrAbsolute));
                using (var s = part.GetStream(FileMode.Open))
                {
                    res = s.Length;
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
                return 0;
            }
            return res;
        }

        /// <remarks>
        /// Ensures:
        /// <ul><li><c>result == null || result.CanRead</c></li></ul>
        /// </remarks>
        public Stream GetLocalThumbnailStream(ref Uri thumbUri)
        {
            // access
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));
            // get the thumbnail over the relationship
            PackagePart thumbPart = null;
            var xs = _openPackage.GetRelationshipsByType(
                "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
            foreach (var x in xs)
                if (x.SourceUri.ToString() == "/")
                {
                    thumbPart = _openPackage.GetPart(x.TargetUri);
                    thumbUri = x.TargetUri;
                    break;
                }
            if (thumbPart == null)
                throw (new Exception("Unable to find AASX thumbnail. Aborting!"));

            var result = thumbPart.GetStream(FileMode.Open);

            // Post-condition
            if (!(result == null || result.CanRead))
            {
                throw new InvalidOperationException("Unexpected unreadable result stream");
            }

            return result;
        }

        /// <remarks>
        /// Ensures:
        /// <ul><li><c>result == null || result.CanRead</c></li></ul>
        /// </remarks>
        public Stream GetLocalThumbnailStream()
        {
            Uri dummy = null;
            var result = GetLocalThumbnailStream(ref dummy);

            // Post-condition
            if (!(result == null || result.CanRead))
            {
                throw new InvalidOperationException("Unexpected unreadable result stream");
            }

            return result;
        }

        public ListOfAasSupplementaryFile GetListOfSupplementaryFiles()
        {
            // new result
            var result = new ListOfAasSupplementaryFile();

            // access
            if (_openPackage != null)
            {

                // get the thumbnail(s) from the package
                var xs = _openPackage.GetRelationshipsByType(
                    "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                foreach (var x in xs)
                    if (x.SourceUri.ToString() == "/")
                    {
                        result.Add(new AdminShellPackageSupplementaryFile(
                            x.TargetUri,
                            location: AdminShellPackageSupplementaryFile.LocationType.InPackage,
                            specialHandling: AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail));
                    }

                // get the origin from the package
                PackagePart originPart = null;
                xs = _openPackage.GetRelationshipsByType(
                    "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                foreach (var x in xs)
                    if (x.SourceUri.ToString() == "/")
                    {
                        originPart = _openPackage.GetPart(x.TargetUri);
                        break;
                    }

                if (originPart != null)
                {
                    // get the specs from the origin
                    PackagePart specPart = null;
                    xs = originPart.GetRelationshipsByType("http://www.admin-shell.io/aasx/relationships/aas-spec");
                    foreach (var x in xs)
                    {
                        specPart = _openPackage.GetPart(x.TargetUri);
                        break;
                    }

                    if (specPart != null)
                    {
                        // get the supplementaries from the package, derived from spec
                        xs = specPart.GetRelationshipsByType("http://www.admin-shell.io/aasx/relationships/aas-suppl");
                        foreach (var x in xs)
                        {
                            result.Add(
                                new AdminShellPackageSupplementaryFile(
                                    x.TargetUri, location: AdminShellPackageSupplementaryFile.LocationType.InPackage));
                        }
                    }
                }
            }

            // add or modify the files to delete
            foreach (var psfDel in _pendingFilesToDelete)
            {
                // already in
                var found = result.Find(x => { return x.Uri == psfDel.Uri; });
                if (found != null)
                    found.Location = AdminShellPackageSupplementaryFile.LocationType.DeletePending;
                else
                {
                    psfDel.Location = AdminShellPackageSupplementaryFile.LocationType.DeletePending;
                    result.Add(psfDel);
                }
            }

            // add the files to store as well
            foreach (var psfAdd in _pendingFilesToAdd)
            {
                // already in (should not happen ?!)
                var found = result.Find(x => { return x.Uri == psfAdd.Uri; });
                if (found != null)
                    found.Location = AdminShellPackageSupplementaryFile.LocationType.AddPending;
                else
                {
                    psfAdd.Location = AdminShellPackageSupplementaryFile.LocationType.AddPending;
                    result.Add(psfAdd);
                }
            }

            // done
            return result;
        }

        public static string GuessMimeType(string fn)
        {
            var file_ext = System.IO.Path.GetExtension(fn).ToLower().Trim();
            var content_type = System.Net.Mime.MediaTypeNames.Text.Plain;
            if (file_ext == ".pdf") content_type = System.Net.Mime.MediaTypeNames.Application.Pdf;
            if (file_ext == ".xml") content_type = System.Net.Mime.MediaTypeNames.Text.Xml;
            if (file_ext == ".txt") content_type = System.Net.Mime.MediaTypeNames.Text.Plain;
            if (file_ext == ".igs") content_type = "application/iges";
            if (file_ext == ".iges") content_type = "application/iges";
            if (file_ext == ".stp") content_type = "application/step";
            if (file_ext == ".step") content_type = "application/step";
            if (file_ext == ".jpg") content_type = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            if (file_ext == ".jpeg") content_type = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            if (file_ext == ".png") content_type = "image/png";
            if (file_ext == ".gif") content_type = System.Net.Mime.MediaTypeNames.Image.Gif;
            return content_type;
        }

        public void PrepareSupplementaryFileParameters(ref string targetDir, ref string targetFn)
        {
            // re-work target dir
            if (targetDir != null)
                targetDir = targetDir.Replace(@"\", "/");

            // rework targetFn
            if (targetFn != null)
                targetFn = Regex.Replace(targetFn, @"[^A-Za-z0-9-.]+", "_");
        }

        /// <summary>
        /// Add a file as supplementary file to package. Operation will be pending, package needs to be saved in order
        /// materialize embedding.
        /// </summary>
        /// <returns>Target path of file in package</returns>
        public string AddSupplementaryFileToStore(
            string sourcePath, string targetDir, string targetFn, bool embedAsThumb,
            AdminShellPackageSupplementaryFile.SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            // beautify parameters
            if ((sourcePath == null && sourceGetBytesDel == null) || targetDir == null || targetFn == null)
                return null;

            // build target path
            targetDir = targetDir.Trim();
            if (!targetDir.EndsWith("/"))
                targetDir += "/";
            targetFn = targetFn.Trim();
            if (sourcePath == "" || targetDir == "" || targetFn == "")
                throw (new Exception("Trying add supplementary file with empty name or path!"));

            var targetPath = "" + targetDir.Trim() + targetFn.Trim();

            // base function
            AddSupplementaryFileToStore(sourcePath, targetPath, embedAsThumb, sourceGetBytesDel, useMimeType);

            // return target path
            return targetPath;
        }

        public void AddSupplementaryFileToStore(string sourcePath, string targetPath, bool embedAsThumb,
            AdminShellPackageSupplementaryFile.SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            // beautify parameters
            if ((sourcePath == null && sourceGetBytesDel == null) || targetPath == null)
                return;

            sourcePath = sourcePath?.Trim();
            targetPath = targetPath.Trim();

            // add record
            _pendingFilesToAdd.Add(
                new AdminShellPackageSupplementaryFile(
                    new Uri(targetPath, UriKind.RelativeOrAbsolute),
                    sourcePath,
                    location: AdminShellPackageSupplementaryFile.LocationType.AddPending,
                    specialHandling: (embedAsThumb
                        ? AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail
                        : AdminShellPackageSupplementaryFile.SpecialHandlingType.None),
                    sourceGetBytesDel: sourceGetBytesDel,
                    useMimeType: useMimeType)
                );

        }

        public void DeleteSupplementaryFile(AdminShellPackageSupplementaryFile psf)
        {
            if (psf == null)
                throw (new Exception("No supplementary file given!"));

            if (psf.Location == AdminShellPackageSupplementaryFile.LocationType.AddPending)
            {
                // is still pending in add list -> remove
                _pendingFilesToAdd.RemoveAll((x) => { return x.Uri == psf.Uri; });
            }

            if (psf.Location == AdminShellPackageSupplementaryFile.LocationType.InPackage)
            {
                // add to pending delete list
                _pendingFilesToDelete.Add(psf);
            }
        }

        public void Close()
        {
            _openPackage?.Close();
            _openPackage = null;
            _fn = "";
            _aasEnv = null;
        }

        public void Dispose()
        {
            Close();
        }

        public string MakePackageFileAvailableAsTempFile(string packageUri, bool keepFilename = false)
        {
            // access
            if (packageUri == null)
                return null;

            // get input stream
            using (var input = GetLocalStreamFromPackage(packageUri))
            {

                // generate tempfile name
                string tempext = System.IO.Path.GetExtension(packageUri);
                string temppath = System.IO.Path.GetTempFileName().Replace(".tmp", tempext);

                // maybe modify tempfile name?
                if (keepFilename)
                {
                    var masterFn = System.IO.Path.GetFileNameWithoutExtension(packageUri);
                    var tmpDir = System.IO.Path.GetDirectoryName(temppath);
                    var tmpFnExt = System.IO.Path.GetFileName(temppath);

                    temppath = System.IO.Path.Combine(tmpDir, "" + masterFn + "_" + tmpFnExt);
                }

                // copy to temp file
                using (var temp = File.OpenWrite(temppath))
                {
                    input.CopyTo(temp);
                    return temppath;
                }
            }
        }

    }
}
