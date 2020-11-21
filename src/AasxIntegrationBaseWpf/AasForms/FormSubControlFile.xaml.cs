/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AdminShellNS;

namespace AasxIntegrationBase.AasForms
{
    public partial class FormSubControlFile : UserControl
    {
        /// <summary>
        /// Is true while <c>UpdateDisplay</c> takes place, in order to distinguish between user updates and
        /// program logic
        /// </summary>
        protected bool UpdateDisplayInCharge = false;

        public FormSubControlFile()
        {
            InitializeComponent();
        }

        public class IndividualDataContext
        {
            public FormInstanceFile instance;
            public FormDescFile desc;
            public AdminShell.File file;

            public static IndividualDataContext CreateDataContext(object dataContext)
            {
                var dc = new IndividualDataContext();
                dc.instance = dataContext as FormInstanceFile;
                dc.desc = dc.instance?.desc as FormDescFile;
                dc.file = dc.instance?.sme as AdminShell.File;

                if (dc.instance == null || dc.desc == null || dc.file == null)
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

            // first update
            UpdateDisplay();

            // attach lambdas for drop
            this.BorderTargetArea.AllowDrop = true;
            this.BorderTargetArea.DragEnter += (object sender2, DragEventArgs e2) =>
            {
                e2.Effects = DragDropEffects.Copy;
            };
            this.BorderTargetArea.PreviewDragOver += (object sender3, DragEventArgs e3) =>
            {
                e3.Handled = true;
            };
            this.BorderTargetArea.Drop += (object sender4, DragEventArgs e4) =>
            {
                if (e4.Data.GetDataPresent(DataFormats.FileDrop, true))
                {
                    // Note that you can have more than one file.
                    string[] files = (string[])e4.Data.GetData(DataFormats.FileDrop);

                    // Assuming you have one file that you care about, pass it off to whatever
                    // handling code you have defined.
                    if (files != null && files.Length > 0)
                    {
                        dc.instance.Touch();
                        dc.instance.FileToLoad = files[0];
                        UpdateDisplay();
                    }
                }

                e4.Handled = true;
            };

            // attach lambdas for clear
            ButtonClear.Click += (object sender5, RoutedEventArgs e5) =>
            {
                dc.instance.Touch();
                dc.instance.FileToLoad = null;
                UpdateDisplay();
            };

            // attach lambdas for select
            ButtonSelect.Click += (object sender6, RoutedEventArgs e6) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                var res = dlg.ShowDialog();
                if (res == true)
                {
                    dc.instance.Touch();
                    dc.instance.FileToLoad = dlg.FileName;
                    UpdateDisplay();
                }
            };
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

            // show file?
            TextBlockTargetArea.Text = "Drag a file to register loading it!";
            if (dc.file.value != null && dc.file.value.Trim().Length > 0)
                TextBlockTargetArea.Text = "File current: " + dc.file.value;
            if (dc.instance.FileToLoad != null)
                TextBlockTargetArea.Text = "File to load: " + dc.instance.FileToLoad;

            // release flag
            UpdateDisplayInCharge = false;
        }

    }
}
