﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Spread = DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Aas = AasCore.Aas3_0;

// ReSharper disable PossiblyMistakenUseOfParamsMethod .. issue, even if according to samples of Word API

// Disable dead code detection due to comments such as `//-9- {Referable}.kind`
// dead-csharp off

namespace AasxPluginExportTable.Table
{
    public class ExportTableAasEntitiesItem
    {
        public int depth;

        /// <summary>
        /// This element carries data from either SM or SME.
        /// </summary>
        public Aas.IReferable Parent;

        public Aas.IReferable rf;
        public Aas.IConceptDescription cd;

        public ExportTableAasEntitiesItem(
            int depth, Aas.IReferable rf = null,
            Aas.IConceptDescription cd = null,
            Aas.IReferable parent = null)
        {
            this.depth = depth;
            this.rf = rf;
            this.cd = cd;
            Parent = parent;
        }

        public Aas.IReferable GetHeadingReferable()
        {
            if (Parent != null)
                return Parent;
            if (rf != null)
                return rf;
            return null;
        }
    }

    public class ExportTableAasEntitiesList : List<ExportTableAasEntitiesItem>
    {

    }

    public class ExportTableProcessor
    {
        protected ImportExportTableRecord Record = null;

        //
        // Constructurs
        //

        public ExportTableProcessor(ImportExportTableRecord record)
        {
            Record = record;
        }

        //
        // Cells
        //

        public class CellRecord
        {
            public string Fg = null, Bg = null, TableBg = null, HorizAlign = null, VertAlign = null, Font = null, Frame = null,
                Text = "", TextWithHeaders = "", Md = "";

            public double? Width = null;

            public int ColSpan = 1;

            public CellRecord() { }

            public CellRecord(string text)
            {
                Text = text;
            }
        }

        public CellRecord GetTopCell(int row, int col)
        {
            if (Record == null)
                return null;
            var i = (1 + row) * (1 + Record.Cols) + 1 + col;
            if (row < 0 || col < 0 || Record.Top == null || i >= Record.Top.Count)
                return null;
            var cr = new CellRecord(Record.Top[i]);
            cr.TextWithHeaders =
                Record.Top[0] + " " + Record.Top[(1 + row) * (1 + Record.Cols)] + " " +
                Record.Top[1 + col] + " " + cr.Text;
            return cr;
        }

        public CellRecord GetBodyCell(int row, int col)
        {
            if (Record == null)
                return null;
            var i = (1 + row) * (1 + Record.Cols) + 1 + col;
            if (row < 0 || col < 0 || Record.Body == null || i >= Record.Body.Count)
                return null;
            var cr = new CellRecord(Record.Body[i]);
            cr.TextWithHeaders =
                Record.Body[0] + " " + Record.Body[(1 + row) * (1 + Record.Cols)] + " " +
                Record.Body[1 + col] + " " + cr.Text;
            return cr;
        }

        //
        // Processors
        //

        public class ItemProcessor
        {
            //
            // Member
            //

            ImportExportTableRecord Record = null;
            public ExportTableAasEntitiesItem Item = null;

            public ItemProcessor() { }

            public string ReplaceNewlineWith = null;

            public int NumberReplacements = 0;

            public ItemProcessor(ImportExportTableRecord record, ExportTableAasEntitiesItem item)
            {
                Record = record;
                Item = item;
            }

            //
            // Internal
            //

            private Regex regexReplacements = null;
            private Regex regexStop = null;
            private Regex regexCommands = null;
            private Dictionary<string, string> repDict = null;

            private void rep(string tag, string value)
            {
                tag = tag?.Trim().ToLower();
                if (tag == null || repDict == null || repDict.ContainsKey(tag))
                    return;
                repDict[tag] = value;
            }

            private void repLangStr(string head, Aas.ILangStringTextType ls)
            {
                if (ls == null)
                    return;
                rep(head + "@" + "" + ls.Language, "" + ls.Text);
            }

            private void repLangStr(string head, Aas.ILangStringDefinitionTypeIec61360 ls)
            {
                if (ls == null)
                    return;
                rep(head + "@" + "" + ls.Language, "" + ls.Text);
            }

            private void repLangStr(string head, Aas.ILangStringShortNameTypeIec61360 ls)
            {
                if (ls == null)
                    return;
                rep(head + "@" + "" + ls.Language, "" + ls.Text);
            }

            private void repLangStr(string head, Aas.ILangStringPreferredNameTypeIec61360 ls)
            {
                if (ls == null)
                    return;
                rep(head + "@" + "" + ls.Language, "" + ls.Text);
            }

            private void repListOfLangStr(string head, List<Aas.ILangStringTextType> lss)
            {
                if (lss == null)
                    return;

                // entity in total
                rep(head, "" + lss.ToStringExtended(format: 2));

                // single entities
                foreach (var ls in lss)
                    repLangStr(head, ls);
            }

            private void repListOfLangStr(string head, List<Aas.ILangStringDefinitionTypeIec61360> lss)
            {
                if (lss == null)
                    return;

                // entity in total
                rep(head, "" + lss.ToString());

                // single entities
                foreach (var ls in lss)
                    repLangStr(head, ls);
            }

            private void repListOfLangStr(string head, List<Aas.ILangStringShortNameTypeIec61360> lss)
            {
                if (lss == null)
                    return;

                // entity in total
                rep(head, "" + lss.ToString());

                // single entities
                foreach (var ls in lss)
                    repLangStr(head, ls);
            }

            private void repListOfLangStr(string head, List<Aas.ILangStringPreferredNameTypeIec61360> lss)
            {
                if (lss == null)
                    return;

                // entity in total
                rep(head, "" + lss.ToString());

                // single entities
                foreach (var ls in lss)
                    repLangStr(head, ls);
            }

