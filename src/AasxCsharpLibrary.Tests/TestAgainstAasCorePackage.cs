using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace AdminShellNS.Tests
{
    public class TestAgainstAasCorePackage
    {
        /**
         * Retrieve the bytes of the valid XML file 01_Festo.aasx.xml from the
         * test resources.
         */
        private static byte[] Get01FestoAasxXmlBytes()
        {
            string pth = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestResources\\AasxCsharpLibrary.Tests\\XmlValidation\\expectedOk\\" +
                "01_Festo.aasx.xml");

            if (!File.Exists(pth))
            {
                throw new FileNotFoundException(
                    $"Could not find the XML file: {pth}");
            }

            return File.ReadAllBytes(pth);
        }

        [Test]
        public void TestThatSupplementaryMaterialIsLoaded()
        {
            var packaging = new AasCore.Aas3.Package.Packaging();
            using var tmpDir = new TemporaryDirectory();

            var pth = System.IO.Path.Combine(tmpDir.Path, "dummy.aasx");

            var supplUri = new Uri(
                "/aasx-suppl/some-company/some-manual.pdf",
                UriKind.Relative);

            var supplContent = Encoding.UTF8.GetBytes("some content");

            // Create a package
            {
                using var pkg = packaging.Create(pth);

                var spec = pkg.MakeSpec(
                    pkg.PutPart(
                        new Uri("/aasx/some-company/data.xml", UriKind.Relative),
                        "text/xml",
                        Get01FestoAasxXmlBytes()));

                pkg.RelateSupplementaryToSpec(
                    pkg.PutPart(
                        supplUri,
                        "application/pdf",
                        supplContent),
                    spec);

                pkg.Flush();
            }

            // Load the AASX using AasxCsharpLibrary
            {
                using var package = new AdminShellPackageEnv(pth);

                Assert.IsTrue(package.IsOpen);

                var lst = package.GetListOfSupplementaryFiles();

                Assert.AreEqual(1, lst.Count);
                var suppl = lst.First();
                Assert.AreEqual(supplUri, suppl.Uri);
                Assert.AreEqual(
                    Encoding.UTF8.GetString(supplContent),
                    Encoding.UTF8.GetString(
                        package.GetByteArrayFromUriOrLocalPackage(
                            suppl.Uri.ToString()))
                );
            }
        }
    }
}