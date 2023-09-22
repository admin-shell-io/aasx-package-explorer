/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;
using System.IO.Packaging;
using System.Threading.Tasks;

// ReSharper disable InconsistentlySynchronizedField
// ReSharper disable AccessToModifiedClosure

namespace AasxPluginContactInformation
{
    public class ShelfAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private ContactInformationOptions _options = null;
        private PluginEventStack _eventStack = null;
        private PluginSessionBase _session = null;
        private AnyUiStackPanel _panel = null;
        private AnyUiContextPlusDialogs _displayContext = null;
        private PluginOperationContextBase _opContext = null;
        private AasxPluginBase _plugin = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        protected AasxLanguageHelper.LangEnum _selectedLang = AasxLanguageHelper.LangEnum.Any;

        protected string _selectedRole = null;

        protected string _selectedFilterText = "";

        private List<ContactEntity> _renderedEntities = new List<ContactEntity>();

        private List<ContactEntity> theDocEntitiesToPreview = new List<ContactEntity>();

        // members for form editing

        protected AnyUiRenderForm _formDoc = null;

        protected static int InstCounter = 1;

        protected string CurrInst = "";

        #endregion

        #region Constructors, as for WPF control
        //=============

        public ShelfAnyUiControl()
        {
        }

        public void Dispose()
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            Aas.Submodel theSubmodel,
            ContactInformationOptions theOptions,
            PluginEventStack eventStack,
            PluginSessionBase session,
            AnyUiStackPanel panel,
            PluginOperationContextBase opContext,
            AnyUiContextPlusDialogs cdp,
            AasxPluginBase plugin)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _session = session;
            _panel = panel;
            _opContext = opContext;
            _displayContext = cdp;
            _plugin = plugin;

            // no form, yet
            _formDoc = null;

            // fill given panel
            RenderFullList(_panel, _uitk);
        }

        public static ShelfAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            ContactInformationOptions options,
            PluginEventStack eventStack,
            PluginSessionBase session,
            object opanel,
            PluginOperationContextBase opContext,
            AnyUiContextPlusDialogs cdp,
            AasxPluginBase plugin)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as Aas.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // do NOT create WPF controls
            FormInstanceBase.createSubControls = false;

            // factory this object
            var shelfCntl = new ShelfAnyUiControl();
            shelfCntl.Start(log, package, sm, options, eventStack, session, panel, opContext, cdp, plugin);

            // return shelf
            return shelfCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullList(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
        {
            // test trivial access
            if (_options == null || _submodel?.SemanticId == null)
                return;

            // make sure for the right Submodel
            ContactInformationOptionsRecord foundRec = null;
            foreach (var rec in _options.LookupAllIndexKey<ContactInformationOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                foundRec = rec;

            if (foundRec == null)
                return;

            // what defaultLanguage
            string defaultLang = null;

            // make new list box items
            _renderedEntities = ListOfContactEntity.ParseSubmodelForV10(
                _package, _submodel, _options, defaultLang, 0, _selectedLang);

            // bring it to the panel            
            RenderPanelOutside(view, uitk, defaultLang, _renderedEntities);
        }

        protected double _lastScrollPosition = 0.0;

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            string defaultLanguage,
            List<ContactEntity> its,
            double? initialScrollPos = null)
        {
            // make an outer grid, very simple grid of two rows: header & body
            var outer = view.Add(uitk.AddSmallGrid(rows: 4, cols: 1, colWidths: new[] { "*" }));
            outer.RowDefinitions[2].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

            // at top, make buttons for the general form
            var header = uitk.AddSmallGridTo(outer, 0, 0, rows: 2, cols: 5,
                    colWidths: new[] { "*", "#", "#", "#", "#" });

            header.Margin = new AnyUiThickness(0);
            header.Background = AnyUiBrushes.LightBlue;

            //
            // Blue bar
            //

            uitk.AddSmallBasicLabelTo(header, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkBlue,
                fontSize: 1.5f,
                setBold: true,
                content: $"Contact informations");

            if (_opContext?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(header, 0, 2,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        content: "Add Contact .."),
                    setValueAsync: (o) => ButtonTabPanels_Click("ButtonAddContact"));

            //
            // Usage info
            //

            if (true)
                uitk.AddSmallBasicLabelTo(header, 1, 0, colSpan: 5,
                    margin: new AnyUiThickness(8, 2, 2, 2),
                    foreground: AnyUiBrushes.DarkBlue,
                    fontSize: 0.8f, textWrapping: AnyUiTextWrapping.Wrap,
                    content: "List of contact information entities in Submodel.");

            //
            // Selectors
            //

            // countries
            var allLangs = new List<object>();
            foreach (var dc in (AasxLanguageHelper.LangEnum[])Enum.GetValues(
                                    typeof(AasxLanguageHelper.LangEnum)))
                allLangs.Add("Nation - " + AasxLanguageHelper.LangEnumToISO3166String[(int)dc]);

            // controls
            var controls = uitk.AddSmallWrapPanelTo(outer, 1, 0,
                background: AnyUiBrushes.MiddleGray, margin: new AnyUiThickness(0, 0, 0, 2));

            //
            // Countries
            //

            AnyUiComboBox cbCountries = null;
            cbCountries = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 120,
                Items = allLangs,
                SelectedIndex = (int)_selectedLang
            }), (o) =>
            {
                // ReSharper disable PossibleInvalidOperationException
                if (cbCountries != null)
                    _selectedLang = (AasxLanguageHelper.LangEnum)cbCountries.SelectedIndex;
                // ReSharper enable PossibleInvalidOperationException
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            });

            //
            // Role
            //

            // prepare selection

            var roleHeader = "Role - ";
            var allRoles = new List<string>();
            allRoles.Add("Role - All");
            if (its != null)
                allRoles.AddRange(its
                    .Select((ce) => roleHeader + ce.Role)
                    .Distinct());

            // ui

            AnyUiComboBox cbRoles = null;
            cbRoles = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiComboBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 200,
                Items = allRoles.Cast<object>().ToList(),
                SelectedIndex = (_selectedRole == null) ? 0 : allRoles.IndexOf(roleHeader + _selectedRole)
            }), (o) =>
            {
                if (cbRoles != null && cbRoles.SelectedIndex.HasValue
                    && cbRoles.SelectedIndex >= 0 && cbRoles.SelectedIndex < allRoles.Count
                    && (allRoles[cbRoles.SelectedIndex.Value] is string selRole)
                    && selRole.Length >= roleHeader.Length)
                {
                    if (cbRoles.SelectedIndex == 0)
                        _selectedRole = null;
                    else
                        _selectedRole = selRole.Substring(roleHeader.Length);
                }

                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            });

            //
            // Text based filter
            //

            AnyUiButton btnFilter = null;

            btnFilter = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiButton()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                Content = "Filter \U0001f846"
            }), (o) =>
            {
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            });

            AnyUiTextBox tbFilterText = null;
            tbFilterText = AnyUiUIElement.RegisterControl(controls.Add(new AnyUiTextBox()
            {
                Margin = new AnyUiThickness(6, 4, 4, 4),
                MinWidth = 200,
                Text = _selectedFilterText
            }), (o) =>
            {
                if (o is string os)
                    _selectedFilterText = os;
                return new AnyUiLambdaActionNone();
            });

            //
            // Scroll area
            //

            // small spacer
            outer.RowDefinitions[2] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 2, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            outer.RowDefinitions[3] = new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Star);
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 3, 0,
                    horizontalScrollBarVisibility: AnyUiScrollBarVisibility.Disabled,
                    verticalScrollBarVisibility: AnyUiScrollBarVisibility.Visible,
                    flattenForTarget: AnyUiTargetPlatform.Browser, initialScrollPosition: initialScrollPos),
                (o) =>
                {
                    if (o is Tuple<double, double> positions)
                    {
                        _lastScrollPosition = positions.Item2;
                    }
                    return new AnyUiLambdaActionNone();
                });

            // need a stack panel to add inside
            var inner = new AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical };
            scroll.Content = inner;

            // render the innerts of the scroll viewer
            inner.Background = AnyUiBrushes.LightGray;

            // the filtering of entities applies late, as the original list is required for
            // role etc. ..

            var filtered = new List<ContactEntity>();
            if (its != null)
                foreach (var ce in its)
                {
                    // Country?
                    if (_selectedLang != AasxLanguageHelper.LangEnum.Any
                        && !AasxLanguageHelper.LangEnumToISO3166String[(int)_selectedLang]
                                .Equals(ce.CountryCode, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Role
                    if (_selectedRole != null
                        && !_selectedRole.Equals(ce.Role, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Filter text?
                    if (_selectedFilterText?.HasContent() == true)
                    {
                        var found = ce.Headline.Contains(_selectedFilterText,
                            StringComparison.InvariantCultureIgnoreCase);
                        found = found || ce.Headline.Contains(_selectedFilterText,
                            StringComparison.InvariantCultureIgnoreCase);
                        if (ce.BodyItems != null)
                            foreach (var bi in ce.BodyItems)
                                found = found || bi.Contains(_selectedFilterText,
                                    StringComparison.InvariantCultureIgnoreCase);
                        if (!found)
                            continue;
                    }

                    // ok, add
                    filtered.Add(ce);
                }

            // display
            foreach (var de in filtered)
            {
                var rde = RenderAnyUiContactEntity(uitk, de);
                if (rde != null)
                    inner.Add(rde);
            }

            // post process
            foreach (var ent in filtered)
            {
                // if a preview file exists, try load directly
                if (ent.PreviewFile?.Path?.HasContent() == true)
                {
                    var inputFn = ent.PreviewFile.Path;

                    try
                    {
                        // from package?
                        if (CheckIfPackageFile(inputFn))
                            inputFn = _package?.MakePackageFileAvailableAsTempFile(ent.PreviewFile.Path);

                        ent.LoadImageFromPath(inputFn);
                    }
                    catch (Exception ex)
                    {
                        // do not show any error message, as it might appear
                        // frequently
                        LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }
                else
                {
                    // add placeholder
                    ent.LoadImageFromResource("AasxPluginContactInformation.Resources.no-contact-preview.png");
                }

                // attach events and add
                ent.DoubleClick += DocumentEntity_DoubleClick;
                ent.MenuClick += DocumentEntity_MenuClick;
            }
        }

        public AnyUiFrameworkElement RenderAnyUiContactEntity(
            AnyUiSmallWidgetToolkit uitk, ContactEntity de)
        {
            // access
            if (de == null)
                return new AnyUiStackPanel();

            // make a outer grid
            var outerG = uitk.AddSmallGrid(1, 1,
                colWidths: new[] { "*" }, rowHeights: new[] { "*" },
                margin: new AnyUiThickness(0));

            // make 1 background border as shadow
            // Note: former i = 2
            uitk.Set(
                uitk.AddSmallBorderTo(outerG, 0, 0,
                    margin: new AnyUiThickness(3 + 2 * 2, 3 + 2 * 2, 3 + 4 - 2 * 2, 3 + 4 - 2 * 2),
                    background: AnyUiBrushes.DarkGray,
                    borderBrush: AnyUiBrushes.Transparent,
                    borderThickness: new AnyUiThickness(1.0),
                    cornerRadius: 3),
                skipForTarget: AnyUiTargetPlatform.Browser);

            // make the border, which will get content
            var border = uitk.AddSmallBorderTo(outerG, 0, 0,
                margin: new AnyUiThickness(3, 3, 3 + 4, 3 + 4),
                background: AnyUiBrushes.White,
                borderBrush: AnyUiBrushes.Black,
                borderThickness: new AnyUiThickness(1.0),
                cornerRadius: 3);

            // the border emits double clicks
            border.EmitEvent = AnyUiEventMask.LeftDouble;
            border.setValueLambda = (o) =>
            {
                if (o is AnyUiEventData ed
                    && ed.Mask == AnyUiEventMask.LeftDouble
                    && ed.ClickCount == 2)
                {
                    de.RaiseDoubleClick();
                }
                return new AnyUiLambdaActionNone();
            };

            // make a grid
            var g = uitk.AddSmallGrid(3, 3,
                colWidths: new[] { "60:", "*", "24:" },
                rowHeights: new[] { "14:", "40:", "24:" },
                margin: new AnyUiThickness(1),
                background: AnyUiBrushes.White);
            border.Child = g;

            // Orga and Country flags flapping in the breeze
            var sp1 = uitk.AddSmallStackPanelTo(g, 0, 1,
                setHorizontal: true);

            if (de.CountryCode?.HasContent() == true)
                sp1.Add(new AnyUiCountryFlag()
                {
                    HorizontalAlignment = AnyUiHorizontalAlignment.Left,
                    ISO3166Code = de.CountryCode,
                    Margin = new AnyUiThickness(0, 0, 3, 0),
                    MinHeight = 14,
                    MaxHeight = 14,
                    MaxWidth = 20
                });

            // Role follows the flag (idea: both associated with first combo boxes left -> right)

            sp1.Add(new AnyUiTextBlock()
            {
                HorizontalAlignment = AnyUiHorizontalAlignment.Left,
                HorizontalContentAlignment = AnyUiHorizontalAlignment.Left,
                Text = $"{(de.Role.HasContent() ? de.Role : "\u2014")}",
                FontSize = 0.8f,
                FontWeight = AnyUiFontWeight.Bold
            });

            // Headline
            uitk.AddSmallBasicLabelTo(g, 1, 1,
                textIsSelectable: false,
                margin: new AnyUiThickness(2),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                fontSize: 1.2f,
                content: $"{de.Headline}");

            // Role

            // body items
            if (de.BodyItems != null && de.BodyItems.Count > 0)
            {
                var bits = string.Join(" \u2022 ", de.BodyItems);
                uitk.AddSmallBasicLabelTo(g, 2, 1,
                    textIsSelectable: false,
                    margin: new AnyUiThickness(2),
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    fontSize: 0.8f,
                    content: $"{bits}");
            }

            // Image
            de.ImgContainerAnyUi =
                uitk.Set(
                    uitk.AddSmallImageTo(g, 0, 0,
                        margin: new AnyUiThickness(2),
                        stretch: AnyUiStretch.Uniform),
                    rowSpan: 3,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);

            var hds = new List<string>();
            if (_opContext?.IsDisplayModeEditOrAdd == true)
            {
                hds.AddRange(new[] { "\u270e", "Edit contact data" });
                hds.AddRange(new[] { "\u2702", "Delete" });
            }
            else
                hds.AddRange(new[] { "\u270e", "View contact data" });

            // context menu
            uitk.AddSmallContextMenuItemTo(g, 2, 2,
                    "\u22ee",
                    hds.ToArray(),
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0),
                    fontWeight: AnyUiFontWeight.Bold,
                    menuItemLambda: null,
                    menuItemLambdaAsync: async (o) =>
                    {
                        if (o is int ti && ti >= 0 && ti < hds.Count)
                            // awkyard, but for compatibility to WPF version
                            await de?.RaiseMenuClick(hds[2 * ti + 1], null);
                        return new AnyUiLambdaActionNone();
                    });

            // ok
            return outerG;
        }

        #endregion

        #region Event handling
        //=============

        private Action<AasxPluginEventReturnBase> _menuSubscribeForNextEventReturn = null;

        protected void PushUpdateEvent(AnyUiRenderMode mode = AnyUiRenderMode.All)
        {
            // bring it to the panel by redrawing the plugin
            _eventStack?.PushEvent(new AasxPluginEventReturnUpdateAnyUi()
            {
                // get the always currentplugin name
                PluginName = _plugin?.GetPluginName(),
                Session = _session,
                Mode = mode,
                UseInnerGrid = true
            });
        }

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            // demands from shelf
            if (_menuSubscribeForNextEventReturn != null)
            {
                // delete first
                var tempLambda = _menuSubscribeForNextEventReturn;
                _menuSubscribeForNextEventReturn = null;

                // execute
                tempLambda(evtReturn);

                // finish
                return;
            }

            // check, if a form is active
            if (_formDoc != null)
            {
                _formDoc.HandleEventReturn(evtReturn);
            }
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 2
                || !(args[0] is AnyUiStackPanel newPanel)
                || !(args[1] is AnyUiContextPlusDialogs newCdp))
                return;

            // ok, re-assign panel and re-display
            _displayContext = newCdp;
            _panel = newPanel;
            _panel.Children.Clear();

            // multiple different views can be renders
            if (_formDoc != null)
            {
                if (_opContext?.IsDisplayModeEditOrAdd == true)
                {
                    _formDoc.RenderFormInst(_panel, _uitk, _opContext,
                        setLastScrollPos: true,
                        lambdaFixCds: (o) => ButtonTabPanels_Click("ButtonFixCDs"),
                        lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"),
                        lambdaOK: (o) => ButtonTabPanels_Click("ButtonUpdate"));
                }
                else
                {
                    _formDoc.RenderFormInst(_panel, _uitk, _opContext,
                        setLastScrollPos: true,
                        lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"));
                }
            }
            else
            {
                // the default: the full shelf
                RenderFullList(_panel, _uitk);
            }
        }

        #endregion

        #region Callbacks
        //===============

        private List<Aas.ISubmodelElement> _updateSourceElements = null;

        private async Task GetFormDescForSingleContact(
            List<Aas.ISubmodelElement> sourceElems)
        {
            // ask the plugin generic forms for information via event stack
            _eventStack?.PushEvent(new AasxIntegrationBase.AasxPluginResultEventInvokeOtherPlugin()
            {
                Session = _session,
                PluginName = "AasxPluginGenericForms",
                Action = "find-form-desc",
                UseAsync = false,
                Args = new object[] {
                    AasxPredefinedConcepts.IdtaContactInformationV10.Static
                        .SM_ContactInformations.GetSemanticRef() }
            });

            // .. and receive incoming event ..
            _menuSubscribeForNextEventReturn = (revt) =>
            {
                if (revt is AasxPluginEventReturnInvokeOther rinv
                    && rinv.ResultData is AasxPluginResultBaseObject rbo
                    && rbo.obj is List<FormDescBase> fdb
                    && fdb.Count == 1
                    && fdb[0] is FormDescSubmodel descSm)
                {
                    _updateSourceElements = sourceElems;

                    // need to identify the form for single contact BELOW the Submodel
                    FormDescSubmodelElementCollection descSmc = null;
                    if (descSm.SubmodelElements != null)
                        foreach (var desc in descSm.SubmodelElements)
                            if (desc is FormDescSubmodelElementCollection desc2
                                && AasxPredefinedConcepts.IdtaContactInformationV10.Static
                                    .CD_ContactInformation.GetReference()?.MatchesExactlyOneKey(
                                        desc2?.KeySemanticId, matchMode: MatchMode.Relaxed) == true)
                            {
                                descSmc = desc2;
                            }

                    if (descSmc == null)
                        return;

                    // build up outer frame
                    var fi = new FormInstanceSubmodelElementCollection(null, descSmc)
                    {
                        outerEventStack = _eventStack,
                        OuterPluginName = _plugin?.GetPluginName(),
                        OuterPluginSession = _session,
                    };

                    // if present, create link to existing data
                    // -> update instead of add
                    fi.PresetInstancesBasedOnSource(_updateSourceElements);

                    // initialize form and start editing
                    _formDoc = new AnyUiRenderForm(
                        fi,
                        updateMode: sourceElems != null);
                    PushUpdateEvent();
                }
            };
        }

        private async Task DocumentEntity_MenuClick(ContactEntity e, string menuItemHeader, object tag)
        {
            // first check
            if (e == null || menuItemHeader == null)
                return;

            // what to do?
            if (tag == null
                && (menuItemHeader == "Edit contact data" || menuItemHeader == "View contact data")
                && e.SourceElementContact?.Value != null)
            {
                // ask the plugin generic forms for information via event stack
                // and subsequently start editing form
                await GetFormDescForSingleContact(e.SourceElementContact.Value);

                // OK
                return;
            }

            if (tag == null && menuItemHeader == "Delete"
                && e.SourceElementContact?.Value != null
                && true == _submodel?.SubmodelElements?.Contains(e.SourceElementContact)
                && true == e.SourceElementContact.SemanticId?.Matches(
                    AasxPredefinedConcepts.IdtaContactInformationV10.Static
                        .CD_ContactInformation.GetReference(), matchMode: MatchMode.Relaxed)
                && _options != null
                && _opContext?.IsDisplayModeEditOrAdd == true)
            {
                // ask back via display context
                if (AnyUiMessageBoxResult.Cancel == await _displayContext?.MessageBoxFlyoutShowAsync(
                    "Delete ContactEntity? This cannot be reverted!",
                    "Contact list",
                    AnyUiMessageBoxButton.OKCancel,
                    AnyUiMessageBoxImage.Question))
                    return;

                // do it
                try
                {
                    _submodel?.SubmodelElements.Remove(e.SourceElementContact);

                    // re-display also in Explorer
                    _eventStack?.PushEvent(new AasxPluginResultEventRedrawAllElements()
                    { Session = _session });

                    // log
                    _log?.Info("Deleted Document(Version).");
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "while deleting contact");
                }
            }
        }

        private void DocumentEntity_DoubleClick(ContactEntity e)
        {
        }

        private async Task<AnyUiLambdaActionBase> ButtonTabPanels_Click(string cmd, string arg = null)
        {
            if (cmd == "ButtonCancel")
            {
                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { RedrawCurrentEntity = true };
            }

            if (cmd == "ButtonUpdate")
            {
                // add
                if (this._formDoc != null
                    && _package != null
                    && _options != null
                    && _submodel != null)
                {
                    // on this level of the hierarchy, shall a new SMEC be created or shall
                    // the existing source of elements be used?
                    List<Aas.ISubmodelElement> currentElements = null;
                    if (_formDoc.InUpdateMode)
                    {
                        currentElements = _updateSourceElements;
                    }
                    else
                    {
                        currentElements = new List<Aas.ISubmodelElement>();
                    }

                    // create a sequence of SMEs
                    try
                    {
                        if (_formDoc.FormInstance is FormInstanceSubmodelElementCollection fismec)
                            fismec.AddOrUpdateDifferentElementsToCollection(
                                currentElements, _package, addFilesToPackage: true);

                        _log?.Info("Document elements updated. Do not forget to save, if necessary!");
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex, "when adding Document");
                    }

                    // the InstSubmodel, which started the process, should have a "fresh" SMEC available
                    // make it unique in the Documentens Submodel
                    var newSmc = (_formDoc.FormInstance as FormInstanceSubmodelElementCollection)?.sme
                            as Aas.SubmodelElementCollection;

                    // if not update, put them into the Document's Submodel
                    if (!_formDoc.InUpdateMode && currentElements != null && newSmc != null)
                    {
                        // make newSmc unique in the cotext of the Submodel
                        FormInstanceHelper.MakeIdShortUnique(_submodel.SubmodelElements, newSmc);

                        // add the elements
                        newSmc.Value = currentElements;

                        // add the whole SMC
                        _submodel.Add(newSmc);
                    }
                }
                else
                {
                    _log?.Error("Preconditions for update entities from Document not met.");
                }

                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            if (cmd == "ButtonFixCDs")
            {
                // check if CDs are present
                var theDefs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());
                var theCds = theDefs.GetAllReferables().Where(
                    (rf) => { return rf is Aas.ConceptDescription; }).ToList();

                // v10
                if (true)
                {
                    theCds = AasxPredefinedConcepts.SmtAdditions.Static.GetAllReferables().Where(
                    (rf) => { return rf is Aas.ConceptDescription; }).ToList();
                }

                if (theCds.Count < 1)
                {
                    _log?.Error(
                        "Not able to find appropriate ConceptDescriptions in pre-definitions. " +
                        "Aborting.");
                    return new AnyUiLambdaActionNone();
                }

                // check for Environment
                var env = _package?.AasEnv;
                if (env == null)
                {
                    _log?.Error(
                        "Not able to access AAS environment for set of Submodel's ConceptDescriptions. Aborting.");
                    return new AnyUiLambdaActionNone();
                }

                // ask back via display context
                if (AnyUiMessageBoxResult.Cancel == await _displayContext?.MessageBoxFlyoutShowAsync(
                    "Add missing ConceptDescriptions to the AAS?",
                    "DocumentShelf",
                    AnyUiMessageBoxButton.OKCancel,
                    AnyUiMessageBoxImage.Question))
                    return new AnyUiLambdaActionNone();

                // do it
                try
                {
                    // ok, check
                    int nr = 0;
                    foreach (var x in theCds)
                    {
                        var cd = x as Aas.ConceptDescription;
                        if (cd == null || cd.Id?.HasContent() != true)
                            continue;
                        var cdFound = env.FindConceptDescriptionById(cd.Id);
                        if (cdFound != null)
                            continue;
                        // ok, add
                        var newCd = cd.Copy();
                        env.ConceptDescriptions.Add(newCd);
                        nr++;
                    }

                    // ok
                    _log?.Info("In total, {0} ConceptDescriptions were added to the AAS environment.", nr);
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "when adding ConceptDescriptions for Document");
                }

                // ok; event pending, nothing here
                return new AnyUiLambdaActionNone();
            }

            if (cmd == "ButtonAddContact")
            {
                // ask the plugin generic forms for information via event stack
                // and subsequently start editing form
                await GetFormDescForSingleContact(sourceElems: null);

                // OK
                return new AnyUiLambdaActionNone();
            }

            // no?
            return new AnyUiLambdaActionNone();
        }

        #endregion

        #region Utilities
        //===============

        private bool CheckIfPackageFile(string fn)
        {
            return fn.StartsWith(@"/");
        }

        #endregion
    }
}
