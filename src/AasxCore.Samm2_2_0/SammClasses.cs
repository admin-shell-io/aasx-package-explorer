/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections;
using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;

// Note: Nice regex for searching prefix in .ttl files:
// @prefix\s+([^:]*)\:\s+<([^>]+)>.

namespace AasCore.Samm2_2_0
{

	/// <summary>
	/// LangString similar to AasCore
	/// </summary>
	public class LangString
	{
		/// <summary>
		/// Language, lower case code, accoding to BCP 47 or ISO 639-1
		/// </summary>
		public string? Language { get; set; }

		/// <summary>
		/// Text in this language.
		/// </summary>
		public string? Text { get; set; }

		public LangString() { }

		public LangString(string language, string text)
		{
			Language = language;
			Text = text;
		}

		public LangString(Aas.ILangStringTextType ls)
		{
			Language = ls.Language;
			Text = ls.Text;
		}
	}

	/// <summary>
	/// This attribute gives a list of given presets to an field or property.
	/// in order to avoid cycles
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
	public class SammPresetListAttribute : System.Attribute
	{
		public string PresetListName = "";

		public SammPresetListAttribute(string presetListName)
		{
			if (presetListName != null)
				PresetListName = presetListName;
		}
	}

	/// <summary>
	/// This attribute marks a string field/ property as multiline.
	/// in order to avoid cycles
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
	public class SammMultiLineAttribute : System.Attribute
	{
		public int? MaxLines = null;

		public SammMultiLineAttribute(int maxLines = -1)
		{
			if (maxLines > 0)
				MaxLines = maxLines;
		}
	}

	/// <summary>
	/// This attribute identifies the uri of a single property within a class.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
	public class SammPropertyUriAttribute : System.Attribute
	{
		public string Uri = "";
		public SammVersion Version = SammVersion.V_1_0_0;

		public SammPropertyUriAttribute(string uri, SammVersion version)
		{
			Uri = uri;
			Version = version;
		}
	}

	/// <summary>
	/// If a RDF collection is parsed, this attribute identifies the uri of 
	/// the relationship pointing to the content node.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
	public class SammCollectionContentUriAttribute : System.Attribute
	{
		public string Uri = "";
		public SammVersion Version = SammVersion.V_1_0_0;

		public SammCollectionContentUriAttribute(string uri, SammVersion version)
		{
			Uri = uri;
			Version = version;
		}
	}

	/// <summary>
	/// Some string based flags to attach to the property
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
	public class SammPropertyFlagsAttribute : System.Attribute
	{
		public string Flags = "";

		public SammPropertyFlagsAttribute(string flags)
		{
			Flags = flags;
		}
	}

	/// <summary>
	/// Enum string representation need a namespace prefix to be serialized to Turtle
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
	public class SammPropertyPrefixAttribute : System.Attribute
	{
		public string Prefix = "";
		public SammVersion Version = SammVersion.V_1_0_0;

		public SammPropertyPrefixAttribute(string prefix, SammVersion version)
		{
			Prefix = prefix;
			Version = version;
		}
	}

	/// <summary>
	/// Shall be implemented by all non-abstract model elements
	/// </summary>
	public interface ISammSelfDescription
	{
		/// <summary>
		/// get short name, which can als be used to distinguish elements.
		/// Exmaple: samm-x
		/// </summary>
		string GetSelfName();

		/// <summary>
		/// Get URN of this element class.
		/// </summary>
		string GetSelfUrn(SammVersion version);
	}

	/// <summary>
	/// Shall be implemented in order to give hints about the
	/// (hierarchical) structuring of elements
	/// </summary>
	public interface ISammStructureModel
	{
		/// <summary>
		/// True, if a top element of a hierarchy
		/// </summary>
		bool IsTopElement();

		/// <summary>
		/// Iterate over all the SAMM elements referenced from this instance
		/// without further recursion (see AasCore).
		/// </summary>
		IEnumerable<SammReference> DescendOnce();
	}

	/// <summary>
	/// SAMM model element; base clase
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/modeling-guidelines.html#attributes-that-all-model-elements-have"/>
	/// </summary>
	public class ModelElement
	{
		// Note:
		// The SAMM meta model details, that every element has a name.
		// For AAS, the name is given by the Id of the ConceptDescription

