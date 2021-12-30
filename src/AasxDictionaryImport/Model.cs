/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdminShellNS;

namespace AasxDictionaryImport.Model
{
    /// <summary>
    /// The generic data model for structure imports.  The core of this data model is a tree of elements that could be
    /// imported into the AAS environment (IElement).  The top-level nodes of this tree typically correspond to
    /// submodels in the AAS.  These elements are stored in a data context (IDataContext) that can be retrieved using a
    /// data source (IDataSource), for example a directory with export files.  A data provider (IDataProvider) can be
    /// used to open data sources.  There should be one IDataProvider implementation per data repository (e. g. IEC CDD
    /// or ECLASS), but there could be multiple IDataSource implementations -- for example one for accessing paths on
    /// the file system and one for retrieving data from the network.
    /// </summary>
    // ReSharper disable once UnusedType.Global
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Provides access to a data repository, for example IEC CDD or ECLASS.  Implementations of this interface can be
    /// used to access IDataSource instances that store the actual data structures.
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// The name of the data provider, suitable for the user interface.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The prompt to use in the user interface when asking the user for the query string for fetching online data
        /// using the <see cref="Fetch"/> method.
        /// </summary>
        string FetchPrompt { get; }

        /// <summary>
        /// Whether this data provider supports fetching data from the network based on a search string provided by the
        /// user.
        /// </summary>
        bool IsFetchSupported { get; }

        /// <summary>
        /// Returns a list of all default data sources for this provider, i. e. all data sources that have been shipped
        /// with the AASX Package Explorer or that are freely available on the internet.
        /// </summary>
        /// <param name="dir">The search path for the default data sources, e. g. the current working directory</param>
        /// <returns>A list of all default data sources</returns>
        IEnumerable<IDataSource> FindDefaultDataSources(string dir);

        /// <summary>
        /// Checks whether the given path contains valid data that can be read by this data provider.  This method
        /// should never throw an exception.  If this method returns true, the data source at the given path can be
        /// opened using the <see cref="OpenPath"/> method.
        /// </summary>
        /// <param name="path">The path of the data to check</param>
        /// <returns>true if the path is a valid data source for this provider</returns>
        bool IsValidPath(string path);

        /// <summary>
        /// Creates a new data source that reads the data stored at the given path.  If <see cref="IsValidPath"/>
        /// returns true for this path, this method will always succeed.  Otherwise it will throw an exception.
        /// </summary>
        /// <param name="path">The path of the data set to open</param>
        /// <param name="type">Type</param>
        /// <returns>A data source that reads from the given path</returns>
        /// <exception cref="ImportException">If the path could not be accessed or does not contain valid data for this
        /// data provider</exception>
        IDataSource OpenPath(string path, Model.DataSourceType type = Model.DataSourceType.Custom);

