/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxPredefinedConcepts;

namespace AasxPluginExportTable.Uml
{
    public interface IBaseWriter
    {
        void StartDoc(ExportUmlRecord options, Aas.Environment env);
        void ProcessTopElement(Aas.IReferable rf, int remainDepth = int.MaxValue);
        void ProcessPost();
        void SaveDoc(string fn);
    }

    /// <summary>
    /// Some utilities to write exports of Submodels and such
    /// </summary>
    public class BaseWriter
    {
        //
        // Members
        //

        protected Aas.Environment _env = null;
        protected ExportUmlRecord _options = new ExportUmlRecord();

        //
        // Ids
        //

        private Random rnd = new Random();

        public string GenerateId(string prefix)
        {
            return prefix + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + rnd.Next(0, 10000);
        }

        //
        // Rendering of attributes
        //

        public string EvalUmlMultiplicity(Aas.ISubmodelElement sme, bool noOne = false)
        {
            string res = AasSmtQualifiers.CardinalityToString(
                AasSmtQualifiers.SmtCardinality.One, format: 3, oneIsEmpty: noOne);
            var qf = AasSmtQualifiers.FindSmtCardinalityQualfier(sme?.Qualifiers);
            if (qf?.Value != null)
            {
                var card = AdminShellEnumHelper.GetEnumMemberFromValueString<AasSmtQualifiers.SmtCardinality>(
                        qf.Value, valElse: AasSmtQualifiers.SmtCardinality.One);

                res = AasSmtQualifiers.CardinalityToString(card, format: 3, oneIsEmpty: noOne);
            }
            return res;
        }

        public Tuple<string, string> EvalMultiplicityBounds(string multiplicity)
        {
            if (multiplicity == null || multiplicity.Length < 4)
                return new Tuple<string, string>("1", "1");
            return new Tuple<string, string>("" + multiplicity[0], "" + multiplicity[3]);
        }

        public bool IsUmlClass(Aas.IReferable rf)
        {
            // check, if rf is a class
            return (rf is Aas.Submodel 
                || rf is Aas.SubmodelElementCollection
                || rf is Aas.SubmodelElementList 
                || rf is Aas.Entity
                || rf is Aas.Operation);
        }

        public string EvalPossibleConceptClassName(
            Aas.IReferable rf, bool allowConceptClasses,
            out Aas.IConceptDescription resCd)
        {
            resCd = null;
            if (IsUmlClass(rf)
                    && allowConceptClasses
                    && _options?.ClassesFromConcepts == true
                    && rf is Aas.IHasSemantics ihs)
            {
                var cd = _env?.FindConceptDescriptionByReference(ihs.SemanticId);
                if (cd?.IdShort?.HasContent() == true
                    && rf.IdShort.Trim() != cd.IdShort.Trim())
                {
                    resCd = cd;
                    return cd.IdShort;
                }
            }
            return null;
        }

        public string EvalFeatureType(Aas.IReferable rf, bool allowConceptClasses = false)
        {
            if (rf is Aas.ISubmodelElement sme)
            {
                if (sme is Aas.Property p)
                    return Aas.Stringification.ToString(p.ValueType);
                var pcn = EvalPossibleConceptClassName(sme, 
                    allowConceptClasses: allowConceptClasses,
                    out var resCd);
                if (pcn != null)
                    return pcn;
            }

            return rf.GetSelfDescription().ElementAbbreviation;
        }

        public string EvalInitialValue(Aas.ISubmodelElement sme, int limitToChars = -1)
        {
            // access
            if (sme == null || limitToChars == 0)
                return "";

            var res = "";
            if (sme is Aas.Property || sme is Aas.Range
                || sme is Aas.MultiLanguageProperty)
                res = sme.ValueAsText();

            if (limitToChars != -1 && res.Length > limitToChars)
                res = res.Substring(0, Math.Max(0, limitToChars - 3)) + "...";

            return res;
        }

        //
        // Register ids for objects
        //

        private Dictionary<string, object> _registeredObjects = new Dictionary<string, object>();
        private int _regObjIndex = 0;

        public string RegisterObject(object obj)
        {
            _regObjIndex += 1;
            var key = "ID" + _regObjIndex.ToString("X8");
            _registeredObjects.Add(key, obj);
            return key;
        }

        public object FindRegisteredObjects(string key)
        {
            if (!_registeredObjects.ContainsKey(key))
                return null;
            return _registeredObjects[key];
        }

    }
}
