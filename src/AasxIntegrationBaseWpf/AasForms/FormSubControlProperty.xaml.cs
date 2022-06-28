/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// ReSharper disable UnusedType.Global

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
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormSubControlProperty : UserControl, IFormListControl
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
            public FormInstanceProperty instance;
            public FormDescProperty desc;
            public AdminShell.Property prop;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceProperty;
                dc.desc = dc.instance?.desc as FormDescProperty;
                dc.prop = dc.instance?.sme as AdminShell.Property;

                if (dc.instance == null || dc.desc == null || dc.prop == null)
                    return null;
                return dc;
            }
        }

        public FormSubControlProperty()
        {
            InitializeComponent();
        }

        // External interfaces

        /// <summary>
        /// Set the property, given by enum <c>IFormListControlPropertyType</c> to the
        /// value
        /// </summary>
        public void SetProperty(IFormListControlPropertyType property, object value)
        {
        }

        /// <summary>
        /// Called from outside allows to show / collapse the detailed list view
        /// </summary>
        public void ShowContentItems(bool completelyVisible)
        {
            // not collapsable
        }

        /// <summary>
        /// Tries to set the keyboard focus (cursor) to the first content field
        /// </summary>
        public void ContentFocus()
        {
            TextBoxValue.Focus();
        }

        // own functionality

        public void UpdateDisplay()
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // set flag
            UpdateDisplayInCharge = true;

            // set plain fields
            TextBlockIndex.Text = (!dc.instance.ShowIndex) ? "" : "#" + (1 + dc.instance.Index);

            // check, if combo box operation
            if (dc.desc.comboBoxChoices != null && dc.desc.comboBoxChoices.Length > 0)
            {
                // may select a different one?
                if (dc.desc.valueFromComboBoxIndex != null && dc.desc.valueFromComboBoxIndex.Length >= 1)
                {
                    for (int i = 0; i < dc.desc.valueFromComboBoxIndex.Length; i++)
                        if (dc.desc.valueFromComboBoxIndex[i].Trim() == dc.prop.value)
                        {
                            ComboBoxValue.SelectedIndex = i;
                            break;
                        }
                }
                else
                {
                    // editable combo box, initialize normal
                    ComboBoxValue.Text = "" + dc.prop.value;
                }
            }
            else
            {
                // initialize
                TextBoxValue.Text = "" + dc.prop.value;
            }

            // release flag
            UpdateDisplayInCharge = false;
        }

        // callbacks

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // save data context
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            // forward the data context to the base cub contol
            SubControlBase.DataContext = this.DataContext;

            // first, do fundamental initialisation
            if (dc.desc.comboBoxChoices != null && dc.desc.comboBoxChoices.Length > 0)
            {
                // make Comboxbox visible
                TextBoxValue.Visibility = Visibility.Hidden;
                ComboBoxValue.Visibility = Visibility.Visible;

                // initialize with choices
                ComboBoxValue.Items.Clear();
                foreach (var cbc in dc.desc.comboBoxChoices)
                    ComboBoxValue.Items.Add(cbc);

                // editable, if valueFromComboBoxIndex[] is null
                var editableMode = (dc.desc.valueFromComboBoxIndex == null ||
                    dc.desc.valueFromComboBoxIndex.Length < 1);
                ComboBoxValue.IsEditable = editableMode;

                // update property
                ComboBoxValue.SelectionChanged += (object sender2, SelectionChangedEventArgs e2) =>
                {
                    var idx = ComboBoxValue.SelectedIndex;
                    var items = dc.desc.valueFromComboBoxIndex;
                    if (items != null && items.Length > 0 && idx >= 0 && idx < items.Length && !editableMode)
                    {
                        if (!UpdateDisplayInCharge)
                        {
                            dc.instance.Touch();
                            dc.prop.value = "" + items[idx];

                            // test
                            var ndx = dc.instance.Index;
                            dc.instance?.parentInstance.TriggerSlaveEvents(dc.instance, ndx);
                        }
                    }
                };

                // update property (don't knwo to realize as lambda)
                ComboBoxValue.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                      new System.Windows.Controls.TextChangedEventHandler(ComboBox_TextChanged));
            }
            else
            {
                // make Textbox visible
                TextBoxValue.Visibility = Visibility.Visible;
                ComboBoxValue.Visibility = Visibility.Hidden;

                // slave value?
                if (dc.desc.SlaveOfIdShort != null && dc.desc.SlaveOfIdShort.Length > 0)
                    this.TextBoxValue.IsReadOnly = true;

                // update property
                TextBoxValue.TextChanged += (object sender3, TextChangedEventArgs e3) =>
                {
                    if (!UpdateDisplayInCharge)
                        dc.instance.Touch();
                    dc.prop.value = TextBoxValue.Text;
                };

            }

            // then update
            UpdateDisplay();

        }

        private void ComboBox_TextChanged(object sender3, TextChangedEventArgs e3)
        {
            // save data context
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc == null)
                return;

            if (!UpdateDisplayInCharge)
                dc.instance.Touch();
            dc.prop.value = ComboBoxValue.Text;
        }

    }
}
