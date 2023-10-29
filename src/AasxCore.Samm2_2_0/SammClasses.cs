/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections;
using System.Drawing;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;

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
		string GetSelfUrn();
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
		public List<LangString>? PreferredName = null;

		/// <summary>
		/// Human readable description in a specific language. This attribute may be defined multiple 
		/// times for different languages but only once for a specific language. There should be at 
		/// least one description defined with an "en" language tag.
		/// </summary>
		public List<LangString>? Description = null;

		/// <summary>
		/// A reference to a related element in an external taxonomy, ontology or other standards document. 
		/// The datatype is xsd:anyURI. This attribute may be defined multiple times.
		/// </summary>
		public List<string>? See = null;
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
	/// Base class for other constraints that constrain a Characteristic in some way, e.g., the Range Constraint 
	/// limits the value range for a Property.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#constraint"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Constraint"/>
	/// </summary>
	public class Constraint : ModelElement
	{		
	}

	/// <summary>
	/// Restricts a value to a specific language.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#language-constraint"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#LanguageConstraint"/>
	/// </summary>
	public class LanguageConstraint : Constraint
	{
		/// <summary>
		/// An ISO 639-1 [iso639] language code for the language of the value of the constrained Property, 
		/// e.g., "de".
		/// </summary>
		public string? LanguageCode; 
	}

	/// <summary>
	/// Restricts a value to a specific locale, i.e., a language with additional region information.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#locale-constraint"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#LocaleConstraint"/>
	/// </summary>
	public class LocaleConstraint : Constraint
	{
		/// <summary>
		/// An IETF BCP 47 language code for the locale of the value of the constrained Property, e.g., "de-DE".
		/// </summary>
		public string? LocaleCode;
	}

	/// <summary>
	/// Restricts the value range of a Property. At least one of <c>maxValue</c> or <c>minValue</c> must be present in a 
	/// Range Constraint.
	/// </summary>
	public class RangeConstraint : Constraint
	{
		/// <summary>
		/// The upper bound of a range.
		/// </summary>
		public string? MaxValue;

		/// <summary>
		/// The lower bound of a range.
		/// </summary>
		public string? MinValue;

		/// <summary>
		/// Defines whether the upper bound of a range is inclusive or exclusive. Possible values are 
		/// <c>AT_MOST</c> and <c>LESS_THAN</c>.
		/// </summary>
		public SammUpperBoundDefinition? UpperBoundDefinition;

		/// <summary>
		/// Defines whether the lower bound of a range is inclusive or exclusive. Possible values are 
		/// <c>AT_LEAST</c> and <c>GREATER_THAN</c>.
		/// </summary>
		public SammLowerBoundDefinition? LowerBoundDefinition;
	}

	/// <summary>
	/// Restricts the encoding of a Property.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#encoding-constraint"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#EncodingConstraint"/>
	/// </summary>
	public class EncodingConstraint : Constraint
	{
		/// <summary>
		/// Configures the encoding. This must be one of the following: 
		/// <c>US-ASCII</c>, <c>ISO-8859-1</c>, <c>UTF-8</c>, <c>UTF-16</c>, <c>UTF-16BE</c> or <c>UTF-16LE</c>.
		/// </summary>
		public SammEncoding? Value;
	}

	/// <summary>
	/// This Constraint can be used to restrict two types of Characteristics:
	/// Characteristics that have a string-like value space; in this case the Constraint restricts the length of the 
	/// (string-) value. 
	/// Collection Characteristics (Collection, Set, Sorted Set, List). In this case the Constraint restricts the 
	/// number of elements in the collection.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#length-constraint"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#LengthConstraint"/>
	/// </summary>
	public class LengthConstraint : Constraint
	{
		/// <summary>
		/// The maximum length. Must be given as xsd:nonNegativeInteger.
		/// </summary>
		public uint? MaxValue;

		/// <summary>
		/// The minimum length. Must be given as xsd:nonNegativeInteger.
		/// </summary>
		public uint? MinValue;
	}

	/// <summary>
	/// Restricts a string value to a regular expression as defined by XQuery 1.0 and XPath 2.0 Functions 
	/// and Operators [xpath-functions].
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#regular-expression-constraint"/>
	/// <see href="https://www.w3.org/TR/xpath-functions-3/#regex-syntax"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#RegularExpressionConstraint"/>
	/// </summary>
	public class RegularExpressionConstraint : Constraint
	{
		/// <summary>
		/// The regular expression.
		/// <see href="https://www.w3.org/TR/xpath-functions-3/#regex-syntax"/>
		/// </summary>
		public string? Value;
	}

	/// <summary>
	/// Defines the scaling factor as well as the amount of integral numbers for a fixed point number. 
	/// The constraint may only be used in conjunction with Characteristics which use the xsd:decimal data type.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#fixed-point-constraint"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#FixedPointConstraint"/>
	/// </summary>
	public class FixedPointConstraint : Constraint
	{
		/// <summary>
		/// The scaling factor for a fixed point number. E.g., if a fixedpoint number is 123.04, the 
		/// scaling factor is 2 (the number of digits after the decimal point). 
		/// Must be given as xsd:positiveInteger.
		/// </summary>
		public uint? Scale;

		/// <summary>
		/// The number of integral digits for a fixed point number. E.g., if a fixedpoint number 
		/// is 123.04, the integer factor is 3 (the number of digits before the decimal point). 
		/// Must be given as xsd:positiveInteger.
		/// </summary>
		public uint? Integer;
	}

	/// <summary>
	/// Base class of all characteristics. This Characteristics Class can also be instantiated directly 
	/// (i.e., without creating a subclass).
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#characteristic-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Characteristic"/>
	/// </summary>
	public class Characteristic : ModelElement, ISammSelfDescription, ISammStructureModel
	{
		// self description
		public string GetSelfName() => "samm-characteristic";
		public string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Characteristic";

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
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Trait"/>
	/// </summary>
	public class Trait : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-trait";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Trait";

		/// <summary>
		/// The Characterstic that is being constrained.
		/// Identified via <c>preferredName</c> in any language
		/// </summary>
		public SammReference? BaseCharacteristic { get; set; }

		/// <summary>
		/// A Constraint that is applicable to the base Characteristic. This attribute may be used multiple times, 
		/// to add multiple Constraints to the base Characteristic.
		/// </summary>
		public List<Constraint>? Constraint { get; set; }
	}

	/// <summary>
	/// A value which can be quantified and may have a unit, e.g., the number of bolts required for a 
	/// processing step or the expected torque with which these bolts should be tightened.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#quantifiable-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Quantifiable"/>
	/// </summary>
	public class Quantifiable : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-quantifiable";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Quantifiable";

		/// <summary>
		/// Reference to a Unit as defined in the Unit catalog
		/// </summary>
		public SammReference? Unit { get; set; }
	}

	/// <summary>
	/// A measurement is a numeric value with an associated unit and quantity kind.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#measurement-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Measurement"/>
	/// </summary>
	public class Measurement : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-measurement";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Measurement";

		/// <summary>
		/// Reference to a Unit as defined in the Unit catalog
		/// </summary>
		public SammReference Unit { get; set; }

		public Measurement()
		{
			Unit = new SammReference("");
		}
}

	/// <summary>
	/// An enumeration represents a list of possible values.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#enumeration-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Enumeration"/>
	/// </summary>
	public class Enumeration : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-enumeration";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Enumeration";

		/// <summary>
		/// List of possible values. The dataType of each of the values must match the 
		/// dataType of the Enumeration.
		/// </summary>
		public List<string> Values { get; set; }

		public Enumeration()
		{
			Values = new List<string>();
		}
	}

	/// <summary>
	/// A state is subclass of Enumeration with a default value.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#state-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#State"/>
	/// </summary>
	public class State : Enumeration, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-state";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#State";

		/// <summary>
		/// The default value for the state.
		/// </summary>
		public string DefaultValue { get; set; }

		public State()
		{
			DefaultValue = "";
		}
	}

	/// <summary>
	/// A time duration.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#duration-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Duration"/>
	/// </summary>
	public class Duration : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-duration";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Duration";

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
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Collection"/>
	/// </summary>
	public class Collection : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-collection";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Collection";

		/// <summary>
		/// Reference to a Characteristic which describes the individual elements contained in the Collection.
		/// </summary>
		public SammReference ElementCharacteristic { get; set; }

		public Collection()
		{
			ElementCharacteristic = new SammReference();
		}
	}

	/// <summary>
	/// A subclass of Collection which may contain duplicates and is ordered.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#list-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#List"/>
	/// </summary>
	public class List : Collection, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-list";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#List";
	}

	/// <summary>
	/// A subclass of Collection which may not contain duplicates and is unordered.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#set-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Set"/>
	/// </summary>
	public class Set : Collection, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-set";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Set";
	}

	/// <summary>
	/// A subclass of Collection which may not contain duplicates and is ordered.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#sorted-set-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#SortedSet"/>
	/// </summary>
	public class SortedSet : Collection, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-sorted-set";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#SortedSet";
	}

	/// <summary>
	/// A subclass of Sorted Set containing values with the exact point in time when the values where recorded.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#time-series-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#TimeSeries"/>
	/// </summary>
	public class TimeSeries : SortedSet, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-time-series";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#TimeSeries";

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
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Code"/>
	/// </summary>
	public class Code : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-code";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Code";
	}

	/// <summary>
	/// Describes a Property whose value can have one of two possible types (a disjoint union). 
	/// This Characteristic does not have one explicit samm:dataType, as it can be the datatype of either 
	/// the left or the right.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#either-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Either"/>"/>
	/// </summary>
	public class Either : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-either";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#Either";

		/// <summary>
		/// The left side of the Either. The attribute references another Characteristic which describes the value.
		/// </summary>
		public string Left { get; set; }

		/// <summary>
		/// The right side of the Either. The attribute references another Characteristic which describes the value.
		/// </summary>
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
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#SingleEntity"/>
	/// </summary>
	public class SingleEntity : Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-single-entity";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#SingleEntity";
	}

	/// <summary>
	/// Describes a Property which contains a scalar string-like value space value with a well-defined structure. 
	/// The Structured Value Characteristic allows the description of the parts of the Property’s value by 
	/// linking to a separate Property definition for each part. To define the parts, the value must be 
	/// deconstructed using a regular expression.
	/// <see href="https://eclipse-esmf.github.io/samm-specification/snapshot/characteristics.html#structured-value-characteristic"/>
	/// <seealso href="urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#StructuredValue"/>
	/// </summary>
	public class StructuredValue: Characteristic, ISammSelfDescription
	{
		// self description
		public new string GetSelfName() => "samm-structured-value";
		public new string GetSelfUrn() => "urn:samm:org.eclipse.esmf.samm:characteristic:2.1.0#StructuredValue";

		/// <summary>
		/// The regular expression used to deconstruct the value into parts that are mapped to separate 
		/// Properties. This regular expression must contain the same number of capture groups as there 
		/// are Properties given in the elements list. The n​th capture group maps to the n​th Property 
		/// in the elements list.
		/// </summary>
		public string DeconstructionRule { get; set; }

		/// <summary>
		/// A list of entries each of which can either be a Property reference or a string literal. 
		/// The list must contain at least one Property reference.
		/// </summary>
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
		public string GetSelfUrn() => "urn:bamm:io.openmanufacturing:meta-model:1.0.0#Property";

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
		public string? ExampleValue { get; set; }

		/// <summary>
		/// One Property has exactly one Characteristic.
		/// </summary>
		public SammReference Characteristic { get; set; }

		public Property()
		{
			Characteristic = new SammReference("");
		}
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
		public string GetSelfUrn() => "urn:bamm:io.openmanufacturing:meta-model:1.0.0#Aspect";

		// structure model
		public bool IsTopElement() => true;
		public IEnumerable<SammReference> DescendOnce()
		{
			if (Properties != null)
				foreach (var x in Properties)
					yield return x;
		}

		// own
		public List<SammReference> Properties { get; set; }
		public List<SammReference> Events { get; set; }
		public List<SammReference> Operations { get; set; }

		public Aspect()
		{
			Properties = new List<SammReference>();
			Events = new List<SammReference>();
			Operations = new List<SammReference>();
		}
	}

	/// <summary>
	/// Provides some constant values to the model.
	/// </summary>
	public static class Constants
	{
		public static string NamespaceURN = "urn:samm:org.eclipse.esmf.samm:";

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

		public static Type[] AddableElements =
		{
			// Top level
			typeof(Aspect),
			typeof(Property),
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
			typeof(StructuredValue)
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

		private static Dictionary<Type, SammElementRenderInfo> _renderInfo = 
			      new Dictionary<Type, SammElementRenderInfo>();

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

			_renderInfo.Add(typeof(Characteristic), new SammElementRenderInfo()
			{
				DisplayName = "Characteristic",
				Abbreviation = "C",
				Foreground = 0xFF000000,
				Background = 0xFFD6E2A6
			});

			_renderInfo.Add(typeof(IEntity), new SammElementRenderInfo()
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

			_renderInfo.Add(typeof(Constraint), new SammElementRenderInfo()
			{
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
	}

	public static class Util
	{
		public static bool HasSammSemanticId(Aas.IHasSemantics hasSem)
		{
			if (hasSem?.SemanticId == null)
				return false;
			if (hasSem.SemanticId.Count() != 1)
				return false;
			if (!hasSem.SemanticId.Keys[0].Value.StartsWith(Constants.NamespaceURN))
				return false;
			return true;
		}

		public static string? GetSammUrn(Aas.IHasSemantics hasSem)
		{
			if (hasSem?.SemanticId == null)
				return null;
			if (hasSem.SemanticId.Count() != 1)
				return null;
			return hasSem.SemanticId.Keys[0].Value;
		}

		public static Dictionary<string, Type> SammUrnToType = new Dictionary<string, Type>();

		public static Dictionary<Type, string> SammTypeToName = new Dictionary<Type, string>();

		static Util()
		{
			// dictionary from URN to type
			foreach (var st in Constants.AddableElements)
			{
				if (Activator.CreateInstance(st, new object[] { }) is ISammSelfDescription ssd)
				{
					SammUrnToType.Add(ssd.GetSelfUrn().ToLower(), st);
					SammTypeToName.Add(st, "" + ssd.GetSelfName());
				}
			}
		}

		public static Type? GetTypeFromUrn(string? urn)
		{
			if (urn == null)
				return null;
			if (SammUrnToType.ContainsKey(urn.ToLower()))
				return SammUrnToType[urn.ToLower()];
			return null;
		}

		public static string? GetNameFromSammType(Type? sammType)
		{
			if (sammType == null)
				return null;
			if (SammTypeToName.ContainsKey(sammType))
				return SammTypeToName[sammType];
			return null;
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
	}
}