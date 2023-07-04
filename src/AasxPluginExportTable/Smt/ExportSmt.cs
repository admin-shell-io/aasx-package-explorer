/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using System.Collections;
using System.Drawing.Imaging;
using AasxPluginExportTable.Uml;
using AasxPluginExportTable.Table;
using System.Runtime.Intrinsics.X86;
using AnyUi;

namespace AasxPluginExportTable.Smt
{
    /// <summary>
    /// This class allows exporting a Submodel to an AsciiDoc specification.
    /// The general approach is to identify several dedicated SME (mostly BLOBs) and
    /// to chunk together their AsciiDoc contents.
    /// </summary>
    public class ExportSmt
    {
        protected LogInstance _log = null;
        protected AdminShellPackageEnv _package = null;
        protected Aas.ISubmodel _srcSm = null;
        protected ExportTableOptions _optionsAll = null;
        protected ExportSmtRecord _optionsSmt = null;
        protected string _tempDir = "";
        protected StringBuilder _adoc = new StringBuilder();
        protected bool _singleFile = true;

        protected void ProcessTextBlob(string header, Aas.IBlob blob)
        {
            // any content
            if (blob?.Value == null || blob.Value.Length < 1)
                return;

            // may wrap
            var text = System.Text.Encoding.UTF8.GetString(blob.Value);
            if (_optionsSmt.WrapLines >= 10)
            {
                text = AdminShellUtil.WrapLinesAtColumn(text, _optionsSmt.WrapLines);
            }

            // simply add
            if (header?.HasContent() == true)
                _adoc.AppendLine("");
            _adoc.AppendLine(header + text);
        }

        protected void ProcessImageLink(Aas.ISubmodelElement sme)
        {
            // first get to the data
            byte[] data = null;
            string dataExt = ".bin";
            if (sme is Aas.IFile smeFile)
            {
                data = _package?.GetByteArrayFromUriOrLocalPackage(smeFile.Value);
                dataExt = Path.GetExtension(smeFile.Value);
            }

            if (sme is Aas.IBlob smeBlop && smeBlop.Value != null)
            {
                // the BLOB may contain direct binary data or 
                // intends to transport text data only
                var convertStrBase64 = AdminShellUtil.CheckIfAsciiOnly(smeBlop.Value);

                var isTxtFmt = AdminShellUtil.CheckForTextContentType(smeBlop.ContentType);
                if (isTxtFmt)
                    convertStrBase64 = false;

                if (convertStrBase64)
                {
                    // assume base64 coded string
                    string strData = System.Text.Encoding.UTF8.GetString(smeBlop.Value);
                    data = System.Convert.FromBase64String(strData);
                }
                else
                    data = smeBlop.Value;

                // assume png
                dataExt = AdminShellUtil.GuessImageTypeExtension(data) ?? ".png";
                if (isTxtFmt)
                    dataExt = ".txt";
            }

            if (data == null)
            {
                _log?.Error("No image data found in AAS element {0}", 
                    sme?.GetReference()?.ToStringExtended(1));
                return;
            }

            if (!dataExt.HasContent())
            {
                _log?.Error("No data format extension found in AAS element {0}", 
                    sme?.GetReference()?.ToStringExtended(1));
                return;
            }

            // check if to link in text?
            var doLink = true;
            var q = sme.HasExtensionOfName("ExportSmt.Args");
            var args = ExportSmtArguments.Parse(q?.Value);
            if (args?.noLink == true)
                doLink = false;

            // determine (automatic) target file name
            var targetName = "image_" + Path.GetRandomFileName().Replace(".", "_");
            if (sme.IdShort.HasContent())
            {
                targetName = AdminShellUtil.FilterFriendlyName(sme.IdShort);

                int p = targetName.ToLower().LastIndexOf("_dot_");
                if (p >= 0)
                {
                    dataExt = "." + targetName.Substring(p + "_dot_".Length);
                    targetName = targetName.Substring(0, p);
                }
            }

            var fn = targetName + dataExt;

            // may be overruled?
            if (args?.fileName?.HasContent() == true)
                fn = args.fileName;

            // save absolute
            var absFn = Path.Combine(_tempDir, fn);
            File.WriteAllBytes(absFn, data);
            _log?.Info("Image data with {0} bytes writen to {1}.", data.Length, absFn);

            // create link text
            var astr = "";
            if (args?.width != null)
                astr = $"width=\"{AdminShellUtil.FromDouble(args.width ?? 0.0, "{0:0.0}")}%\"";
            if (doLink)
            {
                _adoc.AppendLine("");
                _adoc.AppendLine($"image::{fn}[{astr}]");
                _adoc.AppendLine("");
            }
        }

