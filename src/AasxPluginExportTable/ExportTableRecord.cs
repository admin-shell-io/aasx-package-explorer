using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;

// ReSharper disable PossiblyMistakenUseOfParamsMethod .. issue, even if according to samples of Word API

// Disable dead code detection due to comments such as `//-9- {Referable}.kind`
// dead-csharp off

namespace AasxPluginExportTable
{
    public class ExportTableAasEntitiesItem
    {
        public int depth;
        public AdminShell.Submodel sm;
        public AdminShell.SubmodelElement sme;
        public AdminShell.ConceptDescription cd;

        public ExportTableAasEntitiesItem(
            int depth, AdminShell.Submodel sm = null, AdminShell.SubmodelElement sme = null,
            AdminShell.ConceptDescription cd = null)
        {
            this.depth = depth;
            this.sm = sm;
            this.sme = sme;
            this.cd = cd;
        }
    }

    public class ExportTableAasEntitiesList : List<ExportTableAasEntitiesItem>
    {

    }

    public class ExportTableRecord
    {
        //
        // Types
        //

        public enum FormatEnum { TSF = 0, LaTex, Word, Excel }
        public static string[] FormatNames = new string[] { "Tab separated", "LaTex", "Word", "Excel" };

        //
        // Members
        //

        public string Name = "";

        public int Format = 0;

        public int Rows = 1, Cols = 1;

        [JsonIgnore]
        public int RealRows { get { return 1 + Rows; } }
        [JsonIgnore]
        public int RealCols { get { return 1 + Rows; } }

        public bool ReplaceFailedMatches = false;
        public string FailText = "";

        // Note: the records contains elements for 1 + Rows, 1 + Columns fields
        public List<string> Header = new List<string>();
        public List<string> Elements = new List<string>();

        public bool IsValid()
        {
            return Rows >= 1 && Cols >= 1
                && Header != null && Header.Count >= RealRows * RealCols
                && Elements != null && Elements.Count >= RealRows * RealCols;
        }

        //
        // Constructurs
        //

        public ExportTableRecord() { }

        public ExportTableRecord(
            int rows, int cols, string name = "", IEnumerable<string> header = null,
            IEnumerable<string> elements = null)
        {
            this.Rows = rows;
            this.Cols = cols;
            if (name != null)
                this.Name = name;
            if (header != null)
                foreach (var h in header)
                    this.Header.Add(h);
            if (elements != null)
                foreach (var e in elements)
                    this.Elements.Add(e);
        }

        public void SaveToFile(string fn)
        {
            using (StreamWriter file = File.CreateText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, this);
            }
        }

        public static ExportTableRecord LoadFromFile(string fn)
        {
            using (StreamReader file = File.OpenText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                var res = (ExportTableRecord)serializer.Deserialize(file, typeof(ExportTableRecord));
                return res;
            }
        }

        //
        // Cells
        //

        public class CellRecord
        {
            public string Fg = null, Bg = null, HorizAlign = null, VertAlign = null, Font = null, Frame = null,
                Text = "", TextWithHeaders = "";

            public CellRecord() { }

            public CellRecord(string text)
            {
                this.Text = text;
            }
        }

        public CellRecord GetHeaderCell(int row, int col)
        {
            var i = (1 + row) * (1 + this.Cols) + (1 + col);
            if (row < 0 || col < 0 || this.Header == null || i >= this.Header.Count)
                return null;
            var cr = new CellRecord(this.Header[i]);
            cr.TextWithHeaders =
                this.Header[0] + " " + this.Header[(1 + row) * (1 + this.Cols)] + " " +
                this.Header[1 + col] + " " + cr.Text;
            return cr;
        }

