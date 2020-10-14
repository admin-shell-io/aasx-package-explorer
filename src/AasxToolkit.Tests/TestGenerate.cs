using System;
using System.IO;
using System.Xml.Serialization;

using Newtonsoft.Json;
using NUnit.Framework;

using AdminShellNS;

namespace AasxToolkit.Tests
{
    public class TestGenerate
    {
        private static void TestSerialize(AdminShellPackageEnv package)
        {
            var aasenv1 = package.AasEnv;

            if (true)
            {
                try
                {
                    //
                    // Test serialize
                    // this generates a "sample.xml" is addition to the package below, for direct usage.
                    //
                    Log.WriteLine(2, "Test serialize sample.xml ..");
                    using (var s = new StreamWriter("sample.xml"))
                    {
                        var serializer = new XmlSerializer(aasenv1.GetType());
                        var nss = AdminShellPackageEnv.GetXmlDefaultNamespaces();
                        serializer.Serialize(s, aasenv1, nss);
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.Write("While test serializing XML: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }
            }

            if (true)
                try
                {
                    //
                    // Test load
                    // (via package function)
                    //
                    Log.WriteLine(2, "Test de-serialize sample.xml ..");
                    var package2 = new AdminShellPackageEnv("sample.xml");
                    package2.Close();
                }
                catch (Exception ex)
                {
                    Console.Out.Write("While test serializing XML: {0} at {1}", ex.Message, ex.StackTrace);
                    Environment.Exit(-1);
                }

            //
            // Try JSON
            //

            // hardcore!
            if (true)
            {
                Log.WriteLine(2, "Test serialize sample.json ..");
                var sw = new StreamWriter("sample.json");
                sw.AutoFlush = true;
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, aasenv1);
                }

                // try to de-serialize
                Log.WriteLine(2, "Test de-serialize sample.json ..");
                using (StreamReader file = File.OpenText("sample.json"))
                {
                    JsonSerializer serializer2 = new JsonSerializer();
                    serializer2.Converters.Add(new AdminShellConverters.JsonAasxConverter());
                    serializer2.Deserialize(file, typeof(AdminShellV20.AdministrationShellEnv));
                }
            }

            // via utilities
#if FALSE
            {
                var package2 = new AdminShellPackageEnv(aasenv1);
                package2.SaveAs("sample.json", writeFreshly: true);
                package2.Close();

                var package3 = new AdminShellPackageEnv("sample.json");
                package3.Close();
            }
#endif
        }

        [Test]
        public void TestThatItWorks()
        {
            string dataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "data");
            if (!Directory.Exists(dataDir))
            {
                throw new InvalidOperationException(
                    $"Expected the data directory to exist in the test directory, " +
                    $"but could not be found: {dataDir}");
            }

            // Check for a single image from the data directory to warn the user that the sample data has not been
            // provided
            string motorI40Jpg = Path.Combine(dataDir, "MotorI40.JPG");
            if (!File.Exists(motorI40Jpg))
            {
                throw new InvalidOperationException(
                    $"One of the sample files could not be found in the data directory: {motorI40Jpg};" +
                    $"did you download the sample data?");
            }
            
            var package = AasxToolkit.Generate.GeneratePackage();
            TestSerialize(package);
            package.SaveAs("sample-admin-shell.aasx", useMemoryStream: new MemoryStream());
        }
    }
}
