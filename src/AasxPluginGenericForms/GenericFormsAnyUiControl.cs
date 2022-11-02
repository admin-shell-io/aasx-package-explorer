/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

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
    public class GenericFormsAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private AdminShell.Submodel _submodel = null;
        private AasxPluginGenericForms.GenericFormOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;
        private PluginOperationContextBase _opContext = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        protected AnyUiRenderForm _form = null;

        public GenericFormsOptionsRecord _currentFormRecord = null;

        #endregion

        #region Constructors, as for WPF control
        //=============

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            AasxPluginGenericForms.GenericFormOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel,
            PluginOperationContextBase opContext)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;
            _opContext = opContext;

            // fill given panel
            DisplaySubmodel(_panel, _uitk);
        }

        public static GenericFormsAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            AasxPluginGenericForms.GenericFormOptions options,
            PluginEventStack eventStack,
            object opanel,
            PluginOperationContextBase opContext)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // do NOT create WPF controls
            FormInstanceBase.createSubControls = false;

            // factory this object
            var shelfCntl = new GenericFormsAnyUiControl();
            shelfCntl.Start(log, package, sm, options, eventStack, panel, opContext);

            // return shelf
            return shelfCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void DisplaySubmodel(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
        {
            // test trivial access
            if (_options == null || _submodel == null)
                return;

            // identify the record
            // check for a record in options, that matches Submodel
            _currentFormRecord = _options.MatchRecordsForSemanticId(_submodel.semanticId);
            if (_currentFormRecord == null)
                return;

            // check form
            if (_currentFormRecord.FormSubmodel == null || _currentFormRecord.FormSubmodel.SubmodelElements == null)
                return;

            // prepare form instance, take over existing data
            var fi = new FormInstanceSubmodel(_currentFormRecord.FormSubmodel);
            fi.InitReferable(_currentFormRecord.FormSubmodel, _submodel);
            fi.PresetInstancesBasedOnSource(_submodel.submodelElements);
            fi.outerEventStack = _eventStack;

            // initialize form
            _form = new AnyUiRenderForm(
                fi,
                updateMode: true);

            // bring it to the panel
            if (_opContext?.IsDisplayModeEditOrAdd == true)
            {
                _form.RenderFormInst(view, uitk, _opContext,
                    lambdaFixCds: (o) => ButtonTabPanels_Click("ButtonFixCDs"),
                    lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"),
                    lambdaOK: (o) => ButtonTabPanels_Click("ButtonUpdate"));
            }
            else
            {
                // display only
                _form.RenderFormInst(view, uitk, _opContext,
                    lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"));
            }
        }



        #endregion

        #region Event handling
        //=============

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            _form?.HandleEventReturn(evtReturn);
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 1
                || !(args[0] is AnyUiStackPanel newPanel))
                return;

            // ok, re-assign panel and re-display
            _panel = newPanel;
            _panel.Children.Clear();

            _form?.RenderFormInst(_panel, _uitk, _opContext,
                setLastScrollPos: true,
                lambdaFixCds: (o) => ButtonTabPanels_Click("ButtonFixCDs"),
                lambdaCancel: (o) => ButtonTabPanels_Click("ButtonCancel"),
                lambdaOK: (o) => ButtonTabPanels_Click("ButtonUpdate"));
        }

        #endregion

        #region Button clicks
        //=============

        private AnyUiLambdaActionBase ButtonTabPanels_Click(string cmd)
        {
            if (cmd == "ButtonCancel")
            {
                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            if (cmd == "ButtonUpdate")
            {
                // add
                if (this._form != null
                    && _package != null
                    && _options != null
                    && _submodel != null)
                {
                    // on this level of the hierarchy, shall a new SMEC be created or shall
                    // the existing source of elements be used?
                    AdminShell.SubmodelElementWrapperCollection currentElements = null;
                    if (_form.InUpdateMode)
                    {
                        currentElements = _submodel.submodelElements;
                    }
                    else
                    {
                    }

                    // create a sequence of SMEs
                    try
                    {
                        if (_form.FormInstance is FormInstanceSubmodelElementCollection fismec)
                            fismec.AddOrUpdateDifferentElementsToCollection(
                                currentElements, _package, addFilesToPackage: true);

                        if (_form.FormInstance is FormInstanceSubmodel fism)
                            fism.AddOrUpdateDifferentElementsToCollection(
                                currentElements, _package, addFilesToPackage: true);
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex, "when adding Document");
                    }

#if __may_be_not__
                    // save directly to ensure consistency
                    try
                    {
                        if (thePackage.Filename != null)
                            thePackage.SaveAs(thePackage.Filename);
                    }
                    catch (Exception ex)
                    {
                        if (theLogger != null)
                            theLogger.Log(
                                $"Saving package {thePackage.Filename} failed for adding Document " +
                                $"and gave: {ex.Message}");
                    }
#endif
                }
                else
                {
                    _log?.Error("Preconditions for adding entities from GenericForm not met.");
                }

                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            if (cmd == "ButtonFixCDs")
            {
                // check if CDs are present
                if (_currentFormRecord == null || _currentFormRecord.ConceptDescriptions == null ||
                    _currentFormRecord.ConceptDescriptions.Count < 1)
                {
                    _log?.Error(
                        "Not able to find appropriate ConceptDescriptions in the GenericForm option records. " +
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

#if __not_possible
                // be safe?
                if (MessageBoxResult.Yes != MessageBox.Show(
                    "Add missing ConceptDescriptions to the AAS?", "Question",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    return;
#endif

                // ok, check
                int nr = 0;
                foreach (var cd in _currentFormRecord.ConceptDescriptions)
                {
                    if (cd == null || cd.identification == null)
                        continue;
                    var cdFound = env.FindConceptDescription(cd.identification);
                    if (cdFound != null)
                        continue;
                    // ok, add
                    var newCd = new AdminShell.ConceptDescription(cd);
                    env.ConceptDescriptions.Add(newCd);
                    nr++;
                }

                // ok
                _log?.Info("In total, {0} ConceptDescriptions were added to the AAS environment.", nr);

                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            // no?
            return new AnyUiLambdaActionNone();
        }

        #endregion
    }
}
