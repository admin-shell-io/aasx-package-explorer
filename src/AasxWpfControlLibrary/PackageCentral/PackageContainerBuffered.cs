/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AdminShellNS;

namespace AasxWpfControlLibrary.PackageCentral
{
    /// <summary>
    /// This container add functionalities for "indirect load/ save" and backing up file contents
    /// </summary>
    public class PackageContainerBuffered : PackageContainerBase
    {       
        public bool IndirectLoadSave = false;

        public string TempFn;

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
                    Env.TemporarilySaveCloseAndReOpenPackage(() => {
                        File.Copy(Env.Filename, bdfn);
                    });
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
        }
    }
}
