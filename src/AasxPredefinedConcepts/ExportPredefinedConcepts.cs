/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPredefinedConcepts
{
    public static class ExportPredefinedConcepts
    {
        public static void Export(AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, string fn)
        {
            // access
            if (fn == null || env == null || sm == null || sm.idShort == null || sm.submodelElements == null)
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

                var usedCds = new Dictionary<string, AdminShell.ConceptDescription>();
                foreach (var sme in sm.submodelElements?.FindDeep<AdminShell.SubmodelElement>())
                {
                    // for SME, try to lookup CD
                    if (sme.semanticId == null)
                        continue;

                    var cd = env.FindConceptDescription(sme.semanticId.GetAsExactlyOneKey());
                    if (cd == null)
                        continue;

                    // name?
                    var ids = cd.idShort;
                    if ((ids == null || ids == "") && cd.GetIEC61360() != null)
                        ids = cd.GetIEC61360().shortName?.GetDefaultStr();
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

                var keySM = "SM_" + sm.idShort;
                if (true)
                {
                    // ok, for Serialization we just want the plain element with no BLOBs..
                    var settings = new JsonSerializerSettings();
                    settings.ContractResolver = new AdminShellConverters.AdaptiveFilterContractResolver(
                        deep: false, complete: false);
                    var jsonStr = JsonConvert.SerializeObject(sm, Formatting.Indented, settings);

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
                    var settings = new JsonSerializerSettings();
                    settings.ContractResolver = new AdminShellConverters.AdaptiveFilterContractResolver(
                        deep: false, complete: false);
                    var jsonStr = JsonConvert.SerializeObject(usedCds[k], Formatting.Indented, settings);

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

                snippets.WriteLine($"this.{keySM} = bs.RetrieveReferable<AdminShell.Submodel>(\"{keySM}\");");
                foreach (var k in usedCds.Keys)
                    snippets.WriteLine($"this.{k} = bs.RetrieveReferable<AdminShell.ConceptDescription>(\"{k}\");");

                snippets.WriteLine();
            }
        }
    }
}
