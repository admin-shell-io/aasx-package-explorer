/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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

namespace AasxPackageExplorer
{
    public partial class HintBubble : UserControl
    {
        public HintBubble()
        {
            InitializeComponent();
        }

        public string Text
        {
            get
            {
                return innerbubble.Text;
            }
            set
            {
                innerbubble.Text = value;
            }
        }

        public new Brush Foreground
        {
            get { return innerbubble.Foreground; }
            set { innerbubble.Foreground = value; }
        }

        public new Brush Background
        {
            get { return innerbubble.Background; }
            set
            {
                innerbubble.Background = value;
                outerbubble.Fill = value;
            }
        }
    }
}
