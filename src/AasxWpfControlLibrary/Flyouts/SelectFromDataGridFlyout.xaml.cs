/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;
using NPOI.HPSF;

namespace AasxPackageExplorer
{

    /// <summary>
    /// Creates a flyout in order to select items from a list
    /// </summary>
    public partial class SelectFromDataGridFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataSelectFromDataGrid DiaData = new AnyUiDialogueDataSelectFromDataGrid();

        public SelectFromDataGridFlyout()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // dialogue width
            if (DiaData.MaxWidth.HasValue && DiaData.MaxWidth.Value > 200)
            {
                OuterGrid.MaxWidth = DiaData.MaxWidth.Value;
				OuterGrid.MaxHeight = DiaData.MaxWidth.Value * (1080.0 / 1920.0);
			}

			// fill caption
			if (DiaData.Caption != null)
                TextBlockCaption.Text = "" + DiaData.Caption;

			// fill datagrid
            if (DiaData.ColumnDefs != null)
            {
                int i = 0;
                foreach (var cd in DiaData.ColumnDefs)
                {
                    var dc = new DataGridTextColumn()
                    {
                        Width = new DataGridLength(cd.Value,
                            (cd.Type == AnyUiGridUnitType.Auto) ? DataGridLengthUnitType.Auto
                            : ((cd.Type == AnyUiGridUnitType.Pixel) ? DataGridLengthUnitType.Pixel
                                : DataGridLengthUnitType.Star)),
                        Header = "" + ((DiaData.ColumnHeaders != null && i < DiaData.ColumnHeaders.Length)
                                    ? DiaData.ColumnHeaders[i] : ""),
                        Binding = new Binding($"Cells[{i}]")
                    };

                    DataGridEntities.Columns.Add(dc);
                    i++;
                }
            }

			DataGridEntities.ItemsSource = DiaData.Rows;

			// alternative buttons
			if (DiaData.AlternativeSelectButtons != null)
            {
                this.ButtonsPanel.Children.Clear();
                foreach (var txt in DiaData.AlternativeSelectButtons)
                {
                    var b = new Button();
                    b.Content = "" + txt;
                    b.Foreground = Brushes.White;
                    b.FontSize = 18;
                    b.Padding = new Thickness(4);
                    b.Margin = new Thickness(4);
                    b.SetResourceReference(Control.StyleProperty, "TranspRoundCorner");
                    b.Click += ButtonSelect_Click;
                    this.ButtonsPanel.Children.Add(b);
                }
            }
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        //
        // Mechanics
        //


        private bool PrepareResult()
        {
            var i = DataGridEntities.SelectedIndex;
            if (DiaData.Rows != null && i >= 0 && i < DiaData.Rows.Count)
            {
                DiaData.ResultIndex = i;
                DiaData.ResultItem = DiaData.Rows[i];
				DiaData.ResultItems = DataGridEntities.SelectedItems?.Cast<AnyUiDialogueDataGridRow>().ToList();

				return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            // check which button
            for (int bi = 0; bi < ButtonsPanel.Children.Count; bi++)
                if (sender == ButtonsPanel.Children[bi])
                    DiaData.ButtonIndex = bi;

            // give result
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            DiaData.ResultIndex = -1;
            DiaData.ResultItem = null;
            ControlClosed?.Invoke();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // only with single buttons
            if (ButtonsPanel.Children.Count > 1)
                return;

            // ok
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }
    }
}
