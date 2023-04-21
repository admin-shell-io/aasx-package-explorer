﻿/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;

/*
 * The Instances are organized in a different schema than the Descriptions!

Submodel                                    SMEC                                        (Prop, MLP, File..)

LoD cntl    [v]       LoS cntl  [+/-]       LoD cntl    [v]       LoS cntl  [+/-]       SubCnt

+-------------+       +-------------+       +-------------+       +-------------+       +--------------+
|Manuf.       |       |             |       |             |       |             |       |              |
|             |       | +---------+ |       | +---------+ |       |  Name 1 [ ]+------->+ <TextBox/>   |
| ..          | +---+ | |CI1      | |       | | Name    | |       |             |       |              |
|             | |     | |  Name   +---------+ +---------+ |       |             |       +--------------+
|ContactInfo  | ++    | |  Street | |       |             |       | +-------+   |
|             |  |    | +---------+ |       | +---------+ |       | |Name 2 |   |   each instance:
|             |  |    | +---------+ |       | | Street  | |       | +-------+   |   --------------
|             |  |    | |CI2      | |       | +---------+ |       |             |   Desc
|             |  +--+ | |  Name   | |       |             |       |             |   Sme
|             |       | |  Street | |       |             |       |             |   sourceSme
|             |       | +---------+ |       |             |       |             |   SubCnt
|             |       |             |       |             |       |             |
+-------------+       +-------------+       +-------------+       +-------------+

ListOfDifferent       ListOfSame            ListOfDifferent       ListOfSame
(Base)                                      (Base)

Pairs:                just Instances        Pairs:                just Instances
Desc + Instances                            Desc + Instances

outer Desc:
Submodel

 *
 */

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global .. to be carefully checked later

namespace AasxIntegrationBase.AasForms
{
    public static class FormInstanceHelper
    {
        /// <summary>
        /// Check if <c>smw.IdShort</c>c> contains something like "{0:00}" and iterate index to make it unique
        /// </summary>
        public static void MakeIdShortUnique(
            List<Aas.ISubmodelElement> collection, Aas.ISubmodelElement sme)
        {
            // access
            if (collection == null || sme == null)
                return;

            // check, if to make idShort unique?
            if (sme.IdShort.Contains("{0"))
            {
                var newIdShort = collection.IterateIdShortTemplateToBeUnique(sme.IdShort, 999);
                if (newIdShort != null)
                    sme.IdShort = newIdShort;
            }
        }

        /// <summary>
        /// Finds the topmost form instance, e.g. to link to outer event stack.
        /// </summary>
        public static IFormInstanceParent GetTopMostParent(IFormInstanceParent current)
        {
            IFormInstanceParent top = current;
            while (top?.GetInstanceParent() != null)
                top = top.GetInstanceParent();
            return top;
        }
    }

    public interface IFormInstanceParent
    {
        IFormInstanceParent GetInstanceParent();
    }

