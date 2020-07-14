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
    /// <summary>
    /// Interaktionslogik für FormSubControlProperty.xaml
    /// </summary>
    public partial class FormSubControlSMEBase : UserControl
    {
        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place,
        /// in order to distinguish between user updates and program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        public FormSubControlSMEBase()
        {
            InitializeComponent();
        }

        public class IndividualDataContext
        {
            public FormInstanceSubmodelElement instance;
            public FormDescSubmodelElement desc;
            public AdminShell.SubmodelElement sme;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                // this "header" (base) element uses the same context as the actual SME
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceSubmodelElement;
                dc.desc = dc.instance?.desc as FormDescSubmodelElement;
                dc.sme = dc.instance?.sme;

                if (dc.instance == null || dc.desc == null || dc.sme == null)
                    return null;
                return dc;
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

        private void UpdateDisplay()
        {
            // access
            var dc = IndividualDataContext.CreateDataContext(this.DataContext);
            if (dc?.desc == null || dc.sme == null)
                return;

            // eval visibilities
            var visiIdShort = dc.desc.FormEditIdShort;
            var visiDescription = dc.desc.FormEditDescription;
            var visiAtAll = visiIdShort || visiDescription;

            // set plain fields
            LabelIdShort.Visibility = (visiIdShort) ? Visibility.Visible : Visibility.Collapsed;
            TextBoxIdShort.Visibility = LabelIdShort.Visibility;

            TextBoxIdShort.Text = "" + dc.sme.idShort;
            TextBoxIdShort.TextChanged += (object sender3, TextChangedEventArgs e3) =>
            {
                if (!UpdateDisplayInCharge)
                    dc.instance.Touch();
                dc.sme.idShort = TextBoxIdShort.Text;
            };

            LabelDescription.Visibility = (visiDescription) ? Visibility.Visible : Visibility.Collapsed;
            TextBlockInfo.Visibility = LabelDescription.Visibility;
            ButtonLangPlus.Visibility = LabelDescription.Visibility;

            TheGrid.Visibility = (visiAtAll) ? Visibility.Visible : Visibility.Collapsed;
            GridOuter.Margin = new Thickness((visiAtAll) ? 2.0 : 0.0);

            // set flag
            UpdateDisplayInCharge = true;

            // DELETE all grid childs with ROW > 1
            var todel = new List<UIElement>();
            foreach (var c in TheGrid.Children)
                if (c is UIElement && Grid.GetRow(c as UIElement) > 1)
                    todel.Add(c as UIElement);
            foreach (var td in todel)
                TheGrid.Children.Remove(td);

            // renew row definitions
            TheGrid.RowDefinitions.Clear();

            var rd = new RowDefinition();
            rd.Height = GridLength.Auto;
            TheGrid.RowDefinitions.Add(rd);

            var rd2 = new RowDefinition();
            rd2.Height = GridLength.Auto;
            TheGrid.RowDefinitions.Add(rd2);

            // build up net grid
            if (visiDescription)
            {
                int row = 2;
                if (dc.sme.description != null && dc.sme.description.langString != null)
                    foreach (var ls in dc.sme.description.langString)
                    {
                        // another row
                        rd = new RowDefinition();
                        rd.Height = GridLength.Auto;
                        TheGrid.RowDefinitions.Add(rd);

                        // build lang combo
                        var cb = new ComboBox();
                        cb.Margin = new Thickness(2);
                        if (FormDescMultiLangProp.DefaultLanguages != null)
                            foreach (var l in FormDescMultiLangProp.DefaultLanguages)
                                cb.Items.Add(l);
                        cb.IsEditable = true;
                        cb.Text = ls.lang;
                        cb.SelectionChanged += (object sender2, SelectionChangedEventArgs e2) =>
                        {
                            if (!UpdateDisplayInCharge)
                                dc.instance.Touch();
                            ls.lang = "" + cb.SelectedItem;
                        };
                        cb.KeyUp += (object sender3, KeyEventArgs e3) =>
                        {
                            if (!UpdateDisplayInCharge)
                                dc.instance.Touch();
                            ls.lang = cb.Text;
                        };

                        Grid.SetRow(cb, row);
                        Grid.SetColumn(cb, 1);
                        TheGrid.Children.Add(cb);

                        // build str text
                        var tb = new TextBox();
                        tb.Margin = new Thickness(2);
                        tb.Text = ls.str;
                        tb.TextChanged += (object sender2, TextChangedEventArgs e2) =>
                        {
                            if (!UpdateDisplayInCharge)
                                dc.instance.Touch();
                            ls.str = tb.Text;
                        };

                        Grid.SetRow(tb, row);
                        Grid.SetColumn(tb, 2);
                        TheGrid.Children.Add(tb);

                        // build minus button
                        var bt = new Button();
                        bt.Margin = new Thickness(2);
                        bt.Content = "-";
                        var lsToDel = ls;
                        bt.Click += (object sender3, RoutedEventArgs e3) =>
                        {
                            if (dc.sme?.description?.langString != null)
                                if (dc.sme.description.langString.Contains(lsToDel))
                                {
                                    dc.instance.Touch();
                                    dc.sme.description.langString.Remove(lsToDel);
                                    UpdateDisplay();
                                }
                        };

                        Grid.SetRow(bt, row);
                        Grid.SetColumn(bt, 3);
                        TheGrid.Children.Add(bt);

                        // increase the row
                        row++;
                    }
            }

            // release flag
            UpdateDisplayInCharge = false;

        }

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
                if (dc.sme.description == null)
                    dc.sme.description = new AdminShell.Description();

                dc.instance.Touch();
                dc.sme.description.langString.Add(new AdminShell.LangStr("", ""));

                // show
                UpdateDisplay();
            }
        }
    }
}
