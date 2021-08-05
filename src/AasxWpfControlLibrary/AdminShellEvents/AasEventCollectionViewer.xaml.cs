/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using AasxIntegrationBase.AdminShellEvents;
using AasxIntegrationBase.MiniMarkup;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary.PackageCentral;

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
                var info = msg.ToMarkup();
                RichTextBoxEvent.SetMarkup(info);
            }
        }

        private void DataGridMessages_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // double click on top row will activate, other row: disable
            _autoTop = DataGridMessages.SelectedIndex == 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonDetailsBack)
            {
                TabControlDetail.SelectedItem = TabItemMsgEnvelope;
            }
        }
    }
}
