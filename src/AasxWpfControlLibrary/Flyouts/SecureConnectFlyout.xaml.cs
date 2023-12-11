/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AasxIntegrationBase;
using AasxPackageLogic;
using AnyUi;
using Newtonsoft.Json;

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

    public partial class SecureConnectFlyout : UserControl, IFlyoutControl
    {
        //
        // Members / events
        //

        public event IFlyoutControlAction ControlClosed;

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
                Log.Singleton.Error(ex, "When loading Sercure Conncect Presets from Options");
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

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
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
                    try
                    {
                        var pr = this.ThisToPreset();
                        pr.SaveToFile(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
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
                    try
                    {
                        var pr = SecureConnectPreset.LoadFromFile(dlg.FileName);
                        this.ActivatePreset(pr);
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
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