        protected void ProcessUml(Aas.IReferenceElement refel)
        {
            // access
            if (_package?.AasEnv == null || refel == null)
                return;

            // try find target of reference
            var target = _package?.AasEnv.FindReferableByReference(refel.Value);
            if (target == null)
            {
                _log?.Error("ExportSMT: No target reference for UML found in {0}", 
                    refel.GetReference()?.ToStringExtended(1));
                return;
            }

            // check arguments
            var q = refel.HasExtensionOfName("ExportSmt.Args");
            var args = ExportSmtArguments.Parse(q?.Value);
            var processDepth = args?.depth ?? int.MaxValue;

            // determine (automatic) target file name
            var pumlName = "uml_" + Path.GetRandomFileName().Replace(".","_");
            if (refel.IdShort.HasContent())
                pumlName = AdminShellUtil.FilterFriendlyName(refel.IdShort);
            var pumlFn = pumlName + ".puml";
            var absPumlFn = Path.Combine(_tempDir, pumlFn);

            // make options
            var umlOptions = new ExportUmlRecord();
            if (args?.uml != null)
                umlOptions = args.uml;

            // make writer
            var writer = new PlantUmlWriter();
            writer.StartDoc(umlOptions);
            writer.ProcessTopElement(target, processDepth);
            writer.ProcessPost();
            _log?.Info("ExportSMT: writing PlantUML to {0} ..", absPumlFn);
            writer.SaveDoc(absPumlFn);

            // include file into AsciiDoc
            _adoc.AppendLine("");
            _adoc.AppendLine($"[plantuml, {pumlName}, svg]");
            _adoc.AppendLine("----");
            _adoc.AppendLine("include::" + pumlFn + "[]");
            _adoc.AppendLine("----");
            _adoc.AppendLine("");
        }

        protected void ProcessTables(Aas.IReferenceElement refel)
        {
            // access
            if (_package?.AasEnv == null || refel == null)
                return;

            // try find target of reference
            var target = _package?.AasEnv.FindReferableByReference(refel.Value);
            if (target == null)
            {
                _log?.Error("ExportSMT: No target reference for Tables found in {0}",
                    refel.GetReference()?.ToStringExtended(1));
                return;
            }

            // find options for tables
            if (_optionsAll?.Presets == null || _optionsSmt == null 
                || _optionsSmt.PresetTables < 0 
                || _optionsSmt.PresetTables > _optionsAll.Presets.Count)
            {
                _log?.Error("ExportSMT: Error accessing selected table presets for conversion.");
                return;
            }
            var optionsTable = _optionsAll.Presets[_optionsSmt.PresetTables];

            // check arguments
            var q = refel.HasExtensionOfName("ExportSmt.Args");
            var args = ExportSmtArguments.Parse(q?.Value);
            var processDepth = int.MaxValue;
            if (args?.depth != null)
            {
                processDepth = (int) args.depth;
                optionsTable.NoHeadings = true;
            }

            // determine (automatic) target file name
            var tableFn = "table_" + Path.GetRandomFileName().Replace(".", "_");
            if (refel.IdShort.HasContent())
                tableFn = AdminShellUtil.FilterFriendlyName(refel.IdShort);
            tableFn += ".adoc";
            var absTableFn = Path.Combine(_tempDir, tableFn);

            // may change, if to include
            if (_optionsSmt.IncludeTables)
            {
                absTableFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".adoc");
            }

            // start export
            _log?.Info("ExportSMT: Starting table export for element {0} ..",
                refel.GetReference()?.ToStringExtended(1));

            var ticket = new AasxMenuActionTicket();

            AnyUiDialogueTable.Export(
                _optionsAll, optionsTable, absTableFn, 
                target, _package?.AasEnv, ticket, _log, maxDepth: processDepth);

            // include file into AsciiDoc
            if (_optionsSmt.IncludeTables)
            {
                // read file, append
                var lines = File.ReadAllLines(absTableFn);
                
                _adoc.AppendLine("");
                _adoc.AppendLine("// Table generated from " + refel.GetReference()?.ToStringExtended(1));
                _adoc.AppendLine("");

                foreach (var ln in lines)
                    _adoc.AppendLine(ln);

                _adoc.AppendLine("");
            }
            else
            {
                // include file
                _adoc.AppendLine("");
                _adoc.AppendLine("include::" + tableFn + "[]");
                _adoc.AppendLine("");
            }
        }

