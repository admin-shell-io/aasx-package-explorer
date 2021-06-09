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
            int i = 0;
        }
    }
    public class Program
    {
        public static event EventHandler NewDataAvailable;
        public class AnyUiPanelEntry
        {
            public AnyUiPanel panel;
            public int iChild;
            public AnyUiPanelEntry() { }
        }

        public class AnyUiPanelEntryStack
        {
            AnyUiPanelEntry[] recursionStack = new AnyUiPanelEntry[10];
            public int iRecursionStack = 0;
            public AnyUiPanelEntryStack() { }
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

        public static AdminShellPackageEnv env = null;
        public static string[] aasxFiles = null;
        public static string aasxFileSelected = "";
        public static bool editMode = false;
        public static bool hintMode = true;
        public static PackageCentral packages = null;
        public static DispEditHelperEntities helper = null;
        public static ModifyRepo repo = null;

        public static AnyUiStackPanel stack = new AnyUiStackPanel();
        public static AnyUiStackPanel stack2 = new AnyUiStackPanel();
        public static AnyUiStackPanel stack17 = new AnyUiStackPanel();

        public static string LogLine = "The text of the clicked object will be shown here..";

        public static string thumbNail = null;

        public class BlazorDisplayData : AnyUiDisplayDataBase
        {
            public Action<object> MyLambda;

            public BlazorDisplayData() { }

            public BlazorDisplayData(Action<object> lambda)
            {
                MyLambda = lambda;
            }
        }

        public static void loadAasx(string value)
        {
            aasxFileSelected = value;
            if (env != null)
                env.Dispose();
            env = new AdminShellPackageEnv(Program.aasxFileSelected);
            editMode = false;
            thumbNail = null;
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

        public static void loadAasxFiles()
        {
            aasxFiles = Directory.GetFiles(".", "*.aasx");
            Array.Sort(aasxFiles);
            loadAasx(aasxFiles[0]);
        }

        public static async Task getAasxAsync(string input)
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

            var contentLength = response.Content.Headers.ContentLength;
            var contentFn = response.Content.Headers.ContentDisposition?.FileName;

            // ReSharper disable PossibleNullReferenceException
            var contentStream = await response?.Content?.ReadAsStreamAsync();
            if (contentStream == null)
                return;
            // ReSharper enable PossibleNullReferenceException

            string outputDir = ".";
            Console.WriteLine("Writing file: " + outputDir + "\\" + contentFn);
            using (var file = new FileStream(outputDir + "\\" + contentFn,
                FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await contentStream.CopyToAsync(file);
            }
            loadAasxFiles();
            loadAasx(contentFn);
        }
        
        public static void Main(string[] args)
        {
            //// env = new AdminShellPackageEnv("Example_AAS_ServoDCMotor_21.aasx");

            loadAasxFiles();
#if __test__PackageLogic
#else

            packages = new PackageCentral();
            // TODO (MIHO, 2021-06-07): how to initialize?
            //// packages.Main = env;

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

            helper.DisplayOrEditAasEntityAas(
                    packages, env.AasEnv, env.AasEnv.AdministrationShells[0], editMode, stack17, hintMode: hintMode);

            AnyUi.AnyUiDisplayContextHtml.htmlDotnetThread.Start();
#endif

            //
            // Test for Blazor
            //

#if _not_enabled
             stack2 = JsonConvert.DeserializeObject<AnyUiStackPanel>(File.ReadAllText(@"c:\development\file.json"));
             var d = new JavaScriptSerializer();
             stack2 = d.Deserialize<AnyUiStackPanel>(File.ReadAllText(@"c:\development\file.json"));
             var parent = (Dictionary<string, object>)results["Parent"];
#endif

#if _not_enabled
            {
                string s = File.ReadAllText(@"c:\development\file.json");
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                };
                stack2 = JsonConvert.DeserializeObject<AnyUiStackPanel>(s, jsonSerializerSettings);
            }
#endif

            if (true)
            {
                stack.Orientation = AnyUiOrientation.Vertical;

                var lab1 = new AnyUiLabel();
                lab1.Content = "Hallo1";
                lab1.Foreground = AnyUiBrushes.DarkBlue;
                stack.Children.Add(lab1);

                var stck2 = new AnyUiStackPanel();
                stck2.Orientation = AnyUiOrientation.Horizontal;
                stack.Children.Add(stck2);

                var lab2 = new AnyUiLabel();
                lab2.Content = "Hallo2";
                lab2.Foreground = AnyUiBrushes.DarkBlue;
                stck2.Children.Add(lab2);

                var stck3 = new AnyUiStackPanel();
                stck3.Orientation = AnyUiOrientation.Horizontal;
                stck2.Children.Add(stck3);

                var lab3 = new AnyUiLabel();
                lab3.Content = "Hallo3";
                lab3.Foreground = AnyUiBrushes.DarkBlue;
                stck3.Children.Add(lab3);

                if (editMode)
                {
                    var tb = new AnyUiTextBox();
                    tb.Foreground = AnyUiBrushes.Black;
                    tb.Text = "Initial";
                    stck2.Children.Add(tb);

                    var btn = new AnyUiButton();
                    btn.Content = "Click me!";
                    btn.DisplayData = new BlazorDisplayData(lambda: (o) =>
                    {
                        if (o == btn)
                            Program.LogLine = "Hallo, Match zwischen Button und callback!";
                    });
                    stck3.Children.Add(btn);
                }
            }

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
