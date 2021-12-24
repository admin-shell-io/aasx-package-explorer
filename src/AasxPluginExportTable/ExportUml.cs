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

                Package = CreateAppendElement(xmicontent, UMLNS, "Model",
                    new[] {
                        "name", "EA Model",
                        "xmi.id", GenerateId("MX_EAID_")
                    },
                    CreateAppendElement(null, UMLNS, "Namespace.ownedElement",
                        child: CreateAppendElement(null, UMLNS, "Package",
                        new[] {
                            "name", "Package1",
                            "xmi.id", GenerateId("EAPK_"),
                            "isRoot", "false",
                            "isLeaf", "false",
                            "isAbstract", "false",
                            "visibility", "public"
                        },
                        CreateAppendElement(null, UMLNS, "Namespace.ownedElement",
                            null
                            )
                        )
                    )
                );
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

            public void AddFeatures(
                XmlElement featureContainer, 
                AdminShell.SubmodelElementWrapperCollection features)
            {
                if (featureContainer == null || features == null)
                    return;

                foreach (var smw in features)
                    if (smw?.submodelElement is AdminShell.SubmodelElement sme)
                    {
                        var type = EvalFeatureType(sme);
                        var lowerBound = "1";
                        var upperBound = "1";
                        var initialValue = "43";
                        if (sme is AdminShell.Property || sme is AdminShell.Range)
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

                        CreateAppendElement(attribute, UMLNS, "ModelElement.taggedValue",
                            new[] {
                                "type", type,
                                "lowerBound", lowerBound,
                                "upperBound", upperBound
                            });
                    }
            }

            public void AddClass(AdminShell.Submodel sm)
            {
                // access 
                if (sm == null)
                    return;

                // add
                var featureContainer = CreateAppendElement(Package, UMLNS, "Class",
                    new[] {
                        "name", "" + sm.idShort,
                        "xmi.id", GenerateId("MX_EAID_"),
                        "visibility", "public",
                        "isRoot", "false",
                        "isLeaf", "false",
                        "isAbstract", "false",
                        "isActive", "false",
                    },
                    CreateAppendElement(null, UMLNS, "Classifier.feature"));

                AddFeatures(featureContainer, sm.submodelElements);
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
                xmi.AddClass(submodel);
                xmi.SaveDoc(fn);
            }
        }
    }
}
