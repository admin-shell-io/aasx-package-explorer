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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

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

        public void StartDoc(ExportUmlRecord options)
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

        public string ViusalIdShort(Aas.IReferable parent, int index, Aas.IReferable rf)
        {
            if (parent == null)
                return rf?.IdShort;
            if (parent is Aas.ISubmodelElementList)
            {
                return $"[{index:00}]";
            }
            else
                return rf?.IdShort;
        }

        public UmlHandle AddClass(Aas.IReferable rf, string visIdShort)
        {
            // the Referable shall enumerate children (if not, then its not a class)
            var features = rf.EnumerateChildren().ToList();
            if (features.Count < 1)
                return null;

            // add
            var classId = RegisterObject(rf);
            var stereotype = EvalFeatureType(rf);
            if (stereotype.HasContent())
                stereotype = "<<" + stereotype + ">>";
            Writeln($"class {FormatAs(visIdShort, classId)} {stereotype} {{");

            int idx = 0;
            foreach (var sme in features)
            {
                var type = EvalFeatureType(sme);
                var multiplicity = EvalUmlMultiplicity(sme, noOne: true);
                var initialValue = EvalInitialValue(sme, _options.LimitInitialValue);

                var ln = $"  +{ViusalIdShort(rf, idx++, sme)}";
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
            Aas.IReferable parent, Aas.IReferable rf, string visIdShort)
        {
            // access
            if (rf == null)
                return null;

            // act flexible                
            var dstTuple = AddClass(rf, visIdShort);

            // recurse
            var idx = 0;
            var childs = rf.EnumerateChildren();
            if (childs != null)
                foreach (var sme in childs)
                {
                    // idShort
                    var smeIdShort = ViusalIdShort(rf, idx++, sme);

                    // create further entities
                    var srcTuple = ProcessEntity(rf, sme, smeIdShort);

                    // make associations (often, srcTuple will be null, because not a class!)
                    if (srcTuple?.Valid == true && dstTuple?.Valid == true)
                    {
                        var multiplicity = EvalUmlMultiplicity(sme, noOne: true);
                        if (multiplicity.HasContent())
                            multiplicity = "\"" + multiplicity + "\"";
                        Writeln(post: true,
                            line: $"{dstTuple.Id} *-- {multiplicity} {srcTuple.Id} " +
                                    $": \"{ClearName(smeIdShort)}\"");
                    }
                }

            return dstTuple;
        }

        public void ProcessSubmodel(Aas.ISubmodel submodel)
        {
            // access
            if (submodel == null)
                return;

            // frame
            Writeln("mainframe " 
                + AdminShellUtil.MapIntToStringArray((int)submodel.Kind, "SM", new[] { "SMT", "SM" })
                + submodel.IdShort);
            Writeln("");

            // entities
            ProcessEntity(null, submodel, submodel.IdShort);
        }

        public void ProcessPost()
        {
            _builder.Append(_post);
        }

        public void SaveDoc(string fn)
        {
            _builder.AppendLine("@enduml");
            var text = _builder.ToString();
            System.IO.File.WriteAllText(fn, text);
        }
    }
}
