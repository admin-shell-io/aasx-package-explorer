using System;
using System.IO;

using AasxAmlImExport;
using AasxIntegrationBase.AasForms;
using AdminShellNS;

namespace AasxToolkit.Instruction
{
    /// <summary>
    /// Specifies the context shared between the execution of the instructions.
    /// </summary>
    /// <remarks>This class encapsulates the global state
    /// so that it can be easier to identify.</remarks>
    public class Context
    {
        public AdminShellPackageEnv Package = null;
    }

    class Generate : Cli.Instruction
    {
        private readonly Context _context;
        private readonly string _jsonInitFile;

        public Generate(Context context, string jsonInitFile)
        {
            _context = context;
            _jsonInitFile = jsonInitFile;
        }

        public override Cli.ReturnCode Execute()
        {
            try
            {
                _context.Package = AasxToolkit.Generate.GeneratePackage(_jsonInitFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "Failed to generate the package: {0} at {1}", ex.Message, ex.StackTrace);
                return new Cli.ReturnCode(-1);
            }

            Console.Out.WriteLine("Package generated.");
            return null;
        }
    }

    class Load : Cli.Instruction
    {
        private readonly Context _context;
        private readonly string _path;

        public Load(Context context, string path)
        {
            _context = context;
            _path = path;
        }

        public override Cli.ReturnCode Execute()
        {
            Console.Out.WriteLine("Loading package {0} ..", _path);

            try
            {
                if (_path.EndsWith(".aml"))
                {
                    _context.Package = new AdminShellPackageEnv();
                    AmlImport.ImportInto(_context.Package, _path);
                }
                else
                {
                    _context.Package = new AdminShellPackageEnv(_path);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "While loading package {0}: {1} at {2}", _path, ex.Message, ex.StackTrace);
                return new Cli.ReturnCode(-1);
            }

            Console.Out.WriteLine($"Loaded package: {_path}");
            return null;
        }
    }

    class Save : Cli.Instruction
    {
        private readonly Context _context;
        private readonly string _path;

        public Save(Context context, string path)
        {
            _context = context;
            _path = path;
        }

        public override Cli.ReturnCode Execute()
        {
            if (_context.Package == null)
            {
                Console.Error.WriteLine(
                    "You must either generate a package (`gen`) or " +
                    "load a package (`load`) before you can save it.");
                return new Cli.ReturnCode(-1);
            }

            Console.Out.WriteLine("Writing package {0} ..", _path);

            try
            {
                if (Path.GetExtension(_path).ToLower() == ".aml")
                {
                    AmlExport.ExportTo(
                        _context.Package, _path, tryUseCompactProperties: false);
                }
                else
                {
                    _context.Package.SaveAs(_path, writeFreshly: true);
                    _context.Package.Close();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "While saving package {0}: {1} at {2}", _path, ex.Message, ex.StackTrace);
                return new Cli.ReturnCode(-1);
            }

            return null;
        }
    }

    class Validate : Cli.Instruction
    {
        private readonly Context _context;
        private readonly string _path;

        public Validate(Context context, string path)
        {
            _context = context;
            _path = path;
        }

        public override Cli.ReturnCode Execute()
        {
            try
            {
                var recs = new AasValidationRecordList();

                string extension = Path.GetExtension(_path).ToLower();
                if (extension == @".xml")
                {
                    Console.Out.WriteLine($"Validating file {_path} against XSD ..");

                    var stream = File.Open(_path, FileMode.Open, FileAccess.Read);
                    AasSchemaValidation.ValidateXML(recs, stream);
                }
                else if (extension == ".json")
                {
                    Console.Out.WriteLine($"Validating file {_path} against JSON ..");
                    var stream = File.Open(_path, FileMode.Open, FileAccess.Read);
                    AasSchemaValidation.ValidateJSONAlternative(recs, stream);
                }
                else
                {
                    throw new System.NotImplementedException(
                        $"Validation of the file with the extension {extension}: {_path}");
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
                    "While validating package {0}: {1} at {2}", _path, ex.Message, ex.StackTrace);
                return new Cli.ReturnCode(-1);
            }

            return null;
        }
    }

    class ExportTemplate : Cli.Instruction
    {
        private readonly Context _context;
        private readonly string _path;

        public ExportTemplate(Context context, string path)
        {
            _context = context;
            _path = path;
        }

        public override Cli.ReturnCode Execute()
        {
            if (_context.Package == null)
            {
                Console.Error.WriteLine(
                    "You must either generate a package (`gen`) or " +
                    "load a package (`load`) before you can export it as a template.");
                return new Cli.ReturnCode(-1);
            }

            Console.Out.WriteLine("Exporting to file {0} ..", _path);

            try
            {
                AasFormUtils.ExportAsTemplate(_context.Package, _path);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "While exporting package {0}: {1} at {2}", _path, ex.Message, ex.StackTrace);
                return new Cli.ReturnCode(-1);
            }

            Console.Out.WriteLine("Package {0} written.", _path);
            return null;
        }
    }

    class CheckAndFix : Cli.Instruction
    {
        private readonly Context _context;
        private readonly bool _shouldFix;

        public CheckAndFix(Context context, bool shouldFix)
        {
            _context = context;
            _shouldFix = shouldFix;
        }

        public override Cli.ReturnCode Execute()
        {
            try
            {
                if (_context.Package == null)
                {
                    Console.Error.WriteLine(
                        "You must either generate a package (`gen`) or " +
                        "load a package (`load`) before you can check it.");
                    return new Cli.ReturnCode(-1);
                }

                // validate
                var recs = _context.Package?.AasEnv?.ValidateAll();
                if (recs == null)
                {
                    throw new NotImplementedException(
                        "Validation returned null -- we do not know how to handle this situation.");
                }

                if (recs.Count > 0)
                {
                    Console.Out.WriteLine($"Found {recs.Count} issue(s):");
                    foreach (var rec in recs)
                        Console.Out.WriteLine(rec.ToString());

                    if (_shouldFix)
                    {
                        Console.Out.WriteLine($"Fixing all records..");
                        var i = _context.Package.AasEnv.AutoFix(recs);
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
                return new Cli.ReturnCode(-1);
            }

            return null;
        }
    }

    class Test : Cli.Instruction
    {
        private readonly Context _context;

        public Test(Context context)
        {
            _context = context;
        }

        public override Cli.ReturnCode Execute()
        {
            try
            {
                if (_context.Package == null)
                {
                    Console.Error.WriteLine(
                        "You must either generate a package (`gen`) or " +
                        "load a package (`load`) before you can test it.");
                    return new Cli.ReturnCode(-1);
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
                    sr, _context.Package.AasEnv, "", 0, so);

                // test debug
                foreach (var fr in sr.foundResults)
                    Console.Out.WriteLine(
                        "{0}|{1} = {2}", fr.qualifiedNameHead, fr.metaModelName, fr.foundText);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "While testing the package in RAM: {0} at {1}", ex.Message, ex.StackTrace);
                return new Cli.ReturnCode(-1);
            }

            return null;
        }
    }
}