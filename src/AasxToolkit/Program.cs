/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
Many parts of the program became obsolete over time (such as a method <c>CreateSubmodelDocumentation</c>).
The obsoleted code was moved to obsolete/2020-07-20/AasxGenerate/Program.cs for archiving purposes and removed
from this file for readability.
 */

namespace AasxToolkit
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            System.Environment.ExitCode = MainWithExitCode(args);
        }

        public static int MainWithExitCode(string[] args)
        {
            var nl = System.Environment.NewLine;

            var cmdGen = new Cli.Command(
                "gen",
                "generates package in RAM with the internal filename set to *.",
                new[]
                {
                    new Cli.Arg(
                        "init-json-file",
                        "Initialization JSON file (*.json)")
                },
                cmdArgs =>
                {
                    var pth = cmdArgs[0];
                    if (Path.GetExtension(pth).ToLower() != ".json")
                    {
                        return new Cli.Parsing(
                            new[]
                            {
                                $"Expected a JSON file, but the file does not have a .json extension: " +
                                pth
                            });
                    }

                    return new Cli.Parsing(new Instruction.Generate(pth));
                }
            );

            var packageExtensions = new[] { ".xml", ".json", ".aasx", ".aml" };

            var cmdLoad = new Cli.Command(
                "load",
                "loads the AASX package into RAM.",
                new[]
                {
                    new Cli.Arg(
                        "package-file",
                        "Path to the AASX package. " +
                        $"It can be one of: {string.Join(", ", packageExtensions)}"),
                },
                (cmdArgs) =>
                {
                    var pth = cmdArgs[0];

                    if (!packageExtensions.Contains(Path.GetExtension(pth).ToLower()))
                    {
                        return new Cli.Parsing(
                            new[] { $"Unexpected file extension: {pth}" });
                    }

                    return new Cli.Parsing(new Instruction.Load(pth));
                }
            );

            var cmdSave = new Cli.Command(
                "save",
                "saves the AASX package from RAM to a file.",
                new[]
                {
                    new Cli.Arg(
                        "package-file",
                        "Path to the AASX package. " +
                        $"It can be one of: {string.Join(", ", packageExtensions)}"),
                },
                (cmdArgs) =>
                {
                    var pth = cmdArgs[0];
                    if (!packageExtensions.Contains(Path.GetExtension(pth).ToLower()))
                    {
                        return new Cli.Parsing(
                            new[] { $"Unexpected file extension: {pth}" });
                    }

                    return new Cli.Parsing(new Instruction.Save(pth));
                }
            );

            var cmdExDoc = new Cli.Command(
                "exdoc",
                "extracts digital files from VDI2770 style Submodels",
                new[]
                {
                    new Cli.Arg(
                        "doc-sys",
                        "ClassificationSystem of the document. "),
                    new Cli.Arg(
                        "doc-class",
                        "ClassId of the document. "),
                    new Cli.Arg(
                        "target",
                        "Target filename. If '*' will extract one or multiple files with filename printed out. "),
                },
                (cmdArgs) =>
                {
                    return new Cli.Parsing(new Instruction.ExtractDoc(cmdArgs[0], cmdArgs[1], cmdArgs[2]));
                }
            );

            var cmdExportTemplate = new Cli.Command(
                "export-template",
                "saves the AASX package from RAM to a template file.",
                new[]
                {
                    new Cli.Arg(
                        "template-file",
                        "Path to the template file."),
                },
                (cmdArgs) =>
                {
                    return new Cli.Parsing(new Instruction.ExportTemplate(cmdArgs[0]));
                }
            );

            var cmdExportCst = new Cli.Command(
                "export-cst",
                "exports AASX data in Siemens Teamcenter CST format.",
                new[]
                {
                    new Cli.Arg(
                        "export-file",
                        "Path to the export file."),
                },
                (cmdArgs) =>
                {
                    return new Cli.Parsing(new Instruction.ExportCst(cmdArgs[0]));
                }
            );

            var validationExtensions = new[] { ".xml", ".json" };
            var cmdValidate = new Cli.Command(
                "validate",
                "validates the given AASX package.",
                new[]
                {
                    new Cli.Arg(
                        "package-file",
                        "Path to the AASX package to be validated. " +
                        $"It can be one of: {string.Join(", ", validationExtensions)}"),
                },
                (cmdArgs) =>
                {
                    var pth = cmdArgs[0];
                    if (!validationExtensions.Contains(Path.GetExtension(pth).ToLower()))
                    {
                        return new Cli.Parsing(
                            new[] { $"Unexpected file extension: {pth}" });
                    }

                    return new Cli.Parsing(new Instruction.Validate(pth));
                }
            );

            var cmdCheck = new Cli.Command(
                "check",
                "checks the AASX package in RAM.",
                new Cli.Arg[] { },
                (cmdArgs) => new Cli.Parsing(new Instruction.CheckAndFix(false))
            );

            var cmdCheckAndFix = new Cli.Command(
                "check+fix",
                "checks and fixes the AASX package in RAM.",
                new Cli.Arg[] { },
                (cmdArgs) => new Cli.Parsing(new Instruction.CheckAndFix(true))
            );

            var cmdTest = new Cli.Command(
                "test",
                "tests the AASX package in RAM.",
                new Cli.Arg[] { },
                (cmdArgs) => new Cli.Parsing(new Instruction.Test())
            );

            var cmdHelp = new Cli.Command(
                "help",
                "If this command is specified, the program will only display the help message and " +
                "immediately exit (aliases: --help, -help, /help, -h and /h).",
                new Cli.Arg[] { },
                (cmdArgs) =>
                {
                    throw new InvalidOperationException(
                        "The help command should never be parsed. It should be handled *before* " +
                        "the command-line arguments are parsed. The command is declared only so that it appears " +
                        "in the help message.");
                }
            );

            var cmdLine = new Cli.CommandLine(
                nameof(AasxToolkit),
                $"Toolkit for generating and manipulating AASX files.{nl}" +
                "See LICENSE.txt for copyright information.",
                new List<Cli.Command>
                {
                    cmdGen,
                    cmdLoad,
                    cmdSave,
                    cmdExDoc,
                    cmdExportTemplate,
                    cmdExportCst,
                    cmdValidate,
                    cmdCheck,
                    cmdCheckAndFix,
                    cmdTest,
                    cmdHelp
                }
            );

            // # Handle the special "help" command

            var helpAliases = new HashSet<string> { "help", "--help", "-help", "/help", "-h", "/h" };
            if (args.Length == 0 || args.Any(arg => helpAliases.Contains(arg)))
            {
                Console.WriteLine(Cli.GenerateUsageMessage(cmdLine));
                return 0;
            }

            // # Parse

            var parsing = Cli.ParseInstructions(cmdLine, args);
            if (parsing.Errors != null)
            {
                var errorMsg = Cli.FormatParsingErrors(
                    args, parsing.AcceptedArgs, parsing.Errors);
                Console.Error.WriteLine(errorMsg);
                return -1;
            }

            // # Execute

            int returnCode = Execution.Execute(parsing.Instructions);
            return returnCode;
        }
    }
}
