/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable
{
    public static class MyExtensions
    {
    }

    /// <summary>
    /// This class allows exporting a Submodel to various UML formats.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// domain is quite the same and maybe special file format dependencies will 
    /// be re equired in the future.
    /// </summary>
    public class ExportUml
    {
        public class XmiWriter
        {
            public readonly Tuple<string, string> UMLNS =
                new Tuple<string, string>("UML", "http://omg.org/UML1.3");

            public XmlDocument Doc;
            public XmlElement Package;

            public XmlElement CreateAppendElement(XmlElement root, string name)
            {
                var node = Doc.CreateElement(name);
                Doc.AppendChild(root);
                return node;
            }

            //public XmlElement CreateAppendElement(XmlElement root, string prefix, string localName, string namespaceURI,
            //    params string[] attrs)
            //{
            //    var node = Doc.CreateElement(prefix, localName, namespaceURI);

            //    if (attrs != null && attrs.Length > 0 && attrs.Length % 2 == 0)
            //        for (int i = 0; i < attrs.Length / 2; i++)
            //            node.SetAttribute(attrs[2 * i + 0], attrs[2 * i + 1]);

            //    root.AppendChild(node);
            //    return node;
            //}

            //public XmlElement CreateAppendElement(XmlElement root, Tuple<string, string> ns, string localName,
            //    params string[] attrs)
            //{
            //    return CreateAppendElement(root, ns.Item1, localName, ns.Item2, attrs);
            //}

            public XmlElement CreateAppendElement(XmlElement root,                    
                    string prefix, string localName, string namespaceURI,
                    string[] attrs = null,
                    XmlElement child = null)
            {
                var node = Doc.CreateElement(prefix, localName, namespaceURI);

                if (attrs != null && attrs.Length > 0 && attrs.Length % 2 == 0)
                    for (int i = 0; i < attrs.Length / 2; i++)
                        node.SetAttribute(attrs[2 * i + 0], attrs[2 * i + 1]);

                if (child != null)
                    node.AppendChild(child);

                if (root != null)
                    root.AppendChild(node);

                // always return the DEEPEST child!
                if (child != null)
                    return child;
                else
                    return node;
            }

            public XmlElement CreateAppendElement(XmlElement root,
                Tuple<string, string> ns, string localName,
                string[] attrs = null,
                XmlElement child = null)
            {
                return CreateAppendElement(root, ns.Item1, localName, ns.Item2, attrs, child);
            }

            private Random rnd = new Random();

            public string GenerateId(string prefix)
            {
                return prefix + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + rnd.Next(0, 10000);
            }

            public void StartDoc()
            {
                Doc = new XmlDocument();

                XmlSchema schema = new XmlSchema();
                schema.Namespaces.Add("UML", "http://omg.org/UML1.3");
                Doc.Schemas.Add(schema);
                
                var decl = Doc.CreateXmlDeclaration("1.0", "windows-1252", null);
                Doc.AppendChild(decl);

                var root = Doc.CreateElement("XMI");
                Doc.AppendChild(root);
                root.SetAttribute("xmi.version", "1.1");
                root.SetAttribute("xmlns:UML", "http://omg.org/UML1.3");
                root.SetAttribute("timestamp", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"));                

                var xmicontent = CreateAppendElement(root, "XMI.content");
                root.AppendChild(xmicontent);

                //var model = Doc.CreateElement("UML", "Model", "http://omg.org/UML1.3");
                //xmicontent.AppendChild(model);

                //var umlmodel = CreateAppendElement(xmicontent, UMLNS, "Model", 
                //    "name", "EA Model",
                //    "xmi.id", "MX_EAID_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

                //var modelowned = CreateAppendElement(xmicontent, UMLNS, "Namespace.ownedElement");

                var model = CreateAppendElement(xmicontent, UMLNS, "Model",
                    new[] {
                        "name", "EA Model",
                        "xmi.id", GenerateId("MX_EAID_")
                    });

                var modeloe = CreateAppendElement(model, UMLNS, "Namespace.ownedElement");

                var pack = CreateAppendElement(modeloe, UMLNS, "Package",
                            new[] {
                                "name", "Package1",
                                "xmi.id", GenerateId("EAPK_"),
                                "isRoot", "false",
                                "isLeaf", "false",
                                "isAbstract", "false",
                                "visibility", "public"
                            });

                Package = CreateAppendElement(pack, UMLNS, "Namespace.ownedElement2");
            }

            public string EvalFeatureType(AdminShell.SubmodelElement sme)
            {
                // access
                if (sme == null)
                    return "";

                if (sme is AdminShell.Property p && p.valueType.HasContent())
                    return p.valueType;

                return AdminShell.SubmodelElementWrapper.GetElementNameByAdequateType(sme);
            }

            // TODO (MIHO, 2021-12-24): check if to refactor multiplicity handling as utility

            public string EvalUmlMultiplicity(AdminShell.SubmodelElement sme)
            {                
                string res = null;
                var q = sme?.qualifiers.FindType("Multiplicity");
                if (q != null)
                {
                    foreach (var m in (FormMultiplicity[])Enum.GetValues(typeof(FormMultiplicity)))
                        if (("" + q.value) == Enum.GetName(typeof(FormMultiplicity), m))
                            res = "" + AasFormConstants.FormMultiplicityAsUmlCardinality[(int)m];
                }
                return res;
            }

            public Tuple<string, string> EvalMultiplicityBounds(AdminShell.SubmodelElement sme)
            {
                var mul = EvalUmlMultiplicity(sme);
                if (mul == null || mul.Length < 4)
                    return new Tuple<string, string>("1", "1");
                return new Tuple<string, string>("" + mul[0], "" + mul[3]);
            }

            public void AddFeatures(
                XmlElement featureContainer, 
                List<AdminShell.SubmodelElementWrapper> features)
            {
                if (featureContainer == null || features == null)
                    return;

                foreach (var smw in features)
                    if (smw?.submodelElement is AdminShell.SubmodelElement sme)
                    {
                        var type = EvalFeatureType(sme);
                        var multiplicity = EvalMultiplicityBounds(sme);
                        var initialValue = "";
                        if (sme is AdminShell.Property || sme is AdminShell.Range 
                            || sme is AdminShell.MultiLanguageProperty)
                            initialValue = sme.ValueAsText();

                        var attribute = CreateAppendElement(featureContainer, UMLNS, "Attribute",
                            new[] {
                                "name", "" + sme.idShort,
                                "changeable", "none",
                                "visibility", "public",
                                "ownerScope", "instance",
                                "targetScope", "instance"
                            });

                        if (initialValue.HasContent())
                            CreateAppendElement(attribute, UMLNS, "Attribute.initialValue",
                                child: CreateAppendElement(attribute, UMLNS, "Expression",
                                new[] {
                                    "body", initialValue
                                })
                            );

                        var tv = CreateAppendElement(attribute, UMLNS, "ModelElement.taggedValue");
                        CreateAppendElement(tv, UMLNS, "TaggedValue",
                            new[] { "tag", "type", "value", type });
                        CreateAppendElement(tv, UMLNS, "TaggedValue",
                            new[] { "tag", "lowerBound", "value", multiplicity.Item1 });
                        CreateAppendElement(tv, UMLNS, "TaggedValue",
                            new[] { "tag", "upperBound", "value", multiplicity.Item2 });
                    }
            }

            public void AddClass(AdminShell.Referable rf)
            {
                // the Referable shall enumerate children (if not, then its not a class)
                if (!(rf is AdminShell.IEnumerateChildren rfec))
                    return;
                var features = rfec.EnumerateChildren().ToList();

                // add
                var featureContainer = CreateAppendElement(Package, UMLNS, "Class",
                    new[] {
                        "name", "" + rf.idShort,
                        "visibility", "public",
                        "isRoot", "false",
                        "isLeaf", "false",
                        "isAbstract", "false",
                        "isActive", "false",
                    },
                    CreateAppendElement(null, UMLNS, "Classifier.feature"));

                AddFeatures(featureContainer, features);
            }

            public void ProcessEntity(AdminShell.Referable parent, AdminShell.Referable rf)
            {
                // access
                if (rf == null)
                    return;

                // act flexible                
                AddClass(rf);

                // recurse
                if (rf is AdminShell.IEnumerateChildren rfec)
                {
                    var childs = rfec.EnumerateChildren();
                    if (childs != null)
                        foreach (var c in childs)
                            ProcessEntity(rf, c.submodelElement);
                }
            }

            public void SaveDoc(string fn)
            {
                // access
                if (Doc == null || !fn.HasContent())
                    return;

                // save
                Doc.Save(fn);
            }
        }

        public static void ExportUmlToFile(
            AdminShell.AdministrationShellEnv env,
            AdminShell.Submodel submodel, 
            string fn)
        {
            // access
            int format = -1;
            if (!fn.HasContent())
                return;
            string ext = Path.GetExtension(fn);
            if (!ext.HasContent())
                return;
            if (ext.ToLower() == ".xml")
                format = 0;
            if (format < 0)
                return;

            // XMI??
            if (format == 0)
            {
                var xmi = new XmiWriter();
                xmi.StartDoc();
                xmi.ProcessEntity(null, submodel);
                xmi.SaveDoc(fn);
            }
        }
    }
}
