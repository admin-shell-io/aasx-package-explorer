/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;

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
            var mm = AdminShell.Key.MatchMode.Relaxed;                     

            // filter out Submodels
            foreach (var sm in package.AasEnv?.FindAllSubmodelGroupedByAAS((aas, sm) =>
            {
                if (true == sm?.GetSemanticId().Matches(
                    defs11.SM_ManufacturerDocumentation.GetSemanticId(), mm))
                    return true;

                foreach (var x in new[] {
                    "smart.festo.com/AAS/Submodel/ComputerAidedDesign/1/0",
                    "https://admin-shell.io/sandbox/idta/handover/MCAD/0/1/",
                    "https://admin-shell.io/sandbox/idta/handover/EFCAD/0/1/",
                    "https://admin-shell.io/sandbox/idta/handover/PLC/0/1/"
                })
                    if (true == sm?.GetSemanticId().Matches(
                        AdminShell.Key.Submodel, false, AdminShell.Identification.IRI, x, mm))
                        return true;

                return false;
            }))
            {
                // access
                if (sm.submodelElements == null)
                    continue;

                // look for Documents
                foreach (var smcDoc in
                    sm.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        defs11.CD_Document?.GetReference(), mm))
                {
                    // access
                    if (smcDoc == null || smcDoc.value == null)
                        continue;

                    foreach (var smcVer in
                        smcDoc.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                            defs11.CD_DocumentVersion?.GetReference(), mm))
                    {
                        // access
                        if (smcVer == null || smcVer.value == null)
                            continue;

                        // look for document classifications
                        var clsFound = false;
                        foreach (var smcClass in
                            smcDoc.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                                defs11.CD_DocumentClassification?.GetReference(), mm))
                        {
                            // access
                            if (smcClass?.value == null)
                                continue;

                            // shall be a 2770 classification
                            var classSys = "" + smcClass.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defs11.CD_ClassificationSystem?.GetReference(), mm)?.value;

                            // class infos
                            var classId = "" + smcClass.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                    defs11.CD_ClassId?.GetReference(), mm)?.value;

                            // found?
                            clsFound = clsFound || (findSys.Trim().ToLower() == classSys.Trim().ToLower()
                                && findClass.Trim().ToLower() == classId.Trim().ToLower());

                        }

                        // found?
                        if (!clsFound)
                            continue;

                        // digital file
                        var fl = smcVer.value.FindFirstSemanticIdAs<AdminShell.File>(
                                    defs11.CD_DigitalFile?.GetReference(), mm);
                        var fn = fl?.value;
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
