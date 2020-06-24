using AasxIntegrationBase;
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
    public partial class TechDataPropertiesControl : UserControl
    {
        public TechDataPropertiesControl()
        {
            InitializeComponent();
        }

        private TableCell NewTableCellPara(
            string runText, string cellStyleName = null,
            string paraStyleName = null, int columnSpan = 1, Nullable<Thickness> padding = null)
        {
            var run = new Run("" + runText);
            var para = new Paragraph(run);
            if (paraStyleName != null)
                para.Style = this.FindResource(paraStyleName) as Style;
            var cell = new TableCell(para);
            cell.ColumnSpan = columnSpan;
            if (cellStyleName != null)
                cell.Style = this.FindResource(cellStyleName) as Style;
            if (padding != null)
                cell.Padding = padding.Value;
            return cell;
        }

        public void TableAddPropertyRows_Recurse(
            DefinitionsZveiTechnicalData.SetOfDefs theDefs, string defaultLang, AdminShellPackageEnv package,
            Table table, AdminShell.SubmodelElementWrapperCollection smwc, int depth = 0)
        {
            // access
            if (table == null || smwc == null)
                return;

            // make a RowGroup
            var currentRowGroup = new TableRowGroup();
            table.RowGroups.Add(currentRowGroup);

            // go element by element
            foreach (var smw in smwc)
            {
                // access
                if (smw?.submodelElement == null)
                    continue;
                var sme = smw.submodelElement;

                // prepare information about displayName, semantics unit
                var semantics = "-";
                var unit = "";
                // make up property name (1)
                var dispName = "" + sme.idShort;
                var dispNameWithCD = dispName;

                // make up semantics
                if (sme.semanticId != null)
                {
                    if (sme.semanticId.Matches(theDefs.CD_NonstandardizedProperty.GetSingleKey()))
                        semantics = "Non-standardized";
                    else
                    {
                        // the semantics display
                        semantics = "" + sme.semanticId.ToString(2);

                        // find better property name (2)
                        var cd = package?.AasEnv?.FindConceptDescription(sme.semanticId);
                        if (cd != null)
                        {
                            // unit?
                            unit = "" + cd.GetIEC61360()?.unit;

                            // names
                            var dsn = cd.GetDefaultShortName(defaultLang);
                            if (dsn != "")
                                dispNameWithCD = dsn;

                            var dpn = cd.GetDefaultPreferredName(defaultLang);
                            if (dpn != "")
                                dispNameWithCD = dpn;
                        }
                    }
                }

                // make up even better better property name (3)
                var descDef = "" + sme.description?.langString?.GetDefaultStr(defaultLang);
                if (descDef.HasContent())
                {
                    dispName = descDef;
                    dispNameWithCD = dispName;
                }

                // special function?
                if (sme is AdminShell.SubmodelElementCollection &&
                        true == sme.semanticId?.Matches(theDefs.CD_MainSection.GetSingleKey()))
                {
                    // finalize current row group??
                    ;

                    // Main Section
                    var cell = NewTableCellPara("" + dispName, null, "ParaStyleSectionMain", columnSpan: 3,
                            padding: new Thickness(5 * depth, 0, 0, 0));

                    // add cell (to a new row group)
                    currentRowGroup = new TableRowGroup();
                    table.RowGroups.Add(currentRowGroup);
                    var tr = new TableRow();
                    currentRowGroup.Rows.Add(tr);
                    tr.Cells.Add(cell);

                    // recurse into that (again, new group)
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, table,
                        (sme as AdminShell.SubmodelElementCollection).value, depth + 1);

                    // start new group
                    currentRowGroup = new TableRowGroup();
                    table.RowGroups.Add(currentRowGroup);
                }
                else
                if (sme is AdminShell.SubmodelElementCollection &&
                    true == sme.semanticId?.Matches(theDefs.CD_SubSection.GetSingleKey()))
                {
                    // finalize current row group??
                    ;

                    // Sub Section
                    var cell = NewTableCellPara("" + dispName, null, "ParaStyleSectionSub", columnSpan: 3,
                            padding: new Thickness(5 * depth, 0, 0, 0));

                    // add cell (to a new row group)
                    currentRowGroup = new TableRowGroup();
                    table.RowGroups.Add(currentRowGroup);
                    var tr = new TableRow();
                    currentRowGroup.Rows.Add(tr);
                    tr.Cells.Add(cell);

                    // recurse into that
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, table,
                        (sme as AdminShell.SubmodelElementCollection).value, depth + 1);

                    // start new group
                    currentRowGroup = new TableRowGroup();
                    table.RowGroups.Add(currentRowGroup);
                }
                else
                if (sme is AdminShell.Property || sme is AdminShell.MultiLanguageProperty || sme is AdminShell.Range)
                {
                    // make a row (in current group)
                    var tr = new TableRow();
                    currentRowGroup.Rows.Add(tr);

                    // add cells
                    tr.Cells.Add(NewTableCellPara(dispNameWithCD, "CellStylePropertyLeftmost", "ParaStyleProperty",
                                padding: new Thickness(5 * depth, 0, 0, 0)));
                    tr.Cells.Add(NewTableCellPara(semantics, "CellStylePropertyOther", "ParaStyleProperty"));
                    tr.Cells.Add(NewTableCellPara("" + sme.ValueAsText(defaultLang) + " " + unit,
                                "CellStylePropertyOther", "ParaStyleProperty"));
                }
            }

            // finalize current row group??
            ;
        }

        public FlowDocument CreateFlowDocument(
            AdminShellPackageEnv package, DefinitionsZveiTechnicalData.SetOfDefs theDefs,
            string defaultLang, AdminShell.Submodel sm)
        {
            // access
            if (package == null || theDefs == null || sm == null)
                return null;

            // section Properties
            var smcProps =
                sm.submodelElements.FindFirstSemanticIdAs<AdminShell.SubmodelElementCollection>(
                    theDefs.CD_TechnicalProperties.GetSingleKey());
            if (smcProps == null)
                return null;

            // make document
            FlowDocument doc = new FlowDocument();

            // make a table
            var table = new Table();
            doc.Blocks.Add(table);

            table.CellSpacing = 0;
            table.FontFamily = new FontFamily("Arial");

            // make a header
            var tgHeader = new TableRowGroup();
            table.RowGroups.Add(tgHeader);

            var trh = new TableRow();
            tgHeader.Rows.Add(trh);

            trh.Cells.Add(NewTableCellPara("Property", "CellStyleHeaderLeftmost", "ParaStyleHeader"));
            trh.Cells.Add(NewTableCellPara("Semantics", "CellStyleHeaderOther", "ParaStyleHeader"));
            trh.Cells.Add(NewTableCellPara("Value", "CellStyleHeaderOther", "ParaStyleHeader"));

            // print properties

            TableAddPropertyRows_Recurse(theDefs, defaultLang, package, table, smcProps.value);

            // dummy cells
#if FALSE
            int dummyCells = 0;
            if (dummyCells > 0)
            {
                // make a row (in current group)
                var currentRowGroup = new TableRowGroup();
                table.RowGroups.Add(currentRowGroup);

                // add cells
                for (int i = 0; i < 100; i++)
                {
                    var tr = new TableRow();
                    currentRowGroup.Rows.Add(tr);

                    tr.Cells.Add(
                        NewTableCellPara(
                            "" + i,
                            "CellStylePropertyLeftmost", "ParaStyleProperty",
                            padding: new Thickness(5 * 0, 0, 0, 0)));
                    tr.Cells.Add(NewTableCellPara("" + i * i, "CellStylePropertyOther", "ParaStyleProperty"));
                    tr.Cells.Add(
                        NewTableCellPara(
                            "" + Math.Sqrt(1.0 * i), "CellStylePropertyOther", "ParaStyleProperty"));
                }
            }
#endif

            // ok
            return doc;
        }

        public void SetContents(
            AdminShellPackageEnv package, DefinitionsZveiTechnicalData.SetOfDefs theDefs, string defaultLang,
            AdminShell.Submodel sm)
        {
            FlowDocViewer.Document = CreateFlowDocument(package, theDefs, defaultLang, sm);
        }

    }
}