        public CellRecord GetElementsCell(int row, int col)
        {
            var i = (1 + row) * (1 + this.Cols) + (1 + col);
            if (row < 0 || col < 0 || this.Elements == null || i >= this.Elements.Count)
                return null;
            var cr = new CellRecord(this.Elements[i]);
            cr.TextWithHeaders =
                this.Elements[0] + " " + this.Elements[(1 + row) * (1 + this.Cols)] + " " +
                this.Elements[1 + col] + " " + cr.Text;
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

            ExportTableRecord Record = null;
            public ExportTableAasEntitiesItem Item = null;

            public ItemProcessor() { }

            public string ReplaceNewlineWith = null;

            public int NumberReplacements = 0;

            public ItemProcessor(ExportTableRecord record, ExportTableAasEntitiesItem item)
            {
                this.Record = record;
                this.Item = item;
            }

            //
            // Internal
            //

            private Regex regexReplacements = null;
            private Regex regexCommands = null;
            private Dictionary<string, string> repDict = null;

            private void rep(string tag, string value)
            {
                tag = tag?.Trim().ToLower();
                if (tag == null || repDict == null || repDict.ContainsKey(tag))
                    return;
                repDict[tag] = value;
            }

            private void repLangStr(string head, AdminShell.LangStr ls)
            {
                if (ls == null)
                    return;
                rep(head + "@" + "" + ls.lang, "" + ls.str);
            }

            private void repListOfLangStr(string head, List<AdminShell.LangStr> lss)
            {
                if (lss == null)
                    return;
                foreach (var ls in lss)
                    repLangStr(head, ls);
            }

            private void repReferable(string head, AdminShell.Referable rf)
            {
                //-9- {Referable}.{idShort, category, description, description[@en..], elementName, parent}
                if (rf.idShort != null)
                    rep(head + "idShort", rf.idShort);
                if (rf.category != null)
                    rep(head + "category", rf.category);
                if (rf.description != null)
                    repListOfLangStr(head + "description", rf.description.langString);
                rep(head + "elementName", "" + rf.GetElementName());
                rep(head + "parent", "" + ((rf.parent?.idShort != null) ? rf.parent.idShort : "-"));
            }

            private void repModelingKind(string head, AdminShell.ModelingKind k)
            {
                if (k == null)
                    return;

                //-9- {Referable}.kind
                rep(head + "kind", "" + k.kind);
            }

            private void repQualifiable(string head, AdminShell.QualifierCollection qualifiers)
            {
                if (qualifiers == null)
                    return;

                //-9- {Qualifiable}.qualifiers
                rep(head + "qualifiers", "" + qualifiers.ToString(1));
            }

            private void repMultiplicty(string head, AdminShell.QualifierCollection qualifiers)
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
                        if (("" + q.value) == Enum.GetName(typeof(FormMultiplicity), m))
                        {
                            multiStr = "" + AasFormConstants.FormMultiplicityAsUmlCardinality[(int)m];
                        }
                }

