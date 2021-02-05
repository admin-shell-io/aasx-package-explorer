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
using System.IO;
using System.Linq;
using AasxDictionaryImport.Model;
using AasxDictionaryImport.Tests;
using NUnit.Framework;

namespace AasxDictionaryImport.Cdd.Tests
{
    // Assert.Throws is missing the correct InstantHandle annotation so we have to ignore the AccessToDispoedClosure
    // warning, see https://youtrack.jetbrains.com/issue/RSRP-316495
    // ReSharper disable AccessToDisposedClosure
    // ReSharper disable UnusedType.Global
    public class Test_DataProvider : CddTest
    {
        [Test]
        public void Name()
        {
            Assert.AreEqual(new DataProvider().Name, "IEC CDD");
            Assert.AreEqual(new DataProvider().ToString(), "IEC CDD");
        }

        public class IsValidPath
        {
            [Test]
            public void EmptyPath()
            {
                var dataProvider = new DataProvider();
                Assert.That(!dataProvider.IsValidPath(""));
                Assert.That(() => dataProvider.OpenPath("").Load(), Throws.InstanceOf<Model.ImportException>());
            }

            [Test]
            public void MissingDirectory()
            {
                using var tempDir = TempDir.Generate();
                var dataProvider = new DataProvider();

                Assert.That(!dataProvider.IsValidPath(tempDir));
                Assert.That(() => dataProvider.OpenPath(tempDir).Load(), Throws.InstanceOf<Model.ImportException>());
            }

            [Test]
            public void EmptyDirectory()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                Assert.That(!dataProvider.IsValidPath(tempDir));
                Assert.That(() => dataProvider.OpenPath(tempDir).Load(), Throws.InstanceOf<Model.ImportException>());
            }

            [Test]
            public void ValidDirectory()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                CreateEmptyXls(tempDir, GetExportFileName("class", "12345"));
                CreateEmptyXls(tempDir, GetExportFileName("property", "12345"));
                Assert.That(dataProvider.IsValidPath(tempDir));
                Assert.That(() => dataProvider.OpenPath(tempDir).Load(), Throws.Nothing);
            }

