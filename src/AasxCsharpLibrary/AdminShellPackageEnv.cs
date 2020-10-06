using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

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

        public Uri uri = null;

        public string useMimeType = null;

        public string sourceLocalPath = null;
        public SourceGetByteChunk sourceGetBytesDel = null;

        public LocationType location;
        public SpecialHandlingType specialHandling;

        public AdminShellPackageSupplementaryFile(
            Uri uri, string sourceLocalPath = null, LocationType location = LocationType.InPackage,
            SpecialHandlingType specialHandling = SpecialHandlingType.None,
            SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            this.uri = uri;
            this.useMimeType = useMimeType;
            this.sourceLocalPath = sourceLocalPath;
            this.sourceGetBytesDel = sourceGetBytesDel;
            this.location = location;
            this.specialHandling = specialHandling;
        }

        // class derives from Referable in order to provide GetElementName
        public override string GetElementName()
        {
            return "File";
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
            // ReSharper disable EmptyGeneralCatchClause
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
            catch { }
            // ReSharper enable EmptyGeneralCatchClause

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

    }

    /// <summary>
    /// This class encapsulates an AdminShellEnvironment and supplementary files into an AASX Package.
    /// Specifically has the capability to load, update and store .XML, .JSON and .AASX packages.
    /// </summary>
    public class AdminShellPackageEnv : IDisposable
    {
        private string fn = "New Package";

        private string tempFn = null;

        private AdminShell.AdministrationShellEnv aasenv = new AdminShell.AdministrationShellEnv();
        private Package openPackage = null;
        private List<AdminShellPackageSupplementaryFile> pendingFilesToAdd =
            new List<AdminShellPackageSupplementaryFile>();
        private List<AdminShellPackageSupplementaryFile> pendingFilesToDelete =
            new List<AdminShellPackageSupplementaryFile>();

        public AdminShellPackageEnv()
        {
        }

        public AdminShellPackageEnv(AdminShell.AdministrationShellEnv env)
        {
            if (env != null)
                this.aasenv = env;
        }

        public AdminShellPackageEnv(string fn, bool indirectLoadSave = false)
        {
            Load(fn, indirectLoadSave);
        }

        public bool IsOpen
        {
            get
            {
                return openPackage != null;
            }
        }

        public string Filename
        {
            get
            {
                return fn;
            }
        }

        public AdminShell.AdministrationShellEnv AasEnv
        {
            get
            {
                return aasenv;
            }
        }

        public bool Load(string fn, bool indirectLoadSave = false)
        {
            this.fn = fn;
            if (this.openPackage != null)
                this.openPackage.Close();
            this.openPackage = null;

            if (fn.ToLower().EndsWith(".xml"))
            {
                // load only XML
                try
                {
                    var reader = new StreamReader(fn);
                    this.aasenv = AdminShellSerializationHelper.DeserializeXmlFromStreamWithCompat(reader.BaseStream);
                    if (this.aasenv == null)
                        throw (new Exception("Type error for XML file!"));
                    reader.Close();
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While reading AAS {0} at {1} gave: {2}",
                        fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            if (fn.ToLower().EndsWith(".json"))
            {
                // load only JSON
                try
                {
                    using (StreamReader file = System.IO.File.OpenText(fn))
                    {
                        // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        this.aasenv = (AdminShell.AdministrationShellEnv)serializer.Deserialize(
                            file, typeof(AdminShell.AdministrationShellEnv));
                    }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While reading AAS {0} at {1} gave: {2}",
                            fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            if (fn.ToLower().EndsWith(".aasx"))
            {
                var fnToLoad = fn;
                this.tempFn = null;
                if (indirectLoadSave)
                    try
                    {
                        this.tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".aasx");
                        System.IO.File.Copy(fn, this.tempFn);
                        fnToLoad = this.tempFn;

                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While copying AASX {0} for indirect load at {1} gave: {2}",
                                fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                    }

                // load package AASX
                try
                {
                    var package = Package.Open(fnToLoad, FileMode.Open);

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
                                using (StreamReader file = new StreamReader(s))
                                {
                                    JsonSerializer serializer = new JsonSerializer();
                                    serializer.Converters.Add(
                                        new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                    this.aasenv = (AdminShell.AdministrationShellEnv)serializer.Deserialize(
                                        file, typeof(AdminShell.AdministrationShellEnv));
                                }
                            }
                        }
                        else
                        {
                            using (var s = specPart.GetStream(FileMode.Open))
                            {
                                // own catch loop to be more specific
                                this.aasenv = AdminShellSerializationHelper.DeserializeXmlFromStreamWithCompat(s);
                                this.openPackage = package;
                                if (this.aasenv == null)
                                    throw (new Exception("Type error for XML file!"));
                                s.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw (new Exception(
                            string.Format("While reading AAS {0} spec at {1} gave: {2}",
                                fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                    }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While reading AASX {0} at {1} gave: {2}",
                        fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            // Don't know to handle
            throw (new Exception(string.Format($"Not able to handle {fn}.")));
        }

        public bool LoadFromAasEnvString(string content)
        {
            try
            {
                using (var file = new StringReader(content))
                {
                    // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                    this.aasenv = (AdminShell.AdministrationShellEnv)serializer.Deserialize(
                        file, typeof(AdminShell.AdministrationShellEnv));
                }
            }
            catch (Exception ex)
            {
                throw (new Exception(
                    string.Format("While reading AASENV string {0} gave: {1}",
                        AdminShellUtil.ShortLocation(ex), ex.Message)));
            }
            return true;
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
                    this.fn = fn;
                try
                {
                    Stream s = (useMemoryStream != null) ? (Stream)useMemoryStream
                        : File.Open(fn, FileMode.Create, FileAccess.Write);

                    // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                    var serializer = new XmlSerializer(typeof(AdminShell.AdministrationShellEnv));
                    var nss = GetXmlDefaultNamespaces();
                    serializer.Serialize(s, this.aasenv, nss);
                    s.Flush();

                    // close?
                    if (useMemoryStream == null)
                        s.Close();
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
                // this funcitonality is a initial test
                if (!saveOnlyCopy)
                    this.fn = fn;
                try
                {
                    Stream s = (useMemoryStream != null) ? (Stream)useMemoryStream
                        : File.Open(fn, FileMode.Create, FileAccess.Write);

                    // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                    JsonSerializer serializer = new JsonSerializer()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        Formatting = Newtonsoft.Json.Formatting.Indented
                    };
                    var sw = new StreamWriter(s);
                    var writer = new JsonTextWriter(sw);
                    serializer.Serialize(writer, this.aasenv);
                    writer.Flush();
                    sw.Flush();
                    s.Flush();

                    // close?
                    if (useMemoryStream == null)
                    {
                        writer.Close();
                        sw.Close();
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
                    // we want existing contents to be preserved, but no possiblity to change file name
                    // therefore: copy file to new name, re-open!
                    // fn could be changed, therefore close "old" package first
                    if (this.openPackage != null)
                    {
                        // ReSharper disable EmptyGeneralCatchClause
                        try
                        {
                            this.openPackage.Close();
                            if (!writeFreshly)
                            {
                                if (this.tempFn != null)
                                    System.IO.File.Copy(this.tempFn, fn);
                                else
                                    System.IO.File.Copy(this.fn, fn);
                            }
                        }
                        catch { }
                        // ReSharper enable EmptyGeneralCatchClause
                        this.openPackage = null;
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
                            (this.tempFn != null) ? this.tempFn : fn,
                            (writeFreshly) ? FileMode.Create : FileMode.OpenOrCreate);
                    }
                    this.fn = fn;

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
                            // ReSharper disable EmptyGeneralCatchClause
                            try
                            {
                                originPart.DeleteRelationship(specRel.Id);
                                package.DeletePart(specPart.Uri);
                            }
                            catch { }
                            finally { specPart = null; specRel = null; }
                            // ReSharper enable EmptyGeneralCatchClause
                        }
                    }

                    if (specPart == null)
                    {
                        // create, as not existing
                        var frn = "aasenv-with-no-id";
                        if (this.aasenv.AdministrationShells.Count > 0)
                            frn = this.aasenv.AdministrationShells[0].GetFriendlyName() ?? frn;
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
                                    serializer.Serialize(writer, this.aasenv);
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
                            serializer.Serialize(s, this.aasenv, nss);
                        }
                    }

                    // there might be pending files to be deleted (first delete, then add,
                    // in case of identical files in both categories)
                    foreach (var psfDel in pendingFilesToDelete)
                    {
                        // try find an existing part for that file ..
                        var found = false;

                        // normal files
                        xs = specPart.GetRelationshipsByType("http://www.admin-shell.io/aasx/relationships/aas-suppl");
                        foreach (var x in xs)
                            if (x.TargetUri == psfDel.uri)
                            {
                                // try to delete
                                specPart.DeleteRelationship(x.Id);
                                package.DeletePart(psfDel.uri);
                                found = true;
                                break;
                            }

                        // thumbnails
                        xs = package.GetRelationshipsByType(
                            "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                        foreach (var x in xs)
                            if (x.TargetUri == psfDel.uri)
                            {
                                // try to delete
                                package.DeleteRelationship(x.Id);
                                package.DeletePart(psfDel.uri);
                                found = true;
                                break;
                            }

                        if (!found)
                            throw (new Exception(
                                $"Not able to delete pending file {psfDel.uri} in saving package {fn}"));
                    }

                    // after this, there are no more pending for delete files
                    pendingFilesToDelete.Clear();

                    // write pending supplementary files
                    foreach (var psfAdd in pendingFilesToAdd)
                    {
                        // make sure ..
                        if ((psfAdd.sourceLocalPath == null && psfAdd.sourceGetBytesDel == null) ||
                            psfAdd.location != AdminShellPackageSupplementaryFile.LocationType.AddPending)
                            continue;

                        // normal file?
                        if (psfAdd.specialHandling == AdminShellPackageSupplementaryFile.SpecialHandlingType.None ||
                            psfAdd.specialHandling ==
                                AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                        {

                            // try find an existing part for that file ..
                            PackagePart filePart = null;
                            if (psfAdd.specialHandling == AdminShellPackageSupplementaryFile.SpecialHandlingType.None)
                            {
                                xs = specPart.GetRelationshipsByType(
                                    "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                                foreach (var x in xs)
                                    if (x.TargetUri == psfAdd.uri)
                                    {
                                        filePart = package.GetPart(x.TargetUri);
                                        break;
                                    }
                            }
                            if (psfAdd.specialHandling ==
                                AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                            {
                                xs = package.GetRelationshipsByType(
                                    "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
                                foreach (var x in xs)
                                    if (x.SourceUri.ToString() == "/" && x.TargetUri == psfAdd.uri)
                                    {
                                        filePart = package.GetPart(x.TargetUri);
                                        break;
                                    }
                            }

                            if (filePart == null)
                            {
                                // determine mimeType
                                var mimeType = psfAdd.useMimeType;
                                // reconcile mime
                                if (mimeType == null && psfAdd.sourceLocalPath != null)
                                    mimeType = AdminShellPackageEnv.GuessMimeType(psfAdd.sourceLocalPath);
                                // still null?
                                if (mimeType == null)
                                    // see: https://stackoverflow.com/questions/6783921/
                                    // which-mime-type-to-use-for-a-binary-file-thats-specific-to-my-program
                                    mimeType = "application/octet-stream";

                                // create new part and link
                                filePart = package.CreatePart(psfAdd.uri, mimeType, CompressionOption.Maximum);
                                if (psfAdd.specialHandling ==
                                    AdminShellPackageSupplementaryFile.SpecialHandlingType.None)
                                    specPart.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                                if (psfAdd.specialHandling ==
                                    AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                                    package.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        "http://schemas.openxmlformats.org/package/2006/" +
                                        "relationships/metadata/thumbnail");
                            }

                            // now should be able to write
                            using (var s = filePart.GetStream(FileMode.Create))
                            {
                                if (psfAdd.sourceLocalPath != null)
                                {
                                    var bytes = System.IO.File.ReadAllBytes(psfAdd.sourceLocalPath);
                                    s.Write(bytes, 0, bytes.Length);
                                }

                                if (psfAdd.sourceGetBytesDel != null)
                                {
                                    var bytes = psfAdd.sourceGetBytesDel();
                                    if (bytes != null)
                                        s.Write(bytes, 0, bytes.Length);
                                }
                            }
                        }
                    }

                    // after this, there are no more pending for add files
                    pendingFilesToAdd.Clear();

                    // flush, but leave open
                    package.Flush();
                    this.openPackage = package;

                    // if in temp fn, close the package, copy to original fn, re-open the package
                    if (this.tempFn != null)
                        try
                        {
                            package.Close();
                            System.IO.File.Copy(this.tempFn, this.fn, overwrite: true);
                            this.openPackage = Package.Open(this.tempFn, FileMode.OpenOrCreate);
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
            throw (new Exception(string.Format($"Not able to handle {fn}.")));
        }

        private int BackupIndex = 0;

        public void BackupInDir(string backupDir, int maxFiles)
        {
            // access
            if (backupDir == null || maxFiles < 1)
                return;

            // we do it not caring on any errors
            // ReSharper disable EmptyGeneralCatchClause
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
                    serializer.Serialize(s, this.aasenv, nss);
                }
            }
            catch { }
            // ReSharper enable EmptyGeneralCatchClause
        }

        public Stream GetStreamFromUriOrLocalPackage(string uriString)
        {
            // local
            if (this.IsLocalFile(uriString))
                return GetLocalStreamFromPackage(uriString);

            // no ..
            return File.Open(uriString, FileMode.Open, FileAccess.Read);
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
            catch
            {
                return null;
            }
        }

        public bool IsLocalFile(string uriString)
        {
            // access
            if (this.openPackage == null)
                throw (new Exception(string.Format($"AASX Package {this.fn} not opened. Aborting!")));
            if (uriString == null || uriString == "" || !uriString.StartsWith("/"))
                return false;

            // check
            var isLocal = this.openPackage.PartExists(new Uri(uriString, UriKind.RelativeOrAbsolute));
            return isLocal;
        }

        public Stream GetLocalStreamFromPackage(string uriString)
        {
            // access
            if (this.openPackage == null)
                throw (new Exception(string.Format($"AASX Package {this.fn} not opened. Aborting!")));

            // gte part
            var part = this.openPackage.GetPart(new Uri(uriString, UriKind.RelativeOrAbsolute));
            if (part == null)
                throw (new Exception(
                    string.Format($"Cannot access URI {uriString} in {this.fn} not opened. Aborting!")));
            return part.GetStream(FileMode.Open);
        }

        public long GetStreamSizeFromPackage(string uriString)
        {
            long res = 0;
            try
            {
                if (this.openPackage == null)
                    return 0;
                var part = this.openPackage.GetPart(new Uri(uriString, UriKind.RelativeOrAbsolute));
                using (var s = part.GetStream(FileMode.Open))
                {
                    res = s.Length;
                }
            }
            catch { return 0; }
            return res;
        }

        public Stream GetLocalThumbnailStream(ref Uri thumbUri)
        {
            // access
            if (this.openPackage == null)
                throw (new Exception(string.Format($"AASX Package {this.fn} not opened. Aborting!")));
            // get the thumbnail over the relationship
            PackagePart thumbPart = null;
            var xs = this.openPackage.GetRelationshipsByType(
                "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail");
            foreach (var x in xs)
                if (x.SourceUri.ToString() == "/")
                {
                    thumbPart = this.openPackage.GetPart(x.TargetUri);
                    thumbUri = x.TargetUri;
                    break;
                }
            if (thumbPart == null)
                throw (new Exception("Unable to find AASX thumbnail. Aborting!"));
            return thumbPart.GetStream(FileMode.Open);
        }

        public Stream GetLocalThumbnailStream()
        {
            Uri dummy = null;
            return GetLocalThumbnailStream(ref dummy);
        }

        public List<AdminShellPackageSupplementaryFile> GetListOfSupplementaryFiles()
        {
            // new result
            var result = new List<AdminShellPackageSupplementaryFile>();

            // access
            if (this.openPackage != null)
            {

                // get the thumbnail(s) from the package
                var xs = this.openPackage.GetRelationshipsByType(
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
                xs = this.openPackage.GetRelationshipsByType(
                    "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                foreach (var x in xs)
                    if (x.SourceUri.ToString() == "/")
                    {
                        originPart = this.openPackage.GetPart(x.TargetUri);
                        break;
                    }

                if (originPart != null)
                {
                    // get the specs from the origin
                    PackagePart specPart = null;
                    xs = originPart.GetRelationshipsByType("http://www.admin-shell.io/aasx/relationships/aas-spec");
                    foreach (var x in xs)
                    {
                        specPart = this.openPackage.GetPart(x.TargetUri);
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
            foreach (var psfDel in pendingFilesToDelete)
            {
                // already in
                var found = result.Find(x => { return x.uri == psfDel.uri; });
                if (found != null)
                    found.location = AdminShellPackageSupplementaryFile.LocationType.DeletePending;
                else
                {
                    psfDel.location = AdminShellPackageSupplementaryFile.LocationType.DeletePending;
                    result.Add(psfDel);
                }
            }

            // add the files to store as well
            foreach (var psfAdd in pendingFilesToAdd)
            {
                // already in (should not happen ?!)
                var found = result.Find(x => { return x.uri == psfAdd.uri; });
                if (found != null)
                    found.location = AdminShellPackageSupplementaryFile.LocationType.AddPending;
                else
                {
                    psfAdd.location = AdminShellPackageSupplementaryFile.LocationType.AddPending;
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

        public void AddSupplementaryFileToStore(
            string sourcePath, string targetDir, string targetFn, bool embedAsThumb,
            AdminShellPackageSupplementaryFile.SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            // beautify parameters
            if ((sourcePath == null && sourceGetBytesDel == null) || targetDir == null || targetFn == null)
                return;

            // build target path
            targetDir = targetDir.Trim();
            if (!targetDir.EndsWith("/"))
                targetDir += "/";
            targetDir = targetDir.Replace(@"\", "/");
            targetFn = targetFn.Trim();
            if (sourcePath == "" || targetDir == "" || targetFn == "")
                throw (new Exception("Trying add supplementary file with empty name or path!"));

            var targetPath = "" + targetDir.Trim() + targetFn.Trim();

            // base funciton
            AddSupplementaryFileToStore(sourcePath, targetPath, embedAsThumb, sourceGetBytesDel, useMimeType);
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
            pendingFilesToAdd.Add(
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

            if (psf.location == AdminShellPackageSupplementaryFile.LocationType.AddPending)
            {
                // is still pending in add list -> remove
                pendingFilesToAdd.RemoveAll((x) => { return x.uri == psf.uri; });
            }

            if (psf.location == AdminShellPackageSupplementaryFile.LocationType.InPackage)
            {
                // add to pending delete list
                pendingFilesToDelete.Add(psf);
            }
        }

        public void Close()
        {
            if (this.openPackage != null)
                this.openPackage.Close();
            this.openPackage = null;
            this.fn = "";
            this.aasenv = null;
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
            var input = this.GetLocalStreamFromPackage(packageUri);

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
            var temp = File.OpenWrite(temppath);
            input.CopyTo(temp);
            temp.Close();
            return temppath;
        }
    }

}
