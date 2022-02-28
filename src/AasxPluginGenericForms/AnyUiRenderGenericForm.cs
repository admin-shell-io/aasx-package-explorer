using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPluginGenericForms
{
    public class AnyUiRenderGenericForm
    {
        protected bool _formInUpdateMode = false;
        protected FormInstanceSubmodel _currentFormInst = null;

        public AnyUiRenderGenericForm(
            FormInstanceSubmodel formInstance,
            bool updateMode = false)
        {
            _currentFormInst = formInstance;
            _formInUpdateMode = updateMode;
        }

        public bool InUpdateMode => _formInUpdateMode;

        public FormInstanceSubmodel FormInstance => _currentFormInst;

        protected double _lastScrollPosition = 0.0;

        public void RenderFormInst(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            bool setLastScrollPos = false,
            Func<object, AnyUiLambdaActionBase> lambdaFixCds = null,
            Func<object, AnyUiLambdaActionBase> lambdaCancel = null,
            Func<object, AnyUiLambdaActionBase> lambdaOK = null)
        {
            // make an outer grid, very simple grid of two rows: header & body
            var outer = view.Add(uitk.AddSmallGrid(rows: 3, cols: 1, colWidths: new[] { "*" }));
            outer.RowDefinitions[2].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

            // at top, make buttons for the general form
            var header = uitk.AddSmallGridTo(outer, 0, 0, 1, cols: 5, colWidths: new[] { "*", "#", "#", "#", "#" });

            header.Margin = new AnyUiThickness(0);
            header.Background = AnyUiBrushes.LightBlue;

            uitk.AddSmallBasicLabelTo(header, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkBlue,
                fontSize: 1.5f,
                setBold: true,
                content: $"Edit");

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 1,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Fix missing CDs .."),
                lambdaFixCds);
                //(o) =>
                //{
                //    return ButtonTabPanels_Click("ButtonFixCDs");
                //});

            uitk.AddSmallBasicLabelTo(header, 0, 2,
                foreground: AnyUiBrushes.DarkBlue,
                margin: new AnyUiThickness(4, 0, 4, 0),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                content: "|");

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 3,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Cancel"),
                lambdaCancel);
            //(o) =>
            //{
            //    // return ButtonTabPanels_Click("ButtonCancel");
            //});

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 4,
                    margin: new AnyUiThickness(2, 2, 4, 2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Update to AAS"),
                lambdaOK);
                //(o) =>
                //{
                //    return ButtonTabPanels_Click("ButtonUpdate");
                //});

            // small spacer
            var space = uitk.AddSmallBasicLabelTo(outer, 1, 0,
                fontSize: 0.3f,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            double? isc = null;
            if (setLastScrollPos)
                isc = _lastScrollPosition;

            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 2, 0,
                    horizontalScrollBarVisibility: AnyUiScrollBarVisibility.Disabled,
                    verticalScrollBarVisibility: AnyUiScrollBarVisibility.Visible,
                    skipForTarget: AnyUiTargetPlatform.Browser, 
                    initialScrollPosition: isc),
                (o) =>
                {
                    if (o is Tuple<double, double> positions)
                    {
                        _lastScrollPosition = positions.Item2;
                    }
                    return new AnyUiLambdaActionNone();
                }) as AnyUiScrollViewer;

            // need a stack panel to add inside
            var inner = new AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical };
            scroll.Content = inner;

            // render the innerts of the scroll viewer
            inner.Background = AnyUiBrushes.LightGray;
            _currentFormInst.RenderAnyUi(inner, uitk);
        }

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            if (_currentFormInst?.subscribeForNextEventReturn != null)
            {
                // delete first
                var tempLambda = _currentFormInst.subscribeForNextEventReturn;
                _currentFormInst.subscribeForNextEventReturn = null;

                // execute
                tempLambda(evtReturn);
            }
        }

    }
}
