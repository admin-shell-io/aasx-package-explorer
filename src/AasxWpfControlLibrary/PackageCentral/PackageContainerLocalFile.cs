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
using AasxPackageExplorer;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// This container represents a file, which is locally accessible via the computer's file system.
    /// </summary>
    public class PackageContainerLocalFile : PackageContainerBuffered
    {
        /// <summary>
        /// Location of the Container in a certain storage container, e.g. a local or network based
        /// repository. In this implementation, the Location refers to a local file.
        /// </summary>
        [JsonIgnore]
        public override string Location
        {
            get { return _location; }
            set { SetNewLocation(value); OnPropertyChanged("InfoLocation"); }
        }

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
            SetNewLocation(sourceFn);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
        }

        public PackageContainerLocalFile(CopyMode mode, PackageContainerBase other,
            PackageCentral packageCentral = null,
            string sourceFn = null, PackageContainerOptionsBase containerOptions = null)
            : base(mode, other, packageCentral)
        {
            if ((mode & CopyMode.Serialized) > 0 && other != null)
            {
            }
            if ((mode & CopyMode.BusinessData) > 0 && other is PackageContainerLocalFile o)
            {
                sourceFn = o.Location;
            }
            if (sourceFn != null)
                SetNewLocation(sourceFn);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
        }

        public static async Task<PackageContainerLocalFile> CreateAndLoadAsync(
            PackageCentral packageCentral,
            string sourceFn,
            bool overrideLoadResident,
            PackageContainerBase takeOver = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            var res = new PackageContainerLocalFile(
                CopyMode.Serialized, takeOver,
                packageCentral, sourceFn, containerOptions);

            if (overrideLoadResident || true == res.ContainerOptions?.LoadResident)
                await res.LoadFromSourceAsync(runtimeOptions);

            return res;
        }

        private void Init()
        {
        }

        private void SetNewLocation(string sourceFn)
        {
            _location = sourceFn;
            IsFormat = EvalFormat(_location);
            IndirectLoadSave = Options.Curr.IndirectLoadSave && IsFormat == Format.AASX;
        }

        public override string ToString()
        {
            var s = "local file: " + Location;
            if (IndirectLoadSave)
                s += " buffered to: " + TempFn;
            return s;
        }

        public override async Task LoadFromSourceAsync(
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While loading aasx, unknown file format/ extension was encountered!");

            // buffer
            var fn = Location;
            try
            {
                if (IndirectLoadSave)
                {
                    TempFn = CreateNewTempFn(Location, IsFormat);
                    fn = TempFn;
                    System.IO.File.Copy(Location, fn);
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

            await Task.Yield();
        }

        public override async Task SaveToSourceAsync(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // apply possible new source name directly
            if (saveAsNewFileName != null)
                SetNewLocation(saveAsNewFileName);

            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While saving aasx, unknown file format/ extension was encountered!");

            // check open package
            if (Env == null)
            {
                Env = null;
                throw new PackageContainerException(
                    "While saving aasx, package was indeed not existng!");
            }

            // divert on indirect load/ save, to have dedicated try&catch
            if (IndirectLoadSave)
            {
                // the container or package might be new
                if (!Env.IsOpen || TempFn == null)
                {
                    TempFn = CreateNewTempFn(Location, IsFormat);
                    Env.SaveAs(TempFn);
                }

                // do a close, execute and re-open cycle
                try
                {
                    Env.TemporarilySaveCloseAndReOpenPackage(() =>
                    {
                        System.IO.File.Copy(Env.Filename, Location, overwrite: true);
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
                        Env.SaveAs(Location);
                    }
                    catch (Exception ex)
                    {
                        throw new PackageContainerException(
                            $"While direct-saving aasx to source {this.ToString()} " +
                            $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                    }
                }
            }

            // fake async
            await Task.Yield();
        }
    }
}
