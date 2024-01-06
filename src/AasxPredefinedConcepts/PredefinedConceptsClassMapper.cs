/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Aas3_0;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Extensions;
using Namotion.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
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
        public string SupplSemId { get; set; }
        
        public AasxPredefinedCardinality Card { get; set; }

        public AasConceptAttribute()
        {
        }
    }

    /// <summary>
    /// This class is used in auto-generated files by <c>AasxPredefinedConcepts.PredefinedConceptsClassMapper</c>
    /// </summary>
    public class AasClassMapperRange<T>
    {
        public T Min;
        public T Max;
    }

    /// <summary>
    /// If this class is present in an mapped class, then (source) information will be added to the
    /// data objects.
    /// </summary>
    public class AasClassMapperInfo
    {
        public Aas.IReferable Referable;
    }

    /// <summary>
    /// This class provides methods to derive a set of C# class definitions from a SMT definition and
    /// to create runtime instances from it based on a concrete SM.
    /// </summary>
    public class PredefinedConceptsClassMapper
	{
        //
        // Export C# classes
        //

        private static string CSharpTypeFrom(Aas.DataTypeDefXsd valueType)
        {
            switch (valueType)
            {
                case Aas.DataTypeDefXsd.Boolean:
                    return "bool";

                case Aas.DataTypeDefXsd.Byte:
                    return "byte";

                case Aas.DataTypeDefXsd.Date:
                case Aas.DataTypeDefXsd.DateTime:
                case Aas.DataTypeDefXsd.Time:
                    return "DateTime";

                case Aas.DataTypeDefXsd.Float:
                    return "float";

                case Aas.DataTypeDefXsd.Double:
                    return "double";

                case Aas.DataTypeDefXsd.Int:
                case Aas.DataTypeDefXsd.Integer:
                    return "int";

                case Aas.DataTypeDefXsd.Long:
                    return "long";

                case Aas.DataTypeDefXsd.NegativeInteger:
                case Aas.DataTypeDefXsd.NonPositiveInteger:
                    return "int";

                case Aas.DataTypeDefXsd.NonNegativeInteger:
                case Aas.DataTypeDefXsd.PositiveInteger:
                    return "unsigned int";

                case Aas.DataTypeDefXsd.Short:
                    return "short";

                case Aas.DataTypeDefXsd.UnsignedByte:
                    return "unsigned byte";

                case Aas.DataTypeDefXsd.UnsignedInt:
                    return "unsigned int";

                case Aas.DataTypeDefXsd.UnsignedLong:
                    return "usingned long";

                case Aas.DataTypeDefXsd.UnsignedShort:
                    return "unsigned short";
            }
            
            return "string";
        }

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
                        snippets.WriteLine($"{indent}public {dt}{nullOp} {instance};");
                    else
                    if (card == FormMultiplicity.One)
                        snippets.WriteLine($"{indent}public {dt} {instance};");
                    else
                        snippets.WriteLine($"{indent}public List<{dt}> {instance} = new List<{dt}>();");
                }
                else
                {
                    if (card == FormMultiplicity.ZeroToOne)
                        snippets.WriteLine($"{indent}public {dt} {instance} = null;");
                    else
                    if (card == FormMultiplicity.One)
                        snippets.WriteLine($"{indent}public {dt} {instance} = new {dt}();");
                    else
                        snippets.WriteLine($"{indent}public List<{dt}> {instance} = new List<{dt}>();");
                }
            };

            //
            // Property
            //

            if (rf is Aas.Property prop)
            {
                var dt = CSharpTypeFrom(prop.ValueType);
                declareLambda(dt, true, idsff);
            }

            //
            // Range
            //

            if (rf is Aas.Range rng)
            {
                var dt = CSharpTypeFrom(rng.ValueType);
                declareLambda($"AasClassMapperRange<{dt}>", false, idsff);
            }

            //
            // MultiLanguageProperty
            //

            if (rf is MultiLanguageProperty mlp)
            {
                declareLambda("List<ILangStringTextType>", false, idsff);
            }

            //
            // SMC, SML ..
            //

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
	        string indent, Aas.Environment env, Aas.ISubmodel sm, System.IO.StreamWriter snippets,
            bool addInfoObj = false)
		{
            // list of class definitions (not merged, yet)
            var elems = new List<ExportCSharpClassDef>();
            foreach (var sme in sm.SubmodelElements?.FindDeep<Aas.ISubmodelElement>((sme) => sme.IsStructured()))
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

                if (addInfoObj)
                {
                    snippets.WriteLine($"");
                    snippets.WriteLine($"{indent}    // auto-generated informations");
                    snippets.WriteLine($"{indent}    public AasClassMapperInfo __Info__ = null;");
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

			ExportCSharpMapperOnlyClasses("    ", env, sm, snippets,
                addInfoObj: true);
			
            // ExportCSharpMapperSingleItems("    ", env, sm, snippets);

            snippets.WriteLine($"}}");
		}

        //
        // Parse AASX structures
        //

        private class ElemAttrInfo
        {
            public object Obj;
            public FieldInfo Fi;
            public AasConceptAttribute Attr;
        }

        private static object CreateRangeObjectSpecific(Type genericType0, Aas.ISubmodelElement sme)
        {
            // access
            if (genericType0 == null || sme == null || !(sme is Aas.IRange rng))
                return null;

            // create generic instance
            // see: https://stackoverflow.com/questions/4194033/adding-items-to-listt-using-reflection
            var objTyp = genericType0;
            var IListRef = typeof(AasClassMapperRange<>);
            Type[] IListParam = { objTyp };
            object rngObj = Activator.CreateInstance(IListRef.MakeGenericType(IListParam));

            // set
            var rngType = rngObj.GetType();
            AdminShellUtil.SetFieldLazyValue(rngType.GetField("Min"), rngObj, "" + rng.Min);
            AdminShellUtil.SetFieldLazyValue(rngType.GetField("Max"), rngObj, "" + rng.Max);

            // ok
            return rngObj;
        }

        // TODO (MIHO, 2024-01-04): Move to AdminShellUtil ..
        private static void SetFieldLazyFromSme(FieldInfo f, object obj, Aas.ISubmodelElement sme)
        {
            // access
            if (f == null || obj == null || sme == null)
                return;

            // identify type
            var t = AdminShellUtil.GetTypeOrUnderlyingType(f.FieldType);

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(AasClassMapperRange<>)
                && t.GenericTypeArguments.Length >= 1
                && sme is Aas.IRange rng)
            {
                // create generic instance
                var rngObj = CreateRangeObjectSpecific(t.GenericTypeArguments[0], sme);

                // set it
                f.SetValue(obj, rngObj);
            }
            else
            {
                AdminShellUtil.SetFieldLazyValue(f, obj, sme.ValueAsText());
            }
        }

        public static void AddToListLazySme(FieldInfo f, object obj, Aas.ISubmodelElement sme)
        {
            // access
            if (f == null || obj == null || sme == null)
                return;

            // identify type
            var t = AdminShellUtil.GetTypeOrUnderlyingType(f.FieldType);

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(AasClassMapperRange<>)
                && sme is Aas.IRange rng)
            {
                // create generic instance
                var rngObj = CreateRangeObjectSpecific(t.GenericTypeArguments[0], sme);

                // add it
                var listObj = f.GetValue(obj);
                listObj.GetType().GetMethod("Add").Invoke(listObj, new[] { rngObj });
            }
            else
            {
                AdminShellUtil.AddToListLazyValue(obj, sme.ValueAsText());
            }
        }

        private static void ParseAasElemFillData(ElemAttrInfo eai, Aas.ISubmodelElement sme)
        {
            // access
            if (eai?.Fi == null || eai.Attr == null || sme == null)
                return;

            if (sme?.IdShort == "Voltage_L1_N")
            { ; }
           
            // straight?
            if (!sme.IsStructured())
            {
                if (eai.Attr.Card == AasxPredefinedCardinality.One)
                {
                    // scalar value
                    SetFieldLazyFromSme(eai.Fi, eai.Obj, sme);
                }
                else
                if (eai.Attr.Card == AasxPredefinedCardinality.ZeroToOne)
                {
                    // sure to have a nullable type
                    SetFieldLazyFromSme(eai.Fi, eai.Obj, sme);
                }
                else
                if ((eai.Attr.Card == AasxPredefinedCardinality.ZeroToMany
                    || eai.Attr.Card == AasxPredefinedCardinality.OneToMany)
                    && eai.Obj.GetType().IsGenericType
                    && eai.Obj.GetType().GetGenericTypeDefinition() == typeof(List<>))
                {
                    // sure to have a (instantiated) List<scalar>
                    AddToListLazySme(eai.Fi, eai.Obj, sme);
                }
            }
            else
            {
                if (eai.Attr.Card == AasxPredefinedCardinality.One)
                {
                    // assume a already existing object
                    var childObj = eai.Fi.GetValue(eai.Obj);

                    // recurse to fill in
                    ParseAasElemsToObject(sme, childObj);
                }
                else
                if (eai.Attr.Card == AasxPredefinedCardinality.ZeroToOne)
                {
                    // get value first, shall not be present
                    var childObj = eai.Fi.GetValue(eai.Obj);
                    if (childObj != null)
                        throw new Exception(
                            $"ParseAasElemFillData: [0..1] instance for {eai.Fi.FieldType.Name}> already present!");

                    // ok, make new, add
                    childObj = Activator.CreateInstance(eai.Fi.FieldType);
                    eai.Fi.SetValue(eai.Obj, childObj);

                    // recurse to fill in
                    ParseAasElemsToObject(sme, childObj);
                }
                else
                if ((eai.Attr.Card == AasxPredefinedCardinality.ZeroToMany
                    || eai.Attr.Card == AasxPredefinedCardinality.OneToMany)
                    && eai.Fi.FieldType.IsGenericType
                    && eai.Fi.FieldType.GetGenericTypeDefinition() == typeof(List<>)
                    && eai.Fi.FieldType.GenericTypeArguments.Length > 0
                    && eai.Fi.FieldType.GenericTypeArguments[0] != null)
                {
                    // create a new object instance
                    var childObj = Activator.CreateInstance(eai.Fi.FieldType.GenericTypeArguments[0]);

                    // add to list
                    var listObj = eai.Fi.GetValue(eai.Obj);
                    listObj.GetType().GetMethod("Add").Invoke(listObj, new [] { childObj });

                    // recurse to fill in
                    ParseAasElemsToObject(sme, childObj);
                }

                // recurse

            }
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
            var l = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in l)
            {
                // special case
                if (f.FieldType == typeof(AasClassMapperInfo))
                {
                    var info = new AasClassMapperInfo() { Referable = root };
                    f.SetValue(obj, info);
                    continue;
                }

                // store for a bit later processing
                var a = f.GetCustomAttribute<AasConceptAttribute>();
                if (a != null)
                {
                    eais.Add(new ElemAttrInfo() { Obj = obj, Fi = f, Attr = a });
                }
            }

            // now try to fill information
            foreach (var eai in eais)
            {
                // try find sme in Rf
                foreach (var x in root.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                    {
                        var hit = sme?.SemanticId?.MatchesExactlyOneKey(
                            new Aas.Key(Aas.KeyTypes.GlobalReference, eai?.Attr?.Cd),
                            matchMode: MatchMode.Relaxed) == true;

                        if (hit && eai?.Attr?.SupplSemId?.HasContent() == true)
                            hit = hit && sme?.SupplementalSemanticIds?.MatchesAnyWithExactlyOneKey(
                                new Aas.Key(Aas.KeyTypes.GlobalReference, eai?.Attr?.SupplSemId),
                            matchMode: MatchMode.Relaxed) == true;

                        if (hit)
                            ParseAasElemFillData(eai, sme);
                    }

            }
        }
    }
}