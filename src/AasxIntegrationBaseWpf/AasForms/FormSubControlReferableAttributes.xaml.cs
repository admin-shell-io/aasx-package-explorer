/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
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
using AasCore.Aas3_0_RC02;
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormSubControlReferableAttributes : UserControl
    {
        // Members

        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place, in order to distinguish between user updates and
        /// program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        // Constructors & access

        public class IndividualDataContext
        {
            public FormInstanceBase instance;
            public FormDescReferable desc;
            public AasCore.Aas3_0_RC02.IReferable rf;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();

                if (dataContext is FormInstanceSubmodel fism)
                {
                    dc.desc = fism.desc as FormDescReferable;
                    dc.rf = fism.sm;
                    dc.instance = fism;
                }

                if (dataContext is FormInstanceSubmodelElement fisme)
                {
                    dc.desc = fisme.desc as FormDescReferable;
                    dc.rf = fisme.sme;
                    dc.instance = fisme;
                }

                if (dc.instance == null || dc.desc == null || dc.rf == null)
                    return null;
                return dc;
            }
        }

        public FormSubControlReferableAttributes()
        {
            InitializeComponent();
        }

        // own functionality

        public void UpdateDisplay()
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc?.desc == null || dc.rf == null)
                return;

            // eval visibilities
            var visiIdShort = dc.desc.FormEditIdShort;
            var visiDescription = dc.desc.FormEditDescription;
            var visiAtAll = visiIdShort || visiDescription;

            // set plain fields
            LabelIdShort.Visibility = (visiIdShort) ? Visibility.Visible : Visibility.Collapsed;
            TextBoxIdShort.Visibility = LabelIdShort.Visibility;

            TextBoxIdShort.Text = "" + dc.rf.IdShort;
            TextBoxIdShort.TextChanged += (object sender3, TextChangedEventArgs e3) =>
            {
                if (!UpdateDisplayInCharge)
                    dc.instance.Touch();
                dc.rf.IdShort = TextBoxIdShort.Text;
            };

            LabelDescription.Visibility = (visiDescription) ? Visibility.Visible : Visibility.Collapsed;
            TextBlockInfo.Visibility = LabelDescription.Visibility;
            ButtonLangPlus.Visibility = LabelDescription.Visibility;

            GridAttributes.Visibility = (visiAtAll) ? Visibility.Visible : Visibility.Collapsed;

            // set flag
            UpdateDisplayInCharge = true;

            // DELETE all grid childs with ROW > 1
            var todel = new List<UIElement>();
            foreach (var c in GridAttributes.Children)
                if (c is UIElement && Grid.GetRow(c as UIElement) > 1)
                    todel.Add(c as UIElement);
            foreach (var td in todel)
                GridAttributes.Children.Remove(td);

            // renew row definitions
            GridAttributes.RowDefinitions.Clear();

            var rd = new RowDefinition();
            rd.Height = GridLength.Auto;
            GridAttributes.RowDefinitions.Add(rd);

            var rd2 = new RowDefinition();
            rd2.Height = GridLength.Auto;
            GridAttributes.RowDefinitions.Add(rd2);

            // build up net grid
            if (visiDescription)
            {
                int row = 2;
                if (dc.rf.Description != null && dc.rf.Description != null)
                    foreach (var ls in dc.rf.Description)
                    {
                        // another row
                        rd = new RowDefinition();
                        rd.Height = GridLength.Auto;
                        GridAttributes.RowDefinitions.Add(rd);

                        // build lang combo
                        var cb = new ComboBox();
                        cb.Margin = new Thickness(2);
                        if (FormDescMultiLangProp.DefaultLanguages != null)
                            foreach (var l in FormDescMultiLangProp.DefaultLanguages)
                                cb.Items.Add(l);
                        cb.IsEditable = true;
                        cb.Text = ls.Language;
                        cb.SelectionChanged += (object sender2, SelectionChangedEventArgs e2) =>
                        {
                            if (!UpdateDisplayInCharge)
                                dc.instance.Touch();
                            ls.Language = "" + cb.SelectedItem;
                        };
                        cb.KeyUp += (object sender3, KeyEventArgs e3) =>
                        {
                            if (!UpdateDisplayInCharge)
                                dc.instance.Touch();
                            ls.Language = cb.Text;
                        };

                        Grid.SetRow(cb, row);
                        Grid.SetColumn(cb, 1);
                        GridAttributes.Children.Add(cb);

                        // build str text
                        var tb = new TextBox();
                        tb.Margin = new Thickness(2);
                        tb.Text = ls.Text;
                        tb.TextChanged += (object sender2, TextChangedEventArgs e2) =>
                        {
                            if (!UpdateDisplayInCharge)
                                dc.instance.Touch();
                            ls.Text = tb.Text;
                        };

                        Grid.SetRow(tb, row);
                        Grid.SetColumn(tb, 2);
                        GridAttributes.Children.Add(tb);

                        // build minus button
                        var bt = new Button();
                        bt.Margin = new Thickness(2);
                        bt.Content = "-";
                        var lsToDel = ls;
                        bt.Click += (object sender3, RoutedEventArgs e3) =>
                        {
                            if (dc.rf?.Description != null)
                                if (dc.rf.Description.Contains(lsToDel))
                                {
                                    dc.instance.Touch();
                                    dc.rf.Description.Remove(lsToDel);
                                    UpdateDisplay();
                                }
                        };

                        Grid.SetRow(bt, row);
                        Grid.SetColumn(bt, 3);
                        GridAttributes.Children.Add(bt);

                        // increase the row
                        row++;
                    }
            }

            // release flag
            UpdateDisplayInCharge = false;
        }

        // callbacks

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // Plus?
            if (sender == ButtonLangPlus)
            {
                // add
                if (dc.rf.Description == null)
                    dc.rf.Description = new List<AasCore.Aas3_0_RC02.LangString>();

                dc.instance.Touch();
                dc.rf.Description.Add(new AasCore.Aas3_0_RC02.LangString("", ""));

                // show
                UpdateDisplay();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // save data context
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // then update
            UpdateDisplay();
        }

    }
}