            [Test]
            public void MissingRequiredFiles()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                CreateEmptyXls(tempDir, GetExportFileName("CLASS", "12345"));
                Assert.That(!dataProvider.IsValidPath(tempDir));
                Assert.That(() => dataProvider.OpenPath(tempDir).Load(),
                    Throws.TypeOf<MissingExportFilesException>());
            }

            [Test]
            public void MultipleRequiredFiles()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                CreateEmptyXls(tempDir, GetExportFileName("class", "12345"));
                CreateEmptyXls(tempDir, GetExportFileName("property", "12345"));
                CreateEmptyXls(tempDir, GetExportFileName("class", "67890"));
                Assert.That(!dataProvider.IsValidPath(tempDir));
                Assert.That(() => dataProvider.OpenPath(tempDir).Load(),
                    Throws.TypeOf<MultipleExportFilesException>());
            }
        }

        [Test]
        public void OpenPath()
        {
            // failures are already tested in IsValidPath, so we only have to deal with valid paths
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            CreateEmptyXls(tempDir, GetExportFileName("class", "12345"));
            CreateEmptyXls(tempDir, GetExportFileName("property", "12345"));

            Assert.That(dataProvider.IsValidPath(tempDir.Path));
            Assert.That(dataProvider.OpenPath(tempDir.Path),
                Is.EqualTo(new DataSource(dataProvider, tempDir.Path, Model.DataSourceType.Custom)));
        }

        public class GetDefaultPaths
        {
            [Test]
            public void MissingDirectory()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                Assert.That(dataProvider.FindDefaultDataSources(tempDir), Is.Empty);
            }

            [Test]
            public void EmptyDirectory()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                var iecCddPath = Path.Combine(tempDir, "iec-cdd");
                Directory.CreateDirectory(iecCddPath);

                Assert.That(dataProvider.FindDefaultDataSources(tempDir), Is.Empty);
            }

            [Test]
            public void EmptySource()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                var iecCddPath = Path.Combine(tempDir, "iec-cdd");
                Directory.CreateDirectory(iecCddPath);

                using var sourcePath = TempDir.Create(iecCddPath);

                Assert.That(dataProvider.FindDefaultDataSources(tempDir), Is.Empty);
            }

            [Test]
            public void IncompleteSource()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                var iecCddPath = Path.Combine(tempDir, "iec-cdd");
                Directory.CreateDirectory(iecCddPath);

                using var source = TempDir.Create(iecCddPath);
                CreateEmptyXls(source, GetExportFileName("class", "12345"));

                Assert.That(dataProvider.FindDefaultDataSources(tempDir), Is.Empty);
            }

            [Test]
            public void ValidSource()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                var iecCddPath = Path.Combine(tempDir, "iec-cdd");
                Directory.CreateDirectory(iecCddPath);

                using var source = TempDir.Create(iecCddPath);
                CreateEmptyXls(source, GetExportFileName("class", "12345"));
                CreateEmptyXls(source, GetExportFileName("property", "12345"));

                Assert.That(dataProvider.FindDefaultDataSources(tempDir), Is.EquivalentTo(new[] {
                    new DataSource(dataProvider, source, Model.DataSourceType.Default),
                }));
            }

            [Test]
            public void TwoValidSources()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                var iecCddPath = Path.Combine(tempDir, "iec-cdd");
                Directory.CreateDirectory(iecCddPath);

                using var source1 = TempDir.Create(iecCddPath);
                CreateEmptyXls(source1, GetExportFileName("class", "12345"));
                CreateEmptyXls(source1, GetExportFileName("property", "12345"));

                using var source2 = TempDir.Create(iecCddPath);
                CreateEmptyXls(source2, GetExportFileName("class", "67890"));
                CreateEmptyXls(source2, GetExportFileName("property", "67890"));

                Assert.That(dataProvider.FindDefaultDataSources(tempDir), Is.EquivalentTo(new[] {
                    new DataSource(dataProvider, source1, Model.DataSourceType.Default),
                    new DataSource(dataProvider, source2, Model.DataSourceType.Default),
                }));
            }

            [Test]
            public void OneValidOneInvalidSource()
            {
                using var tempDir = TempDir.Create();
                var dataProvider = new DataProvider();

                var iecCddPath = Path.Combine(tempDir, "iec-cdd");
                Directory.CreateDirectory(iecCddPath);

                using var source1 = TempDir.Create(iecCddPath);
                CreateEmptyXls(source1, GetExportFileName("class", "12345"));
                CreateEmptyXls(source1, GetExportFileName("property", "12345"));

                using var source2 = TempDir.Create(iecCddPath);
                CreateEmptyXls(source2, GetExportFileName("class", "67890"));
                CreateEmptyXls(source2, GetExportFileName("property", "67890"));
                CreateEmptyXls(source2, GetExportFileName("class", "12345"));

                Assert.That(dataProvider.FindDefaultDataSources(tempDir), Is.EquivalentTo(new[] {
                    new DataSource(dataProvider, source1, Model.DataSourceType.Default),
                }));
            }
        }
    }

    public class Test_DataSource : CddTest
    {
        internal class UnimplementedDataSource : Model.IDataSource
        {
            public Model.IDataProvider DataProvider => throw new System.NotImplementedException();

            public string Name => throw new System.NotImplementedException();

            public Model.DataSourceType Type => throw new System.NotImplementedException();

            public bool Equals(Model.IDataSource other)
            {
                throw new System.NotImplementedException();
            }

            public Model.IDataContext Load()
            {
                throw new System.NotImplementedException();
            }
        }

        [Test]
        public void Load()
        {
            // Data imports are tested in TestImport.cs, so we only test the basics here

            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();

            CreateEmptyXls(tempDir, GetExportFileName("class", "12345"));
            CreateEmptyXls(tempDir, GetExportFileName("property", "12345"));

            Assert.That(dataProvider.IsValidPath(tempDir));
            Assert.That(() => dataProvider.OpenPath(tempDir).Load(), Throws.Nothing);
        }

        [Test]
        public void Load_Cache()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();

            CreateEmptyXls(tempDir, GetExportFileName("class", "12345"));
            CreateEmptyXls(tempDir, GetExportFileName("property", "12345"));
            Assert.That(dataProvider.IsValidPath(tempDir));

            var source = dataProvider.OpenPath(tempDir);
            var context1 = source.Load();
            var context2 = source.Load();
            Assert.That(Is.ReferenceEquals(context1, context2));
        }

        [Test]
        public void EmptyPath()
        {
            var dataProvider = new DataProvider();

            Assert.That(() => new DataSource(dataProvider, "", Model.DataSourceType.Custom),
                Throws.InstanceOf<Model.ImportException>());
        }

        [Test]
        public void InvalidPath()
        {
            var dataProvider = new DataProvider();

            var invalidCharacters = Path.GetInvalidPathChars();
            if (invalidCharacters.Length == 0)
                return;

            Assert.That(() => new DataSource(dataProvider, invalidCharacters.First().ToString(),
                Model.DataSourceType.Custom), Throws.InstanceOf<Model.ImportException>());
        }

        [TestCase(Model.DataSourceType.Custom)]
        [TestCase(Model.DataSourceType.Default)]
        [TestCase(Model.DataSourceType.Online)]
        public void Attributes(Model.DataSourceType type)
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();

            var dataSource = new DataSource(dataProvider, tempDir, type);
            Assert.That(dataSource.Name, Is.EqualTo(Path.GetFileName(tempDir)));
            Assert.That(dataSource.DataProvider, Is.EqualTo(dataProvider));
            Assert.That(dataSource.Path, Is.EqualTo(tempDir.Path));
            Assert.That(dataSource.Type, Is.EqualTo(type));
        }

        [Test]
        public void Equals()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();

            var dataSource1 = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);
            var dataSource2 = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);
            var dataSource3 = new DataSource(dataProvider, tempDir, Model.DataSourceType.Default);
            var dataSource4 = new DataSource(dataProvider, tempDir, Model.DataSourceType.Online);

            // o == o for all data sources
            Assert.That(dataSource1.Equals(dataSource1));
            Assert.That(dataSource2.Equals(dataSource2));
            Assert.That(dataSource3.Equals(dataSource3));
            Assert.That(dataSource4.Equals(dataSource4));

            // o1 == o2 for all pairs of data sources
            Assert.That(dataSource1.Equals(dataSource2));
            Assert.That(dataSource2.Equals(dataSource1));
            Assert.That(dataSource1.Equals(dataSource3));
            Assert.That(dataSource3.Equals(dataSource1));
            Assert.That(dataSource1.Equals(dataSource4));
            Assert.That(dataSource4.Equals(dataSource1));
            Assert.That(dataSource2.Equals(dataSource3));
            Assert.That(dataSource3.Equals(dataSource2));
            Assert.That(dataSource2.Equals(dataSource4));
            Assert.That(dataSource4.Equals(dataSource2));
            Assert.That(dataSource3.Equals(dataSource4));
            Assert.That(dataSource4.Equals(dataSource3));

            Assert.That(dataSource1.GetHashCode(), Is.EqualTo(dataSource1.GetHashCode()));
            Assert.That(dataSource1.GetHashCode(), Is.EqualTo(dataSource2.GetHashCode()));
            Assert.That(dataSource1.GetHashCode(), Is.EqualTo(dataSource3.GetHashCode()));
            Assert.That(dataSource1.GetHashCode(), Is.EqualTo(dataSource4.GetHashCode()));
        }

        [Test]
        public void Equals_DifferentSource()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();

            var dataSource1 = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);
            var dataSource2 = new UnimplementedDataSource();

            Assert.That(!dataSource1.Equals(dataSource2));
        }

        [Test]
        public void Equals_DifferentPath()
        {
            using var tempDir1 = TempDir.Create();
            using var tempDir2 = TempDir.Create();
            var dataProvider = new DataProvider();

            var dataSource1 = new DataSource(dataProvider, tempDir1, Model.DataSourceType.Custom);
            var dataSource2 = new DataSource(dataProvider, tempDir2, Model.DataSourceType.Custom);

            Assert.That(!dataSource1.Equals(dataSource2));
            Assert.That(!dataSource2.Equals(dataSource1));
        }

        [Test]
        public void Equals_DifferentProvider()
        {
            using var tempDir = TempDir.Create();
            var dataProvider1 = new DataProvider();
            var dataProvider2 = new DataProvider();

            var dataSource1 = new DataSource(dataProvider1, tempDir, Model.DataSourceType.Custom);
            var dataSource2 = new DataSource(dataProvider2, tempDir, Model.DataSourceType.Custom);

            Assert.That(!dataSource1.Equals(dataSource2));
            Assert.That(!dataSource2.Equals(dataSource1));
        }

        [Test]
        public void Equals_DifferentPathAndProvider()
        {
            using var tempDir1 = TempDir.Create();
            using var tempDir2 = TempDir.Create();
            var dataProvider1 = new DataProvider();
            var dataProvider2 = new DataProvider();

            var dataSource1 = new DataSource(dataProvider1, tempDir1, Model.DataSourceType.Custom);
            var dataSource2 = new DataSource(dataProvider2, tempDir2, Model.DataSourceType.Custom);

            Assert.That(!dataSource1.Equals(dataSource2));
            Assert.That(!dataSource2.Equals(dataSource1));
        }

        [Test]
        public void Equals_DifferentObject()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();

            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);
            // We deliberately perform these comparisons to check that they dont work
            // ReSharper disable SuspiciousTypeConversion.Global
            Assert.That(!dataSource.Equals(dataProvider));
            Assert.That(!dataSource.Equals("test"));
            // ReSharper restore restore SuspiciousTypeConversion.Global
        }

        [TestCase(Model.DataSourceType.Custom, "IEC CDD: {0} [{1}]")]
        [TestCase(Model.DataSourceType.Default, "IEC CDD: {0}")]
        [TestCase(Model.DataSourceType.Online, "IEC CDD: {0} [online]")]
        public void Test_ToString(Model.DataSourceType type, string pattern)
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();

            var dataSource = new DataSource(dataProvider, tempDir, type);
            var fileName = Path.GetFileName(tempDir);
            var dirName = Path.GetDirectoryName(tempDir);

            Assert.That(dataSource.ToString(), Is.EqualTo(string.Format(pattern, fileName, dirName)));
        }
    }

    public class Test_Context : CddTest
    {
        [Test]
        public void Empty()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var context = new Context(dataSource,
                new System.Collections.Generic.List<Class>(),
                new System.Collections.Generic.List<Property>());
            Assert.That(context.DataSource, Is.EqualTo(dataSource));
            Assert.That(context.Classes, Is.Empty);
            Assert.That(context.UnknownReferences, Is.Empty);
            Assert.That(context.LoadSubmodels(), Is.Empty);
            Assert.That(context.LoadSubmodelElements(), Is.Empty);

            Assert.That(context.GetElement<Class>(""), Is.Null);
            Assert.That(context.UnknownReferences, Is.Empty);

            Assert.That(context.GetElement<Class>("test"), Is.Null);
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[] {
                new Model.UnknownReference("test", "Class")
            }));

            Assert.That(context.GetElement<Property>("test123"), Is.Null);
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[] {
                new Model.UnknownReference("test", "Class"),
                new Model.UnknownReference("test123", "Property")
            }));
        }

        [Test]
        public void DummyElements()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var clsId = "clsid";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            var cls = new Class(clsDict);

            var propertyId = "propertyid";
            var propertyDict = CreatePropertyDictionary();
            propertyDict["MDC_P001_6"] = propertyId;
            var property = new Property(propertyDict);

            var context = new Context(dataSource, new[] { cls }.ToList(), new[] { property }.ToList());
            Assert.That(context.DataSource, Is.EqualTo(dataSource));
            Assert.That(context.Classes, Is.EquivalentTo(new[] { cls }));
            Assert.That(context.UnknownReferences, Is.Empty);
            Assert.That(context.LoadSubmodels().Select(e => e.Id), Is.EquivalentTo(new[] { cls.Code }));
            Assert.That(context.LoadSubmodelElements().Select(e => e.Id),
                Is.EquivalentTo(new[] { cls.Code, property.Code }));

            Assert.That(context.GetElement<Class>(""), Is.Null);
            Assert.That(context.UnknownReferences, Is.Empty);

            Assert.That(context.GetElement<Class>("test"), Is.Null);
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[] {
                new Model.UnknownReference("test", "Class")
            }));

            Assert.That(context.GetElement<Class>(clsId), Is.EqualTo(cls));
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[] {
                new Model.UnknownReference("test", "Class")
            }));

            Assert.That(context.GetElement<Property>(propertyId), Is.EqualTo(property));
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[] {
                new Model.UnknownReference("test", "Class")
            }));

            Assert.That(context.GetElement<Property>(clsId), Is.Null);
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[] {
                new Model.UnknownReference("test", "Class"),
            }));

            Assert.That(context.GetElement<Class>(propertyId), Is.Null);
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[] {
                new Model.UnknownReference("test", "Class"),
            }));
        }

        [Test]
        public void DuplicateElements()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var clsId = "clsid";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            var cls = new Class(clsDict);

            var propertyId = "propertyid";
            var propertyDict = CreatePropertyDictionary();
            propertyDict["MDC_P001_6"] = propertyId;
            var property = new Property(propertyDict);

            var context1 = new Context(dataSource, new[] { cls }.ToList(), new[] { property }.ToList());
            var context2 = new Context(dataSource,
                new[] { cls, cls, cls }.ToList(),
                new[] { property, property, property, property, property }.ToList());

            // TODO (krahlro-sick, 2020-07-31): make sure that there are no duplicates
            Assert.That(context1.GetElement<Class>(clsId), Is.EqualTo(cls));
            Assert.That(context1.GetElement<Property>(propertyId), Is.EqualTo(property));
            Assert.That(context2.GetElement<Class>(clsId), Is.EqualTo(cls));
            Assert.That(context2.GetElement<Property>(propertyId), Is.EqualTo(property));
        }
    }

    public class Test_ClassWrapper : CddTest
    {
        [Test]
        public void Empty()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var cls = new Class(CreateClassDictionary());
            var context = new Context(dataSource, new[] { cls }.ToList(),
                new System.Collections.Generic.List<Property>());
            var submodels = context.LoadSubmodels();
            Assert.That(submodels, Has.Count.EqualTo(1));
            var submodel = submodels.First();
            Assert.That(submodel, Is.InstanceOf<ClassWrapper>());
            var wrapper = submodel as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);

            if (wrapper == null)
                return;

            Assert.That(wrapper.DataSource, Is.EqualTo(dataSource));
            Assert.That(wrapper.Id, Is.Empty);
            Assert.That(wrapper.Name, Is.Empty);
            Assert.That(wrapper.Parent, Is.Null);
            Assert.That(wrapper.Children, Is.Empty);
        }

        [Test]
        public void Id()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var clsId = "0112/2///62683#ACC505";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(),
                new System.Collections.Generic.List<Property>());
            var submodels = context.LoadSubmodels();
            Assert.That(submodels, Has.Count.EqualTo(1));
            var submodel = submodels.First();
            Assert.That(submodel, Is.InstanceOf<ClassWrapper>());
            var wrapper = submodel as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);

            if (wrapper == null)
                return;

            Assert.That(wrapper.DataSource, Is.EqualTo(dataSource));
            Assert.That(wrapper.Id, Is.EqualTo(clsId));
            Assert.That(wrapper.ToString(), Is.EqualTo(clsId));
            Assert.That(wrapper.Name, Is.Empty);
            Assert.That(wrapper.Parent, Is.Null);
            Assert.That(wrapper.Children, Is.Empty);
            Assert.That(wrapper.GetDetailsUrl(), Is.EqualTo(new Uri(
                "https://cdd.iec.ch/cdd/iec62683/iec62683.nsf/classes/0112-2---62683%23ACC505")));
        }

        [Test]
        public void Name_English()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var clsId = "clsid";
            var clsName = "English Name";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            clsDict["MDC_P004_1.de"] = "foobar";
            clsDict["MDC_P004_1.en"] = clsName;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(),
                new System.Collections.Generic.List<Property>());
            var wrapper = context.LoadSubmodels().First() as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            Assert.That(wrapper.DataSource, Is.EqualTo(dataSource));
            Assert.That(wrapper.Id, Is.EqualTo(clsId));
            Assert.That(wrapper.Name, Is.EqualTo(clsName));
            Assert.That(wrapper.Parent, Is.Null);
            Assert.That(wrapper.Children, Is.Empty);
        }

        [Test]
        public void Name_German()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var clsId = "clsid";
            var clsName = "German Name";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            clsDict["MDC_P004_1.de"] = clsName;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(),
                new System.Collections.Generic.List<Property>());
            var wrapper = context.LoadSubmodels().First() as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            Assert.That(wrapper.DataSource, Is.EqualTo(dataSource));
            Assert.That(wrapper.Id, Is.EqualTo(clsId));
            Assert.That(wrapper.Name, Is.EqualTo(clsName));
            Assert.That(wrapper.Parent, Is.Null);
            Assert.That(wrapper.Children, Is.Empty);
        }

        [Test]
        public void GetDetails()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var clsId = "clsid";
            var clsName = "English Name";
            var clsVersion = "1";
            var clsRevision = "2";
            var clsDefinition = "English Definition";
            var clsDefinitionSource = "Definition Source";
            var clsSuperclass = "superclassid";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            clsDict["MDC_P002_1"] = clsVersion;
            clsDict["MDC_P002_2"] = clsRevision;
            clsDict["MDC_P004_1.de"] = "foobar";
            clsDict["MDC_P004_1.en"] = clsName;
            clsDict["MDC_P005.de"] = "kljlkdfjgl";
            clsDict["MDC_P005.sv"] = "kljl";
            clsDict["MDC_P005.en"] = clsDefinition;
            clsDict["MDC_P006_1"] = clsDefinitionSource;
            clsDict["MDC_P010"] = clsSuperclass;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(),
                new System.Collections.Generic.List<Property>());
            var wrapper = context.LoadSubmodels().First() as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            Assert.That(wrapper.GetDetails(), Is.EquivalentTo(new Dictionary<string, string>
            {
                { "Code", clsId },
                { "Version", clsVersion },
                { "Revision", clsRevision },
                { "Preferred Name", clsName },
                { "Definition", clsDefinition },
                { "Definition Source", clsDefinitionSource },
                { "Superclass", clsSuperclass },
            }));
        }

        [Test]
        public void Properties()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId1 = "propertyid1";
            var propertyDict1 = CreatePropertyDictionary();
            propertyDict1["MDC_P001_6"] = propertyId1;
            var property1 = new Property(propertyDict1);

            var propertyId2 = "propertyid2";
            var propertyDict2 = CreatePropertyDictionary();
            propertyDict2["MDC_P001_6"] = propertyId2;
            var property2 = new Property(propertyDict2);

            var propertyId3 = "propertyid3";
            var propertyDict3 = CreatePropertyDictionary();
            propertyDict3["MDC_P001_6"] = propertyId3;
            var property3 = new Property(propertyDict3);

            var clsId = "clsid";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            clsDict["MDC_P014"] = propertyId1 + "," + propertyId2;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(),
                new[] { property1, property2, property3 }.ToList());
            var wrapper = context.LoadSubmodels().First() as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            var children = wrapper.Children;

            Assert.That(children.Cast<PropertyWrapper>().Select(p => p.Element),
                Is.EquivalentTo(new[] { property1, property2 }));
            Assert.That(Is.ReferenceEquals(children, wrapper.Children));
        }

        [Test]
        public void ImportedProperties()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId1 = "propertyid1";
            var propertyDict1 = CreatePropertyDictionary();
            propertyDict1["MDC_P001_6"] = propertyId1;
            var property1 = new Property(propertyDict1);

            var propertyId2 = "propertyid2";
            var propertyDict2 = CreatePropertyDictionary();
            propertyDict2["MDC_P001_6"] = propertyId2;
            var property2 = new Property(propertyDict2);

            var propertyId3 = "propertyid3";
            var propertyDict3 = CreatePropertyDictionary();
            propertyDict3["MDC_P001_6"] = propertyId3;
            var property3 = new Property(propertyDict3);

            var clsId = "clsid";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            clsDict["MDC_P090"] = propertyId1 + "," + propertyId2;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(),
                new[] { property1, property2, property3 }.ToList());
            var wrapper = context.LoadSubmodels().First() as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            Assert.That(wrapper.Children.Cast<PropertyWrapper>().Select(p => p.Element),
                Is.EquivalentTo(new[] { property1, property2 }));
        }

        [Test]
        public void Properties_UnknownReference()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId = "propertyid";
            var clsId = "clsid";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            clsDict["MDC_P014"] = propertyId;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(), new List<Property>());
            var wrapper = context.LoadSubmodels().First() as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            Assert.That(wrapper.Children.Cast<PropertyWrapper>().Select(p => p.Element), Is.Empty);
            Assert.That(context.UnknownReferences, Is.EquivalentTo(new[]
            {
                UnknownReference.Create<Property>(propertyId),
            }));
        }

        [Test]
        public void Children_Combined()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId1 = "propertyid1";
            var propertyDict1 = CreatePropertyDictionary();
            propertyDict1["MDC_P001_6"] = propertyId1;
            var property1 = new Property(propertyDict1);

            var propertyId2 = "propertyid2";
            var propertyDict2 = CreatePropertyDictionary();
            propertyDict2["MDC_P001_6"] = propertyId2;
            var property2 = new Property(propertyDict2);

            var propertyId3 = "propertyid3";
            var propertyDict3 = CreatePropertyDictionary();
            propertyDict3["MDC_P001_6"] = propertyId3;
            var property3 = new Property(propertyDict3);

            var propertyId4 = "propertyid4";
            var propertyDict4 = CreatePropertyDictionary();
            propertyDict4["MDC_P001_6"] = propertyId4;
            var property4 = new Property(propertyDict4);

            var clsId = "clsid";
            var clsDict = CreateClassDictionary();
            clsDict["MDC_P001_5"] = clsId;
            clsDict["MDC_P014"] = propertyId1;
            clsDict["MDC_P090"] = propertyId2 + "," + propertyId3;
            var cls = new Class(clsDict);

            var context = new Context(dataSource, new[] { cls }.ToList(),
                new[] { property1, property2, property3, property4 }.ToList());
            var wrapper = context.LoadSubmodels().First() as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            Assert.That(wrapper.Children.Cast<PropertyWrapper>().Select(p => p.Element),
                Is.EquivalentTo(new[] { property1, property2, property3 }));
        }

        [Test]
        public void Children_Superclass()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId1 = "propertyid1";
            var propertyDict1 = CreatePropertyDictionary();
            propertyDict1["MDC_P001_6"] = propertyId1;
            var property1 = new Property(propertyDict1);

            var propertyId2 = "propertyid2";
            var propertyDict2 = CreatePropertyDictionary();
            propertyDict2["MDC_P001_6"] = propertyId2;
            var property2 = new Property(propertyDict2);

            var propertyId3 = "propertyid3";
            var propertyDict3 = CreatePropertyDictionary();
            propertyDict3["MDC_P001_6"] = propertyId3;
            var property3 = new Property(propertyDict3);

            var propertyId4 = "propertyid4";
            var propertyDict4 = CreatePropertyDictionary();
            propertyDict4["MDC_P001_6"] = propertyId4;
            var property4 = new Property(propertyDict4);

            var propertyId5 = "propertyid5";
            var propertyDict5 = CreatePropertyDictionary();
            propertyDict5["MDC_P001_6"] = propertyId5;
            var property5 = new Property(propertyDict5);

            var clsId1 = "clsid1";
            var clsDict1 = CreateClassDictionary();
            clsDict1["MDC_P001_5"] = clsId1;
            clsDict1["MDC_P014"] = propertyId1;
            clsDict1["MDC_P090"] = propertyId2;
            var cls1 = new Class(clsDict1);

            var clsId2 = "clsid2";
            var clsDict2 = CreateClassDictionary();
            clsDict2["MDC_P001_5"] = clsId2;
            clsDict2["MDC_P014"] = propertyId3;
            clsDict2["MDC_P090"] = propertyId4;
            clsDict2["MDC_P010"] = clsId1;
            var cls2 = new Class(clsDict2);

            var context = new Context(dataSource, new[] { cls1, cls2 }.ToList(),
                new[] { property1, property2, property3, property4, property5 }.ToList());
            var wrapper = context.LoadSubmodels().First(s => s.Id == clsId2) as ClassWrapper;
            Assert.That(wrapper, Is.Not.Null);
            if (wrapper == null)
                return;

            Assert.That(wrapper.Children.Cast<PropertyWrapper>().Select(p => p.Element),
                Is.EquivalentTo(new[] { property1, property2, property3, property4 }));
        }
    }

    public class Test_PropertyWrapper : CddTest
    {
        [Test]
        public void Empty()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var property = new Property(CreatePropertyDictionary());
            var context = new Context(dataSource, new List<Class>(), new[] { property }.ToList());
            Assert.That(context.LoadSubmodels(), Is.Empty);
            var submodelElements = context.LoadSubmodelElements();
            Assert.That(submodelElements, Has.Count.EqualTo(1));
            var submodelElement = submodelElements.First();
            Assert.That(submodelElement, Is.InstanceOf<PropertyWrapper>());
            var wrapper = submodelElement as PropertyWrapper;
            Assert.That(wrapper, Is.Not.Null);

            if (wrapper == null)
                return;

            Assert.That(wrapper.DataSource, Is.EqualTo(dataSource));
            Assert.That(wrapper.Id, Is.Empty);
            Assert.That(wrapper.Name, Is.Empty);
            Assert.That(wrapper.Parent, Is.Null);
            Assert.That(wrapper.Children, Is.Empty);
        }

        [Test]
        public void Id()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId = "0112/2///62683#ACE101";
            var propertyDict = CreatePropertyDictionary();
            propertyDict["MDC_P001_6"] = propertyId;
            var property = new Property(propertyDict);

            var context = new Context(dataSource, new List<Class>(), new[] { property }.ToList());
            var wrapper = context.LoadSubmodelElements().First() as PropertyWrapper;
            if (wrapper == null)
                return;

            Assert.That(wrapper.DataSource, Is.EqualTo(dataSource));
            Assert.That(wrapper.Id, Is.EqualTo(propertyId));
            Assert.That(wrapper.ToString(), Is.EqualTo(propertyId));
            Assert.That(wrapper.Name, Is.Empty);
            Assert.That(wrapper.Parent, Is.Null);
            Assert.That(wrapper.Children, Is.Empty);
            Assert.That(wrapper.GetDetailsUrl(), Is.EqualTo(new Uri(
                "https://cdd.iec.ch/cdd/iec62683/iec62683.nsf/PropertiesAllVersions/0112-2---62683%23ACE101")));
        }

        [Test]
        public void GetDetails()
        {
            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId = "clsid";
            var propertyName = "English Name";
            var propertyVersion = "1";
            var propertyRevision = "2";
            var propertyDefinition = "English Definition";
            var propertyDefinitionSource = "Definition Source";
            var propertyShortName = "superclassid";
            var propertySymbol = "symbol";
            var propertyUnit = "unit";
            var propertyUnitCode = "unit_code";
            var propertyDataType = "STRING_TYPE";
            var propertyFormat = "X..32";
            var propertyDict = CreatePropertyDictionary();
            propertyDict["MDC_P001_6"] = propertyId;
            propertyDict["MDC_P002_1"] = propertyVersion;
            propertyDict["MDC_P002_2"] = propertyRevision;
            propertyDict["MDC_P004_1.de"] = "foobar";
            propertyDict["MDC_P004_1.en"] = propertyName;
            propertyDict["MDC_P005.de"] = "kljlkdfjgl";
            propertyDict["MDC_P005.sv"] = "kljl";
            propertyDict["MDC_P005.en"] = propertyDefinition;
            propertyDict["MDC_P006_1"] = propertyDefinitionSource;
            propertyDict["MDC_P004_3.sv"] = propertyShortName;
            propertyDict["MDC_P025_1"] = propertySymbol;
            propertyDict["MDC_P023"] = propertyUnit;
            propertyDict["MDC_P041"] = propertyUnitCode;
            propertyDict["MDC_P022"] = propertyDataType;
            propertyDict["MDC_P024"] = propertyFormat;
            var property = new Property(propertyDict);

            var context = new Context(dataSource, new List<Class>(), new[] { property }.ToList());
            var wrapper = context.LoadSubmodelElements().First() as PropertyWrapper;
            if (wrapper == null)
                return;

            Assert.That(wrapper.GetDetails(), Is.EquivalentTo(new Dictionary<string, string>
            {
                { "Code", propertyId },
                { "Version", propertyVersion },
                { "Revision", propertyRevision },
                { "Preferred Name", propertyName },
                { "Definition", propertyDefinition },
                { "Definition Source", propertyDefinitionSource },
                { "Short Name", propertyShortName },
                { "Symbol", propertySymbol },
                { "Primary Unit", propertyUnit },
                { "Unit Code", propertyUnitCode },
                { "Data Type", propertyDataType },
                { "Format", propertyFormat },
            }));
        }

        [Test]
        public void AggregateType()
        {

            using var tempDir = TempDir.Create();
            var dataProvider = new DataProvider();
            var dataSource = new DataSource(dataProvider, tempDir, Model.DataSourceType.Custom);

            var propertyId = "0112/2///62683#ACE101";
            var propertyDict = CreatePropertyDictionary();
            propertyDict["MDC_P001_6"] = propertyId;
            propertyDict["MDC_P022"] = "SET(0,?) OF STRING";
            var property = new Property(propertyDict);

            var context = new Context(dataSource, new List<Class>(), new[] { property }.ToList());
            var wrapper = context.LoadSubmodelElements().First() as PropertyWrapper;
            if (wrapper == null)
                return;

            Assert.That(wrapper.Element.DataType, Is.InstanceOf<AggregateType>());
            if (wrapper.Element.DataType is AggregateType type)
            {
                Assert.That(type.TypeValue, Is.EqualTo(Cdd.AggregateType.Type.Set));
                Assert.That(type.Subtype, Is.EqualTo(new SimpleType(SimpleType.Type.String)));
                Assert.That(type.LowerBound, Is.EqualTo(0));
                Assert.That(type.UpperBound, Is.Null);
            }

            Assert.That(wrapper.Children, Has.Count.EqualTo(1));
            var child = wrapper.Children.First();
            Assert.That(child, Is.InstanceOf<PropertyWrapper>());
            var childWrapper = child as PropertyWrapper;
            if (childWrapper == null)
                return;
            Assert.That(childWrapper.Element.DataType, Is.EqualTo(new SimpleType(SimpleType.Type.String)));
        }

        [Test]
        public void ReferenceType()
        {

        }
    }
    // ReSharper restore UnusedType.Global
    // ReSharper restore AccessToDisposedClosure
}
