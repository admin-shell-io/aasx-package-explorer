/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

namespace AasxPredefinedConcepts
{
    public static class ExportPredefinedConcepts
    {
        public static void Export(Aas.Environment env, Aas.ISubmodel sm, string fn)
        {
            // access
            if (fn == null || env == null || sm == null || sm.IdShort == null || sm.SubmodelElements == null)
                return;

            // make text file
            // ReSharper disable once ConvertToUsingDeclaration
            using (var snippets = new System.IO.StreamWriter(fn))
            {
                // Phase (1) which ConceptDescriptions need to be exported
                // Phase (2) export CDs as JSON
                // Phase (3) export SubModel as JSON without SMEs
                // Phase (4) export list of IDs used
                // Phase (5) generate look ups

                // Phase (1) which ConceptDescriptions need to be exported

                snippets.WriteLine("Phase (1) Check, which ConceptDescriptions need to be exported:");
                snippets.WriteLine("===============================================================");

                var usedCds = new Dictionary<string, Aas.IConceptDescription>();
                foreach (var sme in sm.SubmodelElements?.FindDeep<Aas.ISubmodelElement>())
                {
                    // for SME, try to lookup CD
                    if (sme.SemanticId == null)
                        continue;

                    var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                    if (cd == null)
                        continue;

                    // name?
                    var ids = cd.IdShort;
                    if ((ids == null || ids == "") && cd.GetIEC61360() != null)
                        ids = cd.GetIEC61360().ShortName?.GetDefaultString();
                    if (ids == null || ids == "")
                        continue;
                    ids = "CD_" + ids;

                    // only one in dictionary!
                    if (usedCds.ContainsKey(ids))
                    {
                        snippets.WriteLine($"Information: duplicate evaluated CD idShort {ids} .. skipping!");
                        continue;
                    }
                    snippets.WriteLine($"Identified {ids} to be exported");

                    // add
                    usedCds.Add(ids, cd);
                }
                snippets.WriteLine();

                // Phase (2) export SubModel as JSON without SMEs
                string message = "Phase (2) export SubModel as JSON without SMEs. " +
                    "Paste the following into appropriate JSON (resource) file:";
                snippets.WriteLine(message);
                snippets.WriteLine(new String('=', message.Length));

                var keySM = "SM_" + sm.IdShort;
                if (true)
                {
                    // JsonNET does not work, because modelType is not serialized
#if __old__
                    // ok, for Serialization we just want the plain element with no BLOBs..
                    var settings = new JsonSerializerSettings();
                    settings.ContractResolver = new AdminShellConverters.AdaptiveFilterContractResolver(
                        deep: false, complete: false);
                    settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    var jsonStr = JsonConvert.SerializeObject(sm, Formatting.Indented, settings);
#else
                    // challenge: SM without SMEs
                    var smClone = sm.Copy();
                    smClone.SubmodelElements = null;
                    var jsonStr = Jsonization.Serialize.ToJsonObject(smClone)
                        .ToJsonString(new System.Text.Json.JsonSerializerOptions()
                        {
                            WriteIndented = true
                        });
#endif

                    // export
                    snippets.WriteLine($"\"{keySM}\" : {jsonStr},");
                }

                snippets.WriteLine();

                // Phase (3) export CDs
                message = "Phase (3) export CDs as JSON. Paste the following into appropriate JSON (resource) file:";
                snippets.WriteLine(message);
                snippets.WriteLine(new String('=', message.Length));

                foreach (var k in usedCds.Keys)
                {
                    // ok, for Serialization we just want the plain element with no BLOBs..
#if __old__
                    var settings = new JsonSerializerSettings();
                    settings.ContractResolver = new AdminShellConverters.AdaptiveFilterContractResolver(
                        deep: false, complete: false);
                    settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    var jsonStr = JsonConvert.SerializeObject(usedCds[k], Formatting.Indented, settings);
#else
                    var jsonStr = Jsonization.Serialize.ToJsonObject(usedCds[k])
                        .ToJsonString(new System.Text.Json.JsonSerializerOptions()
                        {
                            WriteIndented = true
                        });
#endif
                    // export
                    snippets.WriteLine($"\"{k}\" : {jsonStr},");
                }

                snippets.WriteLine();

                // Phase (4) export list of IDs used
                message = "Phase (4) export list of IDs used. " +
                    "Paste the following into appropriate C# file and reformat it:";
                snippets.WriteLine(message);
                snippets.WriteLine(new String('=', message.Length));

                snippets.WriteLine(keySM);
                foreach (var k in usedCds.Keys)
                    snippets.WriteLine($"\t\t{k},");

                snippets.WriteLine();

                // Phase (5) generate look ups
                message = "Phase (5) generate look ups. Paste the following into appropriate C# file and reformat it:";
                snippets.WriteLine(message);
                snippets.WriteLine(new String('=', message.Length));

                snippets.WriteLine($"this.{keySM} = bs.RetrieveReferable<Submodel>(\"{keySM}\");");
                foreach (var k in usedCds.Keys)
                    snippets.WriteLine($"this.{k} = bs.RetrieveReferable<ConceptDescription>(\"{k}\");");

                snippets.WriteLine();
            }
        }
    }
}
