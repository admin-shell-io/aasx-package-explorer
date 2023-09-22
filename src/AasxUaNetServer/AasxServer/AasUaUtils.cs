/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AdminShellNS;
using Extensions;
using Opc.Ua;
using Aas = AasCore.Aas3_0;

namespace AasOpcUaServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AasUaUtils
    {
        public static string ToOpcUaName(string input)
        {
            var clean = Regex.Replace(input, @"[^a-zA-Z0-9\-_]", "_");
            while (true)
            {
                var len0 = clean.Length;
                clean = clean.Replace("__", "_");
                if (len0 == clean.Length)
                    break;
            }
            return clean;
        }

        public static string ToOpcUaReference(Aas.IReference refid)
        {
            if (refid == null || refid.IsEmpty())
                return null;

            var semstr = "";
            foreach (var k in refid.Keys)
            {
                if (semstr != "")
                    semstr += ",";
                semstr += String.Format("({0}){1}", k.Type, k.Value);
            }

            return semstr;
        }

        public static List<string> ToOpcUaReferenceList(Aas.IReference refid)
        {
            if (refid == null || refid.IsEmpty())
                return null;

            var res = new List<string>();
            foreach (var k in refid.Keys)
            {
                res.Add(String.Format("({0}){1}", k.Type, k.Value));
            }

            return res;
        }

        public static LocalizedText[] GetUaLocalizedTexts(IList<Aas.IAbstractLangString> ls)
        {
            if (ls == null || ls.Count < 1)
                return new[] { new LocalizedText("", "") };
            var res = new LocalizedText[ls.Count];
            for (int i = 0; i < ls.Count; i++)
                res[i] = new LocalizedText(ls[i].Language, ls[i].Text);
            return res;
        }

        public static LocalizedText[] GetUaLocalizedTexts(IList<Aas.ILangStringPreferredNameTypeIec61360> ls)
        {
            if (ls == null || ls.Count < 1)
                return new[] { new LocalizedText("", "") };
            var res = new LocalizedText[ls.Count];
            for (int i = 0; i < ls.Count; i++)
                res[i] = new LocalizedText(ls[i].Language, ls[i].Text);
            return res;
        }

        public static LocalizedText[] GetUaLocalizedTexts(IList<Aas.ILangStringShortNameTypeIec61360> ls)
        {
            if (ls == null || ls.Count < 1)
                return new[] { new LocalizedText("", "") };
            var res = new LocalizedText[ls.Count];
            for (int i = 0; i < ls.Count; i++)
                res[i] = new LocalizedText(ls[i].Language, ls[i].Text);
            return res;
        }

        public static LocalizedText[] GetUaLocalizedTexts(IList<Aas.ILangStringDefinitionTypeIec61360> ls)
        {
            if (ls == null || ls.Count < 1)
                return new[] { new LocalizedText("", "") };
            var res = new LocalizedText[ls.Count];
            for (int i = 0; i < ls.Count; i++)
                res[i] = new LocalizedText(ls[i].Language, ls[i].Text);
            return res;
        }

        public static LocalizedText GetBestUaDescriptionFromAasDescription(List<Aas.ILangStringTextType> desc)
        {
            var res = new LocalizedText("", "");
            if (desc != null)
            {
                var found = false;
                foreach (var ls in desc)
                    if (!found || ls.Language.Trim().ToLower().StartsWith("en"))
                    {
                        found = true;
                        res = new LocalizedText(ls.Language, ls.Text);
                    }
            }
            return res;
        }

        public static bool AasValueTypeToUaDataType(
            Aas.DataTypeDefXsd valueType, out Type sharpType, out NodeId dataTypeId)
        {
            // defaults
            sharpType = "".GetType();
            dataTypeId = DataTypeIds.String;

            // parse
            if (valueType == Aas.DataTypeDefXsd.Boolean)
            {
                sharpType = typeof(bool);
                dataTypeId = DataTypeIds.Boolean;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.DateTime
                     || valueType == Aas.DataTypeDefXsd.Date
                     || valueType == Aas.DataTypeDefXsd.Time)
            {
                sharpType = typeof(Int64);
                dataTypeId = DataTypeIds.DateTime;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.Decimal
                     || valueType == Aas.DataTypeDefXsd.Integer
                     || valueType == Aas.DataTypeDefXsd.Long
                     || valueType == Aas.DataTypeDefXsd.NonPositiveInteger
                     || valueType == Aas.DataTypeDefXsd.NegativeInteger)
            {
                sharpType = typeof(Int64);
                dataTypeId = DataTypeIds.Int64;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.Int)
            {
                sharpType = typeof(Int32);
                dataTypeId = DataTypeIds.Int32;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.Short)
            {
                sharpType = typeof(Int16);
                dataTypeId = DataTypeIds.Int16;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.Byte)
            {
                sharpType = typeof(SByte);
                dataTypeId = DataTypeIds.Byte;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.NonNegativeInteger
                     || valueType == Aas.DataTypeDefXsd.PositiveInteger
                     || valueType == Aas.DataTypeDefXsd.UnsignedLong)
            {
                sharpType = typeof(UInt64);
                dataTypeId = DataTypeIds.UInt64;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.UnsignedInt)
            {
                sharpType = typeof(UInt32);
                dataTypeId = DataTypeIds.UInt32;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.UnsignedShort)
            {
                sharpType = typeof(UInt16);
                dataTypeId = DataTypeIds.UInt16;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.UnsignedByte)
            {
                sharpType = typeof(Byte);
                dataTypeId = DataTypeIds.Byte;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.Double)
            {
                sharpType = typeof(double);
                dataTypeId = DataTypeIds.Double;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.Float)
            {
                sharpType = typeof(float);
                dataTypeId = DataTypeIds.Float;
                return true;
            }
            else if (valueType == Aas.DataTypeDefXsd.String)
            {
                sharpType = typeof(string);
                dataTypeId = DataTypeIds.String;
                return true;
            }

            return false;
        }
    }
}
