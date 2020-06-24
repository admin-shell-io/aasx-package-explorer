// Copyright (C) 2020 Robin Krahl, RD/ESR, SICK AG <robin.krahl@sick.de>
// This software is licensed under the Apache License 2.0 (Apache-2.0).

#nullable enable

using AasxPackageExplorer;
using AdminShellNS;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace AasxImport.Eclass
{
    /// <summary>
    /// Data provider for eCl@ss Basic data.  The data is read from XML files using the OntoML scheme, see
    /// <see cref="Context"/> for more information.  The complete eCl@ss exports use one file per language and domain.
    /// Currently, we only parse one file at a time, i. e. we can only read data for one language at a time.  Also,
    /// units are stored in a separate file and are currently not parsed.
    /// </summary>
    /// <seealso href="https://wiki.eclass.eu/wiki/ISO_13584-32_ontoML"/>
    public class DataProvider : Model.DataProviderBase
    {
        /// <inheritdoc/>
        public override string Name => "eCl@ss";

        /// <inheritdoc/>
        public override bool IsValidPath(string path)
            => EclassUtils.TryGetDataFileType(path) == EclassUtils.DataFileType.Dictionary;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetDefaultPaths()
        {
            var searchPath = System.IO.Path.GetFullPath(Options.Curr.EclassDir);
            if (!System.IO.Directory.Exists(searchPath))
                return new List<string>();
            return System.IO.Directory.GetFiles(searchPath);
        }

        /// <inheritdoc/>
        protected override Model.IDataSource OpenPath(string path, Model.DataSourceType type)
            => new DataSource(this, path, type);
    }

    /// <summary>
    /// Data source for eCl@ss data.  The eCl@ss data is read from XML files, and a data source represents one eCl@ss
    /// XML file.  For more information on the XML parser, see the <see cref="Context"/> and <see cref="Element"/>
    /// classes.
    /// </summary>
    public class DataSource : Model.FileSystemDataSource
    {
        /// <summary>
        /// Creates a new DataSource object with the given data.
        /// </summary>
        /// <param name="dataProvider">The data provider for this data source</param>
        /// <param name="path">The path of the eCl@ss XML file</param>
        /// <param name="type">The type of the data source</param>
        public DataSource(Model.IDataProvider dataProvider, string path, Model.DataSourceType type)
           : base(dataProvider, path, type)
        {
        }

        /// <inheritdoc/>
        public override Model.IDataContext Load()
        {
            try
            {
                var xml = XDocument.Load(Path);
                return new Context(this, xml);
            }
            catch (XmlException e)
            {
                throw new Model.ImportException($"Could not load the XML document at '{Path}'", e);
            }
        }
    }

    /// <summary>
    /// Data context for eCl@ss data.  The context loads the classes and properties from an XML document.  It
    /// removes all deprecated elements because they are no longer part of the eCl@ss release.  It also computes a
    /// mapping from classification classes to application classes.  For more information on the class types, see
    /// <see cref="Class"/>.
    /// </summary>
    public class Context : Model.IDataContext
    {
        private static XNamespace OntoML { get; } = "urn:iso:std:iso:is:13584:-32:ed-1:tech:xml-schema:ontoml";

        private readonly IDictionary<string, Element> _elements = new Dictionary<string, Element>();

        private readonly ICollection<string> _deprecatedElements = new List<string>();

        /// <summary>
        /// The data source this data has been read from.
        /// </summary>
        public DataSource DataSource { get; }

        /// <summary>
        /// The top-level classes in this data context.
        /// </summary>
        public ICollection<Class> Classes { get; }

        /// <inheritdoc/>
        public ICollection<Model.UnknownReference> UnknownReferences { get; } = new List<Model.UnknownReference>();

        /// <summary>
        /// Creates a new eCl@ss Context and loads the data from the given XML document.
        /// </summary>
        /// <param name="dataSource">The data source for this context</param>
        /// <param name="document">The XML document to read the data from</param>
        public Context(DataSource dataSource, XDocument document)
        {
            DataSource = dataSource;

            var classes = document.Descendants(OntoML + "class").Select(e => new Class(this, e)).ToList();
            AddElements(classes);
            Classes = classes.Where(c => !c.IsDeprecated).ToList();
            AssignApplicationClasses();

            AddElements(document.Descendants(OntoML + "property").Select(e => new Property(this, e)));
        }

        private void AssignApplicationClasses()
        {
            foreach (var cls in Classes)
            {
                foreach (var cclsId in cls.ClassificationClassIds)
                {
                    var ccls = GetElement<Class>(cclsId);
                    if (ccls != null)
                        ccls.ApplicationClasses.Add(cls);
                }
            }
        }

        private void AddElements<T>(IEnumerable<T> elements) where T : Element
        {
            foreach (var element in elements)
            {
                if (element.IsDeprecated)
                {
                    _deprecatedElements.Add(element.Id);
                }
                else
                {
                    // TODO: possible duplicates
                    if (!_elements.ContainsKey(element.Id))
                        _elements.Add(element.Id, element);
                }
            }
        }

        /// <inheritdoc/>
        public ICollection<Model.IElement> LoadSubmodels() => Classes.Cast<Model.IElement>().ToList();

        /// <inheritdoc/>
        public ICollection<Model.IElement> LoadSubmodelElements() => _elements.Values.Cast<Model.IElement>().ToList();

        public Property? GetProperty(Class parent, string id)
        {
            var property = GetElement<Property>(id);
            if (property != null)
                return new Property(property, parent);
            return null;
        }

        private T? GetElement<T>(string id) where T : Element
        {
            if (_elements.TryGetValue(id, out Element e))
                if (e is T t)
                    return t;

            // We don't have to record deprecated elements as unknown references because we don't add them to _elements
            // in the first place
            if (!_deprecatedElements.Contains(id))
                UnknownReferences.Add(Model.UnknownReference.Create<Property>(id));
            return null;
        }
    }

    /// <summary>
    /// An element in the eCl@ss data set, backed by an XML element from the exported XML document.  Currently, we
    /// support <see cref="Class"/> and <see cref="Property"/> elements.
    /// </summary>
    public abstract class Element : Model.LazyElementBase
    {
        protected XElement XElement;

        /// <summary>
        /// The current data context.
        /// </summary>
        public Context Context { get; }

        /// <inheritdoc/>
        public override string Id => XElement.Attributes("id").FirstValue();

        /// <inheritdoc/>
        public override string Name => PreferredName.GetDefault();

        public string Revision => XElement.Elements("revision").FirstValue();

        public MultiString PreferredName => GetMultiString("preferred_name", "label");

        public MultiString Definition => GetMultiString("definition", "text");

        /// <summary>
        /// Whether this element is deprecated.  If this property is true, this element
        /// should be ignored as it is no longer part of the official eCl@ss release.
        /// </summary>
        public bool IsDeprecated => XElement.Elements("is_deprecated").FirstValue() == "true";

        protected Element(Context context, XElement element, Model.IElement? parent = null)
            : base(context.DataSource, parent)
        {
            XElement = element;
            Context = context;
        }

        protected virtual Iec61360Data GetIec61360Data()
        {
            return new Iec61360Data(Id)
            {
                Definition = Definition,
                PreferredName = PreferredName,
            };
        }

        protected MultiString GetMultiString(string name, string childElement)
        {
            var ms = new MultiString();
            foreach (var label in XElement.Elements(name).Elements(childElement))
            {
                var lang = label.Attribute("language_code");
                if (lang != null && lang.Value.Length > 0)
                    ms.Add(lang.Value, label.Value);
            }
            return ms;
        }

        /// <inheritdoc/>
        public override Dictionary<string, string> GetDetails()
        {
            return new Dictionary<string, string>
            {
                { "ID", Id },
                { "Revision", Revision },
                { "Preferred Name", PreferredName.GetDefault() },
                { "Definition", Definition.GetDefault() },
            };
        }

        // TODO: check -- do we have a public URI?
    }

    /// <summary>
    /// An eCl@ss class.  eCl@ss has two types of classes:  application classes and classification classes.
    /// Classification classes are part of the classification hierarchy while an application class defines the
    /// properties for a classification class.  Unfortunately, the classification classes do not store references to
    /// their application classes.  Therefore we manually compute this mapping in <see cref="Context"/> when loading
    /// the data from the XML file.
    /// <para>
    /// In eCl@ss Basic, there is a 1:1 relation between classification and application classes.  In eCl@ss Advanced,
    /// there may be several application classes per classification class (typically one for the basic attributes and
    /// one for the advanced attributes).  As this implementation only supports eCl@ss Basic, we assume that there is
    /// at most one application class per classification class and just transfer the application class's properties to
    /// the classification class.
    /// </para>
    /// </summary>
    public class Class : Element
    {
        /// <summary>
        /// The IDs of the classification classes that this application class describes, or an empty list if this is a
        /// classification class.
        /// </summary>
        public ICollection<string> ClassificationClassIds
            => XElement.Elements("is_case_of").Elements("class").Attributes("class_ref").Values().ToList();

        /// <summary>
        /// The application classes for this classification class, or an empty list if this is an application class.
        /// </summary>
        public ICollection<Class> ApplicationClasses { get; } = new List<Class>();

        /// <summary>
        /// Creates a new Class object within the given context, backed by the given XML element.
        /// </summary>
        /// <param name="context">The context for this element</param>
        /// <param name="element">The XML element with the data for this element</param>
        public Class(Context context, XElement element) : base(context, element)
        {
        }

        /// <inheritdoc/>
        public override bool ImportSubmodelInto(AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.AdministrationShell adminShell)
        {
            if (!IsSelected)
                return false;

            var submodel = Iec61360Utils.CreateSubmodel(env, adminShell, GetIec61360Data());
            ImportSubmodelElementsInto(env, submodel);
            return true;
        }

        /// <inheritdoc/>
        public override bool ImportSubmodelElementsInto(AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.IManageSubmodelElements parent)
        {
            return Children.Count(c => c.ImportSubmodelElementsInto(env, parent)) > 0;
        }

        /// <inheritdoc/>
        protected override ICollection<Model.IElement> LoadChildren()
        {
            if (ApplicationClasses.Count > 0)
                // We assume that there is at most one application class per classification class, so we just transfer
                // the children
                return ApplicationClasses.First().LoadChildren(this);
            return LoadChildren(this);
        }

        private ICollection<Model.IElement> LoadChildren(Class parent)
        {
            return XElement
                .Elements("described_by")
                .Elements("property")
                .Attributes("property_ref")
                .Select(a => Context.GetProperty(parent, a.Value))
                .Where(p => p != null)
                .Cast<Model.IElement>()
                .ToList();
        }
    }

    /// <summary>
    /// An eCl@ss property.  eCl@ss properties are assigned to application classes, see <see cref="Class"/>.
    /// </summary>
    public class Property : Element
    {
        private static XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

        public MultiString ShortName => GetMultiString("short_name", "label");

        public string Type => XElement.Elements("domain").Attributes(Xsi + "type").FirstValue();

        public string UnitIrdi => XElement.Elements("domain").Elements("unit").Attributes("unit_ref").FirstValue();

        /// <summary>
        /// Creates a new Property object within the given context, backed by the given XML element.
        /// </summary>
        /// <param name="context">The context for this element</param>
        /// <param name="element">The XML element with the data for this element</param>
        /// <param name="parent">The parent element</param>
        public Property(Context context, XElement element, Model.IElement? parent = null)
            : base(context, element, parent)
        {
        }

        public Property(Property property, Model.IElement? parent)
            : this(property.Context, property.XElement, parent)
        {
        }

        /// <inheritdoc/>
        public override bool ImportSubmodelElementsInto(AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.IManageSubmodelElements parent)
        {
            if (!IsSelected)
                return false;

            var data = GetIec61360Data();
            var property = Iec61360Utils.CreateProperty(env, data, GetValueType(data.DataType));
            parent.Add(property);
            return true;
        }

        protected override Iec61360Data GetIec61360Data()
        {
            // TODO: unit
            var data = base.GetIec61360Data();
            data.DataType = GetDataType(Type);
            data.ShortName = ShortName;
            data.UnitIrdi = UnitIrdi;
            return data;
        }

        public override Dictionary<string, string> GetDetails()
        {
            var details = base.GetDetails();
            details.Add("Short Name", ShortName.GetDefault());
            return details;
        }

        private static string GetDataType(string type)
        {
            // TODO: This logic is copied from EclassUtils.GenerateConceptDescription -- does it handle all possible
            // values?
            var lowerType = type.ToLower();
            foreach (var aasType in AdminShell.DataSpecificationIEC61360.DataTypeNames)
            {
                if (lowerType.Contains(aasType.ToLower()))
                    return aasType;
            }
            return string.Empty;
        }

        private static string GetValueType(string dataType)
        {
            switch (dataType)
            {
                case "STRING":
                case "STRING_TRANSLATABLE":
                    return "string";
                case "REAL_MEASURE":
                case "REAL_COUNT":
                case "REAL_CURRENCY":
                    return "double"; // TODO: float?
                case "INTEGER_MEASURE":
                case "INTEGER_COUNT":
                case "INTEGER_CURRENCY":
                    return "int";
                case "BOOLEAN":
                    return "boolean";
                case "URL":
                case "RATIONAL":
                case "RATIONAL_MEASURE":
                    return "string";
                case "TIME":
                case "TIMESTAMP":
                    return "time";
                case "DATE":
                    return "date";
            }
            return string.Empty;
        }
    }

    internal static class Extensions
    {
        public static string FirstValue(this IEnumerable<XElement> elements)
            => elements.Values().DefaultIfEmpty("").First();

        public static string FirstValue(this IEnumerable<XAttribute> attributes)
            => attributes.Values().DefaultIfEmpty("").First();

        public static IEnumerable<string> Values(this IEnumerable<XElement> elements)
            => elements.Select(e => e.Value);

        public static IEnumerable<string> Values(this IEnumerable<XAttribute> attributes)
            => attributes.Select(a => a.Value);
    }
}
