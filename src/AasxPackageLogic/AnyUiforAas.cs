using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AnyUi;
using AasxPackageLogic.PackageCentral;

namespace AasxPackageLogic
{
    //
    // Lambda Actions for the editing of an AAS
    //

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
        public AnyUiLambdaActionNavigateTo(AdminShell.Reference targetReference, bool translateAssetToAAS = false)
        {
            this.targetReference = targetReference;
            this.translateAssetToAAS = translateAssetToAAS;
        }
        public AdminShell.Reference targetReference;
        public bool translateAssetToAAS;
    }

    public class AnyUiLambdaActionDisplayContentFile : AnyUiLambdaActionBase
    {
        public AnyUiLambdaActionDisplayContentFile() { }
        public AnyUiLambdaActionDisplayContentFile(
            string fn, string mimeType = null, bool preferInternalDisplay = false)
        {
            this.fn = fn;
            this.mimeType = mimeType;
            this.preferInternalDisplay = preferInternalDisplay;
        }

        public string fn = null;
        public string mimeType = null;
        public bool preferInternalDisplay = false;
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
