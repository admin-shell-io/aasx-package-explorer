/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AdminShellNS;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace AasxPackageExplorer
{
    public partial class ReserveAtEclassFlyout : UserControl, IFlyoutControl
    {

        public static string URL_PREFIX = "https://18.157.197.66:8443/eclass";

        public string irdi { get; set; }
        public string irdi_identifier { get; set; }
        public string type { get; set; }
        public string prefName { get; set; }
        public string label { get; set; }

        //
        // Members / events
        //

        public event IFlyoutControlClosed ControlClosed;

        public string ResultIRDI = null;
        public AdminShell.ConceptDescription ResultCD = null;

        //
        // Init
        //

        public ReserveAtEclassFlyout()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
            //if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Return)
            //{
            //    this.Result = ThisToPreset();
            //    ControlClosed?.Invoke();
            //}
            //if (e.Key == Key.Escape)
            //{
            //    this.Result = null;
            //    ControlClosed?.Invoke();
            //}
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.ResultCD = null;
            this.ResultCD = null;
            ControlClosed?.Invoke();
        }

        //
        // Mechanics
        //

        private void textChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            if (TextBoxIRDIVersion != null && TextBoxIRDI != null && LabelIdentifier != null && LabelURI != null)
            {
                irdi_identifier = $"0173-{TextBoxIRDI.Text}#{TextBoxIRDIVersion.Text}";
                irdi = Regex.Replace(irdi_identifier, @"\W", "_");
                LabelIdentifier.Text = irdi_identifier;
                LabelURI.Text = irdi;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //if (sender == ButtonSelectCertFile)
            //{
            //    // choose filename
            //    var dlg = new Microsoft.Win32.OpenFileDialog();
            //    dlg.DefaultExt = "*.*";
            //    dlg.Filter = "PFX file (*.pfx)|*.pfx|Cert file (*.cer)|*.cer|All files (*.*)|*.*";

            //    // save
            //    if (true == dlg.ShowDialog())
            //    {
            //        ComboBoxCertFile.Text = dlg.FileName;
            //    }
            //}

            if (sender == ButtonSaveEclassEntry)
            {

                try
                {
                    type = $"{URL_PREFIX}/ECLASS_Model#{TextBoxType.Text}";
                    prefName = TextBoxPreferredName.Text;
                    label = TextBoxShortDescription.Text;

                    string toSend = $@"[{{'@id': '{irdi}','@type': ['{type}'],'http://www.w3.org/2000/01/rdf-schema#label': [{{'@value': '{label}',888'@language': 'en-us'}}],'{URL_PREFIX}/ECLASS_Model#its_superclass': [{{'@id': '{irdi}'}}],'{URL_PREFIX}/ECLASS_Model#preferred_name': [{{'@value': '{prefName}','@language': 'en-us'}}]}}]";
       
                    using (var httpClientHandler = new HttpClientHandler())
                    {
                        httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                        using (var client = new HttpClient(httpClientHandler))
                        {
                            // Make request here.
                            // client.DefaultRequestHeaders.Add("Accept", "application/ld+json");
                            // HttpResponseMessage response = await client.GetAsync("https://18.157.197.66:8443/eclass/IRDI_0173_1_01_AAB574_005");
                            // response.EnsureSuccessStatusCode();
                            // string responseBody = await response.Content.ReadAsStringAsync();


                            // if (matches.Count > 0 && matches[0].Groups.Count > 1)
                            //     return null;

                            // AasxIntegrationBase.StoredPrint sp = new AasxIntegrationBase.StoredPrint(JsonPrettify(responseBody));
                            // AasxPackageExplorer.Log.Singleton.Append(sp);

                        }
                    }
        





                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            //if (sender == ButtonLoadPreset)
            //{
            //    // choose filename
            //    var dlg = new Microsoft.Win32.OpenFileDialog();
            //    dlg.FileName = "new.json";
            //    dlg.DefaultExt = "*.json";
            //    dlg.Filter = "Preset JSON file (*.json)|*.json|All files (*.*)|*.*";

            //    // save
            //    if (true == dlg.ShowDialog())
            //    {
            //        try
            //        {
            //            var pr = SecureConnectPreset.LoadFromFile(dlg.FileName);
            //            this.ActivatePreset(pr);
            //        }
            //        catch (Exception ex)
            //        {
            //            AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            //        }
            //    }
            //}
        }


        //
        // Business logic
        //
    }
}