    public interface IFormRenderAnyUi
    {
        void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk, PluginOperationContextBase opctx);
    }

    public class FormInstanceBase : IFormInstanceParent, IFormRenderAnyUi
    {
        /// <summary>
        /// As the descriptions holds descriptive data and dynamic data, the edit funcitonalities
        /// will need working copies of the descriptions (dynamic data)
        /// </summary>
        public FormDescBase desc = null;
        public FormInstanceListOfSame parentInstance = null;

        public IFormInstanceParent GetInstanceParent() { return parentInstance; }

        /// <summary>
        /// For the TOPMOST instance, link to the outer event stack
        /// </summary>
        public PluginEventStack outerEventStack = null;

        /// <summary>
        /// If not null specifies the plugin name, to which form events shall be directed
        /// </summary>
        public string OuterPluginName = null;

        /// <summary>
        /// Session for plugin event return
        /// </summary>
        public PluginSessionBase OuterPluginSession = null;

        /// <summary>
        /// For the TOPMOST instance, receive the next incoming event ..
        /// </summary>
        public Action<AasxPluginEventReturnBase> subscribeForNextEventReturn = null;

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public FormInstanceBase() { }

        public FormInstanceBase(FormInstanceListOfSame parentInstance, FormDescBase desc)
        {
            this.parentInstance = parentInstance;
            this.desc = desc;
        }

        /// <summary>
        /// This STATIC flag indicates, if the form instances shall create WPF controls or not.
        /// </summary>
        public static bool createSubControls = true;

#if USE_WPF
        /// <summary>
        /// The WPF (sub) control, to which this instance is attached to
        /// </summary>
        public UserControl subControl = null;
#endif

        /// <summary>
        /// Set by RenderAnyUito hold the main element of an instance, e.g. to 
        /// trigger updates
        /// </summary>
        public AnyUiControl MainControl = null;

        public int Index
        {
            get
            {
                var list = this.parentInstance?.SubInstances;
                if (list != null)
                {
                    var p = list.IndexOf(this);
                    if (p >= 0)
                        return p;
                }
                return 0;
            }
        }

        /// <summary>
        /// Indicates, tha index numbers for the instance shall be display in the user interfaces
        /// </summary>
        public bool ShowIndex
        {
            get
            {
                var dse = desc as FormDescSubmodelElement;
                if (dse == null)
                    return false;
                var m = dse.Multiplicity;
                return m == FormMultiplicity.OneToMany || m == FormMultiplicity.ZeroToMany;
            }
        }

        /// <summary>
        /// Indicates, if a value change occured to the item
        /// </summary>
        public bool Touched = false;

        /// <summary>
        /// To be called, if item was touched by the user
        /// </summary>
        public void Touch()
        {
            this.Touched = true;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public virtual void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        { }

        public static AnyUiLambdaActionPluginUpdateAnyUi NewLambdaUpdateUi(IFormInstanceParent current)
        {
            // expensive to get the plugin name
            var pn = "AasxPluginGenericForms";
            var topmost = FormInstanceHelper.GetTopMostParent(current) as FormInstanceBase;
            if (topmost?.OuterPluginName != null)
                pn = topmost?.OuterPluginName;

            var la = new AnyUiLambdaActionPluginUpdateAnyUi()
            {
                // TODO (MIHO, 2022-06-27): improve, this is not always the case
                PluginName = pn,
                UseInnerGrid = true
            };
            return la;
        }

        public static AasxPluginEventReturnUpdateAnyUi NewResultEventUpdateUi()
        {
            var la = new AasxPluginEventReturnUpdateAnyUi()
            {
                // TODO (MIHO, 2022-06-27): improve, this is not always the case
                PluginName = "AasxPluginGenericForms",
                UseInnerGrid = true
            };
            return la;
        }

        /// <summary>
        /// Use this function to push events to the outside.
        /// It might adapt the plugin name (important to receive back!)
        /// </summary>
        public void PushAndAdaptEventFromTop(
            AasxPluginResultEventBase evt,
            Action<AasxPluginEventReturnBase> subscribeForReturn = null)
        {
            // access
            if (evt == null)
                return;

            // identify top
            var top = FormInstanceHelper.GetTopMostParent(this);
            var topBase = top as FormInstanceBase;
            if (topBase == null)
                return;

            // adapt?
            evt.Session = topBase.OuterPluginSession;
            if (topBase.OuterPluginName != null
                && evt is AasxPluginEventReturnUpdateAnyUi update)
            {
                update.PluginName = topBase.OuterPluginName;
            }


            // push if possible
            topBase.outerEventStack?.PushEvent(evt);

            // subscribe?
            if (subscribeForReturn != null)
                topBase.subscribeForNextEventReturn = subscribeForReturn;
        }
    }

    public class FormDescInstancesPair
    {
        public FormDescBase desc;
        public FormInstanceListOfSame instances;

        public FormDescInstancesPair() { }

        public FormDescInstancesPair(FormDescBase desc, FormInstanceListOfSame instances)
        {
            this.desc = desc;
            this.instances = instances;
        }
    }

    public interface IFormListOfDifferent
    {
        FormInstanceListOfDifferent GetListOfDifferent();
    }

    public class FormInstanceListOfDifferent : List<FormDescInstancesPair>
    {
        public FormDescInstancesPair FindDesc(FormDescBase searchDesc)
        {
            if (searchDesc == null)
                return null;
            foreach (var x in this)
                if (x?.desc == searchDesc)
                    return x;
            return null;
        }

        public FormDescInstancesPair FindInstance(FormInstanceListOfSame searchInst)
        {
            if (searchInst == null)
                return null;
            foreach (var x in this)
                if (x?.instances == searchInst)
                    return x;
            return null;
        }

        /// <summary>
        /// Render the list of form elements into a list of SubmodelElements.
        /// </summary>
        public List<Aas.ISubmodelElement> AddOrUpdateDifferentElementsToCollection(
            List<Aas.ISubmodelElement> elements,
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false)
        {
            // will be a list of newly added elements (for tracing)
            var res = new List<Aas.ISubmodelElement>();

            // each description / instance pair
            foreach (var pair in this)
            {
                // ok, perform the actual add or update procedure
                var lst = pair.instances.AddOrUpdateSameElementsToCollection(elements, packageEnv, addFilesToPackage);

                // for newly added elements, shaping of idSHort might be required
                if (lst != null)
                    foreach (var sme in lst)
                    {
                        // access
                        if (sme.IdShort == null)
                            continue;

                        // check, if to make idShort unique?
                        FormInstanceHelper.MakeIdShortUnique(elements, sme);

                        // add to tracing
                        res.Add(sme);
                    }
            }
            return res;
        }
    }

    public class FormInstanceListOfSame : IFormInstanceParent, IFormRenderAnyUi
    {
        public FormInstanceBase parentForm = null;
        public FormDescBase workingDesc = null;

#if USE_WPF
        public UserControl subControl = null;
#endif

        public IFormInstanceParent GetInstanceParent() { return parentForm; }

        /// <summary>
        /// These instances are sub-ordinate instances to this instance.
        /// </summary>
        [JsonIgnore]
        public List<FormInstanceBase> SubInstances = null;

        /// <summary>
        /// Hold a list of SME, which were source elements to the set of Instances, in order to figure out, if some
        /// Instances based on source elements are missing when updating.
        /// </summary>
        [JsonIgnore]
        protected List<Aas.ISubmodelElement> InitialSourceElements = null;

        /// <summary>
        /// Clears <c>Instances</c>, <c>InitialSourceElements</c> and further dynamically data-
        /// </summary>
        public virtual void ClearDynamicData()
        {
            this.SubInstances = null;
            this.InitialSourceElements = null;
        }

        public FormInstanceListOfSame(FormInstanceBase parentForm, FormDescBase workingDesc)
        {
            this.parentForm = parentForm;
            this.workingDesc = workingDesc;
        }

        /// <summary>
        /// Within this super-ordinate list of (different) instances, identifiy the instances matching
        /// <c>idShortHead</c> and trigger the instance with infex <c>index</c> on its MasterEvent method
        /// </summary>
        public void TriggerSlaveEvents(FormInstanceSubmodelElement masterInst, int masterIndex)
        {
            // access
            var masterDesc = masterInst?.desc as FormDescSubmodelElement;
            var masterIdShort = masterDesc?.PresetIdShort;
            if (masterInst == null || masterDesc == null || masterIndex < 0 || masterIdShort == null)
                return;
            // go up to list of different
            var pf = this.parentForm;
            if (pf == null || !(pf is IFormListOfDifferent))
                return;
            var lod = (pf as IFormListOfDifferent).GetListOfDifferent();
            if (lod == null)
                return;
            // iterate over this list
            foreach (var li in lod)
            {
                var lisme = li.desc as FormDescSubmodelElement;
                if (lisme == null || lisme.PresetIdShort == null)
                    continue;
                if (lisme.SlaveOfIdShort == null)
                    continue;
                if (!masterIdShort.StartsWith(lisme.SlaveOfIdShort))
                    continue;
                if (li.instances == null || li.instances.SubInstances == null ||
                    li.instances.SubInstances.Count <= masterIndex)
                    break;
                var slaveSme = li.instances.SubInstances[masterIndex] as FormInstanceSubmodelElement;
                if (slaveSme != null)
                    slaveSme.OnSlaveEvent(masterInst.desc as FormDescSubmodelElement, masterInst, masterIndex);
            }
        }

        /// <summary>
        /// Checks, if the <c>sourceElements</c> can be used to pre-set instances for the rendering of
        /// the description/ form.
        /// If not, the display functionality will finally care about creating them.
        /// </summary>
        public void PresetInstancesBasedOnSource(List<Aas.ISubmodelElement> sourceElements = null)
        {
            // access
            var desc = this.workingDesc as FormDescSubmodelElement;

            if (desc == null || desc.KeySemanticId == null || sourceElements == null)
                return;

            // Instances ready?
            if (this.SubInstances == null)
                this.SubInstances = new List<FormInstanceBase>();

            // maximum == 1?
            if (desc.Multiplicity == FormMultiplicity.ZeroToOne || desc.Multiplicity == FormMultiplicity.One)
            {
                var sme = sourceElements.FindFirstSemanticId(desc.KeySemanticId, matchMode: MatchMode.Relaxed);
                if (sme != null)
                {
                    var y = desc.CreateInstance(this, sme);
                    if (y != null)
                        this.SubInstances.Add(y);
                }
            }

            // maximum > 1?
            if (desc.Multiplicity == FormMultiplicity.ZeroToMany || desc.Multiplicity == FormMultiplicity.OneToMany)
            {
                foreach (var sme in sourceElements.FindAllSemanticId(desc.KeySemanticId, matchMode: MatchMode.Relaxed))
                    if (sme != null)
                    {
                        var y = desc.CreateInstance(this, sme);
                        if (y != null)
                            this.SubInstances.Add(y);
                    }
            }

            // prepare list of original source elements
            if (this.InitialSourceElements == null)
                this.InitialSourceElements = new List<Aas.ISubmodelElement>();
            foreach (var inst in this.SubInstances)
                if (inst != null && inst is FormInstanceSubmodelElement &&
                    (inst as FormInstanceSubmodelElement).sourceSme != null)
                    this.InitialSourceElements.Add((inst as FormInstanceSubmodelElement).sourceSme);
        }

        /// <summary>
        /// Render the form description and adds or updates its instances into a list of SubmodelElements.
        /// </summary>
        public List<Aas.ISubmodelElement> AddOrUpdateSameElementsToCollection(
            List<Aas.ISubmodelElement> elements, AdminShellPackageEnv packageEnv = null,
            bool addFilesToPackage = false)
        {
            // access
            var res = new List<Aas.ISubmodelElement>();
            if (this.SubInstances == null || this.workingDesc == null)
                return null;

            // over all instances
            foreach (var ins in this.SubInstances)
            {
                // access
                if (!(ins is FormInstanceSubmodelElement))
                    continue;

                // only touched?
                if (!(ins is FormInstanceSubmodelElementCollection) && !ins.Touched)
                    continue;

                // this is not very nice: distinguish between SMECs und SMEs
                if (!(this.workingDesc is FormDescSubmodelElementCollection))
                {
                    var lst1 = (ins as FormInstanceSubmodelElement).AddOrUpdateSmeToCollection(
                        elements, packageEnv, addFilesToPackage);

                    // for monitoring purpose
                    res.AddRange(lst1);
                }
                else
                {
                    // Special case: SMEC

                    // the Same-Instance was already prepared, however it needs to be eventually
                    // filled with the new elements
                    var smecInst = ins as FormInstanceSubmodelElementCollection;
                    var sourceSmec = smecInst?.sourceSme as Aas.SubmodelElementCollection;

                    List<Aas.ISubmodelElement> newElems = null;
                    bool addMode = false;
                    if (sourceSmec == null)
                    {
                        // will become a NEW SMEC !
                        newElems = new List<Aas.ISubmodelElement>();
                        addMode = true;
                    }
                    else
                    {
                        // will be added to an existing SMEC
                        newElems = sourceSmec.Value;
                        addMode = false;
                    }

                    var lst = (ins as FormInstanceSubmodelElement).AddOrUpdateSmeToCollection(
                        newElems, packageEnv, addFilesToPackage);

                    if (newElems != null && newElems.Count > 0)
                    {
                        var smec = smecInst?.sme as Aas.SubmodelElementCollection;

                        // really add a new instances of the SMEC
                        if (addMode && smecInst != null && smec != null)
                        {
                            // add
                            if (smec.Value == null)
                                smec.Value = new List<Aas.ISubmodelElement>();
                            smec.Value.AddRange(newElems);

                            // make smec unique
                            FormInstanceHelper.MakeIdShortUnique(elements, smec);

                            // to (outer) elements
                            elements.Add(smec);
                        }

                        // for monitoring purpose
                        res.AddRange(lst);
                    }
                }
            }

            // now, check if original SMEs are missing in the Instances and have therefore be removed
            // (kind of post-mortem analysis)
            if (this.InitialSourceElements != null)
                foreach (var ise in this.InitialSourceElements)
                {
                    // exclude trivial cases
                    if (ise == null)
                        continue;

                    // manually search
                    var found = false;
                    foreach (var ins in this.SubInstances)
                        if (ins != null && ins is FormInstanceSubmodelElement &&
                            (ins as FormInstanceSubmodelElement).sourceSme == ise)
                            found = true;

                    // if not foudnd, DELETE original element
                    if (!found)
                        elements.Remove(ise);
                }

            // ok
            return res;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        {
            // need some preparation
            int maxRowsBound = 9999;
            var desc = this.workingDesc as FormDescSubmodelElement;
            int minRows = 0, maxRows = 0;
            bool showButtonsMinus = false, showButtonPlus = false;

            // not right?
            if (desc == null)
                return;

            // figure out, how much additional rows to render
            if (desc.Multiplicity == FormMultiplicity.One)
            {
                minRows = 1;
                maxRows = 1;
                showButtonsMinus = false;
                showButtonPlus = false;
            }
            if (desc.Multiplicity == FormMultiplicity.OneToMany)
            {
                minRows = 1;
                maxRows = maxRowsBound;
                showButtonsMinus = true;
                showButtonPlus = true;
            }
            if (desc.Multiplicity == FormMultiplicity.ZeroToOne)
            {
                minRows = 0;
                maxRows = 1;
                showButtonsMinus = true;
                showButtonPlus = true;
            }
            if (desc.Multiplicity == FormMultiplicity.ZeroToMany)
            {
                minRows = 0;
                maxRows = maxRowsBound;
                showButtonsMinus = true;
                showButtonPlus = true;
            }

            if (opctx?.IsDisplayModeEditOrAdd != true)
            {
                showButtonPlus = false;
                showButtonsMinus = false;
            }

            // reserve instances
            if (SubInstances == null)
                SubInstances = new List<FormInstanceBase>();

            while (SubInstances.Count < minRows)
            {
                var ni = desc.CreateInstance(this);
                if (ni == null)
                    break;

                SubInstances.Add(ni);
            }

            // Intended layout:
            // Grid
            //   FormTitle         [+]
            //    FormInfo
            //   Instance 0        [-]
            //   Instance 1        [-]
            //   Instance 2        [-]

            Func<object, AnyUiLambdaActionBase> lambda = null;
            if (showButtonPlus)
                lambda = (o) =>
                {
                    if (SubInstances.Count < maxRows)
                    {
                        // add a instance
                        var ni = desc.CreateInstance(this);
                        if (ni != null)
                        {
                            SubInstances.Add(ni);
                            return FormInstanceBase.NewLambdaUpdateUi(this);
                        }
                    }
                    // else
                    return new AnyUiLambdaActionNone();
                };

            var g = FormInstanceAnyUiHelper.RenderAnyUiHead(
                view, uitk, opctx, desc, null,
                extraRows: SubInstances.Count,
                plusButtonLambda: lambda);

            g.Background = AnyUiBrushes.White;
            g.Margin = new AnyUiThickness(4.0);

            // simply render the instances

            int row = 1;
            foreach (var si in SubInstances)
            {
                // row by row
                if (si == null)
                    continue;
                var storedSI = si;
                row++;

                // Index
                uitk.AddSmallBasicLabelTo(
                    g, row, 1, foreground: AnyUiBrushes.MiddleGray, fontSize: 0.8f,
                    margin: new AnyUiThickness(0.0, 4.0, 0.0, 0.0),
                    content: $"#{1 + si?.Index}");

                // button
                if (showButtonsMinus)
                    AnyUiUIElement.RegisterControl(
                        uitk.AddSmallButtonTo(g, row, 4, setHeight: 23.0, margin: new AnyUiThickness(1.0),
                            verticalAlignment: AnyUiVerticalAlignment.Top,
                            content: "\u2796"),
                    (o) =>
                    {
                        if (SubInstances.Count > minRows && SubInstances.Contains(storedSI))
                        {
                            // delete
                            // Note: the WPF implementation had a try-catch?
                            SubInstances.Remove(storedSI);
                            return FormInstanceBase.NewLambdaUpdateUi(this);
                        }
                        // else
                        return new AnyUiLambdaActionNone();
                    });

                // SME specific panel with contents
                // outer panel
                // Note: make the panel as wide as possible (if multiplicity allows)
                // ReSharper disable ConditionIsAlwaysTrueOrFalse
                var outer = uitk.AddSmallStackPanelTo(
                    g, row, 2, colSpan: 2 + ((!showButtonsMinus && !showButtonPlus) ? 1 : 0));
                // ReSharper enable ConditionIsAlwaysTrueOrFalse

                // need to make a visual separation, if multiple instances are possible
                if (maxRows > 1)
                    outer.Background = AnyUiBrushes.LightGray;

                // inner panel
                var inner = outer.Add(new AnyUi.AnyUiStackPanel());
                inner.Background = AnyUiBrushes.White;
                inner.Margin = new AnyUiThickness(4, 4, 4, 0);

                // idShort
                if (si.desc is FormDescSubmodelElement descsme
                    && si is FormInstanceSubmodelElement sisme
                    && (descsme.FormEditIdShort || descsme.FormEditDescription))
                {
                    FormInstanceAnyUiHelper.RenderAnyUiRefAttribs(
                        inner, uitk, opctx, descsme, sisme.sme, current: this,
                        touch: sisme.Touch,
                        editIdShort: descsme.FormEditIdShort,
                        editDesc: descsme.FormEditDescription);
                }

                // SME specifics itself
                si.RenderAnyUi(inner, uitk, opctx);
            }

        }

    }

    public static class FormInstanceAnyUiHelper
    {
        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public static AnyUiGrid RenderAnyUiHead(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk, PluginOperationContextBase opctx,
            FormDescReferable desc,
            Aas.IReferable rf,
            int? extraRows = null,
            Func<object, AnyUiLambdaActionBase> plusButtonLambda = null,
            Func<object, AnyUiLambdaActionBase> expandButtonLambda = null)
        {
            // access
            if (desc == null)
                return null;

            // create frame
            var g = view.Add(
                uitk.AddSmallGrid(
                    rows: 2 + (extraRows.HasValue ? extraRows.Value : 0),
                    cols: 6,
                    colWidths: new[] { "2:", "20:", "*", "22:", "22:", "2:" }));

            var wp = uitk.AddSmallWrapPanelTo(g, 0, 1, colSpan: 3);

            wp.Add(new AnyUiSelectableTextBlock()
            {
                Foreground = AnyUiBrushes.DarkBlue,
                FontSize = 1.3f,
                Margin = new AnyUiThickness(0, 0, 10, 0),
                Text = $"{desc?.FormTitle} {"" + rf?.IdShort}"
            });

            if (desc.FormUrl.HasContent())
                AnyUiUIElement.RegisterControl(
                    wp.Add(new AnyUiButton()
                    {
                        MinHeight = 18.0,
                        MaxHeight = 18.0,
                        MinWidth = 18.0,
                        // MaxWidth = 18.0,
                        FontSize = 1.3f,
                        Padding = new AnyUiThickness(0, -4, 0, 0),
                        Content = "\u21a6"
                    }),
                    (o) => new AnyUiLambdaActionDisplayContentFile()
                    {
                        fn = desc.FormUrl,
                        mimeType = System.Net.Mime.MediaTypeNames.Text.Html,
                        preferInternalDisplay = true
                    });

            if (plusButtonLambda != null && opctx?.IsDisplayModeEditOrAdd == true)
            {
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(g, 0, 4, setHeight: 23.0, margin: new AnyUiThickness(1.0),
                        content: "\u2795"), plusButtonLambda);
            }
            else
            if (expandButtonLambda != null)
            {
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(g, 0, 4, setHeight: 22.0, margin: new AnyUiThickness(2.0),
                        content: "\u2bc5"), expandButtonLambda);
            }

            uitk.AddSmallBasicLabelTo(g, 1, 2, foreground: AnyUiBrushes.DarkBlue, fontSize: 0.8f,
                textWrapping: AnyUiTextWrapping.Wrap,
                content: $"{desc?.FormInfo}");

            return g;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public static AnyUiGrid RenderAnyUiRefAttribs(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk, PluginOperationContextBase opctx,
            FormDescReferable desc,
            Aas.IReferable rf,
            IFormInstanceParent current,
            Action touch = null,
            bool editIdShort = false,
            bool editDesc = false)
        {
            // access
            if (desc == null || rf == null)
                return null;

            if (!editIdShort && !editDesc)
                return null;

            // create frame
            var g = view.Add(
                uitk.AddSmallGrid(
                    rows: 2
                        + (editIdShort ? 1 : 0)
                        + (editDesc ? 1 : 0)
                        + (editDesc && rf.Description != null ? rf.Description.Count : 0),
                    cols: 6,
                    colWidths: new[] { "2:", "60:", "60:", "*", "22:", "2:" }));

            int row = 0;

            // idShort
            if (editIdShort)
            {
                uitk.AddSmallBasicLabelTo(g, row, 1,
                    foreground: AnyUiBrushes.DarkGray, fontSize: 0.8f,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    content: "idShort:");

                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallTextBoxTo(g, row, 2, colSpan: 2,
                        margin: new AnyUiThickness(1.0),
                        text: "" + rf.IdShort),
                    (o) =>
                    {
                        if (o is string os)
                            rf.IdShort = os;
                        touch?.Invoke();
                        return new AnyUiLambdaActionNone();
                    });

                // finally
                row++;
            }

            // Description
            if (editDesc)
            {
                // Label in 1st row
                uitk.AddSmallBasicLabelTo(g, row, 1,
                    foreground: AnyUiBrushes.DarkGray, fontSize: 0.8f,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    content: "Description:");

                // info text only, if now langauges
                if (rf.Description == null || rf.Description.Count < 1)
                    uitk.AddSmallBasicLabelTo(g, row, 2, colSpan: 2,
                        foreground: AnyUiBrushes.DarkGray, fontSize: 0.8f,
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center,
                        horizontalAlignment: AnyUiHorizontalAlignment.Right,
                        textWrapping: AnyUiTextWrapping.Wrap, margin: new AnyUiThickness(0, 0, 4, 0),
                        content: "('+' to add language set)");

                // "Plus" in first row
                if (opctx?.IsDisplayModeEditOrAdd == true)
                    AnyUiUIElement.RegisterControl(
                        uitk.AddSmallButtonTo(g, row, 4,
                            setHeight: 23.0, margin: new AnyUiThickness(1.0),
                            content: "\u2795"),
                            (o) =>
                            {
                                rf.AddDescription("", "");
                                touch?.Invoke();
                                return FormInstanceBase.NewLambdaUpdateUi(current);
                            });

                // finally
                row++;

                // list single languages
                if (rf.Description != null)
                    foreach (var ls in rf.Description)
                    {
                        // lang
                        AnyUiUIElement.RegisterControl(
                            uitk.AddSmallComboBoxTo(g, row, 2, margin: new AnyUiThickness(1.0),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                                text: "" + ls.Language,
                                items: AasxLanguageHelper.GetLangCodes().ToArray()),
                            (o) =>
                            {
                                if (o is string os)
                                    ls.Language = os;
                                touch?.Invoke();
                                return new AnyUiLambdaActionNone();
                            });

                        // key
                        AnyUiUIElement.RegisterControl(
                            uitk.AddSmallTextBoxTo(g, row, 3, margin: new AnyUiThickness(1.0),
                                text: "" + ls.Text),
                            (o) =>
                            {
                                if (o is string os)
                                    ls.Text = os;
                                touch?.Invoke();
                                return new AnyUiLambdaActionNone();
                            });

                        // button
                        if (opctx?.IsDisplayModeEditOrAdd == true)
                        {
                            var storedLs = ls;
                            AnyUiUIElement.RegisterControl(
                                uitk.AddSmallButtonTo(g, row, 4, setHeight: 23.0, margin: new AnyUiThickness(1.0),
                                    content: "\u2796"),
                                (o) =>
                                {
                                    if (rf.Description.Contains(storedLs))
                                        rf.Description.Remove(storedLs);
                                    touch?.Invoke();
                                    return FormInstanceBase.NewLambdaUpdateUi(current);
                                });
                        }

                        // row by row
                        row++;
                    }
            }

            // ok
            return g;
        }
    }

    public class FormInstanceSubmodel : FormInstanceBase, IFormListOfDifferent
    {
        /// <summary>
        /// The Submodel is maintained by the instance
        /// </summary>
        public Aas.Submodel sm = null;

        /// <summary>
        /// This links to a Submodel, from which the instance was read/ edited.
        /// </summary>
        public Aas.Submodel sourceSM = null;

        public FormInstanceListOfDifferent PairInstances = new FormInstanceListOfDifferent();

        public FormInstanceSubmodel() { }

        public FormInstanceSubmodel(FormDescSubmodel desc)
            : base(null, desc)
        {
            // require desc!
            if (desc == null || desc.SubmodelElements == null)
                // done
                return;

            foreach (var subDesc in desc.SubmodelElements)
            {
                var los = new FormInstanceListOfSame(this, subDesc);
                var pair = new FormDescInstancesPair(subDesc, los);
                PairInstances.Add(pair);
            }
        }

        public void InitReferable(FormDescSubmodel desc, Aas.Submodel source)
        {
            if (desc == null)
                return;

            // create sm here! (different than handling of SME!!)
            this.sm = new Aas.Submodel("");
            this.sourceSM = source;

            sm.IdShort = desc.PresetIdShort;
            if (source?.IdShort != null)
                sm.IdShort = source.IdShort;
            sm.Category = desc.PresetCategory;
            if (desc.PresetDescription != null)
                sm.Description = desc.PresetDescription.Copy();
            if (source?.Description != null)
                sm.Description = source.Description.Copy();

            if (desc.KeySemanticId != null)
                sm.SemanticId = ExtendReference.CreateFromKey(desc.KeySemanticId);
        }

        public FormInstanceListOfDifferent GetListOfDifferent()
        {
            return PairInstances;
        }

        /// <summary>
        /// Checks, if the <c>sourceElements</c> can be used to pre-set instances for the rendering
        /// of the description/ form.
        /// If not, the display functionality will finally care about creating them.
        /// </summary>
        public void PresetInstancesBasedOnSource(List<Aas.ISubmodelElement> sourceElements = null)
        {
            if (this.PairInstances != null)
                foreach (var pair in this.PairInstances)
                {
                    pair?.instances?.PresetInstancesBasedOnSource(sourceElements);
                }
        }

        /// <summary>
        /// Render the list of form elements into a list of SubmodelElements.
        /// </summary>
        public List<Aas.ISubmodelElement> AddOrUpdateDifferentElementsToCollection(
            List<Aas.ISubmodelElement> elements,
            AdminShellPackageEnv packageEnv = null,
            bool addFilesToPackage = false,
            bool editSource = false)
        {
            // SM itself?
            if (this.sm != null && Touched && this.sourceSM != null && editSource)
            {
                if (this.sm.IdShort != null)
                    this.sourceSM.IdShort = "" + this.sm.IdShort;
                if (this.sm.Description != null)
                    this.sourceSM.Description = sm.Description.Copy();
            }

            // SM as a set of elements
            if (this.PairInstances != null)
                return this.PairInstances.AddOrUpdateDifferentElementsToCollection(
                        elements, packageEnv, addFilesToPackage);
            return null;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public override void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
                PluginOperationContextBase opctx)
        {
            // Intended layout
            // Grid
            //    FormTools
            //    SubmodelName
            //       Quite informative text
            //    idShort
            //    Descriptions   [+]
            //      single Desc  [-] 
            //      single Desc  [-] 
            //    SME 0
            //    SME 1

            var sp = view.Add(
                new AnyUi.AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical });

            var descsm = desc as FormDescSubmodel;

            if (true)
            {
                var g = sp.Add(
                    uitk.AddSmallGrid(rows: 1, cols: 2, colWidths: new[] { "*", "#", "#", "#" }));

                uitk.AddSmallBasicLabelTo(g, 0, 0,
                    content: $"Form tools");

                uitk.AddSmallButtonTo(g, 0, 1,
                    content: "Small/ Large");
            }

            // Block with Submodel name, info, url, plus

            if (true)
            {
                FormInstanceAnyUiHelper.RenderAnyUiHead(sp, uitk, opctx, descsm, sm);
            }

            // idShort / Description

            foreach (var pair in this.PairInstances)
            {
                pair.instances.RenderAnyUi(view, uitk, opctx);
            }
        }
    }

    public class FormInstanceSubmodelElement : FormInstanceBase
    {
        /// <summary>
        /// The SME which is maintained by the instance
        /// </summary>
        public Aas.ISubmodelElement sme = null;

        /// <summary>
        /// This links to a SME, from which the instance was read/ edited.
        /// </summary>
        public Aas.ISubmodelElement sourceSme = null;

        protected void InitReferable(FormDescSubmodelElement desc, Aas.ISubmodelElement source)
        {
            if (desc == null || sme == null)
                return;

            sme.IdShort = desc.PresetIdShort;
            if (source?.IdShort != null)
                sme.IdShort = source.IdShort;
            sme.Category = desc.PresetCategory;
            if (desc.PresetDescription != null)
                sme.Description = desc.PresetDescription.Copy();
            if (source?.Description != null)
                sme.Description = source.Description.Copy();

            if (desc.KeySemanticId != null)
                sme.SemanticId = ExtendReference.CreateFromKey(desc.KeySemanticId);
        }

        /// <summary>
        /// Before rendering the SME into a list of new elements, process the SME.
        /// If <c>Touched</c>, <c>sourceSme</c> and <c>editSource</c> is set, this function shall write back
        /// the new values instead of producing a new element.
        /// </summary>
        /// <returns>True, if a new element shall be rendered from the instance <c>sme</c>.</returns>
        public virtual bool ProcessSmeForRender(
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false, bool editSource = false)
        {
            if (this.sme != null && Touched && this.sourceSme != null && editSource)
            {
                if (this.sme.IdShort != null)
                    this.sourceSme.IdShort = "" + this.sme.IdShort;
                if (this.sme.Description != null)
                    this.sourceSme.Description = this.sme.Description.Copy();
            }
            return false;
        }

        /// <summary>
        /// Render the instance into a list (right now, exactly one!) of SubmodelElements.
        /// Might be overridden in subclasses.
        /// </summary>
        public virtual List<Aas.ISubmodelElement> AddOrUpdateSmeToCollection(
            List<Aas.ISubmodelElement> collectionNewElements,
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false)
        {
            // typically, there will be only one SME
            var res = new List<Aas.ISubmodelElement>();

            // SME present?
            if (sme != null)
            {
                // process (will update existing elements)
                var doAdd = ProcessSmeForRender(packageEnv, addFilesToPackage, editSource: true);

                // still add?
                if (doAdd)
                {
                    // add to elements (this is the real transaction)
                    collectionNewElements.Add(sme.Copy());

                    // add to the tracing information for new elements
                    res.Add(sme.Copy());
                }
            }

            // OK
            return res;
        }

        /// <summary>
        /// Event was raised from master because matching slave.
        /// </summary>
        public virtual void OnSlaveEvent(
            FormDescSubmodelElement masterDesc, FormInstanceSubmodelElement masterInst, int index)
        {
        }
    }

    public class FormInstanceSubmodelElementCollection : FormInstanceSubmodelElement, IFormListOfDifferent
    {
        public FormInstanceListOfDifferent PairInstances = new FormInstanceListOfDifferent();

        public FormInstanceSubmodelElementCollection(
            FormInstanceListOfSame parentInstance,
            FormDescSubmodelElementCollection parentDesc, Aas.ISubmodelElement source = null)
        {
            // way back to description
            this.desc = parentDesc;
            this.parentInstance = parentInstance;
            var smecDesc = this.desc as FormDescSubmodelElementCollection;

            // initialize Referable
            var smec = new Aas.SubmodelElementCollection();
            this.sme = smec;
            InitReferable(parentDesc, source);

            // initially create pairs
            if (smecDesc?.value != null)
                foreach (var subDesc in smecDesc.value)
                {
                    var los = new FormInstanceListOfSame(this, subDesc);
                    var pair = new FormDescInstancesPair(subDesc, los);
                    PairInstances.Add(pair);
                }

            // check, if a source is present
            this.sourceSme = source;
            var smecSource = this.sourceSme as Aas.SubmodelElementCollection;
            if (smecSource != null)
            {
                if (this.PairInstances != null)
                    foreach (var pair in this.PairInstances)
                    {
                        pair?.instances?.PresetInstancesBasedOnSource(smecSource.Value);
                    }
            }

            // create user control
#if USE_WPF
            if (createSubControls)
            {
                this.subControl = new FormSubControlSMEC();
                this.subControl.DataContext = this;
            }
#endif
        }

        public FormInstanceListOfDifferent GetListOfDifferent()
        {
            return PairInstances;
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>

        public override List<Aas.ISubmodelElement> AddOrUpdateSmeToCollection(
            List<Aas.ISubmodelElement> elements, AdminShellPackageEnv packageEnv = null,
            bool addFilesToPackage = false)
        {
            // SMEC as Refrable
            this.ProcessSmeForRender(packageEnv: null, addFilesToPackage: false, editSource: true);

            // SMEC as list of items
            if (this.PairInstances != null)
                return this.PairInstances.AddOrUpdateDifferentElementsToCollection(
                    elements, packageEnv, addFilesToPackage);

            return null;
        }

        /// <summary>
        /// Checks, if the <c>sourceElements</c> can be used to pre-set instances for the rendering
        /// of the description/ form.
        /// If not, the display functionality will finally care about creating them.
        /// </summary>
        public void PresetInstancesBasedOnSource(List<Aas.ISubmodelElement> sourceElements = null)
        {
            if (this.PairInstances != null)
                foreach (var pair in this.PairInstances)
                {
                    pair?.instances?.PresetInstancesBasedOnSource(sourceElements);
                }
        }

        /// <summary>
        /// Render the list of form elements into a list of SubmodelElements.
        /// </summary>
        public List<Aas.ISubmodelElement> AddOrUpdateDifferentElementsToCollection(
            List<Aas.ISubmodelElement> elements, AdminShellPackageEnv packageEnv = null,
            bool addFilesToPackage = false)
        {
            if (this.PairInstances != null)
                return this.PairInstances.AddOrUpdateDifferentElementsToCollection(
                    elements, packageEnv, addFilesToPackage);
            return null;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public override void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        {
            // Intended layout
            // Grid
            //    SubmodelName
            //       Quite informative text
            //    idShort
            //    Descriptions   [+]
            //      single Desc  [-] 
            //      single Desc  [-] 
            //    SME 0
            //    SME 1

            view.Add(new AnyUi.AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical });

#if NOT_REQUIRED
            if (false)
            {
                var descsm = desc as FormDescSubmodelElement;
                FormInstanceAnyUiHelper.RenderAnyUiHead(sp, uitk, opctx, descsm, sme);
            }
#endif

            // idShort / Description

            foreach (var pair in this.PairInstances)
            {
                // Note: make the panel as wide as possible (if multiplicity allows)
                var outer = view.Add(new AnyUi.AnyUiStackPanel());
                outer.Background = AnyUiBrushes.LightGray;

                // inner panel
                var inner = outer.Add(new AnyUi.AnyUiStackPanel());
                inner.Background = AnyUiBrushes.White;
                inner.Margin = new AnyUiThickness(4, 4, 4, 0);

                pair.instances.RenderAnyUi(inner, uitk, opctx);
            }
        }
    }

    public class FormInstanceProperty : FormInstanceSubmodelElement
    {
        public FormInstanceProperty(
            FormInstanceListOfSame parentInstance, FormDescProperty parentDesc,
            Aas.ISubmodelElement source = null, bool deepCopy = false)
        {
            // way back to description
            this.parentInstance = parentInstance;
            this.desc = parentDesc;

            // initialize Referable
            var p = new Aas.Property(Aas.DataTypeDefXsd.String);
            this.sme = p;
            InitReferable(parentDesc, source);

            // check, if a source is present
            this.sourceSme = source;
            var pSource = this.sourceSme as Aas.Property;
            if (pSource != null)
            {
                // take over
                p.ValueType = pSource.ValueType;
                p.Value = pSource.Value;
            }
            else
            {
                // some more preferences
                if (parentDesc.allowedValueTypes != null && parentDesc.allowedValueTypes.Length >= 1)
                    p.ValueType = Aas.Stringification.DataTypeDefXsdFromString(parentDesc.allowedValueTypes[0])
                        ?? Aas.DataTypeDefXsd.String;

                if (parentDesc.presetValue != null && parentDesc.presetValue.Length > 0)
                {
                    p.Value = parentDesc.presetValue;
                    // immediately set touched in order to have this value saved
                    this.Touch();
                }
            }

            // create user control
#if USE_WPF
            if (createSubControls)
            {
                this.subControl = new FormSubControlProperty();
                this.subControl.DataContext = this;
            }
#endif
        }

        /// <summary>
        /// Before rendering the SME into a list of new elements, process the SME.
        /// If <c>Touched</c>, <c>sourceSme</c> and <c>editSource</c> is set,
        /// this function shall write back the new values instead of
        /// producing a new element. Returns True, if a new element shall be rendered.
        /// </summary>
        public override bool ProcessSmeForRender(
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false, bool editSource = false)
        {
            // refer to base (SME) function, but not caring about result
            base.ProcessSmeForRender(packageEnv, addFilesToPackage, editSource);

            var p = this.sme as Aas.Property;
            var pSource = this.sourceSme as Aas.Property;
            if (p != null && Touched && pSource != null && editSource)
            {
                pSource.ValueType = p.ValueType;
                pSource.Value = p.Value;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Event was raised from master because matching slave.
        /// </summary>
        public override void OnSlaveEvent(
            FormDescSubmodelElement masterDesc, FormInstanceSubmodelElement masterInst, int index)
        {
            // access to master
            var pMasterInst = masterInst as FormInstanceProperty;
            var pMaster = pMasterInst?.sme as Aas.Property;
            if (pMaster?.Value == null)
                return;

            // accues to this
            var pThis = this.sme as Aas.Property;
            if (pThis == null)
                return;

            // desc of this
            var pDesc = this.desc as FormDescProperty;
            if (pDesc == null || pDesc.valueFromMasterValue == null ||
                !pDesc.valueFromMasterValue.ContainsKey(pMaster.Value.Trim()))
                return;

            // simply take value
            pThis.Value = pDesc.valueFromMasterValue[pMaster.Value.Trim()];
            this.Touch();

            // refresh
#if USE_WPF
            if (this.subControl != null && this.subControl is FormSubControlProperty scp)
                scp.UpdateDisplay();
#endif
            if (MainControl is AnyUiTextBox mtb)
            {
                mtb.Text = pThis.Value;
                mtb.Touch();
            }

            if (MainControl is AnyUiComboBox mcb && mcb.IsEditable == true)
            {
                mcb.Text = pThis.Value;
                mcb.Touch();
            }
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public override void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        {
            // access
            var prop = sme as Aas.Property;
            var pDesc = desc as FormDescProperty;
            if (prop == null || pDesc == null)
                return;

            // Intended layout
            // Grid
            //    Index
            //    TextBox | ComboBox

            var g = view.Add(
                uitk.AddSmallGrid(rows: 1, cols: 3, colWidths: new[] { "2:", "*", "2:" }));

            // which control?
            if (pDesc.comboBoxChoices != null && pDesc.comboBoxChoices.Length > 0)
            {
                var editableMode = (pDesc.valueFromComboBoxIndex == null ||
                    pDesc.valueFromComboBoxIndex.Length < 1);

                MainControl = AnyUiUIElement.RegisterControl(
                    uitk.AddSmallComboBoxTo(g, 0, 1,
                        margin: new AnyUiThickness(0, 2, 0, 2),
                        horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                        items: pDesc.comboBoxChoices,
                        isEditable: editableMode),
                        (o) =>
                        {
                            if (!(MainControl is AnyUiComboBox mcb) || mcb.SelectedIndex == null)
                                return new AnyUiLambdaActionNone();

                            var idx = mcb.SelectedIndex.Value;
                            var items = pDesc.valueFromComboBoxIndex;
                            if (items != null && items.Length > 0 && idx >= 0 && idx < items.Length && !editableMode)
                            {
                                Touch();
                                prop.Value = "" + items[idx];

                                // dependent values
                                parentInstance?.TriggerSlaveEvents(this, Index);

                                // Note: sending an lambda update will not work (is ignored by combox box), therefore
                                // emitting event via evewnt stack
                                PushAndAdaptEventFromTop(NewResultEventUpdateUi());
                            }

                            return new AnyUiLambdaActionNone();
                        });

                // may select a specific item given by a dedicated value range?
                var mcb2 = MainControl as AnyUiComboBox;
                if (mcb2 != null)
                {
                    if (pDesc.valueFromComboBoxIndex != null && pDesc.valueFromComboBoxIndex.Length >= 1)
                    {
                        for (int i = 0; i < pDesc.valueFromComboBoxIndex.Length; i++)
                            if (pDesc.valueFromComboBoxIndex[i].Trim() == prop.Value)
                            {
                                mcb2.SelectedIndex = i;
                                break;
                            }
                    }
                    else
                    {
                        // editable combo box, initialize normal
                        mcb2.Text = "" + prop.Value;
                    }
                }
            }
            else
            {
                MainControl = AnyUiUIElement.RegisterControl(
                    uitk.AddSmallTextBoxTo(g, 0, 1,
                    margin: new AnyUiThickness(0, 2, 0, 2),
                    text: "" + prop.Value),
                    (o) =>
                    {
                        if (o is string os)
                            prop.Value = os;
                        Touch();
                        return new AnyUiLambdaActionNone();
                    });
            }
        }
    }

    public class FormInstanceMultiLangProp : FormInstanceSubmodelElement
    {
        public FormInstanceMultiLangProp(
            FormInstanceListOfSame parentInstance, FormDescMultiLangProp parentDesc,
            Aas.ISubmodelElement source = null, bool deepCopy = false)
        {
            // way back to description
            this.parentInstance = parentInstance;
            this.desc = parentDesc;

            // initialize Referable
            var mlp = new Aas.MultiLanguageProperty();
            this.sme = mlp;
            InitReferable(parentDesc, source);

            // check, if a source is present
            this.sourceSme = source;
            var mlpSource = this.sourceSme as Aas.MultiLanguageProperty;
            if (mlpSource != null)
            {
                // take over
                if (mlpSource.Value != null)
                    mlp.Value = mlpSource.Value.Copy();
            }

            // create user control
#if USE_WPF
            if (createSubControls)
            {
                this.subControl = new FormSubControlMultiLangProp();
                this.subControl.DataContext = this;
            }
#endif
        }

        /// <summary>
        /// Before rendering the SME into a list of new elements, process the SME.
        /// If <c>Touched</c>, <c>sourceSme</c> and <c>editSource</c> is set, this function shall write back
        /// the new values instead of producing a new element. Returns True, if a new element shall be rendered.
        /// </summary>
        public override bool ProcessSmeForRender(
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false, bool editSource = false)
        {
            // refer to base (SME) function, but not caring about result
            base.ProcessSmeForRender(packageEnv, addFilesToPackage, editSource);

            var mlp = this.sme as Aas.MultiLanguageProperty;
            var mlpSource = this.sourceSme as Aas.MultiLanguageProperty;
            if (mlp != null && Touched && mlpSource != null && editSource)
            {
                if (mlp.Value != null)
                    mlpSource.Value = mlp.Value.Copy();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public override void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        {
            // access
            var mlp = sme as Aas.MultiLanguageProperty;
            if (mlp == null)
                return;

            // Intended layout:
            // Grid
            //   EMPTY             [+]
            //   LANG0 VAL0        [-]
            //   LANG1 VAL1        [-]
            //   LANG2 VAL2        [-]

            var g = view.Add(
                uitk.AddSmallGrid(rows: 1 + (mlp.Value?.Count ?? 0), cols: 5,
                    colWidths: new[] { "2:", "60:", "*", "23:", "2:" }));

            // Label in 1st row
            uitk.AddSmallBasicLabelTo(g, 0, 1,
                foreground: AnyUiBrushes.DarkGray, fontSize: 0.8f,
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                content: "Value:");

            // "Plus" button in 1st row
            if (opctx?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(g, 0, 3, setHeight: 23.0, margin: new AnyUiThickness(1.0),
                        content: "\u2795"),
                        (o) =>
                        {
                            if (mlp.Value == null)
                                mlp.Value = new List<Aas.LangString>();
                            mlp.Value.Add(new Aas.LangString("", ""));
                            Touch();
                            return NewLambdaUpdateUi(this);
                        });

            // no content? .. info on 1st row
            if (mlp.Value == null || mlp.Value.Count < 1)
            {
                uitk.AddSmallBasicLabelTo(g, 0, 2,
                    foreground: AnyUiBrushes.MiddleGray, fontSize: 0.8f,
                    textWrapping: AnyUiTextWrapping.Wrap, margin: new AnyUiThickness(2.0, 4.0, 0.0, 0.0),
                    content: "(add at least one language)");
                return;
            }

            // simply render the langStrs

            int row = 0;
            foreach (var ls in mlp.Value)
            {
                // row by row
                row++;

                // lang
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallComboBoxTo(g, row, 1, margin: new AnyUiThickness(1.0),
                        horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                        text: "" + ls.Language,
                        items: AasxLanguageHelper.GetLangCodes().ToArray()),
                    (o) =>
                    {
                        if (o is string os)
                            ls.Language = os;
                        Touch();
                        return new AnyUiLambdaActionNone();
                    });

                // key
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallTextBoxTo(g, row, 2, margin: new AnyUiThickness(1.0),
                        text: "" + ls.Text),
                    (o) =>
                    {
                        if (o is string os)
                            ls.Text = os;
                        Touch();
                        return new AnyUiLambdaActionNone();
                    });

                // button
                if (opctx?.IsDisplayModeEditOrAdd == true)
                {
                    var storedLs = ls;
                    AnyUiUIElement.RegisterControl(
                        uitk.AddSmallButtonTo(g, row, 3, setHeight: 23.0, margin: new AnyUiThickness(1.0),
                            content: "\u2796"),
                        (o) =>
                        {
                            if (mlp.Value.Contains(storedLs))
                                mlp.Value.Remove(storedLs);
                            Touch();
                            return NewLambdaUpdateUi(this);
                        });
                }
            }
        }
    }

    public class FormInstanceFile : FormInstanceSubmodelElement
    {
        /// <summary>
        /// Holds the file, which shall be loaded into the package __after__ editing.
        /// </summary>
        public string FileToLoad = null;

        public FormInstanceFile(
            FormInstanceListOfSame parentInstance, FormDescFile parentDesc,
            Aas.ISubmodelElement source = null, bool deepCopy = false)
        {
            // way back to description
            this.parentInstance = parentInstance;
            this.desc = parentDesc;

            // initialize Refwrable
            var file = new Aas.File("");
            this.sme = file;
            InitReferable(parentDesc, source);

            // check, if a source is present
            this.sourceSme = source;
            var fileSource = this.sourceSme as Aas.File;
            if (fileSource != null)
            {
                // take over
                file.Value = fileSource.Value;
            }

            // create user control
#if USE_WPF
            if (createSubControls)
            {
                this.subControl = new FormSubControlFile();
                this.subControl.DataContext = this;
            }
#endif
        }

        /// <summary>
        /// Before rendering the SME into a list of new elements, process the SME.
        /// If <c>Touched</c>, <c>sourceSme</c> and <c>editSource</c> is set,
        /// this function shall write back the new values instead of
        /// producing a new element. Returns True, if a new element shall be rendered.
        /// </summary>
        public override bool ProcessSmeForRender(
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false, bool editSource = false)
        {
            // refer to base (SME) function, but not caring about result
            base.ProcessSmeForRender(packageEnv, addFilesToPackage, editSource);

            // access
            var file = this.sme as Aas.File;
            var fileSource = this.sourceSme as Aas.File;

            // need to do more than the base implementation!
            if (file != null)
            {
                if (packageEnv != null)
                {
                    // source path as given by the user
                    var sourcePath = this.FileToLoad?.Trim();

                    if (sourcePath != null && sourcePath.Length > 0)
                    {
                        // target path challenge: shall be unqiue
                        try
                        {
                            var onlyFn = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
                            var onlyExt = System.IO.Path.GetExtension(sourcePath);
                            var salt = Guid.NewGuid().ToString().Substring(0, 8);
                            var targetPath = "/aasx/files/";
                            var targetFn = String.Format("{0}_{1}{2}", onlyFn, salt, onlyExt);

                            // have package to adopt the file name
                            packageEnv.PrepareSupplementaryFileParameters(ref targetPath, ref targetFn);

                            // save
                            file.Value = targetPath + targetFn;

                            if (addFilesToPackage)
                            {
                                packageEnv.AddSupplementaryFileToStore(
                                    sourcePath, targetPath, targetFn, embedAsThumb: false);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(
                                $"FormInstanceFile.RenderAasSmeCollection failed while " +
                                $"writing package for {sourcePath}", ex);
                        }
                    }
                }
            }

            // now, may be edit instead of new
            if (file != null && Touched && fileSource != null && editSource)
            {
                fileSource.Value = file.Value;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public override void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        {
            // access
            var file = sme as Aas.File;
            if (file == null)
                return;

            // Intended layout:
            // Grid
            //   Drop target       [Clear]
            //   """""""""""       [Select]

            var g = view.Add(
                uitk.AddSmallGrid(rows: 2, cols: 4,
                    colWidths: new[] { "2:", "*", "#", "2:" }));

            // prepare file display
            var finfo = "Drag a file to register loading it!";
            if (file.Value.HasContent())
                finfo = "File current: " + file.Value;
            if (FileToLoad != null)
                finfo = "File to load: " + FileToLoad;

            // drop target
            AnyUiUIElement.RegisterControl(
                uitk.AddSmallDropBoxTo(
                    g, 0, 1, rowSpan: 2,
                    margin: new AnyUiThickness(2, 2, 2, 2), minHeight: 40,
                    borderThickness: new AnyUiThickness(1),
                    text: finfo),
                (o) =>
                {
                    if (o is string os)
                        FileToLoad = os;
                    Touch();
                    return NewLambdaUpdateUi(this);
                });

            // "Clear" button in 1st row
            if (opctx?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(g, 0, 2, margin: new AnyUiThickness(1.0),
                        content: "Clear"),
                        (o) =>
                        {
                            file.Value = "";
                            FileToLoad = null;
                            Touch();
                            return NewLambdaUpdateUi(this);
                        });

            // "Select" button in 2nd row
            if (opctx?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(g, 1, 2, margin: new AnyUiThickness(1.0),
                        content: "Select"),
                        (o) =>
                        {
                            // kick off dialogue in main application
                            var tempI = this;
                            tempI.PushAndAdaptEventFromTop(
                                new AasxIntegrationBase.AasxPluginResultEventSelectFile(),
                                (revt) =>
                                {
                                    if (revt is AasxPluginEventReturnSelectFile rsel
                                        && rsel.FileNames != null && rsel.FileNames.Length > 0)
                                    {
                                        // do it
                                        FileToLoad = rsel.FileNames[0];
                                        Touch();

                                        // send event to re-render
                                        tempI.PushAndAdaptEventFromTop(NewResultEventUpdateUi());
                                    }
                                });

                            return new AnyUiLambdaActionNone();
                        });
        }
    }

    public class FormInstanceReferenceElement : FormInstanceSubmodelElement
    {
        public FormInstanceReferenceElement(
            FormInstanceListOfSame parentInstance, FormDescReferenceElement parentDesc,
            Aas.ISubmodelElement source = null, bool deepCopy = false)
        {
            // way back to description
            this.parentInstance = parentInstance;
            this.desc = parentDesc;

            // initialize Referable
            var re = new Aas.ReferenceElement();
            this.sme = re;
            InitReferable(parentDesc, source);

            // check, if a source is present
            this.sourceSme = source;
            var reSource = this.sourceSme as Aas.ReferenceElement;
            if (reSource != null)
            {
                // take over
                if (reSource.Value != null)
                    re.Value = reSource.Value.Copy();
            }

            // create user control
#if USE_WPF
            if (createSubControls)
            {
                this.subControl = new FormSubControlReferenceElement();
                this.subControl.DataContext = this;
            }
#endif
        }

        /// <summary>
        /// Before rendering the SME into a list of new elements, process the SME.
        /// If <c>Touched</c>, <c>sourceSme</c> and <c>editSource</c> is set, this function shall write back
        /// the new values instead of producing a new element. Returns True, if a new element shall be rendered.
        /// </summary>
        public override bool ProcessSmeForRender(
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false, bool editSource = false)
        {
            // refer to base (SME) function, but not caring about result
            base.ProcessSmeForRender(packageEnv, addFilesToPackage, editSource);

            var re = this.sme as Aas.ReferenceElement;
            var reSource = this.sourceSme as Aas.ReferenceElement;
            if (re != null && Touched && reSource != null && editSource)
            {
                if (re.Value != null)
                    reSource.Value = re.Value.Copy();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public override void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        {
            // access
            var refe = sme as Aas.ReferenceElement;
            if (refe == null)
                return;

            // Intended layout:
            // Grid
            //   Reference info    [Clear]
            //   """"""""""""""    [Select]

            var g = view.Add(
                uitk.AddSmallGrid(rows: 2, cols: 4,
                    colWidths: new[] { "2:", "*", "#", "2:" }));

            // prepare reference display
            var info = "(no reference set)";
            if (refe.Value != null)
                info = "(no Keys)";
            if (refe.Value?.Keys != null && refe.Value.Keys.Count > 0)
                info = refe.Value.ToStringExtended(delimiter: System.Environment.NewLine);

            // Use a drop box as info; but now allow drop
            uitk.AddSmallDropBoxTo(
                g, 0, 1, rowSpan: 2,
                margin: new AnyUiThickness(2, 2, 2, 2), minHeight: 40,
                borderThickness: new AnyUiThickness(1),
                text: info);

            // "Clear" button in 1st row
            if (opctx?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(g, 0, 2, margin: new AnyUiThickness(1.0),
                        content: "Clear"),
                        (o) =>
                        {
                            refe.Value = null;
                            Touch();
                            return NewLambdaUpdateUi(this);
                        });

            // "Select" button in 2nd row
            if (opctx?.IsDisplayModeEditOrAdd == true)
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(g, 1, 2, margin: new AnyUiThickness(1.0),
                        content: "Select"),
                        (o) =>
                        {
                            // kick off dialogue in main application
                            var tempI = this;
                            tempI.PushAndAdaptEventFromTop(
                                new AasxIntegrationBase.AasxPluginResultEventSelectAasEntity()
                                {
                                    filterEntities = "All",
                                    showAuxPackage = true,
                                    showRepoFiles = true
                                },
                                (revt) =>
                                {
                                    if (revt is AasxPluginEventReturnSelectAasEntity rsel && rsel.resultKeys != null)
                                    {
                                        // do it
                                        refe.Value = ExtendReference.CreateNew(rsel.resultKeys);
                                        Touch();

                                        // send event to re-render
                                        tempI.PushAndAdaptEventFromTop(NewResultEventUpdateUi());
                                    }
                                });

                            return new AnyUiLambdaActionNone();
                        });
        }
    }

    public class FormInstanceRelationshipElement : FormInstanceSubmodelElement
    {
        public FormInstanceRelationshipElement(
            FormInstanceListOfSame parentInstance, FormDescRelationshipElement parentDesc,
            Aas.ISubmodelElement source = null, bool deepCopy = false)
        {
            // way back to description
            this.parentInstance = parentInstance;
            this.desc = parentDesc;

            // initialize Referable
            var re = new Aas.RelationshipElement(null, null);
            this.sme = re;
            InitReferable(parentDesc, source);

            // check, if a source is present
            this.sourceSme = source;
            var reSource = this.sourceSme as Aas.RelationshipElement;
            if (reSource != null)
            {
                // take over
                if (reSource.First != null)
                    re.First = reSource.First.Copy();
                if (reSource.Second != null)
                    re.Second = reSource.Second.Copy();
            }

            // create user control
#if USE_WPF
            if (createSubControls)
            {
                this.subControl = new FormSubControlRelationshipElement();
                this.subControl.DataContext = this;
            }
#endif
        }

        /// <summary>
        /// Before rendering the SME into a list of new elements, process the SME.
        /// If <c>Touched</c>, <c>sourceSme</c> and <c>editSource</c> is set, this function shall write back
        /// the new values instead of producing a new element. Returns True, if a new element shall be rendered.
        /// </summary>
        public override bool ProcessSmeForRender(
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false, bool editSource = false)
        {
            // refer to base (SME) function, but not caring about result
            base.ProcessSmeForRender(packageEnv, addFilesToPackage, editSource);

            var re = this.sme as Aas.RelationshipElement;
            var reSource = this.sourceSme as Aas.RelationshipElement;
            if (re != null && Touched && reSource != null && editSource)
            {
                if (re.First != null)
                    reSource.First = re.First.Copy();
                if (re.Second != null)
                    reSource.Second = re.Second.Copy();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Render the AnyUI representation of the current instance data structure
        /// </summary>
        public override void RenderAnyUi(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PluginOperationContextBase opctx)
        {
            // access
            var rele = sme as Aas.RelationshipElement;
            if (rele == null)
                return;

            // Intended layout:
            // Grid
            //   first
            //   Reference info    [Clear]
            //   """"""""""""""    [Select]
            //   second
            //   Reference info    [Clear]
            //   """"""""""""""    [Select]

            var g = view.Add(
                uitk.AddSmallGrid(rows: 6, cols: 4,
                    colWidths: new[] { "2:", "*", "#", "2:" }));

            for (int i = 0; i < 2; i++)
            {
                // selektor
                var valGet = (i == 0) ? rele.First : rele.Second;
                Action<Aas.Reference> valSet = (rf) => rele.First = rf;
                if (i == 1)
                    valSet = (rf) => rele.Second = rf;
                var name = (new[] { "first", "second" })[i];
                var row = 3 * i;

                // name
                uitk.AddSmallBasicLabelTo(g, row + 0, 1,
                    foreground: AnyUiBrushes.DarkGray, fontSize: 0.8f,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    content: name);

                // prepare reference display
                var info = "(no reference set)";
                if (valGet != null)
                    info = "(no Keys)";
                if (valGet?.Keys != null && valGet.Keys.Count > 0)
                    info = valGet.ToStringExtended(delimiter: System.Environment.NewLine);

                // Use a drop box as info; but now allow drop
                uitk.AddSmallDropBoxTo(
                    g, row + 1, 1, rowSpan: 2,
                    margin: new AnyUiThickness(2, 2, 2, 2), minHeight: 40,
                    borderThickness: new AnyUiThickness(1),
                    text: info);

                // "Clear" button in 1st row
                if (opctx?.IsDisplayModeEditOrAdd == true)
                    AnyUiUIElement.RegisterControl(
                        uitk.AddSmallButtonTo(g, row + 1, 2, margin: new AnyUiThickness(1.0),
                            content: "Clear"),
                            (o) =>
                            {
                                valSet(null);
                                Touch();
                                return NewLambdaUpdateUi(this);
                            });

                // "Select" button in 2nd row
                if (opctx?.IsDisplayModeEditOrAdd == true)
                    AnyUiUIElement.RegisterControl(
                        uitk.AddSmallButtonTo(g, row + 2, 2, margin: new AnyUiThickness(1.0),
                            content: "Select"),
                            (o) =>
                            {
                                // kick off dialogue in main application
                                var tempI = this;
                                tempI.PushAndAdaptEventFromTop(
                                    new AasxIntegrationBase.AasxPluginResultEventSelectAasEntity()
                                    {
                                        filterEntities = "All",
                                        showAuxPackage = true,
                                        showRepoFiles = true
                                    },
                                    (revt) =>
                                    {
                                        if (revt is AasxPluginEventReturnSelectAasEntity rsel
                                            && rsel.resultKeys != null)
                                        {
                                            // do it
                                            valSet(ExtendReference.CreateNew(rsel.resultKeys));
                                            Touch();

                                            // send event to re-render
                                            tempI.PushAndAdaptEventFromTop(NewResultEventUpdateUi());
                                        }
                                    });

                                return new AnyUiLambdaActionNone();
                            });
            }
        }

    }

    public class FormInstanceCapability : FormInstanceSubmodelElement
    {
        public FormInstanceCapability(
            FormInstanceListOfSame parentInstance, FormDescCapability parentDesc,
            Aas.ISubmodelElement source = null, bool deepCopy = false)
        {
            // way back to description
            this.parentInstance = parentInstance;
            this.desc = parentDesc;

            // initialize Referable
            var re = new Aas.Capability();
            this.sme = re;
            InitReferable(parentDesc, source);

            // check, if a source is present
            this.sourceSme = source;
            var reSource = this.sourceSme as Aas.Capability;
            if (reSource != null)
            {
                // take over
                // nothing here
            }

            // create user control
#if USE_WPF
            if (createSubControls)
            {
                this.subControl = new FormSubControlCapability();
                this.subControl.DataContext = this;
            }
#endif
        }

        /// <summary>
        /// Before rendering the SME into a list of new elements, process the SME.
        /// If <c>Touched</c>, <c>sourceSme</c> and <c>editSource</c> is set, this function shall write back
        /// the new values instead of producing a new element. Returns True, if a new element shall be rendered.
        /// </summary>
        public override bool ProcessSmeForRender(
            AdminShellPackageEnv packageEnv = null, bool addFilesToPackage = false, bool editSource = false)
        {
            // refer to base (SME) function, but not caring about result
            base.ProcessSmeForRender(packageEnv, addFilesToPackage, editSource);

            var re = this.sme as Aas.RelationshipElement;
            var reSource = this.sourceSme as Aas.RelationshipElement;
            if (re != null && Touched && reSource != null && editSource)
            {
                if (re.First != null)
                    reSource.First = re.First.Copy();
                if (re.Second != null)
                    reSource.Second = re.Second.Copy();
                return false;
            }
            return true;
        }

    }

}
