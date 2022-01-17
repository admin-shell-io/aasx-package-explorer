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

namespace AasxPluginCapabilityModel
{
    public partial class CapabilityModelControl : UserControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private AdminShell.Submodel _submodel = null;
        private CapabilityModelOptions _options = null;
        private PluginEventStack _eventStack = null;

        #endregion

        #region Init of component
        //=======================

        public CapabilityModelControl()
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
            CapabilityModelOptions theOptions,
            PluginEventStack eventStack)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
        }

        public static CapabilityModelControl FillWithWpfControls(
            LogInstance log,
            object opackage, object osm,
            CapabilityModelOptions options,
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
            var knownSmCntl = new CapabilityModelControl();
            knownSmCntl.Start(log, package, sm, options, eventStack);
            master.Children.Add(knownSmCntl);

            // return shelf
            return knownSmCntl;
        }

        #endregion

        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            // access
            if (_submodel == null)
            {
                _log?.Error("No Submodel set.");
            }

            // ok
            var smc = AdminShell.SubmodelElementCollection.CreateNew(
                "TestCollection",
                "VARIABLE", new AdminShell.Key(
                    AdminShell.Key.GlobalReference, false,
                    AdminShell.Identification.IRI,
                    "https://admin-shell.io/sandbox/FhG/CapabilityModel/TestSMC/1/0"));

            var prop = AdminShell.Property.CreateNew(
                "TestProperty",
                "VARIABLE", new AdminShell.Key(
                    AdminShell.Key.GlobalReference, false,
                    AdminShell.Identification.IRI,
                    "https://admin-shell.io/sandbox/FhG/CapabilityModel/TestProp/1/0"));

            smc.Add(prop);

            _submodel.Add(smc);

            // re-display also in Explorer
            var evt = new AasxPluginResultEventRedrawAllElements();
            _eventStack?.PushEvent(evt);
        }
    }
}