            private void repReferable(string head, Aas.IReferable rf)
            {
                //-9- {Referable}.{idShort, category, description, description[@en..], elementName, 
                //     elementAbbreviation, parent}
                if (rf.IdShort != null)
                    rep(head + "idShort", rf.IdShort);
                if (rf.Category != null)
                    rep(head + "category", rf.Category);
                if (rf.Description != null)
                    repListOfLangStr(head + "description", rf.Description);
                rep(head + "elementName", "" + rf.GetSelfDescription()?.AasElementName);
                rep(head + "elementAbbreviation", "" + rf.GetSelfDescription()?.ElementAbbreviation);

                if (rf is Aas.ISubmodelElement rfsme)
                {
                    rep(head + "elementShort", "" +
                        rf.GetSelfDescription()?.ElementAbbreviation);
                }
                if (rf is Aas.IReferable rfpar)
                    rep(head + "parent", "" + (rfpar.IdShort != null ? rfpar.IdShort : "-"));

                // further details
                List<string> details = new List<string>();
                if (rf is Aas.ISubmodelElementList sml)
                {
                    details.Add("orderRelevant=" + AdminShellUtil.MapBoolToStringArray(
                        sml.OrderRelevant, "No", new[] { "No", "Yes" }));
                    if (sml.SemanticIdListElement?.IsValid() == true)
                        details.Add("semanticIdListElement=" + sml.SemanticIdListElement.ToStringExtended());
                    details.Add("typeValueListElement=" +
                        AasCore.Aas3_0.Stringification.ToString(sml.TypeValueListElement));
                    if (sml.ValueTypeListElement != null)
                        details.Add("valueTypeListElement=" +
                            AasCore.Aas3_0.Stringification.ToString(sml.ValueTypeListElement));
                }
                if (details.Count < 1)
                    details.Add("-");
                rep(head + "details", "" + string.Join(", ", details));
            }

            private void repModelingKind(string head, Aas.ModellingKind? k)
            {
                if (!k.HasValue)
                    return;

                //-9- {Referable}.kind
                rep(head + "kind", "" + k.ToString());
            }

            private void repQualifiable(string head, List<Aas.IQualifier> qualifiers)
            {
                if (qualifiers == null)
                    return;

                //-9- {Qualifiable}.qualifiers
                rep(head + "qualifiers", "" + qualifiers.ToStringExtended(1));
            }

            private void repMultiplicty(string head, List<Aas.IQualifier> qualifiers)
            {
                // access
                if (qualifiers == null)
                    return;

                // try find
                string multiStr = null;
                var q = qualifiers.FindType("Multiplicity");
                if (q != null)
                {
                    foreach (var m in (FormMultiplicity[])Enum.GetValues(typeof(FormMultiplicity)))
                        if ("" + q.Value == Enum.GetName(typeof(FormMultiplicity), m))
                        {
                            multiStr = "" + AasFormConstants.FormMultiplicityAsUmlCardinality[(int)m];
                        }
                }

                //-9- {Qualifiable}.multiplicity
                rep(head + "multiplicity", "" + multiStr);
            }

            private void repIdentifiable(string head, Aas.IIdentifiable ifi)
            {
                if (ifi == null)
                    return;
                if (ifi.Id != null)
                {
                    //-9- Identifiable.id, administration.{ version, revision}}
                    rep(head + "id", ifi.Id);
                }
                if (ifi.Administration != null)
                {
                    rep(head + "administration.version", "" + ifi.Administration.Version);
                    rep(head + "administration.revision", "" + ifi.Administration.Revision);
                }
            }

            private void repReference(string head, string refName, Aas.IReference rid)
            {
                if (rid == null || refName == null || rid.Keys == null)
                    return;

                // add together
                //-9- {Reference}
                rep(head + refName + "", "" + rid.ToStringExtended(1));

                // add the single parts of the sid
                for (int ki = 0; ki < rid.Keys.Count; ki++)
                {
                    var k = rid.Keys[ki];
                    if (k != null)
                    {
                        // in nice form
                        //-9- {Reference}[0..n]
                        rep(head + refName + $"[{ki}]", "" + k.ToStringExtended(1));
                        // but also in separate parts
                        //-9- {Reference}[0..n].{type, local, idType, value}
                        rep(head + refName + $"[{ki}].type", "" + k.Type);
                        rep(head + refName + $"[{ki}].value", "" + k.Value);
                    }
                }
            }

