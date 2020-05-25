using AasxIntegrationBase;
using AdminShellNS;
using AasxGlobalLogging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    //
    // Data model
    //

    // ReSharper disable ClassNeverInstantiated.Global .. used by JSON

    public class QualifierPreset
    {
        public string name = "";
        public AdminShell.Qualifier qualifier = new AdminShell.Qualifier();
    }

    // ReSharper enable ClassNeverInstantiated.Global

    /// <summary>
    /// Interaktionslogik für SelectFromRepository.xaml
    /// </summary>
    public partial class SelectQualifierPresetFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public AdminShell.Qualifier ResultQualifier = null;

        private List<QualifierPreset> ThePresets = new List<QualifierPreset>();

        public SelectQualifierPresetFlyout(string presetFn)
        {
            InitializeComponent();

            try
            {
                /*
                if (true)
                {
                    ThePresets = new List<QualifierPreset>();
                    ThePresets.Add(new QualifierPreset());
                    using (var s = new StreamWriter("dummy.json"))
                    {
                        var json = JsonConvert.SerializeObject(ThePresets, Newtonsoft.Json.Formatting.Indented);
                        s.WriteLine(json);
                    }
                }
                */

                var init = File.ReadAllText(presetFn);
                ThePresets = JsonConvert.DeserializeObject<List<QualifierPreset>>(init);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"While loading qualifier preset file ({presetFn})");
            }
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        //
        // Mechanics
        //

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill listbox
            ListBoxPresets.Items.Clear();
            foreach (var p in ThePresets)
                ListBoxPresets.Items.Add("" + p.name);
        }

        private bool PrepareResult()
        {
            var i = ListBoxPresets.SelectedIndex;
            if (ThePresets != null && i >= 0 && i < ThePresets.Count)
            {
                this.ResultQualifier = ThePresets[i].qualifier;
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.ResultQualifier = null;
            ControlClosed?.Invoke();
        }

        private void ListBoxPresets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }
    }
}
