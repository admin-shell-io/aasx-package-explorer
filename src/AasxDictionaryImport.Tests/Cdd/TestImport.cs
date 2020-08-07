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
using NUnit.Framework;

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

            var env = new AdminShellV20.AdministrationShellEnv();
            var adminShell = CreateAdminShell(env);

            var p3 = submodelElements.First(e => e.Id == "P3");
            p3.IsSelected = true;
            Assert.That(!p3.ImportSubmodelInto(env, adminShell));
            Assert.That(adminShell.submodelRefs, Has.Count.Zero);

            var c1 = submodels.First(e => e.Id == "C1");
            var children = c1.Children;
            Assert.That(children.Count, Is.EqualTo(2));

            SetSelected(c1, true);
            Assert.That(c1.ImportSubmodelInto(env, adminShell));
            Assert.That(adminShell.submodelRefs, Has.Count.EqualTo(1));
            var submodel = env.FindSubmodel(adminShell.submodelRefs[0]);
            Assert.That(submodel.idShort, Is.EqualTo("MainTestClass"));
            Assert.That(submodel.submodelElements, Is.Not.Null);
            Assert.That(submodel.submodelElements, Has.Count.EqualTo(2));

            var c2 = submodel.submodelElements[0].submodelElement;
            Assert.That(c2, Is.TypeOf<AdminShellV20.SubmodelElementCollection>());
            Assert.That(c2.idShort, Is.EqualTo("Block1"));
            if (!(c2 is AdminShellV20.SubmodelElementCollection c2Coll))
                return;
            Assert.That(c2Coll.value, Has.Count.EqualTo(1));

            var p3Element = c2Coll.value[0].submodelElement;
            Assert.That(p3Element, Is.TypeOf<AdminShellV20.Property>());
            Assert.That(p3Element.idShort, Is.EqualTo("TestProperty1"));
            Assert.That(p3Element.hasDataSpecification, Is.Not.Null);
            Assert.That(p3Element.hasDataSpecification.reference, Has.Count.EqualTo(1));

            var c3 = submodel.submodelElements[1].submodelElement;
            Assert.That(c3, Is.TypeOf<AdminShellV20.SubmodelElementCollection>());
            Assert.That(c3.idShort, Is.EqualTo("Block2"));
            if (!(c3 is AdminShellV20.SubmodelElementCollection c3Coll))
                return;
            Assert.That(c3Coll.value, Has.Count.EqualTo(1));

            var p4Element = c3Coll.value[0].submodelElement;
            Assert.That(p4Element, Is.TypeOf<AdminShellV20.Property>());
            Assert.That(p4Element.idShort, Is.EqualTo("TestProperty2"));
            Assert.That(p4Element.hasDataSpecification, Is.Not.Null);
            Assert.That(p4Element.hasDataSpecification.reference, Has.Count.EqualTo(1));
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
