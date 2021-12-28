﻿/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;

namespace AasxPluginExportTable
{
    /// <summary>
    /// Base class of an context, which will be provided to the matchers in
    /// order to collection information
    /// </summary>
    public class ImportCellMatchContextBase
    {
        /// <summary>
        /// The information is collected as SubmodelElements in order to be re-factored accordingly.
        /// Parent denotes the container, in which the SMEs are to be found (the "table head").
        /// SME are expressed by single table rows.
        /// </summary>
        public AdminShell.SubmodelElement Parent, Sme;
        public AdminShell.ConceptDescription CD;

        /// <summary>
        /// For identification purposes, the parent even of the parent is to be specified.
        /// </summary>
        public string ParentParentName, SmeParentName;

        /// <summary>
        /// The desired element type will be evaluated AFTER the collection phase.
        /// </summary>
        public string ParentElemName, SmeElemName;

        /// <summary>
        /// The desired element values will be evaluated AFTER the collection phase.
        /// </summary>
        public string ParentValue, SmeValue;

        /// <summary>
        /// Another special case to be parsed & set afterwards
        /// </summary>
        public string SmeValueType;

        public ImportCellMatchContextBase()
        {
            Clear();
        }

        public bool IsValid()
        {
            return Parent != null && Sme != null && CD != null;
        }

        public void Clear()
        {
            ParentElemName = "";
            SmeElemName = "";
            ParentParentName = "";
            SmeParentName = "";
            ParentValue = "";
            SmeValue = "";
            Parent = new AdminShell.SubmodelElement();
            Sme = new AdminShell.SubmodelElement();
            CD = new AdminShell.ConceptDescription();
            CD.identification = null;
            CD.CreateDataSpecWithContentIec61360();
        }
    }

    /// <summary>
    /// This is the base class for all cell matcher, which will be initiazized 
    /// by an import preset per cell and allow matching capabilities.
    /// </summary>
    public class ImportCellMatcherBase
    {
        /// <summary>
        /// Cell preset from the job description
        /// </summary>
        public string Preset;

        /// <summary>
        /// Successful match is optinal
        /// </summary>
        public bool Optional;

