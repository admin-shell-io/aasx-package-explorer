/*
Copyright (c) 2018-2022 

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AasxPackageLogic.PackageCentral.AasxFileServerInterface
{
    public class AasxFilePackageContainerBase : PackageContainerBase
    {
        public string PackageId { get; internal set; }

        public override async Task SaveToSourceAsync(string saveAsNewFileName = null, AdminShellPackageEnv.SerializationFormat prefFmt = AdminShellPackageEnv.SerializationFormat.None, PackCntRuntimeOptions runtimeOptions = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
            {
                throw new PackageContainerException("While saving aasx, unknown file format/ extension was encountered!");
            }

            // check open package
            if (Env == null || !Env.IsOpen)
            {
                Env = null;
                throw new PackageContainerException("While saving aasx, package was indeed not existng or not open!");
            }

            //Using copy of the file to upload
            string copyFileName = "";
            try
            {
                copyFileName = Path.GetTempFileName().Replace(".tmp", FormatExt[(int)IsFormat]);
                Env.TemporarilySaveCloseAndReOpenPackage(() =>
                {
                    System.IO.File.Copy(Env.Filename, copyFileName, overwrite: true);
                });
            }
            catch (Exception e)
            {
                Log.Singleton.Error($"Error while creating temporary file of {Env.Filename} : {e.Message}");
            }

            //Upload File to Server
#if TODO
            if (ContainerList != null && ContainerList is PackageContainerAasxFileRepository fileRepository)
            {
                await fileRepository.UpdateFileOnServerAsync(copyFileName, PackageId, runtimeOptions);
            }
#endif

            //Delete Temp file

        }
    }
}
