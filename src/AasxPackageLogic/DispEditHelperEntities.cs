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
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;

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
        // --- Asset
        //
        //

        public void DisplayOrEditAasEntityAsset(
            PackageCentral.PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.AdministrationShell aas, AdminShell.AssetInformation asset,
            bool editMode, ModifyRepo repo, AnyUiStackPanel stack, bool embedded = false,
            bool hintMode = false)
        {
            this.AddGroup(stack, "Asset", this.levelColors.MainSection);

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
                            asset.globalAssetId?.GetAsIdentifier(), asset.fakeIdShort);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "When printing, an error occurred");
                    }
                    this.context?.CloseFlyover();
                }
                return new AnyUiLambdaActionNone();
            });

            // global Asset ID
            if (this.SafeguardAccess(
                    stack, repo, asset.globalAssetId, "globalAssetId:", "Create data element!",
                    v =>
                    {
                        asset.globalAssetId = new AdminShell.GlobalReference();
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListOfIdentifier(
                    stack, "globalAssetId", asset.globalAssetId.Value, repo,
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
                            asset.SetIdentification("" + AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdAsset));
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: asset);
                        }

                        if (i == 1)
                        {
                            var uc = new AnyUiDialogueDataTextBox(
                                "Global Asset ID:",
                                maxWidth: 1400,
                                symbol: AnyUiMessageBoxImage.Question,
                                options: AnyUiDialogueDataTextBox.DialogueOptions.FilterAllControlKeys,
                                text: "" + asset.globalAssetId?.GetAsIdentifier());
                            if (this.context.StartFlyoverModal(uc))
                            {
                                asset.SetIdentification("" + uc.Text);
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
                                text: "" + asset.globalAssetId?.GetAsIdentifier());
                            if (this.context.StartFlyoverModal(uc))
                            {
                                var res = false;

                                try
                                {
                                    // rename
                                    var lrf = env.RenameIdentifiable<AdminShell.AssetInformation>(
                                        asset.globalAssetId?.GetAsIdentifier(),
                                        new AdminShell.Identifier(uc.Text));

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

            // Kind
            this.DisplayOrEditEntityAssetKind(stack, asset.assetKind,
                (k) => { asset.assetKind = k; }, relatedReferable: aas);


            // list of multiple key value pairs
            this.DisplayOrEditEntityListOfIdentifierKeyValuePair(stack, asset.specificAssetId,
                (ico) => { asset.specificAssetId = ico; },
                relatedReferable: aas);

            // Thumbnail: File [0..1]
            // Note: another, may be better approach would be have a special SMWCollection constrained
            // on [0..1] and let the existing functions work on this. This could give better copy/ paste
            // and more. The serialization would then materialize (via getter/setters) this as "File" [0..1].

            this.AddGroup(stack, "DefaultThumbnail: File element", this.levelColors.SubSection,
                auxButtonTitle: (asset.defaultThumbnail == null) ? null : "Delete",
                auxButtonLambda: (o) =>
                {
                    if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                               "Delete Fiel element? This operation can not be reverted!",
                               "ConceptDescriptions",
                               AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                    {
                        asset.defaultThumbnail = null;
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }

                    return new AnyUiLambdaActionNone();
                });

            if (this.SafeguardAccess(
                stack, repo, asset.defaultThumbnail, $"defaultThumbnail:", $"Create empty File element!",
                v =>
                {
                    asset.defaultThumbnail = new AdminShell.File();
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                var substack = AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                // Note: parentContainer = null effectively seems to disable "unwanted" functionality
                DisplayOrEditAasEntitySubmodelElement(
                    packages: packages, env: env, parentContainer: null, wrapper: null,
                    sme: asset.defaultThumbnail,
                    editMode: editMode, repo: repo, stack: substack, hintMode: hintMode);
            }

        }

        //
        //
        // --- AAS Env
        //
        //

        public void DisplayOrEditAasEntityAasEnv(
            PackageCentral.PackageCentral packages, AdminShell.AdministrationShellEnv env,
            VisualElementEnvironmentItem ve, bool editMode, AnyUiStackPanel stack,
            bool hintMode = false)
        {
            this.AddGroup(stack, "Environment of Asset Administration Shells", this.levelColors.MainSection);
            if (env == null)
                return;

            // automatically and silently fix errors
            if (env.AdministrationShells == null)
                env.AdministrationShells = new AdminShell.ListOfAdministrationShells();
            if (env.ConceptDescriptions == null)
                env.ConceptDescriptions = new AdminShell.ListOfConceptDescriptions();
            if (env.Submodels == null)
                env.Submodels = new AdminShell.ListOfSubmodels();

            if (editMode &&
                (ve.theItemType == VisualElementEnvironmentItem.ItemType.Env
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.Shells
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.Assets
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions))
            {
                // some hints
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return env.AdministrationShells == null || env.AdministrationShells.Count < 1; },
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
                            "adding a SubmodelElement to a Submodel. This step is typically done after " +
                            "creating assets and Administration Shell and when creating SubmodelElements."),
                });

                // let the user control the number of entities
                this.AddAction(
                    stack, "Entities:", new[] { "Add AAS", "Add ConceptDescription" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var aas = new AdminShell.AdministrationShell();
                            this.MakeNewIdentifiableUnique(aas);
                            env.AdministrationShells.Add(aas);
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange(
                                StructuralChangeReason.Create));
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: aas);
                        }

                        if (buttonNdx == 1)
                        {
                            var cd = new AdminShell.ConceptDescription();
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
                                    AdminShell.Key.AAS) as VisualElementAdminShell;

                                if (rve != null)
                                {
                                    var copyRecursively = buttonNdx == 1 || buttonNdx == 2;
                                    var createNewIds = env == rve.theEnv;
                                    var copySupplFiles = buttonNdx == 2;

                                    var potentialSupplFilesToCopy = new Dictionary<string, string>();
                                    AdminShell.AdministrationShell destAAS = null;

                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is AdminShell.AdministrationShell sourceAAS)
                                    {
                                        //
                                        // copy AAS
                                        //
                                        try
                                        {
                                            // make a copy of the AAS itself
                                            destAAS = new AdminShell.AdministrationShell(
                                                mdo as AdminShell.AdministrationShell);
                                            if (createNewIds)
                                            {
                                                destAAS.id = new AdminShell.Identifier(
                                                    AdminShellUtil.GenerateIdAccordingTemplate(
                                                        Options.Curr.TemplateIdAas));

                                                if (destAAS.assetInformation != null)
                                                {
                                                    destAAS.assetInformation.SetIdentification(
                                                        AdminShellUtil.GenerateIdAccordingTemplate(
                                                                Options.Curr.TemplateIdAsset));
                                                }
                                            }

                                            env.AdministrationShells.Add(destAAS);
                                            this.AddDiaryEntry(destAAS, new DiaryEntryStructChange(
                                                StructuralChangeReason.Create));

                                            // clear, copy Submodels?
                                            destAAS.submodelRefs = new List<AdminShell.SubmodelRef>();
                                            if (copyRecursively && sourceAAS.submodelRefs != null)
                                            {
                                                foreach (var smr in sourceAAS.submodelRefs)
                                                {
                                                    // need access to source submodel
                                                    var srcSub = rve.theEnv.FindSubmodel(smr);
                                                    if (srcSub == null)
                                                        continue;

                                                    // get hold of suppl file infos?
                                                    if (srcSub.submodelElements != null)
                                                        foreach (var f in
                                                                srcSub.submodelElements.FindDeep<AdminShell.File>())
                                                        {
                                                            if (f != null && f.value != null &&
                                                                    f.value.StartsWith("/") &&
                                                                    !potentialSupplFilesToCopy
                                                                    .ContainsKey(f.value.ToLower().Trim()))
                                                                potentialSupplFilesToCopy[
                                                                    f.value.ToLower().Trim()] =
                                                                        f.value.ToLower().Trim();
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
                                                            destAAS.submodelRefs.Add(destSMR);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // in the same environment?
                                                        // means: we have to generate a new submodel ref 
                                                        // by using template mechanism
                                                        var tid = Options.Curr.TemplateIdSubmodelInstance;
                                                        if (srcSub.kind != null && srcSub.kind.IsTemplate)
                                                            tid = Options.Curr.TemplateIdSubmodelTemplate;

                                                        // create Submodel as deep copy 
                                                        // with new id from scratch
                                                        var dstSub = new AdminShell.Submodel(
                                                            srcSub, shallowCopy: false);
                                                        dstSub.id = new AdminShell.Identifier(
                                                            AdminShellUtil.GenerateIdAccordingTemplate(tid));

                                                        // make a new ref
                                                        var dstRef = AdminShell.SubmodelRef.CreateNew(
                                                            dstSub.GetModelReference());

                                                        // formally add this to active environment and AAS
                                                        env.Submodels.Add(dstSub);
                                                        destAAS.submodelRefs.Add(dstRef);

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
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.Assets
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions)
                {
                    // Cut, copy, paste within list of Assets
                    this.DispPlainListOfIdentifiablePasteHelper<AdminShell.Identifiable>(
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
                                if (cpiid.entity is AdminShell.AdministrationShell itaas)
                                {
                                    // new 
                                    var aas = new AdminShell.AdministrationShell(itaas);
                                    env.AdministrationShells.Add(aas);
                                    this.AddDiaryEntry(aas, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = aas;

                                    // delete
                                    if (del && cpiid.parentContainer is List<AdminShell.AdministrationShell> aasold
                                        && aasold.Contains(itaas))
                                    {
                                        aasold.Remove(itaas);
                                        this.AddDiaryEntry(itaas,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }
                                else
                                if (cpiid.entity is AdminShell.ConceptDescription itcd)
                                {
                                    // new 
                                    var cd = new AdminShell.ConceptDescription(itcd);
                                    env.ConceptDescriptions.Add(cd);
                                    this.AddDiaryEntry(cd, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = cd;

                                    // delete
                                    if (del && cpiid.parentContainer is List<AdminShell.ConceptDescription> cdold
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
                                if (cpism.sm is AdminShell.Submodel itsm)
                                {
                                    // new 
                                    var asset = new AdminShell.Submodel(itsm);
                                    env.Submodels.Add(itsm);
                                    this.AddDiaryEntry(itsm, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = asset;

                                    // delete
                                    if (del && cpism.parentContainer is List<AdminShell.Submodel> smold
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
                                    if (mdo != null && mdo is AdminShell.ConceptDescription)
                                    {
                                        var clone = new AdminShell.ConceptDescription(
                                            mdo as AdminShell.ConceptDescription);
                                        if (env.ConceptDescriptions == null)
                                            env.ConceptDescriptions = new AdminShell.ListOfConceptDescriptions();
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
                            nextFocus: env?.ConceptDescriptions)) as AnyUiComboBox;

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
                                    env.ConceptDescriptions.Sort(new AdminShell.Referable.ComparerIdShort());
                                    success = true;
                                }
                                if (ve.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.Id)
                                {
                                    env.ConceptDescriptions.Sort(new AdminShell.Identifiable.ComparerIdentification());
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
                            () => { return env.AdministrationShells.Count < 1; },
                            "There are no AdministrationShell entities in the environment. " +
                                "Select the 'Administration Shells' item on the middle panel and " +
                                "select 'Add AAS' to add a new entity."),
                        new HintCheck(
                            () => { return env.ConceptDescriptions.Count < 1; },
                            "There are no embedded ConceptDescriptions in the environment. " +
                                "It is a good practive to have those. Select or add an AdministrationShell, " +
                                "Submodel and SubmodelElement and add a ConceptDescription.",
                            severityLevel: HintCheck.Severity.Notice),
                    });

                // overview information

                var g = this.AddSmallGrid(
                    6, 1, new[] { "*" }, margin: new AnyUiThickness(5, 5, 0, 0));
                this.AddSmallLabelTo(
                    g, 0, 0, content: "This structure hold the main entites of Administration shells.");
                this.AddSmallLabelTo(
                    g, 1, 0, content: String.Format("#admin shells: {0}.", env.AdministrationShells.Count),
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
            PackageCentral.PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.AdministrationShell aas,
            bool editMode, AnyUiStackPanel stack, bool hintMode = false)
        {
            this.AddGroup(stack, "Asset Administration Shell", this.levelColors.MainSection);
            if (aas == null)
                return;

            // Entities
            if (editMode && aas?.submodelRefs != null)
            {
                this.AddGroup(stack, "Editing of entities", this.levelColors.MainSection);

                // Up/ down/ del
                this.EntityListUpDownDeleteHelper<AdminShell.AdministrationShell>(
                    stack, repo, env.AdministrationShells, aas, env, "AAS:");

                // Cut, copy, paste within list of AASes
                this.DispPlainIdentifiableCutCopyPasteHelper<AdminShell.AdministrationShell>(
                    stack, repo, this.theCopyPaste,
                    env.AdministrationShells, aas, (o) => { return new AdminShell.AdministrationShell(o); },
                    label: "Buffer:",
                    checkPasteInfo: (cpb) => cpb?.Items?.AllOfElementType<CopyPasteItemSubmodel>() == true,
                    doPasteInto: (cpi, del) =>
                    {
                        // access
                        var item = cpi as CopyPasteItemSubmodel;
                        if (item?.smref == null)
                            return null;

                        // duplicate
                        foreach (var x in aas.submodelRefs)
                            if (x?.Matches(item.smref, AdminShell.Key.MatchMode.Identification) == true)
                                return null;

                        // add 
                        var newsmr = new AdminShell.SubmodelRef(item.smref);
                        aas.submodelRefs.Add(newsmr);

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
                        if (del && item.parentContainer is AdminShell.AdministrationShell aasold
                            && aasold.submodelRefs.Contains(item.smref))
                            aasold.submodelRefs.Remove(item.smref);

                        // ok
                        return newsmr;
                    });

                // Submodels
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return aas.submodelRefs.Count < 1;  },
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
                                var smr = new AdminShell.SubmodelRef();
                                smr.Keys.AddRange(ks);
                                aas.submodelRefs.Add(smr);

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
                            var submodel = new AdminShell.Submodel();
                            this.MakeNewIdentifiableUnique(submodel);
                            this.AddDiaryEntry(submodel,
                                    new DiaryEntryStructChange(StructuralChangeReason.Create));
                            env.Submodels.Add(submodel);

                            // directly create identification, as we need it!
                            if (buttonNdx == 1)
                            {
                                submodel.id.value = AdminShellUtil.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelTemplate);
                                submodel.kind = AdminShell.ModelingKind.CreateAsTemplate();
                            }
                            else
                                submodel.id.value = AdminShellUtil.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelInstance);

                            // create ref
                            var smr = new AdminShell.SubmodelRef();
                            smr.Keys.Add(
                                new AdminShell.Key("Submodel", submodel.id.value));
                            aas.submodelRefs.Add(smr);

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
                                if (mdo != null && mdo is AdminShell.SubmodelRef)
                                {
                                    // we have 2 different use cases: 
                                    // (1) copy between AAS ENVs, 
                                    // (2) copy in one AAS ENV!
                                    if (env != rve.theEnv)
                                    {
                                        // use case (1) copy between AAS ENVs
                                        var clone = env.CopySubmodelRefAndCD(
                                            rve.theEnv, mdo as AdminShell.SubmodelRef, copySubmodel: true,
                                            copyCD: true, shallowCopy: buttonNdx == 0);
                                        if (clone == null)
                                            return new AnyUiLambdaActionNone();
                                        if (aas.submodelRefs == null)
                                            aas.submodelRefs = new List<AdminShell.SubmodelRef>();
                                        aas.submodelRefs.Add(clone);
                                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                                        return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: clone, isExpanded: true);
                                    }
                                    else
                                    {
                                        // use case (2) copy in one AAS ENV!

                                        // need access to source submodel
                                        var srcSub = rve.theEnv.FindSubmodel(mdo as AdminShell.SubmodelRef);
                                        if (srcSub == null)
                                            return new AnyUiLambdaActionNone();

                                        // means: we have to generate a new submodel ref by using template mechanism
                                        var tid = Options.Curr.TemplateIdSubmodelInstance;
                                        if (srcSub.kind != null && srcSub.kind.IsTemplate)
                                            tid = Options.Curr.TemplateIdSubmodelTemplate;

                                        // create Submodel as deep copy 
                                        // with new id from scratch
                                        var dstSub = new AdminShell.Submodel(srcSub, shallowCopy: false);
                                        dstSub.id = new AdminShell.Identifier(
                                            AdminShellUtil.GenerateIdAccordingTemplate(tid));

                                        // make a new ref
                                        var dstRef = AdminShell.SubmodelRef.CreateNew(dstSub.GetModelReference());

                                        // formally add this to active environment 
                                        env.Submodels.Add(dstSub);
                                        this.AddDiaryEntry(dstSub,
                                            new DiaryEntryStructChange(StructuralChangeReason.Create));

                                        // .. and AAS
                                        if (aas.submodelRefs == null)
                                            aas.submodelRefs = new List<AdminShell.SubmodelRef>();
                                        aas.submodelRefs.Add(dstRef);
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
            this.DisplayOrEditEntityHasDataSpecificationReferences(stack, aas.hasDataSpecification,
                (ds) => { aas.hasDataSpecification = ds; }, relatedReferable: aas);

            // Identifiable
            this.DisplayOrEditEntityIdentifiable<AdminShell.AdministrationShell>(
                env, stack, aas,
                Options.Curr.TemplateIdAas,
                null);

            // use some asset reference
            var asset = aas.assetInformation;

            // derivedFrom
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () =>
                    {
                        return asset != null && asset.assetKind != null && asset.assetKind.IsInstance &&
                            ( aas.derivedFrom == null || aas.derivedFrom.Count < 1);
                    },
                    "You have decided to create an AAS for kind = 'Instance'. " +
                        "You might derive this from another AAS of kind = 'Instance' or " +
                        "from another AAS of kind = 'Type'. It is perfectly fair to create " +
                        "an AdministrationShell with no 'derivedFrom' relation! " +
                        "However, for example, if you're an supplier of products which stem from a series-type, " +
                        "you might want to maintain a relation of the AAS's of the individual prouct instances " +
                        "to the AAS of the series type.",
                    severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(
                stack, repo, aas.derivedFrom, "derivedFrom:", "Create data element!",
                v =>
                {
                    aas.derivedFrom = new AdminShell.AssetAdministrationShellRef();
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.AddGroup(stack, "Derived From", this.levelColors.SubSection);

                Func<AdminShell.KeyList, AnyUiLambdaActionBase> lambda = (kl) =>
                {
                    return new AnyUiLambdaActionNavigateTo(
                        AdminShell.ModelReference.CreateNew(kl), translateAssetToAAS: true);
                };

                this.AddKeyListKeys(
                    stack, "derivedFrom", aas.derivedFrom.Keys, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, "AssetAdministrationShell",
                    jumpLambda: lambda, noEditJumpLambda: lambda, relatedReferable: aas);
            }

            //
            // Asset linked with AAS
            //

            if (asset != null)
            {
                DisplayOrEditAasEntityAsset(
                    packages, env, aas, asset, editMode, repo, stack, hintMode: hintMode);
            }
        }

        //
        //
        // --- Submodel Ref
        //
        //

        public void DisplayOrEditAasEntitySubmodelOrRef(
            PackageCentral.PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.AdministrationShell aas,
            AdminShell.SubmodelRef smref, AdminShell.Submodel submodel, bool editMode,
            AnyUiStackPanel stack, bool hintMode = false)
        {
            // This panel renders first the SubmodelReference and then the Submodel, below
            if (smref != null)
            {
                this.AddGroup(stack, "SubmodelReference", this.levelColors.MainSection);

                Func<AdminShell.KeyList, AnyUiLambdaActionBase> lambda = (kl) =>
                 {
                     return new AnyUiLambdaActionNavigateTo(
                         AdminShell.ModelReference.CreateNew(kl), alsoDereferenceObjects: false);
                 };

                this.AddKeyListKeys(
                    stack, "submodelRef", smref.Keys, repo,
                    packages, PackageCentral.PackageCentral.Selector.Main, "SubmodelRef Submodel ",
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

                this.EntityListUpDownDeleteHelper<AdminShell.SubmodelRef>(
                    stack, repo, aas.submodelRefs, smref, aas, "SubmodelRef:", sendUpdateEvent: evTemplate,
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
                this.DispSubmodelCutCopyPasteHelper<AdminShell.SubmodelRef>(stack, repo, this.theCopyPaste,
                    aas.submodelRefs, smref, (sr) => { return new AdminShell.SubmodelRef(sr); },
                    smref, submodel,
                    label: "Buffer:",
                    checkEquality: (r1, r2) =>
                    {
                        if (r1 != null && r2 != null)
                            return (r1.Matches(r2, AdminShell.Key.MatchMode.Identification));
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
                this.DispSubmodelCutCopyPasteHelper<AdminShell.Submodel>(stack, repo, this.theCopyPaste,
                    env.Submodels, submodel, (sm) => { return new AdminShell.Submodel(sm, shallowCopy: false); },
                    null, submodel,
                    label: "Buffer:");
            }

            // normal edit of the submodel
            if (editMode && submodel != null)
            {
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return submodel.submodelElements == null || submodel.submodelElements.Count < 1; },
                        "This Submodel currently has no SubmodelElements, yet. " +
                            "These are the actual carriers of information. " +
                            "You could create them by clicking the 'Add ..' buttons below. " +
                            "Subsequently, when having a SubmodelElement established, " +
                            "you could add meaning by relating it to a ConceptDefinition.",
                        severityLevel: HintCheck.Severity.Notice)
                });
                this.AddAction(
                    stack, "SubmodelElement:",
                    new[] { "Add Property", "Add MultiLang.Prop.", "Add Collection", "Add other .." },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx >= 0 && buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                            if (buttonNdx == 0)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                            if (buttonNdx == 1)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.MultiLanguageProperty;
                            if (buttonNdx == 2)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.SubmodelElementCollection;
                            if (buttonNdx == 3)
                                en = this.SelectAdequateEnum("Select SubmodelElement to create ..");

                            // ok?
                            if (en != AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                            {
                                AdminShell.SubmodelElement sme2 =
                                    AdminShell.SubmodelElementWrapper.CreateAdequateType(en);

                                // add
                                var smw = new AdminShell.SubmodelElementWrapper();
                                smw.submodelElement = sme2;
                                if (submodel.submodelElements == null)
                                    submodel.submodelElements = new AdminShell.SubmodelElementWrapperCollection();
                                submodel.submodelElements.Add(smw);

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
                    stack, "Copy from existing SubmodelElement:",
                    new[] { "Copy single entity", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "SubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null && env != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is AdminShell.SubmodelElement)
                                {
                                    var clone = env.CopySubmodelElementAndCD(
                                        rve.theEnv, mdo as AdminShell.SubmodelElement,
                                        copyCD: true, shallowCopy: buttonNdx == 0);

                                    this.MakeNewReferableUnique(clone?.submodelElement);

                                    if (submodel.submodelElements == null)
                                        submodel.submodelElements =
                                            new AdminShell.SubmodelElementWrapperCollection();

                                    // ReSharper disable once PossibleNullReferenceException -- ignore a false positive
                                    submodel.submodelElements.Add(clone);

                                    // emit events
                                    // TODO (MIHO, 2021-08-17): create events for CDs are not emitted!
                                    this.AddDiaryEntry(clone?.submodelElement,
                                        new DiaryEntryStructChange(StructuralChangeReason.Create));

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        submodel, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // create ConceptDescriptions for ECLASS
                var targets = new List<AdminShell.SubmodelElement>();
                this.IdentifyTargetsForEclassImportOfCDs(
                    env, AdminShell.SubmodelElementWrapper.ListOfWrappersToListOfElems(submodel.submodelElements),
                    ref targets);
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return submodel.submodelElements != null && submodel.submodelElements.Count > 0  &&
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

                            submodel.kind = (buttonNdx == 0)
                                ? AdminShell.ModelingKind.CreateAsTemplate()
                                : AdminShell.ModelingKind.CreateAsInstance();

                            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                            {
                                // set
                                sme.kind = (buttonNdx == 0)
                                    ? AdminShell.ModelingKind.CreateAsTemplate()
                                    : AdminShell.ModelingKind.CreateAsInstance();
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

                            if (submodel.qualifiers != null)
                                submodel.qualifiers.Clear();

                            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                            {
                                // clear
                                if (sme.qualifiers != null)
                                    sme.qualifiers.Clear();
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

                // Referable
                this.DisplayOrEditEntityReferable(stack, submodel, categoryUsual: false);

                // Identifiable
                this.DisplayOrEditEntityIdentifiable<AdminShell.Submodel>(
                    env, stack, submodel,
                    (submodel.kind.kind.Trim().ToLower() == "template")
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
                                    text: submodel.id.value);
                                if (this.context.StartFlyoverModal(uc))
                                {
                                    var res = false;

                                    try
                                    {
                                        // rename
                                        var lrf = env.RenameIdentifiable<AdminShell.Submodel>(
                                            submodel.id,
                                            new AdminShell.Identifier(uc.Text));

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

                // HasKind
                this.DisplayOrEditEntityModelingKind(
                    stack, submodel.kind,
                    (k) => { submodel.kind = k; },
                    instanceExceptionStatement:
                        "Exception: if you want to declare a Submodel, which is been standardised " +
                        "by you or a standardisation body.",
                    relatedReferable: submodel);

                // HasSemanticId
                this.DisplayOrEditEntitySemanticId(stack, submodel.semanticId,
                    (o) => { submodel.semanticId = o; },
                    "The semanticId may be either a reference to a submodel " +
                    "with kind=Type (within the same or another Administration Shell) or " +
                    "it can be an external reference to an external standard " +
                    "defining the semantics of the submodel (for example an PDF if a standard).",
                    addExistingEntities: AdminShell.Key.SubmodelRef + " " + AdminShell.Key.Submodel + " " +
                        AdminShell.Key.ConceptDescription,
                    relatedReferable: submodel);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                this.DisplayOrEditEntityQualifierCollection(
                    stack, submodel.qualifiers,
                    (q) => { submodel.qualifiers = q; },
                    relatedReferable: submodel);

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                this.DisplayOrEditEntityHasDataSpecificationReferences(stack, submodel.hasDataSpecification,
                    (ds) => { submodel.hasDataSpecification = ds; },
                    relatedReferable: submodel);

            }
        }

        //
        //
        // --- Concept Description
        //
        //

        public void DisplayOrEditAasEntityConceptDescription(
            PackageCentral.PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.ConceptDescription cd, bool editMode,
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
                    ParentElem = env.ConceptDescriptions
                };

                this.EntityListUpDownDeleteHelper<AdminShell.ConceptDescription>(
                    stack, repo, env.ConceptDescriptions, cd, env, "CD:", sendUpdateEvent: evTemplate,
                    preventMove: preventMove);
            }

            // Cut, copy, paste within list of CDs
            if (editMode && env != null)
            {
                // cut/ copy / paste
                this.DispPlainIdentifiableCutCopyPasteHelper<AdminShell.ConceptDescription>(
                    stack, repo, this.theCopyPaste,
                    env.ConceptDescriptions, cd, (o) => { return new AdminShell.ConceptDescription(o); },
                    label: "Buffer:");
            }

            // Referable
            this.DisplayOrEditEntityReferable(
                stack, cd,
                new DispEditHelperModules.DispEditInjectAction(
                    new[] { "Sync" },
                    new[] { "Copy (if target is empty) idShort to shortName and SubmodelElement idShort." },
                    (v) =>
                    {
                        AnyUiLambdaActionBase la = new AnyUiLambdaActionNone();
                        if ((int)v != 0)
                            return la;

                        var ds = cd.GetIEC61360();
                        if (ds != null && (ds.shortName == null || ds.shortName.Count < 1))
                        {
                            ds.shortName = new AdminShell.LangStringSetIEC61360("EN?", cd.idShort);
                            this.AddDiaryEntry(cd, new DiaryEntryStructChange());
                            la = new AnyUiLambdaActionRedrawEntity();
                        }

                        if (parentContainer != null & parentContainer is AdminShell.SubmodelElement)
                        {
                            var sme = parentContainer as AdminShell.SubmodelElement;
                            if (sme.idShort == null || sme.idShort.Trim() == "")
                            {
                                sme.idShort = cd.idShort;
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                                la = new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return la;
                    }),
                categoryUsual: false);

            // Identifiable

            this.DisplayOrEditEntityIdentifiable<AdminShell.ConceptDescription>(
                env, stack, cd,
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
                            text: cd.id.value);
                        if (this.context.StartFlyoverModal(uc))
                        {
                            var res = false;

                            try
                            {
                                // rename
                                var lrf = env.RenameIdentifiable<AdminShell.ConceptDescription>(
                                    cd.id,
                                    new AdminShell.Identifier(uc.Text));

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

            // isCaseOf are MULTIPLE references. That is: multiple x multiple keys!
            this.DisplayOrEditEntityListOfModelReferences(stack, cd.IsCaseOf,
                (ico) => { cd.IsCaseOf = ico; },
                "isCaseOf", relatedReferable: cd);

            // joint header for data spec ref and content
            this.AddGroup(stack, "HasDataSpecification:", this.levelColors.SubSection);

            // check, if there is a IEC61360 content amd, subsequently, also a according data specification
            var esc = cd.IEC61360DataSpec;
            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return esc != null && (esc.dataSpecification == null
                            || !esc.dataSpecification.MatchesExactlyOneId(
                                AdminShell.DataSpecificationIEC61360.GetIdentifier())); },
                        "IEC61360 content present, but data specification missing. Please add according reference.",
                        breakIfTrue: true),
                });

            // use the normal module to edit ALL data specifications
            this.DisplayOrEditEntityHasDataSpecificationReferences(stack, cd.embeddedDataSpecification,
                (ds) => { cd.embeddedDataSpecification = ds; },
                addPresetNames: new[] { "IEC61360" },
                addPresetKeyLists: new[] {
                    new AdminShell.ListOfIdentifier(AdminShell.DataSpecificationIEC61360.GetIdentifier())},
                dataSpecRefsAreUsual: true, relatedReferable: cd);

            // the IEC61360 Content

            // TODO (MIHO, 2020-09-01): extend the lines below to cover also data spec. for units

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return cd.IEC61360Content == null; },
                        "Providing an embeddedDataSpecification with IEC61360 data specification content " +
                            "is mandatory. This holds the descriptive information " +
                            "of an concept and allows for an off-line understanding of the meaning " +
                            "of an concept/ SubmodelElement. Please create this data element.",
                        breakIfTrue: true),
                });
            if (this.SafeguardAccess(
                    stack, repo, cd.IEC61360Content, "embeddedDataSpecification:",
                    "Create IEC61360 data specification content",
                    v =>
                    {
                        cd.IEC61360Content = new AdminShell.DataSpecificationIEC61360();
                        this.AddDiaryEntry(cd, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.DisplayOrEditEntityDataSpecificationIEC61360(stack, cd.IEC61360Content, relatedReferable: cd);
            }
        }

        //
        //
        // --- Operation Variable
        //
        //

        public void DisplayOrEditAasEntityOperationVariable(
            PackageCentral.PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.OperationVariable ov, bool editMode,
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

                // entities
                if (parentContainer != null && parentContainer is AdminShell.Operation)
                    // hope is OK to refer to two lists!
                    for (int i = 0; i < 3; i++)
                        if ((parentContainer as AdminShell.Operation)[i].Contains(ov))
                        {
                            this.EntityListUpDownDeleteHelper<AdminShell.OperationVariable>(
                                stack, repo,
                                (parentContainer as AdminShell.Operation)[i],
                                ov, env, "OperationVariable:");
                            break;
                        }

            }

            // always an OperationVariable
            if (true)
            {
                this.AddGroup(stack, "OperationVariable", this.levelColors.MainSection);

                if (ov.value == null)
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
                                    var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                                    if (buttonNdx == 0)
                                        en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                                    if (buttonNdx == 1)
                                        en = AdminShell
                                            .SubmodelElementWrapper
                                            .AdequateElementEnum
                                            .MultiLanguageProperty;
                                    if (buttonNdx == 2)
                                        en = AdminShell
                                            .SubmodelElementWrapper
                                            .AdequateElementEnum
                                            .SubmodelElementCollection;
                                    if (buttonNdx == 3)
                                        en = this.SelectAdequateEnum(
                                            "Select SubmodelElement to create ..",
                                            excludeValues: new[] {
                                                AdminShell.SubmodelElementWrapper.AdequateElementEnum.Operation });

                                    // ok?
                                    if (en != AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                                    {
                                        // create
                                        AdminShell.SubmodelElement sme2 =
                                            AdminShell.SubmodelElementWrapper.CreateAdequateType(en);

                                        // add
                                        var smw = new AdminShell.SubmodelElementWrapper();
                                        smw.submodelElement = sme2;
                                        ov.value = smw;

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
                                         "Delete value, which is the dataset of a SubmodelElement? " +
                                             "This cannot be reverted!",
                                         "AAS-ENV", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                {
                                    ov.value = null;

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
                            stack, "Copy from existing SubmodelElement:",
                            new[] { "Copy single", "Copy recursively" }, repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx == 0 || buttonNdx == 1)
                                {
                                    var rve = this.SmartSelectAasEntityVisualElement(
                                        packages, PackageCentral.PackageCentral.Selector.MainAux,
                                        "SubmodelElement") as VisualElementSubmodelElement;

                                    if (rve != null)
                                    {
                                        var mdo = rve.GetMainDataObject();
                                        if (mdo != null && mdo is AdminShell.SubmodelElement)
                                        {
                                            var clone = env.CopySubmodelElementAndCD(
                                                rve.theEnv, mdo as AdminShell.SubmodelElement,
                                                copyCD: true,
                                                shallowCopy: buttonNdx == 0);

                                            // emit event (for parent container, e.g. Operation)
                                            this.AddDiaryEntry(parentContainer, new DiaryEntryStructChange());

                                            ov.value = clone;
                                            return new AnyUiLambdaActionRedrawEntity();
                                        }
                                    }
                                }

                                return new AnyUiLambdaActionNone();
                            });
                    }

                    // value == SubmodelElement is displayed
                    this.AddGroup(
                        stack, "OperationVariable value (is a SubmodelElement)", this.levelColors.SubSection);
                    var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                    // huh, recursion in a lambda based GUI feedback function??!!
                    if (ov.value != null && ov.value.submodelElement != null) // avoid at least direct recursions!
                        DisplayOrEditAasEntitySubmodelElement(
                            packages, env, parentContainer, ov.value, null, editMode, repo,
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
            PackageCentral.PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.SubmodelElementWrapper wrapper,
            AdminShell.SubmodelElement sme, bool editMode, ModifyRepo repo, AnyUiStackPanel stack,
            bool hintMode = false, bool nestedCds = false)
        {
            //
            // Submodel Element GENERAL
            //

            // if wrapper present, must point to the sme
            if (wrapper != null)
            {
                if (sme != null && sme != wrapper.submodelElement)
                    return;
                sme = wrapper.submodelElement;
            }

            // submodelElement is a must!
            if (sme == null)
                return;

            // edit SubmodelElements's attributes
            if (editMode && parentContainer != null)
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
                if (parentContainer is AdminShell.Submodel && wrapper != null)
                    this.EntityListUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(
                        horizStack, repo, (parentContainer as AdminShell.Submodel).submodelElements, wrapper, env,
                        "SubmodelElement:", nextFocus: wrapper.submodelElement, sendUpdateEvent: evTemplate);

                if (parentContainer is AdminShell.SubmodelElementCollection &&
                        wrapper != null)
                    this.EntityListUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(
                        horizStack, repo, (parentContainer as AdminShell.SubmodelElementCollection).value,
                        wrapper, env, "SubmodelElement:",
                        nextFocus: wrapper.submodelElement, sendUpdateEvent: evTemplate);

                if (parentContainer is AdminShell.Entity && wrapper != null)
                    this.EntityListUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(
                        horizStack, repo, (parentContainer as AdminShell.Entity).statements,
                        wrapper, env, "SubmodelElement:",
                        nextFocus: wrapper.submodelElement, sendUpdateEvent: evTemplate);

                // refactor?
                if (parentContainer is AdminShell.IManageSubmodelElements)
                    this.AddAction(
                        horizStack, "Refactoring:",
                        new[] { "Refactor" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                // which?
                                var refactorSme = this.SmartRefactorSme(sme);
                                var parMgr = (parentContainer as AdminShell.IManageSubmodelElements);

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
                this.DispSmeCutCopyPasteHelper(stack, repo, env, parentContainer, this.theCopyPaste, wrapper, sme,
                    label: "Buffer:");
            }

            // else:
            if (editMode && parentContainer == null)
            {
                // use "emergency mode"
                this.DispSmeCutCopyPasteHelper(stack, repo, env, parentContainer: null, this.theCopyPaste, wrapper, sme,
                    label: "Buffer:");
            }

            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (editMode)
            // ReSharper enable ConditionIsAlwaysTrueOrFalse
            {
                // guess kind or instances
                AdminShell.ModelingKind parentKind = null;
                if (parentContainer != null && parentContainer is AdminShell.Submodel)
                    parentKind = (parentContainer as AdminShell.Submodel).kind;
                if (parentContainer != null && parentContainer is AdminShell.SubmodelElementCollection)
                    parentKind = (parentContainer as AdminShell.SubmodelElementCollection).kind;

                // relating to CDs
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return sme.semanticId == null || sme.semanticId.IsEmpty; },
                            "The semanticId (see below) is empty. " +
                                "This SubmodelElement ist currently not assigned to any ConceptDescription. " +
                                "However, it is recommended to do such assignment. " +
                                "With the 'Assign ..' buttons below you might create and/or assign " +
                                "the SubmodelElement to an ConceptDescription.",
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
                                sme.semanticId = AdminShell.SemanticId.CreateFromKeys(ks);

                                // if empty take over shortName
                                var cd = env.FindConceptDescription(sme.semanticId);
                                if ((sme.idShort == null || sme.idShort.Trim() == "") && cd != null)
                                {
                                    sme.idShort = "" + cd.idShort;
                                    if (sme.idShort == "")
                                        sme.idShort = cd.GetDefaultShortName();
                                }

                                // can set kind?
                                if (parentKind != null && sme.kind == null)
                                    sme.kind = new AdminShell.ModelingKind(parentKind);

                                // emit event
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                            }
                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 1)
                        {
                            // create empty CD
                            var cd = new AdminShell.ConceptDescription();

                            // make an ID, automatically
                            cd.id.value = AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdConceptDescription);

                            // store in AAS enviroment
                            env.ConceptDescriptions.Add(cd);

                            // go over to SubmodelElement
                            // set the semantic id
                            sme.semanticId = AdminShell.SemanticId.CreateFromKey(
                                new AdminShell.Key("ConceptDescription", cd.id.value));

                            // can set kind?
                            if (parentKind != null && sme.kind == null)
                                sme.kind = new AdminShell.ModelingKind(parentKind);

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
                            AdminShell.ConceptDescription resCD = null;
                            if (this.SmartSelectEclassEntity(
                                AnyUiDialogueDataSelectEclassEntity.SelectMode.ConceptDescription,
                                ref resIRDI, ref resCD))
                            {
                                // create the concept description itself, if available,
                                // if not exactly the same is present
                                if (resCD != null)
                                {
                                    var newcd = resCD;
                                    if (null == env.FindConceptDescription(
                                            AdminShell.Key.CreateNew(
                                                AdminShell.Key.ConceptDescription, newcd.id.value)))
                                        env.ConceptDescriptions.Add(newcd);
                                }

                                // set the semantic key
                                sme.semanticId = AdminShell.SemanticId.CreateFromKey(
                                    new AdminShell.Key(AdminShell.Key.ConceptDescription, resIRDI));

                                // if empty take over shortName
                                var cd = env.FindConceptDescription(sme.semanticId);
                                if ((sme.idShort == null || sme.idShort.Trim() == "") && cd != null)
                                    sme.idShort = cd.GetDefaultShortName();

                                // can set kind?
                                if (parentKind != null && sme.kind == null)
                                    sme.kind = new AdminShell.ModelingKind(parentKind);

                                // emit event
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                            }

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // create ConceptDescriptions for ECLASS
                var targets = new List<AdminShell.SubmodelElement>();
                this.IdentifyTargetsForEclassImportOfCDs(
                    env, new List<AdminShell.SubmodelElement>(new[] { sme }), ref targets);
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck( () => { return targets.Count > 0;  },
                        "Consider importing a ConceptDescription from ECLASS for the existing SubmodelElement.",
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

            if (editMode && (sme is AdminShell.SubmodelElementCollection || sme is AdminShell.Entity))
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                List<AdminShell.SubmodelElementWrapper> listOfSMEW = null;
                if (sme is AdminShell.SubmodelElementCollection)
                    listOfSMEW = (sme as AdminShell.SubmodelElementCollection).value;
                if (sme is AdminShell.Entity)
                    listOfSMEW = (sme as AdminShell.Entity).statements;

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return listOfSMEW == null || listOfSMEW.Count < 1; },
                            "This element currently has no SubmodelElements, yet. " +
                                "These are the actual carriers of information. " +
                                "You could create them by clicking the 'Add ..' buttons below. " +
                                "Subsequently, when having a SubmodelElement established, " +
                                "you could add meaning by relating it to a ConceptDefinition.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddAction(
                    stack, "SubmodelElement:",
                    new[] { "Add Property", "Add MultiLang.Prop.", "Add Collection", "Add other .." },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx >= 0 && buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                            if (buttonNdx == 0)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                            if (buttonNdx == 1)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.MultiLanguageProperty;
                            if (buttonNdx == 2)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.SubmodelElementCollection;
                            if (buttonNdx == 3)
                                en = this.SelectAdequateEnum("Select SubmodelElement to create ..");

                            // ok?
                            if (en != AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                            {
                                // create
                                AdminShell.SubmodelElement sme2 =
                                    AdminShell.SubmodelElementWrapper.CreateAdequateType(en);

                                // add
                                if (sme is AdminShell.SubmodelElementCollection smesmc)
                                    smesmc.Add(sme2);
                                if (sme is AdminShell.Entity smeent)
                                    smeent.Add(sme2);

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
                    stack, "Copy from existing SubmodelElement:", new[] { "Copy single", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "SubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is AdminShell.SubmodelElement)
                                {
                                    var clone = env.CopySubmodelElementAndCD(
                                        rve.theEnv, mdo as AdminShell.SubmodelElement, copyCD: true,
                                        shallowCopy: buttonNdx == 0);

                                    if (sme is AdminShell.SubmodelElementCollection smesmc)
                                        smesmc.value.Add(clone);
                                    if (sme is AdminShell.Entity smeent)
                                        smeent.statements.Add(clone);

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

            AdminShell.ConceptDescription jumpToCD = null;
            if (sme.semanticId != null && sme.semanticId.Count > 0)
                jumpToCD = env.FindConceptDescription(sme.semanticId);

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

            if (editMode && sme is AdminShell.Operation smo)
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                for (int dirNdx = 0; dirNdx < 3; dirNdx++)
                {
                    var names = (new[] { "In", "Out", "InOut" })[dirNdx];
                    var dir = (
                        new[]
                        {
                            AdminShell.OperationVariable.Direction.In,
                            AdminShell.OperationVariable.Direction.Out,
                            AdminShell.OperationVariable.Direction.InOut
                        })[dirNdx];

                    this.AddGroup(substack, "OperationVariables " + names, this.levelColors.SubSection);

                    this.AddHintBubble(
                        substack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return smo[dir] == null || smo[dir].Count < 1; },
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
                                var ov = new AdminShell.OperationVariable();
                                if (smo[dir] == null)
                                    smo[dir] = new List<AdminShell.OperationVariable>();
                                smo[dir].Add(ov);

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

                                    var smw2 = new AdminShell.SubmodelElementWrapper(item.sme, shallowCopy: false);

                                    if (smo is AdminShell.IEnumerateChildren smeec)
                                        businessObj = smeec.AddChild(smw2,
                                            new AdminShell.Operation.EnumerationPlacmentOperationVariable()
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
                                    if (mdo != null && mdo is AdminShell.OperationVariable)
                                    {
                                        var clone = new AdminShell.OperationVariable(
                                            mdo as AdminShell.OperationVariable, shallowCopy: buttonNdx == 0);

                                        if (smo[dir] == null)
                                            smo[dir] = new List<AdminShell.OperationVariable>();

                                        smo[dir].Add(clone);

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

            if (editMode && sme is AdminShell.AnnotatedRelationshipElement are)
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                this.AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return are.annotations == null || are.annotations.Count < 1; },
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
                            var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                            if (buttonNdx == 0)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                            if (buttonNdx == 1)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.MultiLanguageProperty;
                            if (buttonNdx == 2)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Range;
                            if (buttonNdx == 3)
                                en = this.SelectAdequateEnum(
                                    "Select SubmodelElement to create ..",
                                    includeValues: AdminShell.SubmodelElementWrapper.AdequateElementsDataElement);

                            // ok?
                            if (en != AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                            {
                                // create, add
                                AdminShell.SubmodelElement sme2 =
                                    AdminShell.SubmodelElementWrapper.CreateAdequateType(en);

                                if (are.annotations == null)
                                    are.annotations = new AdminShell.DataElementWrapperCollection();

                                are.annotations.Add(sme2);

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
                                "SubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is AdminShell.DataElement)
                                {
                                    var clonesmw = new AdminShell.SubmodelElementWrapper(
                                        mdo as AdminShell.DataElement, shallowCopy: true);

                                    if (are.annotations == null)
                                        are.annotations = new AdminShell.DataElementWrapperCollection();

                                    // ReSharper disable once PossibleNullReferenceException  -- ignore a false positive
                                    are.annotations.Add(clonesmw);

                                    // emit event
                                    this.AddDiaryEntry(are, new DiaryEntryStructChange(), allChildrenAffected: true);

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: clonesmw.submodelElement, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

            }

            {
                this.AddGroup(
                    stack,
                    $"Submodel Element ({"" + sme?.GetElementName()})",
                    this.levelColors.MainSection);

                // Referable
                this.DisplayOrEditEntityReferable(stack, sme, categoryUsual: true,
                    injectToIdShort: new DispEditHelperModules.DispEditInjectAction(
                        auxTitles: new[] { "Sync" },
                        auxToolTips: new[] { "Copy (if target is empty) idShort " +
                        "to concept desctiption idShort and shortName." },
                        auxActions: (buttonNdx) =>
                        {
                            if (sme.semanticId != null && sme.semanticId.Count > 0)
                            {
                                var cd = env.FindConceptDescription(sme.semanticId);
                                if (cd != null)
                                {
                                    if (cd.idShort == null || cd.idShort.Trim() == "")
                                        cd.idShort = sme.idShort;

                                    var ds = cd.IEC61360Content;
                                    if (ds != null && (ds.shortName == null || ds.shortName.Count < 1))
                                        ds.shortName = new AdminShell.LangStringSetIEC61360("EN?", sme.idShort);

                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        }),
                    addHintsCategory: new[] {
                        new HintCheck(
                            () =>
                            {
                                return sme is AdminShell.Property &&
                                    (sme.category == null || sme.category.Trim().Length < 1);
                            },
                            "The use of category is strongly recommended for SubmodelElements which are properties! " +
                                "Please check which pre-defined category fits most " +
                                "to the application of the SubmodelElement. \r\n" +
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
                this.DisplayOrEditEntityModelingKind(stack, sme.kind,
                    (k) => { sme.kind = k; },
                    relatedReferable: sme);

                // HasSemanticId
                this.DisplayOrEditEntitySemanticId(stack, sme.semanticId,
                    (sid) => { sme.semanticId = sid; },
                    "The use of semanticId for SubmodelElements is mandatory! " +
                    "Only by this means, an automatic system can identify and " +
                    "understand the meaning of the SubmodelElements and, for example, " +
                    "its unit or logical datatype. " +
                    "The semanticId shall reference to a ConceptDescription within the AAS environment " +
                    "or an external repository, such as IEC CDD or ECLASS or " +
                    "a company / consortia repository.",
                    checkForCD: true,
                    addExistingEntities: AdminShell.Key.ConceptDescription,
                    cpb: theCopyPaste, relatedReferable: sme);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                this.DisplayOrEditEntityQualifierCollection(
                    stack, sme.qualifiers,
                    (q) => { sme.qualifiers = q; }, relatedReferable: sme);

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                this.DisplayOrEditEntityHasDataSpecificationReferences(stack, sme.hasDataSpecification,
                (ds) => { sme.hasDataSpecification = ds; }, relatedReferable: sme);

                //
                // ConceptDescription <- via semantic ID ?!
                //

                if (sme.semanticId != null && sme.semanticId.Count > 0 && !nestedCds)
                {
                    var cd = env.FindConceptDescription(sme.semanticId);
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
            if (sme is AdminShell.Property)
            {
                var p = sme as AdminShell.Property;
                this.AddGroup(stack, "Property", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p.valueType == null || p.valueType.Trim().Length < 1; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddKeyValueRef(
                    stack, "valueType", p, ref p.valueType, null, repo,
                    v =>
                    {
                        p.valueType = v as string;
                        this.AddDiaryEntry(p, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: editMode,
                    comboBoxItems: AdminShell.DataElement.ValueTypeItems);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p.value == null || p.value.Trim().Length < 1; },
                            "The value of the Property. " +
                                "Please provide a string representation " +
                                "(without quotes, '.' as decimal separator, in XML number representation).",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return true == p.value?.Contains('\r') || true == p.value?.Contains('\n'); },
                            "It is strongly not recommended to have multi-line properties. " +
                            "However, the technological possibility is given.",
                            severityLevel: HintCheck.Severity.Notice)

                    });
                this.AddKeyValueRef(
                    stack, "value", p, ref p.value, null, repo,
                    v =>
                    {
                        p.value = v as string;
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
                                caption: $"Edit Property '{"" + p.idShort}'",
                                text: p.value);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                p.value = uc.Text;
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
                                return (p.value == null || p.value.Trim().Length < 1) &&
                                    (p.valueId == null || p.valueId.IsEmpty);
                            },
                            "Yon can express the value also be referring to a (enumumerated) value " +
                                "in a (the respective) repository. " +
                                "Below, you can create a reference to the value in the external repository.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                if (this.SafeguardAccess(
                        stack, repo, p.valueId, "valueId:", "Create data element!",
                        v =>
                        {
                            p.valueId = new AdminShell.GlobalReference();
                            this.AddDiaryEntry(p, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddGroup(stack, "ValueID", this.levelColors.SubSection);
                    this.AddKeyListOfIdentifier(
                        stack, "valueId", p.valueId.Value, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        AdminShell.Key.GlobalReference,
                        relatedReferable: p,
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else if (sme is AdminShell.MultiLanguageProperty)
            {
                var mlp = sme as AdminShell.MultiLanguageProperty;
                this.AddGroup(stack, "MultiLanguageProperty", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return mlp.value == null || mlp.value.Count < 1; },
                            "Please add a string value, defined in multiple languages.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return mlp.value.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, mlp.value, "value:", "Create data element!",
                        v =>
                        {
                            mlp.value = new AdminShell.LangStringSet();
                            this.AddDiaryEntry(mlp, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                    this.AddKeyListLangStr(stack, "value", mlp.value.langString, repo);

                if (this.SafeguardAccess(
                        stack, repo, mlp.valueId, "valueId:", "Create data element!",
                        v =>
                        {
                            mlp.valueId = new AdminShell.GlobalReference();
                            this.AddDiaryEntry(mlp, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddGroup(stack, "ValueID", this.levelColors.SubSection);
                    this.AddKeyListOfIdentifier(
                        stack, "valueId", mlp.valueId.Value, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        AdminShell.Key.GlobalReference,
                        relatedReferable: mlp,
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else if (sme is AdminShell.Range)
            {
                var rng = sme as AdminShell.Range;
                this.AddGroup(stack, "Range", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rng.valueType == null || rng.valueType.Trim().Length < 1; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddKeyValueRef(
                    stack, "valueType", rng, ref rng.valueType, null, repo,
                    v =>
                    {
                        rng.valueType = v as string;
                        this.AddDiaryEntry(rng, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.DataElement.ValueTypeItems);

                var mine = rng.min == null || rng.min.Trim().Length < 1;
                var maxe = rng.max == null || rng.max.Trim().Length < 1;

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return (rng.kind == null || rng.kind.IsInstance) && mine && maxe; },
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
                this.AddKeyValueRef(
                    stack, "min", rng, ref rng.min, null, repo,
                    v =>
                    {
                        rng.min = v as string;
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
                this.AddKeyValueRef(
                    stack, "max", rng, ref rng.max, null, repo,
                    v =>
                    {
                        rng.max = v as string;
                        this.AddDiaryEntry(rng, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
            }
            else if (sme is AdminShell.File fl)
            {
                this.AddGroup(stack, "File", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return fl.mimeType == null || fl.mimeType.Trim().Length < 1 ||
                                    fl.mimeType.IndexOf('/') < 1 || fl.mimeType.EndsWith("/");
                            },
                            "The mime-type of the file. Mandatory information. See RFC2046.")
                    });
                this.AddKeyValueRef(
                    stack, "mimeType", fl, ref fl.mimeType, null, repo,
                    v =>
                    {
                        fl.mimeType = v as string;
                        this.AddDiaryEntry(fl, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.File.GetPopularMimeTypes());

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return fl.value == null || fl.value.Trim().Length < 1; },
                            "The path to an external file or a file relative the AASX package root('/'). " +
                                "Files are typically relative to '/aasx/' or sub-directories of it. " +
                                "External files typically comply to an URL, e.g. starting with 'https://..'.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return fl.value.IndexOf('\\') >= 0; },
                            "Backslashes ('\') are not allow. Please use '/' as path delimiter.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddKeyValueRef(
                    stack, "value", fl, ref fl.value, null, repo,
                    v =>
                    {
                        fl.value = v as string;
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
                                    fl.value = sf.Uri.ToString();
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
                            if (buttonNdx == 0 && fl.value.HasContent())
                            {
                                if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                    "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                {
                                    try
                                    {
                                        // try find ..
                                        var psfs = packages.Main.GetListOfSupplementaryFiles();
                                        var psf = psfs?.FindByUri(fl.value);
                                        if (psf == null)
                                        {
                                            Log.Singleton.Error(
                                                $"Not able to locate supplmentary file {fl.value} for removal! " +
                                                $"Aborting!");
                                        }
                                        else
                                        {
                                            Log.Singleton.Info($"Removing file {fl.value} ..");
                                            packages.Main.DeleteSupplementaryFile(psf);
                                            Log.Singleton.Info(
                                                $"Added {fl.value} to pending package items to be deleted. " +
                                                "A save-operation might be required.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(
                                            ex, $"Removing file {fl.value} in package");
                                    }

                                    // clear value
                                    fl.value = "";

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
                                        fl.mimeType = mimeType;
                                        fl.value = targetPath;

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
                                    var psf = psfs?.FindByUri(fl.value);
                                    if (psf == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Not able to locate supplmentary file {fl.value} for edit. " +
                                            $"Aborting!");
                                        return new AnyUiLambdaActionNone();
                                    }

                                    // try read ..
                                    Log.Singleton.Info($"Reading text-file {fl.value} ..");
                                    string contents;
                                    using (var stream = packages.Main.GetStreamFromUriOrLocalPackage(fl.value))
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
                                            $"Not able to read contents from  supplmentary file {fl.value} " +
                                            $"for edit. Aborting!");
                                        return new AnyUiLambdaActionNone();
                                    }

                                    // edit
                                    var uc = new AnyUiDialogueDataTextEditor(
                                                caption: $"Edit text-file '{fl.value}'",
                                                mimeType: fl.mimeType,
                                                text: contents);
                                    if (!this.context.StartFlyoverModal(uc))
                                        return new AnyUiLambdaActionNone();

                                    // save
                                    using (var stream = packages.Main.GetStreamFromUriOrLocalPackage(
                                        fl.value, FileMode.OpenOrCreate, FileAccess.ReadWrite))
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
                                        ex, $"Edit text-file {fl.value} in package.");
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
                                        fl.mimeType = mimeType;
                                        fl.value = targetPath;

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
            else if (sme is AdminShell.Blob blb)
            {
                this.AddGroup(stack, "Blob", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return blb.mimeType == null || blb.mimeType.Trim().Length < 1 ||
                                    blb.mimeType.IndexOf('/') < 1 || blb.mimeType.EndsWith("/");
                            },
                            "The mime-type of the file. Mandatory information. See RFC2046.")
                    });
                this.AddKeyValueRef(
                    stack, "mimeType", blb, ref blb.mimeType, null, repo,
                    v =>
                    {
                        blb.mimeType = v as string;
                        this.AddDiaryEntry(blb, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.File.GetPopularMimeTypes());

                this.AddKeyValueRef(
                    stack, "value", blb, ref blb.value, null, repo,
                    v =>
                    {
                        blb.value = v as string;
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
                                                caption: $"Edit Blob '{"" + blb.idShort}'",
                                                mimeType: blb.mimeType,
                                                text: blb.value);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                blb.value = uc.Text;
                                this.AddDiaryEntry(blb, new DiaryEntryUpdateValue());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });
            }
            else if (sme is AdminShell.ReferenceElement rfe)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "ReferenceElement", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rfe.value == null || rfe.value.IsEmpty; },
                            "Please choose the target of the reference. " +
                                "You refer to any Referable, if local within the AAS environment or outside. " +
                                "The semantics of your reference shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rfe.value, "Target reference:", "Create data element!",
                        v =>
                        {
                            rfe.value = new AdminShell.ModelReference();
                            this.AddDiaryEntry(rfe, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    Func<AdminShell.KeyList, AnyUiLambdaActionBase> lambda = (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(
                            AdminShell.ModelReference.CreateNew(kl), translateAssetToAAS: true);
                    };
                    this.AddKeyListKeys(stack, "value", rfe.value.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rfe,
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else
            if (sme is AdminShell.RelationshipElement rele)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "" + sme.GetElementName(), this.levelColors.MainSection);

                // re-use lambda
                Func<AdminShell.KeyList, AnyUiLambdaActionBase> lambda = (kl) =>
                {
                    return new AnyUiLambdaActionNavigateTo(
                        AdminShell.ModelReference.CreateNew(kl), translateAssetToAAS: true);
                };

                // members
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rele.first == null || rele.first.IsEmpty; },
                            "Please choose the first element of the relationship. " +
                                "In terms of a semantic triple, it would be the subject. " +
                                "The semantics of your reference (the predicate) shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rele.first, "First relation:", "Create data element!",
                        v =>
                        {
                            rele.first = new AdminShell.ModelReference();
                            this.AddDiaryEntry(rele, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyListKeys(
                        stack, "first", rele.first.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rele);
                }

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rele.second == null || rele.second.IsEmpty; },
                            "Please choose the second element of the relationship. " +
                                "In terms of a semantic triple, it would be the object. " +
                                "The semantics of your reference (the predicate) shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rele.first, "Second relation:", "Create data element!",
                        v =>
                        {
                            rele.second = new AdminShell.ModelReference();
                            this.AddDiaryEntry(rele, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyListKeys(
                        stack, "second", rele.second.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rele);
                }

                // specifically for annotated relationship?
                if (sme is AdminShell.AnnotatedRelationshipElement /* arele */)
                {
                }
            }
            else if (sme is AdminShell.Capability)
            {
                this.AddGroup(stack, "Capability", this.levelColors.MainSection);
                this.AddKeyValue(stack, "Value", "Right now, Capability does not have further value elements.");
            }
            else if (sme is AdminShell.SubmodelElementCollection smc)
            {
                this.AddGroup(stack, "SubmodelElementCollection", this.levelColors.MainSection);
                if (smc.value != null)
                    this.AddKeyValue(stack, "# of values", "" + smc.value.Count);
                else
                    this.AddKeyValue(stack, "Values", "Please add elements via editing of sub-ordinate entities");

                this.AddCheckBox(
                    stack, "ordered:", smc.ordered, " (true e.g. for indexed array)", (b) => { smc.ordered = b; });
                this.AddCheckBox(
                    stack, "allowDuplicates:", smc.allowDuplicates,
                    " (true, if multiple elements with same semanticId)", (b) => { smc.allowDuplicates = b; });
            }
            else if (sme is AdminShell.Operation)
            {
                var p = sme as AdminShell.Operation;
                this.AddGroup(stack, "Operation", this.levelColors.MainSection);
                if (p.inputVariable != null)
                    this.AddKeyValue(stack, "# of input vars.", "" + p.inputVariable.Count);
                if (p.outputVariable != null)
                    this.AddKeyValue(stack, "# of output vars.", "" + p.outputVariable.Count);
                if (p.inoutputVariable != null)
                    this.AddKeyValue(stack, "# of in/out vars.", "" + p.inoutputVariable.Count);
            }
            else if (sme is AdminShell.Entity)
            {
                var ent = sme as AdminShell.Entity;
                this.AddGroup(stack, "Entity", this.levelColors.MainSection);

                if (ent.statements != null)
                    this.AddKeyValue(stack, "# of statements", "" + ent.statements.Count);
                else
                    this.AddKeyValue(
                        stack, "Statements", "Please add statements via editing of sub-ordinate entities");

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return ent.entityType == null ||
                                    ent.GetEntityType() == AdminShell.Entity.EntityTypeEnum.Undef;
                            },
                            "EntityType needs to be either CoManagedEntity (no assigned Asset reference) " +
                                "or SelfManagedEntity (with assigned Asset reference)",
                            severityLevel: HintCheck.Severity.High)
                    });
                this.AddKeyValueRef(
                    stack, "entityType", ent, ref ent.entityType, null, repo,
                    v =>
                    {
                        ent.entityType = v as string;
                        this.AddDiaryEntry(ent, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxItems: AdminShell.Entity.EntityTypeNames,
                    comboBoxIsEditable: true);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return ent.entityType != null &&
                                    ent.GetEntityType() == AdminShell.Entity.EntityTypeEnum.SelfManagedEntity &&
                                    (ent.assetRef == null || ent.assetRef.Count < 1);
                            },
                            "Please choose the Asset for the SelfManagedEntity.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, ent.assetRef, "Asset:", "Create data element!",
                        v =>
                        {
                            ent.assetRef = new AdminShell.ModelReference();
                            this.AddDiaryEntry(ent, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    Func<AdminShell.KeyList, AnyUiLambdaActionBase> lambda = (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(
                            AdminShell.ModelReference.CreateNew(kl), translateAssetToAAS: true);
                    };
                    this.AddKeyListKeys(
                        /* TODO (MIHO, 2021-02-16): this mechanism is ugly and only intended to be temporary!
                           It shall be replaced (after intergrating AnyUI) by a better repo handling */
                        /* Update: already better! */
                        stack, "Asset", ent.assetRef.Keys, repo, packages,
                        PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        AdminShell.Key.AllElements,
                        jumpLambda: lambda,
                        noEditJumpLambda: lambda,
                        relatedReferable: ent);
                }

            }
            else if (sme is AdminShell.BasicEvent bev)
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
                            () => { return bev.observed == null || bev.observed.IsEmpty; },
                                "Please choose the Referabe, e.g. Submodel, SubmodelElementCollection or " +
                                "DataElement which is being observed. " + Environment.NewLine +
                                "You could refer to any Referable, however it recommended restrict the scope " +
                                "to the local AAS or even within a Submodel.",
                            severityLevel: HintCheck.Severity.Notice)
                });
                if (this.SafeguardAccess(
                        stack, repo, bev.observed, "observed:", "Create data element!",
                        v =>
                        {
                            bev.observed = new AdminShell.ModelReference();
                            this.AddDiaryEntry(bev, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyListKeys(stack, "observed", bev.observed.Keys, repo,
                        packages, PackageCentral.PackageCentral.Selector.Main, AdminShell.Key.AllElements,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: (kl) =>
                        {
                            return new AnyUiLambdaActionNavigateTo(AdminShell.ModelReference.CreateNew(kl));
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
                                                caption: $"Edit raw Payload for '{"" + bev.idShort}'",
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
                            var observable = env.FindReferableByReference(bev.observed);

                            // send event
                            var ev = new AasEventMsgEnvelope(
                                DateTime.UtcNow,
                                source: bev.GetModelReference(),
                                sourceSemanticId: bev.semanticId,
                                observableReference: bev.observed,
                                observableSemanticId: (observable as AdminShell.IGetSemanticId)?.GetSemanticId());

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

    }
}
