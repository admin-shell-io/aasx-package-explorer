﻿using System.Collections.Generic;
using AasFormUtils = AasxIntegrationBase.AasForms.AasFormUtils;
using AasSchemaValidation = AdminShellNS.AasSchemaValidation;
using AasValidationRecordList = AdminShellNS.AasValidationRecordList;
using AdminShell = AdminShellNS.AdminShell;
using AdminShellPackageEnv = AdminShellNS.AdminShellPackageEnv;
using AdminShellUtil = AdminShellNS.AdminShellUtil;
using AdminShellV20 = AdminShellNS.AdminShellV20;
using AmlExport = AasxAmlImExport.AmlExport;
using AmlImport = AasxAmlImExport.AmlImport;
using Console = System.Console;
using Exception = System.Exception;
using File = System.IO.File;
using FileAccess = System.IO.FileAccess;
using FileMode = System.IO.FileMode;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;

namespace AasxToolkit
{
    /// <summary>
    /// This module handles the execution of the instructions parsed from the command line.
    /// Please see <see cref="AasxToolkit.Cli"/> for the overall design.
    /// </summary>
    public static class Execution
    {
        public static int Execute(IReadOnlyList<Instruction.IInstruction> instructions)
        {
            // # Context
            AdminShellPackageEnv package = null;

            // # Execution loop

            foreach (var instruction in instructions)
            {
                // The instruction dispatch is intentionally implemented as a switch statement and case blocks
                // instead of encapsulating the individual blocks in separate methods or classes. The shared context
                // is clearly signaled, while we do not gain much in clarity with separate functions and classes.
                //
                // Moreover, the pre-emptive stops in the execution logic were much more difficult to get right with the
                // methods than with this fairly simple construct.
                switch (instruction)
                {
                    case Instruction.Generate generate:
                        {
                            try
                            {
                                package = AasxToolkit.Generate.GeneratePackage(generate.JsonInitFile);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(
                                    "Failed to generate the package: {0} at {1}", ex.Message, ex.StackTrace);
                                return -1;
                            }

                            Console.Out.WriteLine("Package generated.");
                            break;
                        }
                    case Instruction.Load load:
                        {
                            Console.Out.WriteLine("Loading package {0} ..", load.Path);

                            try
                            {
                                if (load.Path.EndsWith(".aml"))
                                {
                                    package = new AdminShellPackageEnv();
                                    AmlImport.ImportInto(package, load.Path);
                                }
                                else
                                {
                                    package = new AdminShellPackageEnv(load.Path);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(
                                    "While loading package {0}: {1} at {2}", load.Path, ex.Message, ex.StackTrace);
                                return -1;
                            }

                            Console.Out.WriteLine($"Loaded package: {load.Path}");
                            break;
                        }
                    case Instruction.Save save:
                        {
                            if (package == null)
                            {
                                Console.Error.WriteLine(
                                    "You must either generate a package (`gen`) or " +
                                    "load a package (`load`) before you can save it.");
                                return -1;
                            }

                            Console.Out.WriteLine("Writing package {0} ..", save.Path);

                            try
                            {
                                if (Path.GetExtension(save.Path).ToLower() == ".aml")
                                {
                                    AmlExport.ExportTo(
                                        package, save.Path, tryUseCompactProperties: false);
                                }
                                else
                                {
                                    package.SaveAs(save.Path, writeFreshly: true);
                                    package.Close();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(
                                    "While saving package {0}: {1} at {2}", save.Path, ex.Message, ex.StackTrace);
                                return -1;
                            }

                            break;
                        }
                    case Instruction.Validate validate:
                        {
                            try
                            {
                                var recs = new AasValidationRecordList();

                                string extension = Path.GetExtension(validate.Path).ToLower();
                                if (extension == @".xml")
                                {
                                    Console.Out.WriteLine($"Validating file {validate.Path} against XSD ..");

                                    var stream = File.Open(validate.Path, FileMode.Open, FileAccess.Read);
                                    AasSchemaValidation.ValidateXML(recs, stream);
                                }
                                else if (extension == ".json")
                                {
                                    Console.Out.WriteLine($"Validating file {validate.Path} against JSON ..");
                                    var stream = File.Open(validate.Path, FileMode.Open, FileAccess.Read);
                                    AasSchemaValidation.ValidateJSONAlternative(recs, stream);
                                }
                                else
                                {
                                    throw new System.InvalidOperationException(
                                        $"Validation of the file with the extension {extension} " +
                                        $"is not supported: {validate.Path}");
                                }

                                if (recs.Count > 0)
                                {
                                    Console.Out.WriteLine($"Found {recs.Count} issue(s):");
                                    foreach (var r in recs)
                                        Console.Out.WriteLine(r.ToString());
                                }
                                else
                                {
                                    Console.Out.WriteLine($"Found no issues.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(
                                    $"While validating the package {validate.Path}: " +
                                    $"{ex.Message} at {ex.StackTrace}");
                                return -1;
                            }

                            break;
                        }
                    case Instruction.ExportTemplate exportTemplate:
                        {
                            if (package == null)
                            {
                                Console.Error.WriteLine(
                                    "You must either generate a package (`gen`) or " +
                                    "load a package (`load`) before you can export it as a template.");
                                return -1;
                            }

                            Console.Out.WriteLine("Exporting to file {0} ..", exportTemplate.Path);

                            try
                            {
                                AasFormUtils.ExportAsTemplate(package, exportTemplate.Path);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(
                                    "While exporting package {0}: {1} at {2}",
                                    exportTemplate.Path, ex.Message, ex.StackTrace);
                                return -1;
                            }

                            Console.Out.WriteLine("Package {0} written.", exportTemplate.Path);
                            break;
                        }
                    case Instruction.CheckAndFix checkAndFix:
                        {
                            try
                            {
                                if (package == null)
                                {
                                    Console.Error.WriteLine(
                                        "You must either generate a package (`gen`) or " +
                                        "load a package (`load`) before you can check it.");
                                    return -1;
                                }

                                // validate
                                var recs = package?.AasEnv?.ValidateAll();
                                if (recs == null)
                                {
                                    throw new InvalidOperationException(
                                        "Validation returned null -- we do not know how to handle this situation.");
                                }

                                if (recs.Count > 0)
                                {
                                    Console.Out.WriteLine($"Found {recs.Count} issue(s):");
                                    foreach (var rec in recs)
                                        Console.Out.WriteLine(rec.ToString());

                                    if (checkAndFix.ShouldFix)
                                    {
                                        Console.Out.WriteLine($"Fixing all records..");
                                        var i = package.AasEnv.AutoFix(recs);
                                        Console.Out.WriteLine($".. gave result {i}.");
                                    }
                                }
                                else
                                {
                                    Console.Out.WriteLine($"Found no issues.");
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(
                                    "While checking the package in RAM: {0} at {1}", ex.Message, ex.StackTrace);
                                return -1;
                            }

                            break;
                        }
                    case Instruction.Test _:
                        {
                            try
                            {
                                if (package == null)
                                {
                                    Console.Error.WriteLine(
                                        "You must either generate a package (`gen`) or " +
                                        "load a package (`load`) before you can test it.");
                                    return -1;
                                }

                                var prop = AdminShellV20.Property.CreateNew("test", "cat01");
                                prop.semanticId = new AdminShellV20.SemanticId(
                                    AdminShellV20.Reference.CreateNew(
                                        "GlobalReference", false, "IRI",
                                        "www.admin-shell.io/nonsense"));

                                var fil = AdminShellV20.File.CreateNew("test", "cat01");
                                fil.semanticId = new AdminShellV20.SemanticId(
                                    AdminShellV20.Reference.CreateNew(
                                        "GlobalReference", false, "IRI",
                                        "www.admin-shell.io/nonsense"));
                                fil.parent = fil;

                                var so = new AdminShellUtil.SearchOptions();
                                so.allowedAssemblies = new[] { typeof(AdminShell).Assembly };
                                var sr = new AdminShellUtil.SearchResults();

                                AdminShellUtil.EnumerateSearchable(
                                    sr, package.AasEnv, "", 0, so);

                                // test debug
                                foreach (var fr in sr.foundResults)
                                    Console.Out.WriteLine(
                                        "{0}|{1} = {2}", fr.qualifiedNameHead, fr.metaModelName, fr.foundText);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(
                                    "While testing the package in RAM: {0} at {1}", ex.Message, ex.StackTrace);
                                return -1;
                            }

                            break;
                        }
                    default:
                        throw ExhaustiveMatching.ExhaustiveMatch.Failed(instruction);
                }
            }

            return 0;
        }
    }
}