/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Newtonsoft.Json;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This container represents a file, which is locally accessible via the computer's file system.
    /// </summary>
    [DisplayName("LocalFile")]
    public class PackageContainerLocalFile : PackageContainerBuffered
    {
        /// <summary>
        /// Location of the Container in a certain storage container, e.g. a local or network based
        /// repository. In this implementation, the Location refers to a local file.
        /// </summary>
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
            string location,
            string fullItemLocation,
            bool overrideLoadResident,
            PackageContainerBase takeOver = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            var res = new PackageContainerLocalFile(
                CopyMode.Serialized, takeOver,
                packageCentral, location, containerOptions);

            if (overrideLoadResident || true == res.ContainerOptions?.LoadResident)
                await res.LoadFromSourceAsync(fullItemLocation, runtimeOptions);

            return res;
        }

        private void Init()
        {
        }

        private void SetNewLocation(string sourceFn, bool doNotRememberLocation = false)
        {
            if (!doNotRememberLocation)
                _location = sourceFn;
            // these flag do only depend on the extension and should therefore fit
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
            string fullItemLocation,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While loading aasx, unknown file format/ extension was encountered!");

            // buffer
            var fn = fullItemLocation;
            try
            {
                if (IndirectLoadSave)
                {
                    TempFn = CreateNewTempFn(fullItemLocation, IsFormat);
                    fn = TempFn;
                    System.IO.File.Copy(fullItemLocation, fn);
                }
                else
                {
                    TempFn = null;
                }
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While buffering aasx from {this.ToString()} full-location {fullItemLocation} " +
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

        public override async Task SaveToSourceAsync(
            string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackCntRuntimeOptions runtimeOptions = null,
            bool doNotRememberLocation = false)
        {
            // apply possible new source name directly
            if (saveAsNewFileName != null)
                SetNewLocation(saveAsNewFileName, doNotRememberLocation: doNotRememberLocation);

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
                    Env.SaveAs(TempFn, prefFmt: prefFmt);
                }

                // do a close, execute and re-open cycle
                try
                {
                    Env.TemporarilySaveCloseAndReOpenPackage(
                        prefFmt: prefFmt, lambda: () =>
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

        public override async Task LoadResidentIfPossible(string fullItemLocation)
        {
            try
            {
                if (!IsOpen && Location.HasContent())
                    await LoadFromSourceAsync(fullItemLocation, PackageCentral?.CentralRuntimeOptions);
                OnPropertyChanged("VisualIsLoaded");
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
                PackageCentral?.CentralRuntimeOptions?.Log?.Error($"Cannot (auto-) load {Location}. Skipping.");
                Close();
            }

            await Task.Yield();
        }
    }
}
