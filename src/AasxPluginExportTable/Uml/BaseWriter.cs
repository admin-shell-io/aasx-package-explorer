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
using AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPluginExportTable.Uml
{
    public interface IBaseWriter
    {
        void StartDoc(ExportUmlRecord options);
        void ProcessSubmodel(Submodel submodel);
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

        // TODO (MIHO, 2021-12-24): check if to refactor multiplicity handling as utility

        public string EvalUmlMultiplicity(AasCore.Aas3_0_RC02.ISubmodelElement sme, bool noOne = false)
        {
            var one = AasFormConstants.FormMultiplicityAsUmlCardinality[(int)FormMultiplicity.One];
            string res = one;
            var q = sme?.Qualifiers?.FindType("Multiplicity");
            if (q != null)
            {
                foreach (var m in (FormMultiplicity[])Enum.GetValues(typeof(FormMultiplicity)))
                    if (("" + q.Value) == Enum.GetName(typeof(FormMultiplicity), m))
                        res = "" + AasFormConstants.FormMultiplicityAsUmlCardinality[(int)m];
            }

            if (noOne && res == one)
                res = "";

            return res;
        }

        public Tuple<string, string> EvalMultiplicityBounds(string multiplicity)
        {
            if (multiplicity == null || multiplicity.Length < 4)
                return new Tuple<string, string>("1", "1");
            return new Tuple<string, string>("" + multiplicity[0], "" + multiplicity[3]);
        }

        public string EvalFeatureType(AasCore.Aas3_0_RC02.IReferable rf)
        {
            if (rf is AasCore.Aas3_0_RC02.ISubmodelElement sme)
            {
                if (sme is AasCore.Aas3_0_RC02.Property p)
                    return Stringification.ToString(p.ValueType);

            }

            return rf.GetSelfDescription().AasElementName;
        }

        public string EvalInitialValue(AasCore.Aas3_0_RC02.ISubmodelElement sme, int limitToChars = -1)
        {
            // access
            if (sme == null || limitToChars == 0)
                return "";

            var res = "";
            if (sme is AasCore.Aas3_0_RC02.Property || sme is AasCore.Aas3_0_RC02.Range
                || sme is AasCore.Aas3_0_RC02.MultiLanguageProperty)
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
