/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public class StardogConnectPresetField
    {
        public string Value = "";
    }

    public class StardogConnectPreset
    {

        public StardogConnectPresetField ServerAddress = new StardogConnectPresetField();
        public StardogConnectPresetField KnowledgeBaseID = new StardogConnectPresetField();
        public StardogConnectPresetField Username = new StardogConnectPresetField();
        public StardogConnectPresetField Password = new StardogConnectPresetField();

        public void SaveToFile(string fn)
        {
            using (StreamWriter file = File.CreateText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, this);
            }
        }

        public static StardogConnectPreset LoadFromFile(string fn)
        {
            using (StreamReader file = File.OpenText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                var res = (StardogConnectPreset)serializer.Deserialize(file, typeof(StardogConnectPreset));
                return res;
            }
        }
    }

    public class StardogConnectPresetList : List<StardogConnectPreset>
    {
    }

    public partial class StardogConnectFlyout : UserControl, IFlyoutControl
    {
        //
        // Members / events
        //
        public Newtonsoft.Json.Linq.JToken token = null;

        public event IFlyoutControlAction ControlClosed;

        public StardogConnectPresetList Presets = new StardogConnectPresetList();

        public StardogConnectPreset Result = null;

        //
        // Init
        //

        public StardogConnectFlyout()
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
                var pl = jtoken.ToObject<StardogConnectPresetList>();
                if (pl != null)
                    this.Presets = pl;
                this.token = jtoken;
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When loading Stardog Connect Presets from Options");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Server.Clear();
            this.Server.Text = this.Presets[0].ServerAddress.Value;
            this.KbID.Clear();
            this.KbID.Text = this.Presets[1].KnowledgeBaseID.Value;
            this.Username.Clear();
            this.Username.Text = this.Presets[2].Username.Value;
            this.Password.Clear();
            this.Password.Password = this.Presets[3].Password.Value;
            
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

        

        private StardogConnectPreset ThisToPreset()
        {
            // start
            var res = new StardogConnectPreset();

            res.ServerAddress.Value = Server.Text;
            res.KnowledgeBaseID.Value = KbID.Text;
            res.Username.Value = Username.Text;
            res.Password.Value = Password.Password;


            return res;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

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
