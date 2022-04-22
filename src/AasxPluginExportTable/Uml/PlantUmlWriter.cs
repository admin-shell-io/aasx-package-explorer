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
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// Creates a text representation for: https://plantuml.com/de/class-diagram
    /// </summary>
    public class PlantUmlWriter : BaseWriter, IBaseWriter
    {
        protected StringBuilder _builder = new StringBuilder();
        protected StringBuilder _post = new StringBuilder();

        public class UmlHandle
        {
            public string Id = "";
            public bool Valid => Id.HasContent();
        }

        public void StartDoc(ExportUmlOptions options)
        {
            if (options != null)
                _options = options;

            Writeln("@startuml");

            Writeln("!theme plain");
            Writeln("left to right direction");
            Writeln("hide class circle");
            Writeln("hide class methods");
            Writeln("skinparam classAttributeIconSize 0");
            Writeln("' skinparam linetype polyline");
            Writeln("skinparam linetype ortho");

            Writeln("");
        }

        protected int _noNameIndex = 1;

        public string ClearName(string name)
        {
            name = "" + name.Replace("\"", "");
            if (!name.HasContent())
                name = "NN" + _noNameIndex++.ToString("D3");
            return name;
        }

        public string FormatAs(string name, string id)
        {
            name = ClearName(name);
            return $"\"{name}\" as {id}";
        }

        public void Writeln(string line, bool post = false)
        {
            if (post)
                _post.AppendLine(line);
            else
                _builder.AppendLine(line);
        }

        public UmlHandle AddClass(AdminShell.Referable rf)
        {
            // the Referable shall enumerate children (if not, then its not a class)
            if (!(rf is AdminShell.IEnumerateChildren rfec))
                return null;
            var features = rfec.EnumerateChildren().ToList();

            // add
            var classId = RegisterObject(rf);
            var stereotype = EvalFeatureType(rf);
            if (stereotype.HasContent())
                stereotype = "<<" + stereotype + ">>";
            Writeln($"class {FormatAs(rf.idShort, classId)} {stereotype} {{");

            foreach (var smw in features)
                if (smw?.submodelElement is AdminShell.SubmodelElement sme)
                {
                    var type = EvalFeatureType(sme);
                    var multiplicity = EvalUmlMultiplicity(sme, noOne: true);
                    var initialValue = EvalInitialValue(sme, _options.LimitInitialValue);

                    var ln = $"  +{sme.idShort}";
                    if (type.HasContent())
                        ln += $" : {type}";
                    if (multiplicity.HasContent())
                        ln += $" [{multiplicity}]";
                    if (initialValue.HasContent())
                        ln += $" = \"{initialValue}\"";
                    Writeln(ln);
                }

            Writeln($"}}");
            Writeln("");

            return new UmlHandle() { Id = classId };
        }

        public UmlHandle ProcessEntity(
            AdminShell.Referable parent, AdminShell.Referable rf)
        {
            // access
            if (rf == null)
                return null;

            // act flexible                
            var dstTuple = AddClass(rf);

            // recurse
            if (rf is AdminShell.IEnumerateChildren rfec)
            {
                var childs = rfec.EnumerateChildren();
                if (childs != null)
                    foreach (var c in childs)
                        if (c?.submodelElement is AdminShell.SubmodelElement sme)
                        {
                            // create further entities
                            var srcTuple = ProcessEntity(rf, sme);

                            // make associations (often, srcTuple will be null, because not a class!)
                            if (srcTuple?.Valid == true && dstTuple?.Valid == true)
                            {
                                var multiplicity = EvalUmlMultiplicity(sme, noOne: true);
                                if (multiplicity.HasContent())
                                    multiplicity = "\"" + multiplicity + "\"";
                                Writeln(post: true,
                                    line: $"{dstTuple.Id} *-- {multiplicity} {srcTuple.Id} " +
                                          $": \"{ClearName(sme.idShort)}\"");
                            }
                        }
            }

            return dstTuple;
        }

        public void ProcessSubmodel(AdminShell.Submodel submodel)
        {
            Writeln("mainframe SMT " + submodel.idShort);
            Writeln("");

            ProcessEntity(null, submodel);
        }

        public void ProcessPost()
        {
            _builder.Append(_post);
        }

        public void SaveDoc(string fn)
        {
            _builder.AppendLine("@enduml");
            var text = _builder.ToString();
            File.WriteAllText(fn, text);
        }
    }
}