                //-9- {Qualifiable}.multiplicity
                rep(head + "multiplicity", "" + multiStr);
            }

            private void repIdentifiable(string head, AdminShell.Identifiable ifi)
            {
                if (ifi == null)
                    return;
                if (ifi.identification != null)
                {
                    //-9- {Identifiable}.{identification[.{idType, id}], administration.{ version, revision}}
                    rep(head + "identification", "[" + ifi.identification.idType + "]" + ifi.identification.id);
                    rep(head + "identification.idType", "" + ifi.identification.idType);
                    rep(head + "identification.id", "" + ifi.identification.id);
                }
                if (ifi.administration != null)
                {
                    rep(head + "administration.version", "" + ifi.administration.version);
                    rep(head + "administration.revision", "" + ifi.administration.revision);
                }
            }

            private void repReference(string head, string refName, AdminShell.Reference rid)
            {
                if (rid == null || refName == null || rid.Keys == null)
                    return;

                // add together
                //-9- {Reference}
                rep(head + refName + "", "" + rid.ToString(1));

                // add the single parts of the sid
                for (int ki = 0; ki < rid.Keys.Count; ki++)
                {
                    var k = rid.Keys[ki];
                    if (k != null)
                    {
                        // in nice form
                        //-9- {Reference}[0..n]
                        rep(head + refName + $"[{ki}]", "" + k.ToString(1));
                        // but also in separate parts
                        //-9- {Reference}[0..n].{type, local, idType, value}
                        rep(head + refName + $"[{ki}].type", "" + k.type);
                        rep(head + refName + $"[{ki}].local", (!k.local) ? "no-local" : "local");
                        rep(head + refName + $"[{ki}].idType", "" + k.idType);
                        rep(head + refName + $"[{ki}].value", "" + k.value);
                    }
                }
            }

            public void Start()
            {
                // init Regex
                // nice tester: http://regexstorm.net/tester
                regexReplacements = new Regex(@"%([a-zA-Z0-9.@\[\]]+)%", RegexOptions.IgnoreCase);
                regexCommands = new Regex(@"%([A-Za-z0-9-_]+)=(.*?)%", RegexOptions.IgnoreCase);

                // init dictionary
                repDict = new Dictionary<string, string>();

                // some general replacements
                rep("nl", "" + Environment.NewLine);
                rep("br", "\r");
                rep("tab", "\t");

                // for the provided AAS entites, set the replacements
                if (this.Item != null)
                {
                    // shortcuts
                    var sm = this.Item.sm;
                    var sme = this.Item.sme;
                    var cd = this.Item.cd;

                    // general for the item
                    rep("depth", "" + this.Item.depth);
                    rep("indent", "" + (new string('~', Math.Max(0, this.Item.depth))));

                    //-1- {Referable} = {SM, SME, CD}
                    if (sm != null)
                    {
                        var head = "SM.";
                        repReferable(head, sm);
                        repModelingKind(head, sm.kind);
                        repQualifiable(head, sm.qualifiers);
                        repMultiplicty(head, sm.qualifiers);
                        repIdentifiable(head, sm);
                        //-1- {Reference} = {semanticId, isCaseOf, unitId}
                        repReference(head, "semanticId", sm.semanticId);
                    }

                    if (sme != null)
                    {
                        var head = "SME.";
                        repReferable(head, sme);
                        repModelingKind(head, sme.kind);
                        repQualifiable(head, sme.qualifiers);
                        repMultiplicty(head, sme.qualifiers);
                        repReference(head, "semanticId", sme.semanticId);

                        //-2- SME.value

                        if (sme is AdminShell.Property)
                        {
                            var p = sme as AdminShell.Property;
                            //-2- Property.{value, valueType, valueId}
                            rep("Property.value", "" + p.value);
                            rep("Property.valueType", "" + p.valueType);
                            if (p.valueId != null)
                                rep("Property.valueId", "" + p.valueId.ToString(1));

                            rep("SME.value", "" + p.value);
                        }

                        if (sme is AdminShell.MultiLanguageProperty)
                        {
                            var mlp = sme as AdminShell.MultiLanguageProperty;
                            //-2- MultiLanguageProperty.{value, vlaueId}
                            repListOfLangStr("MultiLanguageProperty.value", mlp.value?.langString);
                            if (mlp.valueId != null)
                                rep("MultiLanguageProperty.valueId", "" + mlp.valueId.ToString(1));

                            repListOfLangStr("SME.value", mlp.value?.langString);
                        }

                        if (sme is AdminShell.Range)
                        {
                            var r = sme as AdminShell.Range;
                            //-2- Range.{valueType, min, max}
                            rep("Range.valueType", "" + r.valueType);
                            rep("Range.min", "" + r.min);
                            rep("Range.max", "" + r.min);

                            rep("SME.value", "" + r.min + " .. " + r.max);
                        }

                        if (sme is AdminShell.Blob)
                        {
                            var b = sme as AdminShell.Blob;
                            //-2- Blob.{mimeType, value}
                            rep("Blob.mimeType", "" + b.mimeType);
                            rep("Blob.value", "" + b.value);

                            rep("SME.value", "" + ("" + b.value).Length + " bytes");
                        }

                        if (sme is AdminShell.File)
                        {
                            var f = sme as AdminShell.File;
                            //-2- File.{mimeType, value}
                            rep("File.mimeType", "" + f.mimeType);
                            rep("File.value", "" + f.value);

                            rep("SME.value", "" + f.value);
                        }

                        if (sme is AdminShell.ReferenceElement)
                        {
                            var re = sme as AdminShell.ReferenceElement;
                            //-2- ReferenceElement.value
                            rep("ReferenceElement.value", "" + re.value?.ToString(1));

                            rep("SME.value", "" + re.value?.ToString(1));
                        }

                        if (sme is AdminShell.RelationshipElement)
                        {
                            var re = sme as AdminShell.RelationshipElement;
                            //-2- RelationshipElement.{first, second}
                            rep("RelationshipElement.first", "" + re.first?.ToString(1));
                            rep("RelationshipElement.second", "" + re.second?.ToString(1));

                            rep("SME.value", "" + re.first?.ToString(1) + " -> " + re.second?.ToString(1));
                        }

                        if (sme is AdminShell.SubmodelElementCollection)
                        {
                            var smc = sme as AdminShell.SubmodelElementCollection;
                            //-2- SubmodelElementCollection.{value = #elements, ordered, allowDuplicates}
                            rep(
                                "SubmodelElementCollection.value", "" +
                                ((smc.value != null)
                                    ? smc.value.Count
                                    : 0) +
                                " elements");
                            rep("SubmodelElementCollection.ordered", "" + smc.ordered);
                            rep("SubmodelElementCollection.allowDuplicates", "" + smc.allowDuplicates);

                            rep("SME.value", "" + ((smc.value != null) ? smc.value.Count : 0) + " elements");
                        }

                        if (sme is AdminShell.Entity)
                        {
                            var ent = sme as AdminShell.Entity;
                            //-2- Entity.{entityType, asset}
                            rep("Entity.entityType", "" + ent.entityType);
                            if (ent.assetRef != null)
                                rep("Entity.asset", "" + ent.assetRef.ToString(1));
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
                            repListOfLangStr(head + "preferredName", iec.preferredName?.langString);
                            repListOfLangStr(head + "shortName", iec.shortName?.langString);
                            rep(head + "unit", "" + iec.unit);
                            repReference(head, "unitId", AdminShell.Reference.CreateNew(iec.unitId?.Keys));
                            rep(head + "sourceOfDefinition", "" + iec.sourceOfDefinition);
                            rep(head + "symbol", "" + iec.symbol);
                            rep(head + "dataType", "" + iec.dataType);
                            repListOfLangStr(head + "definition", iec.definition?.langString);
                            rep(head + "valueFormat", "" + iec.valueFormat);

                            // do a bit for anyName
                            string anyName = null;
                            if (iec.preferredName != null)
                                anyName = iec.preferredName.GetDefaultStr();
                            if (anyName == null && iec.shortName != null)
                                anyName = iec.shortName.GetDefaultStr();
                            if (anyName == null)
                                anyName = cd.idShort;
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
                if (this.ReplaceNewlineWith != null)
                {
                    input = input.Replace("\r\n", this.ReplaceNewlineWith);
                    input = input.Replace("\n\r", this.ReplaceNewlineWith);
                    input = input.Replace("\n", this.ReplaceNewlineWith);
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

                    if (this.repDict.ContainsKey(tag))
                    {
                        input = Replace(input, match.Index, match.Length, this.repDict[tag]);
                        this.NumberReplacements++;
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

                    var cmd = match.Groups[1].ToString().Trim().ToLower();
                    var arg = match.Groups[2].ToString();
                    var argtl = arg.Trim().ToLower();

                    switch (cmd)
                    {
                        case "fg":
                            cr.Fg = argtl;
                            break;
                        case "bg":
                            cr.Bg = argtl;
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
                    }

                    // in any case, replace the wohl match!
                    // input = Replace(input, match.Index, match.Length, "");
                }

            }
        }

        //
        // TAB separated
        //

        public bool ExportTabSeparated(string fn, ExportTableAasEntitiesList iterateAasEntities, string tab = "\t")
        {
            // access
            if (!IsValid())
                return false;


            using (var f = new StreamWriter(fn))
            {
                // header
                var proc = new ItemProcessor(this, null);
                for (int ri = 0; ri < this.Rows; ri++)
                {
                    var line = "";

                    for (int ci = 0; ci < this.Cols; ci++)
                    {
                        // get cell record
                        var cr = GetHeaderCell(ri, ci);

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
                foreach (var item in iterateAasEntities)
                {
                    // create processing
                    proc = new ItemProcessor(this, item);
                    proc.Start();
                    proc.ReplaceNewlineWith = ""; // for TSF, this is not possible!

                    var lines = new List<string>();

                    // all elements
                    for (int ri = 0; ri < this.Rows; ri++)
                    {
                        var line = "";

                        for (int ci = 0; ci < this.Cols; ci++)
                        {
                            // get cell record
                            var cr = GetElementsCell(ri, ci);

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
            }

            return true;
        }

        //
        // LaTex
        //

        public bool ExportLaTex(string fn, ExportTableAasEntitiesList iterateAasEntities)
        {
            // access
            if (!IsValid())
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
                // ReSharper disable EmptyGeneralCatchClause
                // ReSharper disable PossibleNullReferenceException
                try
                {
                    var bgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Bg);
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(bgc.A, bgc.R, bgc.G, bgc.B);
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
                // ReSharper enable PossibleNullReferenceException
            }

            if (cr.Fg != null)
            {
                // ReSharper disable EmptyGeneralCatchClause
                // ReSharper disable PossibleNullReferenceException
                try
                {
                    var fgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Fg);
                    cell.Style.Font.FontColor = XLColor.FromArgb(fgc.A, fgc.R, fgc.G, fgc.B);
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
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

        public bool ExportExcel(string fn, ExportTableAasEntitiesList iterateAasEntities)
        {
            // access
            if (!IsValid())
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
            int startRowIdx = rowIdx;

            // header
            if (true)
            {
                var proc = new ItemProcessor(this, null);
                proc.Start();

                for (int ri = 0; ri < this.Rows; ri++)
                {
                    for (int ci = 0; ci < this.Cols; ci++)
                    {
                        // get cell record
                        var cr = GetHeaderCell(ri, ci);

                        // process text
                        proc.ProcessCellRecord(cr);

                        // add
                        ExportExcel_AppendTableCell(ws, cr, rowIdx + ri, 1 + ci);
                    }
                }

                rowIdx += this.Rows;
            }

            // elements
            if (true)
            {
                foreach (var item in iterateAasEntities)
                {
                    // create processing
                    var proc = new ItemProcessor(this, item);
                    proc.Start();

                    // all elements
                    for (int ri = 0; ri < this.Rows; ri++)
                    {
                        for (int ci = 0; ci < this.Cols; ci++)
                        {
                            // get cell record
                            var cr = GetElementsCell(ri, ci);

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
                        rowIdx += this.Rows;
                    }
                    else
                    {
                        // delete this out
                        var rng = ws.Range(rowIdx, 1, rowIdx + this.Rows - 1, 1 + this.Cols - 1);
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
                    var proc = new ItemProcessor(this, null);
                    proc.Start();
                    var cr = GetHeaderCell(0, 0);
                    proc.ProcessCellRecord(cr);

                    // borders?
                    if (cr.Frame != null)
                    {
                        var rng = ws.Range(startRowIdx, 1, rowIdx - 1, 1 + this.Cols - 1);

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
                    ws.Columns(1, this.Cols).AdjustToContents();
                }
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
                    // ReSharper disable EmptyGeneralCatchClause
                    // ReSharper disable PossibleNullReferenceException
                    try
                    {
                        var bgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Bg);
                        var bgs = (new ColorConverter()).ConvertToString(bgc).Substring(3);

                        tcp.Append(new DocumentFormat.OpenXml.Wordprocessing.Shading()
                        {
                            Color = "auto",
                            Fill = bgs,
                            Val = ShadingPatternValues.Clear
                        });
                    }
                    catch { }
                    // ReSharper enable EmptyGeneralCatchClause
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
                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    var rp = new RunProperties();

                    if (cr.Fg != null)
                    {
                        var fgc = (System.Windows.Media.Color)ColorConverter.ConvertFromString(cr.Fg);
                        var fgs = (new ColorConverter()).ConvertToString(fgc).Substring(3);

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
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
            }

            para.Append(run);
        }

        public bool ExportWord(string fn, ExportTableAasEntitiesList iterateAasEntities)
        {
            // access
            if (!IsValid())
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

                // make a table
                Table table = body.AppendChild(new Table());

                // do a process on overall table cells
                if (true)
                {
                    var proc = new ItemProcessor(this, null);
                    proc.Start();
                    var cr = GetHeaderCell(0, 0);
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
                    var proc = new ItemProcessor(this, null);
                    proc.Start();

                    for (int ri = 0; ri < this.Rows; ri++)
                    {
                        // new row
                        TableRow tr = table.AppendChild(new TableRow());

                        // over cells
                        for (int ci = 0; ci < this.Cols; ci++)
                        {
                            // get cell record
                            var cr = GetHeaderCell(ri, ci);

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
                    foreach (var item in iterateAasEntities)
                    {
                        // create processing
                        var proc = new ItemProcessor(this, item);
                        proc.Start();

                        // remember rows in order to can deleten them later
                        var newRows = new List<TableRow>();

                        // all elements
                        for (int ri = 0; ri < this.Rows; ri++)
                        {
                            // new row
                            TableRow tr = table.AppendChild(new TableRow());
                            newRows.Add(tr);

                            // over cells
                            for (int ci = 0; ci < this.Cols; ci++)
                            {
                                // get cell record
                                var cr = GetElementsCell(ri, ci);

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

                //
                // End
                //

            }

            return true;
        }

    }
}
