/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aas = AasCore.Aas3_0;

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// AAS cardinality for predefined concepts
    /// </summary>
    public enum AasxPredefinedCardinality { ZeroToOne = 0, One, ZeroToMany, OneToMany };

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class AasConceptAttribute : Attribute
    {
        public string Cd { get; set; }
        public AasxPredefinedCardinality Card { get; set; }

        public AasConceptAttribute()
        {
        }
    }

    /// <summary>
    /// This class provides methods to derive a set of C# class definitions from a SMZT definition and
    /// to create runtime instances from it based on a concrete SM.
    /// </summary>
    public class PredefinedConceptsClassMapper
	{
        //
        // Export C# classes
        //

		private static void ExportCSharpMapperSingleItems(
            string indent, Aas.Environment env, Aas.IReferable rf, System.IO.StreamWriter snippets,
            bool noEmptyLineFirst = false)
        {
			// access
			if (snippets == null || env == null || rf == null)
				return;

            //
            // require CD
            //

            Aas.IConceptDescription cd = null;
            if (rf is Aas.IHasSemantics ihs)
			    cd = env.FindConceptDescriptionByReference(ihs.SemanticId);

            var cdff = AdminShellUtil.FilterFriendlyName(cd?.IdShort, pascalCase: true);

			var cdRef = cd?.GetCdReference()?.ToStringExtended(format: 2);

            //
            // pretty idShort
            //

			var idsff = AdminShellUtil.FilterFriendlyName(rf.IdShort, pascalCase: true);
            if (idsff.HasContent() != true)
                return;

            //
            // check Qualifiers/ Extensions
            //

            FormMultiplicity card = FormMultiplicity.One;
            if (rf is Aas.IQualifiable iqf)
            {
                var tst = AasFormUtils.GetCardinality(iqf.Qualifiers);
                if (tst.HasValue)
                    card = tst.Value;
            }

            var cardSt = card.ToString();

            //
            // lambda for attribute declaration
            //

            Action<string, bool, string> declareLambda = (dt, isScalar, instance) =>
            {
                // empty line ahead
                if (!noEmptyLineFirst)
                    snippets.WriteLine();

                // write attribute's attribute                
                if (cdRef?.HasContent() == true)
                    snippets.WriteLine($"{indent}[AasConcept(Cd = \"{cdRef}\", " +
                        $"Card = AasxPredefinedCardinality.{cardSt})]");

                // write attribute itself
                if (isScalar)
                {
                    var nullOp = (dt == "string") ? "" : "?";

                    if (card == FormMultiplicity.ZeroToOne)
                        snippets.WriteLine($"{indent}{dt}{nullOp} {instance};");
                    else
                    if (card == FormMultiplicity.One)
                        snippets.WriteLine($"{indent}{dt} {instance};");
                    else
                        snippets.WriteLine($"{indent}List<{dt}> {instance} = new List<{dt}>();");
                }
                else
                {
                    if (card == FormMultiplicity.ZeroToOne)
                        snippets.WriteLine($"{indent}{dt} {instance} = null;");
                    else
                    if (card == FormMultiplicity.One)
                        snippets.WriteLine($"{indent}{dt} {instance} = new {dt}();");
                    else
                        snippets.WriteLine($"{indent}List<{dt}> {instance} = new List<{dt}>();");
                }
            };

            //
            // Property
            //

            if (rf is Aas.Property prop)
            {
                var dt = "string";
                switch (prop.ValueType)
                {
                    case Aas.DataTypeDefXsd.Boolean:
                        dt = "bool";
                        break;

                    case Aas.DataTypeDefXsd.Byte:
                        dt = "byte";
                        break;

                    case Aas.DataTypeDefXsd.Date:
                    case Aas.DataTypeDefXsd.DateTime:
                    case Aas.DataTypeDefXsd.Time:
                        dt = "DateTime";
                        break;

                    case Aas.DataTypeDefXsd.Float:
                        dt = "float";
                        break;

                    case Aas.DataTypeDefXsd.Double:
                        dt = "double";
                        break;

                    case Aas.DataTypeDefXsd.Int:
                    case Aas.DataTypeDefXsd.Integer:
                        dt = "int";
                        break;

                    case Aas.DataTypeDefXsd.Long:
                        dt = "long";
                        break;

                    case Aas.DataTypeDefXsd.NegativeInteger:
                    case Aas.DataTypeDefXsd.NonPositiveInteger:
                        dt = "int";
                        break;

                    case Aas.DataTypeDefXsd.NonNegativeInteger:
                    case Aas.DataTypeDefXsd.PositiveInteger:
                        dt = "unsigned int";
                        break;

                    case Aas.DataTypeDefXsd.Short:
                        dt = "short";
                        break;

                    case Aas.DataTypeDefXsd.UnsignedByte:
                        dt = "unsigned byte";
                        break;

                    case Aas.DataTypeDefXsd.UnsignedInt:
                        dt = "unsigned int";
                        break;

                    case Aas.DataTypeDefXsd.UnsignedLong:
                        dt = "usingned long";
                        break;

                    case Aas.DataTypeDefXsd.UnsignedShort:
                        dt = "unsigned short";
                        break;
                }

                declareLambda(dt, true, idsff);
            }

            if ((  rf is Aas.Submodel
                || rf is Aas.SubmodelElementCollection
                || rf is Aas.SubmodelElementList)
                && cdRef?.HasContent() == true)
            {
                // in sequence: class first
#if do_not_do
                snippets.WriteLine($"{indent}public class {cdff} {{");

                foreach (var x in rf.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                        ExportCSharpMapper("" + indent + "    ", env, sme, snippets);

				snippets.WriteLine($"{indent}}}");
#endif

                declareLambda($"CD_{cdff}", false, idsff);
			}
		}

        /// <summary>
        /// The contents of this class are based on one or multiple SMCs (SML..), however
        /// the class itself is associated with the associated CD of the SMC (SML..), therefore
        /// it is intended to aggregate member definitions.
        /// Duplicate members are avoided. Members are found to be duplicate, if IdShort and
        /// SemanticId are the same.
        /// </summary>
        private class ExportCSharpClassDef
        {
            /// <summary>
            /// The respective SM, SMC, SML ..
            /// </summary>
            public Aas.IReferable Rf = null;

            /// <summary>
            /// The associated CD.
            /// </summary>
            public Aas.IConceptDescription Cd = null;

            /// <summary>
            /// Superset of representative memebers.
            /// </summary>
            public List<Aas.ISubmodelElement> Members = new List<Aas.ISubmodelElement>();

			public ExportCSharpClassDef(Aas.Environment env, Aas.IReferable rf)
			{
				Rf = rf;
                if (rf is Aas.IHasSemantics ihs)
                    Cd = env?.FindConceptDescriptionByReference(ihs.SemanticId);

                if (rf == null)
                    return;

                foreach (var x in rf.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                        Members.Add(sme);
			}

            public void EnrichMembersFrom(ExportCSharpClassDef cld)
            {
                if (cld?.Members == null)
                    return;

				foreach (var x in cld.Members)
					if (x is Aas.ISubmodelElement sme)
                    {
                        // check if member with same name and CD is already present
                        var found = false;
                        foreach (var em in Members)
                            if (em?.IdShort?.HasContent() == true
                                && em.IdShort == sme?.IdShort
                                && (em.SemanticId?.IsValid() != true 
                                    || em.SemanticId?.Matches(sme?.SemanticId, MatchMode.Relaxed) == true))
                                found = true;
                        if (!found)
                            Members.Add(sme);
                    }
			}
		}

		private static void ExportCSharpMapperOnlyClasses(
	        string indent, Aas.Environment env, Aas.ISubmodel sm, System.IO.StreamWriter snippets)
		{
			// which is a structual element?
			Func<Aas.IReferable, bool> isStructRf = (rf) =>
				(rf is Aas.SubmodelElementCollection
				|| rf is Aas.SubmodelElementList);

            // list of class definitions (not merged, yet)
            var elems = new List<ExportCSharpClassDef>();
            foreach (var sme in sm.SubmodelElements?.FindDeep<Aas.ISubmodelElement>((sme) => isStructRf(sme)))
				elems.Add(new ExportCSharpClassDef(env, sme));

			// list of merged class defs
			var distElems = new List<ExportCSharpClassDef>();
            foreach (var x in elems.GroupBy((cld) => cld.Cd))
            {
                var l = x.ToList();
                for (int i = 1; i < l.Count; i++)
                    l[0].EnrichMembersFrom(l[i]);
                distElems.Add(l[0]);
            }

			// add Submodel at last, to be sure it is distinct
			distElems.Add(new ExportCSharpClassDef(env, sm));
            // distElems.Reverse();

            // try to output classed, do not recurse by itself
			foreach (var cld in distElems)
			{
				// gather infos
				var cdff = AdminShellUtil.FilterFriendlyName(cld.Cd?.IdShort, pascalCase: true);
                var cdRef = cld?.Cd?.GetCdReference()?.ToStringExtended(format: 2);

                // no empty class
                if (cdff?.HasContent() != true)
                    continue;

                // write out class
                snippets.WriteLine();

                if (cdRef?.HasContent() == true)
                    snippets.WriteLine($"{indent}[AasConcept(Cd = \"{cdRef}\")]");

                snippets.WriteLine($"{indent}public class CD_{cdff}");
                snippets.WriteLine($"{indent}{{");

                if (cdff == "Htv_headers")
                {
                    ;
                }

                if (cld.Members != null)
                {
                    var noEmptyLineFirst = true;
                    foreach (var x in cld.Members)
                        if (x is Aas.ISubmodelElement sme)
                        {
                            ExportCSharpMapperSingleItems("" + indent + "    ", env, sme, snippets,
                                noEmptyLineFirst: noEmptyLineFirst);
                            noEmptyLineFirst = false;
                        }
                }


				snippets.WriteLine($"{indent}}}");
			}
		}

		public static void ExportCSharpClassDefs(Aas.Environment env, Aas.ISubmodel sm, System.IO.StreamWriter snippets)
        {
            // access
            if (snippets == null || env == null || sm == null)
                return;

            var head = AdminShellUtil.CleanHereStringWithNewlines(
				@"
                /*
                Copyright (c) 2018-2023 Festo SE & Co. KG
                <https://www.festo.com/net/de_de/Forms/web/contact_international>
                Author: Michael Hoffmeister

                This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

                This source code may use other Open Source software components (see LICENSE.txt).

                This source code was auto-generated by the AASX Package Explorer.
                */

                using AasxIntegrationBase;
                using AdminShellNS;
                using Extensions;
                using System;
                using System.Collections.Generic;
                using Aas = AasCore.Aas3_0;");
            snippets.WriteLine(head);
            snippets.WriteLine("");

			snippets.WriteLine($"namespace AasxPredefinedConcepts.{AdminShellUtil.FilterFriendlyName(sm?.IdShort)} {{");

			ExportCSharpMapperOnlyClasses("    ", env, sm, snippets);
			
            // ExportCSharpMapperSingleItems("    ", env, sm, snippets);

            snippets.WriteLine($"}}");
		}

        //
        // Parse AASX structures
        //

        private class ElemAttrInfo
        {
            public FieldInfo Fi;
            public AasConceptAttribute Attr;
        }

        private void ParseAasElemFillData(ElemAttrInfo eai, Aas.ISubmodelElement sme)
        {
            // access
            if (eai?.Fi == null || sme == null)
                return;

            // straight?

        }

        /// <summary>
        /// Parse information from the AAS elements (within) <c>root</c> to the 
        /// attributed class referenced by <c>obj</c>. Reflection dictates the
        /// recursion into sub-classes.
        /// </summary>
        public static void ParseAasElemsToObject(Aas.IReferable root, object obj)
        {
            // access
            if (root == null || obj == null)
                return;

            // collect information driven by reflection
            var eais = new List<ElemAttrInfo>();

            // find fields for this object
            var t = obj.GetType();
            var l = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in l)
            {
                var a = f.GetCustomAttribute<AasConceptAttribute>();
                if (a != null)
                {
                    eais.Add(new ElemAttrInfo() { Fi = f, Attr = a });
                }
            }

            // now try to fill information
            foreach (var eai in eais)
            {
                // try find sme in Rf
                foreach (var x in root.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                        if (sme?.SemanticId?.MatchesExactlyOneKey(new Aas.Key(Aas.KeyTypes.GlobalReference, eai?.Attr?.Cd)) == true)
                            ;
            }
        }
    }
}