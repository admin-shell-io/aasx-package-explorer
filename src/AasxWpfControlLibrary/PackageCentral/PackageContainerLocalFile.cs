/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxPackageExplorer;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// This container represents a file, which is locally accessible via the computer's file system.
    /// </summary>
    public class PackageContainerLocalFile : PackageContainerBuffered
    {
        /// <summary>
        /// The file on the computer's file system, which is perceived by the user as the opened
        /// AASX file.
        /// </summary>
        public string SourceFn;

        public PackageContainerLocalFile()
        {
            Init();
        }

        public PackageContainerLocalFile(
            PackageCentral packageCentral,
            string sourceFn, PackageContainerOptionsBase containerOptions = null)
            : base(packageCentral)
        {
            Init();
            SetNewSourceFn(sourceFn);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
            if (true == ContainerOptions?.LoadResident)
                LoadFromSource();
        }

        public override string Filename { get { return SourceFn; } }

        private void Init()
        {
            this.LoadFromSource = this.InternalLoadFromSource;
            this.SaveAsToSource = this.InternalSaveToSource;
        }

        private void SetNewSourceFn(string sourceFn)
        {
            SourceFn = sourceFn;
            IsFormat = EvalFormat(SourceFn);
            IndirectLoadSave = Options.Curr.IndirectLoadSave && IsFormat == Format.AASX;
        }

        public override string ToString()
        {
            var s = "local file: " + SourceFn;
            if (IndirectLoadSave)
                s += " buffered to: " + TempFn;
            return s;
        }

        protected void InternalLoadFromSource(
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While loading aasx, unknown file format/ extension was encountered!");

            // buffer
            var fn = SourceFn;
            try
            {
                if (IndirectLoadSave)
                {
                    TempFn = CreateNewTempFn(SourceFn, IsFormat);
                    fn = TempFn;
                    System.IO.File.Copy(SourceFn, fn);
                }
                else
                {
                    TempFn = null;
                }
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While buffering aasx from {this.ToString()} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }

            // open
            try
            {
                // TODO (MIHO, 2020-12-15): consider removing "indirectLoadSave" from AdminShellPackageEnv
                Env = new AdminShellPackageEnv(fn, indirectLoadSave: false);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While opening aasx {fn} from source {this.ToString()} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        protected void InternalSaveToSource(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackageContainerRuntimeOptions runtimeOptions = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While saving aasx, unknown file format/ extension was encountered!");

            // check open package
            if (Env == null || !Env.IsOpen)
            {
                Env = null;
                throw new PackageContainerException(
                    "While saving aasx, package was indeed not existng or not open!");
            }

            // divert on indirect load/ save, to have dedicated try&catch
            if (IndirectLoadSave)
            {
                // apply possible new source name directly
                if (saveAsNewFileName != null)
                    SetNewSourceFn(saveAsNewFileName);

                // do a close, execute and re-open cycle
                try
                {
                    Env.TemporarilySaveCloseAndReOpenPackage(() => {
                        System.IO.File.Copy(Env.Filename, SourceFn, overwrite: true);
                    });
                }
                catch (Exception ex)
                {
                    throw new PackageContainerException(
                        $"While indirect-saving aasx to source {this.ToString()} " +
                        $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                }
            }
            else
            {
                // new file?
                if (saveAsNewFileName != null)
                {
                    // save as
                    try
                    {
                        Env.SaveAs(saveAsNewFileName, prefFmt: prefFmt);
                        SetNewSourceFn(saveAsNewFileName);
                    }
                    catch (Exception ex)
                    {
                        throw new PackageContainerException(
                            $"While saving aasx to new source {saveAsNewFileName} " +
                            $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                    }
                }
                else
                {
                    // just save
                    try
                    {
                        Env.SaveAs(SourceFn);
                    }
                    catch (Exception ex)
                    {
                        throw new PackageContainerException(
                            $"While direct-saving aasx to source {this.ToString()} " +
                            $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                    }
                }
            }
        }
    }
}
