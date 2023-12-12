/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Newtonsoft.Json;
using WpfMtpControl;
using WpfMtpControl.DataSources;

namespace WpfMtpVisuViewer
{
    public partial class MainWindow : Window
    {
        public WpfMtpControl.MtpVisuOpcUaClient client = new WpfMtpControl.MtpVisuOpcUaClient();

        public AasOpcUaClient testOpcUaClient = null;

        public MainWindow()
        {
            // start
            InitializeComponent();

            // Timer for status
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            // ReSharper disable once RedundantDelegateCreation
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();

            // explicit
            //// textOpcUaClient = new AasOpcUaClient("opc.tcp://127.0.0.1:4840" /* "localhost:4840" */, 
            //// _autoAccept: true, _stopTimeout : 99, _userName: "", _password: "");
            //// textOpcUaClient.Run();
        }

        public void SetMessage(string fmt, params object[] args)
        {
            var st = string.Format(fmt, args);
            this.labelMessages.Text = st;
        }

        private int opcCounter = 0;

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (this.client == null)
                textBoxDataSourceStatus.Text = "(no OPC UA client enabled)";
            else
            {
                this.client.Tick(100);
                textBoxDataSourceStatus.Text = this.client.GetStatus();
            }


            // TODO (MIHO, 2020-09-18): remove this test code
            opcCounter++;
            if (testOpcUaClient != null && opcCounter % 20 == 0)
                try
                {
                    // ReSharper disable once UnusedVariable
                    var x = testOpcUaClient.ReadSubmodelElementValueAsString(
                        "|var|CODESYS Control Win V3.Application.SENSORS.L001.V", 2);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            if (testOpcUaClient != null && opcCounter % 100 == 0)
                try
                {
                    testOpcUaClient.Cancel();
                    testOpcUaClient.Close();
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
        }

        private WpfMtpControl.MtpData activeMtpData = null;

        private MtpDataSourceSubscriber activeSubscriber = null;

        private MtpSymbolMapRecordList hintsForConfigRecs = null;

        private void LoadFile(string fn)
        {
            if (!".aml .zip .mtp".Contains(System.IO.Path.GetExtension(fn.Trim().ToLower())))
                return;
            this.client = new WpfMtpControl.MtpVisuOpcUaClient();
            this.client.ItemChanged += Client_ItemChanged;
            this.activeSubscriber = new MtpDataSourceSubscriber();
            this.activeMtpData = new WpfMtpControl.MtpData();
            this.hintsForConfigRecs = new MtpSymbolMapRecordList();

            mtpVisu.VisuOptions = this.theOptions.VisuOptions;
            this.activeMtpData.LoadAmlOrMtp(activeVisualObjectLib, this.client, this.theOptions.PreLoadInfo,
                this.activeSubscriber, fn, makeUpConfigRecs: hintsForConfigRecs);
            if (this.activeMtpData.PictureCollection.Count > 0)
                mtpVisu.SetPicture(this.activeMtpData.PictureCollection.Values.ElementAt(0));
            mtpVisu.RedrawMtp();
            this.Title = "WPF MTP Viewer prototype - " + fn;
        }

        private void Client_ItemChanged(WpfMtpControl.DataSources.IMtpDataSourceStatus dataSource,
            MtpVisuOpcUaClient.DetailItem itemRef, MtpVisuOpcUaClient.ItemChangeType changeType)
        {
            if (dataSource == null || itemRef == null || itemRef.MtpSourceItemId == null
                || this.activeSubscriber == null)
                return;

            if (changeType == MtpVisuOpcUaClient.ItemChangeType.Value)
                this.activeSubscriber.Invoke(itemRef.MtpSourceItemId, MtpDataSourceSubscriber.ChangeType.Value,
                    itemRef.Value);
        }

        private WpfMtpControl.MtpSymbolLib theSymbolLib = null;

        private WpfMtpControl.MtpVisualObjectLib activeVisualObjectLib = null;

        // ReSharper disable once ClassNeverInstantiated.Global
        public class MtpViewerStandaloneOptions : AasxIntegrationBase.AasxPluginOptionsBase
        {
            public WpfMtpControl.MtpSymbolMapRecordList SymbolMappings = new WpfMtpControl.MtpSymbolMapRecordList();
            public MtpDataSourceOpcUaPreLoadInfo PreLoadInfo = new MtpDataSourceOpcUaPreLoadInfo();
            public MtpVisuOptions VisuOptions = new MtpVisuOptions();
        }

        private MtpViewerStandaloneOptions theOptions = null;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // start
            SetMessage("Application started.");

            // initialize symbol library
            this.theSymbolLib = new MtpSymbolLib();

            var ISO10628 = new ResourceDictionary();
            ISO10628.Source = new Uri(
                "pack://application:,,,/WpfMtpControl;component/Resources/PNID_DIN_EN_ISO_10628.xaml");
            this.theSymbolLib.ImportResourceDicrectory("PNID_ISO10628", ISO10628);

            var FESTO = new ResourceDictionary();
            FESTO.Source = new Uri(
                "pack://application:,,,/WpfMtpControl;component/Resources/PNID_Festo.xaml");
            this.theSymbolLib.ImportResourceDicrectory("PNID_Festo", FESTO);

            // initialize visual object libraries
            activeVisualObjectLib = new WpfMtpControl.MtpVisualObjectLib();
            activeVisualObjectLib.LoadStatic(this.theSymbolLib);

            // to find options
            this.theOptions = AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<MtpViewerStandaloneOptions>(
                "WpfMtpVisuViewer", Assembly.GetExecutingAssembly());
            if (this.theOptions != null && this.theOptions.SymbolMappings != null)
            {
                activeVisualObjectLib.LoadFromSymbolMappings(this.theSymbolLib, this.theOptions.SymbolMappings);
                SetMessage("Options loaded.");
            }

            // load file
            try
            {
                //// LoadFile("Dosing.mtp");
                //// LoadFile("Manifest_PxC_Dosing.aml");
                LoadFile("Manifest_V18.10.31_3_better_name_win3.aml");
                //// LoadFile("Manifest_Sten1.aml");
                //// LoadFile("Manifest_ChemiReaktor_MIHO.aml");

                SetMessage("MTP file loaded.");
            }
            catch (Exception ex)
            {
                SetMessage("Exception: {0}", ex.Message);
            }

            // fit it
            this.mtpVisu.ZoomToFitCanvas();

            // double click handler
            this.mtpVisu.MtpObjectDoubleClick += MtpVisu_MtpObjectDoubleClick;
        }

