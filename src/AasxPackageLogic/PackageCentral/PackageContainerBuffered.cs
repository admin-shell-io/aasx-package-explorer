/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.IO;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This container add functionalities for "indirect load/ save" and backing up file contents
    /// </summary>
    public class PackageContainerBuffered : PackageContainerRepoItem
    {
        [JsonIgnore]
        public bool IndirectLoadSave = false;

        /// <summary>
        /// The temporary file name, which is used to host the AdminShellPackageEnv instead of touching
        /// the original file/ location resource.
        /// </summary>
        [JsonIgnore]
        public string TempFn;

        //
        // Constructors
        //

        public PackageContainerBuffered() { }

        public PackageContainerBuffered(PackageCentral packageCentral) : base(packageCentral) { }

        public PackageContainerBuffered(CopyMode mode, PackageContainerBase other,
            PackageCentral packageCentral = null)
            : base(mode, other, packageCentral)
        {
            if ((mode & CopyMode.Serialized) > 0 && other != null)
            {
            }
            if ((mode & CopyMode.BusinessData) > 0 && other is PackageContainerBuffered o)
            {
                IndirectLoadSave = o.IndirectLoadSave;
            }
        }

        //
        // Further
        //

        public string CreateNewTempFn(string sourceFn, Format fmt)
        {
            // TODO (MIHO, 2020-12-25): think of creating a temp file which resemebles the source file
            // name (for ease of handling)
            var res = System.IO.Path.GetTempFileName().Replace(".tmp", FormatExt[(int)fmt]);
            return res;
        }

        private static int BackupIndex = 0;

        public override void BackupInDir(string backupDir, int maxFiles, BackupType backupType = BackupType.XML)
        {
            // access
            if (backupDir == null || maxFiles < 1 || Env == null || !Env.IsOpen)
                return;

            // we do it not caring on any errors
            try
            {
                // make sure the backup dir exists
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    Log.Singleton.Info(StoredPrint.Color.Blue, "Created backup directory : " + backupDir);
                }

                // get index in form
                if (BackupIndex == 0)
                {
                    // do not always start at 0!!
                    var rnd = new Random();
                    BackupIndex = rnd.Next(maxFiles);
                }
                var ndx = BackupIndex % maxFiles;
                BackupIndex += 1;

                if (backupType == BackupType.XML)
                {
                    var bdfn = Path.Combine(backupDir, $"backup{ndx:000}.xml");
                    Env.SaveAs(bdfn, writeFreshly: true, saveOnlyCopy: true);
                }

                if (backupType == BackupType.FullCopy)
                {
                    var bext = Path.GetExtension(Env.Filename);
                    if (!bext.HasContent())
                        bext = ".aasx";
                    var bdfn = Path.Combine(backupDir, $"backup{ndx:000}{bext}");
                    Env.TemporarilySaveCloseAndReOpenPackage(() =>
                    {
                        System.IO.File.Copy(Env.Filename, bdfn, overwrite: true);
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while backing up AASX {this.ToString()}");
            }
        }
    }
}
