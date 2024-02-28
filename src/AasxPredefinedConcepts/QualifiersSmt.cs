/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Aas3_0;
using AasxIntegrationBase;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Aas = AasCore.Aas3_0;

// reSharper disable UnusedType.Global
// reSharper disable ClassNeverInstantiated.Global

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// This class provides handles specific qualifiers, extensions
    /// for Submodel templates
    /// </summary>
    public static class AasSmtQualifiers
    {
        /// <summary>
        /// Semantically different, but factually equal to <c>FormMultiplicity</c>
        /// </summary>
        public enum SmtCardinality
        {
            [EnumMember(Value = "ZeroToOne")]
            [EnumMemberDisplay("ZeroToOne [0..1]")]
            ZeroToOne = 0,

            [EnumMember(Value = "One")]
            [EnumMemberDisplay("One [1]")]
            One,

            [EnumMember(Value = "ZeroToMany")]
            [EnumMemberDisplay("ZeroToMany [0..*]")]
            ZeroToMany,

            [EnumMember(Value = "OneToMany")]
            [EnumMemberDisplay("OneToMany [1..*]")]
            OneToMany
        };

        /// <summary>
        /// Specifies the user access mode for SubmodelElement instance.When a Submodel is 
        /// received from another party, if set to Read/Only, then the user shall not change the value.
        /// </summary>
        public enum AccessMode
        {
            [EnumMember(Value = "ReadWrite")]
            ReadWrite,

            [EnumMember(Value = "ReadOnly")]
            ReadOnly
        };

        public static Aas.IQualifier CreateQualifierSmtCardinality(SmtCardinality card)
        {
            return new Aas.Qualifier(
                type: "SMT/Cardinality",
                valueType: DataTypeDefXsd.String,
                kind: QualifierKind.TemplateQualifier,
                semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] {
                        new Aas.Key(KeyTypes.GlobalReference,
                            "https://admin-shell.io/SubmodelTemplates/Cardinality/1/0")
                    }).ToList()),
                value: "" + card);
        }

        public static Aas.IQualifier CreateQualifierSmtAllowedValue(string regex)
        {
            return new Aas.Qualifier(
                type: "SMT/AllowedValue",
                valueType: DataTypeDefXsd.String,
                kind: QualifierKind.TemplateQualifier,
                semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] {
                        new Aas.Key(KeyTypes.GlobalReference,
                            "https://admin-shell.io/SubmodelTemplates/AllowedValue/1/0")
                    }).ToList()),
                value: "" + regex);
        }

        public static Aas.IQualifier CreateQualifierSmtExampleValue(string exampleValue)
        {
            return new Aas.Qualifier(
                type: "SMT/ExampleValue",
                valueType: DataTypeDefXsd.String,
                kind: QualifierKind.TemplateQualifier,
                semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] {
                        new Aas.Key(KeyTypes.GlobalReference,
                            "https://admin-shell.io/SubmodelTemplates/ExampleValue/1/0")
                    }).ToList()),
                value: "" + exampleValue);
        }

        public static Aas.IQualifier CreateQualifierSmtDefaultValue(string defaultValue)
        {
            return new Aas.Qualifier(
                type: "SMT/DefaultValue",
                valueType: DataTypeDefXsd.String,
                kind: QualifierKind.TemplateQualifier,
                semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] {
                        new Aas.Key(KeyTypes.GlobalReference,
                            "https://admin-shell.io/SubmodelTemplates/DefaultValue/1/0")
                    }).ToList()),
                value: "" + defaultValue);
        }

        public static Aas.IQualifier CreateQualifierSmtEitherOr(string equivalencyClass)
        {
            return new Aas.Qualifier(
                type: "SMT/EitherOr",
                valueType: DataTypeDefXsd.String,
                kind: QualifierKind.TemplateQualifier,
                semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] {
                        new Aas.Key(KeyTypes.GlobalReference,
                            "https://admin-shell.io/SubmodelTemplates/Cardinality/1/0")
                    }).ToList()),
                value: "" + equivalencyClass);
        }

        public static Aas.IQualifier CreateQualifierSmtRequiredLang(string reqLang)
        {
            return new Aas.Qualifier(
                type: "SMT/RequiredLang",
                valueType: DataTypeDefXsd.String,
                kind: QualifierKind.TemplateQualifier,
                semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] {
                        new Aas.Key(KeyTypes.GlobalReference,
                            "https://admin-shell.io/SubmodelTemplates/RequiredLang/1/0")
                    }).ToList()),
                value: "" + reqLang);
        }

        public static Aas.IQualifier CreateQualifierSmtAccessMode(AccessMode mode)
        {
            return new Aas.Qualifier(
                type: "SMT/AccessMode",
                valueType: DataTypeDefXsd.String,
                kind: QualifierKind.TemplateQualifier,
                semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] {
                        new Aas.Key(KeyTypes.GlobalReference,
                            "https://admin-shell.io/SubmodelTemplates/AccessMode/1/0")
                    }).ToList()),
                value: "" + mode);
        }

        public static Aas.IQualifier[] AllSmtQualifiers =
        {
            CreateQualifierSmtCardinality(SmtCardinality.One),
            CreateQualifierSmtAllowedValue(""),
            CreateQualifierSmtExampleValue(""),
            CreateQualifierSmtDefaultValue(""),
            CreateQualifierSmtEitherOr(""),
            CreateQualifierSmtRequiredLang(""),
            CreateQualifierSmtAccessMode(AccessMode.ReadWrite)
        };

        /// <summary>
        /// Find either <c>type</c> or <c>semanticId</c> and returns the link
        /// to a STATIC IQualifier (not to be changed!).
        /// </summary>
        public static Aas.IQualifier FindQualifierTypeInst(
            string type, Aas.IReference semanticId, bool relaxed = true)
        {
            // at best: tries to find semanticId
            Aas.IQualifier res = null;
            foreach (var qti in AllSmtQualifiers)
                if (semanticId?.IsValid() == true && semanticId.Matches(qti.SemanticId, MatchMode.Relaxed))
                    res = qti;

            // do a more sloppy name comparison?
            if (relaxed && res == null && type?.HasContent() == true)
            {
                // some adoptions ahead
                type = type.Trim().ToLower();
                if (type == "cardinality")
                    type = "smt/cardinality";
                if (type == "multiplicity")
                    type = "smt/cardinality";

                // now try to find
                foreach (var qti in AllSmtQualifiers)
                    if (qti.Type?.HasContent() == true
                        && qti.Type.Trim().ToLower() == type)
                        res = qti;
            }

            // okay?
            return res;
        }

        /// <summary>
        /// Ask for different Qualifier names for cardinality and give a match back.
        /// </summary>
        public static Aas.IQualifier FindSmtCardinalityQualfier(IEnumerable<IQualifier> qualifiers)
        {
            // TODO (MIHO, 2024-02-20): In future, check semanticIds as well?

            return qualifiers.FindQualifierOfAnyType(new[] {
                "SMT/Cardinality", "Cardinality", "Multiplicity" })?.FirstOrDefault();
        }

        /// <summary>
        /// Stringify cardinality
        /// </summary>
        /// <param name="format">0 = ZeroToOne, 1 = ZeroToOne [0..1], 2 = [0..1], 3 = 0..1</param>
        /// <param name="oneIsEmpty">In case of <c>card == 1</c> return ""</param>
        public static string CardinalityToString(SmtCardinality card, int format = 0,
            bool oneIsEmpty = false)
        {
            // Note: after long thinking, MIHO decided to do it the stupid, reliable way ..

#if __way_to_complicated
            multiStr = AdminShellEnumHelper.GetEnumMemberDisplay(typeof(SmtCardinality), card);
            var p = multiStr.IndexOf('[');
            if (p >= 0)
                multiStr = multiStr.Substring(p).Trim('[', ']');
#else
            switch (card) 
            {
                case SmtCardinality.ZeroToOne:
                    switch (format)
                    {
                        case 1:
                            return "ZeroToOne [0..1]";
                        case 2:
                            return "[0..1]";
                        case 3:
                            return "0..1";
                        case 0:
                            return "ZeroToOne";
                        default:
                            throw new NotImplementedException("SmtCardinality/format unknown!");
                    }

                case SmtCardinality.ZeroToMany:
                    switch (format)
                    {
                        case 1:
                            return "ZeroToMany [0..*]";
                        case 2:
                            return "[0..*]";
                        case 3:
                            return "0..*";
                        case 0:
                            return "ZeroToMany";
                        default:
                            throw new NotImplementedException("SmtCardinality/format unknown!");
                    }

                case SmtCardinality.OneToMany:
                    switch (format)
                    {
                        case 1:
                            return "OneToMany [1..*]";
                        case 2:
                            return "[1..*]";
                        case 3:
                            return "1..*";
                        case 0:
                            return "OneToMany";
                        default:
                            throw new NotImplementedException("SmtCardinality/format unknown!");
                    }

                case SmtCardinality.One:
                    if (oneIsEmpty)
                        return "";
                    switch (format)
                    {
                        case 1:
                            return "One [1]";
                        case 2:
                            return "[1]";
                        case 3:
                            return "1";
                        case 0:
                            return "One";
                        default:
                            throw new NotImplementedException("SmtCardinality/format unknown!");
                    }

                default:
                    throw new NotImplementedException("SmtCardinality unknown!");
            }
#endif
        }
    }
}