		/// <summary>
		/// Human readable name in a specific language. This attribute may be defined multiple
		/// times for different languages but only once for a specific language. There should 
		/// be at least one preferredName defined with an "en" language tag.
		/// </summary>
		[SammPropertyUri("bamm:preferredName", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:preferredName", SammVersion.V_2_0_0)]
		public List<LangString>? PreferredName { get; set; } = null;

		// Note: Description is already in the Referable
		///// <summary>
		///// Human readable description in a specific language. This attribute may be defined multiple 
		///// times for different languages but only once for a specific language. There should be at 
		///// least one description defined with an "en" language tag.
		///// </summary>
		//[SammPropertyUri("bamm:description")]
		//public List<LangString>? Description { get; set; } = null;

		/// <summary>
		/// A reference to a related element in an external taxonomy, ontology or other standards document. 
		/// The datatype is xsd:anyURI. This attribute may be defined multiple times.
		/// </summary>
		[SammPropertyUri("bamm:see", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:see", SammVersion.V_2_0_0)]
		[SammPropertyFlags("anyuri")]
		public List<string>? See { get; set; } = null;
	}

	/// <summary>
	/// The system of types used in the Semantic Aspect Meta Model (and subsequently in the models conforming 
	/// to the Semantic Aspect Meta Model) is largely based on a subset of the XML Schema Definition 1.1 
	/// (XSD, [xmlschema11-2]), including the types such as data ranges. In addition to types from XSD, the 
	/// type langString is included as described in the RDF [rdf11] specification; it is used to represent 
	/// strings with an explicit language tag. Using these types allows for example the distinction between 
	/// a plain string and a dateTime string, unlike in JSON. The JSON data of a Property with a certain 
	/// type uses the most convenient corresponding JSON type, i.e. booleans for XSD boolean, number for 
	/// all numeric types, JSON object for Entities and string for everying else.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/datatypes.html#type-hierarchy"/>
	/// </summary>
	public enum SammDataType
	{
		[EnumMember(Value = "xsd:anyURI")]
		AnyUri,

		[EnumMember(Value = "xsd:base64Binary")]
		Base64Binary,

		[EnumMember(Value = "xsd:boolean")]
		Boolean,

		[EnumMember(Value = "xsd:byte")]
		Byte,

		[EnumMember(Value = "xsd:date")]
		Date,

		[EnumMember(Value = "xsd:dateTime")]
		DateTime,

		[EnumMember(Value = "xsd:decimal")]
		Decimal,

		[EnumMember(Value = "xsd:double")]
		Double,

		[EnumMember(Value = "xsd:duration")]
		Duration,

		[EnumMember(Value = "xsd:float")]
		Float,

		[EnumMember(Value = "xsd:gDay")]
		GDay,

		[EnumMember(Value = "xsd:gMonth")]
		GMonth,

		[EnumMember(Value = "xsd:gMonthDay")]
		GMonthDay,

		[EnumMember(Value = "xsd:gYear")]
		GYear,

		[EnumMember(Value = "xsd:gYearMonth")]
		GYearMonth,

		[EnumMember(Value = "xsd:hexBinary")]
		HexBinary,

		[EnumMember(Value = "xsd:int")]
		Int,

		[EnumMember(Value = "xsd:integer")]
		Integer,

		[EnumMember(Value = "xsd:long")]
		Long,

		[EnumMember(Value = "xsd:negativeInteger")]
		NegativeInteger,

		[EnumMember(Value = "xsd:nonNegativeInteger")]
		NonNegativeInteger,

		[EnumMember(Value = "xsd:nonPositiveInteger")]
		NonPositiveInteger,

		[EnumMember(Value = "xsd:positiveInteger")]
		PositiveInteger,

		[EnumMember(Value = "xsd:short")]
		Short,

		[EnumMember(Value = "xsd:string")]
		String,

		[EnumMember(Value = "xsd:time")]
		Time,

		[EnumMember(Value = "xsd:unsignedByte")]
		UnsignedByte,

		[EnumMember(Value = "xsd:unsignedInt")]
		UnsignedInt,

		[EnumMember(Value = "xsd:unsignedLong")]
		UnsignedLong,

		[EnumMember(Value = "xsd:unsignedShort")]
		UnsignedShort,

		[EnumMember(Value = "langString")]
		LangString
	}

	/// <summary>
	/// The AT_LEAST and AT_MOST values for lowerBoundDefinition and upperBoundDefinition define that the 
	/// values for minValue and maxValue are inclusive. The LESS_THAN and GREATER_THAN values for the 
	/// lowerBoundDefinition and upperBoundDefinition define that the values for minValue and maxValue are exclusive.
	/// </summary>
	public enum SammUpperBoundDefinition
	{
		[EnumMember(Value = "AT_MOST")]
		AtMost,
		[EnumMember(Value = "LESS_THAN")]
		LessThan
	}

	/// <summary>
	/// The AT_LEAST and AT_MOST values for lowerBoundDefinition and upperBoundDefinition define that the 
	/// values for minValue and maxValue are inclusive. The LESS_THAN and GREATER_THAN values for the 
	/// lowerBoundDefinition and upperBoundDefinition define that the values for minValue and maxValue are exclusive.
	/// </summary>
	public enum SammLowerBoundDefinition
	{
		[EnumMember(Value = "AT_LEAST")]
		AtLeast,
		[EnumMember(Value = "GREATER_THAN")]
		GreaterThan
	}

	/// <summary>
	/// Allowed encodings for byte streams
	/// </summary>
	public enum SammEncoding
	{
		[EnumMember(Value = "US-ASCII")]
		UsAscii,
		[EnumMember(Value = "ISO-8859-1")]
		Iso8859_1,
		[EnumMember(Value = "UTF-8")]
		Utf8,
		[EnumMember(Value = "UTF-16")]
		Utf16,
		[EnumMember(Value = "UTF-16BE")]
		Utf16BE,
		[EnumMember(Value = "UTF-16LE")]
		Utf16LE
	}

	/// <summary>
	/// This class creates a <c>SammReference</c>, which "feels" like a string.
	/// </summary>
	public class SammReference
	{
		public string Value { get; set; }

		public SammReference(string val = "")
		{
			Value = val;
		}
	}

	/// <summary>
	/// When Properties are used in Aspects and Entities, they can be marked as optional 
	/// (not possible for properties of Abstract Entities). 
	/// This means that a Property’s usage is optional, not the Property itself, which 
	/// would make reusing a Property more difficult.
	/// </summary>
	public class OptionalSammReference : SammReference
	{
		public bool Optional { get; set; } = false;

		public OptionalSammReference(string val = "", bool optional = false)
		{
			Value = val;
			Optional = optional;
		}
	}

	/// <summary>
	/// Single item for <c>NamespaceMap</c>.
	/// </summary>
	public class NamespaceMapItem
	{
		/// <summary>
		/// Prefix of a namespace. 
		/// Format: short sequence of chars. ALWAYS trailing colon (:).
		/// </summary>
		public string Prefix { get; set; } = "";

		/// <summary>
		/// Absolute URI to replace a prefix.
		/// </summary>
		public string Uri { get; set; } = "";

		public NamespaceMapItem() { }
		public NamespaceMapItem(string prefix, string uri)
		{
			Prefix = prefix;
			Uri = uri;
		}
	}

	/// <summary>
	/// This (proprietary) map links prefixes and uris together.
	/// Intended use case is to be embedded into the SAMM aspect
	/// and help resolving complete URIs.
	/// </summary>
	public class NamespaceMap
	{
		/// <summary>
		/// Container of map items.
		/// </summary>
		[JsonIgnore]
		public Dictionary<string, NamespaceMapItem> Map { get; set; } = 
			   new Dictionary<string, NamespaceMapItem>();

		// For JSON
		public NamespaceMapItem[] Items
		{
			get => Map.Keys.Select(k => Map[k]).ToArray();
			set {
				Map.Clear();
				foreach (var v in value)
					AddOrIgnore(v.Prefix, v.Uri);
			}
		}

		public int Count() => Map.Count();

		public NamespaceMapItem this[int index] => Map[Map.Keys.ElementAt(index)];

		public void RemoveAt(int index) => Map.Remove(Map.Keys.ElementAt(index));

		public bool AddOrIgnore(string prefix, string uri)
		{
			if (prefix == null || uri == null)
				return false;
			prefix = prefix.Trim();
			if (!prefix.EndsWith(':'))
				return false;
			if (Map.ContainsKey(prefix))
				return false;
			Map[prefix] = new NamespaceMapItem(prefix, uri);
			return true;
		}

		public string? ExtendUri(string input)
		{
			if (input == null)
				return null;
			var p = input.IndexOf(':');
			if (p < 0)
				return input;
			var ask = input.Substring(0, p) + ':';
			if (!Map.ContainsKey(ask) || (p+1) > input.Length)
				return input;
			var res = Map[ask].Uri + input.Substring(p + 1);
			return res;
		}

		public string? PrefixUri(string input)
		{
			// access
			if (input == null)
				return null;

			// tedious searching
			foreach (var mi in Map.Values)
				if (input.StartsWith(mi.Uri))
				{
					var pf = mi.Prefix;
					return pf + input.Substring(mi.Uri.Length);
				}

			// not found .. give whole back
			return input;
		}

		public bool ContainsPrefix(string prefix) => Map.ContainsKey(prefix);

		public NamespaceMapItem GetFromPrefix(string prefix) => Map[prefix];
	}

	/// <summary>
	/// Base class for other constraints that constrain a Characteristic in some way, e.g., the Range Constraint 
	/// limits the value range for a Property.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#constraint"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Constraint"/>
	/// </summary>
	public class Constraint : ModelElement, ISammSelfDescription
	{
		// self description
		public string GetSelfName() => "samm-constraint";
		public string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Constraint" : "samm-c:Constraint";
	}

	/// <summary>
	/// Restricts a value to a specific language.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#language-constraint"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#LanguageConstraint"/>
	/// </summary>
	public class LanguageConstraint : Constraint, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-language-constraint";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:LanguageConstraint" : "samm-c:LanguageConstraint";

		/// <summary>
		/// An ISO 639-1 [iso639] language code for the language of the value of the constrained Property, 
		/// e.g., "de".
		/// </summary>
		[SammPropertyUri("bamm-c:languageCode", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:languageCode", SammVersion.V_2_0_0)]
		public string? LanguageCode { get; set; }
	}

	/// <summary>
	/// Restricts a value to a specific locale, i.e., a language with additional region information.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#locale-constraint"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#LocaleConstraint"/>
	/// </summary>
	public class LocaleConstraint : Constraint, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-locale-constraint";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:LocaleConstraint" : "samm-c:LocaleConstraint";

		/// <summary>
		/// An IETF BCP 47 language code for the locale of the value of the constrained Property, e.g., "de-DE".
		/// </summary>
		[SammPropertyUri("bamm-c:localeCode", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:localeCode", SammVersion.V_2_0_0)]
		public string? LocaleCode { get; set; }
	}

	/// <summary>
	/// Restricts the value range of a Property. At least one of <c>maxValue</c> or <c>minValue</c> must be present in a 
	/// Range Constraint.
	/// </summary>
	public class RangeConstraint : Constraint, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-range-constraint";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:RangeConstraint" : "samm-c:RangeConstraint";

		/// <summary>
		/// The upper bound of a range.
		/// </summary>
		[SammPropertyUri("bamm-c:maxValue", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:maxValue", SammVersion.V_2_0_0)]
		public string? MaxValue { get; set; }

		/// <summary>
		/// The lower bound of a range.
		/// </summary>
		[SammPropertyUri("bamm-c:minValue", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:minValue", SammVersion.V_2_0_0)]
		public string? MinValue { get; set; }

		/// <summary>
		/// Defines whether the upper bound of a range is inclusive or exclusive. Possible values are 
		/// <c>AT_MOST</c> and <c>LESS_THAN</c>.
		/// </summary>
		[SammPropertyUri("bamm-c:upperBoundDefinition", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:upperBoundDefinition", SammVersion.V_2_0_0)]
		[SammPropertyPrefix("bamm-c:", SammVersion.V_1_0_0)]
		[SammPropertyPrefix("samm-c:", SammVersion.V_2_0_0)]
		public SammUpperBoundDefinition? UpperBoundDefinition { get; set; }

		/// <summary>
		/// Defines whether the lower bound of a range is inclusive or exclusive. Possible values are 
		/// <c>AT_LEAST</c> and <c>GREATER_THAN</c>.
		/// </summary>
		[SammPropertyUri("bamm-c:lowerBoundDefinition", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:lowerBoundDefinition", SammVersion.V_2_0_0)]
		[SammPropertyPrefix("bamm-c:", SammVersion.V_1_0_0)]
		[SammPropertyPrefix("samm-c:", SammVersion.V_2_0_0)]
		public SammLowerBoundDefinition? LowerBoundDefinition { get; set; }
	}

	/// <summary>
	/// Restricts the encoding of a Property.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#encoding-constraint"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#EncodingConstraint"/>
	/// </summary>
	public class EncodingConstraint : Constraint, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-encoding-constraint";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:EncodingConstraint" : "samm-c:EncodingConstraint";

		/// <summary>
		/// Configures the encoding. This must be one of the following: 
		/// <c>US-ASCII</c>, <c>ISO-8859-1</c>, <c>UTF-8</c>, <c>UTF-16</c>, <c>UTF-16BE</c> or <c>UTF-16LE</c>.
		/// </summary>
		[SammPropertyUri("bamm-c:value", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:value", SammVersion.V_2_0_0)]
		[SammPropertyPrefix("bamm-c:", SammVersion.V_1_0_0)]
		[SammPropertyPrefix("samm-c:", SammVersion.V_2_0_0)]
		public SammEncoding? Value { get; set; }
	}

	/// <summary>
	/// This Constraint can be used to restrict two types of Characteristics:
	/// Characteristics that have a string-like value space; in this case the Constraint restricts the length of the 
	/// (string-) value. 
	/// Collection Characteristics (Collection, Set, Sorted Set, List). In this case the Constraint restricts the 
	/// number of elements in the collection.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#length-constraint"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#LengthConstraint"/>
	/// </summary>
	public class LengthConstraint : Constraint, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-length-constraint";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:LengthConstraint" : "samm-c:LengthConstraint";

		/// <summary>
		/// The maximum length. Must be given as xsd:nonNegativeInteger.
		/// </summary>
		[SammPropertyUri("bamm-c:maxValue", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:maxValue", SammVersion.V_2_0_0)]
		public uint? MaxValue { get; set; }

		/// <summary>
		/// The minimum length. Must be given as xsd:nonNegativeInteger.
		/// </summary>
		[SammPropertyUri("bamm-c:minValue", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:minValue", SammVersion.V_2_0_0)]
		public uint? MinValue { get; set; }
	}

	/// <summary>
	/// Restricts a string value to a regular expression as defined by XQuery 1.0 and XPath 2.0 Functions 
	/// and Operators [xpath-functions].
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#regular-expression-constraint"/>
	/// <see href="https://www.w3.org/TR/xpath-functions-3/#regex-syntax"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#RegularExpressionConstraint"/>
	/// </summary>
	public class RegularExpressionConstraint : Constraint, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-regular-expression-constraint";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:RegularExpressionConstraint" : "samm-c:RegularExpressionConstraint";

		/// <summary>
		/// The regular expression.
		/// <see href="https://www.w3.org/TR/xpath-functions-3/#regex-syntax"/>
		/// </summary>
		[SammPropertyUri("bamm:value", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:value", SammVersion.V_2_0_0)]
		public string? Value { get; set; }
	}

	/// <summary>
	/// Defines the scaling factor as well as the amount of integral numbers for a fixed point number. 
	/// The constraint may only be used in conjunction with Characteristics which use the xsd:decimal data type.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#fixed-point-constraint"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#FixedPointConstraint"/>
	/// </summary>
	public class FixedPointConstraint : Constraint, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-fixed-point-constraint";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:FixedPointConstraint" : "samm-c:FixedPointConstraint";

		/// <summary>
		/// The scaling factor for a fixed point number. E.g., if a fixedpoint number is 123.04, the 
		/// scaling factor is 2 (the number of digits after the decimal point). 
		/// Must be given as xsd:positiveInteger.
		/// </summary>
		[SammPropertyUri("bamm-c:scale", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:scale", SammVersion.V_2_0_0)]
		public uint? Scale { get; set; }

		/// <summary>
		/// The number of integral digits for a fixed point number. E.g., if a fixedpoint number 
		/// is 123.04, the integer factor is 3 (the number of digits before the decimal point). 
		/// Must be given as xsd:positiveInteger.
		/// </summary>
		[SammPropertyUri("bamm-c:integer", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:integer", SammVersion.V_2_0_0)]
		public uint? Integer { get; set; }
	}

	/// <summary>
	/// Base class of all characteristics. This Characteristics Class can also be instantiated directly 
	/// (i.e., without creating a subclass).
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#characteristic-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Characteristic"/>
	/// </summary>
	public class Characteristic : ModelElement, ISammSelfDescription, ISammStructureModel
	{
		// self description
		public string GetSelfName() => "samm-characteristic";
		public string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm:Characteristic" : "samm:Characteristic";

		// structure model
		public bool IsTopElement() => false;
		public IEnumerable<SammReference> DescendOnce()
		{
			if (DataType != null)
				yield return DataType;
		}

		/// <summary>
		/// Reference to a scalar or complex (Entity) data type. See Section "Type System" in the Aspect Meta Model.
		/// Also the scalar data types (e.g. xsd:decimal) are treated as references in the first degree.
		/// </summary>	
		[SammPresetList("SammXsdDataTypes")]
		[SammPropertyUri("bamm:dataType", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:dataType", SammVersion.V_2_0_0)]
		public SammReference DataType { get; set; }

		public Characteristic()
		{
			DataType = new SammReference("");
		}
	}

	/// <summary>
	/// The Trait is used to add one or more Constraints to another Characteristic, which is 
	/// referred to as the "base Characteristic". A Trait itself has no samm:dataType, 
	/// because it inherits the type of its samm-c:baseCharacteristic.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#trait-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Trait"/>
	/// </summary>
	public class Trait : Characteristic, ISammSelfDescription, ISammStructureModel
	{
		// self description
		public new string GetSelfName() => "samm-trait";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Trait" : "samm-c:Trait";

		// structure model
		public new bool IsTopElement() => false;
		public new IEnumerable<SammReference> DescendOnce()
		{
			if (BaseCharacteristic != null)
				yield return BaseCharacteristic;
			if (Constraint != null)
				foreach (var cn in Constraint)
					yield return cn;
		}

		/// <summary>
		/// The Characterstic that is being constrained.
		/// Identified via <c>preferredName</c> in any language
		/// </summary>
		[SammPropertyUri("bamm-c:baseCharacteristic", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:baseCharacteristic", SammVersion.V_2_0_0)]
		public SammReference? BaseCharacteristic { get; set; }

		/// <summary>
		/// A Constraint that is applicable to the base Characteristic. This attribute may be used multiple times, 
		/// to add multiple Constraints to the base Characteristic.
		/// </summary>
		[SammPropertyUri("bamm-c:constraint", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:constraint", SammVersion.V_2_0_0)]
		[SammPropertyFlags("constraints")]
		public List<SammReference>? Constraint { get; set; }
	}

	/// <summary>
	/// A value which can be quantified and may have a unit, e.g., the number of bolts required for a 
	/// processing step or the expected torque with which these bolts should be tightened.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#quantifiable-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Quantifiable"/>
	/// </summary>
	public class Quantifiable : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-quantifiable";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Quantifiable" : "samm-c:Quantifiable";

		/// <summary>
		/// Reference to a Unit as defined in the Unit catalog
		/// </summary>
		[SammPropertyUri("bamm-c:unit", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:unit", SammVersion.V_2_0_0)]
		public SammReference? Unit { get; set; }
	}

	/// <summary>
	/// A measurement is a numeric value with an associated unit and quantity kind.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#measurement-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Measurement"/>
	/// </summary>
	public class Measurement : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-measurement";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Measurement" : "samm-c:Measurement";

		/// <summary>
		/// Reference to a Unit as defined in the Unit catalog
		/// </summary>
		[SammPropertyUri("bamm-c:unit", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:unit", SammVersion.V_2_0_0)]
		public SammReference Unit { get; set; }

		public Measurement()
		{
			Unit = new SammReference("");
		}
}

	/// <summary>
	/// An enumeration represents a list of possible values.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#enumeration-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Enumeration"/>
	/// </summary>
	public class Enumeration : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-enumeration";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Enumeration" : "samm-c:Enumeration";

		/// <summary>
		/// List of possible values. The dataType of each of the values must match the 
		/// dataType of the Enumeration.
		/// </summary>
		[SammPropertyUri("bamm-c:values", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:values", SammVersion.V_2_0_0)]
		public List<string> Values { get; set; }

		public Enumeration()
		{
			Values = new List<string>();
		}
	}

	/// <summary>
	/// A state is subclass of Enumeration with a default value.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#state-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#State"/>
	/// </summary>
	public class State : Enumeration, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-state";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:State" : "samm-c:State";

		/// <summary>
		/// The default value for the state.
		/// </summary>
		[SammPropertyUri("bamm-c:defaultValue", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:defaultValue", SammVersion.V_2_0_0)]
		public string DefaultValue { get; set; }

		public State()
		{
			DefaultValue = "";
		}
	}

	/// <summary>
	/// A time duration.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#duration-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Duration"/>
	/// </summary>
	public class Duration : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-duration";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Duration" : "samm-c:Duration";

		/// <summary>
		/// Reference to a Unit as defined in the Unit catalog. The referenced unit or its referenceUnit 
		/// must have the quantityKind unit:time. Currently, the following units can therefore be used: 
		/// unit:centipoisePerBar unit:commonYear unit:day unit:henryPerKiloohm unit:henryPerOhm 
		/// unit:hour unit:kilosecond unit:microhenryPerKiloohm unit:microhenryPerOhm unit:microsecond 
		/// unit:millihenryPerKiloohm unit:millihenryPerOhm unit:millipascalSecondPerBar unit:millisecond 
		/// unit:minuteUnitOfTime unit:month unit:nanosecond unit:pascalSecondPerBar unit:picosecond 
		/// unit:poisePerBar unit:poisePerPascal unit:reciprocalMinute unit:secondUnitOfTime 
		/// unit:shake unit:siderealYear unit:tropicalYear unit:week unit:year
		/// </summary>
		[SammPropertyUri("bamm-c:unit", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:unit", SammVersion.V_2_0_0)]
		public SammReference Unit { get; set; }

		public Duration()
		{
			Unit = new SammReference();
		}
	}

	/// <summary>
	/// A group of values which may be either of a scalar or Entity type. The values may be duplicated and 
	/// are not ordered (i.e., bag semantics).
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#collection-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Collection"/>
	/// </summary>
	public class Collection : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-collection";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Collection" : "samm-c:Collection";

		/// <summary>
		/// Reference to a Characteristic which describes the individual elements contained in the Collection.
		/// </summary>
		[SammPropertyUri("bamm-c:elementCharacteristic", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:elementCharacteristic", SammVersion.V_2_0_0)]
		public SammReference ElementCharacteristic { get; set; }

		public Collection()
		{
			ElementCharacteristic = new SammReference();
		}
	}

	/// <summary>
	/// A subclass of Collection which may contain duplicates and is ordered.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#list-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#List"/>
	/// </summary>
	public class List : Collection, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-list";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:List" : "samm-c:List";
	}

	/// <summary>
	/// A subclass of Collection which may not contain duplicates and is unordered.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#set-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Set"/>
	/// </summary>
	public class Set : Collection, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-set";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Set" : "samm-c:Set";
	}

	/// <summary>
	/// A subclass of Collection which may not contain duplicates and is ordered.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#sorted-set-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#SortedSet"/>
	/// </summary>
	public class SortedSet : Collection, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-sorted-set";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:SortedSet" : "samm-c:SortedSet";
	}

	/// <summary>
	/// A subclass of Sorted Set containing values with the exact point in time when the values where recorded.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#time-series-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#TimeSeries"/>
	/// </summary>
	public class TimeSeries : SortedSet, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-time-series";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:TimeSeries" : "samm-c:TimeSeries";

		// For DataType
		// Set to samm-e:TimeSeriesEntity. This Entity consists of two Properties, namely samm-e:timestamp
		// and samm-e:value.
		// As such the structure for time series data is fixed to a collection of key/value pairs with the
		// timestamp being the key and the value being the value.
		// The samm-e:timestamp property has a fixed Characteristic of samm-c:Timestamp. The Characteristic
		// of the samm-e:value Property is set in the specific Aspect Model giving the value domain specific semantics.	
	}

	/// <summary>
	/// Describes a Property which contains any kind of code. Note that this Characteristic does not 
	/// define a samm:dataType, this must therefore be done when instantiating the Characteristic.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#code-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Code"/>
	/// </summary>
	public class Code : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-code";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Code" : "samm-c:Code";
	}

	/// <summary>
	/// Describes a Property whose value can have one of two possible types (a disjoint union). 
	/// This Characteristic does not have one explicit samm:dataType, as it can be the datatype of either 
	/// the left or the right.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#either-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#Either"/>"/>
	/// </summary>
	public class Either : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-either";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:Either" : "samm-c:Either";

		/// <summary>
		/// The left side of the Either. The attribute references another Characteristic which describes the value.
		/// </summary>
		[SammPropertyUri("bamm-c:left", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:left", SammVersion.V_2_0_0)]
		public string Left { get; set; }

		/// <summary>
		/// The right side of the Either. The attribute references another Characteristic which describes the value.
		/// </summary>
		[SammPropertyUri("bamm-c:right", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:right", SammVersion.V_2_0_0)]
		public string Right { get; set; }

		public Either()
		{
			Left = "";
			Right = "";
		}
	}

	/// <summary>
	/// Describes a Property whose data type is an Entity. The Entity used as data type could be defined in the 
	/// same Aspect Model or the shared Entity namespace of the Semantic Aspect Meta Model.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#single-entity-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#SingleEntity"/>
	/// </summary>
	public class SingleEntity : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-single-entity";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:SingleEntity" : "samm-c:SingleEntity";
	}

	/// <summary>
	/// Describes a Property which contains a scalar string-like value space value with a well-defined structure. 
	/// The Structured Value Characteristic allows the description of the parts of the Property’s value by 
	/// linking to a separate Property definition for each part. To define the parts, the value must be 
	/// deconstructed using a regular expression.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#structured-value-characteristic"/>
	/// <seealso href="urn:bamm:io.openmanufacturing:characteristic:1.0.0#StructuredValue"/>
	/// </summary>
	public class StructuredValue: Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-structured-value";
		public new string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm-c:StructuredValue" : "samm-c:StructuredValue";

		/// <summary>
		/// The regular expression used to deconstruct the value into parts that are mapped to separate 
		/// Properties. This regular expression must contain the same number of capture groups as there 
		/// are Properties given in the elements list. The n​th capture group maps to the n​th Property 
		/// in the elements list.
		/// </summary>
		[SammPropertyUri("bamm-c:deconstructionRule", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:deconstructionRule", SammVersion.V_2_0_0)]
		public string DeconstructionRule { get; set; }

		/// <summary>
		/// A list of entries each of which can either be a Property reference or a string literal. 
		/// The list must contain at least one Property reference.
		/// </summary>
		[SammPropertyUri("bamm-c:elements", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm-c:elements", SammVersion.V_2_0_0)]
		public List<string> Elements { get; set; }

		public StructuredValue()
		{
			DeconstructionRule = "";
			Elements = new List<string>();
		}
	}

	/// <summary>
	/// A Property represents a named value. This element is optional and can appear multiple times in a model ([0..n]). 
	/// One Property has exactly one Characteristic.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/meta-model-elements.html#meta-model-elements"/>
	/// </summary>
	public class Property : ModelElement, ISammSelfDescription, ISammStructureModel
	{
		// self description
		public string GetSelfName() => "samm-property";
		public string GetSelfUrn() => "bamm:Property";
		public string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm:Property" : "samm:Property";

		// structure model
		public bool IsTopElement() => false;
		public IEnumerable<SammReference> DescendOnce()
		{
			if (Characteristic != null)
				yield return Characteristic;
		}

		/// <summary>
		/// This provides an example value for the Property, which requires that the entered data type has been defined 
		/// in a corresponding Characteristic. It is important to ensure that the data type has the correct format. 
		/// Find the Data Types (SAMM 2.1.0) with an example value.
		/// </summary>
		[SammPropertyUri("bamm:exampleValue", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:exampleValue", SammVersion.V_2_0_0)]
		public string? ExampleValue { get; set; }

		/// <summary>
		/// One Property has exactly one Characteristic.
		/// </summary>
		[SammPresetList("Characteristics")]
		[SammPropertyUri("bamm:characteristic", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:characteristic", SammVersion.V_2_0_0)]
		public SammReference Characteristic { get; set; }

		public Property()
		{
			Characteristic = new SammReference("");
		}
	}

	/// <summary>
	/// As defined in the Meta Model Elements, an Entity has a number of Properties. 
	/// </summary>
	public class Entity : ModelElement, ISammSelfDescription, ISammStructureModel
	{
		// self description
		public string GetSelfName() => "samm-entity";
		public string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm:Entity" : "samm:Entity";

		// structure model
		public bool IsTopElement() => false;
		public IEnumerable<SammReference> DescendOnce()
		{
			if (Properties != null)
				foreach (var x in Properties)
					yield return x;
		}

		// own
		[SammPropertyUri("bamm:properties", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:properties", SammVersion.V_2_0_0)]
		[SammCollectionContentUri("bamm:property", SammVersion.V_1_0_0)]
		[SammCollectionContentUri("samm:property", SammVersion.V_2_0_0)]
		public List<OptionalSammReference> Properties { get; set; } 
			   = new List<OptionalSammReference>();
	}

	/// <summary>
	/// An Aspect is the root element of each Aspect Model and has a number of Properties, Events, and Operations. 
	/// This element is mandatory and must appear exactly once per model. 
	/// It has any number of Properties, Operations and Events ([0..n]).
	/// </summary>
	public class Aspect : ModelElement, ISammSelfDescription, ISammStructureModel
	{
		// self description
		public string GetSelfName() => "samm-aspect";
		public string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm:Aspect" : "samm:Aspect";

		// structure model
		public bool IsTopElement() => true;
		public IEnumerable<SammReference> DescendOnce()
		{
			if (Properties != null)
				foreach (var x in Properties)
					yield return x;
		}

		// own

		/// <summary>
		/// The namespaces/ prefix definitions of the SAMM models are attached to the Aspect.
		/// </summary>
		public NamespaceMap Namespaces { get; set; } = new NamespaceMap();

		/// <summary>
		/// Multiline string with comments (for the whole SAMM file).
		/// </summary>
		[SammMultiLine(maxLines: 5)]
		public string Comments { get; set; } = "";

		/// <summary>
		/// A Property represents a named value. This element is optional and can appear 
		/// multiple times in a model ([0..n]). 
		/// </summary>
		[SammPropertyUri("bamm:properties", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:properties", SammVersion.V_2_0_0)]
		[SammCollectionContentUri("bamm:property", SammVersion.V_1_0_0)]
		[SammCollectionContentUri("samm:property", SammVersion.V_2_0_0)]
		public List<OptionalSammReference> Properties { get; set; } = new List<OptionalSammReference>();

		/// <summary>
		/// An Event is a model element that represents a single occurence where the timing is important. 
		/// </summary>
		[SammPropertyUri("bamm:events", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:events", SammVersion.V_2_0_0)]
		[SammCollectionContentUri("bamm:event", SammVersion.V_1_0_0)]
		[SammCollectionContentUri("samm:event", SammVersion.V_2_0_0)]
		public List<OptionalSammReference> Events { get; set; } = new List<OptionalSammReference>();

		/// <summary>
		/// An Operation represents an action that can be triggered on the Aspect. 
		/// </summary>
		[SammPropertyUri("bamm:operations", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:operations", SammVersion.V_2_0_0)]
		[SammCollectionContentUri("bamm:operation", SammVersion.V_1_0_0)]
		[SammCollectionContentUri("samm:operation", SammVersion.V_2_0_0)]
		public List<OptionalSammReference> Operations { get; set; } = new List<OptionalSammReference>();
	}

	public class Unit : ModelElement, ISammSelfDescription
	{
		// self description
		public string GetSelfName() => "samm-unit";
		public string GetSelfUrn(SammVersion v) => (v == SammVersion.V_1_0_0) ? "bamm:Unit" : "samm:Unit";

		/// <summary>
		/// Normalized short code for unit; please refer to the original 
		/// specification for more details.
		/// </summary>
		[SammPropertyUri("bamm:commonCode", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:commonCode", SammVersion.V_2_0_0)]
		public string? CommonCode { get; set; } = null;

		/// <summary>
		/// If the unit is derived from a reference unit, the human readable 
		/// multiplication factor, e.g., "10⁻²⁸ m²"
		/// </summary>
		[SammPropertyUri("bamm:conversionFactor", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:conversionFactor", SammVersion.V_2_0_0)]
		public string? ConversionFactor { get; set; } = null;

		/// <summary>
		/// If the unit is derived from a reference unit, the numeric 
		/// multiplication factor, e.g., "1.0E-28"
		/// </summary>
		[SammPropertyUri("bamm:numericConversionFactor", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:numericConversionFactor", SammVersion.V_2_0_0)]
		public string? NumericConversionFactor { get; set; } = null;

		/// <summary>
		/// The list of quantity kinds, for example unit litre has quantity kind volume, unit 
		/// metre has quantity kinds length, distance, diameter etc.
		/// </summary>
		[SammPropertyUri("bamm:quantityKind", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:quantityKind", SammVersion.V_2_0_0)]
		public string? QuantityKind { get; set; } = null;

		/// <summary>
		/// The unit this unit is derived from, e.g., centimetre is derived from metre.
		/// </summary>
		[SammPropertyUri("bamm:referenceUnit", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:referenceUnit", SammVersion.V_2_0_0)]
		public SammReference? ReferenceUnit { get; set; } = null;

		/// <summary>
		/// The unit’s symbol, e.g., for centimetre the symbol is cm
		/// </summary>
		[SammPropertyUri("bamm:symbol", SammVersion.V_1_0_0)]
		[SammPropertyUri("samm:symbol", SammVersion.V_2_0_0)]
		public string? symbol { get; set; } = null;
	}

	public enum SammVersion { V_1_0_0, V_2_0_0 };

	/// <summary>
	/// This class hold all prefixes/ namespaces, which are "switched" on a version
	/// change.
	/// </summary>
	public class SammIdSet
	{
		/// <summary>
		/// SAMM version, which is to be matched.
		/// "Key" of the record.
		/// </summary>
		public SammVersion Version = SammVersion.V_1_0_0;

		/// <summary>
		/// When matching the version of a turtle file, this key/ value will
		/// be checked for identity.
		/// </summary>
		public NamespaceMapItem Detector = new NamespaceMapItem(
			"bamm:", "urn:bamm:io.openmanufacturing:meta-model:1.0.0#");

		/// <summary>
		/// XSD uri for "a"
		/// </summary>
		public string PredicateA = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

		/// <summary>
		/// XSD datatype of uint
		/// </summary>
		public string XsdNonNegInt = "http://www.w3.org/2001/XMLSchema#nonNegativeInteger";

		/// <summary>
		/// XSD datatype of bool
		/// </summary>
		public string XsdBoolean = "http://www.w3.org/2001/XMLSchema#boolean";

		/// <summary>
		/// XSD datatype of string
		/// </summary>
		public string XsdString = "http://www.w3.org/2001/XMLSchema#string";

		/// <summary>
		/// Accordng to standard of RDF collection, the first element relationship
		/// </summary>
		public string RdfCollFirst = "http://www.w3.org/1999/02/22-rdf-syntax-ns#first";

		/// <summary>
		/// Accordng to standard of RDF collection, the res element relationship
		/// </summary>
		public string RdfCollRest = "http://www.w3.org/1999/02/22-rdf-syntax-ns#rest";

		/// <summary>
		/// Accordng to standard of RDF collection, the end element
		/// </summary>
		public string RdfCollNil = "http://www.w3.org/1999/02/22-rdf-syntax-ns#nil";

		/// <summary>
		/// In RDF collections of SAMM references, the pointer to the content itself
		/// </summary>
		public string RdfCollProperty = "urn:bamm:io.openmanufacturing:meta-model:1.0.0#property";

		/// <summary>
		/// In RDF collections of SAMM references, the optional flag
		/// </summary>
		public string RdfCollOptional = "urn:bamm:io.openmanufacturing:meta-model:1.0.0#optional";

		/// <summary>
		/// The head to be used for auto generated instances
		/// </summary>
		public string DefaultInstanceURN = "https://admin-shell.io/samm-import#";

		/// <summary>
		/// Holds attribute URI name with prefix for the description attribute of a SAMM element.
		/// </summary>
		public string SammDescription = "bamm:description";

		/// <summary>
		/// Holds attribute URI name with prefix for the name attribute of a SAMM element.
		/// </summary>
		public string SammName = "bamm:name";

		/// <summary>
		/// The namespaces/ prefixes used by the own functionality
		/// </summary>
		public NamespaceMap SelfNamespaces = new NamespaceMap();

		public SammIdSet()
		{
			// init namespaces, as being used by self / reflection information
			SelfNamespaces = new NamespaceMap();
			SelfNamespaces.AddOrIgnore("bamm:", "urn:bamm:io.openmanufacturing:meta-model:1.0.0#");
			SelfNamespaces.AddOrIgnore("bamm-c:", "urn:bamm:io.openmanufacturing:characteristic:1.0.0#");
			SelfNamespaces.AddOrIgnore("bamm-e:", "urn:bamm:io.openmanufacturing:entity:1.0.0#");
			SelfNamespaces.AddOrIgnore("unit:", "urn:bamm:io.openmanufacturing:unit:1.0.0#");
			SelfNamespaces.AddOrIgnore("rdf:", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
			SelfNamespaces.AddOrIgnore("rdfs:", "http://www.w3.org/2000/01/rdf-schema#");
			SelfNamespaces.AddOrIgnore("xsd:", "http://www.w3.org/2001/XMLSchema#");
		}

		public Dictionary<string, Type> SammUrnToType = new Dictionary<string, Type>();

		public Dictionary<Type, string> SammTypeToName = new Dictionary<Type, string>();

		public SammIdSet Init()
		{
			// dictionary from URN to type
			foreach (var st in Constants.AddableElements)
			{
				if (Activator.CreateInstance(st, new object[] { }) is ISammSelfDescription ssd)
				{
					// assumption: RDF matching is case sensitive?!
					var fullUri = SelfNamespaces.ExtendUri(ssd.GetSelfUrn(Version));

					if (fullUri != null)
					{
						SammUrnToType.Add(fullUri, st);
						SammTypeToName.Add(st, "" + ssd.GetSelfName());
					}
				}
			}

			return this;
		}

		/// <summary>
		/// This function assumes, that the idSet is on version 1.0.0
		/// with all attributes and wants to be upgraded.
		/// </summary>
		public SammIdSet? SwitchToVersion(SammVersion version)
		{
			// initial version?
			if (version == SammVersion.V_1_0_0)
				return this;

			//
			// 2.0.0
			//

			if (version == SammVersion.V_2_0_0)
			{
				// individual attributes
				Version = SammVersion.V_2_0_0;
				RdfCollProperty = "urn:samm:org.eclipse.esmf.samm:meta-model:2.0.0#property";
				RdfCollOptional = "urn:samm:org.eclipse.esmf.samm:meta-model:2.0.0#optional";

				// init namespaces, as being used by self / reflection information
				SelfNamespaces = new NamespaceMap();
				SelfNamespaces.AddOrIgnore("samm:", "urn:samm:org.eclipse.esmf.samm:meta-model:2.0.0#");
				SelfNamespaces.AddOrIgnore("samm-c:", "urn:samm:org.eclipse.esmf.samm:characteristic:2.0.0#");
				SelfNamespaces.AddOrIgnore("samm-e:", "urn:samm:org.eclipse.esmf.samm:entity:2.0.0#");
				SelfNamespaces.AddOrIgnore("unit:", "urn:samm:org.eclipse.esmf.samm:unit:2.0.0#");
				SelfNamespaces.AddOrIgnore("rdf:", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
				SelfNamespaces.AddOrIgnore("rdfs:", "http://www.w3.org/2000/01/rdf-schema#");
				SelfNamespaces.AddOrIgnore("xsd:", "http://www.w3.org/2001/XMLSchema#");

				Detector = new NamespaceMapItem(
					"samm:", "urn:samm:org.eclipse.esmf.samm:meta-model:2.0.0#");

				// ok
				return this;
			}

			//
			// not found?
			return null;
		}

		public Type? GetTypeFromUrn(string? urn)
		{
			if (urn == null)
				return null;
			if (SammUrnToType.ContainsKey(urn))
				return SammUrnToType[urn];
			return null;
		}

		public string? GetNameFromSammType(Type? sammType)
		{
			if (sammType == null)
				return null;
			if (SammTypeToName.ContainsKey(sammType))
				return SammTypeToName[sammType];
			return null;
		}
	}

	/// <summary>
	/// This class holds predefined id sets for all relevant SAMM versions
	/// </summary>
	public static class SammIdSets
	{
		public static Dictionary<SammVersion, SammIdSet> IdSets
			          = new Dictionary<SammVersion, SammIdSet>();

		public static void AddSet(SammIdSet? set)
		{
			if (set == null)
				return;
			IdSets.Add(set.Version, set);
		}

		public static SammIdSet? GetDefaultVersion()
		{
			return IdSets.Values.LastOrDefault();
		}

		static SammIdSets()
		{
			AddSet(new SammIdSet().Init());
			AddSet(new SammIdSet().SwitchToVersion(SammVersion.V_2_0_0)?.Init());
		}

		public static Tuple<SammIdSet, Type>? GetAnyIdSetTypeFromUrn(string? urn)
		{
			if (urn == null)
				return null;
			foreach (var idset in IdSets.Values)
			{
				var found = idset.GetTypeFromUrn(urn);
				if (found != null)
					return new Tuple<SammIdSet, Type>(idset, found);
			}
			return null;
		}

		public static string? GetAnyNameFromSammType(Type? sammType)
		{
			if (sammType == null)
				return null;
			foreach (var idset in IdSets.Values)
			{
				var found = idset.GetNameFromSammType(sammType);
				if (found != null)
					return found;
			}
			return null;
		}

		public static SammIdSet? DetectVersion(NamespaceMap external)
		{
			// access
			if (external == null)
				return null;

			// loop
			foreach (var idSet in IdSets.Values)
			{
				// need a prefix to compare
				var pf = idSet?.Detector?.Prefix;
				if (pf == null)
					continue;

				// now compare
				if (external.ContainsPrefix(pf) && idSet?.SelfNamespaces.ContainsPrefix(pf) == true
					&& external.GetFromPrefix(pf)?.Uri == idSet.SelfNamespaces.GetFromPrefix(pf)?.Uri)
					return idSet;
			}

			return null;
		}
	}

	/// <summary>
	/// Provides some constant values to the model.
	/// </summary>
	public static class Constants
	{
		/// <summary>
		/// Start of namespace(s) to efficiently recognize a SAMM element.
		/// </summary>
		public static string[] PossibleNamespaceURN = { 
			"urn:samm:org.eclipse.esmf.samm:",
			"urn:bamm:io.openmanufacturing:"
		};

		public static Type[] AddableCharacteristic =
		{ 
			typeof(Trait),
			typeof(Quantifiable),
			typeof(Measurement),
			typeof(Enumeration),
			typeof(State),
			typeof(Duration),
			typeof(Collection),
			typeof(List),
			typeof(Set),
			typeof(SortedSet),
			typeof(TimeSeries),
			typeof(Code),
			typeof(Either),
			typeof(SingleEntity),
			typeof(StructuredValue)
		};

		public static Type[] AddableConstraints =
		{
			typeof(Constraint),
			typeof(LanguageConstraint),
			typeof(LocaleConstraint),
			typeof(RangeConstraint),
			typeof(EncodingConstraint),
			typeof(LengthConstraint),
			typeof(RegularExpressionConstraint),
			typeof(FixedPointConstraint)
		};

		public static Type[] AddableElements =
		{
			// Top level
			typeof(Aspect),
			typeof(Property),
			typeof(Entity),
			typeof(Unit),
			// Characteristic
			typeof(Characteristic),
			typeof(Trait),
			typeof(Quantifiable),
			typeof(Measurement),
			typeof(Enumeration),
			typeof(State),
			typeof(Duration),
			typeof(Collection),
			typeof(List),
			typeof(Set),
			typeof(SortedSet),
			typeof(TimeSeries),
			typeof(Code),
			typeof(Either),
			typeof(SingleEntity),
			typeof(StructuredValue),
			// Constraints
			typeof(Constraint),
			typeof(LanguageConstraint),
			typeof(LocaleConstraint),
			typeof(RangeConstraint),
			typeof(EncodingConstraint),
			typeof(LengthConstraint),
			typeof(RegularExpressionConstraint),
			typeof(FixedPointConstraint)
		};

		/// <summary>
		/// Holds information, how model element types should be rendered on the screen.
		/// </summary>
		public class SammElementRenderInfo
		{
			public string DisplayName = "";
			public string Abbreviation = "";
			public uint Foreground = 0x00000000;
			public uint Background = 0x00000000;
		}

		public class SammElementRenderInfoDict : Dictionary<Type, SammElementRenderInfo>
		{
			public void Add(
				Type[] types, 
				SammElementRenderInfo value,
				string[]? displayNames = null,
				string[]? abbreviations = null)
			{
				for (int i = 0; i < types.Length; i++)
				{
					var mv = value.Copy();
					if (displayNames != null && displayNames.Length > i)
						mv.DisplayName = displayNames[i];
					if (abbreviations != null && abbreviations.Length > i)
						mv.Abbreviation = abbreviations[i];
					Add(types[i], mv);
				}
			}
		}

		private static SammElementRenderInfoDict _renderInfo = 
			      new SammElementRenderInfoDict();

		public static SammElementRenderInfo? GetRenderInfo(Type t)
		{
			if (t != null && _renderInfo.ContainsKey(t))
				return _renderInfo[t];
			return null;
		}		

		static Constants()
		{
			_renderInfo.Add(typeof(Aspect), new SammElementRenderInfo() { 
				DisplayName = "Aspect",
				Abbreviation = "A",
				Foreground = 0xFF000000,
				Background = 0xFF8298E0
			});

			_renderInfo.Add(typeof(Property), new SammElementRenderInfo()
			{
				DisplayName = "Property",
				Abbreviation = "P",
				Foreground = 0xFF000000,
				Background = 0xFFC5C8D4
			});

			_renderInfo.Add(
				types: new[] { 
					typeof(Characteristic),
					typeof(Quantifiable),
					typeof(Measurement),
					typeof(Enumeration),
					typeof(State),
					typeof(Duration),
					typeof(Collection),
					typeof(List),
					typeof(Set),
					typeof(SortedSet),
					typeof(TimeSeries),
					typeof(Code),
					typeof(Either),
					typeof(SingleEntity),
					typeof(StructuredValue)
				},
				displayNames: new[] {
					"Characteristic",
					"Quantifiable",
					"Measurement",
					"Enumeration",
					"State",
					"Duration",
					"Collection",
					"List",
					"Set",
					"SortedSet",
					"TimeSeries",
					"Code",
					"Either",
					"SingleEntity",
					"StructuredValue"
				},
				abbreviations: new[] {
					"C",
					"QN",
					"ME",
					"EN",
					"ST",
					"DU",
					"CL",
					"LI",
					"SE",
					"SS",
					"TS",
					"CO",
					"EI",
					"SE",
					"SV"
				},
				value: new SammElementRenderInfo()
				{
					DisplayName = "Characteristic",
					Abbreviation = "C",
					Foreground = 0xFF000000,
					Background = 0xFFD6E2A6
				});

			_renderInfo.Add(typeof(Entity), new SammElementRenderInfo()
			{
				DisplayName = "Entity",
				Abbreviation = "E",
				Foreground = 0xFF000000,
				Background = 0xFFAEADE0
			});

			_renderInfo.Add(typeof(UnaryExpression), new SammElementRenderInfo()
			{
				DisplayName = "Unit",
				Abbreviation = "U",
				Foreground = 0xFF000000,
				Background = 0xFFB9AB50
			});

			_renderInfo.Add(
				types: new[] {
					typeof(Constraint),
					typeof(LanguageConstraint),
					typeof(LocaleConstraint),
					typeof(RangeConstraint),
					typeof(EncodingConstraint),
					typeof(LengthConstraint),
					typeof(RegularExpressionConstraint),
					typeof(FixedPointConstraint)
				}, 
				displayNames: new[] {
					"Constraint",
					"LanguageConstraint",
					"LocaleConstraint",
					"RangeConstraint",
					"EncodingConstraint",
					"LengthConstraint",
					"RegularExpressionConstraint",
					"FixedPointConstraint"
				},
				abbreviations: new[] {
					"C",
					"LGC",
					"LOC",
					"RGC",
					"ENC",
					"LNC",
					"REC",
					"FPC"
				},
				value: new SammElementRenderInfo() {
					DisplayName = "Constraint",
					Abbreviation = "C",
					Foreground = 0xFF000000,
					Background = 0xFF74AEAF
				});

			_renderInfo.Add(typeof(Trait), new SammElementRenderInfo()
			{
				DisplayName = "Trait",
				Abbreviation = "T",
				Foreground = 0xFF000000,
				Background = 0xFF74AEAF
			});

			_renderInfo.Add(typeof(Operation), new SammElementRenderInfo()
			{
				DisplayName = "Operation",
				Abbreviation = "O",
				Foreground = 0xFF000000,
				Background = 0xFFD5BFDA
			});

			_renderInfo.Add(typeof(EventArgs), new SammElementRenderInfo()
			{
				DisplayName = "Event",
				Abbreviation = "E",
				Foreground = 0xFF000000,
				Background = 0xFFB9D8FA
			});			
		}

		public static uint RenderBackground = 0xFFEFEFF0;

		public static readonly string[] SammXsdDataTypes =
		{
			"xsd:anyURI",
			"xsd:base64Binary",
			"xsd:boolean",
			"xsd:byte",
			"xsd:date",
			"xsd:dateTime",
			"xsd:decimal",
			"xsd:double",
			"xsd:duration",
			"xsd:float",
			"xsd:gDay",
			"xsd:gMonth",
			"xsd:gMonthDay",
			"xsd:gYear",
			"xsd:gYearMonth",
			"xsd:hexBinary",
			"xsd:int",
			"xsd:integer",
			"xsd:long",
			"xsd:negativeInteger",
			"xsd:nonNegativeInteger",
			"xsd:nonPositiveInteger",
			"xsd:positiveInteger",
			"xsd:short",
			"xsd:string",
			"xsd:time",
			"xsd:unsignedByte",
			"xsd:unsignedInt",
			"xsd:unsignedLong",
			"xsd:unsignedShort",
			"langString"
		};

		public static readonly string[] CharacteristicsFixTypes =
		{
			"samm-c:Timestamp",
			"samm-c:Text",
			"samm-c:Boolean",
			"samm-c:Locale",
			"samm-c:Language",
			"samm-c:UnitReference",
			"samm-c:ResourcePath",
			"samm-c:MimeType"
		};

		public static string[]? GetPresetsForListName(string listName)
		{
			if (listName == null)
				return null;
			listName = listName.Trim().ToLower();
			if (listName == "SammXsdDataTypes".ToLower())
				return SammXsdDataTypes;
			if (listName == "Characteristics".ToLower())
				return CharacteristicsFixTypes;
			return null;
		}
	}

	public static class Util
	{
		public static bool HasSammSemanticId(Aas.IHasSemantics hasSem)
		{
			if (hasSem?.SemanticId == null)
				return false;
			if (hasSem.SemanticId.Count() != 1)
				return false;

			foreach (var ns in Constants.PossibleNamespaceURN)
				if (!hasSem.SemanticId.Keys[0].Value.StartsWith(ns))
					return true;
					
			return false;
		}

		public static string? GetSammUrn(Aas.IHasSemantics hasSem)
		{
			if (hasSem?.SemanticId == null)
				return null;
			if (hasSem.SemanticId.Count() != 1)
				return null;
			return hasSem.SemanticId.Keys[0].Value;
		}

		/// <summary>
		/// Any chars which are sitting between "meaningful words" within a URI
		/// </summary>
		public static char[] UriDelimiters = new[] {
			':', '/', '+', '?', '[', ']', '@', '!', '$', '&',
			'\'', '(', ')', '*', ',', ';', '.', '=' };

		public static string? ShortenUri(string? uri)
		{
			// corner case
			if (uri == null)
				return null;
			uri = uri.Trim();
			if (uri.Length < 1)
				return "";

			// simple case: find a anchor / '#'
			var trimPos = uri.LastIndexOf('#');

			// be more flexible?
			if (trimPos < 0)
				trimPos = uri.LastIndexOfAny(UriDelimiters);

			// ok, trim
			if (trimPos >= 0)
				uri = uri.Substring(0, trimPos);

			// return
			return uri;
		}

		public static string? LastWordOfUri(string? uri, string elseStr = "")
		{
			// corner case
			if (uri == null)
				return null;
			uri = uri.Trim();
			if (uri.Length < 1)
				return "";

			// find delimiter?
			var li = uri.LastIndexOf('#'); 
			if (li < 0) 
				li = uri.LastIndexOfAny(UriDelimiters);
			if (li > 0)
				return uri.Substring(li + 1);
			return elseStr;
		}

		public static SammPropertyUriAttribute? FindAnySammPropertyUriAttribute(
			PropertyInfo pi, SammVersion version)
		{
			foreach (var attr in pi.GetCustomAttributes(typeof(SammPropertyUriAttribute)))
				if (attr is SammPropertyUriAttribute pua && pua.Version == version)
					return pua;
			return null;
		}

		public static SammCollectionContentUriAttribute? FindAnySammCollectionContentUriAttribute(
			PropertyInfo pi, SammVersion version)
		{
			foreach (var attr in pi.GetCustomAttributes(typeof(SammCollectionContentUriAttribute)))
				if (attr is SammCollectionContentUriAttribute pua && pua.Version == version)
					return pua;
			return null;
		}

	}
}