using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AdminShellNS;
using AasxIntegrationBase;
using System.Windows.Data;
using System.Windows.Threading;

namespace AasxIntegrationBase.AasForms
{
    /// <summary>
    /// Interaktionslogik for FormElementControl.xaml
    /// </summary>
    public partial class FormListOfSameControl : UserControl, IFormListControl
    {
        // Members

        protected static int maxRowsBound = 9999;
        protected int minRows = 0;
        protected int maxRows = 0;
        protected bool showButtonsMinus = false;

        // Constructurs

        public FormListOfSameControl()
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
            // own instance
            if (property == IFormListControlPropertyType.FormSmaller && value is bool
                && TextBlockFormInfo != null)
                TextBlockFormInfo.Visibility = (bool)value ? Visibility.Visible : Visibility.Collapsed;

            // sub instances?
            var inst = this.DataContext as FormInstanceListOfSame;
            if (inst == null || inst.SubInstances == null || inst.SubInstances.Count < 1)
                return;

            // ReSharper disable SuspiciousTypeConversion.Global
            foreach (var si in inst.SubInstances)
                if (si != null && si is IFormListControl)
                    (si as IFormListControl).SetProperty(property, value);
            // ReSharper enable SuspiciousTypeConversion.Global
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
            // access
            var inst = this.DataContext as FormInstanceListOfSame;
            if (inst == null || inst.SubInstances == null || inst.SubInstances.Count < 1)
                return;

            // pass on
            var sc = inst.SubInstances[0].subControl;
            if (sc != null && sc is IFormListControl)
                (sc as IFormListControl).ContentFocus();
        }

        // own functionalities

        public Button CreateButtonLike(Button src, string content = null)
        {
            var bt = new Button();
            bt.BorderBrush = src.BorderBrush;
            bt.BorderThickness = src.BorderThickness;
            bt.Background = src.Background;
            bt.Foreground = src.Foreground;
            bt.Margin = src.Margin;
            if (content != null)
                bt.Content = content;
            return bt;
        }

        public Binding CreateBindingWithElementName(string path, string elementName)
        {
            var bi = new Binding(path);
            bi.ElementName = elementName;
            return bi;
        }

        public void UpdateDisplay()
        {
            // access
            var inst = this.DataContext as FormInstanceListOfSame;
            var desc = inst?.workingDesc as FormDescSubmodelElement;
            if (inst == null || desc == null || GridOuterElement == null || GridInner == null)
                return;

            // obligatory
            this.TextBlockFormTitle.Text = "" + desc.FormTitle;
            this.TextBlockFormInfo.Text = "" + desc.FormInfo;
            this.TextBlockFormInfo.Visibility = (this.TextBlockFormInfo.Text.Trim() != "")
                ? Visibility.Visible
                : Visibility.Collapsed;

            // url link
            ButtonFormUrl.Visibility = Visibility.Hidden;
            if (desc.FormUrl != null && desc.FormUrl.Length > 0)
            {
                ButtonFormUrl.Visibility = Visibility.Visible;
                ButtonFormUrl.Click += (object sender3, RoutedEventArgs e3) =>
                {
                    // try find topmost instance
                    var top = FormInstanceHelper.GetTopMostParent(inst);
                    var topBase = top as FormInstanceBase;
                    if (topBase != null && topBase.outerEventStack != null)
                    {
                        // give over to event stack
                        var evt = new AasxPluginResultEventDisplayContentFile();
                        evt.fn = desc.FormUrl;
                        evt.preferInternalDisplay = true;
                        topBase.outerEventStack.PushEvent(evt);
                    }
                };
            }

            // (re-) create rows
            GridInner.Children.Clear();
            GridInner.RowDefinitions.Clear();
            int ri = 0;
            foreach (var si in inst.SubInstances)
                if (si?.subControl != null)
                {
                    // row
                    var rd = new RowDefinition();
                    rd.Height = GridLength.Auto;
                    GridInner.RowDefinitions.Add(rd);

                    // have the control itself
                    var sc = si.subControl;

                    Grid.SetRow(sc, ri);
                    Grid.SetColumn(sc, 0);
                    GridInner.Children.Add(sc);

                    if (this.showButtonsMinus)
                    {

                        // make a button like the "-"
                        var bt = CreateButtonLike(ButtonInstancePlus, content: "&#10134;");
                        bt.VerticalAlignment = VerticalAlignment.Top;
                        bt.Content = "\u2796";

                        if (inst.workingDesc != null && inst.workingDesc is FormDescSubmodelElementCollection)
                            bt.Margin = new Thickness(
                                bt.Margin.Left, bt.Margin.Top + 6, bt.Margin.Right, bt.Margin.Bottom);

                        bt.SetBinding(
                            Button.WidthProperty, CreateBindingWithElementName("ActualWidth", "ButtonInstancePlus"));
                        bt.SetBinding(
                            Button.HeightProperty, CreateBindingWithElementName("ActualHeight", "ButtonInstancePlus"));

                        // remeber the instances
                        var masterInst = inst;
                        var subInst = si;

                        // attach the lambda
                        bt.Click += (object sender, RoutedEventArgs e) =>
                        {
                            if (inst.SubInstances.Count > this.minRows)
                            {
                                // carefully delete
                                // ReSharper disable EmptyGeneralCatchClause
                                try
                                {
                                    masterInst.SubInstances.Remove(subInst);
                                }
                                catch { }
                                // ReSharper enable EmptyGeneralCatchClause

                                // redraw
                                UpdateDisplay();
                            }
                        };

                        Grid.SetRow(bt, ri);
                        Grid.SetColumn(bt, 1);
                        GridInner.Children.Add(bt);

                    }
                    else
                    {
                        // give the subcontrol a little more room
                        Grid.SetColumnSpan(sc, 2);
                    }

                    // next row
                    ri++;
                }

        }

