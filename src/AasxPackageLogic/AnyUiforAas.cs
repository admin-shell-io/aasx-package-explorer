/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Aas = AasCore.Aas3_0;

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

    public class AnyUiLambdaActionRedrawAllElements : AnyUiLambdaActionRedrawAllElementsBase
    {
        public DispEditHighlight.HighlightFieldInfo HighlightField = null;

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
        public PackCntChangeEventData Change;
        public object NextFocus = null;

        public AnyUiLambdaActionPackCntChange() { }
        public AnyUiLambdaActionPackCntChange(PackCntChangeEventData change, object nextFocus = null)
        {
            Change = change;
            NextFocus = nextFocus;
        }
    }

    public class AnyUiLambdaActionNavigateTo : AnyUiLambdaActionBase
    {
        public AnyUiLambdaActionNavigateTo() { }
        public AnyUiLambdaActionNavigateTo(
            Aas.IReference targetReference,
            bool translateAssetToAAS = false,
            bool alsoDereferenceObjects = true)
        {
            this.targetReference = targetReference;
            this.translateAssetToAAS = translateAssetToAAS;
            this.alsoDereferenceObjects = alsoDereferenceObjects;
        }
        public Aas.IReference targetReference;
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
        public List<Aas.IKey> ResultKeys;
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

        public static string ApplyFullFilterString(string filter)
        {
            if (filter == null)
                return null;
            var res = filter;
            if (res.Trim().ToLower().Contains("submodelelement"))
            {
                var allElems = " " + string.Join(" ", Enum.GetNames(typeof(Aas.AasSubmodelElements))) + " ";
                res = res.Replace("submodelelement", allElems, StringComparison.InvariantCultureIgnoreCase);
            }
            if (res.Trim().ToLower() == "all")
                return null;
            else
                return " " + res + " ";
        }

        public static bool CheckFilter(string givenFilter, string singleName)
        {
            // special case
            var ff = ApplyFullFilterString(givenFilter);
            if (ff == null)
                return true;

            // regular
            return (
                givenFilter == null || singleName == null
                || ff.ToLower().IndexOf($"{singleName.ToLower().Trim()} ", StringComparison.Ordinal) >= 0);
        }

        public bool PrepareResult(
            VisualElementGeneric selectedItem,
            string filter)
        {
            // access
            if (selectedItem == null)
                return false;
            var siMdo = selectedItem.GetMainDataObject();

            // already one result
            ResultVisualElement = selectedItem;

            //
            // IReferable
            //
            if (siMdo is Aas.IReferable dataRef)
            {
                // check if a valuable item was selected
                // new special case: "GlobalReference" allows to select all (2021-09-11)
                var skip = filter != null &&
                    filter.Trim().ToLower() == Aas.Stringification.ToString(Aas.KeyTypes.GlobalReference).Trim().ToLower();
                if (!skip)
                {
                    var elemname = dataRef.GetSelfDescription().AasElementName;
                    var fullFilter = AnyUiDialogueDataSelectAasEntity.ApplyFullFilterString(filter);
                    if (fullFilter != null && !(fullFilter.IndexOf(elemname + " ", StringComparison.Ordinal) >= 0))
                        return false;
                }

                // ok, prepare list of keys
                ResultKeys = selectedItem.BuildKeyListToTop();

                return true;
            }

            //
            // other special cases
            //
            if (siMdo is Aas.Reference smref &&
                AnyUiDialogueDataSelectAasEntity.CheckFilter(filter, "submodelref"))
            {
                ResultKeys = new List<Aas.IKey>();
                ResultKeys.AddRange(smref.Keys);
                return true;
            }

            if (selectedItem is VisualElementPluginExtension vepe)
            {
                // get main data object of the parent of the plug in ..
                var parentMdo = vepe.Parent.GetMainDataObject();
                if (parentMdo != null)
                {
                    // safe to return a list for the parent ..
                    // (include AAS, as this is important to plug-ins)
                    ResultKeys = selectedItem.BuildKeyListToTop(includeAas: true);

                    // .. enriched by a last element
                    ResultKeys.Add(new Aas.Key(Aas.KeyTypes.FragmentReference, "Plugin:" + vepe.theExt.Tag));

                    // ok
                    return true;
                }
            }

            if (selectedItem is VisualElementAsset veass
                && AnyUiDialogueDataSelectAasEntity.CheckFilter(filter, "AssetInformation")
                && veass.theAsset != null)
            {
                // prepare data
                ResultKeys = selectedItem.BuildKeyListToTop(includeAas: true);
                return true;
            }

            if (selectedItem is VisualElementOperationVariable veov
                && AnyUiDialogueDataSelectAasEntity.CheckFilter(filter, "OperationVariable")
                && veov.theOpVar?.Value != null)
            {
                // prepare data
                ResultKeys = selectedItem.BuildKeyListToTop(includeAas: true);
                return true;
            }

            if (selectedItem is VisualElementSupplementalFile vesf && vesf.theFile != null)
            {
                // prepare data
                ResultKeys = selectedItem.BuildKeyListToTop(includeAas: true);
                return true;
            }

            // uups
            return false;
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

    public class AnyUiDialogueDataSelectFromRepository : AnyUiDialogueDataBase
    {
        /// <summary>
        /// Limit the number of buttons shown in the dialogue.
        /// </summary>
        public static int MaxButtonsToShow = 20;

        public IList<PackageContainerRepoItem> Items = null;
        public PackageContainerRepoItem ResultItem = null;

        public AnyUiDialogueDataSelectFromRepository(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }

        public PackageContainerRepoItem SearchId(string aid)
        {
            // condition
            aid = aid?.Trim().ToLower();
            if (aid?.HasContent() != true)
                return null;

            // first compare against tags
            if (this.Items != null)
                foreach (var ri in this.Items)
                    if (aid == ri.Tag.Trim().ToLower())
                        return ri;

            // if not, compare asset ids
            if (this.Items != null)
                foreach (var ri in this.Items)
                    foreach (var id in ri.EnumerateAssetIds())
                        if (aid == id.Trim().ToLower())
                            return ri;

            return null;
        }
    }

    public class AnyUiDialogueDataSelectQualifierPreset : AnyUiDialogueDataBase
    {
        // in
        // (the presets will be provided by the technology implementation)

        // out
        public Aas.Qualifier ResultQualifier = null;

        public AnyUiDialogueDataSelectQualifierPreset(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }
    }
}