            public void Start()
            {
                // init Regex
                // nice tester: http://regexstorm.net/tester
                regexReplacements = new Regex(@"%([a-zA-Z0-9.@\[\]]+)%", RegexOptions.IgnoreCase);
                regexStop = new Regex(@"^(.*?)%stop%(.*)$", RegexOptions.IgnoreCase);
                regexCommands = new Regex(@"(%([A-Za-z0-9-_]+)=(.*?)%)", RegexOptions.IgnoreCase);

                // init dictionary
                repDict = new Dictionary<string, string>();

                // some general replacements
                rep("nl", "" + Environment.NewLine);
                rep("br", "\r");
                rep("tab", "\t");

                // for the provided AAS entites, set the replacements
                if (Item != null)
                {
                    // shortcuts
                    var par = Item.Parent;
                    var sm = Item.rf as Aas.ISubmodel;
                    var sme = Item.rf as Aas.ISubmodelElement;
                    var cd = Item.cd;

                    // general for the item
                    rep("depth", "" + Item.depth);
                    rep("indent", "" + new string('~', Math.Max(0, Item.depth)));

                    //-1- {Parent}
                    if (par is Aas.Submodel parsm)
                    {
                        var head = "Parent.";
                        repReferable(head, parsm);
                        repModelingKind(head, parsm.Kind);
                        repQualifiable(head, parsm.Qualifiers);
                        repMultiplicty(head, parsm.Qualifiers);
                        repIdentifiable(head, parsm);
                        //-1- {Reference} = {semanticId, isCaseOf, unitId}
                        repReference(head, "semanticId", parsm.SemanticId);
                    }

                    if (par is Aas.ISubmodelElement parsme)
                    {
                        var head = "Parent.";
                        repReferable(head, parsme);
                        repQualifiable(head, parsme.Qualifiers);
                        repMultiplicty(head, parsme.Qualifiers);
                        //-1- {Reference} = {semanticId, isCaseOf, unitId}
                        repReference(head, "semanticId", parsme.SemanticId);
                    }

                    //-1- {Referable} = {SM, SME, CD}
                    if (sm != null)
                    {
                        var head = "SM.";
                        repReferable(head, sm);
                        repModelingKind(head, sm.Kind);
                        repQualifiable(head, sm.Qualifiers);
                        repMultiplicty(head, sm.Qualifiers);
                        repIdentifiable(head, sm);
                        //-1- {Reference} = {semanticId, isCaseOf, unitId}
                        repReference(head, "semanticId", sm.SemanticId);
                    }

                    if (sme != null)
                    {
                        var head = "SME.";
                        repReferable(head, sme);
                        repQualifiable(head, sme.Qualifiers);
                        repMultiplicty(head, sme.Qualifiers);
                        repReference(head, "semanticId", sme.SemanticId);

                        //-2- SME.value

                        if (sme is Aas.Property p)
                        {
                            //-2- Property.{value, valueType, valueId}
                            rep("Property.value", "" + p.Value);
                            rep("Property.valueType", "" + p.ValueType);
                            if (p.ValueId != null)
                                rep("Property.valueId", "" + p.ValueId.ToStringExtended(1));

                            rep("SME.value", "" + p.Value);
                        }

                        if (sme is Aas.MultiLanguageProperty mlp)
                        {
                            //-2- MultiLanguageProperty.{value, vlaueId}
                            repListOfLangStr("MultiLanguageProperty.value", mlp.Value);
                            if (mlp.ValueId != null)
                                rep("MultiLanguageProperty.valueId", "" + mlp.ValueId.ToStringExtended(1));

                            repListOfLangStr("SME.value", mlp.Value);
                        }

                        if (sme is Aas.Range r)
                        {
                            //-2- Range.{valueType, min, max}
                            rep("Range.valueType", "" + r.ValueType);
                            rep("Range.min", "" + r.Min);
                            rep("Range.max", "" + r.Max);

                            rep("SME.value", "" + r.Min + " .. " + r.Max);
                        }

                        if (sme is Aas.Blob b)
                        {
                            //-2- Blob.{mimeType, value}
                            rep("Blob.mimeType", "" + b.ContentType);
                            rep("Blob.value", "" + b.Value);

                            rep("SME.value", "" + ("" + b.Value).Length + " bytes");
                        }

                        if (sme is Aas.File f)
                        {
                            //-2- File.{mimeType, value}
                            rep("File.mimeType", "" + f.ContentType);
                            rep("File.value", "" + f.Value);

                            rep("SME.value", "" + f.Value);
                        }

                        if (sme is Aas.ReferenceElement re)
                        {
                            //-2- ReferenceElement.value
                            rep("ReferenceElement.value", "" + re.Value?.ToStringExtended(1));

                            rep("SME.value", "" + re.Value?.ToStringExtended(1));
                        }

                        if (sme is Aas.RelationshipElement rele)
                        {
                            //-2- RelationshipElement.{first, second}
                            rep("RelationshipElement.first", "" + rele.First?.ToStringExtended(1));
                            rep("RelationshipElement.second", "" + rele.Second?.ToStringExtended(1));

                            rep("SME.value", "" + rele.First?.ToStringExtended(1)
                                + " -> " + rele.Second?.ToStringExtended(1));
                        }

                        if (sme is Aas.SubmodelElementCollection smc)
                        {
                            //-2- SubmodelElementCollection.{value = #elements}
                            rep(
                                "SubmodelElementCollection.value", "" +
                                (smc.Value != null
                                    ? smc.Value.Count
                                    : 0) +
                                " elements");

                            rep("SME.value", "" + (smc.Value != null ? smc.Value.Count : 0) + " elements");
                        }

                        if (sme is Aas.SubmodelElementList sml)
                        {
                            //-2- SubmodelElementList.{value = #elements, orderRelevant}
                            rep(
                                "SubmodelElementList.value", "" +
                                (sml.Value != null
                                    ? sml.Value.Count
                                    : 0) +
                                " elements");
                            rep("SubmodelElementList.orderRelevant", "" + sml.OrderRelevant);

                            rep("SME.value", "" + (sml.Value != null ? sml.Value.Count : 0) + " elements");
                        }

                        if (sme is Aas.Entity ent)
                        {
                            //-2- Entity.{entityType, asset}
                            rep("Entity.entityType", "" + Aas.Stringification.ToString(ent.EntityType));
                            if (ent.GlobalAssetId != null)
                                rep("Entity.globalAssetId", "" + ent.GlobalAssetId);
                        }
                    }

                    if (cd != null)
                    {
                        var head = "CD.";
                        repReferable(head, cd);
                        repIdentifiable(head, cd);
                        if (cd.IsCaseOf != null)
                            for (int icoi = 0; icoi < cd.IsCaseOf.Count; icoi++)
                                repReference(head, $"isCaseOf[{icoi}]", cd.IsCaseOf[icoi]);

                        var iec = cd.GetIEC61360();
                        if (iec != null)
                        {
                            //-2- CD.{preferredName[@en..], shortName[@en..], anyName, unit, unitId,
                            // sourceOfDefinition, symbol, dataType, definition[@en..], valueFormat}
                            repListOfLangStr(head + "preferredName", iec.PreferredName);
                            repListOfLangStr(head + "shortName", iec.ShortName);
                            rep(head + "unit", "" + iec.Unit);
                            repReference(head, "unitId", iec.UnitId);
                            rep(head + "sourceOfDefinition", "" + iec.SourceOfDefinition);
                            rep(head + "symbol", "" + iec.Symbol);
                            rep(head + "dataType", "" + Aas.Stringification.ToString(iec.DataType));
                            repListOfLangStr(head + "definition", iec.Definition);
                            rep(head + "valueFormat", "" + iec.ValueFormat);

                            // do a bit for anyName
                            string anyName = null;
                            if (iec.PreferredName != null)
                                anyName = iec.PreferredName.GetDefaultString();
                            if (anyName == null && iec.ShortName != null)
                                anyName = iec.ShortName.GetDefaultString();
                            if (anyName == null)
                                anyName = cd.IdShort;
                            if (anyName != null)
                                rep(head + "anyName", "" + anyName);
                        }
                    }
                }
            }

