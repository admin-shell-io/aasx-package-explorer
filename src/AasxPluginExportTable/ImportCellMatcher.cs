/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using System.Text.RegularExpressions;

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
        ///  Factory for new cell matchers
        /// </summary>
        public static ImportCellMatcherBase Create(string preset)
        {
            var tripre = preset.Trim();
            var percnum = tripre.Count(c => c == '%');
            
            // try to do a quite strict check
            
            if (percnum == 2 && tripre.Length > 2 && tripre.StartsWith("%") && tripre.EndsWith("%"))
                return new ImportCellMatcherVariable(tripre);

            return new ImportCellMatcherConstant(preset);
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

#if _not_anymore_required
            // elementName
            if (preset == "elementName")
            {
                // in square brackets?
                var m = Regex.Match(cell, @"\[(\w+)\]");
                if (m.Success)
                {
                    elemName = m.Groups[1].ToString();
                    return true;
                }

                // plain -> full string
                elemName = cell;
                return true;
            }
#endif

            // lambda trick to save {}
            var res = false;
            Func<string, string> commit = (s) => { res = true; return s; };

            if (preset == "elementName")
                elemName = commit(cell);

            if (preset == "parent")
                parentName = commit(cell);

            if (preset == "idShort")
                elem.idShort = commit(cell);

            if (preset == "semanticId")
                elem.semanticId = CreateSemanticId(commit(cell));

            if (preset == "description")
                elem.description = new AdminShell.Description(
                    new AdminShell.LangStringSet(
                        AdminShell.ListOfLangStr.Parse(commit(cell))));

            if (preset == "value")
                valueStr = commit(cell);

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

            return res;
        }

        private bool MatchEntity(AdminShell.ConceptDescription cd, string preset, string cell)
        {
            // access
            if (cd == null || preset == null || cell == null)
                return false;

            // lambda trick to save {}
            var res = false;
            Func<string, string> commit = (s) => { res = true; return s; };

            if (preset == "preferredName")
                cd.IEC61360Content.preferredName = new AdminShell.LangStringSetIEC61360(
                    AdminShell.ListOfLangStr.Parse(commit(cell)));

            if (preset == "definition")
                cd.IEC61360Content.definition = new AdminShell.LangStringSetIEC61360(
                    AdminShell.ListOfLangStr.Parse(commit(cell)));

            if (preset == "unit")
                cd.IEC61360Content.unit = commit(cell);

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

            if (Preset.StartsWith("%SME.") && Preset.EndsWith("%"))
                return MatchEntity(context.Sme, Preset.Substring(5, Preset.Length - 6), cell, 
                    ref context.SmeParentName, ref context.SmeElemName, ref context.SmeValue, allowMultiplicity: true);

            if (Preset.StartsWith("%CD.") && Preset.EndsWith("%"))
                return MatchEntity(context.CD, Preset.Substring(4, Preset.Length - 5), cell);

            // for testing purposes
            return true;
        }
    }
}
