/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxPluginHelper = AasxIntegrationBase.AasxPluginHelper;
using Assembly = System.Reflection.Assembly;
using Environment = System.Environment;
using Regex = System.Text.RegularExpressions.Regex;
using StreamReader = System.IO.StreamReader;
using TextReader = System.IO.TextReader;

namespace AasxPackageLogic
{
    /// <summary>
    /// Information about the application.
    /// </summary>
    public class Pref
    {
        public readonly string Authors;

        /// <summary>
        /// The current (used) licenses of the application.
        /// </summary>
        public readonly string LicenseShort;

        /// <summary>
        /// The last build date of the application.
        /// </summary>
        public readonly string BuildDate;

        /// <summary>
        /// The full license texts of the application.
        /// </summary>
        public readonly string LicenseLong;

        /// <summary>
        /// The current version string of the application.
        /// Note: in the past, there was a semantic version such as "1.9.8.3", but
        /// this was not maintained properly. Now, a version is derived from the
        /// build data with the intention, that the according tag in Github-Releases
        /// will be identical.
        /// </summary>
        public readonly string Version;

        public Pref(string authors, string licenseShort, string buildDate, string licenseLong, string version)
        {
            Authors = authors;
            LicenseShort = licenseShort;
            BuildDate = buildDate;
            LicenseLong = licenseLong;
            Version = version;
        }

        /// <summary>
        /// Reads the necessary resources from the system and produces the author, license, build and version
        /// information about the application.
        /// </summary>
        /// <returns>relevant information about the application</returns>
        public static Pref Read()
        {
            string authors = "Michael Hoffmeister, Andreas Orzelski, Erich Barnstedt, Juilee Tikekar et al.";

            string licenseShort =
                "This software is licensed under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)." + Environment.NewLine +
                "The QR code generation is licensed under the MIT license (MIT)." + Environment.NewLine +
                "The Zxing.Net Dot Matrix Code (DMC) generation is licensed " +
                "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The Grapevine REST server framework is licensed " +
                "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The AutomationML.Engine is licensed under the MIT license (MIT)." +
                "The MQTT server and client is licensed " +
                "under the MIT license (MIT)." + Environment.NewLine +
                "The IdentityModel OpenID client is licensed " +
                "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The jose-jwt object signing and encryption is licensed " +
                "under the MIT license (MIT).";

            string buildDate = "";
            using (var stream =
                Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("AasxPackageLogic.Resources.BuildDate.txt"))
            {
                if (stream != null)
                {
                    TextReader tr = new StreamReader(stream);
                    string fileContents = tr.ReadToEnd();
                    if (fileContents.Length > 20)
                        fileContents = fileContents.Substring(0, 20) + "..";
                    buildDate = fileContents.Trim();
                }
            }

            string licenseLong = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                "LICENSE.txt", Assembly.GetEntryAssembly());

            string version = "(not available)";
            {
                // %date% in European format (e.g. during development)
                var m = Regex.Match(buildDate, @"(\d+)\.(\d+)\.(\d+)");
                if (m.Success && m.Groups.Count >= 4)
                {
                    version = "v" + ((m.Groups[3].Value.Length == 2) ? "20" : "")
                                  + m.Groups[3].Value + "-"
                                  + m.Groups[2].Value + "-"
                                  + m.Groups[1].Value;
                }
                else
                {
                    // %date% in US local (e.g. from continuous integration from Github)
                    m = Regex.Match(buildDate, @"(\d+)\/(\d+)\/(\d+)");
                    if (m.Success && m.Groups.Count >= 4)
                        version = "v" + ((m.Groups[3].Value.Length == 2) ? "20" : "")
                                      + m.Groups[3].Value + "-"
                                      + m.Groups[1].Value + "-"
                                      + m.Groups[2].Value;
                }
            }

            return new Pref(authors, licenseShort, buildDate, licenseLong, version);
        }
    }
}