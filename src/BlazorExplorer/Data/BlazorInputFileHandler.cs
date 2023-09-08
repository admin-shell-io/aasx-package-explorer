/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AnyUi;
using BlazorInputFile;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static BlazorExplorer.Pages.PanelPackageContainerList;
using AdminShellNS;
using System;

namespace BlazorUI.Data
{
    /// <summary>
    /// This class handles the automation of a <c>BlazorInputFile</c> component
    /// provided as 3rd party component.
    /// </summary>
    public class BlazorInputFileHandler
    {
        const string DefaultStatus = "Drop a file here to upload it, or click to choose a file";
        const int MaxFileSize = 500 * 1024 * 1024; // 500MB
        public string Status = DefaultStatus;

        public Func<AnyUiDialogueDataOpenFile, Task> FileDropped { get; set; }

        public async Task FileSelected(IFileListEntry[] files)
        {
            // try get the file contents
            try
            {
                var file = files.FirstOrDefault();
                if (file == null)
                {
                    return;
                }
                else if (file.Size > MaxFileSize)
                {
                    Status = $"That's too big. Max size: {MaxFileSize} bytes.";
                }
                else
                {
                    Status = "Loading...";

                    var fn = System.IO.Path.Combine(
                                System.IO.Path.GetTempPath(),
                                System.IO.Path.GetFileName(file.Name));
                    var fileStream = System.IO.File.Create(fn);
                    await file.Data.CopyToAsync(fileStream);
                    fileStream.Close();

                    var ddof = new AnyUiDialogueDataOpenFile();
                    ddof.OriginalFileName = file.Name;
                    ddof.TargetFileName = fn;

                    Status = System.IO.Path.GetFileName(file.Name) + " uploaded. " + DefaultStatus;

                    await FileDropped?.Invoke(ddof);
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
        }
    }
}
