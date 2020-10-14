﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

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
            var context = new Instruction.Context();

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

                    return new Cli.Parsing(
                        new Instruction.Generate(context, pth));
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

                    return new Cli.Parsing(new Instruction.Load(context, pth));
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

                    return new Cli.Parsing(new Instruction.Save(context, pth));
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
                    return new Cli.Parsing(new Instruction.ExportTemplate(context, cmdArgs[0]));
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

                    return new Cli.Parsing(new Instruction.Validate(context, pth));
                }
            );

            var cmdCheck = new Cli.Command(
                "check",
                "checks the AASX package in RAM.",
                new Cli.Arg[] { },
                (cmdArgs) => new Cli.Parsing(
                    new Instruction.CheckAndFix(context, false))
            );

            var cmdCheckAndFix = new Cli.Command(
                "check+fix",
                "checks and fixes the AASX package in RAM.",
                new Cli.Arg[] { },
                (cmdArgs) => new Cli.Parsing(
                    new Instruction.CheckAndFix(context, true))
            );

            var cmdTest = new Cli.Command(
                "test",
                "tests the AASX package in RAM.",
                new Cli.Arg[] { },
                (cmdArgs) => new Cli.Parsing(
                    new Instruction.Test(context))
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
                    cmdExportTemplate,
                    cmdValidate,
                    cmdCheck,
                    cmdCheckAndFix,
                    cmdTest
                }
            );

            var parsing = Cli.ParseInstructions(cmdLine, args);
            if (parsing.Errors != null)
            {
                var errorMsg = Cli.FormatParsingErrors(
                    args, parsing.AcceptedArgs, parsing.Errors);
                Console.Error.WriteLine(errorMsg);
                return -1;
            }

            var returnCode = Cli.Execute(parsing.Instructions);
            if (returnCode == null)
            {
                return 0;
            }

            return returnCode.Value;
        }
    }
}