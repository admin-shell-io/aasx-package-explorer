/*
Copyright (c) 2018-2022 

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPackageLogic.PackageCentral.AasxFileServerInterface
{
    public class PackageContainerAasxFileRepository : PackageContainerListBase
    {
        public Uri Endpoint { get; private set; }

        private AasxFileServerInterfaceService _aasxFileService;
        public PackageContainerAasxFileRepository(string inputText)
        {
            if (inputText.Contains('?'))
            {
                var splitTokens = inputText.Split(new[] { '?' }, 2);
                if (splitTokens[1].Equals("asp.net", StringComparison.OrdinalIgnoreCase))
                {
                    IsAspNetConnection = true;
                }
                inputText = splitTokens[0];
            }
            // always have a location
            Endpoint = new Uri(inputText);

            _aasxFileService = new AasxFileServerInterfaceService(inputText);
        }

        public bool IsAspNetConnection { get; private set; }

        public void GeneratePackageRepository()
        {
            var items = _aasxFileService.GeneratePackageRepository();
            FileMap.Clear();
            foreach (var packageContainer in items)
            {
                if (packageContainer != null)
                {
                    FileMap.Add(packageContainer);
                    packageContainer.ContainerList = this;
                }
            }
        }

        public AasxFilePackageContainerBase LoadAasxFileFromServer(string packageId, PackCntRuntimeOptions runtimeOptions)
        {
            string fileName = _aasxFileService.LoadAasxPackage(packageId, runtimeOptions);

            if (!String.IsNullOrEmpty(fileName))
            {
                var container = new AasxFilePackageContainerBase
                {
                    Env = new AdminShellPackageEnv(fileName, indirectLoadSave: false),
                    ContainerList = this,
                    IsFormat = PackageContainerBase.Format.AASX,        //TODO (jtikekar, 2022-04-04): Based on file
                    PackageId = packageId
                };
                runtimeOptions?.Log?.Info($".. successfully opened as AASX environment: {container.Env?.AasEnv?.ToString()}");
                return container;
            }

            return null;
        }

        internal async Task UpdateFileOnServerAsync(string copyFileName, string packageId, PackCntRuntimeOptions runtimeOptions)
        {
            await _aasxFileService.PutAasxFileOnServerAsync(copyFileName, packageId);
        }

        public override void DeletePackageFromServer(PackageContainerRepoItem fi)
        {
            _aasxFileService.DeleteAasxFileFromServer(fi.PackageId);
            base.DeletePackageFromServer(fi);
        }

        public int AddPackageToServer(string fileName)
        {
            //Using copy of the file to upload
            string copyFileName = "";
            try
            {
                copyFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");
                File.Copy(fileName, copyFileName, true);
            }
            catch (Exception e)
            {
                Log.Singleton.Error($"Error while creating temporary file of {fileName} : {e.Message}");
            }

            var fileContent = File.ReadAllBytes(copyFileName);
            int packageId = _aasxFileService.PostAasxFileOnServer(Path.GetFileName(fileName), fileContent);

            //delete temp file
            File.Delete(copyFileName);


            return packageId;
        }

        public void LoadAasxFile(PackageCentral packageCentral, string fileName, int packageId)
        {
            try
            {
                // load
                var packageEnv = new AdminShellPackageEnv(fileName);

                // for each Admin Shell and then each Asset
                var packageContainer = new PackageContainerRepoItem()
                {
                    ContainerOptions = PackageContainerOptionsBase.CreateDefault(Options.Curr),
                    PackageId = packageId.ToString()
                };

                packageContainer.Env = packageEnv;
                packageContainer.CalculateIdsTagAndDesc();
                packageContainer.VisualState = PackageContainerRepoItem.VisualStateEnum.ReadFrom;
                packageContainer.VisualTime = 2.0;
                this.Add(packageContainer);

                // close directly!
                packageEnv.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
        }
    }
}
