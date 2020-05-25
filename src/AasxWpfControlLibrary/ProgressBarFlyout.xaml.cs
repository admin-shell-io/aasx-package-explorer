using AasxIntegrationBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für SelectFromRepository.xaml
    /// </summary>
    public partial class ProgressBarFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public bool Result = false;

        private Dictionary<Button, MessageBoxResult> buttonToResult = new Dictionary<Button, MessageBoxResult>();

        public ProgressBarFlyout(string caption, string info, MessageBoxImage image)
        {
            InitializeComponent();

            // texts
            this.LabelCaption.Content = caption;
            this.LabelInfo.Content = info;

            // image
            this.ImageIcon.Source = null;
            if (image == MessageBoxImage.Error)
                this.ImageIcon.Source = new BitmapImage(new Uri("/AasxWpfControlLibrary;component/Resources/msg_error.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Hand)
                this.ImageIcon.Source = new BitmapImage(new Uri("/AasxWpfControlLibrary;component/Resources/msg_hand.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Information)
                this.ImageIcon.Source = new BitmapImage(new Uri("/AasxWpfControlLibrary;component/Resources/msg_info.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Question)
                this.ImageIcon.Source = new BitmapImage(new Uri("/AasxWpfControlLibrary;component/Resources/msg_question.png", UriKind.RelativeOrAbsolute));
            if (image == MessageBoxImage.Warning)
                this.ImageIcon.Source = new BitmapImage(new Uri("/AasxWpfControlLibrary;component/Resources/msg_warning.png", UriKind.RelativeOrAbsolute));

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

        //
        // Mechanics
        //

        public string Info
        {
            get
            {
                return LabelInfo.Content as string;
            }
            set
            {
                LabelInfo.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => LabelInfo.Content = value));
            }
        }

        public double Progress
        {
            get
            {
                return TheProgressBar.Value;
            }
            set
            {
                TheProgressBar.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => TheProgressBar.Value = value));
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            ControlClosed?.Invoke();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

    }
}
