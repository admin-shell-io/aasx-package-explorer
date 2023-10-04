/*
Copyright (c) 2018-2023 

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral.AasxFileServerInterface;
using AdminShellNS;
using IO.Swagger.Api;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AasxPackageLogic.PackageCentral
{
    public class AasxFileServerInterfaceService
    {
        private AASXFileServerInterfaceApi _fileApiInstance;
        private object _aasApiInstace;
        private AasxServerService _aasxServerService;

        public AasxFileServerInterfaceService(string basePath)
        {
            try
            {
                var _basePath = basePath;
                _aasxServerService = new AasxServerService(basePath);
            }
            catch (Exception e)
            {
                Log.Singleton.Error(e.Message, "Could not configure ASP.NET file interface");
            }
        }

        //This method retrieved all the packages and corresponsing AASs and Aas.AssetInformation related information form the File Server.
        internal List<PackageContainerRepoItem> GeneratePackageRepository()
        {
            var output = new List<PackageContainerRepoItem>();
            try
            {
                var response = _aasxServerService.GetAllAASXPackageIds();

                foreach (var packageDescription in response)
                {
                    //Get AAS and Aas.AssetInformation
                    foreach (var aasId in packageDescription.AasIds)
                    {
                        try
                        {
                            var aas = _aasxServerService.GetAssetAdministrationShellById(Base64UrlEncoder.Encode(aasId));
                            if (aas != null)
                            {
                                //Get Aas.AssetInformation
                                try
                                {
                                    var asset = aas.AssetInformation;
                                    //var asset = _aasApiInstace.GetAssetInformation(Base64UrlEncoder.Encode(aasId))
                                    if (asset != null)
                                    {
                                        var packageContainer = new PackageContainerRepoItem()
                                        {
                                            ContainerOptions = PackageContainerOptionsBase.CreateDefault(Options.Curr),
                                            Description = $"\"{"" + aas.IdShort}\"", //No more IdShort in asset
                                            Tag = "" + AdminShellUtil.ExtractPascalCasingLetters(aas.IdShort).SubstringMax(0, 3),
                                            PackageId = packageDescription.PackageId
                                        };
                                        packageContainer.AasIds.Add("" + aas?.Id);
                                        packageContainer.AssetIds.Add("" + asset.GlobalAssetId);
                                        output.Add(packageContainer);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(ex.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Singleton.Error(e.Message);
            }

            return output;
        }

        internal int PostAasxFileOnServer(string fileName, byte[] fileContent, PackageContainerAasxFileRepository fileRepository)
        {
            var aasiIds = new List<string>();

            var packageId = _aasxServerService.PostAASXPackage(fileContent, fileName, fileRepository);
            return packageId;
        }

        internal void DeleteAasxFileFromServer(string packageId, PackageContainerAasxFileRepository fileRepository)
        {
            try
            {
                _aasxServerService.DeleteAASXByPackageId(Base64UrlEncoder.Encode(packageId), fileRepository);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex.Message);
            }
        }

        internal Task PutAasxFileOnServerAsync(string fileName, byte[] fileContent, string packageId)
        {
            try
            {
                //TODO (jtikekar, 2022-04-04): aasIds?
                var aasIds = new List<string>();
                _aasxServerService.PutAASXPackageById(Base64UrlEncoder.Encode(packageId), fileContent, fileName);
            }
            catch (Exception e)
            {
                Log.Singleton.Error(e.Message);
            }

            return Task.CompletedTask;
        }

        internal async Task<string> LoadAasxPackageAsync(string packageId, PackCntRuntimeOptions runtimeOptions, PackageContainerAasxFileRepository fileRepository)
        {
            try
            {
                var response = _aasxServerService.GetAASXByPackageId(Base64UrlEncoder.Encode(packageId), fileRepository);
                if (response != null)
                {
                    var contentLength = response.Content.Headers.ContentLength;
                    response.Headers.TryGetValues("X-FileName", out IEnumerable<string> headerValues);
                    var fileName = headerValues.FirstOrDefault();
                    var contentStream = await response?.Content?.ReadAsStreamAsync();
                    if (contentStream == null)
                        throw new PackageContainerException(
                        $"While getting data bytes from XXX via HttpClient " +
                        $"no data-content was responded!");
                    var fileSize = contentLength;

                    using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[100000];
                        long totalBytes = 0;
                        int currentBlockSize = 0;

                        while ((currentBlockSize = contentStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytes += currentBlockSize;

                            file.Write(buffer, 0, currentBlockSize);

                            if (fileSize > totalBytes)
                            {
                                runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Ongoing,
                                fileSize, totalBytes);
                            }
                            else
                            {
                                runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Final, fileSize, totalBytes);
                                runtimeOptions?.Log?.Info($".. download done with {totalBytes} bytes read!");
                            }

                        }

                        file.Close();
                        contentStream.Close();
                    }
                    return fileName;
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex.Message);
            }

            //TODO (jtikekar, 2022-04-04): Change
            return null;
        }
    }
}
