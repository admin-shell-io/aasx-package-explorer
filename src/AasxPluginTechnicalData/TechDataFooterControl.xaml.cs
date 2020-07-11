using AasxPredefinedConcepts;
using AdminShellNS;
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

namespace AasxPluginTechnicalData
{
    /// <summary>
    /// Interaktionslogik für TechDataFooterControl.xaml
    /// </summary>
    public partial class TechDataFooterControl : UserControl
    {
        public TechDataFooterControl()
        {
            InitializeComponent();
        }

        public void SetContents(
            AdminShellPackageEnv package, DefinitionsZveiTechnicalData.SetOfDefs theDefs, string defaultLang,
            AdminShell.Submodel sm)
        {
            // access
            if (sm == null)
                return;

            // section General
            var smcFurther = sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                theDefs.CD_FurtherInformation.GetSingleKey());
            if (smcFurther != null)
            {
                // single items
                TextBoxValidDate.Text = "" + smcFurther.value.FindFirstSemanticIdAs<AdminShell.Property>(
                    theDefs.CD_ValidDate.GetSingleKey())?.value;

                // Lines
                var tsl = new List<string>();
                foreach (var smw in
                    smcFurther.value.FindAllSemanticId(
                        theDefs.CD_TextStatement.GetSingleKey(), allowedTypes: AdminShell.SubmodelElement.PROP_MLP))
                    tsl.Add("" + smw?.submodelElement?.ValueAsText(defaultLang));


                ItemsControlStatements.ItemsSource = tsl;
            }
        }

    }
}
