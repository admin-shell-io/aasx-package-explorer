/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlazorUI
{
    public class Item
    {
        public string Text { get; set; }
        public IEnumerable<Item>
        Childs
        { get; set; }
        public object parent { get; set; }
        public string Type { get; set; }
        public object Tag { get; set; }
        public AdminShellV20.Referable ParentContainer { get; set; }
        public AdminShellV20.SubmodelElementWrapper Wrapper { get; set; }
        public int envIndex { get; set; }

        public static void updateVisibleTree(List<Item> viewItems, Item selectedNode, IList<Item> ExpandedNodes)
        {
            // resharper disable once NotAccessedVariable.Compiler
            int i = 0;
        }
    }

    public class blazorIntance
    {
        public AdminShellPackageEnv env = null;
        public string[] aasxFiles = new string[1];
        public string aasxFileSelected = "";
        public bool editMode = false;
        public bool hintMode = true;
        public PackageCentral packages = null;
        public PackageContainerListHttpRestRepository repository = null;
        public DispEditHelperEntities helper = null;
        public ModifyRepo repo = null;
        public PackageCentral _packageCentral = null;
        public PackageContainerBase container = null;

        public AnyUiStackPanel stack = new AnyUiStackPanel();
        public AnyUiStackPanel stack2 = new AnyUiStackPanel();
        public AnyUiStackPanel stack17 = new AnyUiStackPanel();

        public string thumbNail = null;

        public blazorIntance()
        {
            packages = new PackageCentral();
            _packageCentral = packages;

            env = null;

            helper = new DispEditHelperEntities();
            helper.levelColors = DispLevelColors.GetLevelColorsFromOptions(Options.Curr);
            // some functionality still uses repo != null to detect editMode!!
            repo = new ModifyRepo();
            helper.editMode = editMode;
            helper.hintMode = hintMode;
            helper.repo = repo;
            helper.context = null;
            helper.packages = packages;

            stack17 = new AnyUiStackPanel();
            stack17.Orientation = AnyUiOrientation.Vertical;

            if (env != null && env.AasEnv != null && env.AasEnv.AdministrationShells != null)
                helper.DisplayOrEditAasEntityAas(
                        packages, env.AasEnv, env.AasEnv.AdministrationShells[0], editMode, stack17, hintMode: hintMode);
        }
    }
    public class Program
    {
        public static event EventHandler NewDataAvailable;
        public class AnyUiPanelEntry
        {
            public AnyUiPanel panel;
            public int iChild;
        }

        public class AnyUiPanelEntryStack
        {
            AnyUiPanelEntry[] recursionStack = new AnyUiPanelEntry[10];
            public int iRecursionStack = 0;
            public int getIndex() { return iRecursionStack; }
            public void Pop(out AnyUiPanel panel, out int iChild)
            {
                panel = null;
                iChild = 0;
                if (iRecursionStack > 0)
                {
                    iRecursionStack--;
                    panel = recursionStack[iRecursionStack].panel;
                    iChild = recursionStack[iRecursionStack].iChild;
                    recursionStack[iRecursionStack] = null;
                }
            }
            public void Push(AnyUiPanel panel, int iChild)
            {
                recursionStack[iRecursionStack] = new Program.AnyUiPanelEntry();
                recursionStack[iRecursionStack].panel = panel;
                recursionStack[iRecursionStack].iChild = iChild + 1;
                iRecursionStack++;
            }
        }

        public class BlazorDisplayData : AnyUiDisplayDataBase
        {
            public Action<object> MyLambda;

            public BlazorDisplayData() { }

            public BlazorDisplayData(Action<object> lambda)
            {
                MyLambda = lambda;
            }
        }

        public static void loadAasx(blazorIntance bi, string value)
        {
            bi.aasxFileSelected = value;
            bi.container = null;
            if (bi.env != null)
                bi.env.Dispose();
            bi.env = new AdminShellPackageEnv(bi.aasxFileSelected);
            bi.editMode = false;
            bi.thumbNail = null;
            signalNewData(3); // build new tree, all nodes closed
        }

        // 0 == same tree, only values changed
        // 1 == same tree, structure may change
        // 2 == build new tree, keep open nodes
        // 3 == build new tree, all nodes closed
        public static int signalNewDataMode = 2;
        public static void signalNewData(int mode)
        {
            signalNewDataMode = mode;
            NewDataAvailable?.Invoke(null, EventArgs.Empty);
        }
        public static int getSignalNewDataMode()
        {
            int mode = signalNewDataMode;
            signalNewDataMode = 0;
            return (mode);
        }

        public static void loadAasxFiles(blazorIntance bi, bool load = true)
        {
            bi.aasxFiles = Directory.GetFiles(".", "*.aasx");
            Array.Sort(bi.aasxFiles);
            if (load)
            {
                if (bi.aasxFiles.Count() > 0)
                    loadAasx(bi, bi.aasxFiles[0]);
            }
        }

        public static async Task getAasxAsync(blazorIntance bi, string input)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            //// handler.AllowAutoRedirect = false;

            string dataServer = new Uri(input).GetLeftPart(UriPartial.Authority);

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(dataServer)
            };
            input = input.Substring(dataServer.Length, input.Length - dataServer.Length);
            client.DefaultRequestHeaders.Add("Accept", "application/aas");
            var response = await client.GetAsync(input);

            //// var contentLength = response.Content.Headers.ContentLength;
            var contentFn = response.Content.Headers.ContentDisposition?.FileName;

            // ReSharper disable PossibleNullReferenceException
            var contentStream = await response?.Content?.ReadAsStreamAsync();
            if (contentStream == null)
                return;
            // ReSharper enable PossibleNullReferenceException

            Console.WriteLine("Writing file: " + contentFn);
            await using (var file = new FileStream(contentFn,
                FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await contentStream.CopyToAsync(file);
            }
            loadAasxFiles(bi, false);
            loadAasx(bi, contentFn);
        }

        public static void Main(string[] args)
        {
            //// env = new AdminShellPackageEnv("Example_AAS_ServoDCMotor_21.aasx");

            // loadAasxFiles();
#if __test__PackageLogic
#else
            AnyUi.AnyUiDisplayContextHtml.htmlDotnetThread.Start();
#endif

            //
            // Test for Blazor
            //
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
