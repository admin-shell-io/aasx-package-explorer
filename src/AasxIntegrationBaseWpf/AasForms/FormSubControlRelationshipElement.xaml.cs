﻿/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormSubControlRelationshipElement : UserControl
    {
        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place, in order to distinguish between user updates and
        /// program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        public class ViewModel : WpfViewModelBase
        {
            // data binded properties
            private AdminShell.Reference storedFirst = null;
            private AdminShell.Reference storedSecond = null;

            public AdminShell.Reference StoredFirst
            {
                get { return storedFirst; }
                set { storedFirst = value; OnPropertyChanged("InfoFirst"); }
            }

            public AdminShell.Reference StoredSecond
            {
                get { return storedSecond; }
                set { storedSecond = value; OnPropertyChanged("InfoSecond"); }
            }

            public static string FormatReference(AdminShell.Reference rf)
            {
                if (rf == null)
                    return "(no reference set)";
                if (rf.Count < 1)
                    return "(no Keys)";
                return rf.ToString(format: 1, delimiter: Environment.NewLine);
            }

            public string InfoFirst => FormatReference(storedFirst);
            public string InfoSecond => FormatReference(storedSecond);
        }

        private ViewModel theViewModel = new ViewModel();

        public class IndividualDataContext
        {
            public FormInstanceRelationshipElement instance;
            public FormDescRelationshipElement desc;
            public AdminShell.RelationshipElement relElem;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceRelationshipElement;
                dc.desc = dc.instance?.desc as FormDescRelationshipElement;
                dc.relElem = dc.instance?.sme as AdminShell.RelationshipElement;

                if (dc.instance == null || dc.desc == null || dc.relElem == null)
                    return null;
                return dc;
            }
        }

        public FormSubControlRelationshipElement()
        {
            InitializeComponent();
            GridOuter.DataContext = this.theViewModel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // save data context
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // first update
            this.theViewModel.StoredFirst = dc.relElem?.first;
            this.theViewModel.StoredSecond = dc.relElem?.second;
            UpdateDisplay();

            // attach lambdas for clear
            ButtonClearFirst.Click += (object sender5, RoutedEventArgs e5) =>
            {
                dc.instance.Touch();
                dc.relElem.first = null;
                this.theViewModel.StoredFirst = null;
            };

            ButtonClearSecond.Click += (object sender7, RoutedEventArgs e7) =>
            {
                dc.instance.Touch();
                dc.relElem.second = null;
                this.theViewModel.StoredFirst = null;
            };

            // attach lambdas for select
            for (int i = 0; i < 2; i++)
            {
                // beware of lambda
                int storedI = i;

                (new[] { ButtonSelectFirst, ButtonSelectSecond })[i].Click += (object sender6, RoutedEventArgs e6) =>
                {
                    // try find topmost instance
                    var top = FormInstanceHelper.GetTopMostParent(dc.instance);
                    var topBase = top as FormInstanceBase;
                    if (topBase != null && topBase.outerEventStack != null)
                    {
                        // give over to event stack
                        var ev = new AasxIntegrationBase.AasxPluginResultEventSelectAasEntity();
                        ev.filterEntities = AdminShell.Key.AllElements;
                        ev.showAuxPackage = true;
                        ev.showRepoFiles = true;
                        topBase.outerEventStack.PushEvent(ev);

                        // beware of lambda
                        int storedII = storedI;

                        // .. and receive incoming event
                        topBase.subscribeForNextEventReturn = (revt) =>
                        {
                            if (revt is AasxPluginEventReturnSelectAasEntity rsel && rsel.resultKeys != null)
                            {
                                dc.instance.Touch();
                                var newr = AdminShell.Reference.CreateNew(rsel.resultKeys);

                                if (storedII == 0)
                                {
                                    dc.relElem.first = newr;
                                    this.theViewModel.StoredFirst = newr;
                                }

                                if (storedII == 1)
                                {
                                    dc.relElem.second = newr;
                                    this.theViewModel.StoredSecond = newr;
                                }
                            }
                        };
                    }

                };
            }
        }

        private void UpdateDisplay()
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // set flag
            UpdateDisplayInCharge = true;

            // set plain fields
            TextBlockIndex.Text = (!dc.instance.ShowIndex) ? "" : "#" + (1 + dc.instance.Index);

            // note: the reference is already shown by data binding

            // release flag
            UpdateDisplayInCharge = false;
        }

    }
}
