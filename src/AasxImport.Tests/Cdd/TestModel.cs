// Copyright (C) 2020 Robin Krahl, RD/ESR, SICK AG <robin.krahl@sick.de>
// This software is licensed under the Apache License 2.0 (Apache-2.0).

using AasxImport.Cdd;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.IO;

namespace AasxImport.Tests.Cdd
{
    // ReSharper disable UnusedType.Global
    [TestFixture]
    public class Test_DataProvider
    {
        private string _dataDir;
        private string _tempDir;

        [OneTimeSetUp]
        public void Init()
        {
            _dataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "Cdd", "data");
            _tempDir = GetRandomDirectory(Path.GetTempPath());
            Directory.CreateDirectory(_tempDir);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void Name()
        {
            Assert.AreEqual(new DataProvider().Name, "IEC CDD");
        }

        [Test]
        public void IsValidPath()
        {
            var dataProvider = new DataProvider();
            var path = CreateRandomTempDirectory();

            void load() => dataProvider.OpenPath(path).Load();
            void touch(string type, string id) => CreateEmptyXls(path, GetExportFileName(type, id));

            // empty directory
            Assert.That(!dataProvider.IsValidPath(path));
            Assert.That(load, Throws.InstanceOf<Model.ImportException>());

            // missing required files
            touch("CLASS", "12345");
            Assert.That(!dataProvider.IsValidPath(path));
            Assert.That(load, Throws.TypeOf<MissingExportFilesException>());

            // all required files present
            touch("property", "12345");
            Assert.That(dataProvider.IsValidPath(path));
            Assert.That(load, Throws.Nothing);

            // multiple required files
            touch("class", "67890");
            Assert.That(!dataProvider.IsValidPath(path));
            Assert.That(load, Throws.TypeOf<MultipleExportFilesException>());
        }

        [Test]
        public void IsValidPath_NonexistingPath()
        {
            var dataProvider = new DataProvider();
            var path = CreateRandomTempDirectory();
            Assert.That(!dataProvider.IsValidPath(path));
            Assert.That(() => dataProvider.OpenPath(path).Load(), Throws.InstanceOf<Model.ImportException>());
        }

        [Test]
        public void OpenPath()
        {
            // failures are already tested in IsValidPath, so we only have to deal with valid paths
            var dataProvider = new DataProvider();
            var path = CreateRandomTempDirectory();
            CreateEmptyXls(path, GetExportFileName("class", "12345"));
            CreateEmptyXls(path, GetExportFileName("property", "12345"));

            Assert.That(dataProvider.IsValidPath(path));
            Assert.That(dataProvider.OpenPath(path),
                Is.EqualTo(new DataSource(dataProvider, path, Model.DataSourceType.Custom)));
        }

        [Test]
        public void GetDefaultPaths()
        {
            var dataProvider = new DataProvider();
            var path = CreateRandomTempDirectory();
            var iecCddPath = Path.Combine(path, "iec-cdd");
            void assertDefaultDataSources(IResolveConstraint constraint)
            {
                Assert.That(dataProvider.FindDefaultDataSources(), constraint);
            }
            void assertNoDefaultDataSources() { assertDefaultDataSources(Is.Empty); }

            // missing dir
            WithWorkingDirectory(path, assertNoDefaultDataSources);

            // empty dir
            Directory.CreateDirectory(iecCddPath);
            WithWorkingDirectory(path, assertNoDefaultDataSources);

            // empty source
            var source1 = CreateRandomDirectory(iecCddPath);
            WithWorkingDirectory(path, assertNoDefaultDataSources);

            // incomplete source
            CreateEmptyXls(source1, GetExportFileName("class", "12345"));
            WithWorkingDirectory(path, assertNoDefaultDataSources);

            // valid source
            CreateEmptyXls(source1, GetExportFileName("property", "12345"));
            Assert.That(dataProvider.IsValidPath(source1));
            WithWorkingDirectory(path, () => assertDefaultDataSources(Is.EquivalentTo(new[] {
                new DataSource(dataProvider, source1, Model.DataSourceType.Default),
            })));

            // two valid sources
            var source2 = CreateRandomDirectory(iecCddPath);
            CreateEmptyXls(source2, GetExportFileName("class", "67890"));
            CreateEmptyXls(source2, GetExportFileName("property", "67890"));
            Assert.That(dataProvider.IsValidPath(source2));
            WithWorkingDirectory(path, () => assertDefaultDataSources(Is.EquivalentTo(new[] {
                new DataSource(dataProvider, source1, Model.DataSourceType.Default),
                new DataSource(dataProvider, source2, Model.DataSourceType.Default),
            })));

            // one valid source, one invalid source
            CreateEmptyXls(source2, GetExportFileName("class", "12345"));
            Assert.That(!dataProvider.IsValidPath(source2));
            WithWorkingDirectory(path, () => assertDefaultDataSources(Is.EquivalentTo(new[] {
                new DataSource(dataProvider, source1, Model.DataSourceType.Default),
            })));
        }

        private static string GetExportFileName(string type, string id)
        {
            return $"export_{type.ToUpper()}_TSTM-{id}.xls";
        }

        private void WithWorkingDirectory(string path, Action f)
        {
            var cwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(path);
            try
            {
                f.Invoke();
            }
            finally
            {
                Directory.SetCurrentDirectory(cwd);
            }
        }

        private void CopyXls(string sourceName, string targetPath)
        {
            if (!File.Exists(targetPath))
                File.Delete(targetPath);
            File.Copy(Path.Combine(_dataDir, sourceName), targetPath);
        }

        private void CreateEmptyXls(string path, string name)
        {
            CopyXls("empty.xls", Path.Combine(path, name));
        }

        private string CreateRandomTempDirectory() => CreateRandomDirectory(_tempDir);

        private static string CreateRandomDirectory(string baseDir)
        {
            var path = GetRandomDirectory(baseDir);
            Directory.CreateDirectory(path);
            return path;
        }

        private static string GetRandomDirectory(string baseDir) => Path.Combine(baseDir, Path.GetRandomFileName());
    }
    // ReSharper restore UnusedType.Global

}
