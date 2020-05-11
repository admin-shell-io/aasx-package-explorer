using AasxGlobalLogging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // allow long term logging (for report box)
            Log.LogInstance.EnableLongTermStore();

            // Build up of options
            Log.Info("Application startup.");

            // there is a special case for having "no" command line options ..
            string directAasx = null;
            if (e.Args.Length == 1 && !e.Args[0].StartsWith("-"))
            {
                directAasx = e.Args[0];
                Log.Info("Direct request to load AASX {0} ..", directAasx);
            }

            // If no command line args given, read options via default filename
            if (directAasx != null || e.Args.Length < 1)
            {
                var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                var defFn = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(exePath),
                            System.IO.Path.GetFileNameWithoutExtension(exePath) + ".options.json");

                Log.Info("Check {0} for default options in JSON ..", defFn);
                if (File.Exists(defFn))
                    Options.Curr.ReadJson(defFn);

                // overrule
                if (directAasx != null)
                    Options.Curr.AasxToLoad = directAasx;
            }
            else
            {
                // 2nd parse options
                Log.Info("Parsing commandline options ..");
                foreach (var a in e.Args)
                    Log.Info("argument {0}", a);
                Options.Curr.ParseArgs(e.Args);
            }

            // 3rd further commandline options in extra file
            if (Options.Curr.OptionsTextFn != null)
            {
                Log.Info("Parsing options from distinct file {0} ..", Options.Curr.OptionsTextFn);
                var fullfn = System.IO.Path.GetFullPath(Options.Curr.OptionsTextFn);
                Options.Curr.TryReadOptionsFile(fullfn);
            }

            // search for plugins?
            if (Options.Curr.PluginDir != null)
            {
                var searchDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
                    Options.Curr.PluginDir);
                Log.Info("Searching for plug-ins in {0} ..", searchDir);
                Plugins.TrySearchPlugins(searchDir, Options.Curr.PluginDll);
            }

            // Plugins to be loaded
            Log.Info("Try load and activate plug-ins ..");
            Plugins.TryActivatePlugins(Options.Curr.PluginDll);
            Plugins.TrySetOptionsForPlugins(Options.Curr);

            // at end, write all default options to JSON?
            if (Options.Curr.WriteDefaultOptionsFN != null)
            {
                // info
                var fullfn = System.IO.Path.GetFullPath(Options.Curr.WriteDefaultOptionsFN);
                Log.Info("Writing resulting options into JSOnN {0}", fullfn);

                // retrieve
                Plugins.TrGetDefaultOptionsForPlugins(Options.Curr);
                Options.Curr.WriteJson(fullfn);
            }

            // colors
            if (true)
            {
                var resNames = new string[] { "LightAccentColor", "DarkAccentColor", "DarkestAccentColor", "FocusErrorBrush" };
                for (int i = 0; i < resNames.Length; i++)
                {
                    var x = this.FindResource(resNames[i]);
                    if (x != null && x is System.Windows.Media.SolidColorBrush && Options.Curr.AccentColors.ContainsKey(i))
                        this.Resources[resNames[i]] = new System.Windows.Media.SolidColorBrush(Options.Curr.AccentColors[i]);
                }
                resNames = new string[] { "FocusErrorColor" };
                for (int i = 0; i < resNames.Length; i++)
                {
                    var x = this.FindResource(resNames[i]);
                    if (x != null && x is System.Windows.Media.Color && Options.Curr.AccentColors.ContainsKey(3 + i))
                        this.Resources[resNames[i]] = Options.Curr.AccentColors[3 + i];
                }
            }
            
            // show splash (required for licenses of open source)
            var splash = new CustomSplashScreenNew();
            splash.Show();

            // show main window
            MainWindow wnd = new MainWindow();
            wnd.Show();
        }
    }
}


//
// Licenses
//

// CefSharp
// see: https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE

// Copyright © 2010-2018 The CefSharp Authors. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//    * Redistributions of source code must retain the above copyright
//      notice, this list of conditions and the following disclaimer.
//
//    * Redistributions in binary form must reproduce the above
//      copyright notice, this list of conditions and the following disclaimer
//      in the documentation and/or other materials provided with the
//      distribution.
//
//    * Neither the name of Google Inc. nor the name Chromium Embedded
//      Framework nor the name CefSharp nor the names of its contributors
//      may be used to endorse or promote products derived from this software
//      without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
