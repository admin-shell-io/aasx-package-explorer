/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

namespace AasxToolkit.Instruction
{
    /// <summary>
    /// Represents a single instruction to the program.
    /// </summary>
    /// <remarks>The instruction interface is intentionally defined here (and not in a different file) so that we
    /// can extract this code to a completely separate NuGet package in the future, if necessary.</remarks>
    [ExhaustiveMatching.Closed(
        typeof(Generate),
        typeof(Load),
        typeof(Save),
        typeof(ExtractDoc),
        typeof(Validate),
        typeof(ExportTemplate),
        typeof(ExportCst),
        typeof(CheckAndFix),
        typeof(Test))]
    public interface IInstruction { }

    public class Generate : IInstruction
    {
        public readonly string JsonInitFile;

        public Generate(string jsonInitFile)
        {
            JsonInitFile = jsonInitFile;
        }
    }

    public class Load : IInstruction
    {
        public readonly string Path;

        public Load(string path)
        {
            Path = path;
        }
    }

    public class Save : IInstruction
    {
        public readonly string Path;

        public Save(string path)
        {
            Path = path;
        }
    }

    public class ExtractDoc : IInstruction
    {
        public readonly string DocSys;
        public readonly string DocClass;
        public readonly string Target;

        public ExtractDoc(string docSys, string docClass, string target)
        {
            DocSys = docSys;
            DocClass = docClass;
            Target = target;
        }
    }

    public class Validate : IInstruction
    {
        public readonly string Path;

        public Validate(string path)
        {
            Path = path;
        }
    }

    public class ExportTemplate : IInstruction
    {
        public readonly string Path;

        public ExportTemplate(string path)
        {
            Path = path;
        }
    }

    public class ExportCst : IInstruction
    {
        public readonly string Path;

        public ExportCst(string path)
        {
            Path = path;
        }
    }

    public class CheckAndFix : IInstruction
    {
        public readonly bool ShouldFix;

        public CheckAndFix(bool shouldFix)
        {
            ShouldFix = shouldFix;
        }
    }

    public class Test : IInstruction
    {
        // Intentionally left empty
    }
}