            // see: https://codereview.stackexchange.com/questions/119519/
            // regex-to-first-match-then-replace-found-matches
            public static string Replace(string s, int index, int length, string replacement)
            {
                var builder = new StringBuilder();
                builder.Append(s.Substring(0, index));
                builder.Append(replacement);
                builder.Append(s.Substring(index + length));
                return builder.ToString();
            }

            public void ProcessCellRecord(CellRecord cr)
            {
                if (Record == null || regexReplacements == null || regexCommands == null)
                    return;

                // local
                var input = cr.Text;

                // newline
                if (ReplaceNewlineWith != null)
                {
                    input = input.Replace("\r\n", ReplaceNewlineWith);
                    input = input.Replace("\n\r", ReplaceNewlineWith);
                    input = input.Replace("\n", ReplaceNewlineWith);
                }

                // process from right to left
                // see: https://codereview.stackexchange.com/questions/119519/
                // regex-to-first-match-then-replace-found-matches
                var matchesReplace = regexReplacements.Matches(input);
                foreach (var match in matchesReplace.Cast<Match>().Reverse())
                {
                    if (match.Groups.Count < 2)
                        continue;

                    // OK, found a placeholder-tag to replace
                    var tag = "" + match.Groups[1].Value.Trim().ToLower();

                    if (repDict.ContainsKey(tag))
                    {
                        input = Replace(input, match.Index, match.Length, repDict[tag]);
                        NumberReplacements++;
                    }
                    else
                    if (tag == "stop")
                    {
                        // do nothing! see below!
                        ;
                    }
                    else
                    {
                        // placeholder not found!
                        if (Record.ReplaceFailedMatches)
                        {
                            input = Replace(input, match.Index, match.Length, Record.FailText);
                        }
                    }
                }

                // special case
                while (true)
                {
                    var matchStop = regexStop.Match(input);
                    if (!matchStop.Success)
                        break;

                    var left = matchStop.Groups[1].ToString().Trim();
                    var right = matchStop.Groups[2].ToString().Trim();
                    if (left != "")
                        input = left;
                    else
                        input = right;
                }

                // commit back
                cr.Text = input;

                // for commands, use
                input = cr.TextWithHeaders;

                // evaluate commands
                var matchesCmd = regexCommands.Matches(input);
                foreach (var match in matchesCmd.Cast<Match>().Reverse())
                {
                    // access cmd
                    if (match.Groups.Count < 3)
                        continue;

                    var allMatch = match.Groups[0].ToString();
                    var cmd = match.Groups[2].ToString().Trim().ToLower();
                    var arg = match.Groups[3].ToString();
                    var argtl = arg.Trim().ToLower();

                    switch (cmd)
                    {
                        case "fg":
                            cr.Fg = argtl;
                            break;
                        case "bg":
                            cr.Bg = argtl;
                            break;
                        case "table-bg":
                            cr.TableBg = argtl;
                            break;
                        case "halign":
                            cr.HorizAlign = argtl;
                            break;
                        case "valign":
                            cr.VertAlign = argtl;
                            break;
                        case "font":
                            cr.Font = argtl;
                            break;
                        case "frame":
                            cr.Frame = argtl;
                            break;
                        case "width":
                            if (double.TryParse(argtl, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                                cr.Width = f;
                            break;
                        case "md":
                            cr.Md = argtl;
                            break;
                        case "colspan":
                            if (int.TryParse(argtl, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                                && i >= 2)
                                cr.ColSpan = i;
                            break;
                    }

                    // new approach: try to remove found text in cr.TextWithHeaders
                    // in the original cr.Text
                    int p = cr.Text.LastIndexOf(allMatch);
                    if (p >= 0)
                        cr.Text = cr.Text.Remove(p, allMatch.Length);
                }

            }
        }

        //
        // TAB separated
        //

        public bool ExportTabSeparated(
            string fn,
            List<ExportTableAasEntitiesList> iterateAasEntities,
            string tab = "\t")
        {
            // access
            if (Record?.IsValid() != true)
                return false;

            using (var f = new StreamWriter(fn))
            {

                // over entities
                foreach (var entities in iterateAasEntities)
                {
                    // top
                    var proc = new ItemProcessor(Record, entities.FirstOrDefault());
                    proc.Start();
                    proc.ReplaceNewlineWith = " "; // for TSF, this is not possible!
                    for (int ri = 0; ri < Record.RowsTop; ri++)
                    {
                        var line = "";

                        for (int ci = 0; ci < Record.Cols; ci++)
                        {
                            // get cell record
                            var cr = GetTopCell(ri, ci);

                            // process text
                            proc.ProcessCellRecord(cr);

                            // add
                            if (line != "")
                                line += tab;
                            line += cr.Text;
                        }

                        f.WriteLine(line);
                    }

                    // elements
                    foreach (var item in entities)
                    {
                        // create processing
                        proc = new ItemProcessor(Record, item);
                        proc.Start();
                        proc.ReplaceNewlineWith = " "; // for TSF, this is not possible!

                        var lines = new List<string>();

                        // all elements
                        for (int ri = 0; ri < Record.RowsBody; ri++)
                        {
                            var line = "";

                            for (int ci = 0; ci < Record.Cols; ci++)
                            {
                                // get cell record
                                var cr = GetBodyCell(ri, ci);

                                // process text
                                proc.ProcessCellRecord(cr);

                                // add
                                if (line != "")
                                    line += tab;
                                line += cr.Text;
                            }

                            lines.Add(line);
                        }

                        // export really?
                        if (proc.NumberReplacements > 0)
                            foreach (var line in lines)
                                f.WriteLine(line);
                    }

                    // empty rows
                    for (int i = 0; i < Math.Max(0, Record.RowsGap); i++)
                        f.WriteLine("");
                }
            }

            return true;
        }

        //
        // LaTex
        //

        public bool ExportLaTex(string fn, List<ExportTableAasEntitiesList> iterateAasEntities)
        {
            // access
            if (Record?.IsValid() != true)
                return false;

            using (var f = new StreamWriter(fn))
            {
                f.WriteLine("Not yet implemented");
            }

            return true;
        }

        //
        // Excel
        //

        private void ExportExcel_AppendTableCell(IXLWorksheet ws, CellRecord cr, int ri, int ci)
        {
            // access
            if (ws == null || cr == null)
                return;

            // basic cell
            var cell = ws.Cell(ri, ci);

            // always wrapping text
            cell.Value = "" + cr.Text;
            cell.Style.Alignment.WrapText = true;

            // alignments
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            if (cr.HorizAlign != null)
            {
                if (cr.HorizAlign == "center")
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                if (cr.HorizAlign == "right")
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            if (cr.VertAlign != null)
            {
                if (cr.VertAlign == "center")
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (cr.VertAlign == "bottom")
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            }

            // colors
            if (cr.Bg != null)
            {
                // ReSharper disable PossibleNullReferenceException
                try
                {
                    var bgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Bg);
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(bgc.A, bgc.R, bgc.G, bgc.B);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }
                // ReSharper enable PossibleNullReferenceException
            }

            if (cr.Fg != null)
            {
                // ReSharper disable PossibleNullReferenceException
                try
                {
                    var fgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Fg);
                    cell.Style.Font.FontColor = XLColor.FromArgb(fgc.A, fgc.R, fgc.G, fgc.B);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }
                // ReSharper enable PossibleNullReferenceException
            }

            // font?
            if (cr.Font != null)
            {
                if (cr.Font.Contains("bold"))
                    cell.Style.Font.Bold = true;
                if (cr.Font.Contains("italic"))
                    cell.Style.Font.Italic = true;
                if (cr.Font.Contains("underline"))
                    cell.Style.Font.Underline = XLFontUnderlineValues.Single;
            }
        }

        public bool ExportExcel(string fn, List<ExportTableAasEntitiesList> iterateAasEntities)
        {
            // access
            if (Record?.IsValid() != true
                || !fn.HasContent() || iterateAasEntities == null || iterateAasEntities.Count < 1)
                return false;

            //
            // Excel init
            // Excel with pure OpenXML is very complicated, therefore ClosedXML was used on the top
            // see: https://github.com/closedxml/closedxml/wiki/Basic-Table
            //

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("AAS Export");

            //
            // Export
            //

            int rowIdx = 1;

            // over entities
            foreach (var entities in iterateAasEntities)
            {
                // start

                int startRowIdx = rowIdx;

                // header
                if (true)
                {
                    // in order to access the parent information, take the first entity
                    var proc = new ItemProcessor(Record, entities.FirstOrDefault());
                    proc.Start();

                    for (int ri = 0; ri < Record.RowsTop; ri++)
                    {
                        for (int ci = 0; ci < Record.Cols; ci++)
                        {
                            // get cell record
                            var cr = GetTopCell(ri, ci);

                            // process text
                            proc.ProcessCellRecord(cr);

                            // add
                            ExportExcel_AppendTableCell(ws, cr, rowIdx + ri, 1 + ci);
                        }
                    }

                    rowIdx += Record.RowsTop;
                }

                // elements
                if (true)
                {
                    foreach (var item in entities)
                    {
                        // create processing
                        var proc = new ItemProcessor(Record, item);
                        proc.Start();

                        // all elements
                        for (int ri = 0; ri < Record.RowsBody; ri++)
                        {
                            for (int ci = 0; ci < Record.Cols; ci++)
                            {
                                // get cell record
                                var cr = GetBodyCell(ri, ci);

                                // process text
                                proc.ProcessCellRecord(cr);

                                // add
                                ExportExcel_AppendTableCell(ws, cr, rowIdx + ri, 1 + ci);
                            }
                        }

                        // export really?
                        if (proc.NumberReplacements > 0)
                        {
                            // advance
                            rowIdx += Record.RowsBody;
                        }
                        else
                        {
                            // delete this out
                            var rng = ws.Range(rowIdx, 1, rowIdx + Record.RowsBody - 1, 1 + Record.Cols - 1);
                            rng.Clear();
                        }
                    }
                }

                // some modifications on the whole table?
                if (true)
                {
                    if (rowIdx > startRowIdx + 1)
                    {
                        // do a explicit process of overall table cell
                        var proc = new ItemProcessor(Record, null);
                        proc.Start();
                        var cr = GetTopCell(0, 0);
                        proc.ProcessCellRecord(cr);

                        // borders?
                        if (cr.Frame != null)
                        {
                            var rng = ws.Range(startRowIdx, 1, rowIdx - 1, 1 + Record.Cols - 1);

                            if (cr.Frame == "1")
                            {
                                rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                rng.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            }

                            if (cr.Frame == "2")
                            {
                                rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                                rng.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            }

                            if (cr.Frame == "3")
                            {
                                rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                                rng.Style.Border.InsideBorder = XLBorderStyleValues.Thick;
                            }
                        }

                        // column widths
                        ws.Columns(1, Record.Cols).AdjustToContents();

                        // custom column width
                        for (int ci = 0; ci < Record.Cols; ci++)
                        {
                            // get the cell width from the very first top row
                            var cr2 = GetTopCell(0, ci);
                            proc.ProcessCellRecord(cr2);
                            if (cr2?.Width != null && cr2.Width.Value > 0)
                                ws.Column(1 + ci).Width = cr2.Width.Value;
                        }

                    }
                }

                // leave some lines blank

                rowIdx += Math.Max(0, Record.RowsGap);
            }

            //
            // End
            //

            // Save the new worksheet.
            wb.SaveAs(fn);

            return true;
        }

        //
        // Word
        //

        private void ExportWord_AppendTableCell(TableRow tr, CellRecord cr)
        {
            TableCell tc = tr.AppendChild(new TableCell());
            Paragraph para = tc.AppendChild(new Paragraph());

            // see: https://stackoverflow.com/questions/17675526/
            // how-can-i-modify-the-foreground-and-background-color-of-an-openxml-tablecell/17677892

            if (cr.HorizAlign != null)
            {
                ParagraphProperties pp = new ParagraphProperties();

                if (cr.HorizAlign == "left")
                    pp.Justification = new Justification() { Val = JustificationValues.Left };
                if (cr.HorizAlign == "center")
                    pp.Justification = new Justification() { Val = JustificationValues.Center };
                if (cr.HorizAlign == "right")
                    pp.Justification = new Justification() { Val = JustificationValues.Right };

                para.Append(pp);
            }

            if (cr.VertAlign != null || cr.Bg != null)
            {
                var tcp = tc.AppendChild(new TableCellProperties());

                if (cr.VertAlign == "top")
                    tcp.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Top });
                if (cr.VertAlign == "center")
                    tcp.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
                if (cr.VertAlign == "bottom")
                    tcp.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Bottom });

