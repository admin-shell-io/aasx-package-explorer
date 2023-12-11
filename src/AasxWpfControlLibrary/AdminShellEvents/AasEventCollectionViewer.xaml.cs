/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxIntegrationBase.MiniMarkup;
using AasxIntegrationBaseWpf;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary.PackageCentral;
using Newtonsoft.Json;

namespace AasxWpfControlLibrary.AdminShellEvents
{
    /// <summary>
    /// Interaktionslogik für AasEventCollectionViewer.xaml
    /// </summary>
    public partial class AasEventCollectionViewer : UserControl, IPackageConnectorManageEvents
    {
        //
        // Members
        //

        private ObservableCollection<AasEventMsgEnvelope> _eventStore = new ObservableCollection<AasEventMsgEnvelope>();

        public ObservableCollection<AasEventMsgEnvelope> Store { get { return _eventStore; } }

        private bool _autoTop = false;

        private IFlyoutProvider _flyout;

        /// <summary>
        /// Window (handler) which provides flyout control for this control. Is expected to sit in the MainWindow.
        /// Note: only setter, as direct access from outside shall be redirected to the original source.
        /// </summary>
        public IFlyoutProvider FlyoutProvider { set { _flyout = value; } }

        //
        // Constructor
        //

        public AasEventCollectionViewer()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataGridMessages.DataContext = _eventStore;

            RichTextBoxEvent.SetXaml("<Paragraph><Run>No event selected</Run></Paragraph>");

            TabControlDetail.SelectedItem = TabItemMsgEnvelope;
            RichTextBoxEvent.MiniMarkupLinkClick += (markup, link) =>
            {
                if (markup is MiniMarkupLink mml
                    && mml.LinkObject is IAasPayloadItem pl)
                {
                    // jump to details
                    TabControlDetail.SelectedItem = TabItemDetail;

                    // set text
                    RichTextBoxDetails.Document.Blocks.Clear();
                    RichTextBoxDetails.Document.Blocks.Add(new Paragraph(new Run("" + pl.GetDetailsText())));
                }
            };
        }

        //
        // Interface
        //

        /// <summary>
        /// PackageCentral pushes an AAS event message down to the connector.
        /// Return true, if the event shall be consumed and PackageCentral shall not
        /// push anything further.
        /// </summary>
        /// <param name="ev">The event message</param>
        /// <returns>True, if consume event</returns>
        public bool PushEvent(AasEventMsgEnvelope ev)
        {
            // add
            if (_eventStore != null)
            {
                lock (_eventStore)
                    _eventStore.Insert(0, ev);
            }

            // _autoTop?
            if (_autoTop)
            {
                DataGridMessages.SelectedIndex = 0;
            }

            // do not consume, just want to listen!
            return false;
        }

        //
        // Mechanics
        //

        private void DataGridMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridMessages.SelectedItem is AasEventMsgEnvelope msg)
            {
                TabControlDetail.SelectedItem = TabItemMsgEnvelope;
                var info = msg.ToMarkup();
                RichTextBoxEvent.SetMarkup(info);
            }
        }

        private void DataGridMessages_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // double click on top row will activate, other row: disable
            _autoTop = DataGridMessages.SelectedIndex == 0;
        }

        public void CommandBinding_ContextMenu(string cmd)
        {
            // access
            if (cmd == null)
                return;
            cmd = cmd.ToLower().Trim();

            if (cmd == "clearlist")
            {
                _eventStore.Clear();
                Log.Singleton.Info("Event log cleared.");
            }

            if (cmd == "copyjson" || cmd == "savejson")
            {
                // in both cases, prepare list of events as string
                var lev = new List<AasEventMsgEnvelope>();

                // try to read selected items
                foreach (var o in DataGridMessages.SelectedItems)
                    if (o is AasEventMsgEnvelope ev)
                        lev.Add(ev);

                // fallback?
                if (lev.Count < 1)
                    foreach (var ev in _eventStore)
                        lev.Add(ev);

                var settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AasEventMsgEnvelope) });
                settings.TypeNameHandling = TypeNameHandling.Auto;
                settings.Formatting = Formatting.Indented;
                var json = JsonConvert.SerializeObject(lev, settings);

                // now decide
                if (cmd == "copyjson")
                {
                    System.Windows.Clipboard.SetText(json);
                    Log.Singleton.Info("List of all events messages (including payloads) copied to the " +
                        "system clipboard.");
                }

                if (cmd == "savejson")
                {
                    // prepare dialogue
                    var outputDlg = new Microsoft.Win32.SaveFileDialog();
                    outputDlg.Title = "Select JSON file to be saved";
                    outputDlg.FileName = "new-events.json";

                    outputDlg.DefaultExt = "*.json";
                    outputDlg.Filter = "JSON AAS event files (*.json)|*.json|All files (*.*)|*.*";

                    if (Options.Curr.UseFlyovers && _flyout != null) _flyout.StartFlyover(new EmptyFlyout());
                    var res = outputDlg.ShowDialog();
                    if (Options.Curr.UseFlyovers && _flyout != null) _flyout.CloseFlyover();
                    if (res != true)
                        return;

                    // OK!
                    var fn = outputDlg.FileName;
                    try
                    {
                        Log.Singleton.Info($"Saving AAS events to JSON file {fn} ..");
                        File.WriteAllText(fn, json);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"When saving AAS events to JSON file {fn}");
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonDetailsBack)
            {
                TabControlDetail.SelectedItem = TabItemMsgEnvelope;
            }

            if (sender == ButtonOptions)
            {
                var cm = DynamicContextMenu.CreateNew(new AasxMenu()
                    .AddAction("ClearList", icon: "\u2205", header: "Clear list")
                    .AddAction("CopyJson", icon: "\u29c9", header: "Copy JSON")
                    .AddAction("SaveJson", icon: "\U0001f4be", header: "Save JSON ..")
                    .AddLambda((name, mi, ticket) =>
                    {
                        CommandBinding_ContextMenu(name);
                    }));

                cm.Start(sender as Button);
            }
        }

    }
}
