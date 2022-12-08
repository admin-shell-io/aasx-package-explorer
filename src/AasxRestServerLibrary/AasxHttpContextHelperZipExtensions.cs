using Newtonsoft.Json;
using System;
using System.Text;
using System.Linq;
using Grapevine.Shared;
using Grapevine.Interfaces.Server;
using System.IO;
using Grapevine.Server;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.IO.Compression;

namespace AasxRestServerLibrary
{
    public static class AasxHttpContextHelperZipExtensions
    {
        /**
         * This method is able to evaluate a ZIP fragment and return a suitable serialization.
         */
        public static void EvalGetZIPFragment(this AasxHttpContextHelper helper, IHttpContext context, Stream zipFileStream, string zipFragment)
        {
            try
            {
                ZipArchive archive = LoadZipArchive(zipFileStream);

                var fragmentObject = FindFragmentObject(archive, zipFragment);

                var content = context.Request.QueryString.Get("content") ?? "normal";
                var level = context.Request.QueryString.Get("level") ?? "deep";
                var extent = context.Request.QueryString.Get("extent") ?? "withoutBlobValue";

                if (fragmentObject == null)
                {
                    throw new ZipFragmentEvaluationException($"Fragment evaluation did not return an element.");
                }

                if (content == "raw")
                {
                    var stream = GetFragmentObjectAsStream(context, fragmentObject);
                    SendStreamResponse(context, stream, fragmentObject.Name);
                }
                else
                {

                    JsonConverter converter = new ZipJsonConverter(archive, content, extent, level);
                    string json = JsonConvert.SerializeObject(fragmentObject, Newtonsoft.Json.Formatting.Indented, converter);
                    SendJsonResponse(context, json);
                }

                return;
            }
            catch (ZipFragmentEvaluationException e)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    e.Message);
                return;
            }
        }
        public static Stream EvalGetZIPFragmentAsStream(this AasxHttpContextHelper helper, IHttpContext context, Stream zipFileStream, string zipFragment)
        {
            try
            {
                ZipArchive archive = LoadZipArchive(zipFileStream);

                var fragmentObject = FindFragmentObject(archive, zipFragment);
                var stream = GetFragmentObjectAsStream(context, fragmentObject);

                return stream;
            }
            catch (ZipFragmentEvaluationException e)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    e.Message);
                return null;
            }
        }


        private static Stream GetFragmentObjectAsStream(IHttpContext context, dynamic fragmentObject)
        {
            if (fragmentObject is string)
            {
                throw new ZipFragmentEvaluationException($"ZipFragment represents a folder. This is not supported when 'content' is set to 'raw'!");
            }

            var fragmentObjectStream = fragmentObject.Open();
            var decompressedStream = new MemoryStream();

            // this will decompress the file
            fragmentObjectStream.CopyTo(decompressedStream);
            decompressedStream.Position = 0;

            return decompressedStream;
        }

        public static ZipArchive LoadZipArchive(Stream zipFileStream)
        {
            try
            {
                return new ZipArchive(zipFileStream);
            }
            catch
            {
                throw new ZipFragmentEvaluationException($"Unable to load ZIP archive from stream.");
            }
        }

        private static dynamic FindFragmentObject(ZipArchive archive, string zipFragment)
        {
            zipFragment = zipFragment.Trim('/');

            if (zipFragment.Length == 0)
            {
                return zipFragment;
            }

            try
            {
                var entry = archive.GetEntry(zipFragment);

                if (entry != null)
                {
                    if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith("\\"))
                    {
                        return entry;
                    }
                    else
                    {
                        // path represents a directory
                        return zipFragment;
                    }
                }
                else
                {
                    // might be a directory that does not have its own entry
                    var entries = archive.Entries.Where(e => e.FullName.StartsWith(zipFragment));

                    if (entries.Count() > 0)
                    {
                        return zipFragment;
                    }
                    else
                    {
                        return null;
                    }

                }
            }
            catch
            {
                throw new ZipFragmentEvaluationException($"An error occurred while evaluating zip fragment '" + zipFragment);
            }

        }

        static void SendJsonResponse(IHttpContext context, string json)
        {
            var buffer = context.Request.ContentEncoding.GetBytes(json);
            var length = buffer.Length;

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = length;
            context.Response.SendResponse(buffer);
        }

        static void SendStreamResponse(
            IHttpContext context, Stream stream, string headerAttachmentFileName = null)
        {
            context.Response.ContentType = ContentType.APPLICATION;
            context.Response.ContentLength64 = stream.Length;
            context.Response.SendChunked = true;

            if (headerAttachmentFileName != null)
                context.Response.AddHeader("Content-Disposition", $"attachment; filename={headerAttachmentFileName}");

            stream.CopyTo(context.Response.Advanced.OutputStream);
            context.Response.Advanced.Close();
        }
    }

    /**
     * A JsonConverter that converts any ZipArchiveEntry to a JSON representation. The converter refers to the parameters 'content', 'level' and 
     * 'extent' as defined by "Details of the AAS, part 2".
     * 
     * Note: The serialization algorithm for 'content=normal' is based on directly converting the XML node to JSON.
     */
    public class ZipJsonConverter : JsonConverter
    {
        char[] PathSeparators = new char[] { '/', '\\' };
        string Content;
        string Extent;
        string Level;
        ZipArchive Archive;


        public ZipJsonConverter(ZipArchive archive, string content = "normal", string extent = "withoutBlobValue", string level = "deep")
        {
            this.Archive = archive;
            this.Content = content;
            this.Extent = extent;
            this.Level = level;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ZipArchiveEntry).IsAssignableFrom(objectType) || typeof(string).IsAssignableFrom(objectType);
        }
        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ZipArchiveEntry zipEntry = value as ZipArchiveEntry;

            JContainer result;

            if (Content == "normal")
            {
                result = BuildJsonRecursively(this.Archive, zipEntry != null ? zipEntry.FullName : value as string, this.Level == "deep");
            }
            else if (Content == "path")
            {
                string basePath = "";
                if (zipEntry != null)
                {
                    basePath = zipEntry.FullName;
                }
                else
                {
                    basePath = value as string;
                }

                result = new JArray();
                result.Add(GetChildren(this.Archive, basePath, this.Level == "deep"));
            }
            else
            {
                throw new XmlFragmentEvaluationException("Unsupported content modifier: " + Content);
            }

            result.WriteTo(writer);
            return;

        }

        public JObject BuildJsonRecursively(ZipArchive archive, string path, bool deep, int level = 0)
        {

            ZipArchiveEntry zipEntry = archive.GetEntry(path);

            if (zipEntry != null && zipEntry.Name != "")
            {
                // file
                return new JObject
                {
                    ["type"] = "file",
                    ["name"] = zipEntry.Name,
                    ["fullName"] = zipEntry.FullName,
                    ["compressedLength"] = zipEntry.CompressedLength,
                    ["length"] = zipEntry.Length
                };
            }
            else
            {
                // directory
                var name = path.Trim(PathSeparators).Split(PathSeparators).Last();
                JObject result = new JObject
                {
                    ["type"] = "directory",
                    ["name"] = name,
                    ["fullName"] = path
                };

                if (deep || level == 0)
                {
                    // only go to the first level of children in case we are in 'core' mode

                    try
                    {
                        var children = GetChildren(archive, path, false);

                        if (children.Count() == 0)
                        {
                            return result;
                        }

                        JArray childArray = new JArray();

                        foreach (var child in children)
                        {
                            childArray.Add(BuildJsonRecursively(archive, child, deep, level + 1));
                        }

                        result["subEntries"] = childArray;
                    }
                    catch (Exception e)
                    {
                        throw new ZipFragmentEvaluationException("An internal error occurred while trying to recurse through the ZIP file!", e);
                    }

                }

                return result;
            }
        }

        private IEnumerable<string> GetChildren(ZipArchive archive, string path, bool deep)
        {
            SortedSet<string> result = new SortedSet<string>();

            foreach (var entry in archive?.Entries)
            {
                string entryPath = entry.FullName?.TrimEnd(PathSeparators).Replace("\\\\", "\\");
                string relativeEntryPath = GetRelativePath(path, entryPath);

                if (relativeEntryPath == null)
                {
                    continue;
                }

                string[] entryParts = relativeEntryPath.Trim(PathSeparators).Split(PathSeparators);

                if (deep || entryParts.Length == 2)
                {
                    // some zip files do not contain entries for folders, hence, we add the folder manually
                    int place = entryPath.LastIndexOf(entryParts.Last());
                    result.Add(entryPath.Remove(place, entryParts.Last().Length).TrimEnd(PathSeparators));
                }

                if (deep || entryParts.Length == 1)
                {
                    result.Add(entryPath);
                }
            }

            return result;
        }

        private string GetRelativePath(string sourcePath, string targetPath)
        {
            if (targetPath == null || !targetPath.StartsWith(sourcePath) || sourcePath == targetPath)
            {
                return null;
            }

            if (sourcePath == null || sourcePath.Length == 0)
            {
                return targetPath;
            }

            string relativePath = targetPath.Substring(sourcePath.Length, targetPath.Length - sourcePath.Length);

            if (relativePath.Length == 1 && PathSeparators.Contains(relativePath[0]))
            {
                // the paths are actually the same and only differ in a trailing slash
                return null;
            }

            return relativePath;
        }
    }

    /**
     * An exception that indicates that something went wrong while evaluating a ZIP fragment.
     */
    public class ZipFragmentEvaluationException : ArgumentException
    {

        public ZipFragmentEvaluationException(string message) : base(message)
        {
        }

        public ZipFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
