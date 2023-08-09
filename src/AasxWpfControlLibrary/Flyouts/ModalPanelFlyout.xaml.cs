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
using AasxIntegrationBaseWpf;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class ModalPanelFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataModalPanel DiaData = new AnyUiDialogueDataModalPanel();

        public AnyUiDisplayContextWpf DisplayContext = null;

        protected UIElement RenderedContentContainer = null;

        private Dictionary<Button, AnyUiMessageBoxResult> buttonToResult =
            new Dictionary<Button, AnyUiMessageBoxResult>();

        //
        // Start
        //

        public ModalPanelFlyout(AnyUiDisplayContextWpf displayContext)
        {
            InitializeComponent();

            // remember
            DisplayContext = displayContext;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // outer window
            TextBlockCaption.Text = "" + DiaData?.Caption;

            // buttons
            var layout = MessageBoxFlyout.LayoutButtons(DiaData.DialogButtons, DiaData.ExtraButtons);
            MessageBoxFlyout.RenderButtonLayout(
                this, this.StackPanelButtons, layout,
                Button_Click, out buttonToResult,
                fontSize: 18,
                buttonHeight: 28, buttonWidth: 80);

            // create the panel contents
            CreateWpfPanel();
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button && buttonToResult.ContainsKey(sender as Button))
            {
                var res = buttonToResult[sender as Button];
                DiaData.ResultButton = res;
                DiaData.Result =
                    res == AnyUiMessageBoxResult.OK
                    || res == AnyUiMessageBoxResult.Yes
                    || res >= AnyUiMessageBoxResult.Extra0;
                ControlClosed?.Invoke();
            }
        }

        protected void CreateWpfPanel(bool forceRenderPanel = false)
        {
            // access
            if (DisplayContext == null || DiaData == null)
                return;

            // re-render?
            if (forceRenderPanel)
                DiaData.Panel = DiaData.RenderPanel?.Invoke(DiaData);

            // create some render defaults matching the visual style of the flyout
            var rd = new AnyUiDisplayContextWpf.RenderDefaults()
            {
                FontSizeRel = 1.5f,
                ForegroundSelfStand = AnyUiBrushes.White,
                ForegroundControl = null // leave untouched                
            };

            // direct or scrollable?
            var panelCnt = DisplayContext.GetOrCreateWpfElement(DiaData.Panel, renderDefaults: rd);
            if (!DiaData.DisableScrollArea)
            {
                // use the existing the scroll viewer
                ScrollViewContent.Content = null;
                ScrollViewContent.Content = panelCnt;
                RenderedContentContainer = panelCnt;
                panelCnt.UpdateLayout();
                this.UpdateLayout();
            }
            else
            {
                // delete the inital scroll viewer or rendered container, add directly
                if (RenderedContentContainer == null)
                {
                    AnyUiHelper.TakeOverGridPosition(panelCnt, ScrollViewContent);
                    GridContentAndButtons.Children.Remove(ScrollViewContent);
                }
                else
                {
                    AnyUiHelper.TakeOverGridPosition(panelCnt, RenderedContentContainer);
                    GridContentAndButtons.Children.Remove(RenderedContentContainer);
                }

                GridContentAndButtons.Children.Add(panelCnt);
                RenderedContentContainer = panelCnt;

                panelCnt.UpdateLayout();
                this.UpdateLayout();
            }
        }

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
            if (la is AnyUiLambdaActionModalPanelReRender lamprr)
            {
                CreateWpfPanel(forceRenderPanel: true);
            }
        }

        //
        // Mechanics
        //

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            ControlClosed?.Invoke();
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

    }
}
