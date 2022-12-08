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

namespace AasxRestServerLibrary
{
    public static class AasxHttpContextHelperXmlExtensions
    {
        /**
         * This method is able to evaluate an XML fragment and return a suitable serialization.
         */
        public static void EvalGetXMLFragment(this AasxHttpContextHelper helper, IHttpContext context, Stream xmlFileStream, string xmlFragment)
        {
            try
            {
                XDocument xmlDocument = LoadXmlDocument(xmlFileStream);
                IEnumerable<XObject> fragmentObjects = FindFragmentObjects(xmlDocument, xmlFragment);

                var content = context.Request.QueryString.Get("content") ?? "normal";
                var level = context.Request.QueryString.Get("level") ?? "deep";
                var extent = context.Request.QueryString.Get("extent") ?? "withoutBlobValue";

                if (level == "core")
                {
                    foreach (var fragmentObject in fragmentObjects)
                    {
                        if (fragmentObject.NodeType == XmlNodeType.Element)
                        {
                            RemoveDeeplements(fragmentObject as XElement);
                        }
                    }
                }

                if (content == "xml")
                {
                    if (fragmentObjects.Count() > 1)
                    {
                        throw new XmlFragmentEvaluationException($"Fragment evaluation did return multiple XML elements. Only xPath expressions returning a single element are supported when returning xml content.");
                    }

                    var fragmentObject = fragmentObjects.First();

                    if (fragmentObject.NodeType != XmlNodeType.Element)
                    {
                        throw new XmlFragmentEvaluationException($"Fragment evaluation did not return an Element but a(n) " + fragmentObject.NodeType + "!");
                    }

                    SendXmlResponse(context, fragmentObject as XElement);

                }
                else
                {

                    JsonConverter converter = new XmlJsonConverter(xmlFragment, content, extent);
                    string json = JsonConvert.SerializeObject(fragmentObjects, Newtonsoft.Json.Formatting.Indented, converter);

                    SendJsonResponse(context, json);
                }

                return;
            }
            catch (XmlFragmentEvaluationException e)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    e.Message);
                return;
            }
        }

        public static XDocument LoadXmlDocument(Stream xmlFileStream)
        {
            try
            {
                return XDocument.Load(xmlFileStream);
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Unable to load XML file from stream.");
            }
        }

        private static IEnumerable<XObject> FindFragmentObjects(XDocument xmlDocument, string xmlFragment)
        {
            // select the root element if the fragment is empty
            var xPath = (xmlFragment == null || xmlFragment.Length == 0) ? "/*" : xmlFragment;

            XmlNamespaceManager manager = CreateNamespaceManager(xmlDocument);

            object result;
            try
            {
                result = xmlDocument.XPathEvaluate(xPath, manager);
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Unable to compile xPath query '" + xPath + "'.");
            }

            IEnumerable<XObject> nodes;
            try
            {
                nodes = ((IEnumerable<object>)result).Cast<XObject>();
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Evaluating xPath query '" + xPath + "' did not return a node list.");
            }

            if (nodes.Count() == 0)
            {
                throw new XmlFragmentEvaluationException($"Evaluating xPath query '" + xPath + "' did not return a result.");
            }

            return nodes;
        }

        private static XmlNamespaceManager CreateNamespaceManager(XDocument xmlDocument)
        {
            XPathNavigator navigator = xmlDocument.CreateNavigator();
            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
            navigator.MoveToFollowing(XPathNodeType.Element);
            IDictionary<string, string> namespaces = navigator.GetNamespacesInScope(XmlNamespaceScope.All);
            foreach (KeyValuePair<string, string> ns in namespaces)
            {
                manager.AddNamespace(ns.Key, ns.Value);
            }

            return manager;
        }

        /**
         * A utility method that can be used to remove 'deeply nested elements' from an XML element, i.e. elements that
         * are descendants but no direct children of the given object(s).
         */
        public static void RemoveDeeplements(XNode fragmentObject)
        {

            if (fragmentObject.NodeType == XmlNodeType.Element)
            {
                // select all children's children (the elements to be deleted)
                XElement nodeToDelete;

                while ((nodeToDelete = fragmentObject.XPathSelectElement("./*/*")) != null)
                {
                    nodeToDelete.Remove();
                }
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
     * A JsonConverter that converts any XElement to a JSON representation. The converter refers to the parameters 'content' and 
     * 'extent' as defined by "Details of the AAS, part 2".
     * 
     * Note: The serialization algorithm for 'content=normal' is based on directly converting the XML node to JSON.
     */
    class XmlJsonConverter : JsonConverter
    {
        string BaseXpath;
        string Content;
        string Extent;

        public XmlJsonConverter(string xmlFragment, string content = "normal", string extent = "withoutBlobValue")
        {
            this.BaseXpath = xmlFragment;
            this.Content = content;
            this.Extent = extent;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IEnumerable<XObject>).IsAssignableFrom(objectType);
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
            IEnumerable<XObject> nodeList;
            try
            {
                nodeList = (IEnumerable<XObject>)value;
            }
            catch
            {
                throw new XmlFragmentEvaluationException("Unable to convert object to IEnumerable<XObject>: " + value);
            }

            JContainer result = ConvertToJson(nodeList);
            result.WriteTo(writer);

            return;

        }

        private JContainer ConvertToJson(IEnumerable<XObject> nodeList)
        {
            if (nodeList.Count() == 1)
            {
                return ConvertToJson(nodeList.First());
            }
            else
            {
                JArray result = new JArray();

                foreach (var node in nodeList)
                {
                    result.Add(ConvertToJson(node));
                }

                return result;
            }
        }

        private JContainer ConvertToJson(XObject node)
        {
            JContainer result;

            if (Content == "value")
            {
                // in case of a value-only serialization, we remove all comments and namespace-related information,
                // i.e. namespace declarations, namespace prefixes as well as schema location information
                var nodeWithoutNamespaces = RemoveAllNamespacesAndComments(node);
                result = JObject.FromObject(nodeWithoutNamespaces);
            }
            else if (Content == "normal")
            {
                result = JObject.FromObject(node);
            }
            else if (Content == "path")
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    throw new XmlFragmentEvaluationException($"Fragment evaluation did not return an Element but a(n) " + node.NodeType + ". This is not supported when returning path information!");
                }

                var elementXpath = (BaseXpath == null || BaseXpath.Length == 0 || BaseXpath == "/*") ? "/" + GetLocalXpathExpression(node as XElement) : BaseXpath;

                List<string> paths = CollectChildXpathPathsRecursively(node as XElement, elementXpath);
                result = JArray.FromObject(paths);
            }
            else
            {
                throw new XmlFragmentEvaluationException("Unsupported content modifier: " + Content);
            }

            return result;
        }

        private static XObject RemoveAllNamespacesAndComments(XObject xmlObject)
        {
            if (xmlObject is XElement)
            {
                XElement original = xmlObject as XElement;
                XElement copy = new XElement(original.Name.LocalName);
                copy.Add(original.Attributes().Where(att => !att.IsNamespaceDeclaration && att.Name.LocalName != "schemaLocation").Select(att => RemoveAllNamespacesAndComments(att)));
                copy.Add(original.Nodes().Where(n => n.NodeType != XmlNodeType.Comment).Select(el => RemoveAllNamespacesAndComments(el)));

                return copy;
            }
            else if (xmlObject is XAttribute)
            {
                XAttribute original = xmlObject as XAttribute;
                return new XAttribute(original.Name.LocalName, original.Value);
            }
            else
            {
                return xmlObject;
            }
        }

        private string GetLocalXpathExpression(XElement node)
        {
            var nodeName = node.Name;
            if (nodeName.NamespaceName?.Length == 0)
            {
                // the node is not associated with a namespace, so we can simply use the local name as xPath expression
                return nodeName.LocalName;
            }

            var ns = nodeName.Namespace;
            var nsPrefix = node.GetPrefixOfNamespace(ns);

            if (nsPrefix?.Length > 0)
            {
                // there is a prefix for the namespace of the node so we can use this for the xPath expression
                return nsPrefix + ":" + nodeName.LocalName;
            }

            // the node is in the default namespace (without any prefix); hence, we need to use some special xPath syntax
            // to be able to adress the node (see https://stackoverflow.com/a/2530023)
            return "*[namespace-uri()='" + ns.NamespaceName + "' and local-name()='" + nodeName.LocalName + "']";
        }

        private List<string> CollectChildXpathPathsRecursively(XElement xmlElement, string baseXpath)
        {
            var paths = new List<string>();
            paths.Add(baseXpath);

            Dictionary<string, List<XElement>> childDict = GetChildrenSortedByName(xmlElement);

            foreach (var key in childDict?.Keys)
            {
                var values = childDict[key];

                for (int i = 0; i < values.Count; i++)
                {
                    var childXpath = baseXpath + "/" + key;
                    if (values.Count > 1)
                    {
                        // if there are multiple children with the same name, the xPath query needs to contain an array accessor
                        childXpath += "[" + (i + 1) + "]";
                    }

                    paths.AddRange(CollectChildXpathPathsRecursively(values[i], childXpath));
                }
            }

            foreach (var attribute in xmlElement.Attributes())
            {
                var attributeXpath = baseXpath + "/@" + attribute.Name;
                paths.Add(attributeXpath);
            }

            return paths;
        }

        private Dictionary<string, List<XElement>> GetChildrenSortedByName(XElement xmlElement)
        {
            var childDict = new Dictionary<string, List<XElement>>();

            foreach (var child in xmlElement?.Elements().ToList())
            {
                var childName = GetLocalXpathExpression(child);
                List<XElement> childrenWithSameName;
                if (!childDict.TryGetValue(childName, out childrenWithSameName))
                {
                    childrenWithSameName = new List<XElement>();
                }

                childrenWithSameName.Add(child);
                childDict[childName] = childrenWithSameName;
            }

            return childDict;
        }
    }

    /**
     * An exception that indicates that something went wrong while evaluating an XML fragment.
     */
    public class XmlFragmentEvaluationException : ArgumentException
    {

        public XmlFragmentEvaluationException(string message) : base(message)
        {
        }

        public XmlFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}