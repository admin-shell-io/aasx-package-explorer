using AasxGlobalLogging;
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxPackageExplorer
{
    public class SecureConnectPresetField
    {
        public string Value = "";
        public string[] Choices;
    }

    public class SecureConnectPreset
    {
        public string Name = "";

        public SecureConnectPresetField Protocol = new SecureConnectPresetField();
        public SecureConnectPresetField AuthorizationServer = new SecureConnectPresetField();
        public SecureConnectPresetField AasServer = new SecureConnectPresetField();
        public SecureConnectPresetField CertificateFile = new SecureConnectPresetField();
        public SecureConnectPresetField Password = new SecureConnectPresetField();

        public void SaveToFile(string fn)
        {
            using (StreamWriter file = File.CreateText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, this);
            }
        }

        public static SecureConnectPreset LoadFromFile(string fn)
        {
            using (StreamReader file = File.OpenText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                var res = (SecureConnectPreset)serializer.Deserialize(file, typeof(SecureConnectPreset));
                return res;
            }
        }
    }

    public class SecureConnectPresetList : List<SecureConnectPreset>
    {
    }

    /// <summary>
    /// Interaktionslogik für SelectFromRepository.xaml
    /// </summary>
    public partial class SecureConnectFlyout : UserControl, IFlyoutControl
    {
        //
        // Members / events
        //

        public event IFlyoutControlClosed ControlClosed;

        public SecureConnectPresetList Presets = new SecureConnectPresetList();

        public SecureConnectPreset Result = null;

        //
        // Init
        //

        public SecureConnectFlyout()
        {
            InitializeComponent();
        }

        public void LoadPresets(Newtonsoft.Json.Linq.JToken jtoken)
        {
            // access
            if (jtoken == null)
                return;

            try
            {
                var pl = jtoken.ToObject<SecureConnectPresetList>();
                if (pl != null)
                    this.Presets = pl;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When loading Sercure Conncect Presets from Options");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // active change presets
            ComboBoxPreset.SelectionChanged += (object sender2, SelectionChangedEventArgs e2) =>
            {
                var i = ComboBoxPreset.SelectedIndex;
                if (this.Presets != null && i >= 0 && i < this.Presets.Count)
                    ActivatePreset(this.Presets[i]);
            };

            // activate presets
            this.ActivatePresetList(this.Presets);
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Return)
            {
                this.Result = ThisToPreset();
                ControlClosed?.Invoke();
            }
            if (e.Key == Key.Escape)
            {
                this.Result = null;
                ControlClosed?.Invoke();
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = null;
            ControlClosed?.Invoke();
        }

        //
        // Mechanics
        //

        private void ActivatePresetList(SecureConnectPresetList presets)
        {
            // access & set
            if (presets == null)
                return;

            // load Preset Combo
            ComboBoxPreset.Items.Clear();
            foreach (var p in presets)
                ComboBoxPreset.Items.Add("" + p.Name);

            // activate 1st
            if (presets.Count > 0)
                ComboBoxPreset.SelectedIndex = 0;
        }

        private void ActivatePreset(SecureConnectPreset p)
        {
            // access
            if (p == null)
                return;

            // define a lambda
            Action<ComboBox, SecureConnectPresetField> lambda = (cb, pf) =>
             {
                 cb.Items.Clear();
                 if (pf.Choices != null)
                     foreach (var c in pf.Choices)
                         cb.Items.Add("" + c);
                 cb.Text = "" + pf.Value;
             };

            // 5 cb's
            lambda(ComboBoxProtocol, p.Protocol);
            lambda(ComboBoxAuthServer, p.AuthorizationServer);
            lambda(ComboBoxAasServer, p.AasServer);
            lambda(ComboBoxCertFile, p.CertificateFile);
            lambda(ComboBoxPassword, p.Password);
        }

        private SecureConnectPreset ThisToPreset()
        {
            // start
            var res = new SecureConnectPreset();
            res.Name = "" + ComboBoxPreset.Text;

            // define a lambda
            Action<ComboBox, SecureConnectPresetField> lambda = (cb, pf) =>
            {
                pf.Value = "" + cb.Text;
                var l = new List<string>();
                foreach (var i in cb.Items)
                    l.Add(i.ToString());
                pf.Choices = l.ToArray();
            };

            // 5 cb's
            lambda(ComboBoxProtocol, res.Protocol);
            lambda(ComboBoxAuthServer, res.AuthorizationServer);
            lambda(ComboBoxAasServer, res.AasServer);
            lambda(ComboBoxCertFile, res.CertificateFile);
            lambda(ComboBoxPassword, res.Password);

            return res;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonSelectCertFile)
            {
                // choose filename
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.DefaultExt = "*.*";
                dlg.Filter = "PFX file (*.pfx)|*.pfx|Cert file (*.cer)|*.cer|All files (*.*)|*.*";

                // save
                if (true == dlg.ShowDialog())
                {
                    ComboBoxCertFile.Text = dlg.FileName;
                }
            }

            if (sender == ButtonSavePreset)
            {
                // choose filename
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "new.json";
                dlg.DefaultExt = "*.json";
                dlg.Filter = "Preset JSON file (*.json)|*.json|All files (*.*)|*.*";

                // save
                if (true == dlg.ShowDialog())
                {
                    // ReSharper disable EmptyGeneralCatchClause
                    try
                    {
                        var pr = this.ThisToPreset();
                        pr.SaveToFile(dlg.FileName);
                    }
                    catch { }
                    // ReSharper enable EmptyGeneralCatchClause
                }
            }

            if (sender == ButtonLoadPreset)
            {
                // choose filename
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "new.json";
                dlg.DefaultExt = "*.json";
                dlg.Filter = "Preset JSON file (*.json)|*.json|All files (*.*)|*.*";

                // save
                if (true == dlg.ShowDialog())
                {
                    // ReSharper disable EmptyGeneralCatchClause
                    try
                    {
                        var pr = SecureConnectPreset.LoadFromFile(dlg.FileName);
                        this.ActivatePreset(pr);
                    }
                    catch { }
                    // ReSharper enable EmptyGeneralCatchClause
                }
            }

            if (sender == ButtonStart)
            {
                this.Result = ThisToPreset();
                ControlClosed?.Invoke();
            }
        }


        //
        // Business logic
        //
    }
}