        private void ButtonFormUrl_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        // Callbacks

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // need data context of the UC in the right shape
            var inst = this.DataContext as FormInstanceListOfSame;
            var desc = inst?.workingDesc as FormDescSubmodelElement;
            if (inst == null || desc == null || GridOuterElement == null || GridInner == null)
                return;

            // figure out, how much additional rows to render
            if (desc.Multiplicity == FormMultiplicity.One)
            {
                minRows = 1;
                maxRows = 1;
                showButtonsMinus = false;
                ButtonInstancePlus.Visibility = Visibility.Hidden;
                ButtonInstancePlus.IsEnabled = false;
            }
            if (desc.Multiplicity == FormMultiplicity.OneToMany)
            {
                minRows = 1;
                maxRows = maxRowsBound;
                showButtonsMinus = true;
                ButtonInstancePlus.Visibility = Visibility.Visible;
                ButtonInstancePlus.IsEnabled = true;
            }
            if (desc.Multiplicity == FormMultiplicity.ZeroToOne)
            {
                minRows = 0;
                maxRows = 1;
                showButtonsMinus = true;
                ButtonInstancePlus.Visibility = Visibility.Visible;
                ButtonInstancePlus.IsEnabled = true;
            }
            if (desc.Multiplicity == FormMultiplicity.ZeroToMany)
            {
                minRows = 0;
                maxRows = maxRowsBound;
                showButtonsMinus = true;
                ButtonInstancePlus.Visibility = Visibility.Visible;
                ButtonInstancePlus.IsEnabled = true;
            }

            // reserve instances
            if (inst.SubInstances == null)
                inst.SubInstances = new List<FormInstanceBase>();

            while (inst.SubInstances.Count < minRows)
            {
                var ni = desc.CreateInstance(inst);
                if (ni == null)
                    break;

                inst.SubInstances.Add(ni);
            }

            // update display to create rows
            UpdateDisplay();
        }

        private void ButtonInstancePlusMinus_Click(object sender, RoutedEventArgs e)
        {
            // need data context of the UC in the right shape
            var inst = this.DataContext as FormInstanceListOfSame;
            var desc = inst?.workingDesc as FormDescSubmodelElement;
            if (inst == null || desc == null || GridOuterElement == null || GridInner == null ||
                inst.SubInstances == null)
                return;

            // Plus, allowed?
            if (sender == ButtonInstancePlus && inst.SubInstances.Count < this.maxRows)
            {
                // add a instance
                var ni = desc.CreateInstance(inst);
                if (ni != null)
                    inst.SubInstances.Add(ni);


                // redraw
                UpdateDisplay();

                // try to set focus, AFTER controls have been realized
                // see: https://stackoverflow.com/questions/567216/is-there-a-all-children-loaded-event-in-wpf
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    // show newly created item
                    foreach (var i in inst.SubInstances)
                    {
                        var flc = i.subControl as IFormListControl;
                        if (flc != null && i != ni)
                            flc.ShowContentItems(false);
                        if (flc != null && i == ni)
                        {
                            flc.ShowContentItems(true);
                            flc.ContentFocus();
                        }
                    }
                }));
            }
        }

    }
}