        private void MtpVisu_MtpObjectDoubleClick(MtpData.MtpBaseObject source)
        {
            if (source == null)
                return;
            SetMessage("DblClick name {0} RefID {1}", source.Name, source.RefID);
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                if (files != null && files.Length > 0)
                {
                    try
                    {
                        LoadFile(files[0]);
                    }
                    catch (Exception ex)
                    {
                        SetMessage("Exception: {0}", ex.Message);
                    }
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // fit it
            this.mtpVisu?.ZoomToFitCanvas();
        }

        private int overlayPanelMode = 0;

        private void SetOverlayPanelMode(int newMode)
        {
            this.overlayPanelMode = newMode;

            switch (this.overlayPanelMode)
            {
                case 2:
                    this.ScrollViewerDataSources.Visibility = Visibility.Visible;
                    DataGridDataSources.ItemsSource = this.client.Items;
                    this.RichTextReport.Visibility = Visibility.Collapsed;
                    break;

                case 1:
                    this.ScrollViewerDataSources.Visibility = Visibility.Collapsed;
                    DataGridDataSources.ItemsSource = null;
                    this.RichTextReport.Visibility = Visibility.Visible;
                    ReportOnConfiguration(this.RichTextReport);
                    break;

                default:
                    this.ScrollViewerDataSources.Visibility = Visibility.Collapsed;
                    DataGridDataSources.ItemsSource = null;
                    this.RichTextReport.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == buttonDataSourceDetails)
            {
                if (this.overlayPanelMode != 2)
                    SetOverlayPanelMode(2);
                else
                    SetOverlayPanelMode(0);
            }

            if (sender == buttonConfig)
            {
                if (this.overlayPanelMode != 1)
                    SetOverlayPanelMode(1);
                else
                    SetOverlayPanelMode(0);
            }
        }

        private void AddToRichTextBox(RichTextBox rtb, string text, bool bold = false, double? fontSize = null,
            bool monoSpaced = false)
        {
            var p = new Paragraph();
            if (bold)
                p.FontWeight = FontWeights.Bold;
            if (fontSize.HasValue)
                p.FontSize = fontSize.Value;
            if (monoSpaced)
                p.FontFamily = new FontFamily("Courier New");
            p.Inlines.Add(new Run(text));
            rtb.Document.Blocks.Add(p);
        }

        private void ReportOnConfiguration(RichTextBox rtb)
        {
            // access
            if (rtb == null)
                return;

            rtb.Document.Blocks.Clear();

            //
            // Report on available library symbols
            //

            if (this.theSymbolLib != null)
            {

                AddToRichTextBox(rtb, "Library symbols", bold: true, fontSize: 18);

                AddToRichTextBox(rtb, "The following lists shows available symbol full names.");

                foreach (var x in this.theSymbolLib.Values)
                {
                    AddToRichTextBox(rtb, "" + x.FullName, monoSpaced: true);
                }

                AddToRichTextBox(rtb, "");
            }

            //
            // Hints for configurations
            //

            if (this.hintsForConfigRecs != null)
            {
                AddToRichTextBox(rtb, "Preformatted configuration records", bold: true, fontSize: 18);
                AddToRichTextBox(rtb,
                    "The following JSON elements could be pasted into the options file named " + "" +
                    "'AasxPluginMtpViewer.options.json'. " +
                    "Prior to pasting, an appropriate symbol full name needs to be chosen from above list. " +
                    "For the eClass strings, multiples choices can be delimited by ';'. " +
                    "For EClassVersions, 'null' disables version checking. " +
                    "Either EClassClasses or EClassIRDIs shall be different to 'null'.");

                foreach (var x in this.hintsForConfigRecs)
                {
                    var txt = JsonConvert.SerializeObject(x, Formatting.None);
                    AddToRichTextBox(rtb, "" + txt, monoSpaced: true);
                }

                AddToRichTextBox(rtb, "");
            }
        }
    }
}
