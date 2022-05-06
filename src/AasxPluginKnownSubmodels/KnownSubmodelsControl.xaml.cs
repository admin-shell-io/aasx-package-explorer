/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using AdminShellNS;
using Newtonsoft.Json;

// ReSharper disable InconsistentlySynchronizedField
// checks and everything looks fine .. maybe .Count() is already treated as synchronized action?

// ReSharper disable NegativeEqualityExpression
// checks look cleaner this way

namespace AasxPluginKnownSubmodels
{
    public partial class KnownSubmodelsControl : UserControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private AdminShell.Submodel _submodel = null;
        private KnownSubmodelsOptions _options = null;
        private PluginEventStack _eventStack = null;
        private KnownSubmodelViewModel _viewModel = new KnownSubmodelViewModel();

        #endregion

        #region View Model
        //================

        private ViewModel theViewModel = new ViewModel();

        public class ViewModel : AasxIntegrationBase.WpfViewModelBase
        {
        }

        #endregion
        #region Init of component
        //=======================

        public KnownSubmodelsControl()
        {
            InitializeComponent();

            // Timer for loading
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            KnownSubmodelsOptions theOptions,
            PluginEventStack eventStack)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
        }

        public static KnownSubmodelsControl FillWithWpfControls(
            LogInstance log,
            object opackage, object osm,
            KnownSubmodelsOptions options,
            PluginEventStack eventStack,
            object masterDockPanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var master = masterDockPanel as DockPanel;
            if (package == null || sm == null || master == null)
            {
                return null;
            }

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // create TOP control
            var knownSmCntl = new KnownSubmodelsControl();
            knownSmCntl.Start(log, package, sm, options, eventStack);
            master.Children.Add(knownSmCntl);

            // return shelf
            return knownSmCntl;
        }

        #endregion

        #region Business Logic
        //====================


        #endregion

        #region WPF handling
        //==================

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // change title
            this.LabelPanel.Content = "Known Submodel Templates";

            // try populate
            try
            {
                // re-new
                _viewModel.Clear();

                // figure our relative path
                var basePath = "" + System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly()?.Location);

#if __old
                if (theOptions?.Records != null && theSubmodel?.semanticId != null)
                    foreach (var rec in theOptions.Records)
                    {
                        var found = false;

                        if (rec.AllowSubmodelSemanticId != null)
                            foreach (var x in rec.AllowSubmodelSemanticId)
                                if (true == theSubmodel?.semanticId?.Matches(x))
                                {
                                    found = true;
                                    break;
                                }

                        if (found)
                            _viewModel.Add(new KnownSubmodelViewItem(rec, basePath));
                    }
#else
                foreach (var rec in _options.LookupAllIndexKey<KnownSubmodelsOptionsRecord>(
                    _submodel?.semanticId?.GetAsExactlyOneKey()))
                    _viewModel.Add(new KnownSubmodelViewItem(rec, basePath));
#endif

                ScrollMainContent.ItemsSource = _viewModel;

            }
            catch (Exception ex)
            {
                _log?.Error(ex, "when preparing display items");
            }
        }

        #endregion

        private void CanvasContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void CheckBoxShowRegions_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink lnk
                && lnk.Tag is string furtherUrl)
            {
                // give over to event stack
                var evt = new AasxPluginResultEventDisplayContentFile();
                evt.fn = "" + furtherUrl;
                evt.preferInternalDisplay = true;
                _eventStack?.PushEvent(evt);
            }
        }
    }
}
