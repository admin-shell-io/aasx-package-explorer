/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AasCore.Aas3_0_RC02.HasDataSpecification;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AdminShellNS.Display;
using AdminShellNS.Extenstions;
using AnyUi;
using Extensions;

namespace AasxPackageLogic
{
    public class DispEditHelperEntities : DispEditHelperModules
    {
        static string PackageSourcePath = "";
        static string PackageTargetFn = "";
        static string PackageTargetDir = "/aasx";
        static bool PackageEmbedAsThumbnail = false;

        public DispEditHelperCopyPaste.CopyPasteBuffer theCopyPaste = new DispEditHelperCopyPaste.CopyPasteBuffer();

        public class UploadAssistance
        {
            public string SourcePath = "";
            public string TargetPath = "/aasx/files";
        }
        public UploadAssistance uploadAssistance = new UploadAssistance();

        //
        //
        // --- AssetInformation
        //
        //

        public void DisplayOrEditAasEntityAsset(
            PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env,
            AssetAdministrationShell aas, AssetInformation asset,
            object preferredNextFocus,
            bool editMode, ModifyRepo repo, AnyUiStackPanel stack, bool embedded = false,
            bool hintMode = false)
        {
            this.AddGroup(stack, "AssetInformation", this.levelColors.MainSection);

            // global Asset ID

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => asset.GlobalAssetId?.IsValid() != true,
                    "It is strobly encouraged to have the AAS associated with an global asset id from the " +
                    "very beginning. If the AAS describes a product, the individual asset id should to be " +
                    "found on its typeplate. " +
                    "This  attribute  is  required  as  soon  as  the  AAS  is exchanged via partners in " +
                    "the life cycle of the asset.",
                    severityLevel: HintCheck.Severity.High)
            });

            if (this.SafeguardAccess(
                    stack, repo, asset.GlobalAssetId, "globalAssetId:", "Create data element!",
                    v =>
                    {
                        asset.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                List<string> keys = new();
                foreach(var key in asset.GlobalAssetId.Keys)
                {
                    keys.Add(key.Value);
                }
                this.AddKeyListOfIdentifier(
                    stack, "globalAssetId", keys, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    auxButtonTitles: new[] { "Generate", "Input", "Rename" },
                    auxButtonToolTips: new[] {
                        "Generate an id based on the customizable template option for asset ids.",
                        "Input the id, may be by the aid of barcode scanner",
                        "Rename the id and all occurences of the id in the AAS"
                    },
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            //TODO: jtikekar keyType
                            asset.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "" + AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdAsset))});
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: preferredNextFocus);
                        }

                        if (i == 1)
                        {
                            var uc = new AnyUiDialogueDataTextBox(
                                "Global Asset ID:",
                                maxWidth: 1400,
                                symbol: AnyUiMessageBoxImage.Question,
                                options: AnyUiDialogueDataTextBox.DialogueOptions.FilterAllControlKeys,
                                text: "" + asset.GlobalAssetId?.GetAsIdentifier());
                            if (this.context.StartFlyoverModal(uc))
                            {
                                //TODO: jtikekar keyType
                                asset.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, "" + uc.Text) });
                                this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: asset);
                            }
                        }

                        if (i == 2 && env != null)
                        {
                            var uc = new AnyUiDialogueDataTextBox(
                                "New Global Asset ID:",
                                symbol: AnyUiMessageBoxImage.Question,
                                maxWidth: 1400,
                                text: "" + asset.GlobalAssetId?.GetAsIdentifier());
                            if (this.context.StartFlyoverModal(uc))
                            {
                                var res = false;

                                try
                                {
                                    // rename
                                    var lrf = env.RenameIdentifiable<AssetInformation>(
                                        asset.GlobalAssetId?.GetAsIdentifier(),
                                        uc.Text);

                                    // use this information to emit events
                                    if (lrf != null)
                                    {
                                        res = true;
                                        foreach (var rf in lrf)
                                        {
                                            var rfi = rf.FindParentFirstIdentifiable();
                                            if (rfi != null)
                                                this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                }

                                if (!res)
                                    this.context.MessageBoxFlyoutShow(
                                        "The renaming of the Submodel or some referring elements " +
                                        "has not performed successfully! Please review your inputs and " +
                                        "the AAS structure for any inconsistencies.",
                                        "Warning",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);

                                return new AnyUiLambdaActionRedrawAllElements(asset);
                            }
                        }
                        return new AnyUiLambdaActionNone();

                    });

                // print code sheet
                this.AddAction(stack, "Actions:", new[] { "Print asset code sheet .." }, repo, (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                    {
                        var uc = new AnyUiDialogueDataEmpty();
                        this.context?.StartFlyover(uc);
                        try
                        {
                            this.context?.PrintSingleAssetCodeSheet(
                                asset.GlobalAssetId?.GetAsIdentifier(),
                                asset.GlobalAssetId?.GetAsIdentifier().ToString());
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex, "When printing, an error occurred");
                        }
                        this.context?.CloseFlyover();
                    }
                    return new AnyUiLambdaActionNone();
                });
            }

            // Kind
            this.DisplayOrEditEntityAssetKind(stack, asset.AssetKind,
                (k) => { asset.AssetKind = k; }, relatedReferable: aas);


            // list of multiple key value pairs
            this.DisplayOrEditEntityListOfIdentifierKeyValuePair(stack, asset.SpecificAssetIds,
                (ico) => { asset.SpecificAssetIds = ico; },
                key: "specificAssetId",
                relatedReferable: aas);

            // Thumbnail: File [0..1]
            // Note: another, may be better approach would be have a special SMWCollection constrained
            // on [0..1] and let the existing functions work on this. This could give better copy/ paste
            // and more. The serialization would then materialize (via getter/setters) this as "File" [0..1].

            this.AddGroup(stack, "DefaultThumbnail: File element", this.levelColors.SubSection,
                auxButtonTitle: (asset.DefaultThumbnail == null) ? null : "Delete",
                auxButtonLambda: (o) =>
                {
                    if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                               "Delete Fiel element? This operation can not be reverted!",
                               "ConceptDescriptions",
                               AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                    {
                        asset.DefaultThumbnail = null;
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }

                    return new AnyUiLambdaActionNone();
                });

            if (this.SafeguardAccess(
                stack, repo, asset.DefaultThumbnail, $"defaultThumbnail:", $"Create empty File element!",
                v =>
                {
                    asset.DefaultThumbnail = new Resource(""); //File replaced by resource in V3
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                var substack = AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                // Note: parentContainer = null effectively seems to disable "unwanted" functionality
                DisplayOrEditAasEntitySubmodelElement(
                    packages: packages, env: env, parentContainer: null, wrapper: null,
                    sme: (ISubmodelElement)asset.DefaultThumbnail,
                    editMode: editMode, repo: repo, stack: substack, hintMode: hintMode);
            }

        }

        //
        //
        // --- AAS Env
        //
        //

        public void DisplayOrEditAasEntityAasEnv(
            PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env,
            VisualElementEnvironmentItem ve, bool editMode, AnyUiStackPanel stack,
            bool hintMode = false)
        {
            this.AddGroup(stack, "Environment of AssetInformation Administration Shells", this.levelColors.MainSection);
            if (env == null)
                return;

            // automatically and silently fix errors
            if (env.AssetAdministrationShells == null)
                env.AssetAdministrationShells = new List<AssetAdministrationShell>();
            if (env.ConceptDescriptions == null)
                env.ConceptDescriptions = new List<ConceptDescription>();
            if (env.Submodels == null)
                env.Submodels = new List<Submodel>();

            if (editMode &&
                (ve.theItemType == VisualElementEnvironmentItem.ItemType.Env
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.Shells
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions))
            {
                // some hints
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return env.AssetAdministrationShells == null || env.AssetAdministrationShells.Count < 1; },
                        "There are no Administration Shells in this AAS environment. " +
                            "You should consider adding an Administration Shell by clicking 'Add asset' " +
                            "on the edit panel below. Typically, this is done after adding an asset, " +
                            "as the Administration Shell needs to refer to it.",
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return env.Submodels == null || env.Submodels.Count < 1; },
                        "There are no Submodels in this AAS environment. In this application, Submodels are " +
                            "created by adding them to associated to Administration Shells. " +
                            "Therefore, an Adminstration Shell shall exist before and shall be selected. " +
                            "You could then add Submodels by clicking " +
                            "'Create new Submodel of kind Type/Instance' on the edit panel. " +
                            "This step is typically done after creating asset and Administration Shell."),
                    new HintCheck(
                        () => { return env.ConceptDescriptions == null || env.ConceptDescriptions.Count < 1; },
                        "There are no ConceptDescriptions in this AAS environment. " +
                            "Even if SubmodelElements can reference external concept descriptions, " +
                            "it is best practice to include (duplicates of the) concept descriptions " +
                            "inside the AAS environment. You should consider adding a ConceptDescription " +
                            "by clicking 'Add ConceptDescription' on the panel below or " +
                            "adding a ISubmodelElement to a Submodel. This step is typically done after " +
                            "creating assets and Administration Shell and when creating SubmodelElements."),
                });

                // let the user control the number of entities
                this.AddAction(
                    stack, "Entities:", new[] { "Add AAS", "Add ConceptDescription" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var aas = new AssetAdministrationShell("", null, submodels:new List<Reference>()); //TODO:jtikekar refere an Asset
                            this.MakeNewIdentifiableUnique(aas);
                            env.AssetAdministrationShells.Add(aas);
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange(
                                StructuralChangeReason.Create));
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: aas);
                        }

                        if (buttonNdx == 1)
                        {
                            var cd = new ConceptDescription("");
                            this.MakeNewIdentifiableUnique(cd);
                            env.ConceptDescriptions.Add(cd);
                            this.AddDiaryEntry(cd, new DiaryEntryStructChange(
                                StructuralChangeReason.Create));
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: cd);
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // Copy AAS
                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.Shells)
                {
                    this.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                    this.AddAction(
                        stack, "Copy from existing AAS:",
                        new[] { "Copy single entity ", "Copy recursively", "Copy rec. w/ suppl. files" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0 || buttonNdx == 1 || buttonNdx == 2)
                            {
                                var rve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                                    Stringification.ToString(KeyTypes.AssetAdministrationShell)) as VisualElementAdminShell;

                                if (rve != null)
                                {
                                    var copyRecursively = buttonNdx == 1 || buttonNdx == 2;
                                    var createNewIds = env == rve.theEnv;
                                    var copySupplFiles = buttonNdx == 2;

                                    var potentialSupplFilesToCopy = new Dictionary<string, string>();
                                    AssetAdministrationShell destAAS = null;

                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is AssetAdministrationShell sourceAAS)
                                    {
                                        //
                                        // copy AAS
                                        //
                                        try
                                        {
                                            // make a copy of the AAS itself
                                            destAAS = (mdo as AssetAdministrationShell).Copy();
                                            if (createNewIds)
                                            {
                                                destAAS.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                                        Options.Curr.TemplateIdAas);

                                                if (destAAS.AssetInformation != null)
                                                {
                                                    destAAS.AssetInformation.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.GlobalReference, AdminShellUtil.GenerateIdAccordingTemplate(
                                                                Options.Curr.TemplateIdAsset))});
                                                }
                                            }

                                            env.AssetAdministrationShells.Add(destAAS);
                                            this.AddDiaryEntry(destAAS, new DiaryEntryStructChange(
                                                StructuralChangeReason.Create));

                                            // clear, copy Submodels?
                                            destAAS.Submodels = new List<Reference>();
                                            if (copyRecursively && sourceAAS.Submodels != null)
                                            {
                                                foreach (var smr in sourceAAS.Submodels)
                                                {
                                                    // need access to source submodel
                                                    var srcSub = rve.theEnv.FindSubmodel(smr);
                                                    if (srcSub == null)
                                                        continue;

                                                    // get hold of suppl file infos?
                                                    if (srcSub.SubmodelElements != null)
                                                        foreach (var f in
                                                                srcSub.SubmodelElements.FindDeep<AasCore.Aas3_0_RC02.File>())
                                                        {
                                                            if (f != null && f.Value != null &&
                                                                    f.Value.StartsWith("/") &&
                                                                    !potentialSupplFilesToCopy
                                                                    .ContainsKey(f.Value.ToLower().Trim()))
                                                                potentialSupplFilesToCopy[
                                                                    f.Value.ToLower().Trim()] =
                                                                        f.Value.ToLower().Trim();
                                                        }

                                                    // complicated new ids?
                                                    if (!createNewIds)
                                                    {
                                                        // straightforward between environments
                                                        var destSMR = env.CopySubmodelRefAndCD(
                                                            rve.theEnv, smr, copySubmodel: true, copyCD: true,
                                                            shallowCopy: false);
                                                        if (destSMR != null)
                                                        {
                                                            destAAS.Submodels.Add(destSMR);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // in the same environment?
                                                        // means: we have to generate a new submodel ref 
                                                        // by using template mechanism
                                                        var tid = Options.Curr.TemplateIdSubmodelInstance;
                                                        if (srcSub.Kind != null && srcSub.Kind == ModelingKind.Template)
                                                            tid = Options.Curr.TemplateIdSubmodelTemplate;

                                                        // create Submodel as deep copy 
                                                        // with new id from scratch
                                                        var dstSub = srcSub.Copy();
                                                        dstSub.Id = AdminShellUtil.GenerateIdAccordingTemplate(tid);

                                                        // make a new ref
                                                        var dstRef = dstSub.GetModelReference().Copy();

                                                        // formally add this to active environment and AAS
                                                        env.Submodels.Add(dstSub);
                                                        destAAS.Submodels.Add(dstRef);

                                                        this.AddDiaryEntry(dstSub, new DiaryEntryStructChange(
                                                            StructuralChangeReason.Create));
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Singleton.Error(ex, $"copying AAS");
                                        }

                                        //
                                        // Copy suppl files
                                        //
                                        if (copySupplFiles && rve.thePackage != null && packages.Main != rve.thePackage)
                                        {
                                            // copy conditions met
                                            foreach (var fn in potentialSupplFilesToCopy.Values)
                                            {
                                                try
                                                {
                                                    // copy ONLY if not existing in destination
                                                    // rationale: do not potential harm the source content, 
                                                    // even when voiding destination integrity
                                                    if (rve.thePackage.IsLocalFile(fn)
                                                        && !packages.Main.IsLocalFile(fn))
                                                    {
                                                        var tmpFile =
                                                            rve.thePackage.MakePackageFileAvailableAsTempFile(fn);
                                                        var targetDir = System.IO.Path.GetDirectoryName(fn);
                                                        var targetFn = System.IO.Path.GetFileName(fn);
                                                        packages.Main.AddSupplementaryFileToStore(
                                                            tmpFile, targetDir, targetFn, false);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Singleton.Error(
                                                        ex, $"copying supplementary file {fn}");
                                                }
                                            }
                                        }

                                        //
                                        // Done
                                        //
                                        return new AnyUiLambdaActionRedrawAllElements(
                                            nextFocus: destAAS, isExpanded: true);
                                    }
                                }
                            }

                            return new AnyUiLambdaActionNone();
                        });
                }

                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.Shells
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions)
                {
                    // Cut, copy, paste within list of Assets
                    this.DispPlainListOfIdentifiablePasteHelper<IIdentifiable>(
                        stack, repo, this.theCopyPaste,
                        label: "Buffer:",
                        lambdaPasteInto: (cpi, del) =>
                        {
                            // access
                            if (cpi is CopyPasteItemIdentifiable cpiid)
                            {
                                // some pre-conditions not met?
                                if (cpiid?.entity == null || (del && cpiid?.parentContainer == null))
                                    return null;

                                // divert
                                object res = null;
                                if (cpiid.entity is AssetAdministrationShell itaas)
                                {
                                    // new 
                                    var aas = itaas.Copy();
                                    env.AssetAdministrationShells.Add(aas);
                                    this.AddDiaryEntry(aas, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = aas;

                                    // delete
                                    if (del && cpiid.parentContainer is List<AssetAdministrationShell> aasold
                                        && aasold.Contains(itaas))
                                    {
                                        aasold.Remove(itaas);
                                        this.AddDiaryEntry(itaas,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }
                                else
                                if (cpiid.entity is ConceptDescription itcd)
                                {
                                    // new 
                                    var cd = itcd.Copy();
                                    env.ConceptDescriptions.Add(cd);
                                    this.AddDiaryEntry(cd, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = cd;

                                    // delete
                                    if (del && cpiid.parentContainer is List<ConceptDescription> cdold
                                        && cdold.Contains(itcd))
                                    {
                                        cdold.Remove(itcd);
                                        this.AddDiaryEntry(itcd,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }

                                // ok
                                return res;
                            }

                            if (cpi is CopyPasteItemSubmodel cpism)
                            {
                                // some pre-conditions not met?
                                if (cpism?.sm == null || (del && cpism?.parentContainer == null))
                                    return null;

                                // divert
                                object res = null;
                                if (cpism.sm is Submodel itsm)
                                {
                                    // new 
                                    var asset = itsm.Copy();
                                    env.Submodels.Add(itsm);
                                    this.AddDiaryEntry(itsm, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = asset;

                                    // delete
                                    if (del && cpism.parentContainer is List<Submodel> smold
                                        && smold.Contains(itsm))
                                    {
                                        smold.Remove(itsm);
                                        this.AddDiaryEntry(itsm,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }

                                // ok
                                return res;
                            }

                            // nok
                            return null;
                        });
                }

                //
                // Concept Descriptions
                //

                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions)
                {
                    this.AddGroup(stack, "Import of ConceptDescriptions", this.levelColors.MainSection);

                    // Copy
                    this.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                    this.AddAction(
                        stack, "Copy from existing ConceptDescription:",
                        new[] { "Copy single entity" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var rve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                                    "ConceptDescription") as VisualElementConceptDescription;
                                if (rve != null)
                                {
                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is ConceptDescription)
                                    {
                                        var clone = (mdo as ConceptDescription).Copy();
                                        if (env.ConceptDescriptions == null)
                                            env.ConceptDescriptions = new List<ConceptDescription>();
                                        this.MakeNewIdentifiableUnique(clone);
                                        env.ConceptDescriptions.Add(clone);
                                        this.AddDiaryEntry(clone,
                                            new DiaryEntryStructChange(StructuralChangeReason.Create));
                                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: clone);
                                    }
                                }
                            }

                            return new AnyUiLambdaActionNone();
                        });

                    //
                    // Dynamic rendering
                    //

                    this.AddGroup(stack, "Dynamic rendering of ConceptDescriptions", this.levelColors.MainSection);

                    var g1 = this.AddSubGrid(stack, "Dynamic order:", 1, 2, new[] { "#", "#" });
                    AnyUiComboBox cb1 = null;
                    cb1 = AnyUiUIElement.RegisterControl(
                        this.AddSmallComboBoxTo(g1, 0, 0,
                            margin: new AnyUiThickness(2, 2, 2, 2), padding: new AnyUiThickness(5, 0, 5, 0),
                            minWidth: 250,
                            items: new[] {
                            "List index", "idShort", "Identification", "By Submodel",
                            "By SubmodelElements"
                        }),
                        (o) =>
                        {
                            // resharper disable AccessToModifiedClosure
                            if (cb1?.SelectedIndex.HasValue == true)
                            {
                                ve.CdSortOrder = (VisualElementEnvironmentItem.ConceptDescSortOrder)
                                    cb1.SelectedIndex.Value;
                            }
                            else
                            {
                                Log.Singleton.Error("ComboxBox Dynamic rendering of entities has no value");
                            }
                            // resharper enable AccessToModifiedClosure
                            return new AnyUiLambdaActionNone();
                        },
                        takeOverLambda: new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: env?.ConceptDescriptions));

                    // set currently selected value
                    if (cb1 != null)
                        cb1.SelectedIndex = (int)ve.CdSortOrder;

                    //
                    // Static order 
                    //

                    this.AddGroup(stack, "Static order of ConceptDescriptions", this.levelColors.MainSection);

                    this.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return true;  },
                            "The sort operation permanently changes the order of ConceptDescriptions in the " +
                            "environment. It cannot be reverted!",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                    var g2 = this.AddSubGrid(stack, "Entities:", 1, 1, new[] { "#" });
                    AnyUiUIElement.RegisterControl(
                        this.AddSmallButtonTo(g2, 0, 0, content: "Sort according above order",
                            margin: new AnyUiThickness(2, 2, 2, 2), padding: new AnyUiThickness(5, 0, 5, 0)),
                        (o) =>
                        {
                            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                               "Perform sort operation? This operation can not be reverted!",
                               "ConceptDescriptions",
                               AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            {
                                var success = false;
                                if (ve.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.IdShort)
                                {
                                    env.ConceptDescriptions.Sort(new ComparerIdShort());
                                    success = true;
                                }
                                if (ve.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.Id)
                                {
                                    env.ConceptDescriptions.Sort(new ComparerIdentification());
                                    success = true;
                                }
                                if (ve.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySubmodel)
                                {
                                    var cmp = env.CreateIndexedComparerCdsForSmUsage();
                                    env.ConceptDescriptions.Sort(cmp);
                                    success = true;
                                }

                                if (success)
                                {
                                    ve.CdSortOrder = VisualElementEnvironmentItem.ConceptDescSortOrder.None;
                                    return new AnyUiLambdaActionRedrawAllElements(nextFocus: env?.ConceptDescriptions);
                                }
                                else
                                    this.context.MessageBoxFlyoutShow(
                                       "Cannot apply selected sort order!",
                                       "ConceptDescriptions",
                                       AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                            }

                            return new AnyUiLambdaActionNone();
                        });
                }
            }
            else if (ve.theItemType == VisualElementEnvironmentItem.ItemType.SupplFiles && packages.MainStorable)
            {
                // Files

                this.AddGroup(stack, "Supplementary file to add:", this.levelColors.SubSection);

                var g = this.AddSmallGrid(5, 3, new[] { "#", "*", "#" });
                this.AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(2, 0, 0, 0), content: "Source path: ");
                AnyUiUIElement.RegisterControl(
                    this.AddSmallTextBoxTo(g, 0, 1, margin: new AnyUiThickness(2, 2, 2, 2), text: PackageSourcePath),
                    (o) =>
                    {
                        if (o is string)
                            PackageSourcePath = o as string;
                        return new AnyUiLambdaActionNone();
                    });
                AnyUiUIElement.RegisterControl(
                    this.AddSmallButtonTo(
                        g, 0, 2, margin: new AnyUiThickness(2, 2, 2, 2), padding: new AnyUiThickness(5, 0, 5, 0),
                        content: "Select"),
                        (o) =>
                        {
                            var uc = new AnyUiDialogueDataOpenFile(
                                message: "Select a supplementary file to add..");
                            this.context?.StartFlyoverModal(uc);
                            if (uc.Result && uc.FileName != null)
                            {
                                PackageSourcePath = uc.FileName;
                                PackageTargetFn = System.IO.Path.GetFileName(uc.FileName);
                                PackageTargetFn = PackageTargetFn.Replace(" ", "_");
                            }
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                this.AddSmallLabelTo(g, 1, 0, padding: new AnyUiThickness(2, 0, 0, 0), content: "Target filename: ");
                AnyUiUIElement.RegisterControl(
                    this.AddSmallTextBoxTo(g, 1, 1, margin: new AnyUiThickness(2, 2, 2, 2), text: PackageTargetFn),
                    (o) =>
                    {
                        if (o is string)
                            PackageTargetFn = o as string;
                        return new AnyUiLambdaActionNone();
                    });
                this.AddSmallLabelTo(g, 2, 0, padding: new AnyUiThickness(2, 0, 0, 0), content: "Target path: ");
                AnyUiUIElement.RegisterControl(
                    this.AddSmallTextBoxTo(g, 2, 1, margin: new AnyUiThickness(2, 2, 2, 2), text: PackageTargetDir),
                    (o) =>
                    {
                        if (o is string)
                            PackageTargetDir = o as string;
                        return new AnyUiLambdaActionNone();
                    });
                AnyUiUIElement.RegisterControl(
                    this.AddSmallCheckBoxTo(g, 3, 1, margin: new AnyUiThickness(2, 2, 2, 2),
                    content: "Embed as thumbnail (only one file per package!)", isChecked: PackageEmbedAsThumbnail),
                    (o) =>
                    {
                        if (o is bool)
                            PackageEmbedAsThumbnail = (bool)o;
                        return new AnyUiLambdaActionNone();
                    });
                AnyUiUIElement.RegisterControl(
                    this.AddSmallButtonTo(g, 4, 1, margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0), content: "Add file to package"),
                    (o) =>
                    {
                        try
                        {
                            var ptd = PackageTargetDir;
                            if (PackageEmbedAsThumbnail)
                                ptd = "/";
                            packages.Main.AddSupplementaryFileToStore(
                                PackageSourcePath, ptd, PackageTargetFn, PackageEmbedAsThumbnail);
                            Log.Singleton.Info(
                                "Added {0} to pending package items. A save-operation is required.",
                                PackageSourcePath);
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex, "Adding file to package");
                        }
                        PackageSourcePath = "";
                        PackageTargetFn = "";
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: VisualElementEnvironmentItem.GiveAliasDataObject(
                                VisualElementEnvironmentItem.ItemType.Package));
                    });
                stack.Children.Add(g);
            }
            else
            {
                // Default
                this.AddHintBubble(
                    stack,
                    hintMode,
                    new[] {
                        new HintCheck(
                            () => { return env.AssetAdministrationShells.Count < 1; },
                            "There are no AssetAdministrationShell entities in the environment. " +
                                "Select the 'Administration Shells' item on the middle panel and " +
                                "select 'Add AAS' to add a new entity."),
                        new HintCheck(
                            () => { return env.ConceptDescriptions.Count < 1; },
                            "There are no embedded ConceptDescriptions in the environment. " +
                                "It is a good practive to have those. Select or add an AssetAdministrationShell, " +
                                "Submodel and ISubmodelElement and add a ConceptDescription.",
                            severityLevel: HintCheck.Severity.Notice),
                    });

                // overview information

                var g = this.AddSmallGrid(
                    6, 1, new[] { "*" }, margin: new AnyUiThickness(5, 5, 0, 0));
                this.AddSmallLabelTo(
                    g, 0, 0, content: "This structure hold the main entites of Administration shells.");
                this.AddSmallLabelTo(
                    g, 1, 0, content: String.Format("#admin shells: {0}.", env.AssetAdministrationShells.Count),
                    margin: new AnyUiThickness(0, 5, 0, 0));
                this.AddSmallLabelTo(g, 3, 0, content: String.Format("#submodels: {0}.", env.Submodels.Count));
                this.AddSmallLabelTo(
                    g, 4, 0, content: String.Format("#concept descriptions: {0}.", env.ConceptDescriptions.Count));
                stack.Children.Add(g);
            }
        }


        //
        //
        // --- Supplementary file
        //
        //

        public void DisplayOrEditAasEntitySupplementaryFile(
            PackageCentral.PackageCentral packages,
            VisualElementSupplementalFile entity,
            AdminShellPackageSupplementaryFile psf, bool editMode,
            AnyUiStackPanel stack)
        {
            //
            // Package
            //
            this.AddGroup(stack, "Supplementary file for package of AASX", this.levelColors.MainSection);

            if (editMode && packages.MainStorable && psf != null)
            {
                this.AddAction(stack, "Action", new[] { "Delete" }, repo, (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                        if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                        {
                            // try remember where we are
                            var sibling = entity.FindSibling()?.GetDereferencedMainDataObject();

                            // delete
                            try
                            {
                                packages.Main.DeleteSupplementaryFile(psf);
                                Log.Singleton.Info(
                                "Added {0} to pending package items to be deleted. " +
                                    "A save-operation might be required.", PackageSourcePath);
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "Deleting file in package");
                            }

                            // try to re-focus to a sibling
                            if (sibling != null)
                            {
                                // stay around
                                return new AnyUiLambdaActionRedrawAllElements(
                                    nextFocus: sibling);
                            }
                            else
                            {
                                // jump to root
                                return new AnyUiLambdaActionRedrawAllElements(
                                    nextFocus: VisualElementEnvironmentItem.GiveAliasDataObject(
                                        VisualElementEnvironmentItem.ItemType.Package));
                            }
                        }

                    return new AnyUiLambdaActionNone();
                });
            }
        }

        //
        //
        // --- AAS
        //
        //

        public void DisplayOrEditAasEntityAas(
            PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env,
            AssetAdministrationShell aas,
            bool editMode, AnyUiStackPanel stack, bool hintMode = false)
        {
            this.AddGroup(stack, "Asset Administration Shell", this.levelColors.MainSection);
            if (aas == null)
                return;

            // Entities
            if (editMode && aas?.Submodels != null)
            {
                this.AddGroup(stack, "Editing of entities", this.levelColors.MainSection);

                // Up/ down/ del
                this.EntityListUpDownDeleteHelper<AssetAdministrationShell>(
                    stack, repo, env.AssetAdministrationShells, aas, env, "AAS:");

                // Cut, copy, paste within list of AASes
                this.DispPlainIdentifiableCutCopyPasteHelper<AssetAdministrationShell>(
                    stack, repo, this.theCopyPaste,
                    env.AssetAdministrationShells, aas, (o) => { return (o as AssetAdministrationShell).Copy(); },
                    label: "Buffer:",
                    checkPasteInfo: (cpb) => cpb?.Items?.AllOfElementType<CopyPasteItemSubmodel>() == true,
                    doPasteInto: (cpi, del) =>
                    {
                        // access
                        var item = cpi as CopyPasteItemSubmodel;
                        if (item?.smref == null)
                            return null;

                        // duplicate
                        foreach (var x in aas.Submodels)
                            if (x?.Matches(item.smref, MatchMode.Identification) == true)
                                return null;

                        // add 
                        var newsmr = item.smref.Copy();
                        aas.Submodels.Add(newsmr);

                        // special case: Submodel does not exist, as pasting was from external
                        if (env?.Submodels != null && item.sm != null)
                        {
                            var smtest = env.FindSubmodel(newsmr);
                            if (smtest == null)
                            {
                                env.Submodels.Add(item.sm);
                                this.AddDiaryEntry(item.sm,
                                    new DiaryEntryStructChange(StructuralChangeReason.Create));
                            }
                        }

                        // delete
                        if (del && item.parentContainer is AssetAdministrationShell aasold
                            && aasold.Submodels.Contains(item.smref))
                            aasold.Submodels.Remove(item.smref);

                        // ok
                        return newsmr;
                    });

                // Submodels
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return aas.Submodels.Count < 1;  },
                            "You have no Submodels referenced by this Administration Shell. " +
                                "This is rather unusual, as the Submodels are the actual carriers of information. " +
                                "Most likely, you want to click 'Create new Submodel of kind Instance'. " +
                                "You might also consider to load another AASX as auxiliary AASX " +
                                "(see 'File' menu) to copy structures from.",
                            severityLevel: HintCheck.Severity.Notice)
                    });// adding submodels
                this.AddAction(
                    stack, "SubmodelRef:",
                    new[] {
                        "Reference to existing Submodel",
                        "Create new Submodel of kind Template",
                        "Create new Submodel of kind Instance"
                    },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation creates a reference to an existing Submodel. " +
                                        "By this, two AAS will share exactly the same data records. " +
                                        "Changing one will cause the other AAS's information to change as well. " +
                                        "This operation is rather special. Do you want to proceed?",
                                    "Submodel sharing",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            // select existing Submodel
                            var ks = this.SmartSelectAasEntityKeys(packages,
                                        PackageCentral.PackageCentral.Selector.Main,
                                        "Submodel");
                            if (ks != null)
                            {
                                // create ref
                                var smr = new Reference(ReferenceTypes.GlobalReference, new List<Key>(ks));
                                aas.Submodels.Add(smr);

                                // event for AAS
                                this.AddDiaryEntry(aas, new DiaryEntryStructChange());

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(
                                    nextFocus: smr, isExpanded: true);
                            }
                        }

                        if (buttonNdx == 1 || buttonNdx == 2)
                        {
                            // create new submodel
                            var submodel = new Submodel("");
                            this.MakeNewIdentifiableUnique(submodel);
                            this.AddDiaryEntry(submodel,
                                    new DiaryEntryStructChange(StructuralChangeReason.Create));
                            env.Submodels.Add(submodel);

                            // directly create identification, as we need it!
                            if (buttonNdx == 1)
                            {
                                submodel.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelTemplate);
                                submodel.Kind = ModelingKind.Template;
                            }
                            else
                                submodel.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelInstance);

                            // create ref
                            var smr = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.Submodel, submodel.Id)});
                            aas.Submodels.Add(smr);

                            // event for AAS
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange());

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smr, isExpanded: true);

                        }

                        return new AnyUiLambdaActionNone();
                    });

                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return this.packages.AuxAvailable;  },
                        "You have opened an auxiliary AASX package. You can copy elements from it!",
                        severityLevel: HintCheck.Severity.Notice)
                });
                this.AddAction(
                    stack, "Copy from existing Submodel:",
                    new[] { "Copy single entity ", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "SubmodelRef") as VisualElementSubmodelRef;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is Reference)
                                {
                                    // we have 2 different use cases: 
                                    // (1) copy between AAS ENVs, 
                                    // (2) copy in one AAS ENV!
                                    if (env != rve.theEnv)
                                    {
                                        // use case (1) copy between AAS ENVs
                                        var clone = env.CopySubmodelRefAndCD(
                                            rve.theEnv, mdo as Reference, copySubmodel: true,
                                            copyCD: true, shallowCopy: buttonNdx == 0);
                                        if (clone == null)
                                            return new AnyUiLambdaActionNone();
                                        if (aas.Submodels == null)
                                            aas.Submodels = new List<Reference>();
                                        aas.Submodels.Add(clone);
                                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                                        return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: clone, isExpanded: true);
                                    }
                                    else
                                    {
                                        // use case (2) copy in one AAS ENV!

                                        // need access to source submodel
                                        var srcSub = rve.theEnv.FindSubmodel(mdo as Reference);
                                        if (srcSub == null)
                                            return new AnyUiLambdaActionNone();

                                        // means: we have to generate a new submodel ref by using template mechanism
                                        var tid = Options.Curr.TemplateIdSubmodelInstance;
                                        if (srcSub.Kind != null && srcSub.Kind == ModelingKind.Template)
                                            tid = Options.Curr.TemplateIdSubmodelTemplate;

                                        // create Submodel as deep copy 
                                        // with new id from scratch
                                        var dstSub = srcSub.Copy();
                                        dstSub.Id = AdminShellUtil.GenerateIdAccordingTemplate(tid);

                                        // make a new ref
                                        var dstRef = dstSub.GetModelReference().Copy();

                                        // formally add this to active environment 
                                        env.Submodels.Add(dstSub);
                                        this.AddDiaryEntry(dstSub,
                                            new DiaryEntryStructChange(StructuralChangeReason.Create));

                                        // .. and AAS
                                        if (aas.Submodels == null)
                                            aas.Submodels = new List<Reference>();
                                        aas.Submodels.Add(dstRef);
                                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                                        return new AnyUiLambdaActionRedrawAllElements(
                                            nextFocus: dstRef, isExpanded: true);
                                    }
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });
            }

            // Referable
            this.DisplayOrEditEntityReferable(stack, aas, categoryUsual: false);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.DisplayOrEditEntityHasDataSpecificationReferences(stack, aas.DataSpecifications,
                (ds) => { aas.DataSpecifications = ds; }, relatedReferable: aas);

            // Identifiable
            this.DisplayOrEditEntityIdentifiable(
                stack, aas,
                Options.Curr.TemplateIdAas,
                null);

            // use some asset reference
            var asset = aas.AssetInformation;

            // derivedFrom
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () =>
                    {
                        return asset != null && asset.AssetKind != null && asset.AssetKind== AssetKind.Instance &&
                            ( aas.DerivedFrom == null || aas.DerivedFrom.Keys.Count < 1);
                    },
                    "You have decided to create an AAS for kind = 'Instance'. " +
                        "You might derive this from another AAS of kind = 'Instance' or " +
                        "from another AAS of kind = 'Type'. It is perfectly fair to create " +
                        "an AssetAdministrationShell with no 'derivedFrom' relation! " +
                        "However, for example, if you're an supplier of products which stem from a series-type, " +
                        "you might want to maintain a relation of the AAS's of the individual prouct instances " +
                        "to the AAS of the series type.",
                    severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(
                stack, repo, aas.DerivedFrom, "derivedFrom:", "Create data element!",
                v =>
                {
                    aas.DerivedFrom = new Reference(ReferenceTypes.GlobalReference, new List<Key>() { new Key(KeyTypes.AssetAdministrationShell, "")});
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.AddGroup(stack, "Derived From", this.levelColors.SubSection);

                Func<List<Key>, AnyUiLambdaActionBase> lambda = (kl) =>
                {
                    return new AnyUiLambdaActionNavigateTo(
                        new Reference(ReferenceTypes.ModelReference, new List<Key>(kl)), translateAssetToAAS: true);
                };

                this.AddKeyListKeys(
                    stack, "derivedFrom", aas.DerivedFrom.Keys, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, "AssetAdministrationShell",
                    jumpLambda: lambda, noEditJumpLambda: lambda, relatedReferable: aas);
            }

            //
            // Asset linked with AAS
            //

            // show group only if no AssetInformation is available because
            // else DisplayOrEditAasEntityAsset will do
            if (aas.AssetInformation == null)
                this.AddGroup(stack, "AssetInformation", this.levelColors.MainSection);

            if (this.SafeguardAccess(
                stack, repo, aas.AssetInformation, "AssetInformation:", "Create data element!",
                v =>
                {
                    aas.AssetInformation = new AssetInformation(AssetKind.Type);
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                DisplayOrEditAasEntityAsset(
                    packages, env, aas, aas.AssetInformation,
                    preferredNextFocus: aas,
                    editMode: editMode, repo: repo, stack: stack, hintMode: hintMode);
            }
        }

        //
        //
        // --- Submodel Ref
        //
        //

        public void DisplayOrEditAasEntitySubmodelOrRef(
            PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env,
            AssetAdministrationShell aas,
            Reference smref, Submodel submodel, bool editMode,
            AnyUiStackPanel stack, bool hintMode = false)
        {
            // This panel renders first the SubmodelReference and then the Submodel, below
            if (smref != null)
            {
                this.AddGroup(stack, "SubmodelReference", this.levelColors.MainSection);

                Func<List<Key>, AnyUiLambdaActionBase> lambda = (kl) =>
                 {
                     return new AnyUiLambdaActionNavigateTo(
                         new Reference(ReferenceTypes.ModelReference, new List<Key>(kl)), alsoDereferenceObjects: false);
                 };

                this.AddKeyListKeys(
                    stack, "submodelRef", smref.Keys, repo,
                    packages, PackageCentral.PackageCentral.Selector.Main, "Reference Submodel ",
                    takeOverLambdaAction: new AnyUiLambdaActionRedrawAllElements(smref),
                    jumpLambda: lambda, relatedReferable: aas);
            }

            // entities when under AAS (smref)
            if (editMode && smref != null)
            {
                this.AddGroup(stack, "Editing of entities", this.levelColors.MainSection);

                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = smref,
                    ParentElem = aas
                };

                this.EntityListUpDownDeleteHelper<Reference>(
                    stack, repo, aas.Submodels, smref, aas, "Reference:", sendUpdateEvent: evTemplate,
                    explicitParent: aas);
            }

            // entities other
            if (editMode && smref == null && submodel != null)
            {
                this.AddGroup(
                    stack, "Editing of entities (environment's Submodel collection)",
                    this.levelColors.MainSection);

                this.AddAction(stack, "Submodel:", new[] { "Delete" }, repo, (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                        if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                 "Delete selected Submodel? This operation can not be reverted!", "AAS-ENV",
                                 AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                        {
                            if (env.Submodels.Contains(submodel))
                                env.Submodels.Remove(submodel);
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(StructuralChangeReason.Delete));
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: null, isExpanded: null);
                        }

                    return new AnyUiLambdaActionNone();
                });
            }

            // Cut, copy, paste within an aas
            // Resharper disable once ConditionIsAlwaysTrueOrFalse
            if (editMode && smref != null && submodel != null && aas != null)
            {
                // cut/ copy / paste
                this.DispSubmodelCutCopyPasteHelper<Reference>(stack, repo, this.theCopyPaste,
                    aas.Submodels, smref, (sr) => { return new Reference(sr.Type, new List<Key>(sr.Keys)); },
                    smref, submodel,
                    label: "Buffer:",
                    checkEquality: (r1, r2) =>
                    {
                        if (r1 != null && r2 != null)
                            return (r1.Matches(r2, MatchMode.Identification));
                        return false;
                    },
                    extraAction: (cpi) =>
                    {
                        if (cpi is CopyPasteItemSubmodel item)
                        {
                            // special case: Submodel does not exist, as pasting was from external
                            if (env?.Submodels != null && item.smref != null && item.sm != null)
                            {
                                var smtest = env.FindSubmodel(item.smref);
                                if (smtest == null)
                                {
                                    env.Submodels.Add(item.sm);
                                    this.AddDiaryEntry(item.sm,
                                        new DiaryEntryStructChange(StructuralChangeReason.Create));
                                }
                            }
                        }
                    });
            }
            else
            // Cut, copy, paste within the Submodels
            if (editMode && smref == null && submodel != null && env != null)
            {
                // cut/ copy / paste
                this.DispSubmodelCutCopyPasteHelper<Submodel>(stack, repo, this.theCopyPaste,
                    env.Submodels, submodel, (sm) => { return sm.Copy(); },
                    null, submodel,
                    label: "Buffer:");
            }

            // normal edit of the submodel
            if (editMode && submodel != null)
            {
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return submodel.SubmodelElements == null || submodel.SubmodelElements.Count < 1; },
                        "This Submodel currently has no SubmodelElements, yet. " +
                            "These are the actual carriers of information. " +
                            "You could create them by clicking the 'Add ..' buttons below. " +
                            "Subsequently, when having a ISubmodelElement established, " +
                            "you could add meaning by relating it to a ConceptDefinition.",
                        severityLevel: HintCheck.Severity.Notice)
                });
                this.AddAction(
                    stack, "ISubmodelElement:",
                    new[] { "Add Property", "Add MultiLang.Prop.", "Add Collection", "Add other .." },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx >= 0 && buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AasSubmodelElements.SubmodelElement;
                            if (buttonNdx == 0)
                                en = AasSubmodelElements.Property;
                            if (buttonNdx == 1)
                                en = AasSubmodelElements.MultiLanguageProperty;
                            if (buttonNdx == 2)
                                en = AasSubmodelElements.SubmodelElementCollection;
                            if (buttonNdx == 3)
                                en = this.SelectAdequateEnum("Select SubmodelElement to create ..");

                            // ok?
                            if (en != AasSubmodelElements.SubmodelElement)
                            {
                                ISubmodelElement sme2 =
                                    AdminShellUtil.CreateSubmodelElementFromEnum(en);

                                // add
                                ISubmodelElement smw = sme2;
                                if (submodel.SubmodelElements == null)
                                    submodel.SubmodelElements = new List<ISubmodelElement>();
                                submodel.SubmodelElements.Add(smw);

                                // emit event
                                this.AddDiaryEntry(sme2, new DiaryEntryStructChange(StructuralChangeReason.Create));

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme2, isExpanded: true);
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });

                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return this.packages.AuxAvailable;  },
                        "You have opened an auxiliary AASX package. You can copy elements from it!",
                        severityLevel: HintCheck.Severity.Notice)
                });
                this.AddAction(
                    stack, "Copy from existing ISubmodelElement:",
                    new[] { "Copy single entity", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "ISubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null && env != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is ISubmodelElement)
                                {
                                    var clone = env.CopySubmodelElementAndCD(
                                        rve.theEnv, mdo as ISubmodelElement,
                                        copyCD: true, shallowCopy: buttonNdx == 0);

                                    this.MakeNewReferableUnique(clone);

                                    if (submodel.SubmodelElements == null)
                                        submodel.SubmodelElements =
                                            new List<ISubmodelElement>();

                                    // ReSharper disable once PossibleNullReferenceException -- ignore a false positive
                                    submodel.SubmodelElements.Add(clone);

                                    // emit events
                                    // TODO (MIHO, 2021-08-17): create events for CDs are not emitted!
                                    this.AddDiaryEntry(clone,
                                        new DiaryEntryStructChange(StructuralChangeReason.Create));

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        submodel, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // create ConceptDescriptions for ECLASS
                var targets = new List<ISubmodelElement>();
                this.IdentifyTargetsForEclassImportOfCDs(
                    env, new List<ISubmodelElement>(submodel.SubmodelElements),
                    ref targets);
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return submodel.SubmodelElements != null && submodel.SubmodelElements.Count > 0  &&
                                    targets.Count > 0;
                            },
                            "Consider importing ConceptDescriptions from ECLASS for existing SubmodelElements.",
                            severityLevel: HintCheck.Severity.Notice)
                });
                this.AddAction(
                    stack, "ConceptDescriptions from ECLASS:",
                    new[] { "Import missing" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // ReSharper disable RedundantCast
                            this.ImportEclassCDsForTargets(
                                env, (smref != null) ? (object)smref : (object)submodel, targets);
                            // ReSharper enable RedundantCast
                        }

                        return new AnyUiLambdaActionNone();
                    });

                this.AddAction(
                    stack, "Submodel & -elements:",
                    new[] { "Turn to kind Template", "Turn to kind Instance", "Remove qualifiers" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will affect all Kind attributes of " +
                                        "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                    "Setting Kind",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            submodel.Kind = (buttonNdx == 0)
                                ? ModelingKind.Template
                                : ModelingKind.Instance;

                            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                            {
                                // set
                                sme.Kind = (buttonNdx == 0)
                                    ? ModelingKind.Template
                                    : ModelingKind.Instance;
                                // recurse
                                return true;
                            });

                            // emit event for Submodel and children
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                        }

                        if (buttonNdx == 2)
                        {
                            if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will affect all Qualifers of " +
                                        "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                    "Remove qualifiers",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            if (submodel.Qualifiers != null)
                                submodel.Qualifiers.Clear();

                            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                            {
                                // clear
                                if (sme.Qualifiers != null)
                                    sme.Qualifiers.Clear();
                                // recurse
                                return true;
                            });

                            // emit event for Submodel and children
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                        }

                        return new AnyUiLambdaActionNone();
                    });

            }

            if (submodel != null)
            {

                // Submodel
                this.AddGroup(stack, "Submodel", this.levelColors.MainSection);

                // IReferable
                this.DisplayOrEditEntityReferable(stack, submodel, categoryUsual: false);

                // Identifiable
                this.DisplayOrEditEntityIdentifiable(
                    stack, submodel,
                    (submodel.Kind == ModelingKind.Template)
                        ? Options.Curr.TemplateIdSubmodelTemplate
                        : Options.Curr.TemplateIdSubmodelInstance,
                    new DispEditHelperModules.DispEditInjectAction(
                        new[] { "Rename" },
                        (i) =>
                        {
                            if (i == 0 && env != null)
                            {
                                var uc = new AnyUiDialogueDataTextBox(
                                    "New ID:",
                                    symbol: AnyUiMessageBoxImage.Question,
                                    maxWidth: 1400,
                                    text: submodel.Id);
                                if (this.context.StartFlyoverModal(uc))
                                {
                                    var res = false;

                                    try
                                    {
                                        // rename
                                        var lrf = env.RenameIdentifiable<Submodel>(
                                            submodel.Id, uc.Text);

                                        // use this information to emit events
                                        if (lrf != null)
                                        {
                                            res = true;
                                            foreach (var rf in lrf)
                                            {
                                                var rfi = rf.FindParentFirstIdentifiable();
                                                if (rfi != null)
                                                    this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                    }

                                    if (!res)
                                        this.context.MessageBoxFlyoutShow(
                                            "The renaming of the Submodel or some referring elements " +
                                            "has not performed successfully! Please review your inputs and " +
                                            "the AAS structure for any inconsistencies.",
                                            "Warning",
                                            AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                                    return new AnyUiLambdaActionRedrawAllElements(smref);
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        }));
                //checkForIri: submodel.Kind != null && submodel.Kind == ModelingKind.Instance);

                // HasKind
                this.DisplayOrEditEntityModelingKind(
                    stack, (ModelingKind)submodel.Kind,
                    (k) => { submodel.Kind = k; },
                    instanceExceptionStatement:
                        "Exception: if you want to declare a Submodel, which is been standardised " +
                        "by you or a standardisation body.",
                    relatedReferable: submodel);

                // HasSemanticId
                this.DisplayOrEditEntitySemanticId(stack, submodel.SemanticId,
                    (o) => { submodel.SemanticId = o; },
                    "The semanticId may be either a reference to a submodel " +
                    "with kind=Type (within the same or another Administration Shell) or " +
                    "it can be an external reference to an external standard " +
                    "defining the semantics of the submodel (for example an PDF if a standard).",
                    addExistingEntities: KeyTypes.Referable + " " + KeyTypes.Submodel + " " +
                        KeyTypes.ConceptDescription,
                    relatedReferable: submodel);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                this.DisplayOrEditEntityQualifierCollection(
                    stack, submodel.Qualifiers,
                    (q) => { submodel.Qualifiers = q; },
                    relatedReferable: submodel);

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                this.DisplayOrEditEntityHasDataSpecificationReferences(stack, submodel.DataSpecifications,
                    (ds) => { submodel.DataSpecifications = ds; },
                    relatedReferable: submodel);

            }
        }

        //
        //
        // --- Concept Description
        //
        //

        public void DisplayOrEditAasEntityConceptDescription(
            PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env,
            IReferable parentContainer, ConceptDescription cd, bool editMode,
            ModifyRepo repo,
            AnyUiStackPanel stack, bool embedded = false, bool hintMode = false, bool preventMove = false)
        {
            this.AddGroup(stack, "ConceptDescription", this.levelColors.MainSection);

            // Up/ down/ del
            if (editMode && !embedded)
            {
                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = cd,
                    ParentElem = (IClass)env.ConceptDescriptions
                };

                this.EntityListUpDownDeleteHelper<ConceptDescription>(
                    stack, repo, env.ConceptDescriptions, cd, env, "CD:", sendUpdateEvent: evTemplate,
                    preventMove: preventMove);
            }

            // Cut, copy, paste within list of CDs
            if (editMode && env != null)
            {
                // cut/ copy / paste
                this.DispPlainIdentifiableCutCopyPasteHelper<ConceptDescription>(
                    stack, repo, this.theCopyPaste,
                    env.ConceptDescriptions, cd, (o) => { return (o as ConceptDescription).Copy(); },
                    label: "Buffer:");
            }

            // IReferable
            this.DisplayOrEditEntityReferable(
                stack, cd,
                new DispEditHelperModules.DispEditInjectAction(
                    new[] { "Sync" },
                    new[] { "Copy (if target is empty) idShort to shortName and ISubmodelElement idShort." },
                    (v) =>
                    {
                        AnyUiLambdaActionBase la = new AnyUiLambdaActionNone();
                        if ((int)v != 0)
                            return la;

                        var ds = cd.GetIEC61360();
                        if (ds != null && (ds.shortName == null || ds.shortName.Count < 1))
                        {
                            ds.shortName = new LangStringSetIEC61360();
                            ds.shortName.Add(new LangString("EN?", cd.IdShort));
                            this.AddDiaryEntry(cd, new DiaryEntryStructChange());
                            la = new AnyUiLambdaActionRedrawEntity();
                        }

                        if (parentContainer != null & parentContainer is ISubmodelElement)
                        {
                            var sme = parentContainer as ISubmodelElement;
                            if (sme.IdShort == null || sme.IdShort.Trim() == "")
                            {
                                sme.IdShort = cd.IdShort;
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                                la = new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return la;
                    }),
                categoryUsual: false);

            // Identifiable

            this.DisplayOrEditEntityIdentifiable(
                stack, cd,
                Options.Curr.TemplateIdConceptDescription,
                new DispEditHelperModules.DispEditInjectAction(
                new[] { "Rename" },
                (i) =>
                {
                    if (i == 0 && env != null)
                    {
                        var uc = new AnyUiDialogueDataTextBox(
                            "New ID:",
                            symbol: AnyUiMessageBoxImage.Question,
                            maxWidth: 1400,
                            text: cd.Id);
                        if (this.context.StartFlyoverModal(uc))
                        {
                            var res = false;

                            try
                            {
                                // rename
                                var lrf = env.RenameIdentifiable<ConceptDescription>(
                                    cd.Id, uc.Text);

                                // use this information to emit events
                                if (lrf != null)
                                {
                                    res = true;
                                    foreach (var rf in lrf)
                                    {
                                        var rfi = rf.FindParentFirstIdentifiable();
                                        if (rfi != null)
                                            this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }

                            if (!res)
                                this.context.MessageBoxFlyoutShow(
                                    "The renaming of the ConceptDescription or some referring elements has not " +
                                        "performed successfully! Please review your inputs and the AAS " +
                                        "structure for any inconsistencies.",
                                        "Warning",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                            return new AnyUiLambdaActionRedrawAllElements(cd);
                        }
                    }
                    return new AnyUiLambdaActionNone();
                }));
            //checkForIri: false);

            // isCaseOf are MULTIPLE references. That is: multiple x multiple keys!
            this.DisplayOrEditEntityListOfReferences(stack, cd.IsCaseOf,
                (ico) => { cd.IsCaseOf = ico; },
                "isCaseOf", relatedReferable: cd);

            // joint header for data spec ref and content
            this.AddGroup(stack, "HasDataSpecification:", this.levelColors.SubSection);

            // check, if there is a IEC61360 content amd, subsequently, also a according data specification
            var esc = cd.EmbeddedDataSpecification?.IEC61360;
            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return esc != null && (esc.DataSpecification == null
                            || !esc.DataSpecification.Matches(
                                DataSpecificationIEC61360.GetIdentifier())); },
                        "IEC61360 content present, but data specification missing. Please add according reference.",
                        breakIfTrue: true),
                });

            //TODO:jtikekar cd.dataspecifications vs embeddedDS
            // use the normal module to edit ALL data specifications
            this.DisplayOrEditEntityHasDataSpecificationReferences(stack, cd.EmbeddedDataSpecification,
                (ds) => { cd.EmbeddedDataSpecification = ds; },
                addPresetNames: new[] { "IEC61360" },
                addPresetKeyLists: new[] {
                    new List<Key>(){ new Key(KeyTypes.GlobalReference, DataSpecificationIEC61360.GetIdentifier()) } },
                dataSpecRefsAreUsual: true, relatedReferable: cd);

            // the IEC61360 Content

            // TODO (MIHO, 2020-09-01): extend the lines below to cover also data spec. for units

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return cd.EmbeddedDataSpecification.IEC61360Content == null; },
                        "Providing an embeddedDataSpecification with IEC61360 data specification content " +
                            "is mandatory. This holds the descriptive information " +
                            "of an concept and allows for an off-line understanding of the meaning " +
                            "of an concept/ ISubmodelElement. Please create this data element.",
                        breakIfTrue: true),
                });
            if (this.SafeguardAccess(
                    stack, repo, cd.EmbeddedDataSpecification?.IEC61360Content, "embeddedDataSpecification:",
                    "Create IEC61360 data specification content",
                    v =>
                    {
                        cd.EmbeddedDataSpecification.IEC61360Content = new DataSpecificationIEC61360();
                        this.AddDiaryEntry(cd, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.DisplayOrEditEntityDataSpecificationIEC61360(stack, cd.EmbeddedDataSpecification.IEC61360Content, relatedReferable: cd);
            }
        }

        //
        //
        // --- Operation Variable
        //
        //

        public void DisplayOrEditAasEntityOperationVariable(
            PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env,
            IReferable parentContainer, OperationVariable ov, bool editMode,
            AnyUiStackPanel stack, bool hintMode = false)
        {
            //
            // Submodel Element GENERAL
            //

            // OperationVariable is a must!
            if (ov == null)
                return;

            if (editMode)
            {
                this.AddGroup(stack, "Editing of entities", this.levelColors.MainSection);

                //// entities
                //if (parentContainer != null && parentContainer is Operation)
                //    // hope is OK to refer to two lists!
                //    for (int i = 0; i < 3; i++)
                //        if ((parentContainer as Operation)[i].Contains(ov))
                //        {
                //            this.EntityListUpDownDeleteHelper<OperationVariable>(
                //                stack, repo,
                //                (parentContainer as Operation)[i],
                //                ov, env, "OperationVariable:");
                //            break;
                //        }

                // entities
                if (parentContainer != null && parentContainer is Operation operation)
                {
                    // hope is OK to refer to two lists!
                    if(operation.InputVariables.Contains(ov))
                    {
                        this.EntityListUpDownDeleteHelper<OperationVariable>(
                                stack, repo,
                                operation.InputVariables,
                                ov, env, "OperationVariable:");
                    }
                    else if(operation.OutputVariables.Contains(ov))
                    {
                        this.EntityListUpDownDeleteHelper<OperationVariable>(
                                stack, repo,
                                operation.OutputVariables,
                                ov, env, "OperationVariable:");
                    }
                    else if(operation.InoutputVariables.Contains(ov))
                    {
                        this.EntityListUpDownDeleteHelper<OperationVariable>(
                                stack, repo,
                                operation.InputVariables,
                                ov, env, "OperationVariable:");
                    }
                }

            }

            // always an OperationVariable
            if (true)
            {
                this.AddGroup(stack, "OperationVariable", this.levelColors.MainSection);

                if (ov.Value == null)
                {
                    this.AddGroup(
                        stack, "OperationVariable value is not set!", this.levelColors.SubSection);

                    if (editMode)
                    {
                        this.AddAction(
                            stack, "value:",
                            new[] { "Add Property", "Add MultiLang.Prop.", "Add Collection", "Add other .." },
                            repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx >= 0 && buttonNdx <= 3)
                                {
                                    // which adequate type?
                                    var en = AasSubmodelElements.SubmodelElement;
                                    if (buttonNdx == 0)
                                        en = AasSubmodelElements.Property;
                                    if (buttonNdx == 1)
                                        en = AasSubmodelElements
                                            .MultiLanguageProperty;
                                    if (buttonNdx == 2)
                                        en = AasSubmodelElements
                                            .SubmodelElementCollection;
                                    if (buttonNdx == 3)
                                        en = this.SelectAdequateEnum(
                                            "Select ISubmodelElement to create ..",
                                            excludeValues: new[] {
                                                AasSubmodelElements.Operation });

                                    // ok?
                                    if (en != AasSubmodelElements.SubmodelElement)
                                    {
                                        // create
                                        ISubmodelElement sme2 =
                                            AdminShellUtil.CreateSubmodelElementFromEnum(en);

                                        // add
                                        var smw = sme2;
                                        ov.Value = smw;

                                        // emit event (for parent container, e.g. Operation)
                                        this.AddDiaryEntry(parentContainer,
                                            new DiaryEntryStructChange(StructuralChangeReason.Create));

                                        // redraw
                                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: ov);
                                    }
                                }
                                return new AnyUiLambdaActionNone();
                            });

                    }
                }
                else
                {
                    // value is already set
                    // operations on it

                    if (editMode)
                    {
                        this.AddAction(stack, "value:", new[] { "Remove existing" }, repo, (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                         "Delete value, which is the dataset of a ISubmodelElement? " +
                                             "This cannot be reverted!",
                                         "AAS-ENV", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                {
                                    ov.Value = null;

                                    // emit event (for parent container, e.g. Operation)
                                    this.AddDiaryEntry(parentContainer, new DiaryEntryStructChange());

                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            return new AnyUiLambdaActionNone();
                        });

                        this.AddHintBubble(stack, hintMode, new[] {
                            new HintCheck(
                                () => { return this.packages.AuxAvailable;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                        this.AddAction(
                            stack, "Copy from existing ISubmodelElement:",
                            new[] { "Copy single", "Copy recursively" }, repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx == 0 || buttonNdx == 1)
                                {
                                    var rve = this.SmartSelectAasEntityVisualElement(
                                        packages, PackageCentral.PackageCentral.Selector.MainAux,
                                        "ISubmodelElement") as VisualElementSubmodelElement;

                                    if (rve != null)
                                    {
                                        var mdo = rve.GetMainDataObject();
                                        if (mdo != null && mdo is ISubmodelElement)
                                        {
                                            var clone = env.CopySubmodelElementAndCD(
                                                rve.theEnv, mdo as ISubmodelElement,
                                                copyCD: true,
                                                shallowCopy: buttonNdx == 0);

                                            // emit event (for parent container, e.g. Operation)
                                            this.AddDiaryEntry(parentContainer, new DiaryEntryStructChange());

                                            ov.Value = clone;
                                            return new AnyUiLambdaActionRedrawEntity();
                                        }
                                    }
                                }

                                return new AnyUiLambdaActionNone();
                            });
                    }

                    // value == ISubmodelElement is displayed
                    this.AddGroup(
                        stack, "OperationVariable value (is a ISubmodelElement)", this.levelColors.SubSection);
                    var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                    // huh, recursion in a lambda based GUI feedback function??!!
                    if (ov.Value != null && ov.Value != null) // avoid at least direct recursions!
                        DisplayOrEditAasEntitySubmodelElement(
                            packages, env, parentContainer, ov.Value, null, editMode, repo,
                            substack, hintMode);
                }
            }
        }


        //
        //
        // --- Submodel Element
        //
        //

        public void DisplayOrEditAasEntitySubmodelElement(
            PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env,
            IReferable parentContainer, ISubmodelElement wrapper,
            ISubmodelElement sme, bool editMode, ModifyRepo repo, AnyUiStackPanel stack,
            bool hintMode = false, bool nestedCds = false)
        {
            //
            // Submodel Element GENERAL
            //

            // if wrapper present, must point to the sme
            if (wrapper != null)
            {
                if (sme != null && sme != wrapper)
                    return;
                sme = wrapper;
            }

            // submodelElement is a must!
            if (sme == null)
                return;

            // edit SubmodelElements's attributes
            if (editMode)
            {
                this.AddGroup(stack, "Editing of entities", this.levelColors.MainSection);

                // for sake of space efficiency, smuggle "Refactor" into this
                var horizStack = new AnyUiWrapPanel();
                horizStack.Orientation = AnyUiOrientation.Horizontal;
                stack.Children.Add(horizStack);

                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = sme,
                    ParentElem = parentContainer
                };

                // entities helper
                if (parentContainer != null && parentContainer is Submodel && wrapper != null)
                    this.EntityListUpDownDeleteHelper<ISubmodelElement>(
                        horizStack, repo, (parentContainer as Submodel).SubmodelElements, wrapper, env,
                        "ISubmodelElement:", nextFocus: wrapper, sendUpdateEvent: evTemplate);

                if (parentContainer != null && parentContainer is SubmodelElementCollection &&
                        wrapper != null)
                    this.EntityListUpDownDeleteHelper<ISubmodelElement>(
                        horizStack, repo, (parentContainer as SubmodelElementCollection).Value,
                        wrapper, env, "ISubmodelElement:",
                        nextFocus: wrapper, sendUpdateEvent: evTemplate);

                if (parentContainer != null && parentContainer is Entity && wrapper != null)
                    this.EntityListUpDownDeleteHelper<ISubmodelElement>(
                        horizStack, repo, (parentContainer as Entity).Statements,
                        wrapper, env, "ISubmodelElement:",
                        nextFocus: wrapper, sendUpdateEvent: evTemplate);

                // refactor?
                if (parentContainer != null && parentContainer is IReferable)
                    this.AddAction(
                        horizStack, "Refactoring:",
                        new[] { "Refactor" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                // which?
                                var refactorSme = this.SmartRefactorSme(sme);
                                var parMgr = (parentContainer as ISubmodelElement);

                                // ok?
                                if (refactorSme != null && parMgr != null)
                                {
                                    // open heart surgery: change in parent container accepted
                                    parMgr.Remove(sme);
                                    parMgr.Add(refactorSme);

                                    // notify event
                                    this.AddDiaryEntry(sme,
                                        new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    this.AddDiaryEntry(refactorSme,
                                        new DiaryEntryStructChange(StructuralChangeReason.Create));

                                    // redraw
                                    return new AnyUiLambdaActionRedrawAllElements(nextFocus: refactorSme);
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        });

                // cut/ copy / paste
                if (parentContainer != null)
                {
                    this.DispSmeCutCopyPasteHelper(stack, repo, env, parentContainer, this.theCopyPaste, wrapper, sme,
                        label: "Buffer:");
                }
            }


            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (editMode)
            // ReSharper enable ConditionIsAlwaysTrueOrFalse
            {
                // guess kind or instances
                ModelingKind? parentKind = ModelingKind.Template;
                if (parentContainer != null && parentContainer is Submodel)
                    parentKind = (ModelingKind)(parentContainer as Submodel).Kind;
                if (parentContainer != null && parentContainer is SubmodelElementCollection)
                    parentKind = (ModelingKind)(parentContainer as SubmodelElementCollection).Kind;

                // relating to CDs
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return sme.SemanticId == null || sme.SemanticId.IsEmpty(); },
                            "The semanticId (see below) is empty. " +
                                "This ISubmodelElement ist currently not assigned to any ConceptDescription. " +
                                "However, it is recommended to do such assignemt. " +
                                "With the 'Assign ..' buttons below you might create and/or assign " +
                                "the ISubmodelElement to an ConceptDescription.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddAction(
                    stack, "Concept Description:",
                    new[] { "Assign to existing CD", "Create empty and assign", "Create and assign from ECLASS" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // select existing CD
                            var ks = this.SmartSelectAasEntityKeys(
                                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo);
                            if (ks != null)
                            {
                                // set the semantic id
                                sme.SemanticId = new Reference(ReferenceTypes.GlobalReference, new List<Key>(ks));

                                // if empty take over shortName
                                var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                                if ((sme.IdShort == null || sme.IdShort.Trim() == "") && cd != null)
                                {
                                    sme.IdShort = "" + cd.IdShort;
                                    if (sme.IdShort == "")
                                        sme.IdShort = cd.GetDefaultShortName();
                                }

                                // can set kind?
                                if (/*parentKind != null && */sme.Kind == null)
                                    sme.Kind = parentKind;

                                // emit event
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                            }
                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 1)
                        {
                            // create empty CD
                            var cd = new ConceptDescription(AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdConceptDescription));


                            // store in AAS enviroment
                            env.ConceptDescriptions.Add(cd);

                            // go over to ISubmodelElement
                            // set the semantic id
                            sme.SemanticId = new Reference(ReferenceTypes.ModelReference, new List<Key>() { new Key(KeyTypes.ConceptDescription, cd.Id) });

                            // can set kind?
                            if (parentKind != null && sme.Kind == null)
                                sme.Kind = parentKind;

                            // emit event
                            this.AddDiaryEntry(sme, new DiaryEntryStructChange());

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 2)
                        {
                            // feature available
                            if (Options.Curr.EclassDir == null)
                            {
                                // eclass dir?
                                this.context?.MessageBoxFlyoutShow(
                                        "The AASX Package Explore can take over ECLASS definition. " +
                                        "In order to do so, the commandine parameter -eclass has" +
                                        "to refer to a folder withe ECLASS XML files.", "Information",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                                return new AnyUiLambdaActionNone();
                            }

                            // select
                            string resIRDI = null;
                            ConceptDescription resCD = null;
                            if (this.SmartSelectEclassEntity(
                                AnyUiDialogueDataSelectEclassEntity.SelectMode.ConceptDescription,
                                ref resIRDI, ref resCD))
                            {
                                // create the concept description itself, if available,
                                // if not exactly the same is present
                                if (resCD != null)
                                {
                                    var newcd = resCD;
                                    if (null == env.FindConceptDescriptionByReference(new Reference(ReferenceTypes.ModelReference, new List<Key>() { new Key(KeyTypes.ConceptDescription, newcd.Id) })))
                                        env.ConceptDescriptions.Add(newcd);
                                }

                                // set the semantic key
                                sme.SemanticId = new Reference(ReferenceTypes.ModelReference, new List<Key>() { new Key(KeyTypes.ConceptDescription, resIRDI) });

                                // if empty take over shortName
                                var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                                if ((sme.IdShort == null || sme.IdShort.Trim() == "") && cd != null)
                                    sme.IdShort = cd.GetDefaultShortName();

                                // can set kind?
                                if (parentKind != null && sme.Kind == null)
                                    sme.Kind = parentKind;

                                // emit event
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                            }

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // create ConceptDescriptions for ECLASS
                var targets = new List<ISubmodelElement>();
                this.IdentifyTargetsForEclassImportOfCDs(
                    env, new List<ISubmodelElement>(new[] { sme }), ref targets);
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck( () => { return targets.Count > 0;  },
                        "Consider importing a ConceptDescription from ECLASS for the existing ISubmodelElement.",
                        severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddAction(
                    stack, "ConceptDescriptions from ECLASS:", new[] { "Import missing" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            this.ImportEclassCDsForTargets(env, sme, targets);
                        }

                        return new AnyUiLambdaActionNone();
                    });

            }

            if (editMode && (sme is SubmodelElementCollection || sme is Entity))
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                List<ISubmodelElement> listOfSMEW = null;
                if (sme is SubmodelElementCollection)
                    listOfSMEW = (sme as SubmodelElementCollection).Value;
                if (sme is Entity)
                    listOfSMEW = (sme as Entity).Statements;

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return listOfSMEW == null || listOfSMEW.Count < 1; },
                            "This element currently has no SubmodelElements, yet. " +
                                "These are the actual carriers of information. " +
                                "You could create them by clicking the 'Add ..' buttons below. " +
                                "Subsequently, when having a ISubmodelElement established, " +
                                "you could add meaning by relating it to a ConceptDefinition.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddAction(
                    stack, "ISubmodelElement:",
                    new[] { "Add Property", "Add MultiLang.Prop.", "Add Collection", "Add other .." },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx >= 0 && buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AasSubmodelElements.SubmodelElement;
                            if (buttonNdx == 0)
                                en = AasSubmodelElements.Property;
                            if (buttonNdx == 1)
                                en = AasSubmodelElements.MultiLanguageProperty;
                            if (buttonNdx == 2)
                                en = AasSubmodelElements.SubmodelElementCollection;
                            if (buttonNdx == 3)
                                en = this.SelectAdequateEnum("Select ISubmodelElement to create ..");

                            // ok?
                            if (en != AasSubmodelElements.SubmodelElement)
                            {
                                // create
                                ISubmodelElement sme2 =
                                    AdminShellUtil.CreateSubmodelElementFromEnum(en);

                                // add
                                if (sme is SubmodelElementCollection smesmc)
                                    smesmc.Add(sme2);
                                if (sme is Entity smeent)
                                {
                                    if(smeent.Statements == null)
                                    {
                                        smeent.Statements = new List<ISubmodelElement>();
                                    }
                                    smeent.Statements.Add(sme);
                                }

                                // notify event
                                this.AddDiaryEntry(sme2, new DiaryEntryStructChange(StructuralChangeReason.Create));

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme2);
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                });
                this.AddAction(
                    stack, "Copy from existing ISubmodelElement:", new[] { "Copy single", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "ISubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is ISubmodelElement)
                                {
                                    var clone = env.CopySubmodelElementAndCD(
                                        rve.theEnv, mdo as ISubmodelElement, copyCD: true,
                                        shallowCopy: buttonNdx == 0);

                                    if (sme is SubmodelElementCollection smesmc)
                                        smesmc.Value.Add(clone);
                                    if (sme is Entity smeent)
                                        smeent.Statements.Add(clone);

                                    // emit event
                                    this.AddDiaryEntry(sme, new DiaryEntryStructChange());

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: sme, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });
            }

            ConceptDescription jumpToCD = null;
            if (sme?.SemanticId != null && sme.SemanticId.Keys.Count > 0)
                jumpToCD = env?.FindConceptDescriptionByReference(sme.SemanticId);

            if (jumpToCD != null && editMode)
            {
                this.AddGroup(stack, "Navigation of entities", this.levelColors.MainSection);

                this.AddAction(stack, "Navigate to:", new[] { "Concept Description" }, repo, (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                    {
                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: jumpToCD, isExpanded: true);
                    }
                    return new AnyUiLambdaActionNone();
                });
            }

            if (editMode && sme is Operation smo)
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                for (int dirNdx = 0; dirNdx < 3; dirNdx++)
                {
                    var names = (new[] { "In", "Out", "InOut" })[dirNdx];
                    var dir = (
                        new[]
                        {
                            OperationVariableDirection.In,
                            OperationVariableDirection.Out,
                            OperationVariableDirection.InOut
                        })[dirNdx];

                    this.AddGroup(substack, "OperationVariables " + names, this.levelColors.SubSection);

                    List<OperationVariable> operationVariables = null;
                    if(dir == OperationVariableDirection.In)
                    {
                        operationVariables = smo.InputVariables;
                    }
                    else if(dir == OperationVariableDirection.Out)
                    {
                        operationVariables = smo.OutputVariables;
                    }
                    else if(dir == OperationVariableDirection.InOut)
                    {
                        operationVariables = smo.InoutputVariables;
                    }

                    this.AddHintBubble(
                        substack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return operationVariables == null || operationVariables.Count < 1; },
                                "This collection of OperationVariables currently has no elements, yet. " +
                                    "Please check, which in- and out-variables are required.",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                    this.AddAction(
                        substack, "OperationVariable:", new[] { "Add", "Paste into" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var ov = new OperationVariable(null); //TODO: jtikekar not a good solution to add null SME
                                operationVariables ??= new List<OperationVariable>();
                                operationVariables.Add(ov);  //TODO:jtikekar test if get assigned to corresponding in, out inOut variables in actual operation

                                // emit event
                                this.AddDiaryEntry(smo, new DiaryEntryStructChange());

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: ov);
                            }

                            if (buttonNdx == 1
                                && this.theCopyPaste?.Valid == true
                                && this.theCopyPaste.Items != null
                                && this.theCopyPaste.Items.AllOfElementType<CopyPasteItemSME>())
                            {
                                object businessObj = null;
                                foreach (var it in this.theCopyPaste.Items)
                                {
                                    // access
                                    var item = it as CopyPasteItemSME;
                                    if (item?.sme == null)
                                    {
                                        Log.Singleton.Error("When pasting SME, an element was invalid.");
                                        continue;
                                    }

                                    var smw2 = item.sme.Copy();

                                    businessObj = smo.AddChild(smw2,
                                        new EnumerationPlacmentOperationVariable()
                                        {
                                            Direction = dir
                                        });

                                    // may delete original
                                    if (!this.theCopyPaste.Duplicate)
                                    {
                                        this.DispDeleteCopyPasteItem(item);

                                        // emit event
                                        this.AddDiaryEntry(item.sme,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }

                                // emit event
                                this.AddDiaryEntry(smo, new DiaryEntryStructChange());

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: businessObj);
                            }

                            return new AnyUiLambdaActionNone();
                        });

                    this.AddHintBubble(
                        substack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return this.packages.AuxAvailable;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                    this.AddAction(
                        substack, "Copy from existing OperationVariable:", new[] { "Copy single", "Copy recursively" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0 || buttonNdx == 1)
                            {
                                var rve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                                    "OperationVariable") as VisualElementOperationVariable;

                                if (rve != null)
                                {
                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is OperationVariable)
                                    {
                                        var clone = (mdo as OperationVariable).Copy();

                                        List<OperationVariable> opVars = null;
                                        if(dir == OperationVariableDirection.In)
                                        {
                                            opVars = smo.InputVariables;
                                        }
                                        else if(dir == OperationVariableDirection.Out)
                                        {
                                            opVars = smo.OutputVariables;
                                        }
                                        else if(dir == OperationVariableDirection.InOut)
                                        {
                                            opVars = smo.InoutputVariables;
                                        }

                                        opVars ??= new List<OperationVariable>();

                                        opVars.Add(clone);

                                        // emit event
                                        this.AddDiaryEntry(smo, new DiaryEntryStructChange());

                                        return new AnyUiLambdaActionRedrawAllElements(
                                            nextFocus: smo, isExpanded: true);
                                    }
                                }
                            }

                            return new AnyUiLambdaActionNone();
                        });

                }

            }

            if (editMode && sme is AnnotatedRelationshipElement are)
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                this.AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return are.Annotations == null || are.Annotations.Count < 1; },
                            "The annotations collection currently has no elements, yet. " +
                                "Consider add DataElements or refactor to ordinary RelationshipElement.",
                            severityLevel: HintCheck.Severity.Notice)
                        });
                this.AddAction(
                    stack, "annotation:", new[] { "Add Property", "Add MultiLang.Prop.", "Add Range", "Add other .." },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx >= 0 && buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AasSubmodelElements.SubmodelElement;
                            if (buttonNdx == 0)
                                en = AasSubmodelElements.Property;
                            if (buttonNdx == 1)
                                en = AasSubmodelElements.MultiLanguageProperty;
                            if (buttonNdx == 2)
                                en = AasSubmodelElements.Range;
                            if (buttonNdx == 3)
                                en = this.SelectAdequateEnum(
                                    "Select ISubmodelElement to create ..",
                                    includeValues: new AasSubmodelElements[] {AasSubmodelElements.SubmodelElementCollection, AasSubmodelElements.RelationshipElement, AasSubmodelElements.AnnotatedRelationshipElement, AasSubmodelElements.Capability, AasSubmodelElements.Operation, AasSubmodelElements.BasicEventElement, AasSubmodelElements.Entity});

                            // ok?
                            if (en != AasSubmodelElements.SubmodelElement)
                            {
                                // create, add
                                ISubmodelElement sme2 =
                                    AdminShellUtil.CreateSubmodelElementFromEnum(en);

                                if (are.Annotations == null)
                                    are.Annotations = new List<IDataElement>();

                                are.Annotations.Add((IDataElement)sme2);

                                // emit event
                                this.AddDiaryEntry(are, new DiaryEntryStructChange(), allChildrenAffected: true);

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme2);
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });

                this.AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddAction(
                    substack, "Copy from existing DataElement:", new[] { "Copy single" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "ISubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is IDataElement)
                                {
                                    var clonesmw = (mdo as IDataElement).Copy();

                                    if (are.Annotations == null)
                                        are.Annotations = new List<IDataElement>();

                                    // ReSharper disable once PossibleNullReferenceException  -- ignore a false positive
                                    are.Annotations.Add(clonesmw);

                                    // emit event
                                    this.AddDiaryEntry(are, new DiaryEntryStructChange(), allChildrenAffected: true);

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: clonesmw, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

            }

            {
                this.AddGroup(
                    stack,
                    $"Submodel Element ({"" + sme?.GetSelfDescription().AasElementName})",
                    this.levelColors.MainSection);

                // IReferable
                this.DisplayOrEditEntityReferable(stack, sme, categoryUsual: true,
                    injectToIdShort: new DispEditHelperModules.DispEditInjectAction(
                        auxTitles: new[] { "Sync" },
                        auxToolTips: new[] { "Copy (if target is empty) idShort " +
                        "to concept desctiption idShort and shortName." },
                        auxActions: (buttonNdx) =>
                        {
                            if (sme.SemanticId != null && sme.SemanticId.Keys.Count > 0)
                            {
                                var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                                if (cd != null)
                                {
                                    if (cd.IdShort == null || cd.IdShort.Trim() == "")
                                        cd.IdShort = sme.IdShort;

                                    var ds = cd.EmbeddedDataSpecification.IEC61360Content;
                                    if (ds != null && (ds.shortName == null || ds.shortName.Count < 1))
                                    {
                                        ds.shortName = new LangStringSetIEC61360
                                        {
                                            new LangString("EN?", sme.IdShort)
                                        };
                                    }
                                        
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        }),
                    addHintsCategory: new[] {
                        new HintCheck(
                            () =>
                            {
                                return sme is Property &&
                                    (sme.Category == null || sme.Category.Trim().Length < 1);
                            },
                            "The use of category is strongly recommended for SubmodelElements which are properties! " +
                                "Please check which pre-defined category fits most " +
                                "to the application of the ISubmodelElement. \r\n" +
                                "CONSTANT => A constant property is a property with a value that " +
                                "does not change over time. " +
                                "In ECLASS this kind of category has the category 'Coded Value'. \r\n" +
                                "PARAMETER => A parameter property is a property that is once set and " +
                                "then typically does not change over time. " +
                                "This is for example the case for configuration parameters. \r\n" +
                                "VARIABLE => A variable property is a property that is calculated during runtime, " +
                                "i.e. its value is a runtime value. ",
                           severityLevel: HintCheck.Severity.Notice)
                    });

                // Kind
                this.DisplayOrEditEntityModelingKind(stack, sme.Kind,
                    (k) => { sme.Kind = k; },
                    relatedReferable: sme);

                // HasSemanticId
                this.DisplayOrEditEntitySemanticId(stack, sme.SemanticId,
                    (sid) => { sme.SemanticId = sid; },
                    "The use of semanticId for SubmodelElements is mandatory! " +
                    "Only by this means, an automatic system can identify and " +
                    "understand the meaning of the SubmodelElements and, for example, " +
                    "its unit or logical datatype. " +
                    "The semanticId shall reference to a ConceptDescription within the AAS environment " +
                    "or an external repository, such as IEC CDD or ECLASS or " +
                    "a company / consortia repository.",
                    checkForCD: true,
                    addExistingEntities: KeyTypes.ConceptDescription.ToString(),
                    cpb: theCopyPaste, relatedReferable: sme);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                this.DisplayOrEditEntityQualifierCollection(
                    stack, sme.Qualifiers,
                    (q) => { sme.Qualifiers = q; }, relatedReferable: sme);

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                this.DisplayOrEditEntityHasDataSpecificationReferences(stack, sme.DataSpecifications,
                (ds) => { sme.DataSpecifications = ds; }, relatedReferable: sme);

                //
                // ConceptDescription <- via semantic ID ?!
                //

                if (sme.SemanticId != null && sme.SemanticId.Keys.Count > 0 && !nestedCds)
                {
                    var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                    if (cd == null)
                    {
                        this.AddGroup(
                            stack, "ConceptDescription cannot be looked up within the AAS environment!",
                            this.levelColors.MainSection);
                    }
                    else
                    {
                        DisplayOrEditAasEntityConceptDescription(
                            packages, env, sme, cd, editMode, repo, stack,
                            embedded: true,
                            hintMode: hintMode);
                    }
                }

            }

            //
            // Submodel Element VALUES
            //
            if (sme is Property)
            {
                var p = sme as Property;
                this.AddGroup(stack, "Property", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p?.ValueType == null || Stringification.ToString(p.ValueType).Trim().Length < 1; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                var valType = Stringification.ToString(p.ValueType);
                this.AddKeyValueRef(
                    stack, "valueType", p, ref valType, null, repo,
                    v =>
                    {
                        p.ValueType = (DataTypeDefXsd)Stringification.DataTypeDefFromString((string)v);
                        this.AddDiaryEntry(p, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: editMode,
                    comboBoxItems: Enum.GetNames(typeof(DataTypeDefXsd)));

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p.Value == null || p.Value.Trim().Length < 1; },
                            "The value of the Property. " +
                                "Please provide a string representation " +
                                "(without quotes, '.' as decimal separator, in XML number representation).",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return true == p.Value?.Contains('\r') || true == p.Value?.Contains('\n'); },
                            "It is strongly not recommended to have multi-line properties. " +
                            "However, the technological possibility is given.",
                            severityLevel: HintCheck.Severity.Notice)

                    });

                var val = p.Value;
                this.AddKeyValueRef(
                    stack, "value", p, ref val, null, repo,
                    v =>
                    {
                        p.Value = v as string;
                        this.AddDiaryEntry(p, new DiaryEntryUpdateValue());
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: new[] { "\u2261" },
                    auxButtonToolTips: new[] { "Edit in multiline editor" },
                    auxButtonLambda: (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var uc = new AnyUiDialogueDataTextEditor(
                                caption: $"Edit Property '{"" + p.IdShort}'",
                                text: p.Value);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                p.Value = uc.Text;
                                this.AddDiaryEntry(p, new DiaryEntryUpdateValue());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return (p.Value == null || p.Value.Trim().Length < 1) &&
                                    (p.ValueId == null || p.ValueId.IsEmpty());
                            },
                            "Yon can express the value also be referring to a (enumumerated) value " +
                                "in a (the respective) repository. " +
                                "Below, you can create a reference to the value in the external repository.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                if (this.SafeguardAccess(
                        stack, repo, p.ValueId, "valueId:", "Create data element!",
                        v =>
                        {
                            p.ValueId = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                            this.AddDiaryEntry(p, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddGroup(stack, "ValueID", this.levelColors.SubSection);
                    this.AddKeyListKeys(
                        stack, "valueId", p.ValueId.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        Stringification.ToString(KeyTypes.GlobalReference),
                        relatedReferable: p,
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else if (sme is MultiLanguageProperty)
            {
                var mlp = sme as MultiLanguageProperty;
                this.AddGroup(stack, "MultiLanguageProperty", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return mlp.Value == null || mlp.Value.LangStrings.Count < 1; },
                            "Please add a string value, defined in multiple languages.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return mlp.Value.LangStrings.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, mlp.Value, "value:", "Create data element!",
                        v =>
                        {
                            mlp.Value = new LangStringSet(new List<LangString>());
                            this.AddDiaryEntry(mlp, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                    this.AddKeyListLangStr(stack, "value", mlp.Value.LangStrings, repo);

                if (this.SafeguardAccess(
                        stack, repo, mlp.ValueId, "valueId:", "Create data element!",
                        v =>
                        {
                            mlp.ValueId = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                            this.AddDiaryEntry(mlp, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddGroup(stack, "ValueID", this.levelColors.SubSection);
                    this.AddKeyListKeys(
                        stack, "valueId", mlp.ValueId.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        Stringification.ToString(KeyTypes.GlobalReference),
                        relatedReferable: mlp,
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else if (sme is AasCore.Aas3_0_RC02.Range)
            {
                var rng = sme as AasCore.Aas3_0_RC02.Range;
                this.AddGroup(stack, "Range", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rng?.ValueType == null; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                var valueType = Stringification.ToString(rng.ValueType);
                this.AddKeyValueRef(
                    stack, "valueType", rng, ref valueType, null, repo,
                    v =>
                    {
                        rng.ValueType = (DataTypeDefXsd)Stringification.DataTypeDefXsdFromString((string)v);
                        this.AddDiaryEntry(rng, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true,
                    comboBoxItems: Enum.GetNames(typeof(DataTypeDefXsd)));

                var mine = rng.Min == null || rng.Min.Trim().Length < 1;
                var maxe = rng.Max == null || rng.Max.Trim().Length < 1;

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return (rng.Kind == null || rng.Kind == ModelingKind.Instance) && mine && maxe; },
                            "Please provide either min or max.",
                            severityLevel: HintCheck.Severity.High,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return mine; },
                            "The value of the Property. " +
                                "Please provide a string representation (without quotes, '.' as decimal separator, " +
                                "in XML number representation). " +
                                "If the min value is missing then the value is assumed to be negative infinite.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                var min = rng.Min;
                this.AddKeyValueRef(
                    stack, "min", rng, ref min, null, repo,
                    v =>
                    {
                        rng.Min = v as string;
                        this.AddDiaryEntry(rng, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return maxe; },
                            "The value of the Property. " +
                                "Please provide a string representation (without quotes, '.' as decimal separator, " +
                                "in XML number representation). " +
                                "If the min value is missing then the value is assumed to be positive infinite.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                var max = rng.Max;
                this.AddKeyValueRef(
                    stack, "max", rng, ref max, null, repo,
                    v =>
                    {
                        rng.Max = v as string;
                        this.AddDiaryEntry(rng, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
            }
            else if (sme is AasCore.Aas3_0_RC02.File fl)
            {
                this.AddGroup(stack, "File", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return fl.ContentType == null || fl.ContentType.Trim().Length < 1 ||
                                    fl.ContentType.IndexOf('/') < 1 || fl.ContentType.EndsWith("/");
                            },
                            "The mime-type of the file. Mandatory information. See RFC2046.")
                    });

                var contentType = fl.ContentType;
                this.AddKeyValueRef(
                    stack, "mimeType", fl, ref contentType, null, repo,
                    v =>
                    {
                        fl.ContentType = v as string;
                        this.AddDiaryEntry(fl, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShellUtil.GetPopularMimeTypes());

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return fl.Value == null || fl.Value.Trim().Length < 1; },
                            "The path to an external file or a file relative the AASX package root('/'). " +
                                "Files are typically relative to '/aasx/' or sub-directories of it. " +
                                "External files typically comply to an URL, e.g. starting with 'https://..'.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return fl.Value.IndexOf('\\') >= 0; },
                            "Backslashes ('\') are not allow. Please use '/' as path delimiter.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                var flValue = fl.Value;
                this.AddKeyValueRef(
                    stack, "value", fl, ref flValue, null, repo,
                    v =>
                    {
                        fl.Value = v as string;
                        this.AddDiaryEntry(fl, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: new[] { "Choose supplementary file", },
                    auxButtonToolTips: new[] { "Select existing supplementary files" },
                    auxButtonLambda: (bi) =>
                    {
                        if (bi == 0)
                        {
                            // Select
                            var ve = this.SmartSelectAasEntityVisualElement(
                                        packages, PackageCentral.PackageCentral.Selector.Main, "File");
                            if (ve != null)
                            {
                                var sf = (ve.GetMainDataObject()) as AdminShellPackageSupplementaryFile;
                                if (sf != null)
                                {
                                    fl.Value = sf.Uri.ToString();
                                    this.AddDiaryEntry(fl, new DiaryEntryStructChange());
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

                if (editMode && uploadAssistance != null && packages.Main != null)
                {
                    // More file actions
                    this.AddAction(
                        stack, "Action", new[] { "Remove existing file", "Create text file", "Edit text file" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0 && fl.Value.HasContent())
                            {
                                if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                    "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                {
                                    try
                                    {
                                        // try find ..
                                        var psfs = packages.Main.GetListOfSupplementaryFiles();
                                        var psf = psfs?.FindByUri(fl.Value);
                                        if (psf == null)
                                        {
                                            Log.Singleton.Error(
                                                $"Not able to locate supplmentary file {fl.Value} for removal! " +
                                                $"Aborting!");
                                        }
                                        else
                                        {
                                            Log.Singleton.Info($"Removing file {fl.Value} ..");
                                            packages.Main.DeleteSupplementaryFile(psf);
                                            Log.Singleton.Info(
                                                $"Added {fl.Value} to pending package items to be deleted. " +
                                                "A save-operation might be required.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(
                                            ex, $"Removing file {fl.Value} in package");
                                    }

                                    // clear value
                                    fl.Value = "";

                                    // value event
                                    this.AddDiaryEntry(fl, new DiaryEntryUpdateValue());

                                    // show empty
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }

                            if (buttonNdx == 1)
                            {
                                // ask for a name
                                var uc = new AnyUiDialogueDataTextBox(
                                    "Name of text file to create",
                                    symbol: AnyUiMessageBoxImage.Question,
                                    maxWidth: 1400,
                                    text: "Textfile_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

                                if (!uc.Result)
                                {
                                    return new AnyUiLambdaActionNone();
                                }

                                var ptd = "/aasx/";
                                var ptfn = uc.Text.Trim();
                                packages.Main.PrepareSupplementaryFileParameters(ref ptd, ref ptfn);

                                // make sure the name is not already existing
                                var psfs = packages.Main.GetListOfSupplementaryFiles();
                                var psf = psfs?.FindByUri(ptd + ptfn);
                                if (psf != null)
                                {
                                    this.context?.MessageBoxFlyoutShow(
                                        $"The supplementary file {ptd + ptfn} is already existing in the " +
                                        "package. Please re-try with a different file name.", "Create text file",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                                    return new AnyUiLambdaActionNone();
                                }

                                // try execute
                                try
                                {
                                    // make temp file
                                    var tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".txt");
                                    System.IO.File.WriteAllText(tempFn, "");

                                    var mimeType = AdminShellPackageEnv.GuessMimeType(ptfn);

                                    var targetPath = packages.Main.AddSupplementaryFileToStore(
                                        tempFn, ptd, ptfn,
                                        embedAsThumb: false, useMimeType: mimeType);

                                    if (targetPath == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Error creating text-file {ptd + ptfn} within package");
                                    }
                                    else
                                    {
                                        Log.Singleton.Info(
                                            $"Added empty text-file {ptd + ptfn} to pending package items. " +
                                            $"A save-operation is required.");
                                        fl.ContentType = mimeType;
                                        fl.Value = targetPath;

                                        // value + struct event
                                        this.AddDiaryEntry(fl, new DiaryEntryStructChange());
                                        this.AddDiaryEntry(fl, new DiaryEntryUpdateValue());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"Creating text-file {ptd + ptfn} within package");
                                }
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                            }

                            if (buttonNdx == 2)
                            {
                                try
                                {
                                    // try find ..
                                    var psfs = packages.Main.GetListOfSupplementaryFiles();
                                    var psf = psfs?.FindByUri(fl.Value);
                                    if (psf == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Not able to locate supplmentary file {fl.Value} for edit. " +
                                            $"Aborting!");
                                        return new AnyUiLambdaActionNone();
                                    }

                                    // try read ..
                                    Log.Singleton.Info($"Reading text-file {fl.Value} ..");
                                    string contents;
                                    using (var stream = packages.Main.GetStreamFromUriOrLocalPackage(fl.Value))
                                    {
                                        using (var sr = new StreamReader(stream))
                                        {
                                            // read contents
                                            contents = sr.ReadToEnd();
                                        }
                                    }

                                    // test
                                    if (contents == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Not able to read contents from  supplmentary file {fl.Value} " +
                                            $"for edit. Aborting!");
                                        return new AnyUiLambdaActionNone();
                                    }

                                    // edit
                                    var uc = new AnyUiDialogueDataTextEditor(
                                                caption: $"Edit text-file '{fl.Value}'",
                                                mimeType: fl.ContentType,
                                                text: contents);
                                    if (!this.context.StartFlyoverModal(uc))
                                        return new AnyUiLambdaActionNone();

                                    // save
                                    using (var stream = packages.Main.GetStreamFromUriOrLocalPackage(
                                        fl.Value, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                    {
                                        using (var sw = new StreamWriter(stream))
                                        {
                                            // write contents
                                            sw.Write(uc.Text);
                                        }
                                    }

                                    // value event
                                    this.AddDiaryEntry(fl, new DiaryEntryUpdateValue());
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"Edit text-file {fl.Value} in package.");
                                }

                                // reshow
                                return new AnyUiLambdaActionRedrawEntity();
                            }

                            return new AnyUiLambdaActionNone();
                        });

                    // Further file assistance
                    this.AddGroup(stack, "Supplementary file assistance", this.levelColors.SubSection);

                    this.AddKeyValueRef(
                        stack, "Target path", this.uploadAssistance, ref this.uploadAssistance.TargetPath, null, repo,
                        v =>
                        {
                            this.uploadAssistance.TargetPath = v as string;
                            return new AnyUiLambdaActionNone();
                        });

                    this.AddKeyDropTarget(
                        stack, "Source file to add",
                        !(this.uploadAssistance.SourcePath.HasContent())
                            ? "(Please drop a file to set source file to add)"
                            : this.uploadAssistance.SourcePath,
                        null, repo,
                        v =>
                        {
                            this.uploadAssistance.SourcePath = v as string;
                            return new AnyUiLambdaActionRedrawEntity();
                        }, minHeight: 40);

                    this.AddAction(
                    stack, "Action", new[] { "Select source file", "Add or update to AASX" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var uc = new AnyUiDialogueDataOpenFile(
                                message: "Select a supplementary file to add..");
                                this.context?.StartFlyoverModal(uc);
                                if (uc.Result && uc.FileName != null)
                                {
                                    this.uploadAssistance.SourcePath = uc.FileName;
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }

                            if (buttonNdx == 1)
                            {
                                try
                                {
                                    var ptd = uploadAssistance.TargetPath.Trim();
                                    var ptfn = System.IO.Path.GetFileName(uploadAssistance.SourcePath);
                                    packages.Main.PrepareSupplementaryFileParameters(ref ptd, ref ptfn);

                                    var mimeType = AdminShellPackageEnv.GuessMimeType(ptfn);

                                    var targetPath = packages.Main.AddSupplementaryFileToStore(
                                        uploadAssistance.SourcePath, ptd, ptfn,
                                        embedAsThumb: false, useMimeType: mimeType);

                                    if (targetPath == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Error adding file {uploadAssistance.SourcePath} to package");
                                    }
                                    else
                                    {
                                        Log.Singleton.Info(
                                            $"Added {ptfn} to pending package items. A save-operation is required.");
                                        fl.ContentType = mimeType;
                                        fl.Value = targetPath;

                                        // value + struct event
                                        this.AddDiaryEntry(fl, new DiaryEntryStructChange());
                                        this.AddDiaryEntry(fl, new DiaryEntryUpdateValue());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"Adding file {uploadAssistance.SourcePath} to package");
                                }

                                // refresh dialogue
                                uploadAssistance.SourcePath = "";
                                return new AnyUiLambdaActionRedrawEntity();
                            }

                            return new AnyUiLambdaActionNone();
                        });
                }
            }
            else if (sme is Blob blb)
            {
                this.AddGroup(stack, "Blob", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return blb.ContentType == null || blb.ContentType.Trim().Length < 1 ||
                                    blb.ContentType.IndexOf('/') < 1 || blb.ContentType.EndsWith("/");
                            },
                            "The mime-type of the file. Mandatory information. See RFC2046.")
                    });

                var blbContentType = blb.ContentType;
                this.AddKeyValueRef(
                    stack, "mimeType", blb, ref blbContentType, null, repo,
                    v =>
                    {
                        blb.ContentType = v as string;
                        this.AddDiaryEntry(blb, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShellUtil.GetPopularMimeTypes());

                var blbValue = Encoding.Default.GetString(blb.Value);
                this.AddKeyValueRef(
                    stack, "value", blb, ref blbValue, null, repo,
                    v =>
                    {
                        blb.Value = Encoding.Default.GetBytes((string)v);
                        this.AddDiaryEntry(blb, new DiaryEntryUpdateValue());
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: new[] { "\u2261" },
                    auxButtonToolTips: new[] { "Edit in multiline editor" },
                    auxButtonLambda: (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var uc = new AnyUiDialogueDataTextEditor(
                                                caption: $"Edit Blob '{"" + blb.IdShort}'",
                                                mimeType: blb.ContentType,
                                                text: blbValue);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                blb.Value = Encoding.Default.GetBytes(uc.Text);
                                this.AddDiaryEntry(blb, new DiaryEntryUpdateValue());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });
            }
            else if (sme is ReferenceElement rfe)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "ReferenceElement", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rfe.Value == null || rfe.Value.IsEmpty(); },
                            "Please choose the target of the reference. " +
                                "You refer to any IReferable, if local within the AAS environment or outside. " +
                                "The semantics of your reference shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rfe.Value, "Target reference:", "Create data element!",
                        v =>
                        {
                            rfe.Value = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                            this.AddDiaryEntry(rfe, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    Func<List<Key>, AnyUiLambdaActionBase> lambda = (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(
                            new Reference(ReferenceTypes.ModelReference, new List<Key>(kl)), translateAssetToAAS: true);
                    };
                    this.AddKeyListKeys(stack, "value", rfe.Value.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, "",
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rfe,
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else
            if (sme is RelationshipElement rele)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "" + sme.GetSelfDescription().AasElementName, this.levelColors.MainSection);

                // re-use lambda
                Func<List<Key>, AnyUiLambdaActionBase> lambda = (kl) =>
                {
                    return new AnyUiLambdaActionNavigateTo(
                        new Reference(ReferenceTypes.ModelReference, new List<Key>(kl)), translateAssetToAAS: true);
                };

                // members
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rele.First == null || rele.First.IsEmpty(); },
                            "Please choose the first element of the relationship. " +
                                "In terms of a semantic triple, it would be the subject. " +
                                "The semantics of your reference (the predicate) shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rele.First, "First relation:", "Create data element!",
                        v =>
                        {
                            rele.First = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                            this.AddDiaryEntry(rele, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyListKeys(
                        stack, "first", rele.First.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, "",
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rele);
                }

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rele.Second == null || rele.Second.IsEmpty(); },
                            "Please choose the second element of the relationship. " +
                                "In terms of a semantic triple, it would be the object. " +
                                "The semantics of your reference (the predicate) shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rele.First, "Second relation:", "Create data element!",
                        v =>
                        {
                            rele.Second = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                            this.AddDiaryEntry(rele, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyListKeys(
                        stack, "second", rele.Second.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, "",
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rele);
                }

                // specifically for annotated relationship?
                if (sme is AnnotatedRelationshipElement /* arele */)
                {
                }
            }
            else if (sme is Capability)
            {
                this.AddGroup(stack, "Capability", this.levelColors.MainSection);
                this.AddKeyValue(stack, "Value", "Right now, Capability does not have further value elements.");
            }
            else if (sme is SubmodelElementCollection smc)
            {
                this.AddGroup(stack, "SubmodelElementCollection", this.levelColors.MainSection);
                if (smc.Value != null)
                    this.AddKeyValue(stack, "# of values", "" + smc.Value.Count);
                else
                    this.AddKeyValue(stack, "Values", "Please add elements via editing of sub-ordinate entities");
            }
            else if (sme is Operation)
            {
                var p = sme as Operation;
                this.AddGroup(stack, "Operation", this.levelColors.MainSection);
                if (p.InputVariables != null)
                    this.AddKeyValue(stack, "# of input vars.", "" + p.InputVariables.Count);
                if (p.OutputVariables != null)
                    this.AddKeyValue(stack, "# of output vars.", "" + p.OutputVariables.Count);
                if (p.InoutputVariables != null)
                    this.AddKeyValue(stack, "# of in/out vars.", "" + p.InoutputVariables.Count);
            }
            else if (sme is Entity)
            {
                var ent = sme as Entity;
                this.AddGroup(stack, "Entity", this.levelColors.MainSection);

                if (ent.Statements != null)
                    this.AddKeyValue(stack, "# of statements", "" + ent.Statements.Count);
                else
                    this.AddKeyValue(
                        stack, "Statements", "Please add statements via editing of sub-ordinate entities");

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return ent?.EntityType == null;
                                    
                            },
                            "EntityType needs to be either CoManagedEntity (no assigned AssetInformation reference) " +
                                "or SelfManagedEntity (with assigned AssetInformation reference)",
                            severityLevel: HintCheck.Severity.High)
                    });

                var entType = Stringification.ToString(ent.EntityType);
                this.AddKeyValueRef(
                    stack, "entityType", ent, ref entType, null, repo,
                    v =>
                    {
                        ent.EntityType = (EntityType)Stringification.EntityTypeFromString((string)v);
                        this.AddDiaryEntry(ent, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxItems: Enum.GetNames(typeof(EntityType)),
                    comboBoxIsEditable: true);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return ent?.EntityType != null &&
                                    ent.EntityType == EntityType.SelfManagedEntity &&
                                    (ent.GlobalAssetId == null || ent.GlobalAssetId.Keys.Count < 1);
                            },
                            "Please choose the AssetInformation for the SelfManagedEntity.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, ent.GlobalAssetId.GetAsIdentifier(), "AssetInformation:", "Create data element!",
                        v =>
                        {
                            ent.GlobalAssetId = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                            this.AddDiaryEntry(ent, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    Func<List<Key>, AnyUiLambdaActionBase> lambda = (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(
                            new Reference(ReferenceTypes.ModelReference, new List<Key>(kl)), translateAssetToAAS: true);
                    };
                    this.AddKeyListKeys(
                        /* TODO (MIHO, 2021-02-16): this mechanism is ugly and only intended to be temporary!
                           It shall be replaced (after intergrating AnyUI) by a better repo handling */
                        /* Update: already better! */
                        stack, "AssetInformation", ent.GlobalAssetId.Keys, repo, packages,
                        PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        "",
                        jumpLambda: lambda,
                        noEditJumpLambda: lambda,
                        relatedReferable: ent);
                }

            }
            else if (sme is BasicEventElement bev)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "BasicEvent", this.levelColors.MainSection);

                // attributed
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return bev.Observed == null || bev.Observed.IsEmpty(); },
                                "Please choose the Referabe, e.g. Submodel, SubmodelElementCollection or " +
                                "DataElement which is being observed. " + System.Environment.NewLine +
                                "You could refer to any IReferable, however it recommended restrict the scope " +
                                "to the local AAS or even within a Submodel.",
                            severityLevel: HintCheck.Severity.Notice)
                });
                if (this.SafeguardAccess(
                        stack, repo, bev.Observed, "observed:", "Create data element!",
                        v =>
                        {
                            bev.Observed = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                            this.AddDiaryEntry(bev, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyListKeys(stack, "observed", bev.Observed.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.Main, "",
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: (kl) =>
                        {
                            return new AnyUiLambdaActionNavigateTo(new Reference(ReferenceTypes.ModelReference, new List<Key>(kl)));
                        },
                        relatedReferable: bev);
                }

                // group
                this.AddGroup(stack, "Invocation of Events", this.levelColors.SubSection);

                this.AddAction(
                    stack, "Emit Event:", new[] { "Emit directly", "Emit with JSON payload" }, repo,
                    addWoEdit: new[] { true, true },
                    action: (buttonNdx) =>
                    {
                        string PayloadsRaw = null;

                        if (buttonNdx == 1)
                        {
                            var uc = new AnyUiDialogueDataTextEditor(
                                                caption: $"Edit raw Payload for '{"" + bev.IdShort}'",
                                                mimeType: "application/json",
                                                text: "[]");
                            if (this.context.StartFlyoverModal(uc))
                            {
                                PayloadsRaw = uc.Text;
                            }
                        }

                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            // find the observable)
                            var observable = env.FindReferableByReference(bev.Observed);

                            // send event
                            var ev = new AasEventMsgEnvelope(
                                DateTime.UtcNow,
                                source: bev.GetModelReference(),
                                sourceSemanticId: bev.SemanticId,
                                observableReference: bev.Observed,
                                observableSemanticId: (observable as IHasSemantics)?.SemanticId);

                            // specific payload?
                            if (PayloadsRaw != null)
                            {
                                ev.PayloadItems = null;
                                ev.PayloadsRaw = PayloadsRaw;
                            }

                            // emit it to PackageCentral
                            packages?.PushEvent(ev);

                            return new AnyUiLambdaActionNone();
                        }

                        return new AnyUiLambdaActionNone();
                    });
            }
            else
                this.AddGroup(stack, "Submodel Element is unknown!", this.levelColors.MainSection);
        }

        //
        //
        // --- View
        //
        //

        //Vies no more supported in V3
        //public void DisplayOrEditAasEntityView(
        //    PackageCentral.PackageCentral packages,
        //    AasCore.Aas3_0_RC02.Environment env, AssetAdministrationShell shell,
        //    View view, bool editMode, AnyUiStackPanel stack,
        //    bool hintMode = false)
        //{
        //    //
        //    // View
        //    //
        //    this.AddGroup(stack, "View", this.levelColors.MainSection);

        //    if (editMode)
        //    {
        //        // Up/ down/ del
        //        this.EntityListUpDownDeleteHelper<View>(
        //            stack, repo, shell.views.views, view, env, "View:");

        //        // let the user control the number of references
        //        this.AddHintBubble(
        //            stack, hintMode,
        //            new[] {
        //                new HintCheck(
        //                    () => { return view.containedElements == null || view.containedElements.Count < 1; },
        //                    "This View currently has no references to SubmodelElements, yet. " +
        //                        "You could create them by clicking the 'Add ..' button below.",
        //                    severityLevel: HintCheck.Severity.Notice)
        //        });
        //        this.AddAction(
        //            stack, "containedElements:", new[] { "Add Reference to ISubmodelElement", }, repo,
        //            (buttonNdx) =>
        //            {
        //                if (buttonNdx == 0)
        //                {
        //                    var ks = this.SmartSelectAasEntityKeys(
        //                                packages, PackageCentral.PackageCentral.Selector.Main, "ISubmodelElement");
        //                    if (ks != null)
        //                    {
        //                        this.AddDiaryEntry(view, new DiaryEntryStructChange());
        //                        view.AddContainedElement(ks);
        //                    }
        //                    return new AnyUiLambdaActionRedrawAllElements(nextFocus: view);
        //                }

        //                return new AnyUiLambdaActionNone();
        //            });
        //    }
        //    else
        //    {
        //        int num = 0;
        //        if (view.containedElements != null && view.containedElements.reference != null)
        //            num = view.containedElements.reference.Count;

        //        var g = this.AddSmallGrid(1, 1, new[] { "*" });
        //        this.AddSmallLabelTo(g, 0, 0, content: $"# of containedElements: {num}");
        //        stack.Children.Add(g);
        //    }

        //    // IReferable
        //    this.DisplayOrEditEntityReferable(stack, view, categoryUsual: false);

        //    // HasSemantics
        //    this.DisplayOrEditEntitySemanticId(stack, view.SemanticId,
        //        (sid) => { view.SemanticId = sid; },
        //        "Only by adding this, a computer can distinguish, for what the view is really meant for.",
        //        checkForCD: false,
        //        addExistingEntities: KeyTypes.ConceptDescription,
        //        relatedReferable: view);

        //    // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
        //    this.DisplayOrEditEntityHasDataSpecificationReferences(stack, view.hasDataSpecification,
        //        (ds) => { view.hasDataSpecification = ds; },
        //        relatedReferable: view);

        //}

        //View no more supported in V3
        //public void DisplayOrEditAasEntityViewReference(
        //    PackageCentral.PackageCentral packages, AasCore.Aas3_0_RC02.Environment env, View view,
        //    ContainedElementRef reference, bool editMode, AnyUiStackPanel stack)
        //{
        //    //
        //    // View
        //    //
        //    this.AddGroup(stack, "Reference (containedElement) of View ", this.levelColors.MainSection);

        //    if (editMode)
        //    {
        //        // Up/ down/ del
        //        this.EntityListUpDownDeleteHelper<ContainedElementRef>(
        //            stack, repo, view.containedElements.reference, reference, null, "Reference:");
        //    }

        //    // normal reference
        //    this.AddKeyListKeys(stack, "containedElement", reference.Keys, repo,
        //        packages, PackageCentral.PackageCentral.Selector.Main, "",
        //        relatedReferable: view);
        //}
    }
}
