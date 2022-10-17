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
                XmlDocument xmlDocument = LoadXmlDocument(xmlFileStream);
                XPathNodeIterator fragmentObjectsIterator = FindFragmentObjects(xmlDocument, xmlFragment);

                if (fragmentObjectsIterator.Count > 1)
                {
                    throw new XmlFragmentEvaluationException($"Fragment evaluation did return multiple XML elements. Only xPath expressions returning a single element are supported.");
                }

                fragmentObjectsIterator.MoveNext();
                XPathNavigator fragmentObject = fragmentObjectsIterator.Current;

                var content = context.Request.QueryString.Get("content") ?? "normal";
                var level = context.Request.QueryString.Get("level") ?? "deep";
                var extent = context.Request.QueryString.Get("extent") ?? "withoutBlobValue";

                if (level == "core")
                {
                    DeeplyNestedXmlElementsRemover.RemoveDeeplements(fragmentObject);
                }

                if (content == "xml") {

                    if (fragmentObject.NodeType != XPathNodeType.Element)
                    {
                        throw new XmlFragmentEvaluationException($"Fragment evaluation did not return an Element but a(n) " + fragmentObject.NodeType + "!");
                    }
                    XElement xmlElement = XElement.Parse(fragmentObject.OuterXml);

                    SendXmlResponse(context, xmlElement);

                } else
                {

                    JsonConverter converter = new XmlJsonConverter(xmlFragment, content, extent);
                    string json = JsonConvert.SerializeObject(fragmentObject, Newtonsoft.Json.Formatting.Indented, converter);
                
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

        private static XmlDocument LoadXmlDocument(Stream xmlFileStream)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlFileStream);
                return doc;
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Unable to load XML file from stream.");
            }
        }

        private static XPathNodeIterator FindFragmentObjects(XmlDocument xmlDocument, string xmlFragment)
        {
            var xPath = HttpUtility.UrlDecode(xmlFragment.Trim('/'));

            XPathNavigator navigator = xmlDocument.CreateNavigator();
            XPathExpression query;

            try
            {
                query = navigator.Compile(xPath);
            }
            catch
            {
                throw new XmlFragmentEvaluationException($"Unable to compile xPath query '" + xPath + "'.");
            }

            // register the namespace prefixes used in the XML document so that they can be used in xPath queries
            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
            navigator.MoveToFollowing(XPathNodeType.Element);
            IDictionary<string, string> namespaces = navigator.GetNamespacesInScope(XmlNamespaceScope.All);
            foreach (KeyValuePair<string, string> ns in namespaces)
            {
                manager.AddNamespace(ns.Key, ns.Value);
            }
            navigator.MoveToRoot();

            query.SetContext(manager);
            XPathNodeIterator nodes = navigator.Select(query);

            if (nodes.Count == 0)
            {
                throw new XmlFragmentEvaluationException($"Evaluating xPath query '" + xPath + "' did not return a result.");
            }

            return nodes;
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

        static void SendTextResponse(IHttpContext context, object element, string mimeType = "text/plain")
        {
            string txt = element.ToString();
            context.Response.ContentType = ContentType.TXT;
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
            this.BaseXpath = HttpUtility.UrlDecode(xmlFragment.Trim('/')); ; 
            this.Content = content;
            this.Extent = extent;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(XPathNavigator).IsAssignableFrom(objectType);
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
            XPathNavigator navigator = value as XPathNavigator;

            if (navigator == null)
            {
                throw new XmlFragmentEvaluationException("Unable to convert object to XPathNavigator: " + value);
            }

            JContainer result;


            if (Content == "value")
            {
                result = new JObject();
                result["value"] = navigator.InnerXml;
            } else
            {
                if (navigator.NodeType != XPathNodeType.Element)
                {
                    throw new XmlFragmentEvaluationException($"Unable to convert XML fragment to XElement. Xpath evaluation probably did return a(n) " + navigator.NodeType + " instead!");
                }

                XElement xmlElement = XElement.Parse(navigator.OuterXml);

                if (Content == "normal")
                {
                    result = JObject.FromObject(xmlElement);
                } else if (Content == "path") { 

                    List<string> paths = CollectChildXpathPathsRecursively(xmlElement, BaseXpath);
                    result = JArray.FromObject(paths);
                } else
                {
                    throw new XmlFragmentEvaluationException("Unsupported content modifier: " + Content);
                }
            }

            result.WriteTo(writer);
            return;

        }

        private List<string> CollectChildXpathPathsRecursively(XElement xmlElement, string baseXpath)
        {
            var paths = new List<string>();
            paths.Add(baseXpath);

            Dictionary<XName, List<XElement>> childDict = GetChildrenSortedByName(xmlElement);

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

            return paths;
        }

        private static Dictionary<XName, List<XElement>> GetChildrenSortedByName(XElement xmlElement)
        {
            var childDict = new Dictionary<XName, List<XElement>>();

            foreach (var child in xmlElement?.Elements().ToList())
            {
                List<XElement> childrenWithSameName;
                if (!childDict.TryGetValue(child.Name, out childrenWithSameName))
                {
                    childrenWithSameName = new List<XElement>();
                }

                childrenWithSameName.Add(child);
                childDict[child.Name] = childrenWithSameName;
            }

            return childDict;
        }
    }

    /**
     * A utility class that can be used to remove 'deeply nested elements' from an XML element, i.e. elements that
     * are descendants but no direct children of the given object(s).
     */
    class DeeplyNestedXmlElementsRemover
    {
        private DeeplyNestedXmlElementsRemover() { }

        public static void RemoveDeeplements(XPathNavigator fragmentObject)
        {
            
            if (fragmentObject.NodeType == XPathNodeType.Element)
            {
                XPathNodeIterator nodesToDelete;

                // select all children's children (the elements to be deleted)
                while ((nodesToDelete = fragmentObject.Select("./*/*")).Count > 0)
                {

                    nodesToDelete.MoveNext();
                    if (nodesToDelete.Current.NodeType == XPathNodeType.Element)
                    {
                        nodesToDelete.Current.DeleteSelf();
                    }
                }
            }
        }
    }



    /**
     * An exception that indicates that something went wrong while evaluating an XML fragment.
     */
    public class XmlFragmentEvaluationException : ArgumentException {

        public XmlFragmentEvaluationException(string message) : base(message)
        {
        }

        public XmlFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}