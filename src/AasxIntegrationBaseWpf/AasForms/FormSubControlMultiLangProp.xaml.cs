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
    public partial class FormSubControlMultiLangProp : UserControl
    {
        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place, in order to distinguish between user updates and
        /// program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        public FormSubControlMultiLangProp()
        {
            InitializeComponent();
        }

        public class IndividualDataContext
        {
            public FormInstanceMultiLangProp instance;
            public FormDescMultiLangProp desc;
            public AdminShell.MultiLanguageProperty prop;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceMultiLangProp;
                dc.desc = dc.instance?.desc as FormDescMultiLangProp;
                dc.prop = dc.instance?.sme as AdminShell.MultiLanguageProperty;

                if (dc.instance == null || dc.desc == null || dc.prop == null)
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
            if (dc == null)
                return;

            // set plain fields
            TextBlockIndex.Text = (!dc.instance.ShowIndex) ? "" : "#" + (1 + dc.instance.Index);
            TextBlockInfo.Visibility = (dc.prop.value == null || dc.prop.value.IsEmpty)
                ? Visibility.Visible
                : Visibility.Hidden;

            // set flag
            UpdateDisplayInCharge = true;

            // DELETE all grid childs with ROW > 0
            var todel = new List<UIElement>();
            foreach (var c in TheGrid.Children)
                if (c is UIElement && Grid.GetRow(c as UIElement) > 0)
                    todel.Add(c as UIElement);
            foreach (var td in todel)
                TheGrid.Children.Remove(td);

            // renew row definition
            TheGrid.RowDefinitions.Clear();

            var rd = new RowDefinition();
            rd.Height = GridLength.Auto;
            TheGrid.RowDefinitions.Add(rd);

            // build up net grid
            int row = 1;
            if (dc.prop.value != null && dc.prop.value.langString != null)
                foreach (var ls in dc.prop.value.langString)
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
                        if (dc.prop?.value?.langString != null)
                            if (dc.prop.value.langString.Contains(lsToDel))
                            {
                                dc.instance.Touch();
                                dc.prop.value.langString.Remove(lsToDel);
                                UpdateDisplay();
                            }
                    };

                    Grid.SetRow(bt, row);
                    Grid.SetColumn(bt, 3);
                    TheGrid.Children.Add(bt);

                    // increase the row
                    row++;
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
                if (dc.prop.value == null)
                    dc.prop.value = new AdminShell.LangStringSet();

                dc.instance.Touch();
                dc.prop.value.Add("", "");

                // show
                UpdateDisplay();
            }
        }
    }
}
