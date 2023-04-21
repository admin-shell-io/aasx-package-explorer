/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AasxPackageLogic
{
    public static class EclassUtils
    {
        // Determine ECLASS files types
        //
        //

        public enum DataFileType { Invalid, Unknown, Units, Dictionary, Other };

        public static DataFileType TryGetDataFileType(string fn)
        {
            var res = DataFileType.Other;
            try
            {
                using (FileStream fileSteam = System.IO.File.OpenRead(fn))
                {
                    using (XmlReader reader = XmlReader.Create(fileSteam))
                    {
                        var iterations = 0;
                        while (reader.Read())
                        {
                            if (reader.IsStartElement())
                            {
                                if (reader.Name == "unt:eclass_units")
                                    res = DataFileType.Units;
                                if (reader.Name == "dic:eclass_dictionary")
                                    res = DataFileType.Dictionary;
                                break;
                            }
                            if (iterations++ > 100)
                                break;
                        }
                    }
                }
            }
            catch { return DataFileType.Invalid; };
            return res;
        }

        // Searching for text in ECLASS files
        //
        //

        public class SearchItem
        {
            public string Entity { get; set; }
            public string IRDI { get; set; }
            public string Info { get; set; }
            public XmlNode ContentNode { get; set; }

            public SearchItem()
            {
                Entity = "";
                IRDI = "";
                Info = "";
                ContentNode = null;
            }

            public SearchItem(string entity, string irdi, string info, XmlNode content)
            {
                Entity = entity;
                IRDI = irdi;
                Info = info;
                ContentNode = content;
            }
        }

        public class FileItem
        {
            public string fn = "";
            public EclassUtils.DataFileType dft = EclassUtils.DataFileType.Unknown;

            public FileItem()
            {
            }

            public FileItem(string fn, EclassUtils.DataFileType dft)
            {
                this.fn = fn;
                this.dft = dft;
            }
        }

        public class SearchJobData
        {
            public int maxItems = 1000;
            public List<FileItem> eclassFiles = new List<FileItem>();
            public string searchText = "";
            public List<string> searchIRDIs = new List<string>();
            public bool searchInClasses = false;
            public bool searchInDatatypes = false;
            public bool searchInProperties = true;
            public bool searchInUnits = false;
            public int numClass = 0;
            public int numDatatype = 0;
            public int numProperty = 0;
            public bool tooMany = false;
            public List<SearchItem> items = new List<SearchItem>();

            public SearchJobData()
            {
            }

            public SearchJobData(string eclassFullPath)
            {
                if (eclassFullPath == null || eclassFullPath == "")
                    return;
                foreach (var fn in System.IO.Directory.GetFiles(eclassFullPath, "*.xml"))
                {
                    var dft = EclassUtils.TryGetDataFileType(fn);
                    eclassFiles.Add(new EclassUtils.FileItem(fn, dft));
                }
            }
        }

        private static SearchItem CreateSearchItemFromPropertyNode(XmlNode node, string proposedType)
        {
            SearchItem res = null;
            var irdi = EclassUtils.GetAttributeByName(node, "id");
            if (irdi == null)
                irdi = EclassUtils.GetAttributeByName(node, "xml:id");
            if (irdi != null)
            {
                var info = "";
                if (node.Name == "ontoml:property" && node.HasChildNodes)
                {
                    var n2 = node.SelectSingleNode("preferred_name");
                    if (n2 != null && n2.HasChildNodes)
                        foreach (var c2 in n2.ChildNodes)
                            if (c2 is XmlNode && (c2 as XmlNode).Name == "label")
                                info = (c2 as XmlNode).InnerText;
                    proposedType = "prop";
                }
                res = new SearchItem(proposedType, irdi, info, node);
            }
            return res;
        }

        public static void SearchForTextInEclassFiles(SearchJobData a, Action<double> updateProgress)
        {
            if (a == null)
                return;
            if (a.eclassFiles == null || a.eclassFiles.Count < 1)
                return;

            double progressPerFile = 1.0 / a.eclassFiles.Count;

            var lastTimeOfUpdate = DateTime.Now;

            for (int fileNdx = 0; fileNdx < a.eclassFiles.Count; fileNdx++)
            {
                long totalSize = 1 + new System.IO.FileInfo(a.eclassFiles[fileNdx].fn).Length;

                try
                {

                    using (FileStream fileSteam = System.IO.File.OpenRead(a.eclassFiles[fileNdx].fn))
                    {
                        var settings = new XmlReaderSettings();
                        settings.ConformanceLevel = ConformanceLevel.Document;

                        int numElems = 0;
                        using (XmlReader reader = XmlReader.Create(fileSteam, settings))
                        {
                            while (reader.Read())
                            {
                                if (reader.IsStartElement())
                                {
                                    string searchForType = null;
                                    if (reader.Name == "ontoml:class" && a.searchInClasses)
                                    {
                                        searchForType = "cls";
                                        a.numClass++;
                                    }
                                    if (reader.Name == "ontoml:datatype" && a.searchInDatatypes)
                                    {
                                        searchForType = "dt";
                                        a.numDatatype++;
                                    }

                                    if (reader.Name == "ontoml:property" && a.searchInProperties)
                                    {
                                        searchForType = "prop";
                                        a.numProperty++;
                                    }
                                    if (reader.Name == "unitsml:Unit" && a.searchInUnits)
                                    {
                                        searchForType = "unit";
                                        a.numProperty++;
                                    }

                                    if (searchForType != null)
                                    {
                                        // always get the XmlDocument (can either read outer xml or the same as a node)
                                        var doc = new XmlDocument();
                                        var node = doc.ReadNode(reader);
                                        // contains the text
                                        if (node.OuterXml.Trim().ToLower().Contains(a.searchText))
                                        {
                                            var sItem = CreateSearchItemFromPropertyNode(node, searchForType);
                                            if (sItem != null)
                                            {
                                                a.items.Add(sItem);

                                                // not more than max
                                                if (a.items.Count > a.maxItems)
                                                {
                                                    a.tooMany = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    numElems++;
                                    if (numElems % 500 == 0
                                        && (DateTime.Now - lastTimeOfUpdate).TotalMilliseconds > 1000)
                                    {
                                        lastTimeOfUpdate = DateTime.Now;

                                        long currPos = fileSteam.Position;
                                        double frac = Math.Min(
                                            100.0d * progressPerFile * (fileNdx) +
                                                (100.0d * currPos) * progressPerFile / totalSize,
                                            100.0);
                                        if (updateProgress != null)
                                            updateProgress(frac);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
        }

        // Search IRDI in files
        //
        //

        public static void SearchForIRDIinEclassFiles(SearchJobData a, Action<double> updateProgress)
        {
            if (a == null)
                return;
            if (a.eclassFiles == null || a.eclassFiles.Count < 1)
                return;
            if (a.searchIRDIs == null || a.searchIRDIs.Count < 1)
                return;

            double progressPerFile = 1.0 / a.eclassFiles.Count;

            // 1st pass: search all content files for IRDI
            //

            var unitIrdisToSearch = new List<string>();

            for (int fileNdx = 0; fileNdx < a.eclassFiles.Count; fileNdx++)
            {
                if (a.eclassFiles[fileNdx].dft != DataFileType.Dictionary &&
                        a.eclassFiles[fileNdx].dft != DataFileType.Other)
                    continue;

                long totalSize = 1 + new System.IO.FileInfo(a.eclassFiles[fileNdx].fn).Length;

                try
                {

                    using (FileStream fileSteam = System.IO.File.OpenRead(a.eclassFiles[fileNdx].fn))
                    {
                        var settings = new XmlReaderSettings();
                        settings.ConformanceLevel = ConformanceLevel.Document;

                        int numElems = 0;
                        using (XmlReader reader = XmlReader.Create(fileSteam, settings))
                        {
                            while (reader.Read())
                            {
                                if (reader.IsStartElement() && reader.Name == "ontoml:property")
                                {
                                    // always get the XmlDocument (can either read outer xml or the same as a node)
                                    var doc = new XmlDocument();
                                    var node = doc.ReadNode(reader);

                                    // very specific: do we have a property with a valid id?
                                    if (node.Name == "ontoml:property")
                                    {
                                        var id = GetAttributeByName(node, "id");
                                        if (id != null && a.searchIRDIs.Contains(id.Trim().ToLower()))
                                        {
                                            var sItem = CreateSearchItemFromPropertyNode(node, "prop");
                                            if (sItem != null)
                                            {
                                                a.items.Add(sItem);

                                                // not more than max
                                                if (a.items.Count > a.maxItems)
                                                {
                                                    a.tooMany = true;
                                                    break;
                                                }

                                                // check as well, if a unit is being referenced
                                                var ndu = node.SelectSingleNode("domain/unit");
                                                if (ndu != null)
                                                {
                                                    var urefIrdi = GetAttributeByName(ndu, "unit_ref");
                                                    if (urefIrdi != null)
                                                        unitIrdisToSearch.Add(urefIrdi);
                                                }
                                            }
                                        }
                                    }

                                    numElems++;
                                    if (numElems % 500 == 0)
                                    {
                                        long currPos = fileSteam.Position;
                                        double frac = Math.Min(
                                            100.0d * progressPerFile * (fileNdx) +
                                                (100.0d * currPos) * progressPerFile / totalSize,
                                            100.0);
                                        if (updateProgress != null)
                                            updateProgress(frac);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            // 2st pass: search all unit files only for unit-IRDIs
            //

            for (int fileNdx = 0; fileNdx < a.eclassFiles.Count; fileNdx++)
            {
                if (a.eclassFiles[fileNdx].dft != DataFileType.Units ||
                        unitIrdisToSearch == null || unitIrdisToSearch.Count < 1)
                    continue;

                long totalSize = 1 + new System.IO.FileInfo(a.eclassFiles[fileNdx].fn).Length;

                try
                {

                    using (FileStream fileSteam = System.IO.File.OpenRead(a.eclassFiles[fileNdx].fn))
                    {
                        var settings = new XmlReaderSettings();
                        settings.ConformanceLevel = ConformanceLevel.Document;

                        int numElems = 0;
                        using (XmlReader reader = XmlReader.Create(fileSteam, settings))
                        {
                            while (reader.Read())
                            {
                                if (reader.IsStartElement() && reader.Name == "unitsml:Unit")
                                {
                                    // always get the XmlDocument (can either read outer xml or the same as a node)
                                    var doc = new XmlDocument();
                                    var node = doc.ReadNode(reader);

                                    // prepare the outer XML
                                    var oxml = node.OuterXml.Trim().ToLower();
                                    foreach (var uits in unitIrdisToSearch)
                                        if (uits != null && uits != "" && oxml.Contains(uits.ToLower().Trim()))
                                        {
                                            foreach (var x in GetChildNodesByName(node, "unitsml:CodeListValue"))
                                            {
                                                var cln = GetAttributeByName(x, "codeListName");
                                                var ucv = GetAttributeByName(x, "unitCodeValue");
                                                if (cln == "IRDI" && ucv.ToLower().Trim() == uits.ToLower().Trim())
                                                {
                                                    // read to be added as unit
                                                    var sItem = new SearchItem("unit", uits, "", node);
                                                    a.items.Add(sItem);
                                                }
                                            }
                                        }

                                    numElems++;
                                    if (numElems % 500 == 0)
                                    {
                                        long currPos = fileSteam.Position;
                                        double frac = Math.Min(
                                            100.0d * progressPerFile * (fileNdx) +
                                                (100.0d * currPos) * progressPerFile / totalSize,
                                            100.0);
                                        if (updateProgress != null)
                                            updateProgress(frac);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

        }


        // Generate concept description
        //
        //


        public static string GetAttributeByName(XmlNode node, string aname)
        {
            if (node?.Attributes == null || aname == null)
                return null;
            string res = null;
            foreach (XmlAttribute a in node.Attributes)
                if (a.Name == aname)
                    res = a.Value;
            return res;
        }

        public static List<XmlNode> GetChildNodesByName(XmlNode node, string aname)
        {
            List<XmlNode> res = new List<XmlNode>();
            if (node?.ChildNodes == null || aname == null)
                return res;
            if (node.HasChildNodes)
                foreach (var x in node.ChildNodes)
                    if (x is XmlNode && (x as XmlNode).Name == aname)
                        res.Add(x as XmlNode);
            return res;
        }

        private static string GetChildInnerText(XmlNode node, string childElemName, string otherwise)
        {
            if (node == null)
                return otherwise;

            var n = node.SelectSingleNode(childElemName);
            if (n != null)
                return n.InnerText;

            return otherwise;
        }

        private static void FindChildLangStrings(
            XmlNode node, string childName, string childChildName, string langCodeAttrib,
            Action<Aas.LangString> action)
        {
            if (node == null || action == null)
                return;
            var n1 = node.SelectSingleNode(childName);
            if (n1 != null)
            {
                var nl = n1.SelectNodes(childChildName);
                if (nl != null)
                    foreach (XmlNode ni in nl)
                        if (ni.Attributes != null && ni.Attributes[langCodeAttrib] != null)
                        {
                            var ls = new Aas.LangString(ni.Attributes["language_code"].InnerText, ni.InnerText);
                            action(ls);
                        }
            }
        }

        public static Aas.ConceptDescription GenerateConceptDescription(
            List<EclassUtils.SearchItem> input, string targetIrdi)
        {
            // access
            if (input == null || input.Count < 1)
                return null;

            // new cd
            var res = new Aas.ConceptDescription("");

            // MIHO 2020-10-02: fix bug, create IEC61360 content
            var eds = ExtendEmbeddedDataSpecification.CreateIec61360WithContent();
            res.EmbeddedDataSpecifications = new List<Aas.EmbeddedDataSpecification>();
            res.EmbeddedDataSpecifications.Add(eds);
            var ds = eds.DataSpecificationContent as Aas.DataSpecificationIec61360;

            // over all, first is significant
            for (int i = 0; i < input.Count; i++)
            {
                // only accept property nodes, which are targetIrdi
                var si = input[i];
                if (si.Entity != "prop" || si.IRDI.ToLower().Trim() != targetIrdi.ToLower().Trim())
                    continue;
                var node = si.ContentNode;
                if (node == null)
                    continue;

                // first is significant
                if (i == 0)
                {
                    // identification
                    res.Id = input[i].IRDI;

                    // isCase of
                    if (res.IsCaseOf.IsNullOrEmpty())
                    {
                        res.IsCaseOf = new List<Aas.Reference>();
                    }

                    res.IsCaseOf.Add(new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>() { new Aas.Key(Aas.KeyTypes.GlobalReference, input[i].IRDI) }));

                    // administration
                    res.Administration = new Aas.AdministrativeInformation();
                    var n1 = node.SelectSingleNode("revision");
                    if (n1 != null)
                        res.Administration.Revision = "" + n1.InnerText;

                    // short name -> TBD in future
                    FindChildLangStrings(node, "short_name", "label", "language_code", (ls) =>
                    {
                        ds.ShortName = new List<Aas.LangString>
                        {
                            new Aas.LangString("EN?", ls.Text)
                        };
                        res.IdShort = ls.Text;
                    });

                    // guess data type
                    var nd = node.SelectSingleNode("domain");
                    if (nd != null)
                    {
                        var ndt = GetAttributeByName(nd, "xsi:type");
                        if (ndt != null)
                        {
                            // try find a match
                            ds.DataType = Aas.Stringification.DataTypeIec61360FromString(ndt);
                        }
                    }

                    // unit
                    var ndu = node.SelectSingleNode("domain/unit");
                    if (ndu != null)
                    {
                        var urefIrdi = GetAttributeByName(ndu, "unit_ref");
                        if (urefIrdi != null)
                        {
                            foreach (var xi in input)
                                if (xi.IRDI.ToLower().Trim() == urefIrdi.ToLower().Trim() && xi.ContentNode != null)
                                {
                                    foreach (var xiun in GetChildNodesByName(xi.ContentNode, "unitsml:UnitName"))
                                        if (xiun != null)
                                        {
                                            ds.UnitId = new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>() { new Aas.Key(Aas.KeyTypes.GlobalReference, urefIrdi.Trim()) });
                                            ds.Unit = xiun.InnerText.Trim();
                                        }
                                }
                        }
                    }
                }

                // all have language texts
                FindChildLangStrings(node, "preferred_name", "label", "language_code", (ls) =>
                {
                    if (ds.PreferredName == null)
                        ds.PreferredName = new List<Aas.LangString>();

                    // ReSharper disable PossibleNullReferenceException -- ignore a false positive
                    ds.PreferredName.Add(ls);
                });

                FindChildLangStrings(node, "definition", "text", "language_code", (ls) =>
                {
                    if (ds.Definition == null)
                        ds.PreferredName = new List<Aas.LangString>();

                    // ReSharper disable PossibleNullReferenceException -- ignore a false positive
                    ds.Definition.Add(ls);
                });

            }

            // Phase 2: fix some shortcomings
            //

            try
            {
                if (ds.ShortName == null || ds.ShortName.Count < 1) // TBD: multi-language short name?!
                {
                    if (ds.PreferredName != null && !(ds.PreferredName.Count < 1))
                    {
                        var found = false;
                        foreach (var pn in ds.PreferredName)
                        {
                            // let have "en" always have precedence!
                            if (found && !pn.Language.ToLower().Trim().Contains("en"))
                                continue;
                            // ok
                            found = true;
                            // Array of words
                            var words = pn.Text.Split(
                                new[] { ' ', '\t', '-', '_' },
                                StringSplitOptions.RemoveEmptyEntries);
                            var sn = "";
                            foreach (var w in words)
                            {
                                var part = w.ToLower().Trim();
                                if (part.Length > 3)
                                    part = part.Substring(0, 3);
                                if (part.Length > 0)
                                    part = Char.ToUpperInvariant(part[0]) + part.Substring(1);
                                sn += part;
                            }
                            // set it
                            ds.ShortName = new List<Aas.LangString>
                            {
                                new Aas.LangString("EN?", sn)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            // ok
            return res;
        }

        // Generate further information
        //
        //

        public static string GetIrdiForUnitSearchItem(SearchItem si)
        {
            if (si == null || si.ContentNode == null || si.Entity != "unit")
                return null;

            string res = null;

            foreach (var x in GetChildNodesByName(si.ContentNode, "unitsml:CodeListValue"))
            {
                var cln = GetAttributeByName(x, "codeListName");
                var ucv = GetAttributeByName(x, "unitCodeValue");
                if (cln == "IRDI")
                {
                    res = ucv;
                    break;
                }
            }

            return res;
        }

    }
}