        public void ExportSmtToFile(
            LogInstance log,
            AnyUiContextPlusDialogs displayContext,
            AdminShellPackageEnv package,
            Aas.ISubmodel submodel,
            ExportTableOptions optionsAll,
            ExportSmtRecord optionsSmt,
            string fn)
        {
            // access
            if (optionsSmt == null || submodel == null || optionsSmt == null || !fn.HasContent())
                return;
            _log = log;
            _package = package;
            _srcSm = submodel;
            _optionsAll = optionsAll;
            _optionsSmt = optionsSmt;

            // decide to write singleFile?
            _singleFile = fn.ToLower().EndsWith(".adoc");

            // create temp directory
            _tempDir = AdminShellUtil.GetTemporaryDirectory();
            log?.Info("ExportSmt: using temp directory {0} ..", _tempDir);

            // predefined semantic ids
            var defs = AasxPredefinedConcepts.AsciiDoc.Static;
            var mm = MatchMode.Relaxed;

            // walk the Submodel
            _srcSm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
            {
                // semantic id
                var semId = sme?.SemanticId;
                if (semId?.IsValid() != true)
                    return true;

                // elements
                if (sme is Aas.IBlob blob)
                {
                    if (semId.Matches(defs.CD_TextBlock.GetCdReference(), mm))
                        ProcessTextBlob("", blob);
                    if (semId.Matches(defs.CD_CoverPage.GetCdReference(), mm))
                        ProcessTextBlob("", blob);
                    if (semId.Matches(defs.CD_Heading1.GetCdReference(), mm))
                        ProcessTextBlob("== ", blob);
                    if (semId.Matches(defs.CD_Heading2.GetCdReference(), mm))
                        ProcessTextBlob("=== ", blob);
                    if (semId.Matches(defs.CD_Heading3.GetCdReference(), mm))
                        ProcessTextBlob("==== ", blob);
                }

                if (sme is Aas.IFile || sme is Aas.IBlob)
                {
                    if (semId.Matches(defs.CD_ImageFile.GetCdReference(), mm))
                        ProcessImageLink(sme);
                }

                if (sme is Aas.IReferenceElement refel)
                {
                    if (semId.Matches(defs.CD_GenerateUml.GetCdReference(), mm))
                        ProcessUml(refel);
                    if (semId.Matches(defs.CD_GenerateTables.GetCdReference(), mm))
                        ProcessTables(refel);
                }

                // go further on
                return true;
            });

            // ok, build raw Adoc
            var adocText = _adoc.ToString();

            // build adoc file
            var title = (_srcSm.IdShort?.HasContent() == true) 
                    ? AdminShellUtil.FilterFriendlyName(_srcSm.IdShort) 
                    : "output";
            var adocFn = title + ".adoc";
            var absAdocFn = Path.Combine(_tempDir, adocFn);

            // write it
            File.WriteAllText(absAdocFn, adocText);
            log?.Info("ExportSmt: written {0} bytes to temp file {1}.", adocText.Length, absAdocFn);

            // start outside commands?
            if (_optionsSmt.ExportHtml)
            {
                var cmd = _optionsAll.SmtExportHtmlCmd;
                var args = _optionsAll.SmtExportHtmlArgs
                    .Replace("%WD%", "" + _tempDir)
                    .Replace("%ADOC%", "" + adocFn);

                displayContext?.MenuExecuteSystemCommand("Exporting HTML", _tempDir, cmd, args);
            }

            if (_optionsSmt.ExportPdf)
            {
                var cmd = _optionsAll.SmtExportPdfCmd;
                var args = _optionsAll.SmtExportPdfArgs
                    .Replace("%WD%", "" + _tempDir)
                    .Replace("%ADOC%", "" + adocFn);

                displayContext?.MenuExecuteSystemCommand("Exporting PDF", _tempDir, cmd, args);
            }

            // now, how to handle files?
            if (_singleFile)
            {
                // simply copy
                File.Copy(absAdocFn, fn, overwrite: true);
                log?.Info("ExportSmt: copied temp file to {0}", fn);
            }
            else
            {
                // create zip package
                var first = true;
                foreach (var infn in Directory.EnumerateFiles(_tempDir, "*"))
                {
                    AdminShellUtil.AddFileToZip(
                        fn, infn, 
                        fileMode: first ? FileMode.Create : FileMode.OpenOrCreate);
                    first = false;
                }
                log?.Info("ExportSmt: packed all files to {0}", fn);
            }

            // remove temp directory
            Directory.Delete(_tempDir, recursive: true);
            log?.Info("ExportSmt: deleted temp directory.");
        }
    }
}
