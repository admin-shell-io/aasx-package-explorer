/*
 * Copyright (c) 2020 SICK AG <info@sick.de>
 *
 * This software is licensed under the Apache License 2.0 (Apache-2.0).
 * The ExcelDataReder dependency is licensed under the MIT license
 * (https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/LICENSE).
 */

#nullable enable

using System.IO;
using System.Linq;
using AasxDictionaryImport.Model;
using AasxDictionaryImport.Tests;
using AdminShellNS;
using Aas = AasCore.Aas3_0;
using NUnit.Framework;
using Extensions;

namespace AasxDictionaryImport.Cdd.Tests
{
    public class Test_Importer : CddTest
    {
        [Test]
        public void Simple()
        {
            using var tempDir = TempDir.Create();
            CopyXls("simple-class.xls", Path.Combine(tempDir.Path, GetExportFileName("class", "simple")));
            CopyXls("simple-property.xls", Path.Combine(tempDir.Path, GetExportFileName("property", "simple")));

            var dataProvider = new DataProvider();
            var source = dataProvider.OpenPath(tempDir.Path);
            var context = source.Load();

            var submodels = context.LoadSubmodels();
            Assert.That(submodels.Select(c => new[] { c.Id, c.Name }), Is.EqualTo(new[] {
                new[] { "C1", "Main test class" },
                new[] { "C2", "Block 1" },
                new[] { "C3", "Block 2" },
            }));

            var submodelElements = context.LoadSubmodelElements();
            Assert.That(submodelElements.Select(c => new[] { c.Id, c.Name }), Is.EquivalentTo(new[] {
                new[] { "C1", "Main test class" },
                new[] { "C2", "Block 1" },
                new[] { "C3", "Block 2" },
                // P1: "Block reference 1 is resolved to:
                new[] { "C2", "Block 1" },
                // P2: "Block reference 2 is resolved to:
                new[] { "C3", "Block 2" },
                new[] { "P3", "Test property 1" },
                new[] { "P4", "Test property 2" },
            }));

            var env = new Aas.Environment();
            var adminShell = CreateAdminShell(env);

            var p3 = submodelElements.First(e => e.Id == "P3");
            p3.IsSelected = true;
            Assert.That(!p3.ImportSubmodelInto(env, adminShell));
            Assert.That(adminShell.Submodels, Has.Count.Zero);

            var c1 = submodels.First(e => e.Id == "C1");
            var children = c1.Children;
            Assert.That(children.Count, Is.EqualTo(2));

            SetSelected(c1, true);
            Assert.That(c1.ImportSubmodelInto(env, adminShell));
            Assert.That(adminShell.Submodels, Has.Count.EqualTo(1));
            var submodel = env.FindSubmodel(adminShell.Submodels[0]);
            Assert.That(submodel.IdShort, Is.EqualTo("MainTestClass"));
            Assert.That(submodel.SubmodelElements, Is.Not.Null);
            Assert.That(submodel.SubmodelElements, Has.Count.EqualTo(2));

            var c2 = submodel.SubmodelElements[0];
            Assert.That(c2, Is.TypeOf<Aas.SubmodelElementCollection>());
            Assert.That(c2.IdShort, Is.EqualTo("Block1"));
            if (!(c2 is Aas.ISubmodelElementCollection c2Coll))
                return;
            Assert.That(c2Coll.Value, Has.Count.EqualTo(1));

            var p3Element = c2Coll.Value[0];
            Assert.That(p3Element, Is.TypeOf<Aas.IProperty>());
            Assert.That(p3Element.IdShort, Is.EqualTo("TestProperty1"));
            // TODO (Robin, 2020-09-03): please check
            // dead-csharp off
            // Assert.That(p3Element.hasDataSpecification, Is.Not.Null);
            // Assert.That(p3Element.hasDataSpecification, Has.Count.EqualTo(1));
            // dead-csharp on

            var c3 = submodel.SubmodelElements[1];
            Assert.That(c3, Is.TypeOf<Aas.SubmodelElementCollection>());
            Assert.That(c3.IdShort, Is.EqualTo("Block2"));
            if (!(c3 is Aas.ISubmodelElementCollection c3Coll))
                return;
            Assert.That(c3Coll.Value, Has.Count.EqualTo(1));

            var p4Element = c3Coll.Value[0];
            Assert.That(p4Element, Is.TypeOf<Aas.Property>());
            Assert.That(p4Element.IdShort, Is.EqualTo("TestProperty2"));
            // TODO (Robin, 2020-09-03): please check
            // dead-csharp off
            // Assert.That(p4Element.hasDataSpecification, Is.Not.Null);
            // Assert.That(p4Element.hasDataSpecification, Has.Count.EqualTo(1));
            // dead-csharp on
        }

        private void SetSelected(IElement element, bool selected)
        {
            element.IsSelected = selected;
            foreach (var child in element.Children)
            {
                SetSelected(child, selected);
            }
        }
    }
}
