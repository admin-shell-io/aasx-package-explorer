/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// Attempt for XMI v1.1 (UML v1.3) approach. 
    /// Drawbacks: no type information, no aggregations
    /// </summary>
    public class Xmi11Writer : XmlWriter, IBaseWriter
    {
        public readonly Tuple<string, string> UMLNS =
            new Tuple<string, string>("UML", "http://omg.org/UML1.3");

        public XmlElement Package;

        public void StartDoc(ExportUmlRecord options)
        {
            if (options != null)
                _options = options;

            Doc = new XmlDocument();

            XmlSchema schema = new XmlSchema();
            schema.Namespaces.Add(UMLNS.Item1, UMLNS.Item2);
            Doc.Schemas.Add(schema);

            var decl = Doc.CreateXmlDeclaration("1.0", "windows-1252", null);
            Doc.AppendChild(decl);

            var root = Doc.CreateElement("XMI");
            Doc.AppendChild(root);
            root.SetAttribute("xmi.version", "1.1");
            root.SetAttribute("xmlns:" + UMLNS.Item1, UMLNS.Item2);
            root.SetAttribute("timestamp", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"));

            var xmicontent = CreateAppendElement(root, "XMI.content");
            root.AppendChild(xmicontent);

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

        public string EvalFeatureType(Aas.ISubmodelElement sme)
        {
            // access
            if (sme == null)
                return "";

            if (sme is Aas.Property p)
                return Aas.Stringification.ToString(p.ValueType);

            return sme.GetSelfDescription()?.AasElementName;
        }

        public void AddFeatures(
            XmlElement featureContainer,
            List<Aas.ISubmodelElement> features)
        {
            if (featureContainer == null || features == null)
                return;

            foreach (var sme in features)
            {
                var type = EvalFeatureType(sme);
                var multiplicity = EvalMultiplicityBounds(EvalUmlMultiplicity(sme));
                var initialValue = "";
                if (sme is Aas.Property || sme is Aas.Range
                    || sme is Aas.MultiLanguageProperty)
                    initialValue = sme.ValueAsText();

                var attribute = CreateAppendElement(featureContainer, UMLNS, "Attribute",
                    new[] {
                            "name", "" + sme.IdShort,
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

        public void AddClass(Aas.IReferable rf)
        {
            // the Referable shall enumerate children (if not, then its not a class)
            var features = rf.EnumerateChildren().ToList();

            // add
            var featureContainer = CreateAppendElement(Package, UMLNS, "Class",
                new[] {
                        "name", "" + rf.IdShort,
                        "visibility", "public",
                        "isRoot", "false",
                        "isLeaf", "false",
                        "isAbstract", "false",
                        "isActive", "false",
                },
                CreateAppendElement(null, UMLNS, "Classifier.feature"));

            AddFeatures(featureContainer, features);
        }

        public void ProcessEntity(Aas.IReferable parent, Aas.IReferable rf)
        {
            // access
            if (rf == null)
                return;

            // act flexible                
            AddClass(rf);

            // recurse
            foreach (var c in rf.EnumerateChildren())
                ProcessEntity(rf, c);
        }

        public void ProcessSubmodel(Aas.Submodel submodel)
        {
            ProcessEntity(null, submodel);
        }

        public void ProcessPost()
        {
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
}
