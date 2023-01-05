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
using Aas = AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// Attempt for XMI v2.1 (UML v2.0) approach. 
    /// Drawbacks: no type information, no aggregations
    /// </summary>
    public class Xmi21Writer : XmlWriter, IBaseWriter
    {
        public readonly Tuple<string, string> XMINS =
            new Tuple<string, string>("xmi", "http://schema.omg.org/spec/XMI/2.1");

        public readonly Tuple<string, string> UMLNS =
            new Tuple<string, string>("uml", "http://schema.omg.org/spec/UML/2.1");

        public XmlElement Package;

        public class XmiHandle
        {
            public string Id;
            public XmlElement Elem;
            public bool Valid => Id.HasContent() && Elem != null;
        }

        public class XmiTupleAssociationJob
        {
            public string AssocType = "";
            public string Name = "";
            public string Multiplicity = "1";
            public XmiHandle SrcTuple, DstTuple;
            public bool Valid =>
                AssocType.HasContent() && Name.HasContent()
                && SrcTuple?.Valid == true && DstTuple?.Valid == true;
        }

        protected List<XmiTupleAssociationJob> _associationJobs = new List<XmiTupleAssociationJob>();

        public void StartDoc(ExportUmlRecord options)
        {
            if (options != null)
                _options = options;

            Doc = new XmlDocument();

            XmlSchema schema = new XmlSchema();
            schema.Namespaces.Add(UMLNS.Item1, UMLNS.Item2);
            Doc.Schemas.Add(schema);

            AddNamespace(UMLNS.Item1, UMLNS.Item2);
            AddNamespace(XMINS.Item1, XMINS.Item2);

            var decl = Doc.CreateXmlDeclaration("1.0", "windows-1252", null);
            Doc.AppendChild(decl);

            var root = Doc.CreateElement(XMINS.Item1, "XMI", XMINS.Item2);
            Doc.AppendChild(root);
            root.SetAttribute("version", XMINS.Item2, "2.1");
            root.SetAttribute("xmlns:" + UMLNS.Item1, UMLNS.Item2);
            root.SetAttribute("xmlns:" + XMINS.Item1, XMINS.Item2);

            // Note: if the info below is altered, then EA will NOT import type tags of the attributes!
            CreateAppendElement(root, XMINS, "Documentation",
                new[] {
                    "exporter", "Enterprise Architect",
                    "exporterVersion", "6.5"
                });

            var model = CreateAppendElement(root, UMLNS, "Model",
                new[] {
                        "xmi:type", "uml:Model",
                        "name", "AAS_Model",
                        "visibility", "public"
                });

            var pack1 = CreateAppendElement(model, "packagedElement",
                new[] {
                        "xmi:type", "uml:Package",
                        "xmi:id", RegisterObject("AAS Model"),
                        "name", "AAS Model",
                        "visibility", "public"
                });

            Package = CreateAppendElement(pack1, "packagedElement",
                new[] {
                        "xmi:type", "uml:Package",
                        "xmi:id", RegisterObject("Package1"),
                        "name", "Package1",
                        "visibility", "public"
                });
        }

        public void AddMultiplicity(
            XmlElement attribute,
            string attrId,
            string multiplicity)
        {
            // access
            if (attribute == null || !attrId.HasContent() || !multiplicity.HasContent())
                return;

            // some special cases
            var multi = EvalMultiplicityBounds(multiplicity);
            var m1 = multi.Item1;
            var m2 = multi.Item2;

            if (m2 == "*")
            {
                m2 = "-1";
            }

            // add
            CreateAppendElement(attribute, "lowerValue",
                        new[] {
                            "xmi:type", "uml:LiteralInteger",
                            "xmi:id", attrId,
                            "value", m1
                        });

            CreateAppendElement(attribute, "upperValue",
                new[] {
                            "xmi:type", "uml:LiteralInteger",
                            "xmi:id", attrId,
                            "value", m2
                });
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
                var multiplicity = EvalUmlMultiplicity(sme);
                var initialValue = EvalInitialValue(sme, _options.LimitInitialValue);

                var attrId = RegisterObject(sme);

                var attribute = CreateAppendElement(featureContainer, "ownedAttribute",
                    new[] {
                        "xmi:type", "uml:Aas.Property",
                        "xmi:id", attrId,
                        "name", "" + sme.IdShort,
                        "visibility", "public",
                        "isStatic", "false",
                        "isReadOnly", "false",
                        "isDerived", "false",
                        "isOrdered", "false",
                        "isUnique", "true",
                        "isDerivedUnion", "false"
                    });

                AddMultiplicity(attribute, attrId, multiplicity);

                CreateAppendElement(attribute, "defaultValue",
                    new[] {
                        "xmi:type", "uml:LiteralString",
                        "xmi:id", attrId,
                        "value", initialValue
                    });

                CreateAppendElement(attribute, "type",
                    new[] {
                        "xmi:idref", "EAnone_" + type
                    });
            }
        }

        public XmiHandle AddClass(Aas.IReferable rf)
        {
            // the Referable shall enumerate children (if not, then its not a class)
            var features = rf.EnumerateChildren().ToList();

            // add
            var classId = RegisterObject(rf);
            var classContainer = CreateAppendElement(Package, "packagedElement",
                new[] {
                        "xmi:type", "uml:Class",
                        "xmi:id", classId,
                        "name", "" + rf.IdShort,
                        "visibility", "public"
                });

            AddFeatures(classContainer, features);

            return new XmiHandle() { Id = classId, Elem = classContainer };
        }

        public XmiHandle ProcessEntity(
            Aas.IReferable parent, Aas.IReferable rf)
        {
            // access
            if (rf == null)
                return null;

            // act flexible                
            var dstTuple = AddClass(rf);

            // recurse
            foreach (var sme in rf.EnumerateChildren())
            {
                // create further entities
                var srcTuple = ProcessEntity(rf, sme);

                // make associations (often, srcTuple will be null, because not a class!)
                var job = new XmiTupleAssociationJob()
                {
                    AssocType = "Aggr",
                    Name = "" + sme.IdShort,
                    Multiplicity = "" + EvalUmlMultiplicity(sme),
                    SrcTuple = srcTuple,
                    DstTuple = dstTuple
                };
                if (job.Valid)
                    _associationJobs.Add(job);
            }

            return dstTuple;
        }

        public void ProcessSubmodel(Aas.Submodel submodel)
        {
            ProcessEntity(null, submodel);
        }

        public void ProcessPost()
        {
            foreach (var job in _associationJobs)
            {
                if (job.Valid && job.AssocType == "Aggr")
                {
                    // create an association attribute in the child (src) class                   
                    var assocId = RegisterObject(job);
                    var assocAttrId = RegisterObject("Assoc");
                    var ownEndId = RegisterObject("End");

                    var attribute = CreateAppendElement(job.SrcTuple.Elem, "ownedAttribute",
                        new[] {
                            "xmi:type", "uml:Aas.Property",
                            "xmi:id", assocAttrId,
                            "visibility", "public",
                            "association", assocId,
                            "isStatic", "false",
                            "isReadOnly", "false",
                            "isDerived", "false",
                            "isOrdered", "false",
                            "isUnique", "true",
                            "isDerivedUnion", "false",
                            "aggregation", "none"
                        });

                    CreateAppendElement(attribute, "type",
                    new[] {
                            "xmi:idref", "" + job.DstTuple.Id
                    });

                    //// AddMultiplicity(attribute, assocAttrId, job.Multiplicity);

                    // create the association itself
                    var assoc = CreateAppendElement(Package, "packagedElement",
                        new[] {
                            "xmi:type", "uml:Association",
                            "xmi:id", assocId,
                            "name", job.Name,
                            "visibility", "public"
                        });

                    // position of memberEnd(s) before ownedEnd seem to influence
                    // the directionality of the association

                    CreateAppendElement(assoc, "memberEnd",
                        new[] {
                            "xmi:idref", "" + ownEndId
                        });

                    CreateAppendElement(assoc, "memberEnd",
                        new[] {
                            "xmi:idref", "" + assocAttrId
                        });

                    var ownedEnd1 = CreateAppendElement(assoc, "ownedEnd",
                        new[] {
                            "xmi:type", "uml:Aas.Property",
                            "xmi:id", ownEndId,
                            "visibility", "public",
                            "association", assocId,
                            "isStatic", "false",
                            "isReadOnly", "false",
                            "isDerived", "false",
                            "isOrdered", "false",
                            "isUnique", "true",
                            "isDerivedUnion", "false",
                            "aggregation", "composite"
                        });

                    CreateAppendElement(ownedEnd1, "type",
                        new[] {
                            "xmi:idref", "" + job.SrcTuple.Id
                        });

                    AddMultiplicity(ownedEnd1, ownEndId, job.Multiplicity);

                }
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
}
