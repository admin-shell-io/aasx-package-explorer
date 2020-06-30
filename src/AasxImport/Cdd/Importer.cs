// Copyright (C) 2020 Robin Krahl, RD/ESR, SICK AG <robin.krahl@sick.de>
// This software is licensed under the Apache License 2.0 (Apache-2.0).

#nullable enable

using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AasxImport.Cdd
{
    /// <summary>
    /// Converts a IEC CDD class into a AAS submodel and imports it into an administration shell.
    /// <para>
    /// The importer performs the following mappings (IEC CDD to AAS):
    /// <code>
    /// Class    --> Submodel (top-level)
    ///              SubmodelElementCollection (property with CLASS_REFERENCE/CLASS_INSTANCE)
    /// Property --> SubmodelElement (default)
    ///              SubmodelElementCollection (aggregate or level type)
    /// </code>
    /// It also generates concept descriptions using the IEC CDD metadata.
    /// </para>
    /// </summary>
    internal class Importer
    {
        private readonly AdminShellV20.AdministrationShellEnv _env;
        private readonly Context _context;
        private readonly bool _all;

        /// <summary>
        /// Creates a new IEC CDD Importer.
        /// </summary>
        /// <param name="env">The environment to import the data into</param>
        /// <param name="context">The data context of the IEC CDD data</param>
        public Importer(AdminShellV20.AdministrationShellEnv env, Context context)
        {
            _env = env;
            _context = context;
            _all = context.DataSource.ImportAllAttributes;
        }

        /// <summary>
        /// Import the given IEC CDD class as a submodel into the given admin shell.
        /// </summary>
        /// <param name="cls">The IEC CDD class to import</param>
        /// <param name="adminShell">The admin shell to import the submodel into</param>
        /// <returns>true if the class was imported successfully</returns>
        public bool ImportSubmodel(ClassWrapper cls, AdminShellV20.AdministrationShell adminShell)
        {
            if (!cls.IsSelected)
                return false;

            var submodel = Iec61360Utils.CreateSubmodel(_env, adminShell, cls.Element.GetIec61360Data(_all));
            AddProperties(submodel, cls.Children);
            return true;
        }

        /// <summary>
        /// Import the given IEC CDD element as a submodel element into the given parent
        /// element.
        /// </summary>
        /// <param name="element">The IEC CDD element to import</param>
        /// <param name="parent">The parent element to import the submodel into</param>
        /// <returns>true if the class was imported successfully</returns>
        public bool ImportSubmodelElements(Model.IElement element, AdminShell.IManageSubmodelElements parent)
        {
            if (!element.IsSelected)
                return false;

            var submodelElement = CreateSubmodelElement(element);
            if (submodelElement != null)
            {
                parent.Add(submodelElement);
                return true;
            }

            return false;
        }

        private void AddProperties<T>(T elements, IEnumerable<Model.IElement> properties)
            where T : AdminShellV20.IManageSubmodelElements
        {
            foreach (var property in properties)
            {
                if (!property.IsSelected)
                    continue;

                var element = CreateSubmodelElement(property);
                if (element != null)
                    elements.Add(element);
            }
        }

        private AdminShellV20.SubmodelElement? CreateSubmodelElement(Model.IElement e)
        {
            if (e is ClassWrapper cls)
                return CreatePropertyCollection(cls.Element, cls.Children);
            else if (e is PropertyWrapper property)
                return CreatePropertySubmodelElement(property);
            return null;
        }

        private AdminShellV20.SubmodelElementCollection CreatePropertyCollection(Class cls,
            IEnumerable<Model.IElement> properties)
        {
            var collection = Iec61360Utils.CreateCollection(_env, cls.GetIec61360Data(_all));
            AddProperties(collection, properties);
            return collection;
        }

        private AdminShellV20.SubmodelElement? CreatePropertySubmodelElement(PropertyWrapper wrapper)
        {
            var reference = wrapper.Element.DataType.GetClassReference();
            if (reference != null)
            {
                var cls = reference.Get(_context);
                if (cls != null)
                    return CreatePropertyCollection(cls, wrapper.Children);
            }
            else if (wrapper.Element.DataType is AggregateType aggregateType)
                return CreateAggregateCollection(wrapper, aggregateType);
            else if (wrapper.Element.DataType is LevelType levelType)
                return CreateLevelCollection(wrapper.Element, levelType);
            return CreateProperty(wrapper.Element);
        }

        private AdminShellV20.SubmodelElementCollection CreateAggregateCollection(
            PropertyWrapper wrapper, AggregateType aggregateType)
        {
            var collection = Iec61360Utils.CreateCollection(_env, wrapper.Element.GetIec61360Data(_all));

            // TODO: clone base element instead of parsing the element multiple times
            if (wrapper.Children.Count == 1)
            {
                var child = wrapper.Children.First();
                if (child.IsSelected)
                {
                    for (int i = 0; i < Math.Max(1, aggregateType.MinimumElementCount); i++)
                    {
                        var element = CreateSubmodelElement(child);
                        if (element != null)
                        {
                            element.idShort += i;
                            collection.Add(element);
                        }
                    }
                }
            }

            return collection;
        }

        private AdminShellV20.SubmodelElementCollection CreateLevelCollection(Property property, LevelType levelType)
        {
            var data = property.GetIec61360Data(_all);
            var collection = Iec61360Utils.CreateCollection(_env, data);

            foreach (var levelValue in levelType.Types)
            {
                var p = CreateLevelProperty(data, levelType, levelValue);
                collection.Add(p);
            }

            return collection;
        }

        private AdminShellV20.Property CreateProperty(Property property)
        {
            return Iec61360Utils.CreateProperty(_env, property.GetIec61360Data(_all),
                GetValueType(property.DataType));
        }

        private AdminShellV20.Property CreateLevelProperty(Iec61360Data data, LevelType levelType,
            LevelType.Type levelValue)
        {
            // idShort for the level property: <Level><Property>, e. g. MinimumOperatingTemperature,
            // MaximumOperatingTemperature
            var idShort = levelValue.ToString() + data.IdShort;
            return new AdminShellV20.Property()
            {
                idShort = idShort,
                kind = AdminShellV20.ModelingKind.CreateAsInstance(),
                valueType = GetValueType(levelType.Subtype),
            };
        }

        private static string GetValueType(DataType type)
        {
            if (type is SimpleType simpleType)
            {
                return SimpleTypeToValueType(simpleType);
            }
            else if (type is EnumType enumType)
            {
                return EnumTypeToValueType(enumType);
            }
            else if (type is AggregateType || type is ClassInstanceType || type is ClassReferenceType
                || type is LevelType)
            {
                // With these data types, we should never end up in this method as we should create a collection
                // instead.
                // TODO: Log error?
            }
            else if (type is UnknownType)
            {
                // TODO: Log error?
            }
            else
            {
                // TODO: implement:
                // LargeObjectType, PlacementType
            }
            return String.Empty;
        }

        private static string SimpleTypeToValueType(SimpleType simpleType)
        {
            switch (simpleType.TypeValue)
            {
                case SimpleType.Type.Boolean:
                    return "boolean";
                case SimpleType.Type.Date:
                    return "date";
                case SimpleType.Type.DateTime:
                    return "dateTime";
                case SimpleType.Type.Int:
                case SimpleType.Type.IntCurrency:
                case SimpleType.Type.IntMeasure:
                    return "int";
                case SimpleType.Type.Real:
                case SimpleType.Type.RealCurrency:
                case SimpleType.Type.RealMeasure:
                    return "double"; // TODO: or float?
                case SimpleType.Type.String:
                    return "string";
                case SimpleType.Type.Time:
                    return "time";
                case SimpleType.Type.Binary:
                case SimpleType.Type.Html5:
                case SimpleType.Type.Icid:
                case SimpleType.Type.Irdi:
                case SimpleType.Type.Iso29002Irdi:
                case SimpleType.Type.NonTranslatableString:
                case SimpleType.Type.Number:
                case SimpleType.Type.RationalMeasure:
                case SimpleType.Type.Rational:
                case SimpleType.Type.TranslatableString:
                case SimpleType.Type.Uri:
                    return "string";
            }
            return String.Empty;
        }

        private static string EnumTypeToValueType(EnumType enumType)
        {
            switch (enumType.TypeValue)
            {
                case EnumType.Type.Boolean:
                    return "boolean";
                case EnumType.Type.Code:
                case EnumType.Type.String:
                    return "string";
                case EnumType.Type.Int:
                    return "int";
                case EnumType.Type.Instance:
                case EnumType.Type.Rational:
                case EnumType.Type.Real:
                case EnumType.Type.Reference:
                    return "string";
            }
            return String.Empty;
        }
    }
}
