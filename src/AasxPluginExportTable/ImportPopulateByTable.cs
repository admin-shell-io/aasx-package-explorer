/*
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
    /// This class create a context to a Submodel, which shall be incrementally popoulated by
    /// table contents.
    /// </summary>
    public class ImportPopulateByTable
    {
        //
        // Context management
        //

        protected LogInstance _log;
        protected ImportExportTableRecord _job;
        protected AdminShell.Submodel _sm;
        protected AdminShell.AdministrationShellEnv _env;
        protected ExportTableOptions _options;

        protected List<ImportCellMatcherBase> _matcherTop;
        protected List<ImportCellMatcherBase> _matcherBody;

        public ImportPopulateByTable(
            LogInstance log,
            ImportExportTableRecord job,
            AdminShell.Submodel sm,
            AdminShell.AdministrationShellEnv env,
            ExportTableOptions options)
        {
            // context
            _log = log;
            _job = job;
            _sm = sm;
            _env = env;
            _options = options;

            // prepare Submodel
            if (sm == null || _job.Top == null || _job.Body == null)
                return;
            if (sm.submodelElements == null)
                sm.submodelElements = new AdminShell.SubmodelElementWrapperCollection();

            // prepare matchers
            _matcherTop = _job.Top.Select((s) => ImportCellMatcherBase.Create(s)).ToList();
            _matcherBody = _job.Body.Select((s) => ImportCellMatcherBase.Create(s)).ToList();
        }

        public bool IsValid
        {
            get
            {
                return true == _job?.IsValid()
                    && _sm?.submodelElements != null
                    && _env != null;
            }
        }

        //
        // Population
        //

        protected bool CheckMatcherCells(
            IImportTableProvider table,
            ImportCellMatchContextBase context,
            int rowofs,
            List<ImportCellMatcherBase> matcher,
            int matcherRows)
        {
            // access
            if (table == null || matcher == null || matcherRows < 1)
                return false;

            // over rows?
            if (rowofs + matcherRows > table.MaxRows)
                return false;

            for (int r = 0; r < matcherRows; r++)
                for (int c = 0; c < _job.Cols; c++)
                {
                    var cell = table.Cell(r + rowofs, c);
                    if (cell == null)
                        cell = "";

                    var mi = (1 + r) * (1 + _job.Cols) + 1 + c;
                    var mat = (mi >= matcher.Count) ? null : matcher[mi];
                    if (mat == null)
                    {
                        _log?.Info($"    invalid matcher at top ({1 + r}, {1 + c})");
                        return false;
                    }
                    if (!mat.Matches(context, cell))
                    {
                        _log?.Info("{0}", $"    matcher was false ({1 + r}, {1 + c}) " +
                            $"preset='{mat.Preset}' cell='{cell}'");
                        return false;
                    }
                }

            // ok
            return true;
        }

        protected class ContextResult
        {
            public AdminShell.Referable Elem;
            public AdminShell.SubmodelElementWrapperCollection Wrappers;
        }

        protected class FilteredElementName
        {
            public string Name = "";
            public string ValueType = "";

            public AdminShell.SubmodelElementWrapper.AdequateElementEnum NameEnum;

            public static FilteredElementName Parse(string str)
            {
                // access
                if (str == null || str.Trim() == "")
                    return null;

                FilteredElementName res = null;

                // match the most complex/ restricted format
                var m = Regex.Match(str, @"\[\s*(\w+)\s*\]\s*/\s*(\w+)");
                if (m.Success)
                    res = new FilteredElementName()
                    {
                        Name = m.Groups[1].ToString().Trim(),
                        ValueType = m.Groups[2].ToString().Trim()
                    };

                // again an acceptable format
                m = Regex.Match(str, @"(\w+)\s*/\s*(\w+)");
                if (res == null && m.Success)
                    res = new FilteredElementName()
                    {
                        Name = m.Groups[1].ToString().Trim(),
                        ValueType = m.Groups[2].ToString().Trim()
                    };

                // nothing with slash, but maybe only square?
                m = Regex.Match(str, @"\[\s*(\w+)\s*\]");
                if (res == null && m.Success)
                    res = new FilteredElementName()
                    {
                        Name = m.Groups[1].ToString().Trim(),
                        ValueType = ""
                    };

                // if not, best guess
                if (res == null)
                    res = new FilteredElementName()
                    {
                        Name = str.Trim(),
                        ValueType = ""
                    };

                // now check, if something meaningful was found
                if (res.Name.Trim().ToLower() == AdminShell.Key.Submodel.ToLower())
                {
                    // successful special case
                    res.Name = AdminShell.Key.Submodel;
                    return res;
                }

                // has to be a SME type
                var ae = AdminShell.SubmodelElementWrapper.GetAdequateEnum2(res.Name, useShortName: true);
                if (ae == AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                    return null;

                // ok, nice
                res.Name = AdminShell.SubmodelElementWrapper.GetAdequateName(ae);
                res.NameEnum = ae;
                return res;
            }
        }

        protected MultiValueDictionary<string, string> _idShortToParentName =
            new MultiValueDictionary<string, string>();

        protected ContextResult CreateTopReferable(
            ImportCellMatchContextBase context,
            bool actInHierarchy = false)
        {
            // access
            if (context?.Parent == null || _sm == null)
                return null;

            // what element to create? makes this sense?
            var fen = FilteredElementName.Parse(context.ParentElemName);
            if (fen == null)
                return null;
            if (fen.Name != AdminShell.Key.Submodel
                && fen.NameEnum != AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown
                && fen.NameEnum != AdminShell.SubmodelElementWrapper.AdequateElementEnum.SubmodelElementCollection)
                return null;

            // special case: directly into the (existing) Submodel
            ContextResult res = null;
            if (fen.Name == AdminShell.Key.Submodel)
            {
                // prepare the result (already)
                res = new ContextResult()
                {
                    Elem = _sm,
                    Wrappers = _sm.submodelElements
                };

                // kind of manually take over data
                // this changes the actual data of the Submodel the plugin is associated with!
                AasConvertHelper.TakeOverSmeToSm(context.Parent, _sm);

                // ok
                return res;
            }

            // ok, if not, then ordinary case: create a SME and add it (somewhere) to the SM
            // this ALREADY should take over the most of the data
            // Note: value data is not required, as fixed to SMC!
            var sme = AdminShell.SubmodelElementWrapper.CreateAdequateType(fen.NameEnum, context.Parent);
            if (!(sme is AdminShell.SubmodelElementCollection smesmc))
                return null;

            smesmc.value = new AdminShell.SubmodelElementWrapperCollection();

            res = new ContextResult()
            {
                Elem = sme,
                Wrappers = smesmc.value
            };

            // try to act within the hierarchy
            // does only search SME but no SM, however, this is not a flaw, as adding to SM is the default
            if (actInHierarchy && context.ParentParentName.HasContent() && context.Parent.idShort.HasContent())
            {
                foreach (var rootsmc in _sm.submodelElements.FindDeep<AdminShell.SubmodelElementCollection>((testsmc) =>
                {
                    // first condition is, that the parents match!
                    if (!testsmc.idShort.HasContent() || testsmc.parent == null)
                        return false;

                    // try testing of allowed parent names
                    if (!(testsmc.parent is AdminShell.Referable testsmcpar))
                        return false;
                    var test1 = context.ParentParentName.ToLower().Contains(testsmcpar.idShort.ToLower().Trim());
                    var test2 = false;
                    if (_idShortToParentName.ContainsKey(testsmcpar.idShort))
                        foreach (var pn in _idShortToParentName[testsmcpar.idShort])
                            test2 = test2 || context.ParentParentName.ToLower().Contains(pn.ToLower().Trim());

                    if (!(test1 || test2))
                        return false;

                    // next is, that some part of of given idShort match the idShort of children
                    // of investigated SMC
                    var parts = context.Parent.idShort.Split(new[] { ',', ';', '|' },
                                    StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                        if (part?.Trim().ToLower() == testsmc.idShort.Trim().ToLower())
                            return true;

                    // or, maybe more meaningful, if the semantic ids are the same?
                    if (context.Parent.semanticId?.IsEmpty == false
                        && testsmc.semanticId?.IsEmpty == false
                        && testsmc.semanticId.Matches(context.Parent.semanticId, AdminShell.Key.MatchMode.Relaxed))
                        return true;

                    // not found
                    return false;
                }))
                {
                    // rootsmc contains a valid SMC to the above criteria
                    // NOTHING needs to be added; found SMC needs to be given back
                    res.Elem = rootsmc;
                    res.Wrappers = rootsmc.value;

                    // this seems to be a valid ParentName, which is now "renamed" by the idShort
                    // of the SMC
                    _idShortToParentName.Add(rootsmc.idShort, context.Parent.idShort);

                    // need to adopt the rootsmc further by parent information??
                    ;

                    // ok
                    return res;
                }
            }

            // simply add new SMC directly to SM
            _sm.Add(sme);
            return res;
        }

        protected ContextResult CreateBodySme(
            ImportCellMatchContextBase context,
            ContextResult refTop)
        {
            // access
            if (context?.Sme == null || refTop?.Wrappers == null)
                return null;

            // make sure, there is an 'ordniary' SME to create
            var fen = FilteredElementName.Parse(context.SmeElemName);
            if (fen == null)
                return null;
            if (fen.NameEnum == AdminShellV20.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                return null;

            // create, add
            var sme = AdminShell.SubmodelElementWrapper.CreateAdequateType(fen.NameEnum, context.Sme);
            refTop.Wrappers.Add(sme);
            sme.parent = refTop.Elem; // unfortunately required, ass Wrapper.Add() cannot set parent
            var res = new ContextResult() { Elem = sme };

            // allow a selection a values
            if (sme is AdminShell.Property prop)
            {
                prop.value = context.SmeValue;

                // demux
                prop.valueType = fen.ValueType;
                if (!fen.ValueType.HasContent() && context.SmeValueType.HasContent())
                    prop.valueType = context.SmeValueType;
            }

            if (sme is AdminShell.MultiLanguageProperty mlp)
            {
                mlp.value = new AdminShell.LangStringSet(AdminShell.LangStr.LANG_DEFAULT, context.SmeValue);
            }

            if (sme is AdminShell.File file)
            {
                file.value = context.SmeValue;
            }

            if (sme is AdminShell.SubmodelElementCollection smc)
            {
                smc.value = new AdminShell.SubmodelElementWrapperCollection();
                res.Wrappers = smc.value;
            }

            // ok
            return res;
        }

        protected ContextResult CreateBodyCD(
            ImportCellMatchContextBase context,
            AdminShell.AdministrationShellEnv env)
        {
            // access
            if (context?.Sme == null || context?.CD == null || env == null || _options == null)
                return null;

            // first test, if the CD already exists
            var test = env.FindConceptDescription(context.Sme.semanticId);
            if (test != null)
                return new ContextResult() { Elem = test };

            // a semanticId is required to link the Sme and the CD together
            if (context.Sme.semanticId == null || context.Sme.semanticId.Count < 1)
            {
                // generate a new one for SME + CD
                // this modifies the SME!
                var id = new AdminShell.Identification(
                    AdminShell.Identification.IRI,
                    AdminShellUtil.GenerateIdAccordingTemplate(_options.TemplateIdConceptDescription));

                context.Sme.semanticId = new AdminShell.SemanticId(
                    new AdminShell.Key(AdminShell.Key.ConceptDescription, true, id.idType, id.id));
            }

            // create, add
            var cd = new AdminShell.ConceptDescription(context?.CD);
            env.ConceptDescriptions.Add(cd);
            var res = new ContextResult() { Elem = cd };

            // link CD to SME
            var sid = context.Sme.semanticId.GetAsExactlyOneKey();
            if (sid == null)
                // should not happen, see above
                return null;
            cd.identification = new AdminShell.Identification(sid.idType, sid.value);

            // some further attributes
            if (!cd.idShort.HasContent())
                cd.idShort = context.Sme.idShort;

            // ok
            return res;
        }

        protected bool CheckRowIsEmpty(IImportTableProvider table, int rowofs)
        {
            // access
            if (table == null || rowofs >= table.MaxRows)
                return true;

            // check
            var isEmpty = true;
            for (int c = 0; c < _job.Cols; c++)
                if (table.Cell(rowofs, c).HasContent())
                    isEmpty = false;
            return isEmpty;
        }

        public void PopulateBy(IImportTableProvider table)
        {
            // access
            if (!IsValid || table == null || table.MaxRows < 1 || table.MaxCols < 1
                || _matcherTop == null || _matcherBody == null)
                return;

            _log?.Info("Starting populating from NEW TABLE");

            // use 2 contexts to collect information during the matching
            var contextTop = new ImportCellMatchContextBase();
            var contextBody = new ImportCellMatchContextBase();

            // simply scan over potential tops and bodies
            int rowofs = 0;
            int conseqEmptyRows = 0;
            while (rowofs + _job.RowsTop + _job.RealRowsBody <= table.MaxRows)
            {
                // log
                _log?.Info("{0}", $"  check row {rowofs} starting with {"" + table.Cell(rowofs, 0)} for top ..");

                // first do a evaluation, if the complete row is empty
                if (CheckRowIsEmpty(table, rowofs))
                {
                    // break the full scanning process (using absurd high limit)?
                    conseqEmptyRows++;
                    if (conseqEmptyRows >= 100)
                        break;

                    // now, but still empty: take a shortcut to next row
                    rowofs++;
                    continue;
                }
                conseqEmptyRows = 0;

                // top matches?
                contextTop.Clear();
                if (CheckMatcherCells(table, contextTop, rowofs, _matcherTop, _job.RowsTop))
                {
                    // log
                    _log?.Info($"  found matching TOP!");

                    // care for the (containg) top element
                    var refTop = CreateTopReferable(contextTop, actInHierarchy: _job.ActInHierarchy);
                    if (refTop == null)
                    {
                        _log?.Info($"  error creating data for TOP! Skipping!");
                        rowofs++;
                        continue;
                    }

                    // try find elements
                    var rowofs2 = rowofs + _job.RowsTop;
                    var lastGoodRow = rowofs;

                    while (rowofs2 < table.MaxRows)
                    {
                        // log
                        _log?.Info("{0}", $"  check row {rowofs2} starting with " +
                            $"{"" + table.Cell(rowofs2, 0)} for body ..");

                        // be definition, a completely empty line will break the matching
                        if (CheckRowIsEmpty(table, rowofs2))
                            break;

                        // matches
                        contextBody.Clear();
                        if (CheckMatcherCells(table, contextBody, rowofs2, _matcherBody, _job.RowsBody))
                        {
                            // log again
                            _log?.Info($"    found matching BODY as well!");

                            // remember to never visit again
                            lastGoodRow = rowofs2;

                            // an CD with empty identification will cause a new id, therefore
                            // the SME.semanticId will be altered accordingly and will be
                            // written later
                            var cdBody = CreateBodyCD(contextBody, _env);
                            if (cdBody == null)
                            {
                                _log?.Info($"  error creating ConceptDescription for BODY! Skipping!");
                                rowofs2 += _job.RowsBody;
                                continue;
                            }

                            // create SME
                            var refBody = CreateBodySme(contextBody, refTop);
                            if (refBody == null)
                            {
                                _log?.Info($"  error creating SubmodelElement for BODY! Skipping!");
                                rowofs2 += _job.RowsBody;
                                continue;
                            }


                        }

                        // find next row?
                        rowofs2 += _job.RowsBody;
                    }

                    // advance at least to last good row + 1
                    rowofs = lastGoodRow + 1;
                }

                // default
                rowofs++;
            }
        }
    }
}
