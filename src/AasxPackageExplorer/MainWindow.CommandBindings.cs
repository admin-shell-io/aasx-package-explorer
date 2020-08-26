﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using AasxGlobalLogging;
using AasxIntegrationBase;
using AasxSignature;
using AasxUANodesetImExport;
using AdminShellNS;
using Jose;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains all command bindings, such as for the main menu, in order to reduce the
    /// comlexity of MainWindow.xaml.cs
    /// </summary>
    public partial class MainWindow : Window, IFlyoutProvider
    {
        private string lastFnForInitialDirectory = null;

        public void RememberForInitialDirectory(string fn)
        {
            this.lastFnForInitialDirectory = fn;
        }

        public string DetermineInitialDirectory(string existingFn = null)
        {
            // ReSharper disable EmptyGeneralCatchClause
            string res = null;

            if (existingFn != null)
                try
                {
                    res = System.IO.Path.GetDirectoryName(existingFn);
                }
                catch { }

            // may be can used last?
            if (res == null && lastFnForInitialDirectory != null)
                try
                {
                    res = System.IO.Path.GetDirectoryName(lastFnForInitialDirectory);
                }
                catch { }

            // ReSharper enable EmptyGeneralCatchClause
            return res;
        }


        private void CommandBinding_GeneralDispatch(string cmd)
        {
            if (cmd == "new")
            {
                if (MessageBoxResult.Yes == MessageBoxFlyoutShow(
                    "Create new Adminshell environment? This operation can not be reverted!", "AASX",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning))
                {
                    try
                    {
                        // clear
                        ClearAllViews();
                        // create new AASX package
                        thePackageEnv = new AdminShellPackageEnv();
                        // redraw everything
                        RedrawAllAasxElements();
                        RedrawElementView();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "When creating new AASX, an error occurred");
                        return;
                    }
                }
            }

            if (cmd == "open" || cmd == "openaux")
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(thePackageEnv.Filename);
                dlg.Filter =
                    "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|" +
                    "AAS JSON file (*.json)|*.json|All files (*.*)|*.*";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    if (cmd == "open")
                        UiLoadPackageWithNew(
                            ref thePackageEnv, new AdminShellPackageEnv(dlg.FileName), dlg.FileName,
                            onlyAuxiliary: false);
                    if (cmd == "openaux")
                        UiLoadPackageWithNew(
                            ref thePackageAux,
                            new AdminShellPackageEnv(dlg.FileName), dlg.FileName, onlyAuxiliary: true);
                }
            }

            if (cmd == "save")
            {
                try
                {
                    // save
                    thePackageEnv.SaveAs(thePackageEnv.Filename);
                    // backup
                    if (Options.Curr.BackupDir != null)
                    {
                        var fullfn = System.IO.Path.GetFullPath(Options.Curr.BackupDir);
                        thePackageEnv.BackupInDir(fullfn, Options.Curr.BackupFiles);
                    }
                    // as saving changes the structure of pending supplementary files, re-display
                    RedrawAllAasxElements();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "When saving AASX, an error occurred");
                    return;
                }
                Log.Info("AASX saved successfully: {0}", thePackageEnv.Filename);
            }

            if (cmd == "saveas")
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(thePackageEnv.Filename);
                dlg.FileName = thePackageEnv.Filename;
                dlg.DefaultExt = "*.aasx";
                dlg.Filter =
                    "AASX package files (*.aasx)|*.aasx|AASX package files w/ JSON (*.aasx)|*.aasx|" +
                    "AAS XML file (*.xml)|*.xml|AAS JSON file (*.json)|*.json|All files (*.*)|*.*";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
                if (res == true)
                {
                    try
                    {
                        // preferred format
                        var prefFmt = AdminShellPackageEnv.PreferredFormat.None;
                        if (dlg.FilterIndex == 2)
                            prefFmt = AdminShellPackageEnv.PreferredFormat.Json;
                        // save
                        RememberForInitialDirectory(dlg.FileName);
                        thePackageEnv.SaveAs(dlg.FileName, prefFmt: prefFmt);
                        // backup
                        if (Options.Curr.BackupDir != null)
                        {
                            var fullfn = System.IO.Path.GetFullPath(Options.Curr.BackupDir);
                            thePackageEnv.BackupInDir(fullfn, Options.Curr.BackupFiles);
                        }
                        // as saving changes the structure of pending supplementary files, re-display
                        RedrawAllAasxElements();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "When saving AASX, an error occurred");
                        return;
                    }
                    Log.Info("AASX saved successfully as: {0}", dlg.FileName);
                }
            }

            if (cmd == "close" && thePackageEnv != null)
            {
                if (MessageBoxResult.Yes == MessageBoxFlyoutShow(
                    "Do you want to close the open package? Please make sure that you have saved before.",
                    "Close Package?", MessageBoxButton.YesNo, MessageBoxImage.Question))
                    try
                    {
                        thePackageEnv.Close();
                        RedrawAllAasxElements();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "When closing AASX, an error occurred");
                    }
            }

            if ((cmd == "sign" || cmd == "validate" || cmd == "encrypt") && thePackageEnv != null)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "AASX package files (*.aasx)|*.aasx";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res == true)
                {
                    if (cmd == "sign")
                    {
                        PackageHelper.SignAll(dlg.FileName);
                    }
                    if (cmd == "validate")
                    {
                        PackageHelper.Validate(dlg.FileName);
                    }
                    if (cmd == "encrypt")
                    {
                        var dlg2 = new Microsoft.Win32.OpenFileDialog();
                        dlg2.Filter = ".cer files (*.cer)|*.cer";
                        if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                        res = dlg2.ShowDialog();
                        if (Options.Curr.UseFlyovers) this.CloseFlyover();

                        if (res == true)
                        {
                            try
                            {
                                X509Certificate2 x509 = new X509Certificate2(dlg2.FileName);
                                var publicKey = x509.GetRSAPublicKey();

                                Byte[] binaryFile = File.ReadAllBytes(dlg.FileName);
                                string binaryBase64 = Convert.ToBase64String(binaryFile);

                                string payload = "{ \"file\" : \" " + binaryBase64 + " \" }";

                                string fileToken = Jose.JWT.Encode(
                                    payload, publicKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);
                                Byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileToken);

                                var dlg3 = new Microsoft.Win32.SaveFileDialog();
                                dlg3.Filter = "AASX2 package files (*.aasx2)|*.aasx2";
                                dlg3.FileName = dlg.FileName + "2";
                                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                                res = dlg3.ShowDialog();
                                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                                if (res == true)
                                {
                                    File.WriteAllBytes(dlg3.FileName, fileBytes);
                                }
                            }
                            catch
                            {
                                System.Windows.MessageBox.Show(
                                    this, "Can not encrypt with " + dlg2.FileName, "Decrypt .AASX2",
                                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                    }
                }
            }
            if ((cmd == "decrypt") && thePackageEnv != null)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "AASX package files (*.aasx2)|*.aasx2";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res == true)
                {
                    if (cmd == "decrypt")
                    {
                        var dlg2 = new Microsoft.Win32.OpenFileDialog();
                        dlg2.Filter = ".pfx files (*.pfx)|*.pfx";
                        if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                        res = dlg2.ShowDialog();
                        if (Options.Curr.UseFlyovers) this.CloseFlyover();

                        if (res == true)
                        {
                            try
                            {
                                X509Certificate2 x509 = new X509Certificate2(dlg2.FileName, "i40");
                                var privateKey = x509.GetRSAPrivateKey();

                                Byte[] binaryFile = File.ReadAllBytes(dlg.FileName);
                                string fileString = System.Text.Encoding.UTF8.GetString(binaryFile);

                                string fileString2 = Jose.JWT.Decode(
                                    fileString, privateKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256CBC_HS512);

                                var parsed0 = JObject.Parse(fileString2);
                                string binaryBase64_2 = parsed0.SelectToken("file").Value<string>();

                                Byte[] fileBytes2 = Convert.FromBase64String(binaryBase64_2);

                                var dlg4 = new Microsoft.Win32.SaveFileDialog();
                                dlg4.Filter = "AASX package files (*.aasx)|*.aasx";
                                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                                res = dlg4.ShowDialog();
                                if (Options.Curr.UseFlyovers) this.CloseFlyover();
                                if (res == true)
                                {
                                    File.WriteAllBytes(dlg4.FileName, fileBytes2);
                                }
                            }
                            catch
                            {
                                System.Windows.MessageBox.Show(
                                    this, "Can not decrypt with " + dlg2.FileName, "Decrypt .AASX2",
                                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                    }
                }
            }

            if (cmd == "closeaux" && thePackageAux != null)
                try
                {
                    thePackageAux.Close();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "When closing auxiliary AASX, an error occurred");
                }

            if (cmd == "exit")
                System.Windows.Application.Current.Shutdown();

            if (cmd == "connectopcua")
                MessageBoxFlyoutShow(
                    "In future versions, this feature will allow connecting to an online Administration Shell " +
                    "via OPC UA or similar.",
                    "Connect", MessageBoxButton.OK, MessageBoxImage.Hand);

            if (cmd == "about")
            {
                var ab = new AboutBox();
                ab.ShowDialog();
            }

            if (cmd == "helpgithub")
            {
                ShowHelp();
            }

            if (cmd == "editkey")
                MenuItemWorkspaceEdit.IsChecked = !MenuItemWorkspaceEdit.IsChecked;

            if (cmd == "hintskey")
                MenuItemWorkspaceHints.IsChecked = !MenuItemWorkspaceHints.IsChecked;

            if (cmd == "editmenu" || cmd == "editkey" || cmd == "hintsmenu" || cmd == "hintskey")
            {
                // try to remember current selected data object
                object currMdo = null;
                if (DisplayElements.SelectedItem != null)
                    currMdo = DisplayElements.SelectedItem.GetMainDataObject();

                // edit mode affects the total element view
                RedrawAllAasxElements();
                // fake selection
                RedrawElementView();
                // select last object
                if (currMdo != null)
                    DisplayElements.TrySelectMainDataObject(currMdo, wishExpanded: true);
            }

            if (cmd == "test")
            {
                DisplayElements.Test();
            }

            if (cmd == "queryrepo")
                CommandBinding_QueryRepo();

            if (cmd == "genrepo")
                CommandBinding_GenerateRepo();

            if (cmd == "printrepo")
                CommandBinding_PrintRepo();

            if (cmd == "printasset")
                CommandBinding_PrintAsset();

            if (cmd == "opcread")
                CommandBinding_OpcUaClientRead();

            if (cmd == "submodelread")
                CommandBinding_SubmodelRead();

            if (cmd == "submodelwrite")
                CommandBinding_SubmodelWrite();

            if (cmd == "submodelput")
                CommandBinding_SubmodelPut();

            if (cmd == "submodelget")
                CommandBinding_SubmodelGet();

            if (cmd == "bmecatimport")

                CommandBinding_BMEcatImport();

            if (cmd == "csvimport")
                CommandBinding_CSVImport();

            if (cmd == "opcuaimportnodeset")
                CommandBinding_OpcUaImportNodeSet();

            if (cmd == "importsubmodel")
                CommandBinding_ImportSubmodel();

            if (cmd == "importaml")
                CommandBinding_ImportAML();

            if (cmd == "exportaml")
                CommandBinding_ExportAML();

            if (cmd == "opcuai4aasexport")
                CommandBinding_ExportOPCUANodeSet();

            if (cmd == "opcuai4aasimport")
                CommandBinding_ImportOPCUANodeSet();

            if (cmd == "opcuaexportnodesetuaplugin")
                CommandBinding_ExportNodesetUaPlugin();

            if (cmd == "serverrest")
                CommandBinding_ServerRest();

            if (cmd == "mqttpub")
                CommandBinding_MQTTPub();

            if (cmd == "connectsecure")
                CommandBinding_ConnectSecure();

            if (cmd == "connectrest")
                CommandBinding_ConnectRest();

            if (cmd == "copyclipboardelementjson")
                CommandBinding_CopyClipboardElementJson();

            if (cmd == "exportgenericforms")
                CommandBinding_ExportGenericForms();

            if (cmd == "exportpredefineconcepts")
                CommandBinding_ExportPredefineConcepts();

            if (cmd == "exporttable")
                CommandBinding_ExportTable();

            if (cmd == "serverpluginemptysample")
                CommandBinding_ExecutePluginServer(
                    "EmptySample", "server-start", "server-stop", "Empty sample plug-in.");

            if (cmd == "serverpluginopcua")
                CommandBinding_ExecutePluginServer(
                    "Net46AasxServerPlugin", "server-start", "server-stop", "Plug-in for OPC UA Server for AASX.");

            if (cmd == "serverpluginmqtt")
                CommandBinding_ExecutePluginServer(
                    "AasxPluginMqttServer", "MQTTServer-start", "server-stop", "Plug-in for MQTT Server for AASX.");

            if (cmd == "newsubmodelfromplugin")
                CommandBinding_NewSubmodelFromPlugin();

            if (cmd == "convertelement")
                CommandBinding_ConvertElement();

            if (cmd == "toolsfindtext" || cmd == "toolsfindforward" || cmd == "toolsfindbackward")
                CommandBinding_ToolsFind(cmd);
        }

        public void CommandBinding_ConnectSecure()
        {
            // make dialgue flyout
            var uc = new SecureConnectFlyout();
            uc.LoadPresets(Options.Curr.SecureConnectPresets);

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
            });

            // succss?
            if (uc.Result == null)
                return;
            var preset = uc.Result;

            // make listing flyout
            var logger = new LogInstance();
            var uc2 = new LogMessageFlyout("Secure connecting ..", "Start secure connect ..", () =>
            {
                return logger.PopLastShortTermPrint();
            });
            uc2.EnableLargeScreen();

            // do some statistics
            Log.Info("Start secure connect ..");
            Log.Info("Protocol: {0}", preset.Protocol.Value);
            Log.Info("AuthorizationServer: {0}", preset.AuthorizationServer.Value);
            Log.Info("AasServer: {0}", preset.AasServer.Value);
            Log.Info("CertificateFile: {0}", preset.CertificateFile.Value);
            Log.Info("Password: {0}", preset.Password.Value);

            logger.Info("Protocol: {0}", preset.Protocol.Value);
            logger.Info("AuthorizationServer: {0}", preset.AuthorizationServer.Value);
            logger.Info("AasServer: {0}", preset.AasServer.Value);
            logger.Info("CertificateFile: {0}", preset.CertificateFile.Value);
            logger.Info("Password: {0}", preset.Password.Value);

            // start CONNECT as a worker (will start in the background)
            var worker = new BackgroundWorker();
            AdminShellPackageEnv envToload = null;
            worker.DoWork += (s1, e1) =>
            {
                for (int i = 0; i < 15; i++)
                {
                    var sb = new StringBuilder();
                    for (double j = 0; j < 1; j += 0.0025)
                        sb.Append($"{j}");
                    logger.Info("The output is: {0} gives {1} was {0}", i, sb.ToString());
                    logger.Info(0, StoredPrint.ColorBlue, "This is blue");
                    logger.Info(0, StoredPrint.ColorRed, "This is red");
                    logger.Error("This is an error!");
                    logger.InfoWithHyperlink(0, "This is an link", "(Link)", "https://www.google.de");
                    logger.Info("----");
                    Thread.Sleep(2134);
                }

                envToload = null;
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc2, closingAction: () =>
            {
                // clean up
            });

            // commit Package
            if (envToload != null)
            {
            }

            // done
            Log.Info("Secure connect done.");
        }

        public void CommandBinding_QueryRepo()
        {
            var uc = new SelectFromRepositoryFlyout();
            uc.Margin = new Thickness(10);
            var fullfn = System.IO.Path.GetFullPath(Options.Curr.AasxRepositoryFn);
            if (uc.LoadAasxRepoFile(fullfn))
            {
                uc.ControlClosed += () =>
                {
                    var fn = uc.ResultFilename;
                    if (fn != null && fn != "")
                    {
                        Log.Info("Switching to {0} ..", fn);
                        UiLoadPackageWithNew(
                            ref thePackageEnv, new AdminShellPackageEnv(fn), fn, onlyAuxiliary: false);
                    }

                };
                this.StartFlyover(uc);
            }
        }

        public void CommandBinding_GenerateRepo()
        {
            // get the input files
            var inputDlg = new Microsoft.Win32.OpenFileDialog();
            inputDlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            inputDlg.Title = "Multi-select AASX package files to be in repository";
            inputDlg.Filter = "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|All files (*.*)|*.*";
            inputDlg.Multiselect = true;

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = inputDlg.ShowDialog();
            if (Options.Curr.UseFlyovers) this.CloseFlyover();

            if (res != true || inputDlg.FileNames.Length < 1)
                return;

            RememberForInitialDirectory(inputDlg.FileName);

            // get the output file
            var exFn = System.AppDomain.CurrentDomain.BaseDirectory;
            if (inputDlg.FileNames.Length > 0)
                exFn = inputDlg.FileNames[0];

            var outputDlg = new Microsoft.Win32.SaveFileDialog();
            outputDlg.InitialDirectory = DetermineInitialDirectory(exFn);
            outputDlg.Title = "Select AASX repository to be generated";
            outputDlg.FileName = "new-aasx-repo.json";
            outputDlg.DefaultExt = "*.json";
            outputDlg.Filter = "AASX repository files (*.json)|*.json|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            res = outputDlg.ShowDialog();
            if (Options.Curr.UseFlyovers) this.CloseFlyover();

            if (res != true)
                return;

            RememberForInitialDirectory(outputDlg.FileName);

            // ok
            try
            {
                AasxFileRepository.GenerateRepositoryFromFileNames(inputDlg.FileNames, outputDlg.FileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When printing, an error occurred");
            }
        }

        public void CommandBinding_PrintRepo()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select AASX repository to be printed";
            dlg.Filter = "AASX repository files (*.json)|*.json|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    AasxPrintFunctions.PrintRepositoryCodeSheet(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When printing, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_PrintAsset()
        {
            AdminShell.Asset asset = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementAsset)
            {
                var ve = DisplayElements.SelectedItem as VisualElementAsset;
                if (ve != null && ve.theAsset != null)
                    asset = ve.theAsset;
            }

            if (asset == null)
            {
                MessageBoxFlyoutShow(
                    "No asset selected for printing code sheet.", "Print code sheet",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            try
            {
                if (asset.identification != null)
                {
                    AasxPrintFunctions.PrintSingleAssetCodeSheet(asset.identification.id, asset.idShort);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When printing, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ServerRest()
        {
            // make a logger
            var logger = new AasxRestServerLibrary.GrapevineLoggerToListOfStrings();

            // make listing flyout
            var uc = new LogMessageFlyout("AASX REST Server", "Starting REST server ..", () =>
            {
                var st = logger.Pop();
                return (st == null) ? null : new StoredPrint(st);
            });

            // start REST as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.DoWork += (s1, e1) =>
            {
                AasxRestServerLibrary.AasxRestServer.Start(
                    this.thePackageEnv, Options.Curr.RestServerHost, Options.Curr.RestServerPort, logger);
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
                AasxRestServerLibrary.AasxRestServer.Stop();
            });
        }

        public void CommandBinding_MQTTPub()
        {
            // make a logger
            var logger = new AasxMqttClient.GrapevineLoggerToListOfStrings();

            // make listing flyout
            var uc = new LogMessageFlyout("AASX MQTT Publisher", "Starting MQTT Client ..", () =>
            {
                var st = logger.Pop();
                return (st == null) ? null : new StoredPrint(st);
            });

            // start MQTT Client as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.DoWork += async (s1, e1) =>
            {
                try
                {
                    await AasxMqttClient.MqttClient.StartAsync(this.thePackageEnv, logger);
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () => { });
        }

        public async void CommandBinding_ConnectRest()
        {
            var uc = new TextBoxFlyout("REST server adress:", MessageBoxImage.Question);
            uc.Text = "http://" + Options.Curr.RestServerHost + ":" + Options.Curr.RestServerPort;
            this.StartFlyoverModal(uc);
            if (uc.Result)
            {
                string input = uc.Text.ToLower();
                string tag = "openid1";
                tag = input.Substring(0, tag.Length);
                if (tag == "openid1" || tag == "openid2" || tag == "openid3")
                {
                    string value = input.Substring(tag.Length + 1);
                    if (thePackageEnv.IsOpen)
                    {
                        thePackageEnv.Close();
                    }
                    await AasxOpenIdClient.OpenIDClient.Run(tag, value);

                    UiLoadPackageWithNew(
                        ref thePackageEnv,
                        new AdminShellPackageEnv(AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx"),
                        AasxOpenIdClient.OpenIDClient.outputDir + "\\download.aasx", onlyAuxiliary: false);
                    return;
                }

                var url = uc.Text;
                Log.Info($"Connecting to REST server {url} ..");

                try
                {
                    var client = new AasxRestServerLibrary.AasxRestClient(url);
                    theOnlineConnection = client;
                    var pe = client.OpenPackageByAasEnv();
                    if (pe != null)
                        UiLoadPackageWithNew(ref thePackageEnv, pe, uc.Text, onlyAuxiliary: false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Connecting to REST server {url}");
                }
            }
        }

        public void CommandBinding_BMEcatImport()
        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected for BMEcat information.", "BMEcat import",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(thePackageEnv.Filename);
            dlg.Filter = "BMEcat XML files (*.bmecat)|*.bmecat|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    BMEcatTools.ImportBMEcatToSubModel(dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "When importing BMEcat, an error occurred");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_CSVImport()
        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "CSV import", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(thePackageEnv.Filename);
            dlg.Filter = "CSV files (*.CSV)|*.csv|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    CSVTools.ImportCSVtoSubModel(dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "When importing CSV, an error occurred");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_OpcUaImportNodeSet()
        {
            VisualElementSubmodelRef ve = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve == null || ve.theSubmodel == null || ve.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(thePackageEnv.Filename);
            dlg.Filter = "OPC UA NodeSet XML files (*.XML)|*.XML|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
                try
                {
                    // do it
                    RememberForInitialDirectory(dlg.FileName);
                    OpcUaTools.ImportNodeSetToSubModel(dlg.FileName, ve.theEnv, ve.theSubmodel, ve.theSubmodelRef);
                    // redisplay
                    RedrawAllAasxElements();
                    RedrawElementView();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "When importing, an error occurred");
                }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        private void CommandBinding_ExecutePluginServer(
            string pluginName, string actionName, string stopName, string caption, string[] additionalArgs = null)
        {
            // check
            var pi = Plugins.FindPluginInstance(pluginName);
            if (pi == null || !pi.HasAction(actionName) || !pi.HasAction(stopName))
            {
                var res = MessageBoxFlyoutShow(
                        $"This function requires a binary plug-in file named '{pluginName}', " +
                        $"which needs to be added to the command line, with an action named '{actionName}'. " +
                        "Press 'OK' to show help page on GitHub.",
                        "Plug-in not present",
                        MessageBoxButton.OKCancel, MessageBoxImage.Hand);
                if (res == MessageBoxResult.OK)
                {
                    ShowHelp();
                }
                return;
            }

            // activate server via plugin
            // make listing flyout
            var uc = new LogMessageFlyout(caption, $"Starting plug-in {pluginName}, action {actionName} ..", () =>
            {
                return this.FlyoutLoggingPop();
            });

            this.FlyoutLoggingStart();

            uc.ControlCloseWarnTime = 10000;
            uc.ControlWillBeClosed += () =>
            {
                uc.LogMessage("Initiating closing (wait at max 10sec) ..");
                pi.InvokeAction(stopName);
            };
            uc.AddPatternError(new Regex(@"^\[1\]"));

            // start server as a worker (will start in the background)
            var worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (s1, e1) =>
            {
                try
                {
                    // total argument list
                    var totalArgs = new List<string>();
                    if (pi.args != null)
                        totalArgs.AddRange(pi.args);
                    if (additionalArgs != null)
                        totalArgs.AddRange(additionalArgs);

                    // invoke
                    pi.InvokeAction(actionName, this.thePackageEnv, totalArgs.ToArray());

                }
                catch (Exception ex)
                {
                    uc.LogMessage("Exception in plug-in: " + ex.Message + " in " + ex.StackTrace);
                    uc.LogMessage("Stopping...");
                    Thread.Sleep(5000);
                }
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
                // in any case, close flyover
                this.FlyoutLoggingStop();
                uc.LogMessage("Completed.");
                uc.CloseControlExplicit();
            };
            worker.RunWorkerAsync();

            // modal dialogue
            this.StartFlyoverModal(uc, closingAction: () =>
            {
#if FALSE
                if (false && worker.IsBusy)
                    try
                    {
                        worker.CancelAsync();
                        worker.Dispose();
                    }
                    catch { }
#endif
            });
        }

        public void CommandBinding_SubmodelWrite()
        {
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "Submodel Write", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var obj = ve1.theSubmodel;

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(thePackageEnv.Filename);
            dlg.FileName = "Submodel_" + obj.idShort + ".json";
            dlg.Filter = "JSON files (*.JSON)|*.json|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
            {
                RememberForInitialDirectory(dlg.FileName);
                using (var s = new StreamWriter(dlg.FileName))
                {
                    var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    s.WriteLine(json);
                }
            }
            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_SubmodelRead()
        {
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "Submodel Read", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var obj = ve1.theSubmodel;

            // ok!
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.FileName = "Submodel_" + obj.idShort + ".json";
            dlg.Filter = "JSON files (*.JSON)|*.json|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();

            if (res == true)
            {
                var aas = thePackageEnv.AasEnv.FindAASwithSubmodel(obj.identification);

                // de-serialize Submodel
                AdminShell.Submodel submodel = null;

                try
                {
                    RememberForInitialDirectory(dlg.FileName);
                    using (StreamReader file = System.IO.File.OpenText(dlg.FileName))
                    {
                        ITraceWriter tw = new MemoryTraceWriter();
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.TraceWriter = tw;
                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        submodel = (AdminShell.Submodel)serializer.Deserialize(file, typeof(AdminShell.Submodel));
                    }
                }
                catch (Exception)
                {
                    MessageBoxFlyoutShow(
                        "Can not read SubModel.", "Submodel Read", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // need id for idempotent behaviour
                if (submodel == null || submodel.identification == null)
                {
                    MessageBoxFlyoutShow(
                        "Identification of SubModel is (null).", "Submodel Read",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // datastructure update
                if (thePackageEnv == null || thePackageEnv.AasEnv == null || thePackageEnv.AasEnv.Assets == null)
                {
                    MessageBoxFlyoutShow(
                        "Error accessing internal data structures.", "Submodel Read",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // add Submodel
                var existingSm = thePackageEnv.AasEnv.FindSubmodel(submodel.identification);
                if (existingSm != null)
                    thePackageEnv.AasEnv.Submodels.Remove(existingSm);
                thePackageEnv.AasEnv.Submodels.Add(submodel);

                // add SubmodelRef to AAS
                // access the AAS
                var newsmr = AdminShell.SubmodelRef.CreateNew(
                    "Submodel", true, submodel.identification.idType, submodel.identification.id);
                var existsmr = aas.HasSubmodelRef(newsmr);
                if (!existsmr)
                {
                    aas.AddSubmodelRef(newsmr);
                }
                RedrawAllAasxElements();
                RedrawElementView();
            }
            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        static string PUTURL = "http://???:51310";

        public void CommandBinding_SubmodelPut()
        {
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "PUT Submodel", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var input = new TextBoxFlyout("REST server adress:", MessageBoxImage.Question);
            input.Text = PUTURL;
            this.StartFlyoverModal(input);
            if (!input.Result)
            {
                return;
            }
            PUTURL = input.Text;
            Log.Info($"Connecting to REST server {PUTURL} ..");

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "PUT Submodel", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var obj = ve1.theSubmodel;
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);

            try
            {
                var client = new AasxRestServerLibrary.AasxRestClient(PUTURL);
                client.PutSubmodel(json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Connecting to REST server {PUTURL}");
            }

#if FALSE
            // ok!
#pragma warning disable 0162
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(thePackageEnv.Filename);
            dlg.FileName = "Submodel_" + obj.idShort + ".json";
            dlg.Filter = "JSON files (*.JSON)|*.json|All files (*.*)|*.*";
            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog();
            if (res == true)
            {
                RememberForInitialDirectory(dlg.FileName);
                using (var s = new StreamWriter(dlg.FileName))
                {
                    // var settings = new JsonSerializerSettings();
                    var json2 = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    s.WriteLine(json2);
                }
            }
            if (Options.Curr.UseFlyovers) this.CloseFlyover();
#pragma warning restore 0162
#endif
        }

        static string GETURL = "http://???:51310";

        public void CommandBinding_SubmodelGet()
        {
            VisualElementSubmodelRef ve1 = null;

            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected.", "GET Submodel", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var input = new TextBoxFlyout("REST server adress:", MessageBoxImage.Question);
            input.Text = GETURL;
            this.StartFlyoverModal(input);
            if (!input.Result)
            {
                return;
            }
            GETURL = input.Text;
            Log.Info($"Connecting to REST server {GETURL} ..");

            var obj = ve1.theSubmodel;
            var sm = "";
            try
            {
                var client = new AasxRestServerLibrary.AasxRestClient(GETURL);
                sm = client.GetSubmodel(obj.idShort);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Connecting to REST server {GETURL}");
            }

            {
                var aas = thePackageEnv.AasEnv.FindAASwithSubmodel(obj.identification);

                // de-serialize Submodel
                AdminShell.Submodel submodel = null;

                try
                {
                    using (TextReader reader = new StringReader(sm))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                        submodel = (AdminShell.Submodel)serializer.Deserialize(reader, typeof(AdminShell.Submodel));
                    }
                }
                catch (Exception)
                {
                    MessageBoxFlyoutShow(
                        "Can not read SubModel.", "Submodel Read", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // need id for idempotent behaviour
                if (submodel == null || submodel.identification == null)
                {
                    MessageBoxFlyoutShow(
                        "Identification of SubModel is (null).", "Submodel Read",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // datastructure update
                if (thePackageEnv == null || thePackageEnv.AasEnv == null || thePackageEnv.AasEnv.Assets == null)
                {
                    MessageBoxFlyoutShow(
                        "Error accessing internal data structures.", "Submodel Read",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // add Submodel
                var existingSm = thePackageEnv.AasEnv.FindSubmodel(submodel.identification);
                if (existingSm != null)
                    thePackageEnv.AasEnv.Submodels.Remove(existingSm);
                thePackageEnv.AasEnv.Submodels.Add(submodel);

                // add SubmodelRef to AAS
                // access the AAS
                var newsmr = AdminShell.SubmodelRef.CreateNew(
                    "Submodel", true, submodel.identification.idType, submodel.identification.id);
                var existsmr = aas.HasSubmodelRef(newsmr);
                if (!existsmr)
                {
                    aas.AddSubmodelRef(newsmr);
                }
                RedrawAllAasxElements();
                RedrawElementView();
            }
        }

        public void CommandBinding_OpcUaClientRead()
        {
            // OZ
            {
                VisualElementSubmodelRef ve1 = null;
                if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                    ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

                if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
                {
                    MessageBoxFlyoutShow(
                        "No valid SubModel selected for OPC import.", "OPC import",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {

                    // Durch das Submodel iterieren
                    {
                        int count = ve1.theSubmodel.qualifiers.Count;
                        if (count != 0)
                        {
                            int stopTimeout = Timeout.Infinite;
                            bool autoAccept = true;
                            // Variablen aus AAS Qualifiern
                            string Username = "";
                            string Password = "";
                            string URL = "";
                            int Namespace = 0;
                            string Path = "";

                            int i = 0;


                            while (i < 5 && i < count) // URL, Username, Password, Namespace, Path
                            {
                                var p = ve1.theSubmodel.qualifiers[i];

                                switch (i)
                                {
                                    case 0: // URL
                                        if (p.type == "OPCURL")
                                        {
                                            URL = p.value;
                                        }
                                        break;
                                    case 1: // Username
                                        if (p.type == "OPCUsername")
                                        {
                                            Username = p.value;
                                        }
                                        break;
                                    case 2: // Password
                                        if (p.type == "OPCPassword")
                                        {
                                            Password = p.value;
                                        }
                                        break;
                                    case 3: // Namespace
                                        if (p.type == "OPCNamespace")
                                        {
                                            Namespace = int.Parse(p.value);
                                        }
                                        break;
                                    case 4: // Path
                                        if (p.type == "OPCPath")
                                        {
                                            Path = p.value;
                                        }
                                        break;
                                }
                                i++;
                            }

                            if (URL == "" || Username == "" || Password == "" || Namespace == 0 || Path == "")
                            {
                                return;
                            }

                            // find OPC plug-in
                            var pi = Plugins.FindPluginInstance("AasxPluginOpcUaClient");
                            if (pi == null || !pi.HasAction("create-client") || !pi.HasAction("read-sme-value"))
                            {
                                Log.Error(
                                    "No plug-in 'AasxPluginOpcUaClient' with appropriate " +
                                    "actions 'create-client()', 'read-sme-value()' found.");
                                return;
                            }

                            // create client
                            // ReSharper disable ConditionIsAlwaysTrueOrFalse
                            var resClient =
                                pi.InvokeAction(
                                    "create-client", URL, autoAccept, stopTimeout,
                                    Username, Password) as AasxPluginResultBaseObject;
                            // ReSharper enable ConditionIsAlwaysTrueOrFalse
                            if (resClient == null || resClient.obj == null)
                            {
                                Log.Error("Plug-in 'AasxPluginOpcUaClient' cannot create client access!");
                                return;
                            }

                            // over all SMEs
                            count = ve1.theSubmodel.submodelElements.Count;
                            i = 0;
                            while (i < count)
                            {
                                if (ve1.theSubmodel.submodelElements[i].submodelElement is AdminShell.Property)
                                {
                                    // access data
                                    var p = ve1.theSubmodel.submodelElements[i].submodelElement as AdminShell.Property;
                                    var nodeName = "" + Path + p?.idShort;

                                    // do read() via plug-in
                                    var resValue = pi.InvokeAction(
                                        "read-sme-value", resClient.obj,
                                        nodeName, Namespace) as AasxPluginResultBaseObject;

                                    // set?
                                    if (resValue != null && resValue.obj != null && resValue.obj is string)
                                    {
                                        var value = (string)resValue.obj;
                                        p?.Set(p.valueType, value);
                                    }
                                }
                                i++;
                            }
                        }

                        RedrawAllAasxElements();
                        RedrawElementView();
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "executing OPC UA client");
                }
            }

        }

        public void CommandBinding_ImportSubmodel()
        {
            VisualElementAdminShell ve = null;
            if (DisplayElements.SelectedItem != null)
            {
                if (DisplayElements.SelectedItem is VisualElementAdminShell)
                {
                    ve = DisplayElements.SelectedItem as VisualElementAdminShell;
                }
                else
                {
                    MessageBoxFlyoutShow("Please select the administration shell for the submodel import.",
                        "Submodel Import", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var dataChanged = false;
            try
            {
                if (ve != null && ve.theEnv != null && ve.theAas != null)
                    dataChanged = AasxDictionaryImport.Import.ImportSubmodel(ve.theEnv, ve.theAas);
                else
                    dataChanged = AasxDictionaryImport.Import.ImportSubmodel(thePackageEnv.AasEnv);
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred during the submodel import.");
            }

            if (dataChanged)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                RestartUIafterNewPackage();
                Mouse.OverrideCursor = null;
            }
        }

        public void CommandBinding_ImportAML()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select AML file to be imported";
            dlg.Filter = "AutomationML files (*.aml)|*.aml|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    AasxAmlImExport.AmlImport.ImportInto(this.thePackageEnv, dlg.FileName);
                    this.RestartUIafterNewPackage();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When importing AML, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ExportAML()
        {
            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select AML file to be exported";
            dlg.FileName = "new.aml";
            dlg.DefaultExt = "*.aml";
            dlg.Filter =
                "AutomationML files (*.aml)|*.aml|AutomationML files (*.aml) (compact)|" +
                "*.aml|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    AasxAmlImExport.AmlExport.ExportTo(
                        this.thePackageEnv, dlg.FileName, tryUseCompactProperties: dlg.FilterIndex == 2);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When exporting AML, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ExportNodesetUaPlugin()
        {
            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select Nodeset2.XML file to be exported";
            dlg.FileName = "new.xml";
            dlg.DefaultExt = "*.xml";
            dlg.Filter = "OPC UA Nodeset2 files (*.xml)|*.xml|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    CommandBinding_ExecutePluginServer(
                        "Net46AasxServerPlugin",
                        "server-start",
                        "server-stop",
                        "Export Nodeset2 via OPC UA Server...",
                        new[] { "-export-nodeset", dlg.FileName }
                        );
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When exporting UA nodeset via plug-in, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_CopyClipboardElementJson()
        {
            // get the selected element
            var ve = DisplayElements.SelectedItem;

            // allow only some elements
            if (!(ve is VisualElementConceptDescription
                || ve is VisualElementSubmodelElement
                || ve is VisualElementAdminShell
                || ve is VisualElementAsset
                || ve is VisualElementOperationVariable
                || ve is VisualElementReference
                || ve is VisualElementSubmodel
                || ve is VisualElementSubmodelRef
                || ve is VisualElementView))
                ve = null;

            // need to have business object
            var mdo = ve?.GetMainDataObject();

            if (ve == null || mdo == null)
            {
                MessageBoxFlyoutShow(
                    "No valid element selected.", "Copy selected elements",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ok, for Serialization we just want the plain element with no BLOBs..
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new AdminShellConverters.AdaptiveFilterContractResolver(
                deep: false, complete: false);
            var jsonStr = JsonConvert.SerializeObject(mdo, Formatting.Indented, settings);

            // copy to clipboard
            if (jsonStr != null && jsonStr != "")
            {
                System.Windows.Clipboard.SetText(jsonStr);
                Log.Info("Copied selected element to clipboard.");
            }
            else
            {
                Log.Info("No JSON text could be generated for selected element.");
            }
        }

        public void CommandBinding_ExportGenericForms()
        {
            // trivial things
            if (this.thePackageEnv == null || !this.thePackageEnv.IsOpen)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            // a SubmodelRef shall be exported
            VisualElementSubmodelRef ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected for exporting options file for GenericForms.", "Generic Forms",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select options file for GenericForms to be exported";
            dlg.FileName = "new.add-options.json";
            dlg.DefaultExt = "*.add-options.json";
            dlg.Filter = "options file for GenericForms (*.add-options.json)|*.add-options.json|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    Log.Info("Exporting add-options file to GenericForm: {0}", dlg.FileName);
                    RememberForInitialDirectory(dlg.FileName);
                    AasxIntegrationBase.AasForms.AasFormUtils.ExportAsGenericFormsOptions(
                        ve1.theEnv, ve1.theSubmodel, dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When exporting options file for GenericForms, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ExportPredefineConcepts()
        {
            // trivial things
            if (this.thePackageEnv == null || !this.thePackageEnv.IsOpen)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            // a SubmodelRef shall be exported
            VisualElementSubmodelRef ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected for exporting snippets.", "Snippets for PredefinedConcepts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.Title = "Select text file for PredefinedConcepts to be exported";
            dlg.FileName = "new.txt";
            dlg.DefaultExt = "*.txt";
            dlg.Filter = "Text file for PredefinedConcepts (*.txt)|*.txt|All files (*.*)|*.*";

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(this);

            try
            {
                if (res == true)
                {
                    RememberForInitialDirectory(dlg.FileName);
                    Log.Info("Exporting text snippets for PredefinedConcepts: {0}", dlg.FileName);
                    AasxPredefinedConcepts.ExportPredefinedConcepts.Export(
                        thePackageEnv.AasEnv, ve1.theSubmodel, dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When exporting text snippets for PredefinedConcepts, an error occurred");
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ConvertElement()
        {
            // trivial things
            if (this.thePackageEnv == null || !this.thePackageEnv.IsOpen)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            // a Referable shall be exported
            AdminShell.Referable rf = null;
            object bo = null;
            if (DisplayElements.SelectedItem != null)
            {
                bo = DisplayElements.SelectedItem.GetMainDataObject();
                rf = DisplayElements.SelectedItem.GetDereferencedMainDataObject() as AdminShell.Referable;
            }

            if (rf == null)
            {
                MessageBoxFlyoutShow(
                    "No valid Referable selected for conversion.", "Convert Referable",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // try to get offers
            var offers = AasxPredefinedConcepts.Convert.ConvertPredefinedConcepts.CheckForOffers(rf);
            if (offers == null || offers.Count < 1)
            {
                MessageBoxFlyoutShow(
                    "No valid conversion offers found for this Referable. Aborting.", "Convert Referable",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // convert these to list items
            var fol = new List<SelectFromListFlyoutItem>();
            foreach (var o in offers)
                fol.Add(new SelectFromListFlyoutItem(o.OfferDisplay, o));

            // show a list
            // prompt for this list
            var uc = new SelectFromListFlyout();
            uc.Caption = "Select Conversion action to be executed ..";
            uc.ListOfItems = fol;
            this.StartFlyoverModal(uc);
            if (uc.ResultItem != null && uc.ResultItem.Tag != null &&
                uc.ResultItem.Tag is AasxPredefinedConcepts.Convert.ConvertOfferBase)
                try
                {
                    {
                        var offer = uc.ResultItem.Tag as AasxPredefinedConcepts.Convert.ConvertOfferBase;
                        offer?.Provider?.ExecuteOffer(
                            this.thePackageEnv, rf, offer, deleteOldCDs: true, addNewCDs: true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Executing user defined conversion");
                }

            // redisplay
            // add to "normal" event quoue
            DispEditEntityPanel.AddWishForOutsideAction(new ModifyRepo.LambdaActionRedrawAllElements(bo));
        }

        public void CommandBinding_ExportTable()
        {
            // trivial things
            if (this.thePackageEnv == null || !this.thePackageEnv.IsOpen)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            // a SubmodelRef shall be exported
            VisualElementSubmodelRef ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementSubmodelRef)
                ve1 = DisplayElements.SelectedItem as VisualElementSubmodelRef;

            if (ve1 == null || ve1.theSubmodel == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid SubModel selected for exporting table.", "Export Table",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // check, if required plugin can be found
            var pluginName = "AasxPluginExportTable";
            var actionName = "export-submodel";
            var pi = Plugins.FindPluginInstance(pluginName);
            if (pi == null || !pi.HasAction(actionName))
            {
                var res = MessageBoxFlyoutShow(
                        $"This function requires a binary plug-in file named '{pluginName}', " +
                        $"which needs to be added to the command line, with an action named '{actionName}'. " +
                        $"Press 'OK' to show help page on GitHub.",
                        "Plug-in not present",
                        MessageBoxButton.OKCancel, MessageBoxImage.Hand);
                if (res == MessageBoxResult.OK)
                {
                    ShowHelp();
                }
                return;
            }

            // try activate plugin
            pi.InvokeAction(actionName, this, ve1.theEnv, ve1.theSubmodel);
        }

        public void CommandBinding_NewSubmodelFromPlugin()
        {
            // trivial things
            if (this.thePackageEnv == null || !this.thePackageEnv.IsOpen)
            {
                MessageBoxFlyoutShow(
                    "An AASX package needs to be open", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            // an AAS needs to be selected
            VisualElementAdminShell ve1 = null;
            if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementAdminShell)
                ve1 = DisplayElements.SelectedItem as VisualElementAdminShell;

            if (ve1 == null || ve1.theAas == null || ve1.theEnv == null)
            {
                MessageBoxFlyoutShow(
                    "No valid AAS selected for creating a new Submodel.", "New Submodel from plugins",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // create a list of plugins, which are capable of generating Submodels
            var listOfSm = new List<SelectFromListFlyoutItem>();
            foreach (var lpi in Plugins.LoadedPlugins.Values)
            {
                if (lpi.HasAction("get-list-new-submodel"))
                    try
                    {
                        var lpires = lpi.InvokeAction("get-list-new-submodel") as AasxPluginResultBaseObject;
                        if (lpires != null)
                        {
                            var lpireslist = lpires.obj as List<string>;
                            if (lpireslist != null)
                                foreach (var smname in lpireslist)
                                    listOfSm.Add(new SelectFromListFlyoutItem(
                                        "" + lpi.name + " | " + "" + smname,
                                        new Tuple<Plugins.PluginInstance, string>(lpi, smname)
                                        ));
                        }
                    }
                    catch { }
            }

            // could be nothing
            if (listOfSm.Count < 1)
            {
                MessageBoxFlyoutShow(
                    "No plugins generating Submodels found. Aborting.", "New Submodel from plugins",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // prompt for this list
            var uc = new SelectFromListFlyout();
            uc.Caption = "Select Plug-in and Submodel to be generated ..";
            uc.ListOfItems = listOfSm;
            this.StartFlyoverModal(uc);
            if (uc.ResultItem != null && uc.ResultItem.Tag != null &&
                uc.ResultItem.Tag is Tuple<Plugins.PluginInstance, string>)
            {
                // get result arguments
                var TagTuple = uc.ResultItem.Tag as Tuple<Plugins.PluginInstance, string>;
                var lpi = TagTuple?.Item1;
                var smname = TagTuple?.Item2;
                if (lpi == null || smname == null || smname.Length < 1)
                {
                    MessageBoxFlyoutShow(
                        "Error accessing plugins. Aborting.", "New Submodel from plugins",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // try to invoke plugin to get submodel
                AasxPluginResultBaseObject gsres = null;
                try
                {
                    gsres = lpi.InvokeAction("generate-submodel", smname) as AasxPluginResultBaseObject;
                }
                catch { }

                // something
                var smres = gsres?.obj as AdminShell.Submodel;
                if (gsres == null || smres == null)
                {
                    MessageBoxFlyoutShow(
                        "Error accessing plugins. Aborting.", "New Submodel from plugins",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                try
                {
                    // Submodel needs an identification
                    smres.identification = new AdminShell.Identification("IRI", "");
                    if (smres.kind == null || smres.kind.IsInstance)
                        smres.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelInstance);
                    else
                        smres.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                            Options.Curr.TemplateIdSubmodelTemplate);

                    // add
                    var smref = new AdminShell.SubmodelRef(smres.GetReference());
                    ve1.theAas.AddSubmodelRef(smref);
                    thePackageEnv.AasEnv.Submodels.Add(smres);

                    // redisplay
                    // add to "normal" event quoue
                    DispEditEntityPanel.AddWishForOutsideAction(new ModifyRepo.LambdaActionRedrawAllElements(smref));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "when adding Submodel to AAS");
                }
            }
        }

        public void CommandBinding_ToolsFind(string cmd)
        {
            // access
            if (ToolsGrid == null || TabControlTools == null || TabItemToolsFind == null || ToolFindReplace == null)
                return;

            if (cmd == "toolsfindtext")
            {
                // make panel visible
                ToolsGrid.Visibility = Visibility.Visible;
                TabControlTools.SelectedItem = TabItemToolsFind;

                // set the link to the AAS environment
                // Note: dangerous, as it might change WHILE the find tool is opened!
                ToolFindReplace.TheAasEnv = this.thePackageEnv?.AasEnv;

                // cursor
                ToolFindReplace.FocusFirstField();
            }

            if (cmd == "toolsfindforward")
                ToolFindReplace.FindForward();

            if (cmd == "toolsfindbackward")
                ToolFindReplace.FindBackward();
        }

        public void CommandBinding_ImportOPCUANodeSet()
        {
            //choose File to import to
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML File (.xml)|*.xml|Text documents (.txt)|*.txt"; // Filter files by extension

            if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
            var result = dlg.ShowDialog();

            if (result == true)
            {
                RememberForInitialDirectory(dlg.FileName);
                UANodeSet InformationModel = UANodeSetExport.getInformationModel(dlg.FileName);
                thePackageEnv = UANodeSetImport.Import(InformationModel);
                RestartUIafterNewPackage();
            }

            if (Options.Curr.UseFlyovers) this.CloseFlyover();
        }

        public void CommandBinding_ExportOPCUANodeSet()
        {
            string filename = "i4AASCS.xml";
            string workingDirectory = "" + Environment.CurrentDirectory;

            // ReSharper disable PossibleNullReferenceException
            if (File.Exists(
                Path.Combine(
                    System.IO.Path.GetDirectoryName(
                        Directory.GetParent(workingDirectory).Parent.FullName),
                    filename)))
            // ReSharper enable PossibleNullReferenceException
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                dlg.Title = "Select AML file to be exported";
                dlg.FileName = "new.xml";
                dlg.DefaultExt = "*.xml";
                dlg.Filter = "XML File (.xml)|*.xml|Text documents (.txt)|*.txt";

                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = true == dlg.ShowDialog(this);
                if (!res)
                    return;

                RememberForInitialDirectory(dlg.FileName);

                UANodeSet InformationModel = null;

                // ReSharper disable PossibleNullReferenceException
                InformationModel = UANodeSetExport.getInformationModel(
                    Path.Combine(
                        System.IO.Path.GetDirectoryName(
                            Directory.GetParent(workingDirectory).Parent.FullName),
                        filename));
                // ReSharper enable PossibleNullReferenceException

                UANodeSetExport.root = InformationModel.Items.ToList();

                foreach (AdminShellV20.Asset ass in thePackageEnv.AasEnv.Assets)
                {
                    UANodeSetExport.CreateAAS(ass.idShort, thePackageEnv.AasEnv);
                }

                InformationModel.Items = UANodeSetExport.root.ToArray();

                using (var writer = new System.IO.StreamWriter(dlg.FileName))
                {
                    var serializer = new XmlSerializer(InformationModel.GetType());
                    serializer.Serialize(writer, InformationModel);
                    writer.Flush();
                }
                if (Options.Curr.UseFlyovers) this.CloseFlyover();
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Mapping Types could not be found.", "Error", MessageBoxButton.OK);
            }
        }
    }
}
