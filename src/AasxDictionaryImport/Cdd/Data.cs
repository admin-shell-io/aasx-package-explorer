/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AasxDictionaryImport.Cdd
{
    /// <summary>
    /// Base class for all elements in the CDD hierarchy.
    /// <para>
    /// This class defines the common attributes for all elements in the CDD hierarchy (Code, PreferredName, Definition)
    /// and provides helper methods to access the data stored in the element.  The data is stored as a dictionary that
    /// maps the attribute codes to the attribute values, for example {"MDC_P001_12" => "0112/2///62683#ACI081"}.
    /// </para>
    /// </summary>
    public abstract class Element
    {
        private const string BaseUrl = "https://cdd.iec.ch/cdd/";
        private const string UrlPathPattern = "iec{0}/iec{0}.nsf/{2}/{1}";
        private static readonly Regex CodeRegex = new Regex("^0112/2///([0-9]+)(_[0-9]+)?#.*$");
        private readonly Dictionary<string, string> _data;

        /// <summary>
        /// The ID of the element.
        /// </summary>
        public abstract string Code { get; }

        /// <summary>
        /// The version of the element.
        /// </summary>
        public String Version => GetString("MDC_P002_1");

        /// <summary>
        /// The revision of the element.
        /// </summary>
        public String Revision => GetString("MDC_P002_2");

        /// <summary>
        /// The preferred name of the element in multiple languages.
        /// </summary>
        public MultiString PreferredName => GetMultiString("MDC_P004_1");

        /// <summary>
        /// The definition of the element in multiple languages.
        /// </summary>
        public MultiString Definition => GetMultiString("MDC_P005");

        /// <summary>
        /// Creates a new Element object based on the given data.
        /// </summary>
        /// <param name="data">The element data as a mapping from attribute codes to attribute values</param>
        protected Element(Dictionary<string, string> data)
        {
            _data = data;
        }

        /// <summary>
        /// Creates a new Element object, copying the data from the given element.
        /// </summary>
        /// <param name="element">The element to copy the data from</param>
        protected Element(Element element)
        {
            _data = new Dictionary<string, string>(element._data);
        }

        /// <summary>
        /// Returns the IEC 61360 attributes for this element.
        /// </summary>
        /// <param name="all">Whether all attributes or only the free attributes should be used</param>
        /// <returns>The IEC 61360 data for this element</returns>
        public virtual Iec61360Data GetIec61360Data(bool all)
        {
            var data = new Iec61360Data(FormatIrdi(Code, Version))
            {
                PreferredName = PreferredName,
            };
            if (all)
                data.Definition = Definition;
            return data;
        }

        /// <summary>
        /// Returns the IEC CDD domain for this element, or null if the domain could not be determined.  The domain is
        /// extracted from the element code, assuming that it has the format "0112/2///{domain}#{id}".
        /// </summary>
        /// <returns>The IEC CDD domain for this element, or null if it can't be extracted</returns>
        private string? GetDomain()
        {
            var match = CodeRegex.Match(Code);
            if (!match.Success)
                return null;
            return match.Groups[1].Value;
        }

        /// <summary>
        /// Returns the URL to the IEC CDD web page for this element, or null if the URL could not be determined.
        /// </summary>
        /// <returns>The URL for the IEC CDD web page for this element, or null if it could not be determined</returns>
        // ReSharper disable once ReturnTypeCanBeNotNullable
        public Uri? GetDetailsUrl()
        {
            var baseUri = new Uri(BaseUrl);
            var domain = GetDomain();

            if (domain == null)
                return null;

            var code = EncodeCode();
            var endpoint = GetEndpoint();
            var path = String.Format(UrlPathPattern, domain, code, endpoint);
            return new Uri(baseUri, path);
        }

        /// <summary>
        /// Returns the URL-encoded code for this element.
        /// </summary>
        /// <returns>The URL-encoded code for this element</returns>
        private string EncodeCode()
        {
            return Code.Replace('/', '-').Replace("#", "%23");
        }

        /// <summary>
        /// Returns the endpoint to use when constructing the IEC CDD URL for this element.  See the URL pattern in <see
        /// cref="UrlPathPattern"/> for more information.
        /// </summary>
        /// <returns>The endpoint to use in the details URL for this element</returns>
        protected abstract string GetEndpoint();

        /// <inheritdoc/>
        public override string ToString()
        {
            return Code + ": " + PreferredName;
        }

        protected string GetString(string key)
        {
            return _data[key];
        }

        protected MultiString GetMultiString(string key)
        {
            var data = new Dictionary<string, string>();
            var prefix = key + ".";
            foreach (var s in _data.Keys)
            {
                if (s.StartsWith(prefix))
                    data.Add(s.Substring(prefix.Length), _data[s]);
            }
            return new MultiString(data);
        }

        protected Reference<T> GetReference<T>(string key)
            where T : Element
        {
            return new Reference<T>(GetString(key));
        }

        private List<string> GetList(string key)
        {
            var list = new List<string>();
            var value = GetString(key).TrimStart('(', '{').TrimEnd(')', '}');
            foreach (var part in value.Split(','))
            {
                var trimmedPart = part.Trim();
                if (trimmedPart.Length > 0)
                    list.Add(trimmedPart);
            }
            return list;
        }

        protected ReferenceList<T> GetReferenceList<T>(string key)
            where T : Element
        {
            var list = new ReferenceList<T>();
            foreach (var id in GetList(key))
            {
                list.Add(new Reference<T>(id));
            }
            return list;
        }

        protected static string FormatIrdi(string code, string version) => $"{code}#{version.PadLeft(3, '0')}";
    }

    /// <summary>
    /// Reference to an element in the CDD hierarchy.
    /// </summary>
    /// <typeparam name="T">The type of the referenced element</typeparam>
    public class Reference<T> where T : Element
    {
        public string Code { get; }

        public bool IsSet => Code.Length > 0;

        public Reference(string code)
        {
            Code = code;
        }

        public T? Get(Context context)
        {
            return context.GetElement<T>(Code);
        }

        public override string ToString()
        {
            return Code;
        }
    }

    /// <summary>
    /// A list of references to elements in the CDD hierarchy.
    /// </summary>
    /// <typeparam name="T">The type of the referenced elements</typeparam>
    public class ReferenceList<T> : List<Reference<T>> where T : Element
    {
        public List<T> Get(Context context)
        {
            var list = new List<T>();
            foreach (var r in this)
            {
                var value = r.Get(context);
                if (value != null)
                    list.Add(value);
            }
            return list;
        }
    }

    /// <summary>
    /// CDD class, a collection of CDD properties.
    /// </summary>
    // Instances of this type are created using reflection in Parser.ParseElement.
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Class : Element
    {
        public override string Code => GetString("MDC_P001_5");

        public string DefinitionSource => GetString("MDC_P006_1");

        public Reference<Class> Superclass => GetReference<Class>("MDC_P010");

        public ReferenceList<Property> Properties
            => GetReferenceList<Property>("MDC_P014");

        public ReferenceList<Property> ImportedProperties
            => GetReferenceList<Property>("MDC_P090");

        public Class(Dictionary<string, string> data) : base(data)
        {
        }

        public ReferenceList<Property> GetAllProperties(Context context)
        {
            var properties = new ReferenceList<Property>();
            properties.AddRange(Properties);
            properties.AddRange(ImportedProperties);
            var superclass = Superclass.Get(context);
            if (superclass != null)
            {
                properties.AddRange(superclass.GetAllProperties(context));
            }
            return properties;
        }

        public override Iec61360Data GetIec61360Data(bool all)
        {
            var data = base.GetIec61360Data(all);
            if (all)
                data.DefinitionSource = DefinitionSource;
            return data;
        }

        protected override string GetEndpoint()
        {
            return "classes";
        }
    }

    /// <summary>
    /// CDD property, either an actual property or a reference to a class that holds more properties.
    /// </summary>
    // Instances of this type are created using reflection in Parser.ParseElement.
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Property : Element
    {
        private readonly DataType? _overrideDataType;

        public override string Code => GetString("MDC_P001_6");

        public MultiString ShortName => GetMultiString("MDC_P004_3");

        public string DefinitionSource => GetString("MDC_P006_1");

        public string Symbol => GetString("MDC_P025_1");

        public string PrimaryUnit => GetString("MDC_P023");

        public string UnitCode => GetString("MDC_P041");

        public string RawDataType => GetString("MDC_P022");

        public DataType DataType
        {
            get
            {
                if (_overrideDataType != null)
                    return _overrideDataType;
                return DataType.ParseType(RawDataType);
            }
        }

        public string Format => GetString("MDC_P024");

        public Property(Dictionary<string, string> data) : base(data)
        {
        }

        private Property(Property property, DataType dataType) : base(property)
        {
            _overrideDataType = dataType;
        }

        public Property ReplaceDataType(DataType dataType)
        {
            return new Property(this, dataType);
        }

        public override Iec61360Data GetIec61360Data(bool all)
        {
            var data = base.GetIec61360Data(all);
            data.Unit = PrimaryUnit;
            data.UnitIrdi = UnitCode;
            data.DataFormat = Format;
            data.Symbol = Symbol;
            if (all)
            {
                data.ShortName = ShortName;
                data.DefinitionSource = DefinitionSource;
                data.DataType = RawDataType;
            }
            return data;
        }

        protected override string GetEndpoint()
        {
            return "PropertiesAllVersions";
        }
    }

    public abstract class DataType
    {
        private const string TypeSuffix = "_TYPE";
        private static readonly Regex TypeRegex = new Regex(@"^([^\(\)]+)(?:\((.+)\))?$");
        private static readonly Regex CompositeTypeRegex = new Regex(@"^(.*) OF (.*)$");

        public virtual Reference<Class>? GetClassReference()
        {
            return null;
        }

        public static DataType ParseType(string s)
        {
            var match = CompositeTypeRegex.Match(s);
            if (match.Success)
            {
                var subtype = ParseType(match.Groups[2].Value);
                return ParseCompositeType(match.Groups[1].Value, subtype);
            }

            return ParseBasicType(s);
        }

        private static Tuple<string, string[]>? ParseTypeNameArgs(string s)
        {
            var match = TypeRegex.Match(s);
            if (!match.Success)
                return null;

            var typeName = match.Groups[1].Value.ToUpperInvariant();
            var typeArgs = new string[] { };
            if (match.Groups[2].Value.Length > 0)
                typeArgs = match.Groups[2].Value.Split(',').Select(p => p.Trim()).ToArray();

            if (typeName.EndsWith(TypeSuffix))
                typeName = typeName.Substring(0, typeName.Length - TypeSuffix.Length);

            return Tuple.Create(typeName, typeArgs);
        }

        private static DataType ParseBasicType(string s)
        {
            var typeTuple = ParseTypeNameArgs(s);
            DataType? dataType = null;
            if (typeTuple != null)
            {
                var typeName = typeTuple.Item1;
                var typeArgs = typeTuple.Item2;

                dataType ??= SimpleType.ParseType(typeName, typeArgs);
                dataType ??= EnumType.ParseType(typeName, typeArgs);
                dataType ??= ClassInstanceType.ParseType(typeName, typeArgs);
                dataType ??= ClassReferenceType.ParseType(typeName, typeArgs);
                dataType ??= LargeObjectType.ParseType(typeName, typeArgs);
                dataType ??= PlacementType.ParseType(typeName, typeArgs);
            }
            return dataType ?? new UnknownType(s);
        }

        private static DataType ParseCompositeType(string s, DataType subtype)
        {
            var typeTuple = ParseTypeNameArgs(s);
            DataType? dataType = null;
            if (typeTuple != null)
            {
                var typeName = typeTuple.Item1;
                var typeArgs = typeTuple.Item2;

                dataType ??= AggregateType.ParseType(typeName, typeArgs, subtype);
                dataType ??= LevelType.ParseType(typeName, typeArgs, subtype);
            }
            return dataType ?? new UnknownType(s);
        }

        protected static bool ParseTypeEnum<TEnum>(string s, out TEnum result) where TEnum : struct
        {
            return Enum.TryParse(s.Replace("_", String.Empty), true, out result);
        }
    }

    public class SimpleType : DataType, IEquatable<SimpleType>
    {
        public Type TypeValue { get; }


        public SimpleType(Type typeValue)
        {
            TypeValue = typeValue;
        }

        public bool Equals(SimpleType? type)
        {
            return type != null && type.TypeValue == TypeValue;
        }

        public override bool Equals(object? o)
        {
            return Equals(o as SimpleType);
        }

        public override int GetHashCode()
        {
            return TypeValue.GetHashCode();
        }

        public static SimpleType? ParseType(string typeName, string[] typeArgs)
        {
            if (typeArgs.Length != 0)
                return null;

            if (ParseTypeEnum(typeName, out Type innerType))
                return new SimpleType(innerType);

            return null;
        }

        public enum Type
        {
            Boolean,
            Binary,
            String,
            TranslatableString,
            NonTranslatableString,
            DateTime,
            Date,
            Time,
            Irdi,
            Icid,
            Iso29002Irdi,
            Uri,
            Html5,
            Number,
            Int,
            IntMeasure,
            IntCurrency,
            Rational,
            RationalMeasure,
            Real,
            RealMeasure,
            RealCurrency,
        }
    }

    public class EnumType : DataType
    {
        private const string EnumPrefix = "ENUM_";

        public Type TypeValue { get; }

        public string ReferenceCode { get; }

        public EnumType(Type typeValue, string referenceCode)
        {
            TypeValue = typeValue;
            ReferenceCode = referenceCode;
        }

        public static EnumType? ParseType(string typeName, string[] typeArgs)
        {
            if (!typeName.StartsWith(EnumPrefix))
                return null;
            typeName = typeName.Substring(EnumPrefix.Length);
            if (typeArgs.Length != 1)
                return null;
            if (ParseTypeEnum(typeName, out Type innerType))
                return new EnumType(innerType, typeArgs[0]);
            return null;
        }

        public enum Type
        {
            Code,
            Int,
            Real,
            String,
            Rational,
            Reference,
            Instance,
            Boolean,
        }
    }

    public class ClassInstanceType : DataType
    {
        public Reference<Class> Class { get; }

        public ClassInstanceType(string irdi)
        {
            Class = new Reference<Class>(irdi);
        }

        public override Reference<Class> GetClassReference() => Class;

        public static ClassInstanceType? ParseType(string typeName, string[] typeArgs)
        {
            if (typeName == "CLASS_INSTANCE" && typeArgs.Length == 1)
            {
                return new ClassInstanceType(typeArgs[0]);
            }
            return null;
        }
    }

    public class ClassReferenceType : DataType
    {
        public Reference<Class> Class { get; }

        public ClassReferenceType(string irdi)
        {
            Class = new Reference<Class>(irdi);
        }

        public override Reference<Class> GetClassReference() => Class;

        public static ClassReferenceType? ParseType(string typeName, string[] typeArgs)
        {
            if (typeName == "CLASS_REFERENCE" && typeArgs.Length == 1)
            {
                return new ClassReferenceType(typeArgs[0]);
            }
            return null;
        }
    }

    public class AggregateType : DataType
    {
        /// <summary>
        /// The lower bound of this aggregate type.  The meaning of this value
        /// depends on the TypeValue:
        /// - for Bag, List, UniqueList, Set and ConstrainedSet:  The lower
        ///   bound is the minimum number of elements in the aggregate type.
        /// - for all Array types:  The lower bound is the index of the first
        ///   element in this aggregate type.  This means that there are
        ///   exactly UpperBound - LowerBound + 1 elements.
        /// </summary>
        public int LowerBound { get; }

        /// <summary>
        /// The upper bound of this aggregate type.  The meaning of this value
        /// depends on the TypeValue:
        /// - for Bag, List, UniqueList, Set and ConstrainedSet:  The upper
        ///   bound is the maximum number of elements in the aggregate type.
        ///   If it is set to null, there is no upper bound.
        /// - for all Array types: The upper bound is the index of the last
        ///   element in this aggregate type.  This means that there are
        ///   exactly UpperBound - LowerBound + 1 elements.  It may not be null.
        /// </summary>
        public int? UpperBound { get; }

        /// <summary>
        /// The minimum number of elements in this aggregate type, or zero if
        /// there is no lower bound for the number of elements.
        /// </summary>
        public int MinimumElementCount
        {
            get
            {
                switch (TypeValue)
                {
                    case Type.Bag:
                    case Type.List:
                    case Type.UniqueList:
                    case Type.Set:
                        return LowerBound;
                    case Type.ConstrainedSet:
                        return 0;
                    case Type.Array:
                    case Type.OptionalArray:
                    case Type.UniqueArray:
                    case Type.UniqueOptionalArray:
                        // UpperBound may not be null for array types
                        if (UpperBound == null)
                            return 0;
                        return UpperBound.Value - LowerBound + 1;
                    default:
                        return 0;
                }
            }
        }

        public Type TypeValue { get; }

        /// <summary>
        /// The type of the elements in this aggregate type.
        /// </summary>
        public DataType Subtype { get; }

        public AggregateType(Type typeValue, DataType subtype, int lowerBound, int? upperBound)
        {
            TypeValue = typeValue;
            Subtype = subtype;
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        public static AggregateType? ParseType(string typeName, string[] typeArgs, DataType subtype)
        {
            // We need at least the lower and upper bound as arguments.
            if (typeArgs.Length < 2)
                return null;

            if (!Int32.TryParse(typeArgs[0], out int lowerBound))
                return null;

            int? upperBound = null;
            if (typeArgs[1] != "?")
            {
                if (!Int32.TryParse(typeArgs[1], out int upperBoundValue))
                    return null;
                upperBound = upperBoundValue;
            }

            if (ParseTypeEnum(typeName, out Type typeValue))
                return new AggregateType(typeValue, subtype, lowerBound, upperBound);

            return null;
        }

        public enum Type
        {
            Bag,
            List,
            UniqueList,
            Set,
            ConstrainedSet,
            Array,
            OptionalArray,
            UniqueArray,
            UniqueOptionalArray,
        }
    }

    public class LevelType : DataType
    {
        /// <summary>
        /// The types of this level property, for example {Minimum, Maximum}
        /// for LEVEL(MIN,MAX) OF INT_TYPE.  May not be empty.
        /// </summary>
        public ISet<Type> Types { get; }

        /// <summary>
        /// The type of the underlying property, for example
        /// SimpleType(Type.Integer) for LEVEL(MIN,MAX) OF INT_TYPE.
        /// </summary>
        public DataType Subtype { get; }

        public LevelType(ISet<Type> types, DataType subtype)
        {
            Types = types;
            Subtype = subtype;
        }

        public static LevelType? ParseType(string typeName, string[] typeArgs, DataType subtype)
        {
            if (typeName != "LEVEL")
                return null;
            var types = new HashSet<Type>();
            foreach (var arg in typeArgs)
            {
                var typeValue = ParseTypeValue(arg);
                if (typeValue == null)
                    return null;
                types.Add((Type)typeValue);
            }
            if (types.Count > 0)
                return new LevelType(types, subtype);
            return null;
        }

        private static Type? ParseTypeValue(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "min":
                    return Type.Minimum;
                case "max":
                    return Type.Maximum;
                case "typ":
                    return Type.Typical;
                case "nom":
                    return Type.Nominal;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The type of a level value.
        /// </summary>
        public enum Type
        {
            Minimum,
            Maximum,
            Nominal,
            Typical,
        }
    }

    public class LargeObjectType : DataType
    {
        public static LargeObjectType? ParseType(string typeName, string[] typeArgs)
        {
            if (typeName == "LOB")
                return new LargeObjectType();
            return null;
        }
    }

    public class PlacementType : DataType
    {
        public Type TypeValue { get; }

        public PlacementType(Type typeValue)
        {
            TypeValue = typeValue;
        }

        public static PlacementType? ParseType(string typeName, string[] typeArgs)
        {
            if (ParseTypeEnum(typeName, out Type type))
                return new PlacementType(type);
            return null;
        }

        public enum Type
        {
            Axis1Placement2D,
            Axis1Placement3D,
            Axis2Placement2D,
            Axis2Placement3D,
        }
    }

    public class UnknownType : DataType
    {
        public UnknownType(string typeValue)
        {
            TypeValue = typeValue;
        }

        public string TypeValue { get; }
    }
}