        /// <summary>
        /// Fetch data from this data provider from the network using the given query string provided by the user.
        /// This method will only work if <see cref="IsFetchSupported"/> is true.  Otherwise it will throw an
        /// ImportException.
        /// </summary>
        /// <param name="query">The query string provided by the user</param>
        /// <returns>A data source loaded from the network using the given query string</returns>
        /// <exception cref="ImportException">If the data cannot be retrieved from the network or if this data provider
        /// does not support fetching data from the network</exception>
        IDataSource Fetch(string query);
    }

    /// <summary>
    /// Provides access to a data set extracted from a data provider.
    /// </summary>
    public interface IDataSource : IEquatable<IDataSource>
    {
        /// <summary>
        /// The data provider for this data set.
        /// </summary>
        IDataProvider DataProvider { get; }

        /// <summary>
        /// The name of this data source.  For a file, this should be the file name.  For a directory, this should be
        /// the name of the top-level directory.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The type of this data source.
        /// </summary>
        DataSourceType Type { get; }

        /// <summary>
        /// Loads and returns the data stored in this data sources.  This method should cache the loaded data.
        /// </summary>
        /// <returns>The data stored in this data source</returns>
        /// <exception cref="ImportException">If the data source could not be accessed or does not contain valid
        /// data</exception>
        IDataContext Load(); // cache this
    }

    /// <summary>
    /// The type of a data source.
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>
        /// A source in the default search path, i. e. that has been shipped with the AASX Package Explorer.
        /// </summary>
        Default,
        /// <summary>
        /// A custom data source, usually provided and selected by the user.
        /// </summary>
        Custom,
        /// <summary>
        /// An export that has been performed directly by the AASX Package Explorer using the network.
        /// </summary>
        Online,
    }

    /// <summary>
    /// A collection of elements stored in a data set as a tree.  The LoadSubmodels and LoadSubmodelElements methods can
    /// be used to retrieve the elements stored in this data set.  If the data source contains undefined references,
    /// they will be ignored and recorded in the UnknownReferences collection.
    /// </summary>
    public interface IDataContext
    {
        /// <summary>
        /// A collection of unknown references.  If an element in this data set references another element that is not
        /// part of the data set, the reference is ignored and recorded in this collection.
        /// </summary>
        ICollection<Model.UnknownReference> UnknownReferences { get; }

        /// <summary>
        /// Loads and returns those elements that correspond to an AAS submodel, for example IEC CDD top-level classes.
        /// </summary>
        /// <returns>A collection of elements corresponding to AAS submodels</returns>
        ICollection<IElement> LoadSubmodels();

        /// <summary>
        /// Loads and returns those elements that correspond to AAS submodel elements, for example IEC CDD classes and
        /// properties.
        /// </summary>
        /// <returns>A collection of elements corresponding to AAS submodel elements</returns>
        ICollection<IElement> LoadSubmodelElements();
    }

    /// <summary>
    /// An element of a data set, identified by a unique ID.  The format of the ID depends on the data source.  To
    /// examine the next level of the tree, use the Children property.  Implementations of this interface are free to
    /// use lazy loading for the children, but should cache the results.
    /// <para>
    /// Elements can be converted into AAS submodels using the ImportSubmodelInto method.  Implementations should
    /// respect the value of the IsSelected property, both for this element and for all children.
    /// </para>
    /// </summary>
    public interface IElement
    {
        /// <summary>
        /// The data source for this element.
        /// </summary>
        IDataSource DataSource { get; }

        /// <summary>
        /// The ID of this element.  This should be a unique ID.  The format of the ID depends on the data source.
        /// Typically, an IRDI is used.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name of this element, usually in English.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The display name of this element, usually in English.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The parent of this element or null if the element is a root element of the element tree.
        /// </summary>
        IElement? Parent { get; }

        /// <summary>
        /// The list of children of this element.  Implementations may use lazy loading for this list but should cache
        /// the result.
        /// </summary>
        ICollection<IElement> Children { get; }

        /// <summary>
        /// Stores whether the user selected this element for the import.  If this is false, this element should be
        /// ignored during the import.
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Checks whether all parts of the given query match this element.  Implementations should at least check the
        /// name and the ID and may also check other attributes.
        /// </summary>
        /// <param name="queryParts">A list of query strings</param>
        /// <returns>true if all query strings match this element</returns>
        bool Match(IEnumerable<string> queryParts);

        /// <summary>
        /// Converts this element into an AAS submodel and adds it to the given admin shell.  If this element cannot be
        /// converted into a submodel, this method returns false.
        /// <para>
        /// Implementations of this method should check the IsSelected attribute both for this element and for all
        /// children.  Only those elements with IsSelected equals true should be imported into the admin shell.
        /// </para>
        /// </summary>
        /// <param name="env">The admin shell environment for the import</param>
        /// <param name="adminShell">The admin shell to add the submodel to</param>
        /// <returns>true if the import was successful, or false if the import failed or if this element cannot be
        /// converted to an AAS submodel</returns>
        bool ImportSubmodelInto(AdminShell.AdministrationShellEnv env,
            AdminShell.AdministrationShell adminShell);

        /// <summary>
        /// Converts this element into a AAS submodel element (i. e. a property or a collection) and adds it to the
        /// given parent element (typically a submodel).  If this element cannot be converted into a submodel element,
        /// this method returns false.
        /// <para>
        /// Implementations of this method should check the IsSelected attribute both for this element and for all
        /// children.  Only those elements with IsSelected equals true should be imported.
        /// </para>
        /// </summary>
        /// <param name="env">The admin shell environment for the import</param>
        /// <param name="parent">The parent element to add the submodel elements to</param>
        /// <returns>true if the import was successful, or false if the import failed or
        /// if this element cannot be converted to an AAS submodel element</returns>
        bool ImportSubmodelElementsInto(AdminShell.AdministrationShellEnv env,
            AdminShell.IManageSubmodelElements parent);

        /// <summary>
        /// Returns all detail information for this element, suitable for the user interface.  The keys of the returned
        /// dictionary are the name of the attribute, the values are the attribute values.  This dictionary should also
        /// include the ID and the name, typically as the first elements.
        /// </summary>
        /// <returns>The detail information for this element</returns>
        Dictionary<string, string> GetDetails();

        /// <summary>
        /// Returns a URL that points to a web page with more information for this element (if available).
        /// </summary>
        /// <returns>A URL with more information for this element or null</returns>
        Uri? GetDetailsUrl();
    }

    /// <summary>
    /// A reference to an element that could not be resolved within a data set.
    /// </summary>
    public class UnknownReference : IEquatable<UnknownReference>
    {
        /// <summary>
        /// The ID of the referenced element.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The expected type of the referenced element, typically a class name.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Creates a new UnknownReference object.
        /// </summary>
        /// <param name="id">The ID of the reference element</param>
        /// <param name="type">The expected type of the referenced element</param>
        public UnknownReference(string id, string type)
        {
            Id = id;
            Type = type;
        }

        /// <inheritdoc/>
        public bool Equals(UnknownReference? reference)
        {
            return reference != null &&
                Id.Equals(reference.Id) &&
                Type.Equals(reference.Type);
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            return Equals(o as UnknownReference);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + Type.GetHashCode();
                return hash;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Type}<{Id}>";
        }

        /// <summary>
        /// Creates a new UnknownReference object.
        /// </summary>
        /// <typeparam name="T">The expected type of the referenced element</typeparam>
        /// <param name="id">The ID of the referenced element</param>
        /// <returns>A new UnknownReference object with the given data</returns>
        public static UnknownReference Create<T>(string id)
        {
            return new UnknownReference(id, typeof(T).Name);
        }
    }

    /// <summary>
    /// Default implementation of the IDataProvider interface.
    /// </summary>
    public abstract class DataProviderBase : IDataProvider
    {
        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public virtual string FetchPrompt { get; } = string.Empty;

        /// <inheritdoc/>
        public virtual bool IsFetchSupported { get; } = false;

        /// <inheritdoc/>
        public virtual IEnumerable<IDataSource> FindDefaultDataSources(string dir)
        {
            return GetDefaultPaths(dir)
                .Where(IsValidPath)
                .Select(p => OpenPath(p, Model.DataSourceType.Default))
                .ToList();
        }

        /// <inheritdoc/>
        public override string ToString() => Name;

        /// <inheritdoc/>
        public abstract bool IsValidPath(string path);

        /// <inheritdoc/>
        public virtual IDataSource Fetch(string query) =>
            throw new ImportException("Fetch not supported by this data provider");

        /// <summary>
        /// Returns all paths that could contain a default data source.
        /// </summary>
        /// <param name="dir">The search path for the default data sources, e. g. the current working directory</param>
        /// <returns>A list of all possible default data sources</returns>
        protected abstract IEnumerable<string> GetDefaultPaths(string dir);

        /// <summary>
        /// Creates a new data source that reads the data stored at the given path and sets the data source type to the
        /// given value.  If IsValidPath returns true for this path, this method will always succeed.  Otherwise it will
        /// throw an exception.
        /// </summary>
        /// <param name="path">The path of the data set to open</param>
        /// <param name="type">The type of the data source to open</param>
        /// <returns>A data source that reads from the given path</returns>
        /// <exception cref="ImportException">If the path could not be accessed or does not contain valid data for this
        /// data provider</exception>
        public abstract IDataSource OpenPath(string path, DataSourceType type = Model.DataSourceType.Custom);
    }

    /// <summary>
    /// Default implementation of the IElement interface.
    /// </summary>
    public abstract class ElementBase : IElement
    {
        /// <inheritdoc/>
        public virtual IDataSource DataSource { get; }

        /// <inheritdoc/>
        public abstract string Id { get; }

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public virtual string DisplayName => Name;

        /// <inheritdoc/>
        public virtual IElement? Parent { get; }

        /// <inheritdoc/>
        public abstract ICollection<IElement> Children { get; }

        /// <inheritdoc/>
        public bool IsSelected { get; set; } = true;

        protected ElementBase(IDataSource dataSource, IElement? parent = null)
        {
            DataSource = dataSource;
            Parent = parent;
        }

        /// <inheritdoc/>
        public abstract Dictionary<string, string> GetDetails();

        /// <inheritdoc/>
        public virtual Uri? GetDetailsUrl() => null;

        /// <inheritdoc/>
        public virtual bool ImportSubmodelInto(AdminShell.AdministrationShellEnv env,
            AdminShell.AdministrationShell adminShell) => false;

        /// <inheritdoc/>
        public virtual bool ImportSubmodelElementsInto(AdminShell.AdministrationShellEnv env,
            AdminShell.IManageSubmodelElements parent) => false;

        /// <inheritdoc/>
        public virtual bool Match(IEnumerable<string> queryParts)
        {
            return queryParts.All(Match);
        }

        /// <summary>
        /// Check whether the given string matches this element.  Implementations should at least check the ID and the
        /// name and may also check other attributes.
        /// </summary>
        /// <param name="query">The string to match this element against</param>
        /// <returns>true if this element matches the given string, otherwise false</returns>
        protected virtual bool Match(string query)
        {
            return new[] { Id, Name }.Any(s => s.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }
    }

    /// <summary>
    /// Default implementation of the IElement interface that implements lazy loading for the element's children.
    /// </summary>
    public abstract class LazyElementBase : ElementBase
    {
        private ICollection<IElement>? _children;

        /// <inheritdoc/>
        public override ICollection<IElement> Children
        {
            get
            {
                _children ??= LoadChildren();
                return _children;
            }
        }

        protected LazyElementBase(IDataSource dataSource, IElement? parent = null)
            : base(dataSource, parent)
        {
        }

        protected virtual ICollection<IElement> LoadChildren() => new List<IElement>();
    }

    /// <summary>
    /// A data source that reads its data from a path on the file system.  If no name is specified, the file name
    /// component of the path is used.
    /// </summary>
    public abstract class FileSystemDataSource : IDataSource
    {
        /// <inheritdoc/>
        public IDataProvider DataProvider { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public DataSourceType Type { get; }

        /// <summary>
        /// The path used by this data source.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a new FileSystemDataSource object and uses the file name component of the path as its name.
        /// </summary>
        /// <param name="dataProvider">The data provider</param>
        /// <param name="path">The path of the directory</param>
        /// <param name="type">The type of this data source</param>
        /// <exception cref="ImportException">If the path cannot be accessed</exception>
        protected FileSystemDataSource(IDataProvider dataProvider, string path,
            DataSourceType type)
            : this(dataProvider, GetName(path), path, type)
        {
        }

        /// <summary>
        /// Creates a new FileSystemDataSource object.
        /// </summary>
        /// <param name="dataProvider">The data provider</param>
        /// <param name="name">The name of this data source</param>
        /// <param name="path">The path of this data source</param>
        /// <param name="type">The type of this data source</param>
        protected FileSystemDataSource(IDataProvider dataProvider, string name,
            string path, DataSourceType type)
        {
            DataProvider = dataProvider;
            Name = name;
            Type = type;
            Path = path;
        }

        /// <inheritdoc/>
        public abstract IDataContext Load();

        /// <inheritdoc/>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(DataProvider);
            stringBuilder.Append(": ");
            stringBuilder.Append(Name);
            switch (Type)
            {
                case DataSourceType.Custom:
                    stringBuilder.Append(" [");
                    stringBuilder.Append(System.IO.Path.GetDirectoryName(Path));
                    stringBuilder.Append("]");
                    break;
                case DataSourceType.Online:
                    stringBuilder.Append(" [online]");
                    break;
                case DataSourceType.Default:
                    break;
            }
            return stringBuilder.ToString();
        }

        /// <inheritdoc/>
        public bool Equals(IDataSource? source)
        {
            if (!(source is FileSystemDataSource ds))
                return false;
            return DataProvider.Equals(ds.DataProvider) && Path.Equals(ds.Path);
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            return Equals(o as IDataSource);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + DataProvider.GetHashCode();
                hash = hash * 23 + Path.GetHashCode();
                return hash;
            }
        }

        private static string GetName(string path)
        {
            try
            {
                return System.IO.Path.GetFileName(path);
            }
            catch (ArgumentException ex)
            {
                throw new ImportException($"Could not get file name for path '{path}'", ex);
            }
        }
    }

    /// <summary>
    /// An exception that occured during the import.  IDataProvider implementors should create subclasses for all types
    /// of exceptions that could occur during the import.
    /// </summary>
    public class ImportException : Exception
    {
        public ImportException() : base()
        {
        }

        public ImportException(string message) : base(message)
        {
        }

        public ImportException(string message, Exception exception) : base(message, exception)
        {
        }
    }
}
