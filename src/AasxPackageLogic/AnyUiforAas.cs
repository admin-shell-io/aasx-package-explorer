/*
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
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;

namespace AasxPackageLogic
{
    //
    // Lambda Actions for the editing of an AAS
    //

    public class AnyUiLambdaActionList : AnyUiLambdaActionBase
    {
        public List<AnyUiLambdaActionBase> Actions = new List<AnyUiLambdaActionBase>();

        public AnyUiLambdaActionList() { }
        public AnyUiLambdaActionList(List<AnyUiLambdaActionBase> actions)
        {
            if (actions != null)
                Actions = actions;
        }
        public AnyUiLambdaActionList(IEnumerable<AnyUiLambdaActionBase> actions)
        {
            if (actions != null)
                Actions = new List<AnyUiLambdaActionBase>(actions);
        }
        public AnyUiLambdaActionList(params AnyUiLambdaActionBase[] actions)
        {
            if (actions != null)
                Actions = new List<AnyUiLambdaActionBase>(actions);
        }
    }

    public class AnyUiLambdaActionRedrawEntity : AnyUiLambdaActionBase { }
    public class AnyUiLambdaActionRedrawAllElements : AnyUiLambdaActionBase
    {
        public object NextFocus = null;
        public bool? IsExpanded = null;
        public DispEditHighlight.HighlightFieldInfo HighlightField = null;
        public bool OnlyReFocus = false;

        public AnyUiLambdaActionRedrawAllElements(
            object nextFocus, bool? isExpanded = true,
            DispEditHighlight.HighlightFieldInfo highlightField = null,
            bool onlyReFocus = false)
        {
            this.NextFocus = nextFocus;
            this.IsExpanded = isExpanded;
            this.HighlightField = highlightField;
            this.OnlyReFocus = onlyReFocus;
        }
    }

    public class AnyUiLambdaActionSelectMainObjects : AnyUiLambdaActionBase
    {
        public object[] MainObjects = null;

        public AnyUiLambdaActionSelectMainObjects() { }
        public AnyUiLambdaActionSelectMainObjects(IEnumerable<object> mainObjects)
        {
            MainObjects = mainObjects.ToArray();
        }
    }

    public class AnyUiLambdaActionPackCntChange : AnyUiLambdaActionBase
    {
        public AnyUiLambdaActionPackCntChange() { }
        public AnyUiLambdaActionPackCntChange(PackCntChangeEventData change)
        {
            this.Change = change;
        }
        public PackCntChangeEventData Change;
    }

    public class AnyUiLambdaActionNavigateTo : AnyUiLambdaActionBase
    {
        public AnyUiLambdaActionNavigateTo() { }
        public AnyUiLambdaActionNavigateTo(
            AdminShell.Reference targetReference,
            bool translateAssetToAAS = false,
            bool alsoDereferenceObjects = true)
        {
            this.targetReference = targetReference;
            this.translateAssetToAAS = translateAssetToAAS;
            this.alsoDereferenceObjects = alsoDereferenceObjects;
        }
        public AdminShell.Reference targetReference;
        public bool translateAssetToAAS = false;
        public bool alsoDereferenceObjects = true;
    }   

    //
    // Dialogues (Flyovers) involving specific AAS entities
    //

    public class AnyUiDialogueDataSelectAasEntity : AnyUiDialogueDataBase
    {
        // in
        public PackageCentral.PackageCentral.Selector Selector;
        public string Filter;

        // out
        public AdminShell.KeyList ResultKeys;
        public VisualElementGeneric ResultVisualElement;

        public AnyUiDialogueDataSelectAasEntity(
            string caption = "",
            double? maxWidth = null,
            PackageCentral.PackageCentral.Selector? selector = PackageCentral.PackageCentral.Selector.Main,
            string filter = null)
            : base(caption, maxWidth)
        {
            if (selector.HasValue)
                Selector = selector.Value;
            if (filter != null)
                Filter = filter;
        }
    }

    public class AnyUiDialogueDataSelectReferableFromPool : AnyUiDialogueDataBase
    {
        // in
        // (the pool will be provided by the technology implementation)

        // out
        public int ResultIndex = -1;
        public object ResultItem = null;

        public AnyUiDialogueDataSelectReferableFromPool(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }
    }

    public class AnyUiDialogueDataSelectQualifierPreset : AnyUiDialogueDataBase
    {
        // in
        // (the presets will be provided by the technology implementation)

        // out
        public AdminShell.Qualifier ResultQualifier = null;

        public AnyUiDialogueDataSelectQualifierPreset(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }
    }
}
