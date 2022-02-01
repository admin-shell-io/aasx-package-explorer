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

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        protected bool _formInUpdateMode = false;
        protected AdminShell.SubmodelElementWrapperCollection _updateSourceElements = null;

        protected GenericFormsOptionsRecord _currentFormRecord = null;
        protected FormInstanceSubmodel _currentFormInst = null;

        #endregion

        #region Constructors, as for WPF control
        //=============

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            AasxPluginGenericForms.GenericFormOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;

            // fill given panel
            DisplaySubmodel(_panel, _uitk);
        }

        public static GenericFormsAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            AasxPluginGenericForms.GenericFormOptions options,
            PluginEventStack eventStack,
            object opanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // factory this object
            var shelfCntl = new GenericFormsAnyUiControl();
            shelfCntl.Start(log, package, sm, options, eventStack, panel);

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

            // initialize form
            _formInUpdateMode = true;
            _updateSourceElements = _submodel.submodelElements;

            // take over existing data
            _currentFormInst = new FormInstanceSubmodel(_currentFormRecord.FormSubmodel);
            _currentFormInst.InitReferable(_currentFormRecord.FormSubmodel, _submodel);
            _currentFormInst.PresetInstancesBasedOnSource(_updateSourceElements);
            _currentFormInst.outerEventStack = _eventStack;

            // set background
            view.Background = AnyUiBrushes.LightGray;

            // bring it to the panel
            _currentFormInst.RenderAnyUi(view, uitk);
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
            _panel.Background = AnyUiBrushes.LightGray;
            _currentFormInst.RenderAnyUi(_panel, _uitk);
        }

        #endregion

    }
}
