/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using AdminShellNS;

namespace AasxPluginPlotting
{
    public partial class PlottingViewControl : UserControl
    {

        public class PlotArguments
        {
            public string idx;
            public string fmt;
            public double? xmin, ymin, xmax, ymax;
        }

        // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
        public class PlotItem : /* IEquatable<DetailItem>, */ INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public AdminShell.SubmodelElement SME = null;
            public string ArgsStr = "";
            public PlotArguments Args = null;

            private string _path = "";
            public string DisplayPath
            {
                get { return _path; }
                set { _path = value; OnPropertyChanged("DisplayPath"); }
            }

            private string _value = null;
            public string DisplayValue
            {
                get { return _value; }
                set { _value = value; OnPropertyChanged("DisplayValue"); }
            }

            private AdminShell.Description _description = new AdminShell.Description();
            public AdminShell.Description Description
            {
                get { return _description; }
                set { _description = value; OnPropertyChanged("DisplayDescription"); }
            }

            private string _displayDescription = "";
            public string DisplayDescription
            {
                get { return _displayDescription; }
                set { _displayDescription = value; OnPropertyChanged("DisplayDescription"); }
            }

            public PlotItem() { }

            public PlotItem(AdminShell.SubmodelElement sme, string args, 
                string path, string value, AdminShell.Description description)
            {
                SME = sme;
                ArgsStr = args;
                _path = path;
                _value = value;
                _description = description;
                _displayDescription = description?.GetDefaultStr();
                TryParseArgs();
            }

            private void TryParseArgs()
            {
                try
                {
                    Args = null;
                    if (!ArgsStr.HasContent())
                        return;
                    Args = Newtonsoft.Json.JsonConvert.DeserializeObject<PlotArguments>(ArgsStr);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
        }

        public class ListOfPlotItem : ObservableCollection<PlotItem>
        {
            public void UpdateLang(string lang)
            {
                foreach (var it in this)
                    it.DisplayDescription = it.Description?.GetDefaultStr(lang);
            }

            public void UpdateValues(string lang = null)
            {
                foreach (var it in this)
                    if (it.SME != null)
                    {
                        var dbl = it.SME.ValueAsDouble();
                        if (dbl.HasValue && it.Args?.fmt != null)
                        {
                            try
                            {
                                it.DisplayValue = "" + dbl.Value.ToString(it.Args.fmt, CultureInfo.InvariantCulture);
                            }
                            catch (Exception ex)
                            {
                                it.DisplayValue = "<fmt error>";
                                LogInternally.That.SilentlyIgnoredError(ex);
                            }
                        }
                        else
                            it.DisplayValue = "" + it.SME.ValueAsText(lang);
                    }
            }

            public void RebuildFromSubmodel(AdminShell.Submodel sm)
            {
                // clear & access
                this.Clear();
                if (sm == null)
                    return;

                // find all SME with appropriate Qualifer
                sm.RecurseOnSubmodelElements(this, (state, parents, sme) =>
                {
                    // qualifier
                    var q = sme.HasQualifierOfType("Plotting.Args");
                    if (q == null)
                        return;

                    // select for SME type
                    /* TODO (MIHO, 2021-01-04): consider at least to include MLP, as well */
                    if (!(sme is AdminShell.Property))
                        return;

                    // build path
                    var path = sme.idShort;
                    if (parents != null)
                        foreach (var par in parents)
                            path = "" + par.idShort + " / " + path;

                    // add
                    this.Add(new PlotItem(sme, "" + q.value, path, "" + sme.ValueAsText(), sme.description));
                });
            }
        }

        public ListOfPlotItem PlotItems = new ListOfPlotItem();

        public PlottingViewControl()
        {
            InitializeComponent();
        }

        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private PlottingOptions theOptions = null;
        private PluginEventStack theEventStack = null;

        private string theDefaultLang = null;

        public void Start(
            AdminShellPackageEnv package,
            AdminShell.Submodel sm,
            PlottingOptions options,
            PluginEventStack eventStack)
        {
            // set the context
            this.thePackage = package;
            this.theSubmodel = sm;
            this.theOptions = options;
            this.theEventStack = eventStack;

            // ok, directly set contents
            SetContents();
        }

        private void SetContents()
        {
            // already set the contents?
        }

        //
        // Data logic
        //



        //
        // Mechanics
        //

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // languages
            foreach (var lng in AasxLanguageHelper.GetLangCodes())
                ComboBoxLang.Items.Add("" + lng);
            ComboBoxLang.Text = "en";

            // try display plot items
            PlotItems.RebuildFromSubmodel(theSubmodel);
            DataGridPlotItems.DataContext = PlotItems;

            // start a timer
            // Timer for status
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            // ReSharper disable once RedundantDelegateCreation
            dispatcherTimer.Tick += (se, e2) =>
            {
                if (PlotItems != null)
                {
                    PlotItems.UpdateValues();
                }
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            dispatcherTimer.Start();
        }
    }
}
