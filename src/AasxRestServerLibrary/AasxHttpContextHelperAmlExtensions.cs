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
    public static class AasxHttpContextHelperAmlExtensions
    {
        /**
         * This method is able to evaluate an AutomationML fragment and return a suitable serialization.
         */
        public static void EvalGetAMLFragment(this AasxHttpContextHelper helper, IHttpContext context, Stream amlFileStream, string amlFragment)
        {
            CAEXBasicObject fragmentObject;

            try
            {
                CAEXDocument caexDocument = LoadCaexDocument(amlFileStream);
                fragmentObject = FindFragmentObject(caexDocument, amlFragment);

                var content = context.Request.QueryString.Get("content") ?? "normal";
                var level = context.Request.QueryString.Get("level") ?? "deep";
                var extent = context.Request.QueryString.Get("extent") ?? "withoutBlobValue";

                if (level == "core")
                {
                    DeeplyNestedElementsRemover.RemoveDeeplements(fragmentObject);
                }

                if (content == "xml")
                {
                    SendXmlResponse(context, fragmentObject.Node);
                }
                else
                {
                    JsonConverter converter = new AmlJsonConverter(content, extent);
                    string json = JsonConvert.SerializeObject(fragmentObject, Newtonsoft.Json.Formatting.Indented, converter);

                    SendJsonResponse(context, json);
                }

                return;
            }
            catch (AmlFragmentEvaluationException e)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    e.Message);
                return;
            }
        }

        public static CAEXDocument LoadCaexDocument(Stream amlFileStream)
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
                }
                catch
                {
                    throw new AmlFragmentEvaluationException($"Unable to load AML file/container from stream.");
                }
            }
        }

        private static CAEXBasicObject FindFragmentObject(CAEXDocument caexDocument, string amlFragment)
        {
            CAEXBasicObject fragmentObject;

            String caexPath = amlFragment.Trim('/');
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
                        throw new AmlFragmentEvaluationException($"Unable to locate element with ID '" + id + "' within AML file.");
                    }

                    caexPath = caexObject.GetFullNodePath() + idFragmentMatches[0].Groups[2].ToString();
                }

                fragmentObject = caexDocument.FindByPath(caexPath);
            }

            if (fragmentObject == null)
            {
                throw new AmlFragmentEvaluationException($"Unable to locate element with path '" + amlFragment + "' within AML file.");
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
     * A JsonConverter that converts any CAEXBasicObject to a JSON representation. The converter refers to the parameters 'content' and 
     * 'extent' as defined by "Details of the AAS, part 2".
     * 
     * Note: The serialization algorithm for 'content=normal' is based on converting the XML represenation of the CAEXBsicObject to JSON.
     */
    class AmlJsonConverter : JsonConverter
    {
        string Content;
        string Extent;

        public AmlJsonConverter(string content = "normal", string extent = "withoutBlobValue")
        {
            this.Content = content;
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
                throw new AmlFragmentEvaluationException("Unable to convert object to CAEXBasicObject: " + value);
            }

            JContainer result;

            if (Content == "normal")
            {
                result = JObject.FromObject(basicObject.Node);
            }
            else if (Content == "path")
            {
                List<string> paths = CollectCaexPaths(basicObject);
                result = JArray.FromObject(paths);
            }
            else if (Content == "value")
            {
                result = BuildJsonValueRecursively(basicObject);
            }
            else
            {
                throw new AmlFragmentEvaluationException("Unsupported content modifier: " + Content);
            }

            result.WriteTo(writer);
            return;

        }

        private JObject BuildJsonValueRecursively(CAEXBasicObject value)
        {
            JObject o = new JObject();

            if (value is CAEXFileType)
            {
                var caexFile = value as CAEXFileType;
                caexFile.InstanceHierarchy.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
                caexFile.SystemUnitClassLib.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
                caexFile.RoleClassLib.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
                caexFile.InterfaceClassLib.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
                caexFile.AttributeTypeLib.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            }
            (value as SystemUnitClassLibType)?.SystemUnitClass.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as InterfaceClassLibType)?.InterfaceClass.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as RoleClassLibType)?.RoleClass.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as AttributeTypeLibType)?.AttributeType.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as IInternalElementContainer)?.InternalElement.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as IObjectWithExternalInterface)?.ExternalInterface.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as SystemUnitFamilyType)?.SystemUnitClass.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as RoleFamilyType)?.RoleClass.ToList().ForEach(element => BuildJsonValueRecursively(element, o));
            (value as AttributeFamilyType)?.AttributeType.ToList().ForEach(element => BuildJsonValueRecursively(element, o));

            // fixme: how to handle/serialize nested attributes? should we incorporate the nested path (instead of the name) as JSON property name?
            (value as IObjectWithAttributes)?.AttributeAndDescendants.ToList().ForEach(attribute => o[attribute.Name] = attribute.Value);

            if (value is AttributeType)
            {
                o["@Value"] = (value as AttributeType).Value;
            }

            return o;

        }

        private void BuildJsonValueRecursively(CAEXObject childObject, JObject parentJObject)
        {
            parentJObject[childObject.Name] = BuildJsonValueRecursively(childObject);
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


    }

    /**
     * A utility class that can be used to remove 'deeply nested elements' from a CAEXBasicObject, i.e. elements that
     * are descendants but no direct children of the given object.
     * 
     * Note: This will only remove deeply nested elements of certain types (e.g. InternalElements, ExternalInterfaces, 
     * etc. but not Attributes, AdditionalInformation, etc.) to be aligned as closely as possible with 'Details of the
     * AAS', part 2.
     */
    class DeeplyNestedElementsRemover
    {
        private DeeplyNestedElementsRemover() { }

        public static void RemoveDeeplements(CAEXBasicObject value)
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
            (value as SystemUnitClassLibType)?.SystemUnitClass.ToList().ForEach(RemoveNestedElements);
            (value as InterfaceClassLibType)?.InterfaceClass.ToList().ForEach(RemoveNestedElements);
            (value as RoleClassLibType)?.RoleClass.ToList().ForEach(RemoveNestedElements);
            (value as AttributeTypeLibType)?.AttributeType.ToList().ForEach(RemoveNestedElements);
            (value as IInternalElementContainer)?.InternalElement.ToList().ForEach(RemoveNestedElements);
            (value as IObjectWithExternalInterface)?.ExternalInterface.ToList().ForEach(RemoveNestedElements);
            (value as SystemUnitFamilyType)?.SystemUnitClass.ToList().ForEach(RemoveNestedElements);
            (value as RoleFamilyType)?.RoleClass.ToList().ForEach(RemoveNestedElements);
            (value as AttributeFamilyType)?.AttributeType.ToList().ForEach(RemoveNestedElements);
        }

        private static void RemoveNestedElements(InstanceHierarchyType ih)
        {
            ih.InternalElement.Remove();
        }

        private static void RemoveNestedElements(SystemUnitClassLibType sucl)
        {
            sucl.SystemUnitClass.Remove();
        }

        private static void RemoveNestedElements(RoleClassLibType rcl)
        {
            rcl.RoleClass.Remove();
        }

        private static void RemoveNestedElements(InterfaceClassLibType icl)
        {
            icl.InterfaceClass.Remove();
        }
        private static void RemoveNestedElements(AttributeTypeLibType atl)
        {
            atl.AttributeType.Remove();
        }
        private static void RemoveNestedElements(InternalElementType ie)
        {
            ie.InternalElement.Remove();
            ie.InternalLink.Remove();
            ie.ExternalInterface.Remove();
        }
        private static void RemoveNestedElements(SystemUnitFamilyType suf)
        {
            suf.SystemUnitClass.Remove();
            suf.InternalElement.Remove();
            suf.ExternalInterface.Remove();
        }
        private static void RemoveNestedElements(ExternalInterfaceType ei)
        {
            ei.ExternalInterface.Remove();
        }
        private static void RemoveNestedElements(RoleClassType rc)
        {
            rc.ExternalInterface.Remove();
        }
        private static void RemoveNestedElements(InterfaceClassType ic)
        {
            ic.ExternalInterface.Remove();
        }
        private static void RemoveNestedElements(InterfaceFamilyType ift)
        {
            ift.InterfaceClass.Remove();
        }
        private static void RemoveNestedElements(AttributeFamilyType aft)
        {
            aft.AttributeType.Remove();
        }

    }

    /**
     * An exception that indicates that something went wrong while evaluating an AML20 fragment.
     */
    public class AmlFragmentEvaluationException : ArgumentException
    {

        public AmlFragmentEvaluationException(string message) : base(message)
        {
        }

        public AmlFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}