                if (cr.Bg != null)
                {
                    // ReSharper disable PossibleNullReferenceException
                    try
                    {
                        var bgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Bg);
                        var bgs = new ColorConverter().ConvertToString(bgc).Substring(3);

                        tcp.Append(new Shading()
                        {
                            Color = "auto",
                            Fill = bgs,
                            Val = ShadingPatternValues.Clear
                        });
                    }
                    catch (Exception ex)
                    {
                        LogInternally.That.SilentlyIgnoredError(ex);
                    }
                    // ReSharper enable PossibleNullReferenceException
                }
            }

            // var run = new Run(new Text(cr.Text));
            // make a run with multiple breaks
            var run = new Run();
            var lines = cr.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var l in lines)
            {
                if (run.ChildElements != null && run.ChildElements.Count > 0)
                    run.AppendChild(new Break());
                run.AppendChild(new Text(l));
            }

            if (cr.Fg != null || cr.Font != null)
            {
                try
                {
                    var rp = new RunProperties();

                    if (cr.Fg != null)
                    {
                        var fgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Fg);
                        var fgs = new ColorConverter().ConvertToString(fgc).Substring(3);

                        rp.Append(new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = fgs });
                    }

                    if (cr.Font != null && cr.Font.Contains("bold"))
                        rp.Bold = new Bold();

                    if (cr.Font != null && cr.Font.Contains("italic"))
                        rp.Italic = new Italic();

                    if (cr.Font != null && cr.Font.Contains("underline"))
                        rp.Underline = new Underline();

                    run.RunProperties = rp;
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            para.Append(run);
        }

        public bool ExportWord(string fn, List<ExportTableAasEntitiesList> iterateAasEntities)
        {
            // access
            if (Record?.IsValid() != true)
                return false;

            // Create Document
            using (WordprocessingDocument wordDocument =
                WordprocessingDocument.Create(fn, WordprocessingDocumentType.Document, true))
            {
                //
                // Word init
                // see: http://www.ludovicperrichon.com/create-a-word-document-with-openxml-and-c/#table
                //

                // Add a main document part.
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                // Create the document structure and add some text.
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                //
                // Export
                //

                // over entities
                foreach (var entities in iterateAasEntities)
                {

                    // make a table
                    DocumentFormat.OpenXml.Wordprocessing.Table table =
                        body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Table());

                    // do a process on overall table cells
                    if (true)
                    {
                        var proc = new ItemProcessor(Record, null);
                        proc.Start();
                        var cr = GetTopCell(0, 0);
                        proc.ProcessCellRecord(cr);

                        // do some borders?
                        if (cr?.Frame != null)
                        {
                            UInt32Value thickOuter = 6;
                            UInt32Value thickInner = 6;
                            if (cr.Frame == "2")
                                thickOuter = 12;
                            if (cr.Frame == "3")
                            {
                                thickOuter = 12;
                            }

                            var tblProperties = table.AppendChild(new TableProperties());
                            var tblBorders = tblProperties.AppendChild(new TableBorders());
                            tblBorders.Append(
                                new TopBorder()
                                {
                                    Val = new EnumValue<BorderValues>(BorderValues.Thick),
                                    Color = "000000",
                                    Size = thickOuter
                                });
                            tblBorders.Append(
                                new LeftBorder()
                                {
                                    Val = new EnumValue<BorderValues>(BorderValues.Thick),
                                    Color = "000000",
                                    Size = thickOuter
                                });
                            tblBorders.Append(
                                new RightBorder()
                                {
                                    Val = new EnumValue<BorderValues>(BorderValues.Thick),
                                    Color = "000000",
                                    Size = thickOuter
                                });
                            tblBorders.Append(
                                new BottomBorder()
                                {
                                    Val = new EnumValue<BorderValues>(BorderValues.Thick),
                                    Color = "000000",
                                    Size = thickOuter
                                });
                            tblBorders.Append(
                                new InsideHorizontalBorder()
                                {
                                    Val = new EnumValue<BorderValues>(BorderValues.Thick),
                                    Color = "000000",
                                    Size = thickInner
                                });
                            tblBorders.Append(
                                new InsideVerticalBorder()
                                {
                                    Val = new EnumValue<BorderValues>(BorderValues.Thick),
                                    Color = "000000",
                                    Size = thickInner
                                });
                        }
                    }

                    // header
                    if (true)
                    {
                        // in order to access the parent information, take the first entity
                        var proc = new ItemProcessor(Record, entities.FirstOrDefault());
                        proc.Start();

                        for (int ri = 0; ri < Record.RowsTop; ri++)
                        {
                            // new row
                            TableRow tr = table.AppendChild(new TableRow());

                            // over cells
                            for (int ci = 0; ci < Record.Cols; ci++)
                            {
                                // get cell record
                                var cr = GetTopCell(ri, ci);

                                // process text
                                proc.ProcessCellRecord(cr);

                                // add
                                ExportWord_AppendTableCell(tr, cr);
                            }
                        }
                    }

                    // elements
                    if (true)
                    {
                        foreach (var item in entities)
                        {
                            // create processing
                            var proc = new ItemProcessor(Record, item);
                            proc.Start();

                            // remember rows in order to can deleten them later
                            var newRows = new List<TableRow>();

                            // all elements
                            for (int ri = 0; ri < Record.RowsBody; ri++)
                            {
                                // new row
                                TableRow tr = table.AppendChild(new TableRow());
                                newRows.Add(tr);

                                // over cells
                                for (int ci = 0; ci < Record.Cols; ci++)
                                {
                                    // get cell record
                                    var cr = GetBodyCell(ri, ci);

                                    // process text
                                    proc.ProcessCellRecord(cr);

                                    // add
                                    ExportWord_AppendTableCell(tr, cr);
                                }
                            }

                            // export really?
                            if (proc.NumberReplacements > 0)
                            {
                                // advance
                            }
                            else
                            {
                                // delete this out
                                foreach (var r in newRows)
                                    table.RemoveChild(r);
                            }
                        }
                    }

                    // empty rows
                    for (int i = 0; i < Math.Max(0, Record.RowsGap); i++)
                        body.AppendChild(new Paragraph(new Run(new Text(" "))));

                }

                //
                // End
                //

            }

            return true;
        }

        //
        // Markdown (GitHub syntax)
        //

        public bool ExportMarkdownGithub(
            string fn,
            List<ExportTableAasEntitiesList> iterateAasEntities)
        {
            // access
            if (Record?.IsValid() != true)
                return false;

            using (var f = new StreamWriter(fn))
            {
                // Heading
                if (!Record.NoHeadings)
                    f.WriteLine("# 3 Heading");
                f.WriteLine();

                // over entities
                var headingIdx = 1;
                foreach (var entities in iterateAasEntities)
                {
                    // top entity
                    var topEnt = entities.FirstOrDefault();

                    // Heading
                    if (!Record.NoHeadings)
                    {
                        var hr = topEnt?.GetHeadingReferable();
                        f.WriteLine($"## 3.{headingIdx++} {(hr != null ? hr.IdShort : "Heading")}");
                    }

                    // one blank line is important for Markdown
                    f.WriteLine();

                    // top
                    var proc = new ItemProcessor(Record, topEnt);
                    proc.Start();
                    proc.ReplaceNewlineWith = "<br/>";

                    for (int ri = 0; ri < Record.RowsTop; ri++)
                    {
                        var line = "|";
                        var headline = false;
                        for (int ci = 0; ci < Record.Cols; ci++)
                        {
                            // get cell record
                            var cr = GetTopCell(ri, ci);

                            // process text
                            proc.ProcessCellRecord(cr);

                            // flags
                            if (cr.Md.Contains("headline"))
                                headline = true;

                            // add
                            line += " " + cr.Text + "|";
                        }

                        if (headline)
                            f.WriteLine();

                        f.WriteLine(line);

                        // write dashes line
                        if (headline)
                        {
                            // Markdown .. indicate table horizontal row
                            f.Write("|");
                            for (int ci = 0; ci < Record.Cols; ci++)
                                f.Write(" :--- |");
                            f.WriteLine();
                        }
                    }

                    // elements
                    foreach (var item in entities)
                    {
                        // create processing
                        proc = new ItemProcessor(Record, item);
                        proc.Start();
                        proc.ReplaceNewlineWith = "<br/>";

                        var lines = new List<string>();

                        // all elements
                        for (int ri = 0; ri < Record.RowsBody; ri++)
                        {
                            var line = "|";
                            for (int ci = 0; ci < Record.Cols; ci++)
                            {
                                // get cell record
                                var cr = GetBodyCell(ri, ci);

                                // process text
                                proc.ProcessCellRecord(cr);

                                // add
                                line += " " + cr.Text + "|";
                            }

                            lines.Add(line);
                        }

                        // export really?
                        if (proc.NumberReplacements > 0)
                            foreach (var line in lines)
                                f.WriteLine(line);
                    }

                    // empty rows
                    for (int i = 0; i < Math.Max(0, Record.RowsGap); i++)
                        f.WriteLine("");
                }
            }

            return true;
        }

        //
        // AsciiDoc
        // see: https://asciidoc-py.github.io/chunked/index.html
        //

        protected class AsciiDocColorState
        {
            public string TableBg = null;

            /// <summary>
            /// 0 = at init, 1 = in table
            /// </summary>
            public int State = 0;

            public string CurrBg = null;
        }

        private string ExportAsciiDocEvalColumnText(
            CellRecord cr, AsciiDocColorState cs, ref int colSkip)
        {
            if (cr == null)
                return "";
            var colStart = "";
            if (cr.ColSpan > 1)
            {
                colStart = $"{cr.ColSpan}+";
                colSkip = cr.ColSpan - 1;
            }
            if (cr.Font != null)
            {
                if (cr.Font.Contains("bold"))
                    colStart += "s";
                if (cr.Font.Contains("italic"))
                    colStart += "e";
                if (cr.Font.Contains("header"))
                    colStart += "h";
            }

            // the pipe sets the column start
            colStart += "|";

            // background color handling
            if (cs != null)
            {
                // initial start
                if (cs.State == 0)
                {
                    // switch to table background or 1st cell background ..
                    if (cr.Bg?.HasContent() == true)
                    {
                        colStart += "{set:cellbgcolor:" + cr.Bg + "}";
                        cs.CurrBg = cr.Bg;
                    }
                    else
                    {
                        colStart += "{set:cellbgcolor:" + cs.TableBg + "}";
                        cs.CurrBg = cs.TableBg;
                    }
                    cs.State = 1;
                }
                else
                if (cr.Bg?.HasContent() == true && cr.Bg != cs.CurrBg)
                {
                    // switch to cell background
                    colStart += "{set:cellbgcolor:" + cr.Bg + "}";
                    cs.CurrBg = cr.Bg;
                }
                else
                if (cr.Bg?.HasContent() != true && cs.CurrBg != cs.TableBg)
                {
                    // switch back to table background
                    colStart += "{set:cellbgcolor:" + cs.TableBg + "}";
                    cs.CurrBg = cs.TableBg;
                }
            }

            // column body
            var colBody = cr.Text.Replace("|", "\\|");

            // foreground more straight forward
            // only "plain colors" allowed, no rgb valur :-(
            if (cr.Fg != null
                && Regex.IsMatch(cr.Fg, @"([a-zA-Z.]+)"))
            {
                // escape more
                colBody = colBody.Replace("#", "\\#");

                // unquoted text syntax
                colBody = " [" + cr.Fg + "]#" + colBody.Trim() + "#";
            }

            return colStart + colBody + Environment.NewLine;
        }

        public bool ExportAsciiDoc(
            string fn,
            List<ExportTableAasEntitiesList> iterateAasEntities)
        {
            // access
            if (Record?.IsValid() != true)
                return false;

            using (var f = new StreamWriter(fn))
            {
                // Heading
                if (!Record.NoHeadings)
                    f.WriteLine("== Tables");
                f.WriteLine();

                // over entities
                foreach (var entities in iterateAasEntities)
                {
                    // top entity
                    var topEnt = entities.FirstOrDefault();

                    // Heading
                    if (!Record.NoHeadings)
                    {
                        var hr = topEnt?.GetHeadingReferable();
                        f.WriteLine($"=== {(hr != null ? hr.IdShort : "Heading")}");
                    }

                    // one blank line is important for Markdown
                    f.WriteLine();

                    // top
                    var proc = new ItemProcessor(Record, topEnt);
                    proc.Start();
                    proc.ReplaceNewlineWith = " +" + Environment.NewLine;

                    // table header
                    var colSpeci = new List<string>();
                    for (int ci = 0; ci < Record.Cols; ci++)
                    {
                        // get the cell width from the very first top row
                        var cr = GetTopCell(0, ci);
                        proc.ProcessCellRecord(cr);
                        if (cr?.Width.HasValue == true)
                            colSpeci.Add($"{cr.Width.Value:f0}%");
                        else
                            colSpeci.Add("1");
                    }
                    f.WriteLine($"[width=\"100%\", cols=\"{string.Join(",", colSpeci)}\"]");
                    f.WriteLine("|===");

                    // figure out background of the wholew table (top is for top and bottom)
                    // this will enable color handling for the rest
                    AsciiDocColorState colorState = null;
                    {
                        var cr = GetTopCell(0, 0);
                        proc.ProcessCellRecord(cr);
                        if (cr.TableBg?.HasContent() == true)
                            colorState = new AsciiDocColorState() { TableBg = cr.TableBg };
                    }

                    // table top rows
                    for (int ri = 0; ri < Record.RowsTop; ri++)
                    {
                        var line = "";
                        int colSkip = 0;
                        for (int ci = 0; ci < Record.Cols; ci++)
                        {
                            // skip column
                            if (colSkip > 0)
                            {
                                colSkip--;
                                continue;
                            }

                            // get cell record
                            var cr = GetTopCell(ri, ci);

                            // process text
                            proc.ProcessCellRecord(cr);

                            // cell formatting
                            var colText = ExportAsciiDocEvalColumnText(cr, colorState, ref colSkip);

                            // add
                            line += colText;
                        }

                        f.WriteLine(line);
                    }

                    // end row
                    f.WriteLine("");
                    f.WriteLine("");

                    // elements
                    foreach (var item in entities)
                    {
                        // create processing
                        proc = new ItemProcessor(Record, item);
                        proc.Start();
                        proc.ReplaceNewlineWith = " +" + Environment.NewLine;

                        var lines = new List<string>();

                        // all elements
                        for (int ri = 0; ri < Record.RowsBody; ri++)
                        {
                            var line = "";
                            int colSkip = 0;
                            for (int ci = 0; ci < Record.Cols; ci++)
                            {
                                // skip column
                                if (colSkip > 0)
                                {
                                    colSkip--;
                                    continue;
                                }

                                // get cell record
                                var cr = GetBodyCell(ri, ci);

                                // process text
                                proc.ProcessCellRecord(cr);

                                // cell formatting
                                var colText = ExportAsciiDocEvalColumnText(cr, colorState, ref colSkip);

                                // add
                                line += colText;
                            }

                            lines.Add(line);
                            lines.Add("");
                            lines.Add("");
                        }

                        // export really?
                        if (proc.NumberReplacements > 0)
                            foreach (var line in lines)
                                f.WriteLine(line);
                    }

                    // table footer
                    f.WriteLine("|===");

                    // empty rows
                    for (int i = 0; i < Math.Max(0, Record.RowsGap); i++)
                        f.WriteLine("");
                }
            }

            return true;
        }
    }
}