        /// <summary>
        ///  Factory for new cell matchers
        /// </summary>
        public static ImportCellMatcherBase Create(string preset)
        {
            // options - part 1
            string options = null;
            var m = Regex.Match(preset, @"%(opt)%(.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
            if (m.Success)
            {
                options = m.Groups[1].ToString();
                preset = m.Groups[2].ToString();
            }

            // prepare matching
            var tripre = preset.Trim();
            var percnum = tripre.Count(c => c == '%');

            // strict: exactly one variable            
            if (percnum == 2 && tripre.Length > 2 && tripre.StartsWith("%") && tripre.EndsWith("%"))
                return new ImportCellMatcherVariable(tripre);

            // match a sequence?
            m = Regex.Match(tripre, @"^\s*%seq\s*=\s*([^%]+)%(.*)$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
            if (m.Success)
                return new ImportCellMatcherSequence(m.Groups[1].ToString(), m.Groups[2].ToString());

            // create
            var res = new ImportCellMatcherConstant(preset);

            // options - part 2
            if (options.HasContent())
            {
                if (options == "opt")
                    res.Optional = true;
            }

            // ok
            return res;
        }

        /// <summary>
        /// Checks, if the cell content can be matched by the matcher
        /// </summary>
        public virtual bool Matches(ImportCellMatchContextBase context, string cell)
        {
            return false;
        }
    }

    /// <summary>
    /// This matcher matches a constant cell
    /// </summary>
    public class ImportCellMatcherConstant : ImportCellMatcherBase
    {
        public bool MatchStart, MatchContains;

        public ImportCellMatcherConstant(string preset)
        {
            Preset = preset;

            if (Preset.StartsWith("^"))
            {
                MatchStart = true;
                Preset = Preset.Substring(1);
            }

            if (Preset.StartsWith("."))
            {
                MatchContains = true;
                Preset = Preset.Substring(1);
            }
        }

        public override bool Matches(ImportCellMatchContextBase context, string cell)
        {
            return
                   MatchStart && cell.StartsWith(Preset)
                || MatchContains && cell.Contains(Preset)
                || cell == Preset;
        }
    }

    /// <summary>
    /// This matcher matches a variable cell
    /// </summary>
    public class ImportCellMatcherVariable : ImportCellMatcherBase
    {
        public ImportCellMatcherVariable(string preset)
        {
            Preset = preset;
        }

        private AdminShell.SemanticId CreateSemanticId(string cell)
        {
            if (!cell.HasContent())
                return null;

            var key = AdminShell.Key.Parse(cell, AdminShell.Key.ConceptDescription,
                        allowFmtAll: true);
            if (key == null)
                return null;

            return new AdminShell.SemanticId(key);
        }

        private bool MatchEntity(
            AdminShell.SubmodelElement elem, string preset, string cell,
            ref string parentName, ref string elemName, ref string valueStr,
            bool allowMultiplicity = false)
        {
            // access
            if (elem == null || preset == null || cell == null)
                return false;

            // lambda trick to save lines of code
            var res = false;
            Func<string, string> commit = (s) => { res = true; return s; };

            if (preset == "elementName")
                elemName = commit(cell);

            if (preset == "parent")
                parentName = commit(cell);

            if (preset == "idShort")
                elem.idShort = commit(cell);

            if (preset == "category")
                elem.category = commit(cell);

            if (preset == "kind")
                elem.kind = new AdminShell.ModelingKind(commit(cell));

            if (preset == "semanticId")
                elem.semanticId = CreateSemanticId(commit(cell));

            if (preset == "description")
                elem.description = new AdminShell.Description(
                    new AdminShell.LangStringSet(
                        AdminShell.ListOfLangStr.Parse(commit(cell))));

            if (preset == "value")
                valueStr = commit(cell);

            if (preset == "valueType")
            {
                // value type can be distorted in many ways, so commit in each case
                var vt = commit(cell);

                // adopt
                var m = Regex.Match(vt, @"^\s*\[(.*)\]");
                if (m.Success)
                    vt = m.Groups[1].ToString();

                // exclude SMEs
                foreach (var x in AdminShell.SubmodelElementWrapper.AdequateElementNames
                                    .Union(AdminShell.SubmodelElementWrapper.AdequateElementShortName))
                    if (x != null && vt.Trim().ToLower() == x.ToLower())
                        return true;

                // very special case
                if (vt.Trim() == "n/a")
                    return true;

                // set
                if (elem is AdminShell.Property prop)
                    prop.valueType = vt;
            }

            // very special
            if (allowMultiplicity && preset == "multiplicity")
            {
                var multival = "One";
                var tricell = cell.Trim();
                if (tricell == "0..1" || tricell == "[0..1]" || tricell == "ZeroToOne")
                    multival = "ZeroToOne";
                if (tricell == "0..*" || tricell == "[0..*]" || tricell == "ZeroToMany")
                    multival = "ZeroToMany";
                if (tricell == "1..*" || tricell == "[1..*]" || tricell == "OneToMany")
                    multival = "OneToMany";
                if (elem.qualifiers == null)
                    elem.qualifiers = new AdminShell.QualifierCollection();
                elem.qualifiers.Add(new AdminShell.Qualifier() { type = "Multiplicity", value = multival });
                return true;
            }

            // very crazy to split in multiple Qualifiers
            // idea: use '|' to delimit Qualifiers, use 't,s=v,id' to parse Qualifiers,
            //       use ',' to delimit keys (after first ','), use xx[yy]zzz to parse keys
            if (preset == "qualifiers")
            {
                var qstr = commit(cell);
                if (qstr != null)
                {
                    var qparts = qstr.Split(new[] { '|', '*', '\r', '\n', '\t' }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach(var qp in qparts)
                    {
                        var q = AdminShell.Qualifier.Parse(qp);
                        if (q != null)
                            elem.qualifiers.Add(q);
                    }
                }
            }

            return res;
        }

        private void MatchSpecialCases(
            ImportCellMatchContextBase context, string preset, string cell)
        {
            // access
            if (context == null || preset == null || cell == null)
                return;

            // lambda trick to lines of code
            if (preset == "Property.valueType")
            {
                // value type can be distorted in many ways, so commit in each case
                var vt = cell;

                // adopt
                var m = Regex.Match(vt, @"^\s*\[(.*)\]");
                if (m.Success)
                    vt = m.Groups[1].ToString();

                // exclude SMEs
                foreach (var x in AdminShell.SubmodelElementWrapper.AdequateElementNames
                                    .Union(AdminShell.SubmodelElementWrapper.AdequateElementShortName))
                    if (x != null && vt.Trim().ToLower() == x.ToLower())
                        return;

                // very special case
                if (vt.Trim() == "n/a")
                    return;

                // set
                context.SmeValueType = vt;
            }
        }

        private bool MatchEntity(AdminShell.ConceptDescription cd, string preset, string cell)
        {
            // access
            if (cd == null || preset == null || cell == null)
                return false;

            // lambda trick to save lines of code
            var res = false;
            Func<string, string> commit = (s) => { res = true; return s; };

            if (preset == "preferredName")
                cd.IEC61360Content.preferredName = new AdminShell.LangStringSetIEC61360(
                    AdminShell.ListOfLangStr.Parse(commit(cell)));

            if (preset == "shortName")
                cd.IEC61360Content.shortName = new AdminShell.LangStringSetIEC61360(
                    AdminShell.ListOfLangStr.Parse(commit(cell)));

            if (preset == "definition")
                cd.IEC61360Content.definition = new AdminShell.LangStringSetIEC61360(
                    AdminShell.ListOfLangStr.Parse(commit(cell)));

            if (preset == "unit")
                cd.IEC61360Content.unit = commit(cell);

            if (preset == "unitId")
                cd.IEC61360Content.unitId = AdminShell.UnitId.CreateNew(
                    AdminShell.Reference.Parse(commit(cell)));

            if (preset == "sourceOfDefinition")
                cd.IEC61360Content.sourceOfDefinition = commit(cell);

            if (preset == "symbol")
                cd.IEC61360Content.symbol = commit(cell);

            if (preset == "dataType")
                cd.IEC61360Content.dataType = commit(cell);

            return res;
        }

        public override bool Matches(ImportCellMatchContextBase context, string cell)
        {
            // access
            if (true != context?.IsValid() || cell == null)
                return false;

            // try break down into pieces
            if (Preset.StartsWith("%Parent.") && Preset.EndsWith("%"))
                return MatchEntity(context.Parent, Preset.Substring(8, Preset.Length - 9), cell,
                    ref context.ParentParentName, ref context.ParentElemName, ref context.ParentValue);

            if (Preset.StartsWith("%") && Preset.EndsWith("%") && Preset.Length > 2)
                MatchSpecialCases(context, Preset.Substring(1, Preset.Length - 2), cell);

            if (Preset.StartsWith("%SME.") && Preset.EndsWith("%"))
                return MatchEntity(context.Sme, Preset.Substring(5, Preset.Length - 6), cell,
                    ref context.SmeParentName, ref context.SmeElemName, ref context.SmeValue, allowMultiplicity: true);

            if (Preset.StartsWith("%CD.") && Preset.EndsWith("%"))
                return MatchEntity(context.CD, Preset.Substring(4, Preset.Length - 5), cell);

            // for testing purposes
            return true;
        }
    }

    /// <summary>
    /// This matcher matches a constant cell
    /// </summary>
    public class ImportCellMatcherSequence : ImportCellMatcherBase
    {
        protected string _separator;
        protected List<ImportCellMatcherBase> _sequence;

        public ImportCellMatcherSequence(string separator, string preset)
        {
            // trivial
            Preset = preset;
            _sequence = new List<ImportCellMatcherBase>();

            // try to interpolate preset
            _separator = null;
            var septr = separator.Trim(' ');
            if (byte.TryParse(septr, out byte b))
                _separator = "" + Convert.ToChar(b);
            if (septr == @"\n" || septr == "<NL>")
                _separator = "\n";
            if (septr == @"\r" || septr == "<CR>")
                _separator = "\r";
            if (septr == @"\t" || septr == "<TAB>")
                _separator = "\t";
            if (_separator == null)
                _separator = separator;

            // now, split the preset iteratively
            while (true)
            {
                var m = Regex.Match(preset, @"%([^%]+)%(.*)$", RegexOptions.Compiled);
                if (!m.Success)
                    break;

                _sequence.Add(new ImportCellMatcherVariable("%" + m.Groups[1].ToString() + "%"));

                preset = m.Groups[2].ToString();
            }

            ;
        }

        public override bool Matches(ImportCellMatchContextBase context, string cell)
        {
            // trivial
            if (context == null || cell == null || _separator == null || _sequence == null)
                return false;

            // try split cell in a sequence
            var cellseq = cell.Split(new[] { _separator }, StringSplitOptions.None);
            if (cellseq.Length == 0)
                // zero is true
                return true;

            // go as far as we can
            for (int i = 0; i < Math.Min(_sequence.Count, cellseq.Length); i++)
                if (!_sequence[i].Matches(context, cellseq[i]) && !Optional)
                    return false;

            return true;
        }
    }
}
