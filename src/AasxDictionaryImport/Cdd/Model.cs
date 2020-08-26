/*
 * Copyright (c) 2020 SICK AG <info@sick.de>
 *
 * This software is licensed under the Apache License 2.0 (Apache-2.0).
 * The ExcelDataReder dependency is licensed under the MIT license
 * (https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/LICENSE).
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using AdminShellNS;

namespace AasxDictionaryImport.Cdd
{
    /// <summary>
    /// A data provider for the IEC CDD Database.  The database stores classes and properties that can be mapped to AAS
    /// submodels, collections and properties.  The data is typically stored as a set of XLS files.
    /// </summary>
    public class DataProvider : Model.DataProviderBase
    {
        /// <inheritdoc/>
        public override string Name => "IEC CDD";

        /// <inheritdoc/>
        public override bool IsValidPath(string path) => Parser.IsValidDirectory(path);

        /// <inheritdoc/>
        protected override IEnumerable<string> GetDefaultPaths()
        {
            var searchDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "iec-cdd");
            if (!System.IO.Directory.Exists(searchDirectory))
                return new List<string>();
            return System.IO.Directory.GetDirectories(searchDirectory);
        }

        /// <inheritdoc/>
        protected override Model.IDataSource OpenPath(string path, Model.DataSourceType type)
            => new DataSource(this, path, type);
    }

    /// <summary>
    /// An IEC CDD data source.  The data is read from a directory that contains a set of XLS files exported from the
    /// IEC CDD.  For more information on the format of these files, see the Parser class.
    /// </summary>
    public class DataSource : Model.FileSystemDataSource
    {
        private Context? _context;

        /// <summary>
        /// Creates a new IEC CDD DataSource object.
        /// </summary>
        /// <param name="dataProvider">The IEC CDD data provider</param>
        /// <param name="directory">The directory to read the data from</param>
        /// <param name="type">The type of the data source</param>
        /// <exception cref="Model.ImportException">If the given directory does not exist</exception>
        public DataSource(Model.IDataProvider dataProvider, string directory, Model.DataSourceType type)
            : base(dataProvider, directory, type)
        {
            if (!System.IO.Directory.Exists(directory))
                throw new Model.ImportException($"Directory '{directory}' does not exist");
        }

        /// <inheritdoc/>
        public override Model.IDataContext Load()
        {
            _context ??= new Parser().Parse(this);
            return _context;
        }
    }

    /// <summary>
    /// An IEC CDD data context.  This class stores a dictionary with all elements (using the code as the key), as well
    /// as a list of all top-level classes.
    /// </summary>
    public class Context : Model.IDataContext
    {
        private readonly IDictionary<string, Element> _elements = new Dictionary<string, Element>();

        /// <summary>
        /// The data source this data has been read from.
        /// </summary>
        public DataSource DataSource { get; }

        /// <summary>
        /// The top-level classes in this data context.
        /// </summary>
        public IEnumerable<Class> Classes { get; }

        /// <inheritdoc/>
        public ICollection<Model.UnknownReference> UnknownReferences { get; } = new List<Model.UnknownReference>();

        /// <summary>
        /// Creates a new IEC CDD Context.
        /// </summary>
        /// <param name="dataSource">The data source for this context</param>
        /// <param name="classes">The classes stored in the data source</param>
        /// <param name="properties">The properties stored in the data source</param>
        public Context(DataSource dataSource, List<Class> classes, List<Property> properties)
        {
            DataSource = dataSource;
            Classes = classes;

            AddElements(classes);
            AddElements(properties);
        }

        /// <summary>
        /// Returns the element with the given code or null if the element could not be found.
        /// </summary>
        /// <typeparam name="T">The type of the element to retrieve</typeparam>
        /// <param name="key">The code of thje element to retrieve</param>
        /// <returns>The requested element or null if the element could not be found</returns>
        public T? GetElement<T>(string key) where T : Element
        {
            if (key.Length == 0)
                return null;

            if (!_elements.TryGetValue(key, out Element element))
            {
                UnknownReferences.Add(Model.UnknownReference.Create<T>(key));
                return null;
            }

            return element as T;
        }

        private void AddElements<T>(IEnumerable<T> elements) where T : Element
        {
            foreach (var element in elements)
            {
                if (!_elements.ContainsKey(element.Code))
                    _elements.Add(element.Code, element);
            }
        }

        /// <inheritdoc/>
        public ICollection<Model.IElement> LoadSubmodels()
        {
            return Classes.Select(cls => new ClassWrapper(this, cls)).ToList<Model.IElement>();
        }
    }

    /// <summary>
    /// An element in the IEC CDD data structure.  This is a high-level view of the actual IEC CDD elements stored in
    /// the types defined in the Data.cs file.  The reason for this separation is that some properties may be reference
    /// to classes or aggregate types.  This additional level of indirection should not be shown in the user interface.
    /// The wrapper classes make it easier to handle these special cases.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped element</typeparam>
    public abstract class ElementWrapper<T> : Model.LazyElementBase where T : Element
    {
        /// <summary>
        /// The current data context.
        /// </summary>
        public Context Context { get; }

        /// <summary>
        /// The wrapped element.
        /// </summary>
        public T Element { get; }

        /// <inheritdoc/>
        public override string Id => Element.Code;

        /// <inheritdoc/>
        public override string Name => Element.PreferredName.GetDefault();

        /// <summary>
        /// Creates a new ElementWrapper for an IEC CDD element.
        /// </summary>
        /// <param name="context">The current data context</param>
        /// <param name="element">The wrapped IEC CDD element</param>
        /// <param name="parent">The parent element, or null if this is a top-level element</param>
        protected ElementWrapper(Context context, T element, Model.IElement? parent = null)
            : base(context.DataSource, parent)
        {
            Context = context;
            Element = element;
        }

        /// <inheritdoc/>
        public override Dictionary<string, string> GetDetails()
        {
            return new Dictionary<string, string>
            {
                { "Code", Element.Code },
                { "Version", Element.Version },
                { "Revision", Element.Revision },
                { "Preferred Name", Element.PreferredName.GetDefault() },
                { "Definition", Element.Definition.GetDefault() }
            };
        }

        /// <inheritdoc/>
        public override Uri? GetDetailsUrl() => Element.GetDetailsUrl();

        /// <inheritdoc/>
        public override string ToString() => Id;

        protected ICollection<Model.IElement> LoadChildren(Class cls)
        {
            return cls.GetAllProperties(Context).Get(Context)
                .Select(p => new PropertyWrapper(Context, p, this))
                .ToList<Model.IElement>();
        }
    }

    /// <summary>
    /// A wrapper for an IEC CDD class.  See the ElementWrapper type for more information.
    /// </summary>
    public class ClassWrapper : ElementWrapper<Class>
    {
        /// <summary>
        /// Creates a new ClassWrapper for an IEC CDD class.
        /// </summary>
        /// <param name="context">The current data context</param>
        /// <param name="cls">The wrapped IEC CDD class</param>
        /// <param name="parent">The parent element, or null if this is a top-level element</param>
        public ClassWrapper(Context context, Class cls, Model.IElement? parent = null)
            : base(context, cls, parent)
        {
        }

        /// <inheritdoc/>
        public override bool ImportSubmodelInto(AdminShellV20.AdministrationShellEnv env,
           AdminShellV20.AdministrationShell adminShell)
        {
            return new Importer(env, Context).ImportSubmodel(this, adminShell);
        }

        /// <inheritdoc/>
        public override Dictionary<string, string> GetDetails()
        {
            var dict = base.GetDetails();
            dict.Add("Definition Source", Element.DefinitionSource);
            dict.Add("Superclass", Element.Superclass.Code);
            return dict;
        }

        protected override ICollection<Model.IElement> LoadChildren() => LoadChildren(Element);
    }

    /// <summary>
    /// A wrapper for an IEC CDD property.  See the ElementWrapper type for more information.
    /// </summary>
    public class PropertyWrapper : ElementWrapper<Property>
    {
        private readonly Class? _referenceClass;

        /// <inheritdoc/>
        public override string Id
        {
            get
            {
                if (_referenceClass != null)
                    return _referenceClass.Code;
                return base.Id;
            }
        }

        /// <inheritdoc/>
        public override string Name
        {
            get
            {
                if (_referenceClass != null)
                    return _referenceClass.PreferredName.GetDefault();
                return base.Name;
            }
        }

        /// <summary>
        /// Creates a new PropertyWrapper for an IEC CDD property.
        /// </summary>
        /// <param name="context">The current data context</param>
        /// <param name="property">The wrapped IEC CDD property</param>
        /// <param name="parent">The parent element</param>
        public PropertyWrapper(Context context, Property property, Model.IElement parent)
            : base(context, property, parent)
        {
            Reference<Class>? reference = property.DataType.GetClassReference();
            if (reference != null)
                _referenceClass = reference.Get(context);
        }

        /// <inheritdoc/>
        public override Dictionary<string, string> GetDetails()
        {
            var dict = base.GetDetails();
            dict.Add("Definition Source", Element.DefinitionSource);
            dict.Add("Short Name", Element.ShortName.GetDefault());
            dict.Add("Symbol", Element.Symbol);
            dict.Add("Primary Unit", Element.PrimaryUnit);
            dict.Add("Unit Code", Element.UnitCode);
            dict.Add("Data Type", Element.RawDataType);
            dict.Add("Format", Element.Format);
            return dict;
        }

        protected override ICollection<Model.IElement> LoadChildren()
        {
            if (_referenceClass != null)
                return LoadChildren(_referenceClass);

            var children = new List<Model.IElement>();
            if (Element.DataType is AggregateType aggregateType)
            {
                var subProperty = Element.ReplaceDataType(aggregateType.Subtype);
                children.Add(new PropertyWrapper(Context, subProperty, this));
            }
            return children;
        }
    }
}
