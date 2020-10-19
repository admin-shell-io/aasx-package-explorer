using System;
using System.IO;

using AasxAmlImExport;
using AasxIntegrationBase.AasForms;
using AdminShellNS;

namespace AasxToolkit.Instruction
{

    class Generate : Cli.IInstruction
    {
        public readonly string JsonInitFile;

        public Generate(string jsonInitFile)
        {
            JsonInitFile = jsonInitFile;
        }
    }

    class Load : Cli.IInstruction
    {
        public readonly string Path;

        public Load(string path)
        {
            Path = path;
        }
    }

    class Save : Cli.IInstruction
    {
        public readonly string Path;

        public Save(string path)
        {
            Path = path;
        }
    }

    class Validate : Cli.IInstruction
    {
        public readonly string Path;

        public Validate(string path)
        {
            Path = path;
        }
    }

    class ExportTemplate : Cli.IInstruction
    {
        public readonly string Path;

        public ExportTemplate(string path)
        {
            Path = path;
        }
    }

    class CheckAndFix : Cli.IInstruction
    {
        public readonly bool ShouldFix;

        public CheckAndFix(bool shouldFix)
        {
            ShouldFix = shouldFix;
        }
    }

    class Test : Cli.IInstruction
    {
        // Intentionally left empty
    }
}