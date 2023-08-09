/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This container represents a file, which is hosted in a special "per user" directory
    /// on a server. The location of the user directory is determined fixed in the <c>Options</c>.
    /// </summary>
    [DisplayName("LocalFile")]
    public class PackageContainerUserFile : PackageContainerBuffered
    {
        public const string Scheme = "user://";

        /// <summary>
        /// Location of the Container in a certain storage container, e.g. a local or network based
        /// repository. In this implementation, the Location refers to a local file.
        /// </summary>
        public override string Location
        {
            get { return _location; }
            set
            {
                SetNewLocation(value); OnPropertyChanged("InfoLocation");
            }
        }

        public PackageContainerUserFile()
        {
            Init();
        }

        public PackageContainerUserFile(
            PackageCentral packageCentral,
            string sourceFn, PackageContainerOptionsBase containerOptions = null)
            : base(packageCentral)
        {
            Init();
            SetNewLocation(sourceFn);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
        }

        public PackageContainerUserFile(CopyMode mode, PackageContainerBase other,
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

        public static async Task<PackageContainerUserFile> CreateAndLoadAsync(
            PackageCentral packageCentral,
            string location,
            string fullItemLocation,
            bool overrideLoadResident,
            PackageContainerBase takeOver = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            var res = new PackageContainerUserFile(
                CopyMode.Serialized, takeOver,
                packageCentral, location, containerOptions);

            if (overrideLoadResident || true == res.ContainerOptions?.LoadResident)
                await res.LoadFromSourceAsync(fullItemLocation, runtimeOptions);

            return res;
        }

        private void Init()
        {
        }

        private void SetNewLocation(string sourceFn)
        {
            // no path information is allow
            _location = System.IO.Path.GetFileName(sourceFn);
            IsFormat = EvalFormat(_location);
            IndirectLoadSave = Options.Curr.IndirectLoadSave && IsFormat == Format.AASX;
        }

        public override string ToString()
        {
            var s = "user file: " + Location;
            return s;
        }

        /// <summary>
        /// Provides the directory name based on the given information.
        /// </summary>
        /// <returns>Null in case of any violation of the rules</returns>
        protected static string CheckBuildAbsoluteUserPath(string dir, string name, bool createDirIfNeeded = false)
        {
            // access
            if (dir?.HasContent() != true || name?.HasContent() != true)
                return null;

            // make absolute
            var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            var absdir = Path.Combine(Path.GetDirectoryName(exePath), dir);
            if (!System.IO.Path.IsPathRooted(absdir))
                return null;

            // how to handle existence of dir?
            if (!System.IO.Directory.Exists(absdir))
            {
                if (createDirIfNeeded)
                    System.IO.Directory.CreateDirectory(absdir);
                else
                    return null;
            }

            // check user name
            if (!Regex.Match(name, @"[A-Za-z][-A-Za-z0-9_]{0,49}").Success)
                return null;

            // only now combine
            return System.IO.Path.Combine(absdir, name);
        }

        public static bool CheckForUserFilesPossible()
        {
            return null != CheckBuildAbsoluteUserPath(
                Options.Curr.UserDir, Options.Curr.UserName,
                createDirIfNeeded: false);
        }

        public static string BuildUserFilePath(string fn, bool createDirIfNeeded = false)
        {
            // strip path information
            if (fn == null)
                return null;
            fn = System.IO.Path.GetFileName(fn);
            if (!fn.HasContent())
                return null;

            // check for pre-conditions
            var userdir = CheckBuildAbsoluteUserPath(Options.Curr.UserDir, Options.Curr.UserName, createDirIfNeeded);
            if (userdir == null)
                return null;

            // only now combine
            return System.IO.Path.Combine(userdir, fn);
        }

        public static IEnumerable<string> EnumerateUserFiles(string searchPattern = null)
        {
            // check for pre-conditions
            if (searchPattern?.HasContent() != true)
                searchPattern = "*";

            var userdir = CheckBuildAbsoluteUserPath(Options.Curr.UserDir, Options.Curr.UserName);
            if (userdir == null)
                yield break;

            // but null if not exist
            if (!System.IO.Directory.Exists(userdir))
                yield break;


            // list
            var res = new List<string>();
            try
            {
                // filter for files and filenames only
                res = System.IO.Directory.GetFiles(userdir, searchPattern)
                    .Select((path) => System.IO.Path.GetFileName(path)).ToList();
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            // give back
            foreach (var ri in res)
                yield return ri;
        }

        public override async Task LoadFromSourceAsync(
            string fullItemLocation,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While loading aasx, unknown file format/ extension was encountered!");

            // prepare user file path
            fullItemLocation = BuildUserFilePath(fullItemLocation);

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

        public override async Task SaveToSourceAsync(string saveAsNewFileName = null,
            AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None,
            PackCntRuntimeOptions runtimeOptions = null,
            bool doNotRememberLocation = false)
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

            // prepare user file path
            var fullItemLocation = (saveAsNewFileName != null)
                ? BuildUserFilePath(saveAsNewFileName)
                : BuildUserFilePath(Location);

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
                        System.IO.File.Copy(Env.Filename, fullItemLocation, overwrite: true);
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
                        Env.SaveAs(fullItemLocation, prefFmt: prefFmt);
                    }
                    catch (Exception ex)
                    {
                        throw new PackageContainerException(
                            $"While saving aasx to new source {fullItemLocation} " +
                            $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                    }
                }
                else
                {
                    // just save
                    try
                    {
                        Env.SaveAs(fullItemLocation);
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
