using AdminShellNS;
using AasxGlobalLogging;
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

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für ToolControlFindReplace.xaml
    /// </summary>
    public partial class ToolControlFindReplace : UserControl
    {
        //
        // Members
        //

        public AdminShellUtil.SearchOptions TheSearchOptions = new AdminShellUtil.SearchOptions();
        public AdminShellUtil.SearchResults TheSearchResults = new AdminShellUtil.SearchResults();

        public AdminShell.AdministrationShellEnv TheAasEnv = null;

        public delegate void ResultSelectedDelegate(AdminShellUtil.SearchResultItem resultItem);

        public event ResultSelectedDelegate ResultSelected = null;

        private int CurrentResultIndex = -1;

        //
        // Initialize
        //

        public ToolControlFindReplace()
        {
            InitializeComponent();

            TheSearchOptions.allowedAssemblies = new System.Reflection.Assembly[] { typeof(AdminShell).Assembly };

            // the combo box needs a special treatment in order to have it focussed ..
            ComboBoxToolsFindText.Loaded += (object sender, RoutedEventArgs e) =>
            {
                // try focus again after loading ..
                ComboBoxToolsFindText.Focus();
            };

            ComboBoxToolsFindText.GotFocus += (object sender, RoutedEventArgs e) =>
            {
                var textBox = ComboBoxToolsFindText.Template.FindName("PART_EditableTextBox", ComboBoxToolsFindText) as TextBox;
                if (textBox != null)
                    textBox.Select(0, textBox.Text.Length);
            };

        }

        //
        // Public functionality
        //

        public void FocusFirstField()
        {
            if (ComboBoxToolsFindText == null)
                return;

            ComboBoxToolsFindText.Focus();
        }

        public void FindForward()
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsFindForward, null);
        }

        public void FindBackward()
        {
            // simulate
            ButtonToolsFind_Click(ButtonToolsFindBackward, null);
        }

        public void ClearResults()
        {
            TheSearchResults.Clear();
            CurrentResultIndex = -1;
        }

        public void UpdateToOptions()
        {
            TheSearchOptions.findText = ComboBoxToolsFindText.Text;
            TheSearchOptions.isIgnoreCase = CheckBoxToolsFindIgnoreCase.IsChecked == true;
            TheSearchOptions.isRegex = CheckBoxToolsFindRegex.IsChecked == true;
        }

        public void SetFindInfo(int index, int count, AdminShellUtil.SearchResultItem sri)
        {
            if (this.ButtonToolsFindInfo == null)
                return;

            var baseTxt = $"{index} of {count}";
            /*
            if (sri != null && sri.businessObject != null && sri.businessObject is AdminShell.Referable)
                baseTxt += " in " + (sri.businessObject as AdminShell.Referable).GetElementName();
            */

            this.ButtonToolsFindInfo.Text = baseTxt;
        }

        public void DoSearch()
        {
            // access
            if (TheSearchOptions == null || TheSearchResults == null || TheAasEnv == null)
                return;

            // do not accept empty field
            if (TheSearchOptions.findText == null || TheSearchOptions.findText.Length < 1)
            {
                this.ButtonToolsFindInfo.Text = "no search!";
                return;
            }

            // execution
            try
            {
                AdminShellUtil.EnumerateSearchable(TheSearchResults, TheAasEnv, "", 0, TheSearchOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When searching for results");
            }

            // try to go to 1st result
            CurrentResultIndex = -1;
            if (TheSearchResults.foundResults != null && TheSearchResults.foundResults.Count > 0 && ResultSelected != null)
            {
                CurrentResultIndex = 0;
                var sri = TheSearchResults.foundResults[0];
                SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri); 
                ResultSelected(sri);
            }
            else
            {
                this.ButtonToolsFindInfo.Text = "not found!";
            }
        }

        //
        // Callbacks
        //

        private void ComboBoxToolsFindText_KeyUp(object sender, KeyEventArgs e)
        {
            if (ComboBoxToolsFindText == null || TheSearchOptions == null)
                return;
          
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                // kick a new search
                UpdateToOptions();
                ClearResults();
                DoSearch();
            }
        }

        private void ButtonToolsFind_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxToolsFindText == null || TheSearchResults == null || TheSearchResults.foundResults == null || ResultSelected == null)
                return;

            if (sender == ButtonToolsFindBackward || sender == ButtonToolsFindForward)
            {
                // 1st check .. renew search?
                if (ComboBoxToolsFindText.Text != TheSearchOptions.findText)
                {
                    // kick a new search
                    TheSearchOptions.findText = ComboBoxToolsFindText.Text;
                    ClearResults();
                    DoSearch();
                    return;
                }
            }

            // continue search?
            if (sender == ButtonToolsFindBackward && CurrentResultIndex > 0)
            {
                CurrentResultIndex--;
                var sri = TheSearchResults.foundResults[CurrentResultIndex];
                SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                ResultSelected(sri);
            }

            if (sender == ButtonToolsFindForward && CurrentResultIndex >= 0 && CurrentResultIndex < TheSearchResults.foundResults.Count - 1)
            {
                CurrentResultIndex++;
                var sri = TheSearchResults.foundResults[CurrentResultIndex];
                SetFindInfo(1 + CurrentResultIndex, TheSearchResults.foundResults.Count, sri);
                ResultSelected(sri);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.ButtonToolsFindInfo.Text = "";
        }
    }
}
