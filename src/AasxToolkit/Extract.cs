/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using Extensions;
using System;

namespace AasxToolkit
{
    public static class Extract
    {
        public static void Extract2770Doc(
            AdminShellPackageEnv package,
            string findSys, string findClass, string targetFn)
        {
            // access
            if (package?.AasEnv?.Submodels == null || !findSys.HasContent()
                || !findClass.HasContent() || !targetFn.HasContent())
                return;

            var defs11 = AasxPredefinedConcepts.VDI2770v11.Static;
            var mm = MatchMode.Relaxed;

            // filter out Submodels
            foreach (var sm in package.AasEnv?.FindAllSubmodelGroupedByAAS((aas, sm) =>
            {
                if (true == sm?.SemanticId.Matches(
                    defs11.SM_ManufacturerDocumentation.SemanticId))
                    return true;

                foreach (var x in new[] {
                    "smart.festo.com/AAS/Submodel/ComputerAidedDesign/1/0",
                    "https://admin-shell.io/sandbox/idta/handover/MCAD/0/1/",
                    "https://admin-shell.io/sandbox/idta/handover/EFCAD/0/1/",
                    "https://admin-shell.io/sandbox/idta/handover/PLC/0/1/"
                })
                    if (true == sm?.SemanticId.Matches(
                        KeyTypes.Submodel, x, mm))
                        return true;

                return false;
            }))
            {
                // access
                if (sm.SubmodelElements == null)
                    continue;

                // look for Documents
                foreach (var smcDoc in
                    sm.SubmodelElements.FindAllSemanticIdAs<SubmodelElementCollection>(
                        defs11.CD_Document?.GetReference().GetAsExactlyOneKey(), mm))
                {
                    // access
                    if (smcDoc == null || smcDoc.Value == null)
                        continue;

                    foreach (var smcVer in
                        smcDoc.Value.FindAllSemanticIdAs<SubmodelElementCollection>(
                            defs11.CD_DocumentVersion?.GetReference().GetAsExactlyOneKey(), mm))
                    {
                        // access
                        if (smcVer == null || smcVer.Value == null)
                            continue;

                        // look for document classifications
                        var clsFound = false;
                        foreach (var smcClass in
                            smcDoc.Value.FindAllSemanticIdAs<SubmodelElementCollection>(
                                defs11.CD_DocumentClassification?.GetReference().GetAsExactlyOneKey(), mm))
                        {
                            // access
                            if (smcClass?.Value == null)
                                continue;

                            // shall be a 2770 classification
                            var classSys = "" + smcClass.Value.FindFirstSemanticIdAs<Property>(
                                    defs11.CD_ClassificationSystem?.GetReference().GetAsExactlyOneKey(), mm)?.Value;

                            // class infos
                            var classId = "" + smcClass.Value.FindFirstSemanticIdAs<Property>(
                                    defs11.CD_ClassId?.GetReference().GetAsExactlyOneKey(), mm)?.Value;

                            // found?
                            clsFound = clsFound || (findSys.Trim().ToLower() == classSys.Trim().ToLower()
                                && findClass.Trim().ToLower() == classId.Trim().ToLower());

                        }

                        // found?
                        if (!clsFound)
                            continue;

                        // digital file
                        var fl = smcVer.Value.FindFirstSemanticIdAs<File>(
                                    defs11.CD_DigitalFile?.GetReference().GetAsExactlyOneKey(), mm);
                        var fn = fl?.Value;
                        if (!fn.HasContent())
                            continue;

                        // try to make available
                        try
                        {
                            string inputFn = null;

                            if (fn.StartsWith("/"))
                            {
                                // 
                                // local access
                                //

                                inputFn = package.MakePackageFileAvailableAsTempFile(fn, keepFilename: true);
                            }

                            if (fn.StartsWith("http"))
                            {
                                // 
                                // download?
                                //

                                throw new NotImplementedException("missing http access");
                            }

                            Log.WriteLine($"Found document and providing temp file {inputFn} ..");

                            // how to make output name
                            string outputFn = System.IO.Path.GetFileName(inputFn);
                            if (targetFn != "*")
                                outputFn = targetFn;

                            // copy
                            Log.WriteLine($"  .. copy to target file name {outputFn} ..");
                            System.IO.File.Copy(inputFn, outputFn);

                            // success
                            Log.WriteLine($"DONE: {outputFn}");
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Error {0}", ex.Message);
                        }
                    }
                }
            }
        }
    }
}
