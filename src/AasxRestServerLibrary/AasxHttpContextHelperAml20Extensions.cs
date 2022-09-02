using Newtonsoft.Json;
using System;
using System.Text;
using Aml.Engine.CAEX;
using System.Linq;
using Grapevine.Shared;
using Grapevine.Interfaces.Server;
using System.IO;
using Aml.Engine.AmlObjects;
using Grapevine.Server;
using System.Text.RegularExpressions;
using System.Web;
using Aml.Engine.CAEX.Extensions;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace AasxRestServerLibrary
{
    public static class AasxHttpContextHelperAml20Extensions
    {
        /**
         * This method is able to evaluate an AutomationML20 fragment and return a suitable serialization.
         */
        public static void EvalGetAML20Fragment(this AasxHttpContextHelper helper, IHttpContext context, Stream amlFileStream, string amlFragment)
        {
            CAEXBasicObject fragmentObject;

            try
            {
                CAEXDocument caexDocument = LoadCaexDocument(amlFileStream);
                fragmentObject = FindFragmentObject(caexDocument, amlFragment);

            } catch (Aml20FragmentEvaluationException e)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    e.Message);
                return;
            }

            var content = context.Request.QueryString.Get("Content") ?? "normal";
            var level = context.Request.QueryString.Get("Level") ?? "deep";
            var extent = context.Request.QueryString.Get("Extent") ?? "withoutBlobValue";

            if (content == "xml") {
                SendXmlResponse(context, fragmentObject.Node);
            } else
            {
                JsonConverter converter = new Aml20CoreJsonConverter(content, level, extent);
                string json = JsonConvert.SerializeObject(fragmentObject, Newtonsoft.Json.Formatting.Indented, converter);
                
                SendJsonResponse(context, json);
            }

            return;
        }

        private static CAEXDocument LoadCaexDocument(Stream amlFileStream)
        {
            try
            {
                // first try: 'normal' AML file
                return CAEXDocument.LoadFromStream(amlFileStream);
            }
            catch
            {
                // second try: AMLX package
                try
                {
                    var amlContainer = new AutomationMLContainer(amlFileStream);
                    return CAEXDocument.LoadFromStream(amlContainer.RootDocumentStream());
                } catch
                {
                    throw new Aml20FragmentEvaluationException($"Unable to load AML file/container from stream.");
                }
            }
        }

        private static CAEXBasicObject FindFragmentObject(CAEXDocument caexDocument, string amlFragment)
        {
            CAEXBasicObject fragmentObject;

            String caexPath = HttpUtility.UrlDecode(amlFragment.Trim('/'));
            if (caexPath.Length == 0)
            {
                fragmentObject = caexDocument.CAEXFile;
            }
            else
            {

                Regex guidFragmentRegEx = new Regex(@"^([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                // If the path starts with an ID, we cannot use 'FindByPath'. Hence, we need to first find the element identified by the ID, get its CAEX path, and then build the resulting CAEX path.
                if (guidFragmentRegEx.IsMatch(caexPath))
                {
                    MatchCollection idFragmentMatches = guidFragmentRegEx.Matches(amlFragment);
                    var id = idFragmentMatches[0].Groups[1].ToString();
                    var caexObject = caexDocument.FindByID(id);

                    if (caexObject == null)
                    {
                        throw new Aml20FragmentEvaluationException($"Unable to locate element with ID '" + id + "' within AML file.");
                    }

                    caexPath = caexObject.GetFullNodePath() + idFragmentMatches[0].Groups[2].ToString();
                }

                fragmentObject = caexDocument.FindByPath(caexPath);
            }

            if (fragmentObject == null)
            {
                throw new Aml20FragmentEvaluationException($"Unable to locate element with path '" + amlFragment + "' within AML file.");
            }

            return fragmentObject;
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

        static void SendXmlResponse(IHttpContext context, XElement element, string mimeType = "text/xml")
        {
            string txt = element.ToString();
            context.Response.ContentType = ContentType.XML;
            if (mimeType != null)
                context.Response.Advanced.ContentType = mimeType;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = txt.Length;
            context.Response.SendResponse(txt);
        }

    }

    /**
     * A JsonConverter that converts any CAEXBasicObject to a JSON representation but limits this representation to the 'core' level as defined by "Details of the AAS, part 2".
     * This means that only the element and direct children a serialized but nested children are skipped in the serialization.
     * The basic serialization is based on converting the XML represenation of the CAEXBsicObject to JSON.
     */ 
    class Aml20CoreJsonConverter : JsonConverter
    {
        string Content;
        string Level;
        string Extent;
        
        public Aml20CoreJsonConverter(string content = "normal", string level = "deep", string extent = "withoutBlobValue")
        {
            this.Content = content;
            this.Level = level;
            this.Extent = extent;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(CAEXBasicObject).IsAssignableFrom(objectType);
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
            CAEXBasicObject basicObject = value as CAEXBasicObject;

            if (basicObject == null)
            {
                throw new Aml20FragmentEvaluationException("Unable to convert object to CAEXBasicObject: " + value);
            }

            if (Level == "core")
            {
                RemoveDeeplements(basicObject);
            }

            if (Content == "normal")
            {
                JObject o = JObject.FromObject(basicObject.Node);
                o.WriteTo(writer);
                return;
            } else if (Content == "path")
            {
                List<string> paths = CollectCaexPaths(basicObject);

                JArray o = JArray.FromObject(paths);
                o.WriteTo(writer);
                return;
            } else if (Content == "value")
            {

            }
            
        }

        private List<string> CollectCaexPaths(CAEXBasicObject caexBasicObject)
        {
            var paths = new List<string>();

            if (caexBasicObject is CAEXObject)
            {
                paths.Add((caexBasicObject as CAEXObject).GetFullNodePath());
            }

            paths.AddRange(ChildPaths<InternalElementType>(caexBasicObject));
            paths.AddRange(ChildPaths<SystemUnitFamilyType>(caexBasicObject));
            paths.AddRange(ChildPaths<RoleClassType>(caexBasicObject));
            paths.AddRange(ChildPaths<InterfaceClassType>(caexBasicObject));
            paths.AddRange(ChildPaths<AttributeTypeType>(caexBasicObject));

            return paths;
        }

        private IEnumerable<string> ChildPaths<T>(CAEXBasicObject basicObject) where T : CAEXObject
        {
            IEnumerable<T> children = basicObject.Descendants<T>();
            
            return children.Select(c => c.GetFullNodePath());
        }

        private void RemoveDeeplements(CAEXBasicObject value)
        {
            if (value is CAEXFileType)
            {
                var caexFile = value as CAEXFileType;
                caexFile.InstanceHierarchy.ToList().ForEach(RemoveNestedElements);
                caexFile.SystemUnitClassLib.ToList().ForEach(RemoveNestedElements);
                caexFile.RoleClassLib.ToList().ForEach(RemoveNestedElements);
                caexFile.InterfaceClassLib.ToList().ForEach(RemoveNestedElements);
                caexFile.AttributeTypeLib.ToList().ForEach(RemoveNestedElements);
            }
            if (value is SystemUnitClassLibType)
            {
                (value as SystemUnitClassLibType).SystemUnitClass.ToList().ForEach(RemoveNestedElements);
            }
            if (value is InterfaceClassLibType)
            {
                (value as InterfaceClassLibType).InterfaceClass.ToList().ForEach(RemoveNestedElements);
            }
            if (value is RoleClassLibType)
            {
                (value as RoleClassLibType).RoleClass.ToList().ForEach(RemoveNestedElements);
            }
            if (value is AttributeTypeLibType)
            {
                (value as AttributeTypeLibType).AttributeType.ToList().ForEach(RemoveNestedElements);
            }
            if (value is IInternalElementContainer)
            {
                (value as IInternalElementContainer).InternalElement.ToList().ForEach(RemoveNestedElements);
            }
            if (value is IClassWithExternalInterface)
            {
                (value as IClassWithExternalInterface).ExternalInterface.ToList().ForEach(RemoveNestedElements);
            }
            if (value is SystemUnitFamilyType)
            {
                (value as SystemUnitFamilyType).SystemUnitClass.ToList().ForEach(RemoveNestedElements);
            }
            if (value is RoleFamilyType)
            {
                (value as RoleFamilyType).RoleClass.ToList().ForEach(RemoveNestedElements);
            }
            if (value is AttributeFamilyType)
            {
                (value as AttributeFamilyType).AttributeType.ToList().ForEach(RemoveNestedElements);
            }
        }

        private void RemoveNestedElements(InstanceHierarchyType ih)
        {
            ih.InternalElement.Remove();
        }

        private void RemoveNestedElements(SystemUnitClassLibType sucl)
        {
            sucl.SystemUnitClass.Remove();
        }

        private void RemoveNestedElements(RoleClassLibType rcl)
        {
            rcl.RoleClass.Remove();
        }

        private void RemoveNestedElements(InterfaceClassLibType icl)
        {
            icl.InterfaceClass.Remove();
        }
        private void RemoveNestedElements(AttributeTypeLibType atl)
        {
            atl.AttributeType.Remove();
        }
        private void RemoveNestedElements(InternalElementType ie)
        {
            ie.InternalElement.Remove();
            ie.InternalLink.Remove();
            ie.ExternalInterface.Remove();
        }
        private void RemoveNestedElements(SystemUnitFamilyType suf)
        {
            suf.SystemUnitClass.Remove();
            suf.InternalElement.Remove();
            suf.ExternalInterface.Remove();
        }
        private void RemoveNestedElements(ExternalInterfaceType ei)
        {
            ei.ExternalInterface.Remove();
        }
        private void RemoveNestedElements(RoleClassType rc)
        {
            rc.ExternalInterface.Remove();
        }
        private void RemoveNestedElements(InterfaceClassType ic)
        {
            ic.ExternalInterface.Remove();
        }
        private void RemoveNestedElements(InterfaceFamilyType ift) {
            ift.InterfaceClass.Remove();
        }
        private void RemoveNestedElements(AttributeFamilyType aft)
        {
            aft.AttributeType.Remove();
        }        
    }

    /**
     * An exception that indicates that something went wrong while evaluating an AML20 fragment.
     */
    public class Aml20FragmentEvaluationException : ArgumentException {

        public Aml20FragmentEvaluationException(string message) : base(message)
        {
        }

        public Aml20FragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}