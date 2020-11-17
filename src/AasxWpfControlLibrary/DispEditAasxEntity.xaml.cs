using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxGlobalLogging;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxWpfControlLibrary;
using AdminShellNS;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für DispEditAasEntity.xaml
    /// </summary>
    public partial class DispEditAasxEntity : UserControl
    {

        private PackageCentral packages = null;
        private VisualElementGeneric theEntity = null;

        private ModifyRepo theModifyRepo = new ModifyRepo();

        private DispEditHelperModules helper = new DispEditHelperModules();

        private DispEditHelperCopyPaste.CopyPasteBuffer theCopyPaste = new DispEditHelperCopyPaste.CopyPasteBuffer();

        static string PackageSourcePath = "";
        static string PackageTargetFn = "";
        static string PackageTargetDir = "/aasx";
        static bool PackageEmbedAsThumbnail = false;

        public class UploadAssistance
        {
            public string SourcePath = "";
            public string TargetPath = "/aasx/files";
        }
        public UploadAssistance uploadAssistance = new UploadAssistance();

        #region Public events and properties
        //
        // Public events and properties
        //

        public DispEditAasxEntity()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Timer for below
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // check for wishes from the modify repo
            if (theModifyRepo != null && theModifyRepo.WishForOutsideAction != null)
            {
                while (theModifyRepo.WishForOutsideAction.Count > 0)
                {
                    var temp = theModifyRepo.WishForOutsideAction[0];
                    theModifyRepo.WishForOutsideAction.RemoveAt(0);

                    // trivial?
                    if (temp is ModifyRepo.LambdaActionNone)
                        continue;

                    // what?
                    if (temp is ModifyRepo.LambdaActionRedrawEntity)
                    {
                        // redraw ourselves?
                        if (packages != null && theEntity != null)
                            DisplayOrEditVisualAasxElement(
                                packages, theEntity, helper.editMode, helper.hintMode,
                                flyoutProvider: helper.flyoutProvider);
                    }

                    // all other elements refer to superior functionality
                    this.WishForOutsideAction.Add(temp);
                }
            }
        }

        private void ContentUndo_Click(object sender, RoutedEventArgs e)
        {
            if (theModifyRepo != null)
                theModifyRepo.CallUndoChanges();
        }

        public List<ModifyRepo.LambdaAction> WishForOutsideAction = new List<ModifyRepo.LambdaAction>();

        public void CallUndo()
        {
            if (theModifyRepo != null)
                theModifyRepo.CallUndoChanges();
        }

        public void AddWishForOutsideAction(ModifyRepo.LambdaAction action)
        {
            if (action != null && WishForOutsideAction != null)
                WishForOutsideAction.Add(action);
        }

        #endregion


        #region Element View Drawing

        //
        //
        // --- Asset
        //
        //

        public void DisplayOrEditAasEntityAsset(
            PackageCentral packages, AdminShell.AdministrationShellEnv env, AdminShell.Asset asset,
            bool editMode, ModifyRepo repo, StackPanel stack, Brush[][] levelColors, bool embedded = false,
            bool hintMode = false)
        {
            helper.AddGroup(stack, "Asset", levelColors[0][0], levelColors[0][1]);

            // Up/ down/ del
            if (editMode && !embedded)
            {
                helper.EntityListUpDownDeleteHelper<AdminShell.Asset>(stack, repo, env.Assets, asset, env, "Asset:");
            }

            // Cut, copy, paste within list of Assets
            if (editMode && env != null)
            {
                // cut/ copy / paste
                helper.DispPlainIdentifiableCutCopyPasteHelper<AdminShell.Asset>(
                    stack, repo, this.theCopyPaste,
                    env.Assets, asset, (o) => { return new AdminShell.Asset(o); },
                    label: "Buffer:");
            }

            // print code sheet
            helper.AddAction(stack, "Actions:", new[] { "Print asset code sheet .." }, repo, (buttonNdx) =>
            {
                if (buttonNdx == 0)
                {
                    if (helper.flyoutProvider != null) helper.flyoutProvider.StartFlyover(new EmptyFlyout());

                    try
                    {
                        if (asset != null && asset.identification != null)
                        {
                            AasxPrintFunctions.PrintSingleAssetCodeSheet(asset.identification.id, asset.idShort);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "When printing, an error occurred");
                    }

                    if (helper.flyoutProvider != null) helper.flyoutProvider.CloseFlyover();
                }
                return new ModifyRepo.LambdaActionNone();
            });

            // Referable
            helper.DisplayOrEditEntityReferable(stack, asset, categoryUsual: false);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            helper.DisplayOrEditEntityHasDataSpecificationReferences(stack, asset.hasDataSpecification,
                (ds) => { asset.hasDataSpecification = ds; });

            // Identifiable
            helper.DisplayOrEditEntityIdentifiable(
                stack, asset,
                Options.Curr.TemplateIdAsset,
                new DispEditHelperModules.DispEditInjectAction(
                new[] { "Input", "Rename" },
                (i) =>
                {
                    if (i == 0)
                    {
                        var uc = new TextBoxFlyout(
                            "Asset ID:", MessageBoxImage.Question,
                            TextBoxFlyout.DialogueOptions.FilterAllControlKeys);
                        if (helper.flyoutProvider != null)
                        {
                            helper.flyoutProvider.StartFlyoverModal(uc);
                            if (uc.Result)
                            {
                                asset.identification.id = uc.Text;
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: asset);
                            }
                        }
                    }
                    if (i == 1 && env != null)
                    {
                        var uc = new TextBoxFlyout("New ID:", MessageBoxImage.Question, maxWidth: 1400);
                        uc.Text = asset.identification.id;
                        if (helper.flyoutProvider != null)
                        {
                            helper.flyoutProvider.StartFlyoverModal(uc);
                            if (uc.Result)
                            {
                                var res = false;

                                // ReSharper disable EmptyGeneralCatchClause
                                try
                                {
                                    res = env.RenameIdentifiable<AdminShell.Asset>(
                                        asset.identification,
                                        new AdminShell.Identification(
                                            asset.identification.idType, uc.Text));
                                }
                                catch { }
                                // ReSharper enable EmptyGeneralCatchClause

                                if (!res)
                                    helper.flyoutProvider.MessageBoxFlyoutShow(
                                     "The renaming of the Submodel or some referring elements " +
                                        "has not performed successfully! Please review your inputs and " +
                                        "the AAS structure for any inconsistencies.",
                                        "Warning",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                return new ModifyRepo.LambdaActionRedrawAllElements(asset);
                            }
                        }
                    }
                    return new ModifyRepo.LambdaActionNone();

                }),
                checkForIri: true);

            // Kind
            helper.DisplayOrEditEntityAssetKind(stack, asset.kind,
                (k) => { asset.kind = k; });

            // special Submode references
            helper.AddGroup(stack, "Submodel references with special meaning", levelColors[1][0], levelColors[1][1]);

            // AssetIdentificationModelRef
            helper.DisplayOrEditEntitySubmodelRef(stack, asset.assetIdentificationModelRef,
                (smr) => { asset.assetIdentificationModelRef = smr; },
                "assetIdentificationModel");

            // BillOfMaterialRef
            helper.DisplayOrEditEntitySubmodelRef(stack, asset.billOfMaterialRef,
                (smr) => { asset.billOfMaterialRef = smr; },
                "billOfMaterial");

        }

        //
        //
        // --- AAS Env
        //
        //

        public void DisplayOrEditAasEntityAasEnv(
            PackageCentral packages, AdminShell.AdministrationShellEnv env,
            VisualElementEnvironmentItem.ItemType envItemType, bool editMode, ModifyRepo repo, StackPanel stack,
            Brush[][] levelColors, bool hintMode = false)
        {
            helper.AddGroup(stack, "Environment of Asset Administration Shells", levelColors[0][0], levelColors[0][1]);

            // automatically and silently fix errors
            if (env.AdministrationShells == null)
                env.AdministrationShells = new List<AdminShell.AdministrationShell>();
            if (env.Assets == null)
                env.Assets = new List<AdminShell.Asset>();
            if (env.ConceptDescriptions == null)
                env.ConceptDescriptions = new AdminShell.ListOfConceptDescriptions();
            if (env.Submodels == null)
                env.Submodels = new List<AdminShell.Submodel>();

            if (editMode &&
                (envItemType == VisualElementEnvironmentItem.ItemType.Env ||
                    envItemType == VisualElementEnvironmentItem.ItemType.Shells ||
                    envItemType == VisualElementEnvironmentItem.ItemType.Assets ||
                    envItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions))
            {
                // some hints
                helper.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return env.Assets == null || env.Assets.Count < 1; },
                        "There are no assets in this AAS environment. You should consider adding " +
                            "an asset by clicking 'Add asset' on the edit panel below."),
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
                helper.AddAction(
                    stack, "Entities:", new[] { "Add Asset", "Add AAS", "Add ConceptDescription" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var asset = new AdminShell.Asset();
                            env.Assets.Add(asset);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: asset);
                        }

                        if (buttonNdx == 1)
                        {
                            var aas = new AdminShell.AdministrationShell();
                            env.AdministrationShells.Add(aas);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: aas);
                        }

                        if (buttonNdx == 2)
                        {
                            var cd = new AdminShell.ConceptDescription();
                            env.ConceptDescriptions.Add(cd);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: cd);
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

                // Copy AAS
                if (envItemType == VisualElementEnvironmentItem.ItemType.Shells)
                {
                    helper.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return helper.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                    helper.AddAction(
                        stack, "Copy from existing AAS:",
                        new[] { "Copy single entity ", "Copy recursively", "Copy rec. w/ suppl. files" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0 || buttonNdx == 1 || buttonNdx == 2)
                            {
                                var rve = helper.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.Selector.MainAux,
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
                                            // in any case, copy the Asset as well
                                            var sourceAsset = rve.theEnv.FindAsset(sourceAAS.assetRef);
                                            var destAsset = sourceAsset;
                                            if (copyRecursively)
                                            {
                                                destAsset = new AdminShell.Asset(sourceAsset);
                                                if (createNewIds)
                                                    destAsset.identification = new AdminShell.Identification(
                                                        AdminShell.Identification.IRI,
                                                        Options.Curr.GenerateIdAccordingTemplate(
                                                            Options.Curr.TemplateIdAsset));
                                                env.Assets.Add(destAsset);
                                            }

                                            // make a copy of the AAS itself
                                            destAAS = new AdminShell.AdministrationShell(
                                                mdo as AdminShell.AdministrationShell);
                                            destAAS.assetRef = null;
                                            if (copyRecursively)
                                                destAAS.assetRef = new AdminShell.AssetRef(destAsset.GetReference());
                                            if (createNewIds)
                                                destAAS.identification = new AdminShell.Identification(
                                                    AdminShell.Identification.IRI,
                                                    Options.Curr.GenerateIdAccordingTemplate(
                                                        Options.Curr.TemplateIdAas));
                                            env.AdministrationShells.Add(destAAS);

                                            // clear, copy Submodels?
                                            destAAS.submodelRefs = new List<AdminShellV20.SubmodelRef>();
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
                                                        dstSub.identification = new AdminShell.Identification(
                                                            AdminShell.Identification.IRI,
                                                            Options.Curr.GenerateIdAccordingTemplate(tid));

                                                        // make a new ref
                                                        var dstRef = AdminShell.SubmodelRef.CreateNew(
                                                            dstSub.GetReference());

                                                        // formally add this to active environment and AAS
                                                        env.Submodels.Add(dstSub);
                                                        destAAS.submodelRefs.Add(dstRef);
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex, $"copying AAS");
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
                                                    Log.Error(ex, $"copying supplementary file {fn}");
                                                }
                                            }
                                        }

                                        //
                                        // Done
                                        //
                                        return new ModifyRepo.LambdaActionRedrawAllElements(
                                            nextFocus: destAAS, isExpanded: true);
                                    }
                                }
                            }

                            return new ModifyRepo.LambdaActionNone();
                        });
                }

                //
                // Concept Descriptions
                //
                if (envItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions)
                {
                    // Copy
                    helper.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return helper.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                    helper.AddAction(
                        stack, "Copy from existing ConceptDescription:",
                        new[] { "Copy single entity" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var rve = helper.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.Selector.MainAux,
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
                                        env.ConceptDescriptions.Add(clone);
                                        return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: clone);
                                    }
                                }
                            }

                            return new ModifyRepo.LambdaActionNone();
                        });

                    // Sort
                    helper.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return true;  },
                            "The sort operation permanently changes the order of ConceptDescriptions in the " +
                            "environment. It cannot be reverted!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                    var g = helper.AddSubGrid(stack, "Sort entities by:", 1, 2, new[] { "#", "#" });
                    var cb = helper.AddSmallComboBoxTo(g, 0, 0,
                        margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0),
                        minWidth: 150,
                        items: new[] {
                        "idShort", "Id", "Usage in Submodels"
                    });
                    cb.SelectedIndex = 0;
                    repo.RegisterControl(
                        helper.AddSmallButtonTo(g, 0, 1, content: "Sort!",
                            margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0)),
                        (o) =>
                        {
                            if (MessageBoxResult.Yes == helper.flyoutProvider.MessageBoxFlyoutShow(
                               "Perform sort operation? This operation can not be reverted!", "ConceptDescriptions",
                               MessageBoxButton.YesNo, MessageBoxImage.Warning))
                            {
                                var success = false;
                                if (cb.SelectedIndex == 0)
                                {
                                    env.ConceptDescriptions.Sort(new AdminShell.Referable.ComparerIdShort());
                                    success = true;
                                }
                                if (cb.SelectedIndex == 1)
                                {
                                    env.ConceptDescriptions.Sort(new AdminShell.Identifiable.ComparerIdentification());
                                    success = true;
                                }
                                if (cb.SelectedIndex == 2)
                                {
                                    var cmp = env.CreateIndexedComparerCdsForSmUsage();
                                    env.ConceptDescriptions.Sort(cmp);
                                    success = true;
                                }

                                if (success)
                                    return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: null);
                            }

                            return new ModifyRepo.LambdaActionNone();
                        });
                }
            }
            else if (envItemType == VisualElementEnvironmentItem.ItemType.SupplFiles && packages.MainStorable)
            {
                // Files

                helper.AddGroup(stack, "Supplementary file to add:", levelColors[1][0], levelColors[1][1]);

                var g = helper.AddSmallGrid(5, 3, new[] { "#", "*", "#" });
                helper.AddSmallLabelTo(g, 0, 0, padding: new Thickness(2, 0, 0, 0), content: "Source path: ");
                repo.RegisterControl(
                    helper.AddSmallTextBoxTo(g, 0, 1, margin: new Thickness(2, 2, 2, 2), text: PackageSourcePath),
                    (o) =>
                    {
                        if (o is string)
                            PackageSourcePath = o as string;
                        return new ModifyRepo.LambdaActionNone();
                    });
                repo.RegisterControl(
                    helper.AddSmallButtonTo(
                        g, 0, 2, margin: new Thickness(2, 2, 2, 2), padding: new Thickness(5, 0, 5, 0),
                        content: "Select"),
                        (o) =>
                        {
                            var dlg = new Microsoft.Win32.OpenFileDialog();
                            var res = dlg.ShowDialog();
                            if (res == true)
                            {
                                PackageSourcePath = dlg.FileName;
                                PackageTargetFn = System.IO.Path.GetFileName(dlg.FileName);
                                PackageTargetFn = PackageTargetFn.Replace(" ", "_");
                            }
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                helper.AddSmallLabelTo(g, 1, 0, padding: new Thickness(2, 0, 0, 0), content: "Target filename: ");
                repo.RegisterControl(
                    helper.AddSmallTextBoxTo(g, 1, 1, margin: new Thickness(2, 2, 2, 2), text: PackageTargetFn),
                    (o) =>
                    {
                        if (o is string)
                            PackageTargetFn = o as string;
                        return new ModifyRepo.LambdaActionNone();
                    });
                helper.AddSmallLabelTo(g, 2, 0, padding: new Thickness(2, 0, 0, 0), content: "Target path: ");
                repo.RegisterControl(
                    helper.AddSmallTextBoxTo(g, 2, 1, margin: new Thickness(2, 2, 2, 2), text: PackageTargetDir),
                    (o) =>
                    {
                        if (o is string)
                            PackageTargetDir = o as string;
                        return new ModifyRepo.LambdaActionNone();
                    });
                repo.RegisterControl(
                    helper.AddSmallCheckBoxTo(g, 3, 1, margin: new Thickness(2, 2, 2, 2),
                    content: "Embed as thumbnail (only one file per package!)", isChecked: PackageEmbedAsThumbnail),
                    (o) =>
                    {
                        if (o is bool)
                            PackageEmbedAsThumbnail = (bool)o;
                        return new ModifyRepo.LambdaActionNone();
                    });
                repo.RegisterControl(
                    helper.AddSmallButtonTo(g, 4, 1, margin: new Thickness(2, 2, 2, 2),
                    padding: new Thickness(5, 0, 5, 0), content: "Add file to package"),
                    (o) =>
                    {
                        try
                        {
                            var ptd = PackageTargetDir;
                            if (PackageEmbedAsThumbnail)
                                ptd = "/";
                            packages.Main.AddSupplementaryFileToStore(
                                PackageSourcePath, ptd, PackageTargetFn, PackageEmbedAsThumbnail);
                            Log.Info(
                                "Added {0} to pending package items. A save-operation is required.",
                                PackageSourcePath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Adding file to package");
                        }
                        PackageSourcePath = "";
                        PackageTargetFn = "";
                        return new ModifyRepo.LambdaActionRedrawAllElements(
                            nextFocus: VisualElementEnvironmentItem.GiveDataObject(
                                VisualElementEnvironmentItem.ItemType.Package));
                    });
                stack.Children.Add(g);
            }
            else
            {
                // Default
                helper.AddHintBubble(
                    stack,
                    hintMode,
                    new[] {
                        new HintCheck(
                            () => { return env.AdministrationShells.Count < 1; },
                            "There are no AdministrationShell entities in the environment. " +
                                "Select the 'Administration Shells' item on the middle panel and " +
                                "select 'Add AAS' to add a new entity."),
                        new HintCheck(
                            () => { return env.Assets.Count < 1; },
                            "There are no Asset entities in the environment. " +
                                "Select the 'Assets' item on the middle panel and " +
                                "select 'Add asset' to add a new entity."),
                        new HintCheck(
                            () => { return env.ConceptDescriptions.Count < 1; },
                            "There are no embedded ConceptDescriptions in the environment. " +
                                "It is a good practive to have those. Select or add an AdministrationShell, " +
                                "Submodel and SubmodelElement and add a ConceptDescription.",
                            severityLevel: HintCheck.Severity.Notice),
                    });

                // overview information

                var g = helper.AddSmallGrid(
                    6, 1, new[] { "*" }, margin: new Thickness(5, 5, 0, 0));
                helper.AddSmallLabelTo(
                    g, 0, 0, content: "This structure hold the main entites of Administration shells.");
                helper.AddSmallLabelTo(
                    g, 1, 0, content: String.Format("#admin shells: {0}.", env.AdministrationShells.Count),
                    margin: new Thickness(0, 5, 0, 0));
                helper.AddSmallLabelTo(g, 2, 0, content: String.Format("#assets: {0}.", env.Assets.Count));
                helper.AddSmallLabelTo(g, 3, 0, content: String.Format("#submodels: {0}.", env.Submodels.Count));
                helper.AddSmallLabelTo(
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
            PackageCentral packages, AdminShellPackageSupplementaryFile psf, bool editMode, ModifyRepo repo,
            StackPanel stack, Brush[][] levelColors)
        {
            //
            // Package
            //
            helper.AddGroup(stack, "Supplementary file for package of AASX", levelColors[0][0], levelColors[0][1]);

            if (editMode && packages.MainStorable && psf != null)
            {
                helper.AddAction(stack, "Action", new[] { "Delete" }, repo, (buttonNdx) =>
               {
                   if (buttonNdx == 0)
                       if (helper.flyoutProvider != null &&
                           MessageBoxResult.Yes == helper.flyoutProvider.MessageBoxFlyoutShow(
                               "Delete selected entity? This operation can not be reverted!", "AASX",
                               MessageBoxButton.YesNo, MessageBoxImage.Warning))
                       {
                           try
                           {
                               packages.Main.DeleteSupplementaryFile(psf);
                               Log.Info(
                               "Added {0} to pending package items to be deleted. " +
                                   "A save-operation might be required.", PackageSourcePath);
                           }
                           catch (Exception ex)
                           {
                               Log.Error(ex, "Deleting file in package");
                           }
                           return new ModifyRepo.LambdaActionRedrawAllElements(
                           nextFocus: VisualElementEnvironmentItem.GiveDataObject(
                               VisualElementEnvironmentItem.ItemType.Package));
                       }

                   return new ModifyRepo.LambdaActionNone();
               });
            }
        }

        //
        //
        // --- AAS
        //
        //

        public void DisplayOrEditAasEntityAas(
            PackageCentral packages, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas,
            bool editMode, ModifyRepo repo, StackPanel stack, Brush[][] levelColors, bool hintMode = false)
        {
            helper.AddGroup(stack, "Asset Administration Shell", levelColors[0][0], levelColors[0][1]);

            // Entities
            if (editMode)
            {
                helper.AddGroup(stack, "Editing of entities", levelColors[0][0], levelColors[0][1]);

                // Up/ down/ del
                helper.EntityListUpDownDeleteHelper<AdminShell.AdministrationShell>(
                    stack, repo, env.AdministrationShells, aas, env, "AAS:");

                // Cut, copy, paste within list of AASes
                helper.DispPlainIdentifiableCutCopyPasteHelper<AdminShell.AdministrationShell>(
                    stack, repo, this.theCopyPaste,
                    env.AdministrationShells, aas, (o) => { return new AdminShell.AdministrationShell(o); },
                    label: "Buffer:");

                // Submodels
                helper.AddHintBubble(
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
                helper.AddAction(
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
                            if (helper.flyoutProvider != null &&
                                MessageBoxResult.Yes != helper.flyoutProvider.MessageBoxFlyoutShow(
                                    "This operation creates a reference to an existing Submodel. " +
                                        "By this, two AAS will share exactly the same data records. " +
                                        "Changing one will cause the other AAS's information to change as well. " +
                                        "This operation is rather special. Do you want to proceed?",
                                    "Submodel sharing",
                                    MessageBoxButton.YesNo, MessageBoxImage.Warning))
                                return new ModifyRepo.LambdaActionNone();

                            // select existing Submodel
                            var ks = helper.SmartSelectAasEntityKeys(packages, PackageCentral.Selector.Main,
                                "Submodel");
                            if (ks != null)
                            {
                                // create ref
                                var smr = new AdminShell.SubmodelRef();
                                smr.Keys.AddRange(ks);
                                aas.submodelRefs.Add(smr);

                                // redraw
                                return new ModifyRepo.LambdaActionRedrawAllElements(
                                    nextFocus: smr, isExpanded: true);
                            }
                        }

                        if (buttonNdx == 1 || buttonNdx == 2)
                        {
                            // create new submodel
                            var submodel = new AdminShell.Submodel();
                            env.Submodels.Add(submodel);

                            // directly create identification, as we need it!
                            submodel.identification.idType = AdminShell.Identification.IRI;
                            if (buttonNdx == 1)
                            {
                                submodel.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelTemplate);
                                submodel.kind = AdminShell.ModelingKind.CreateAsTemplate();
                            }
                            else
                                submodel.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelInstance);

                            // create ref
                            var smr = new AdminShell.SubmodelRef();
                            smr.Keys.Add(
                                new AdminShell.Key(
                                    "Submodel", true, submodel.identification.idType, submodel.identification.id));
                            aas.submodelRefs.Add(smr);

                            // redraw
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: smr, isExpanded: true);

                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return helper.packages.AuxAvailable;  },
                        "You have opened an auxiliary AASX package. You can copy elements from it!",
                        severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "Copy from existing Submodel:",
                    new[] { "Copy single entity ", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = helper.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.Selector.MainAux,
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
                                            return new ModifyRepo.LambdaActionNone();
                                        if (aas.submodelRefs == null)
                                            aas.submodelRefs = new List<AdminShell.SubmodelRef>();
                                        aas.submodelRefs.Add(clone);
                                        return new ModifyRepo.LambdaActionRedrawAllElements(
                                        nextFocus: clone, isExpanded: true);
                                    }
                                    else
                                    {
                                        // use case (2) copy in one AAS ENV!

                                        // need access to source submodel
                                        var srcSub = rve.theEnv.FindSubmodel(mdo as AdminShell.SubmodelRef);
                                        if (srcSub == null)
                                            return new ModifyRepo.LambdaActionNone();

                                        // means: we have to generate a new submodel ref by using template mechanism
                                        var tid = Options.Curr.TemplateIdSubmodelInstance;
                                        if (srcSub.kind != null && srcSub.kind.IsTemplate)
                                            tid = Options.Curr.TemplateIdSubmodelTemplate;

                                        // create Submodel as deep copy 
                                        // with new id from scratch
                                        var dstSub = new AdminShell.Submodel(srcSub, shallowCopy: false);
                                        dstSub.identification = new AdminShell.Identification(
                                            AdminShell.Identification.IRI,
                                            Options.Curr.GenerateIdAccordingTemplate(tid));

                                        // make a new ref
                                        var dstRef = AdminShell.SubmodelRef.CreateNew(dstSub.GetReference());

                                        // formally add this to active environment and AAS
                                        env.Submodels.Add(dstSub);
                                        if (aas.submodelRefs == null)
                                            aas.submodelRefs = new List<AdminShell.SubmodelRef>();
                                        aas.submodelRefs.Add(dstRef);
                                        return new ModifyRepo.LambdaActionRedrawAllElements(
                                            nextFocus: dstRef, isExpanded: true);
                                    }
                                }
                            }
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

                // let the user control the number of entities
                helper.AddAction(stack, "Entities:", new[] { "Add View" }, repo, (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                    {
                        var view = new AdminShell.View();
                        aas.AddView(view);
                        return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: view);
                    }

                    return new ModifyRepo.LambdaActionNone();
                });
            }

            // Referable
            helper.DisplayOrEditEntityReferable(stack, aas, categoryUsual: false);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            helper.DisplayOrEditEntityHasDataSpecificationReferences(stack, aas.hasDataSpecification,
                (ds) => { aas.hasDataSpecification = ds; });

            // Identifiable
            helper.DisplayOrEditEntityIdentifiable(
                stack, aas,
                Options.Curr.TemplateIdAas,
                null,
                checkForIri: true);

            // use some asset reference

            var asset = env.FindAsset(aas.assetRef);

            // derivedFrom

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () =>
                    {
                        return asset != null && asset.kind != null && asset.kind.IsInstance &&
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
            if (helper.SafeguardAccess(
                stack, repo, aas.derivedFrom, "derivedFrom:", "Create data element!",
                v =>
                {
                    aas.derivedFrom = new AdminShell.AssetAdministrationShellRef();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
            {
                helper.AddGroup(stack, "Derived From", levelColors[1][0], levelColors[1][1]);
                helper.AddKeyListKeys(
                    stack, "derivedFrom", aas.derivedFrom.Keys, repo,
                    packages, PackageCentral.Selector.MainAuxFileRepo, "AssetAdministrationShell");
            }

            // assetRef

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return asset == null; },
                    "No asset is associated with this Administration Shell. " +
                        "This might be, because some identification changed. " +
                        "Use 'Add existing' to assign the Administration Shell with an existing asset " +
                        "in the AAS environment or use 'Add blank' to create an arbitray reference."),
            });
            if (helper.SafeguardAccess(
                stack, repo, aas.assetRef, "assetRef:", "Create data element!",
                v =>
                {
                    aas.assetRef = new AdminShell.AssetRef();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
            {
                helper.AddGroup(stack, "Asset Reference", levelColors[1][0], levelColors[1][1]);
                helper.AddKeyListKeys(stack, "assetRef", aas.assetRef.Keys, repo,
                    packages, PackageCentral.Selector.Main, "Asset");
            }

            //
            // Asset linked with AAS
            //

            if (asset != null)
            {
                DisplayOrEditAasEntityAsset(
                    packages, env, asset, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
        }

        //
        //
        // --- Submodel Ref
        //
        //

        public void DisplayOrEditAasEntitySubmodelOrRef(
            PackageCentral packages, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas,
            AdminShell.SubmodelRef smref, AdminShell.Submodel submodel, bool editMode, ModifyRepo repo,
            StackPanel stack, Brush[][] levelColors, bool hintMode = false)
        {
            // This panel renders first the SubmodelReference and then the Submodel, below
            if (smref != null)
            {
                helper.AddGroup(stack, "SubmodelReference", levelColors[0][0], levelColors[0][1]);
                helper.AddKeyListKeys(
                    stack, "submodelRef", smref.Keys, repo,
                    packages, PackageCentral.Selector.Main, "SubmodelRef Submodel ",
                    takeOverLambdaAction: new ModifyRepo.LambdaActionRedrawAllElements(smref));
            }

            // entities when under AAS (smref)
            if (editMode && smref != null)
            {
                helper.AddGroup(stack, "Editing of entities", levelColors[0][0], levelColors[0][1]);

                helper.EntityListUpDownDeleteHelper<AdminShell.SubmodelRef>(
                    stack, repo, aas.submodelRefs, smref, aas, "SubmodelRef:");
            }

            // entities other
            if (editMode && smref == null && submodel != null)
            {
                helper.AddGroup(
                    stack, "Editing of entities (environment's Submodel collection)",
                    levelColors[0][0], levelColors[0][1]);

                helper.AddAction(stack, "Submodel:", new[] { "Delete" }, repo, (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                        if (helper.flyoutProvider != null &&
                             MessageBoxResult.Yes == helper.flyoutProvider.MessageBoxFlyoutShow(
                                 "Delete selected Submodel? This operation can not be reverted!", "AASX",
                                 MessageBoxButton.YesNo, MessageBoxImage.Warning))
                        {
                            if (env.Submodels.Contains(submodel))
                                env.Submodels.Remove(submodel);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: null, isExpanded: null);
                        }

                    return new ModifyRepo.LambdaActionNone();
                });
            }

            // Cut, copy, paste within an aas
            // Resharper disable once ConditionIsAlwaysTrueOrFalse
            if (editMode && smref != null && submodel != null && aas != null)
            {
                // cut/ copy / paste
                helper.DispSubmodelCutCopyPasteHelper<AdminShell.SubmodelRef>(stack, repo, this.theCopyPaste,
                    aas.submodelRefs, smref, (sr) => { return new AdminShell.SubmodelRef(sr); },
                    smref, submodel,
                    label: "Buffer:");
            }
            else
            // Cut, copy, paste within the Submodels
            if (editMode && smref == null && submodel != null && env != null)
            {
                // cut/ copy / paste
                helper.DispSubmodelCutCopyPasteHelper<AdminShell.Submodel>(stack, repo, this.theCopyPaste,
                    env.Submodels, submodel, (sm) => { return new AdminShell.Submodel(sm, shallowCopy: false); },
                    null, submodel,
                    label: "Buffer:");
            }

            // normal edit of the submodel
            if (editMode && submodel != null)
            {
                helper.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return submodel.submodelElements == null || submodel.submodelElements.Count < 1; },
                        "This Submodel currently has no SubmodelElements, yet. " +
                            "These are the actual carriers of information. " +
                            "You could create them by clicking the 'Add ..' buttons below. " +
                            "Subsequently, when having a SubmodelElement established, " +
                            "you could add meaning by relating it to a ConceptDefinition.",
                        severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
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
                                en = helper.SelectAdequateEnum("Select SubmodelElement to create ..");

                            // ok?
                            if (en != AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown)
                            {
                                AdminShell.SubmodelElement sme2 =
                                    AdminShell.SubmodelElementWrapper.CreateAdequateType(en);

                                // add
                                var smw = new AdminShell.SubmodelElementWrapper();
                                smw.submodelElement = sme2;
                                if (submodel.submodelElements == null)
                                    submodel.submodelElements = new AdminShellV20.SubmodelElementWrapperCollection();
                                submodel.submodelElements.Add(smw);

                                // redraw
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: sme2, isExpanded: true);
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return helper.packages.AuxAvailable;  },
                        "You have opened an auxiliary AASX package. You can copy elements from it!",
                        severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "Copy from existing SubmodelElement:",
                    new[] { "Copy single entity", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = helper.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.Selector.MainAux,
                                "SubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null && env != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is AdminShell.SubmodelElement)
                                {
                                    var clone = env.CopySubmodelElementAndCD(
                                        rve.theEnv, mdo as AdminShell.SubmodelElement,
                                        copyCD: true, shallowCopy: buttonNdx == 0);

                                    if (submodel.submodelElements == null)
                                        submodel.submodelElements =
                                            new AdminShellV20.SubmodelElementWrapperCollection();

                                    // ReSharper disable once PossibleNullReferenceException -- ignore a false positive
                                    submodel.submodelElements.Add(clone);
                                    return new ModifyRepo.LambdaActionRedrawAllElements(
                                        submodel, isExpanded: true);
                                }
                            }
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

                // create ConceptDescriptions for eCl@ss
                var targets = new List<AdminShell.SubmodelElement>();
                helper.IdentifyTargetsForEclassImportOfCDs(
                    env, AdminShell.SubmodelElementWrapper.ListOfWrappersToListOfElems(submodel.submodelElements),
                    ref targets);
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return submodel.submodelElements != null && submodel.submodelElements.Count > 0  &&
                                    targets.Count > 0;
                            },
                            "Consider importing ConceptDescriptions from eCl@ss for existing SubmodelElements.",
                            severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "ConceptDescriptions from eCl@ss:",
                    new[] { "Import missing" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // ReSharper disable RedundantCast
                            helper.ImportEclassCDsForTargets(
                                env, (smref != null) ? (object)smref : (object)submodel, targets);
                            // ReSharper enable RedundantCast
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddAction(
                    stack, "Submodel & -elements:",
                    new[] { "Turn to kind Template", "Turn to kind Instance" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (helper.flyoutProvider != null &&
                            MessageBoxResult.Yes != helper.flyoutProvider.MessageBoxFlyoutShow(
                                "This operation will affect all Kind attributes of " +
                                    "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                "Setting Kind",
                                MessageBoxButton.YesNo, MessageBoxImage.Warning))
                            return new ModifyRepo.LambdaActionNone();

                        submodel.kind = (buttonNdx == 0)
                            ? AdminShell.ModelingKind.CreateAsTemplate()
                            : AdminShell.ModelingKind.CreateAsInstance();

                        submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                        {
                            sme.kind = (buttonNdx == 0)
                                ? AdminShell.ModelingKind.CreateAsTemplate()
                                : AdminShell.ModelingKind.CreateAsInstance();
                        });

                        return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                    });

            }

            if (submodel != null)
            {

                // Submodel
                helper.AddGroup(stack, "Submodel", levelColors[0][0], levelColors[0][1]);

                // Referable
                helper.DisplayOrEditEntityReferable(stack, submodel, categoryUsual: false);

                // Identifiable
                helper.DisplayOrEditEntityIdentifiable(
                    stack, submodel,
                    (submodel.kind.kind.Trim().ToLower() == "template")
                        ? Options.Curr.TemplateIdSubmodelTemplate
                        : Options.Curr.TemplateIdSubmodelInstance,
                    new DispEditHelperModules.DispEditInjectAction(
                        new[] { "Rename" },
                        (i) =>
                        {
                            if (i == 0 && env != null)
                            {
                                var uc = new TextBoxFlyout("New ID:", MessageBoxImage.Question, maxWidth: 1400);
                                uc.Text = submodel.identification.id;
                                if (helper.flyoutProvider != null)
                                {
                                    helper.flyoutProvider.StartFlyoverModal(uc);
                                    if (uc.Result)
                                    {
                                        var res = false;

                                        // ReSharper disable EmptyGeneralCatchClause
                                        try
                                        {
                                            res = env.RenameIdentifiable<AdminShell.Submodel>(
                                                submodel.identification,
                                                new AdminShell.Identification(
                                                    submodel.identification.idType, uc.Text));
                                        }
                                        catch { }
                                        // ReSharper enable EmptyGeneralCatchClause

                                        if (!res)
                                            helper.flyoutProvider.MessageBoxFlyoutShow(
                                             "The renaming of the Submodel or some referring elements " +
                                                "has not performed successfully! Please review your inputs and " +
                                                "the AAS structure for any inconsistencies.",
                                                "Warning",
                                                MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return new ModifyRepo.LambdaActionRedrawAllElements(smref);
                                    }
                                }
                            }
                            return new ModifyRepo.LambdaActionNone();
                        }),
                    checkForIri: submodel.kind != null && submodel.kind.IsInstance);

                // HasKind
                helper.DisplayOrEditEntityModelingKind(
                    stack, submodel.kind,
                    (k) => { submodel.kind = k; },
                    instanceExceptionStatement:
                        "Exception: if you want to declare a Submodel, which is been standardised " +
                        "by you or a standardisation body.");

                // HasSemanticId
                helper.DisplayOrEditEntitySemanticId(stack, submodel.semanticId,
                    (o) => { submodel.semanticId = o; },
                    "The semanticId may be either a reference to a submodel " +
                    "with kind=Type (within the same or another Administration Shell) or " +
                    "it can be an external reference to an external standard " +
                    "defining the semantics of the submodel (for example an PDF if a standard).",
                    addExistingEntities: AdminShell.Key.SubmodelRef + " " + AdminShell.Key.Submodel + " " +
                        AdminShell.Key.ConceptDescription);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                helper.DisplayOrEditEntityQualifierCollection(
                    stack, submodel.qualifiers,
                    (q) => { submodel.qualifiers = q; });

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                helper.DisplayOrEditEntityHasDataSpecificationReferences(stack, submodel.hasDataSpecification,
                (ds) => { submodel.hasDataSpecification = ds; });

            }
        }

        //
        //
        // --- Concept Description
        //
        //

        public void DisplayOrEditAasEntityConceptDescription(
            PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.ConceptDescription cd, bool editMode,
            ModifyRepo repo, StackPanel stack, Brush[][] levelColors, bool embedded = false, bool hintMode = false)
        {
            helper.AddGroup(stack, "ConceptDescription", levelColors[0][0], levelColors[0][1]);

            // Up/ down/ del
            if (editMode && !embedded)
            {
                helper.EntityListUpDownDeleteHelper<AdminShell.ConceptDescription>(
                    stack, repo, env.ConceptDescriptions, cd, env, "CD:");
            }

            // Cut, copy, paste within list of CDs
            if (editMode && env != null)
            {
                // cut/ copy / paste
                helper.DispPlainIdentifiableCutCopyPasteHelper<AdminShell.ConceptDescription>(
                    stack, repo, this.theCopyPaste,
                    env.ConceptDescriptions, cd, (o) => { return new AdminShell.ConceptDescription(o); },
                    label: "Buffer:");
            }

            // Referable
            helper.DisplayOrEditEntityReferable(
                stack, cd,
                new DispEditHelperModules.DispEditInjectAction(
                    new[] { "Sync" },
                    new[] { "Copy (if target is empty) idShort to shortName and SubmodelElement idShort." },
                    (v) =>
                    {
                        ModifyRepo.LambdaAction la = new ModifyRepo.LambdaActionNone();
                        if ((int)v != 0)
                            return la;

                        var ds = cd.GetIEC61360();
                        if (ds != null && (ds.shortName == null || ds.shortName.Count < 1))
                        {
                            ds.shortName = new AdminShellV20.LangStringSetIEC61360("EN?", cd.idShort);
                            la = new ModifyRepo.LambdaActionRedrawEntity();
                        }

                        if (parentContainer != null & parentContainer is AdminShell.SubmodelElement)
                        {
                            var sme = parentContainer as AdminShell.SubmodelElement;
                            if (sme.idShort == null || sme.idShort.Trim() == "")
                            {
                                sme.idShort = cd.idShort;
                                la = new ModifyRepo.LambdaActionRedrawEntity();
                            }
                        }
                        return la;
                    }),
                categoryUsual: false);

            // Identifiable

            helper.DisplayOrEditEntityIdentifiable(
                stack, cd,
                Options.Curr.TemplateIdConceptDescription,
                new DispEditHelperModules.DispEditInjectAction(
                new[] { "Rename" },
                (i) =>
                {
                    if (i == 0 && env != null)
                    {
                        var uc = new TextBoxFlyout("New ID:", MessageBoxImage.Question, maxWidth: 1400);
                        uc.Text = cd.identification.id;
                        if (helper.flyoutProvider != null)
                        {
                            helper.flyoutProvider.StartFlyoverModal(uc);
                            if (uc.Result)
                            {
                                var res = false;

                                // ReSharper disable EmptyGeneralCatchClause
                                try
                                {
                                    res = env.RenameIdentifiable<AdminShell.ConceptDescription>(
                                        cd.identification,
                                        new AdminShell.Identification(cd.identification.idType, uc.Text));
                                }
                                catch { }
                                // ReSharper enable EmptyGeneralCatchClause

                                if (!res)
                                    helper.flyoutProvider.MessageBoxFlyoutShow(
                                     "The renaming of the ConceptDescription or some referring elements has not " +
                                         "performed successfully! Please review your inputs and the AAS " +
                                         "structure for any inconsistencies.",
                                         "Warning",
                                         MessageBoxButton.OK, MessageBoxImage.Warning);
                                return new ModifyRepo.LambdaActionRedrawAllElements(cd);
                            }
                        }
                    }
                    return new ModifyRepo.LambdaActionNone();
                }),
                checkForIri: false);

            // isCaseOf are MULTIPLE references. That is: multiple x multiple keys!
            helper.DisplayOrEditEntityListOfReferences(stack, cd.IsCaseOf,
                (ico) => { cd.IsCaseOf = ico; },
                "isCaseOf");

            // joint header for data spec ref and content
            helper.AddGroup(stack, "HasDataSpecification:", levelColors[1][0], levelColors[1][1]);

            // check, if there is a IEC61360 content amd, subsequently, also a according data specification
            var esc = cd.IEC61360DataSpec;
            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return esc != null && (esc.dataSpecification == null
                            || !esc.dataSpecification.MatchesExactlyOneKey(
                                AdminShell.DataSpecificationIEC61360.GetKey())); },
                        "IEC61360 content present, but data specification missing. Please add according reference.",
                        breakIfTrue: true),
                });

            // use the normal module to edit ALL data specifications
            helper.DisplayOrEditEntityHasDataSpecificationReferences(stack, cd.embeddedDataSpecification,
                (ds) => { cd.embeddedDataSpecification = ds; },
                addPresetNames: new[] { "IEC61360" },
                addPresetKeyLists: new[] {
                    AdminShell.KeyList.CreateNew( AdminShell.DataSpecificationIEC61360.GetKey() )},
                dataSpecRefsAreUsual: true);

            // the IEC61360 Content

            // TODO (MIHO, 2020-09-01): extend the lines below to cover also data spec. for units

            helper.AddHintBubble(
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
            if (helper.SafeguardAccess(
                    stack, repo, cd.IEC61360Content, "embeddedDataSpecification:",
                    "Create IEC61360 data specification content",
                    v =>
                    {
                        cd.IEC61360Content = new AdminShell.DataSpecificationIEC61360();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.DisplayOrEditEntityDataSpecificationIEC61360(stack, cd.IEC61360Content);
            }
        }

        //
        //
        // --- Operation Variable
        //
        //

        public void DisplayOrEditAasEntityOperationVariable(
            PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.OperationVariable ov, bool editMode,
            ModifyRepo repo, StackPanel stack, Brush[][] levelColors, bool hintMode = false)
        {
            //
            // Submodel Element GENERAL
            //

            // OperationVariable is a must!
            if (ov == null)
                return;

            if (editMode)
            {
                helper.AddGroup(stack, "Editing of entities", levelColors[0][0], levelColors[0][1]);

                // entities
                if (parentContainer != null && parentContainer is AdminShell.Operation)
                    // hope is OK to refer to two lists!
                    for (int i = 0; i < 3; i++)
                        if ((parentContainer as AdminShell.Operation)[i].Contains(ov))
                            helper.EntityListUpDownDeleteHelper<AdminShell.OperationVariable>(
                                stack, repo,
                                (parentContainer as AdminShell.Operation)[i],
                                ov, env, "OperationVariable:");

            }

            // always an OperationVariable
            if (true)
            {
                helper.AddGroup(stack, "OperationVariable", levelColors[0][0], levelColors[0][1]);

                if (ov.value == null)
                {
                    helper.AddGroup(
                        stack, "OperationVariable value is not set!", levelColors[1][0], levelColors[1][1]);

                    if (editMode)
                    {
                        helper.AddAction(
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
                                        en = helper.SelectAdequateEnum(
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

                                        // redraw
                                        return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: ov);
                                    }
                                }
                                return new ModifyRepo.LambdaActionNone();
                            });

                    }
                }
                else
                {
                    // value is already set
                    // operations on it

                    if (editMode)
                    {
                        helper.AddAction(stack, "value:", new[] { "Remove existing" }, repo, (buttonNdx) =>
                       {
                           if (buttonNdx == 0)
                               if (helper.flyoutProvider != null &&
                                    MessageBoxResult.Yes == helper.flyoutProvider.MessageBoxFlyoutShow(
                                        "Delete value, which is the dataset of a SubmodelElement? " +
                                            "This cannot be reverted!",
                                        "AASX", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                               {
                                   ov.value = null;
                                   return new ModifyRepo.LambdaActionRedrawEntity();
                               }
                           return new ModifyRepo.LambdaActionNone();
                       });

                        helper.AddHintBubble(stack, hintMode, new[] {
                            new HintCheck(
                                () => { return helper.packages.AuxAvailable;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                        helper.AddAction(
                            stack, "Copy from existing SubmodelElement:",
                            new[] { "Copy single", "Copy recursively" }, repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx == 0 || buttonNdx == 1)
                                {
                                    var rve = helper.SmartSelectAasEntityVisualElement(
                                        packages, PackageCentral.Selector.MainAux,
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

                                            ov.value = clone;
                                            return new ModifyRepo.LambdaActionRedrawEntity();
                                        }
                                    }
                                }

                                return new ModifyRepo.LambdaActionNone();
                            });
                    }

                    // value == SubmodelElement is displayed
                    helper.AddGroup(
                        stack, "OperationVariable value (is a SubmodelElement)", levelColors[1][0], levelColors[1][1]);
                    var substack = helper.AddSubStackPanel(stack, "  "); // just a bit spacing to the left
                    // huh, recursion in a lambda based GUI feedback function??!!
                    if (ov.value != null && ov.value.submodelElement != null) // avoid at least direct recursions!
                        DisplayOrEditAasEntitySubmodelElement(
                            packages, env, parentContainer, ov.value, null, editMode, repo,
                            substack, levelColors, hintMode);
                }
            }
        }


        //
        //
        // --- Submodel Element
        //
        //

        public void DisplayOrEditAasEntitySubmodelElement(
            PackageCentral packages, AdminShell.AdministrationShellEnv env,
            AdminShell.Referable parentContainer, AdminShell.SubmodelElementWrapper wrapper,
            AdminShell.SubmodelElement sme, bool editMode, ModifyRepo repo, StackPanel stack,
            Brush[][] levelColors, bool hintMode = false)
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
            var editSmeAttr = true;

            if (editMode)
            {
                helper.AddGroup(stack, "Editing of entities", levelColors[0][0], levelColors[0][1]);

                // for sake of space efficiency, smuggle "Refactor" into this
                var horizStack = new WrapPanel();
                horizStack.Orientation = Orientation.Horizontal;
                stack.Children.Add(horizStack);

                // entities helper
                if (parentContainer != null && parentContainer is AdminShell.Submodel && wrapper != null)
                    helper.EntityListUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(
                        horizStack, repo, (parentContainer as AdminShell.Submodel).submodelElements, wrapper, env,
                        "SubmodelElement:", nextFocus: wrapper.submodelElement);

                if (parentContainer != null && parentContainer is AdminShell.SubmodelElementCollection &&
                        wrapper != null)
                    helper.EntityListUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(
                        horizStack, repo, (parentContainer as AdminShell.SubmodelElementCollection).value,
                        wrapper, env, "SubmodelElement:",
                        nextFocus: wrapper.submodelElement);

                if (parentContainer != null && parentContainer is AdminShell.Entity && wrapper != null)
                    helper.EntityListUpDownDeleteHelper<AdminShell.SubmodelElementWrapper>(
                        horizStack, repo, (parentContainer as AdminShell.Entity).statements,
                        wrapper, env, "SubmodelElement:",
                        nextFocus: wrapper.submodelElement);

                // refactor?
                if (parentContainer != null && parentContainer is AdminShell.IManageSubmodelElements)
                    helper.AddAction(
                        horizStack, "Refactoring:",
                        new[] { "Refactor" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                // which?
                                var refactorSme = helper.SmartRefactorSme(sme);
                                var parMgr = (parentContainer as AdminShell.IManageSubmodelElements);

                                // ok?
                                if (refactorSme != null && parMgr != null)
                                {
                                    // open heart surgery: change in parent container
                                    parMgr.Remove(sme);
                                    parMgr.Add(refactorSme);

                                    // redraw
                                    return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: refactorSme);
                                }
                            }
                            return new ModifyRepo.LambdaActionNone();
                        });
            }

            // cut/ copy / paste
            if (parentContainer != null)
            {
                helper.DispSmeCutCopyPasteHelper(stack, repo, env, parentContainer, this.theCopyPaste, wrapper, sme,
                    label: "Buffer:");
#if _in_refactoring
                helper.AddAction(
                    stack, "Buffer:",
                    new[] { "Cut", "Copy", "Paste above", "Paste below", "Paste into" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            // store info
                            var cpb = new CopyPasteBuffer();
                            cpb.duplicate = buttonNdx == 1;
                            cpb.parentContainer = parentContainer;
                            cpb.wrapper = wrapper;
                            cpb.sme = sme;
                            this.theCopyPaste = cpb;

                            // user feedback
                            Log.Info(
                                0, StoredPrint.ColorBlue,
                                "Stored SubmodelElement '{0}'({1}) to internal buffer.{2}", "" + sme?.idShort,
                                "" + sme?.GetElementName(),
                                cpb.duplicate
                                    ? " Paste will duplicate."
                                    : " Paste will cut at original position.");
                        }

                        if (buttonNdx == 2 || buttonNdx == 3 || buttonNdx == 4)
                        {
                            // access copy/paste
                            var cpb = this.theCopyPaste;

                            // present
                            if (cpb == null || cpb.sme == null || cpb.wrapper == null ||
                                cpb.parentContainer == null)
                            {
                                if (helper.flyoutProvider != null)
                                    helper.flyoutProvider.MessageBoxFlyoutShow(
                                        "No (valid) information in copy/paste buffer.", "Copy & Paste",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                return new ModifyRepo.LambdaActionNone();
                            }

                            // user feedback
                            Log.Info(
                                "Pasting buffer with SubmodelElement '{0}'({1}) to internal buffer.",
                                "" + cpb.sme?.idShort, "" + cpb.sme?.GetElementName());

                            // apply info
                            var smw2 = new AdminShell.SubmodelElementWrapper(cpb.sme, shallowCopy: false);

                            // insertation depends on parent container
                            if (buttonNdx == 2)
                            {
                                if (parentContainer is AdminShell.Submodel pcsm && wrapper != null)
                                    helper.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                        pcsm.submodelElements, smw2, wrapper);

                                if (parentContainer is AdminShell.SubmodelElementCollection pcsmc &&
                                        wrapper != null)
                                    helper.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                        pcsmc.value, smw2, wrapper);

                                if (parentContainer is AdminShell.Entity pcent &&
                                        wrapper != null)
                                    helper.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                        pcent.statements, smw2, wrapper);

                                if (parentContainer is AdminShell.AnnotatedRelationshipElement pcarel &&
                                        wrapper != null)
                                    helper.AddElementInListBefore<AdminShell.SubmodelElementWrapper>(
                                        pcarel.annotations, smw2, wrapper);

                                // TODO (Michael Hoffmeister, 2020-08-01): Operation mssing here?
                            }
                            if (buttonNdx == 3)
                            {
                                if (parentContainer is AdminShell.Submodel pcsm && wrapper != null)
                                    helper.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                        pcsm.submodelElements, smw2, wrapper);

                                if (parentContainer is AdminShell.SubmodelElementCollection pcsmc &&
                                        wrapper != null)
                                    helper.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                        pcsmc.value, smw2, wrapper);

                                if (parentContainer is AdminShell.Entity pcent && wrapper != null)
                                    helper.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                        pcent.statements, smw2, wrapper);

                                if (parentContainer is AdminShell.AnnotatedRelationshipElement pcarel &&
                                        wrapper != null)
                                    helper.AddElementInListAfter<AdminShell.SubmodelElementWrapper>(
                                        pcarel.annotations, smw2, wrapper);

                                // TODO (Michael Hoffmeister, 2020-08-01): Operation mssing here?
                            }
                            if (buttonNdx == 4)
                            {
                                if (sme is AdminShell.IEnumerateChildren smeec)
                                    smeec.AddChild(smw2);
                            }

                            // may delete original
                            if (!cpb.duplicate)
                            {
                                if (cpb.parentContainer is AdminShell.Submodel pcsm && wrapper != null)
                                    helper.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                                        pcsm.submodelElements, cpb.wrapper, null);

                                if (cpb.parentContainer is AdminShell.SubmodelElementCollection pcsmc &&
                                        wrapper != null)
                                    helper.DeleteElementInList<AdminShell.SubmodelElementWrapper>(
                                        pcsmc.value, cpb.wrapper, null);

                                // the buffer is tainted
                                this.theCopyPaste = null;
                            }

                            // try to focus
                            return new ModifyRepo.LambdaActionRedrawAllElements(
                                nextFocus: smw2.submodelElement, isExpanded: true);
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });
#endif
            }

            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (editMode && editSmeAttr)
            // ReSharper enable ConditionIsAlwaysTrueOrFalse
            {
                // guess kind or instances
                AdminShell.ModelingKind parentKind = null;
                if (parentContainer != null && parentContainer is AdminShell.Submodel)
                    parentKind = (parentContainer as AdminShell.Submodel).kind;
                if (parentContainer != null && parentContainer is AdminShell.SubmodelElementCollection)
                    parentKind = (parentContainer as AdminShell.SubmodelElementCollection).kind;

                // relating to CDs
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return sme.semanticId == null || sme.semanticId.IsEmpty; },
                            "The semanticId (see below) is empty. " +
                                "This SubmodelElement ist currently not assigned to any ConceptDescription. " +
                                "However, it is recommended to do such assignemt. " +
                                "With the 'Assign ..' buttons below you might create and/or assign " +
                                "the SubmodelElement to an ConceptDescription.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddAction(
                    stack, "Concept Description:",
                    new[] { "Assign to existing CD", "Create empty and assign", "Create and assign from eCl@ss" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // select existing CD
                            var ks = helper.SmartSelectAasEntityKeys(
                                        packages, PackageCentral.Selector.MainAuxFileRepo);
                            if (ks != null)
                            {
                                // set the semantic id
                                sme.semanticId = AdminShell.SemanticId.CreateFromKeys(ks);

                                // if empty take over shortName
                                var cd = env.FindConceptDescription(sme.semanticId.Keys);
                                if ((sme.idShort == null || sme.idShort.Trim() == "") && cd != null)
                                {
                                    sme.idShort = "" + cd.idShort;
                                    if (sme.idShort == "")
                                        sme.idShort = cd.GetDefaultShortName();
                                }

                                // can set kind?
                                if (parentKind != null && sme.kind == null)
                                    sme.kind = new AdminShell.ModelingKind(parentKind);
                            }
                            // redraw
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 1)
                        {
                            // create empty CD
                            var cd = new AdminShell.ConceptDescription();

                            // make an ID, automatically
                            cd.identification.idType = AdminShell.Identification.IRI;
                            cd.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdConceptDescription);

                            // store in AAS enviroment
                            env.ConceptDescriptions.Add(cd);

                            // go over to SubmodelElement
                            // set the semantic id
                            sme.semanticId = AdminShell.SemanticId.CreateFromKey(
                                new AdminShell.Key(
                                    "ConceptDescription", true, cd.identification.idType, cd.identification.id));

                            // can set kind?
                            if (parentKind != null && sme.kind == null)
                                sme.kind = new AdminShell.ModelingKind(parentKind);

                            // redraw
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 2)
                        {
                            // feature available
                            if (Options.Curr.EclassDir == null)
                            {
                                // eclass dir?
                                if (helper.flyoutProvider != null)
                                    helper.flyoutProvider.MessageBoxFlyoutShow(
                                        "The AASX Package Explore can take over eCl@ss definition. " +
                                        "In order to do so, the commandine parameter -eclass has" +
                                        "to refer to a folder withe eCl@ss XML files.", "Information",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                return new ModifyRepo.LambdaActionNone();
                            }

                            // select
                            string resIRDI = null;
                            AdminShell.ConceptDescription resCD = null;
                            if (helper.SmartSelectEclassEntity(
                                SelectEclassEntityFlyout.SelectMode.ConceptDescription, ref resIRDI, ref resCD))
                            {
                                // create the concept description itself, if available,
                                // if not exactly the same is present
                                if (resCD != null)
                                {
                                    var newcd = resCD;
                                    if (null == env.FindConceptDescription(
                                            AdminShell.Key.CreateNew(
                                                AdminShell.Key.ConceptDescription, true,
                                                newcd.identification.idType, newcd.identification.id)))
                                        env.ConceptDescriptions.Add(newcd);
                                }

                                // set the semantic key
                                sme.semanticId = AdminShell.SemanticId.CreateFromKey(
                                    new AdminShell.Key(
                                        AdminShell.Key.ConceptDescription, true,
                                        AdminShell.Identification.IRDI, resIRDI));

                                // if empty take over shortName
                                var cd = env.FindConceptDescription(sme.semanticId.Keys);
                                if ((sme.idShort == null || sme.idShort.Trim() == "") && cd != null)
                                    sme.idShort = cd.GetDefaultShortName();

                                // can set kind?
                                if (parentKind != null && sme.kind == null)
                                    sme.kind = new AdminShell.ModelingKind(parentKind);
                            }

                            // redraw
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

                // create ConceptDescriptions for eCl@ss
                var targets = new List<AdminShell.SubmodelElement>();
                helper.IdentifyTargetsForEclassImportOfCDs(
                    env, new List<AdminShell.SubmodelElement>(new[] { sme }), ref targets);
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck( () => { return targets.Count > 0;  },
                        "Consider importing a ConceptDescription from eCl@ss for the existing SubmodelElement.",
                        severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddAction(
                    stack, "ConceptDescriptions from eCl@ss:", new[] { "Import missing" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            helper.ImportEclassCDsForTargets(env, sme, targets);
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

            }

            if (editMode && (sme is AdminShell.SubmodelElementCollection || sme is AdminShell.Entity))
            {
                helper.AddGroup(stack, "Editing of sub-ordinate entities", levelColors[0][0], levelColors[0][1]);

                List<AdminShell.SubmodelElementWrapper> listOfSMEW = null;
                if (sme is AdminShell.SubmodelElementCollection)
                    listOfSMEW = (sme as AdminShell.SubmodelElementCollection).value;
                if (sme is AdminShell.Entity)
                    listOfSMEW = (sme as AdminShell.Entity).statements;

                helper.AddHintBubble(
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
                helper.AddAction(
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
                                en = helper.SelectAdequateEnum("Select SubmodelElement to create ..");

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

                                // redraw
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: sme2);
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return helper.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "Copy from existing SubmodelElement:", new[] { "Copy single", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = helper.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.Selector.MainAux,
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
                                    return new ModifyRepo.LambdaActionRedrawAllElements(
                                        nextFocus: sme, isExpanded: true);
                                }
                            }
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });
            }

            AdminShell.ConceptDescription jumpToCD = null;
            if (sme.semanticId != null && sme.semanticId.Count > 0)
                jumpToCD = env.FindConceptDescription(sme.semanticId.Keys);

            if (jumpToCD != null && editMode)
            {
                helper.AddGroup(stack, "Navigation of entities", levelColors[0][0], levelColors[0][1]);

                helper.AddAction(stack, "Navigate to:", new[] { "Concept Description" }, repo, (buttonNdx) =>
               {
                   if (buttonNdx == 0)
                   {
                       return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: jumpToCD, isExpanded: true);
                   }
                   return new ModifyRepo.LambdaActionNone();
               });
            }

            if (editMode && sme is AdminShell.Operation smo)
            {
                helper.AddGroup(stack, "Editing of sub-ordinate entities", levelColors[0][0], levelColors[0][1]);

                var substack = helper.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

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

                    helper.AddGroup(substack, "OperationVariables " + names, levelColors[1][0], levelColors[1][1]);

                    helper.AddHintBubble(
                        substack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return smo[dir] == null || smo[dir].Count < 1; },
                                "This collection of OperationVariables currently has no elements, yet. " +
                                    "Please check, which in- and out-variables are required.",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                    helper.AddAction(
                        substack, "OperationVariable:", new[] { "Add" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var ov = new AdminShell.OperationVariable();
                                if (smo[dir] == null)
                                    smo[dir] = new List<AdminShell.OperationVariable>();
                                smo[dir].Add(ov);

                                // redraw
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: ov);
                            }
                            return new ModifyRepo.LambdaActionNone();
                        });

                    helper.AddHintBubble(
                        substack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return helper.packages.AuxAvailable;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                    helper.AddAction(
                        substack, "Copy from existing OperationVariable:", new[] { "Copy single", "Copy recursively" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0 || buttonNdx == 1)
                            {
                                var rve = helper.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.Selector.MainAux,
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
                                        return new ModifyRepo.LambdaActionRedrawAllElements(
                                            nextFocus: smo, isExpanded: true);
                                    }
                                }
                            }

                            return new ModifyRepo.LambdaActionNone();
                        });

                }

            }

            if (editMode && sme is AdminShell.AnnotatedRelationshipElement are)
            {
                helper.AddGroup(stack, "Editing of sub-ordinate entities", levelColors[0][0], levelColors[0][1]);

                var substack = helper.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                helper.AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return are.annotations == null || are.annotations.Count < 1; },
                            "The annotations collection currently has no elements, yet. " +
                                "Consider add DataElements or refactor to ordinary RelationshipElement.",
                            severityLevel: HintCheck.Severity.Notice)
                        });
                helper.AddAction(
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
                                en = helper.SelectAdequateEnum(
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

                                // redraw
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: sme2);
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return helper.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddAction(
                    substack, "Copy from existing DataElement:", new[] { "Copy single" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var rve = helper.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.Selector.MainAux,
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

                                    return new ModifyRepo.LambdaActionRedrawAllElements(
                                        nextFocus: clonesmw.submodelElement, isExpanded: true);
                                }
                            }
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

            }

            if (editSmeAttr)
            {

                helper.AddGroup(
                    stack,
                    $"Submodel Element ({"" + sme?.GetElementName()})",
                    levelColors[0][0], levelColors[0][1]);

                // Referable
                helper.DisplayOrEditEntityReferable(stack, sme, categoryUsual: true,
                    injectToIdShort: new DispEditHelperModules.DispEditInjectAction(
                        auxTitles: new[] { "Sync" },
                        auxToolTips: new[] { "Copy (if target is empty) idShort " +
                        "to concept desctiption idShort and shortName." },
                        auxActions: (buttonNdx) =>
                        {
                            if (sme.semanticId != null && sme.semanticId.Count > 0)
                            {
                                var cd = env.FindConceptDescription(sme.semanticId.Keys);
                                if (cd != null)
                                {
                                    if (cd.idShort == null || cd.idShort.Trim() == "")
                                        cd.idShort = sme.idShort;

                                    var ds = cd.IEC61360Content;
                                    if (ds != null && (ds.shortName == null || ds.shortName.Count < 1))
                                        ds.shortName = new AdminShellV20.LangStringSetIEC61360("EN?", sme.idShort);

                                    return new ModifyRepo.LambdaActionRedrawEntity();
                                }
                            }
                            return new ModifyRepo.LambdaActionNone();
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
                                "In eCl@ss this kind of category has the category 'Coded Value'. \r\n" +
                                "PARAMETER => A parameter property is a property that is once set and " +
                                "then typically does not change over time. " +
                                "This is for example the case for configuration parameters. \r\n" +
                                "VARIABLE => A variable property is a property that is calculated during runtime, " +
                                "i.e. its value is a runtime value. ",
                           severityLevel: HintCheck.Severity.Notice)
                    });

                // Kind
                helper.DisplayOrEditEntityModelingKind(stack, sme.kind,
                    (k) => { sme.kind = k; });

                // HasSemanticId
                helper.DisplayOrEditEntitySemanticId(stack, sme.semanticId,
                    (sid) => { sme.semanticId = sid; },
                    "The use of semanticId for SubmodelElements is mandatory! " +
                    "Only by this means, an automatic system can identify and " +
                    "understand the meaning of the SubmodelElements and, for example, " +
                    "its unit or logical datatype. " +
                    "The semanticId shall reference to a ConceptDescription within the AAS environment " +
                    "or an external repository, such as IEC CDD or eCl@ss or " +
                    "a company / consortia repository.",
                    checkForCD: true,
                    addExistingEntities: AdminShell.Key.ConceptDescription,
                    cpb: theCopyPaste);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                helper.DisplayOrEditEntityQualifierCollection(
                    stack, sme.qualifiers,
                    (q) => { sme.qualifiers = q; });

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                helper.DisplayOrEditEntityHasDataSpecificationReferences(stack, sme.hasDataSpecification,
                (ds) => { sme.hasDataSpecification = ds; });

                //
                // ConceptDescription <- via semantic ID ?!
                //

                if (sme.semanticId != null && sme.semanticId.Count > 0)
                {
                    var cd = env.FindConceptDescription(sme.semanticId.Keys);
                    if (cd == null)
                    {
                        helper.AddGroup(
                            stack, "ConceptDescription cannot be looked up within the AAS environment!",
                            levelColors[0][0], levelColors[0][1]);
                    }
                    else
                    {
                        DisplayOrEditAasEntityConceptDescription(
                            packages, env, sme, cd, editMode, repo, stack, levelColors,
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
                helper.AddGroup(stack, "Property", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p.valueType == null || p.valueType.Trim().Length < 1; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddKeyValueRef(
                    stack, "valueType", p, ref p.valueType, null, repo,
                    v => { p.valueType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.DataElement.ValueTypeItems);

                helper.AddHintBubble(
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
                helper.AddKeyValueRef(
                    stack, "value", p, ref p.value, null, repo,
                    v => { p.value = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitles: new[] { "\u2261" },
                    auxButtonToolTips: new[] { "Edit in multiline editor" },
                    auxButtonLambda: (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var uc = new TextEditorFlyout($"Edit Property '{"" + p.idShort}'");
                            uc.SetMimeTypeAndText("", p.value);
                            helper.flyoutProvider?.StartFlyoverModal(uc);
                            if (uc.Result)
                            {
                                p.value = uc.Text;
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddHintBubble(
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

                if (helper.SafeguardAccess(
                        stack, repo, p.valueId, "valueId:", "Create data element!",
                        v =>
                        {
                            p.valueId = new AdminShell.Reference();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "ValueID", levelColors[1][0], levelColors[1][1]);
                    helper.AddKeyListKeys(
                        stack, "valueId", p.valueId.Keys, repo,
                        packages, PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.GlobalReference);
                }
            }
            else if (sme is AdminShell.MultiLanguageProperty)
            {
                var mlp = sme as AdminShell.MultiLanguageProperty;
                helper.AddGroup(stack, "MultiLanguageProperty", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
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
                if (helper.SafeguardAccess(
                        stack, repo, mlp.value, "value:", "Create data element!",
                        v =>
                        {
                            mlp.value = new AdminShell.LangStringSet();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                    helper.AddKeyListLangStr(stack, "value", mlp.value.langString, repo);

                if (helper.SafeguardAccess(
                        stack, repo, mlp.valueId, "valueId:", "Create data element!",
                        v =>
                        {
                            mlp.valueId = new AdminShell.Reference();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "ValueID", levelColors[1][0], levelColors[1][1]);
                    helper.AddKeyListKeys(
                        stack, "valueId", mlp.valueId.Keys, repo,
                        packages, PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.GlobalReference);
                }
            }
            else if (sme is AdminShell.Range)
            {
                var rng = sme as AdminShell.Range;
                helper.AddGroup(stack, "Range", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rng.valueType == null || rng.valueType.Trim().Length < 1; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddKeyValueRef(
                    stack, "valueType", rng, ref rng.valueType, null, repo,
                    v => { rng.valueType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.DataElement.ValueTypeItems);

                var mine = rng.min == null || rng.min.Trim().Length < 1;
                var maxe = rng.max == null || rng.max.Trim().Length < 1;

                helper.AddHintBubble(
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
                helper.AddKeyValueRef(
                    stack, "min", rng, ref rng.min, null, repo,
                    v => { rng.min = v as string; return new ModifyRepo.LambdaActionNone(); });

                helper.AddHintBubble(
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
                helper.AddKeyValueRef(
                    stack, "max", rng, ref rng.max, null, repo,
                    v => { rng.max = v as string; return new ModifyRepo.LambdaActionNone(); });
            }
            else if (sme is AdminShell.File fl)
            {
                helper.AddGroup(stack, "File", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
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
                helper.AddKeyValueRef(
                    stack, "mimeType", fl, ref fl.mimeType, null, repo,
                    v => { fl.mimeType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.File.GetPopularMimeTypes());

                helper.AddHintBubble(
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
                helper.AddKeyValueRef(
                    stack, "value", fl, ref fl.value, null, repo,
                    v => { fl.value = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitles: new[] { "Choose supplementary file", },
                    auxButtonToolTips: new[] { "Select existing supplementary files" },
                    auxButtonLambda: (bi) =>
                    {
                        if (bi == 0)
                        {
                            // Select
                            var ve = helper.SmartSelectAasEntityVisualElement(
                                        packages, PackageCentral.Selector.Main, "File");
                            if (ve != null)
                            {
                                var sf = (ve.GetMainDataObject()) as AdminShellPackageSupplementaryFile;
                                if (sf != null)
                                {
                                    fl.value = sf.uri.ToString();
                                    return new ModifyRepo.LambdaActionRedrawEntity();
                                }
                            }
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });

                if (editMode && uploadAssistance != null && packages.Main != null)
                {
                    // More file actions
                    helper.AddAction(
                        stack, "Action", new[] { "Remove existing file", "Create text file", "Edit text file" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0 && fl.value.HasContent())
                            {
                                if (helper.flyoutProvider != null &&
                                    MessageBoxResult.Yes == helper.flyoutProvider.MessageBoxFlyoutShow(
                                    "Delete selected entity? This operation can not be reverted!", "AASX",
                                    MessageBoxButton.YesNo, MessageBoxImage.Warning))
                                {
                                    try
                                    {
                                        // try find ..
                                        var psfs = packages.Main.GetListOfSupplementaryFiles();
                                        var psf = psfs?.FindByUri(fl.value);
                                        if (psf == null)
                                        {
                                            Log.Error($"Not able to locate supplmentary file {fl.value} for removal! " +
                                                $"Aborting!");
                                        }
                                        else
                                        {
                                            Log.Info($"Removing file {fl.value} ..");
                                            packages.Main.DeleteSupplementaryFile(psf);
                                            Log.Info($"Added {fl.value} to pending package items to be deleted. " +
                                                "A save-operation might be required.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, $"Removing file {fl.value} in package");
                                    }

                                    // clear value
                                    fl.value = "";

                                    // show empty
                                    return new ModifyRepo.LambdaActionRedrawEntity();
                                }
                            }

                            if (buttonNdx == 1)
                            {
                                // ask for a name
                                var uc = new TextBoxFlyout("Name of text file to create",
                                        MessageBoxImage.Question);
                                uc.Text = "Textfile_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
                                helper.flyoutProvider?.StartFlyoverModal(uc);
                                if (!uc.Result)
                                {
                                    return new ModifyRepo.LambdaActionNone();
                                }

                                var ptd = "/aasx/";
                                var ptfn = uc.Text.Trim();
                                packages.Main.PrepareSupplementaryFileParameters(ref ptd, ref ptfn);

                                // make sure the name is not already existing
                                var psfs = packages.Main.GetListOfSupplementaryFiles();
                                var psf = psfs?.FindByUri(ptd + ptfn);
                                if (psf != null)
                                {
                                    helper.flyoutProvider?.MessageBoxFlyoutShow(
                                        $"The supplementary file {ptd + ptfn} is already existing in the " +
                                        "package. Please re-try with a different file name.", "Create text file",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return new ModifyRepo.LambdaActionNone();
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
                                        Log.Error($"Error creating text-file {ptd + ptfn} within package");
                                    }
                                    else
                                    {
                                        Log.Info(
                                            $"Added empty text-file {ptd + ptfn} to pending package items. " +
                                            $"A save-operation is required.");
                                        fl.mimeType = mimeType;
                                        fl.value = targetPath;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, $"Creating text-file {ptd + ptfn} within package");
                                }
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: sme);
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
                                        Log.Error($"Not able to locate supplmentary file {fl.value} for edit. " +
                                            $"Aborting!");
                                        return new ModifyRepo.LambdaActionNone();
                                    }

                                    // try read ..
                                    Log.Info($"Reading text-file {fl.value} ..");
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
                                        Log.Error($"Not able to read contents from  supplmentary file {fl.value} " +
                                            $"for edit. Aborting!");
                                        return new ModifyRepo.LambdaActionNone();
                                    }

                                    // edit
                                    var uc = new TextEditorFlyout($"Edit text-file '{fl.value}'");
                                    uc.SetMimeTypeAndText(fl.mimeType, contents);
                                    helper.flyoutProvider?.StartFlyoverModal(uc);
                                    if (!uc.Result)
                                        return new ModifyRepo.LambdaActionNone();

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
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, $"Edit text-file {fl.value} in package.");
                                }

                                // reshow
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            }

                            return new ModifyRepo.LambdaActionNone();
                        });

                    // Further file assistance
                    helper.AddGroup(stack, "Supplementary file assistance", levelColors[1][0], levelColors[1][1]);

                    helper.AddKeyValueRef(
                        stack, "Target path", this.uploadAssistance, ref this.uploadAssistance.TargetPath, null, repo,
                        v =>
                        {
                            this.uploadAssistance.TargetPath = v as string;
                            return new ModifyRepo.LambdaActionNone();
                        });

                    helper.AddKeyDropTarget(
                        stack, "Source file to add",
                        !(this.uploadAssistance.SourcePath.HasContent())
                            ? "(Please drop a file to set source file to add)"
                            : this.uploadAssistance.SourcePath,
                        null, repo,
                        v =>
                        {
                            this.uploadAssistance.SourcePath = v as string;
                            return new ModifyRepo.LambdaActionNone();
                        }, minHeight: 40);

                    helper.AddAction(
                    stack, "Action", new[] { "Select source file", "Add or update to AASX" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var dlg = new Microsoft.Win32.OpenFileDialog();
                                var res = dlg.ShowDialog();
                                if (res == true)
                                {
                                    this.uploadAssistance.SourcePath = dlg.FileName;
                                    return new ModifyRepo.LambdaActionRedrawEntity();
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
                                        Log.Error($"Error adding file {uploadAssistance.SourcePath} to package");
                                    }
                                    else
                                    {
                                        Log.Info(
                                            $"Added {ptfn} to pending package items. A save-operation is required.");
                                        fl.mimeType = mimeType;
                                        fl.value = targetPath;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, $"Adding file {uploadAssistance.SourcePath} to package");
                                }

                                // refresh dialogue
                                uploadAssistance.SourcePath = "";
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            }

                            return new ModifyRepo.LambdaActionNone();
                        });
                }
            }
            else if (sme is AdminShell.Blob blb)
            {
                helper.AddGroup(stack, "Blob", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return blb.mimeType == null || blb.mimeType.Trim().Length < 1 ||
                                    blb.mimeType.IndexOf('/') < 1 || blb.mimeType.EndsWith("/");
                            },
                            "The mime-type of the file. Mandatory information. See RFC2046.")
                    });
                helper.AddKeyValueRef(
                    stack, "mimeType", blb, ref blb.mimeType, null, repo,
                    v => { blb.mimeType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.File.GetPopularMimeTypes());

                helper.AddKeyValueRef(
                    stack, "value", blb, ref blb.value, null, repo,
                    v => { blb.value = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitles: new[] { "\u2261" },
                    auxButtonToolTips: new[] { "Edit in multiline editor" },
                    auxButtonLambda: (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var uc = new TextEditorFlyout($"Edit Blob '{"" + blb.idShort}'");
                            uc.SetMimeTypeAndText(blb.mimeType, blb.value);
                            helper.flyoutProvider?.StartFlyoverModal(uc);
                            if (uc.Result)
                            {
                                blb.value = uc.Text;
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });
            }
            else if (sme is AdminShell.ReferenceElement rfe)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                helper.AddGroup(stack, "ReferenceElement", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
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
                if (helper.SafeguardAccess(
                        stack, repo, rfe.value, "Target reference:", "Create data element!",
                        v =>
                        {
                            rfe.value = new AdminShell.Reference();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddKeyListKeys(stack, "value", rfe.value.Keys, repo,
                        packages, PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: (kl) =>
                        {
                            return new ModifyRepo.LambdaActionNavigateTo(AdminShell.Reference.CreateNew(kl));
                        });
                }
            }
            else
            if (sme is AdminShell.RelationshipElement rele)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                helper.AddGroup(stack, "" + sme.GetElementName(), levelColors[0][0], levelColors[0][1]);

                // members
                helper.AddHintBubble(
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
                if (helper.SafeguardAccess(
                        stack, repo, rele.first, "First relation:", "Create data element!",
                        v =>
                        {
                            rele.first = new AdminShell.Reference();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddKeyListKeys(
                        stack, "first", rele.first.Keys, repo,
                        packages, PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: (kl) =>
                        {
                            return new ModifyRepo.LambdaActionNavigateTo(AdminShell.Reference.CreateNew(kl));
                        });
                }

                helper.AddHintBubble(
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
                if (helper.SafeguardAccess(
                        stack, repo, rele.first, "Second relation:", "Create data element!",
                        v =>
                        {
                            rele.second = new AdminShell.Reference();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddKeyListKeys(
                        stack, "second", rele.second.Keys, repo,
                        packages, PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: (kl) =>
                        {
                            return new ModifyRepo.LambdaActionNavigateTo(AdminShell.Reference.CreateNew(kl));
                        });
                }

                // specifically for annotated relationship?
                if (sme is AdminShell.AnnotatedRelationshipElement /* arele */)
                {
                }
            }
            else if (sme is AdminShell.Capability)
            {
                helper.AddGroup(stack, "Capability", levelColors[0][0], levelColors[0][1]);
                helper.AddKeyValue(stack, "Value", "Right now, Capability does not have further value elements.");
            }
            else
            if (sme is AdminShell.SubmodelElementCollection smc)
            {
                helper.AddGroup(stack, "SubmodelElementCollection", levelColors[0][0], levelColors[0][1]);
                if (smc.value != null)
                    helper.AddKeyValue(stack, "# of values", "" + smc.value.Count);
                else
                    helper.AddKeyValue(stack, "Values", "Please add elements via editing of sub-ordinate entities");

                helper.AddCheckBox(
                    stack, "ordered:", smc.ordered, " (true e.g. for indexed array)", (b) => { smc.ordered = b; });
                helper.AddCheckBox(
                    stack, "allowDuplicates:", smc.allowDuplicates,
                    " (true, if multiple elements with same semanticId)", (b) => { smc.allowDuplicates = b; });
            }
            else if (sme is AdminShell.Operation)
            {
                var p = sme as AdminShell.Operation;
                helper.AddGroup(stack, "Operation", levelColors[0][0], levelColors[0][1]);
                if (p.inputVariable != null)
                    helper.AddKeyValue(stack, "# of input vars.", "" + p.inputVariable.Count);
                if (p.outputVariable != null)
                    helper.AddKeyValue(stack, "# of output vars.", "" + p.outputVariable.Count);
                if (p.inoutputVariable != null)
                    helper.AddKeyValue(stack, "# of in/out vars.", "" + p.inoutputVariable.Count);
            }
            else if (sme is AdminShell.Entity)
            {
                var ent = sme as AdminShell.Entity;
                helper.AddGroup(stack, "Entity", levelColors[0][0], levelColors[0][1]);

                if (ent.statements != null)
                    helper.AddKeyValue(stack, "# of statements", "" + ent.statements.Count);
                else
                    helper.AddKeyValue(
                        stack, "Statements", "Please add statements via editing of sub-ordinate entities");

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return ent.entityType == null ||
                                    ent.GetEntityType() == AdminShellV20.Entity.EntityTypeEnum.Undef;
                            },
                            "EntityType needs to be either CoManagedEntity (no assigned Asset reference) " +
                                "or SelfManagedEntity (with assigned Asset reference)",
                            severityLevel: HintCheck.Severity.High)
                    });
                helper.AddKeyValueRef(
                    stack, "entityType", ent, ref ent.entityType, null, repo,
                    v => { ent.entityType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxItems: AdminShell.Entity.EntityTypeNames,
                    comboBoxIsEditable: true);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return ent.entityType != null &&
                                    ent.GetEntityType() == AdminShellV20.Entity.EntityTypeEnum.SelfManagedEntity &&
                                    (ent.assetRef == null || ent.assetRef.Count < 1);
                            },
                            "Please choose the Asset for the SelfManagedEntity.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (helper.SafeguardAccess(
                        stack, repo, ent.assetRef, "Asset:", "Create data element!",
                        v =>
                        {
                            ent.assetRef = new AdminShell.AssetRef();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddKeyListKeys(
                        stack, "Asset", ent.assetRef.Keys, repo, packages, PackageCentral.Selector.MainAuxFileRepo,
                        AdminShell.Key.AllElements);
                }

            }
            else
                helper.AddGroup(stack, "Submodel Element is unknown!", levelColors[0][0], levelColors[0][1]);
        }

        //
        //
        // --- View
        //
        //

        public void DisplayOrEditAasEntityView(
            PackageCentral packages, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell shell,
            AdminShell.View view, bool editMode, ModifyRepo repo, StackPanel stack, Brush[][] levelColors,
            bool hintMode = false)
        {
            //
            // View
            //
            helper.AddGroup(stack, "View", levelColors[0][0], levelColors[0][1]);

            if (editMode)
            {
                // Up/ down/ del
                helper.EntityListUpDownDeleteHelper<AdminShell.View>(
                    stack, repo, shell.views.views, view, env, "View:");

                // let the user control the number of references
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return view.containedElements == null || view.containedElements.Count < 1; },
                            "This View currently has no references to SubmodelElements, yet. " +
                                "You could create them by clicking the 'Add ..' button below.",
                            severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "containedElements:", new[] { "Add Reference to SubmodelElement", }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var ks = helper.SmartSelectAasEntityKeys(
                                        packages, PackageCentral.Selector.Main, "SubmodelElement");
                            if (ks != null)
                            {
                                view.AddContainedElement(ks);
                            }
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: view);
                        }

                        return new ModifyRepo.LambdaActionNone();
                    });
            }
            else
            {
                int num = 0;
                if (view.containedElements != null && view.containedElements.reference != null)
                    num = view.containedElements.reference.Count;

                var g = helper.AddSmallGrid(1, 1, new[] { "*" });
                helper.AddSmallLabelTo(g, 0, 0, content: $"# of containedElements: {num}");
                stack.Children.Add(g);
            }

            // Referable
            helper.DisplayOrEditEntityReferable(stack, view, categoryUsual: false);

            // HasSemantics
            helper.DisplayOrEditEntitySemanticId(stack, view.semanticId,
                (sid) => { view.semanticId = sid; },
                "Only by adding this, a computer can distinguish, for what the view is really meant for.",
                checkForCD: false,
                addExistingEntities: AdminShell.Key.ConceptDescription);

            // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            helper.DisplayOrEditEntityHasDataSpecificationReferences(stack, view.hasDataSpecification,
                (ds) => { view.hasDataSpecification = ds; });

        }

        public void DisplayOrEditAasEntityViewReference(
            PackageCentral packages, AdminShell.AdministrationShellEnv env, AdminShell.View view,
            AdminShell.ContainedElementRef reference, bool editMode, ModifyRepo repo, StackPanel stack,
            Brush[][] levelColors)
        {
            //
            // View
            //
            helper.AddGroup(stack, "Reference (containedElement) of View ", levelColors[0][0], levelColors[0][1]);

            if (editMode)
            {
                // Up/ down/ del
                helper.EntityListUpDownDeleteHelper<AdminShell.ContainedElementRef>(
                    stack, repo, view.containedElements.reference, reference, null, "Reference:");
            }

            // normal reference
            helper.AddKeyListKeys(stack, "containedElement", reference.Keys, repo,
                packages, PackageCentral.Selector.Main, AdminShell.Key.AllElements);
        }

        //
        //
        // --- Overall calling function
        //
        //

        public StackPanel ClearDisplayDefautlStack()
        {
            theMasterPanel.Children.Clear();
            var sp = new StackPanel();
            DockPanel.SetDock(sp, Dock.Top);
            theMasterPanel.Children.Add(sp);
            return sp;
        }

        public void ClearHighlight()
        {
            if (this.helper != null)
                this.helper.ClearHighlights();
        }

        public class DisplayRenderHints
        {
            public bool scrollingPanel = true;
            public bool showDataPanel = true;
        }

        public DisplayRenderHints DisplayOrEditVisualAasxElement(
            PackageCentral packages,
            VisualElementGeneric entity,
            bool editMode, bool hintMode = false,
            IFlyoutProvider flyoutProvider = null,
            DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            //
            // Start
            //

            var renderHints = new DisplayRenderHints();

            if (theMasterPanel == null || entity == null)
            {
                renderHints.showDataPanel = false;
                return renderHints;
            }

            var stack = ClearDisplayDefautlStack();

            // ReSharper disable CoVariantArrayConversion
            Brush[][] levelColors = new Brush[][]
            {
                new [] {
                    (SolidColorBrush)System.Windows.Application.Current.Resources["DarkestAccentColor"],
                    Brushes.White
                },
                new [] {
                    (SolidColorBrush)System.Windows.Application.Current.Resources["LightAccentColor"],
                    Brushes.Black
                },
                new [] {
                    (SolidColorBrush)System.Windows.Application.Current.Resources["LightAccentColor"],
                    Brushes.Black
                }
            };
            // ReSharper enable CoVariantArrayConversion

            // hint mode disable, when not edit

            hintMode = hintMode && editMode;

            // remember objects for UI thread / redrawing
            this.packages = packages;
            this.theEntity = entity;
            helper.packages = packages;
            helper.flyoutProvider = flyoutProvider;
            helper.levelColors = levelColors;
            helper.highlightField = hightlightField;

            // modify repository
            ModifyRepo repo = null;
            if (editMode)
            {
                repo = theModifyRepo;
                repo.Clear();
            }
            helper.editMode = editMode;
            helper.hintMode = hintMode;
            helper.repo = repo;

            //
            // Dispatch
            //

            if (entity is VisualElementEnvironmentItem)
            {
                var x = entity as VisualElementEnvironmentItem;
                DisplayOrEditAasEntityAasEnv(
                    packages, x.theEnv, x.theItemType, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementAdminShell)
            {
                var x = entity as VisualElementAdminShell;
                DisplayOrEditAasEntityAas(
                    packages, x.theEnv, x.theAas, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementAsset)
            {
                var x = entity as VisualElementAsset;
                DisplayOrEditAasEntityAsset(
                    packages, x.theEnv, x.theAsset, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodelRef)
            {
                var x = entity as VisualElementSubmodelRef;
                AdminShell.AdministrationShell aas = null;
                if (x.Parent is VisualElementAdminShell xpaas)
                    aas = xpaas.theAas;
                DisplayOrEditAasEntitySubmodelOrRef(
                    packages, x.theEnv, aas, x.theSubmodelRef, x.theSubmodel, editMode, repo, stack, levelColors,
                    hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodel)
            {
                var x = entity as VisualElementSubmodel;
                DisplayOrEditAasEntitySubmodelOrRef(
                    packages, x.theEnv, null, null, x.theSubmodel, editMode, repo, stack, levelColors,
                    hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodelElement)
            {
                var x = entity as VisualElementSubmodelElement;
                DisplayOrEditAasEntitySubmodelElement(
                    packages, x.theEnv, x.theContainer, x.theWrapper, x.theWrapper.submodelElement, editMode,
                    repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementOperationVariable)
            {
                var x = entity as VisualElementOperationVariable;
                DisplayOrEditAasEntityOperationVariable(
                    packages, x.theEnv, x.theContainer, x.theOpVar, editMode, repo,
                    stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementConceptDescription)
            {
                var x = entity as VisualElementConceptDescription;
                DisplayOrEditAasEntityConceptDescription(
                    packages, x.theEnv, null, x.theCD, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementView)
            {
                var x = entity as VisualElementView;
                if (x.Parent != null && x.Parent is VisualElementAdminShell xpaas)
                    DisplayOrEditAasEntityView(
                        packages, x.theEnv, xpaas.theAas, x.theView, editMode, repo, stack, levelColors,
                        hintMode: hintMode);
                else
                    helper.AddGroup(stack, "View is corrupted!", levelColors[0][0], levelColors[0][1]);
            }
            else if (entity is VisualElementReference)
            {
                var x = entity as VisualElementReference;
                if (x.Parent != null && x.Parent is VisualElementView xpev)
                    DisplayOrEditAasEntityViewReference(
                        packages, x.theEnv, xpev.theView, (AdminShell.ContainedElementRef)x.theReference,
                        editMode, repo, stack, levelColors);
                else
                    helper.AddGroup(stack, "Reference is corrupted!", levelColors[0][0], levelColors[0][1]);
            }
            else
            if (entity is VisualElementSupplementalFile)
            {
                var x = entity as VisualElementSupplementalFile;
                DisplayOrEditAasEntitySupplementaryFile(packages, x.theFile, editMode, repo, stack, levelColors);
            }
            else if (entity is VisualElementPluginExtension)
            {
                // get data
                var x = entity as VisualElementPluginExtension;

                // create controls
                object result = null;

                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    // replace at top level
                    theMasterPanel.Children.Clear();
                    if (x.thePlugin != null)
                        result = x.thePlugin.InvokeAction(
                            "fill-panel-visual-extension", x.thePackage, x.theReferable, theMasterPanel);
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause

                // add?
                if (result == null)
                {
                    // re-init display!
                    stack = ClearDisplayDefautlStack();

                    // helping message
                    helper.AddGroup(
                        stack, "Entity from Plugin cannot be rendered!", levelColors[0][0], levelColors[0][1]);
                }
                else
                {
                }

                // show no panel nor scroll
                renderHints.scrollingPanel = false;
                renderHints.showDataPanel = false;

            }
            else
                helper.AddGroup(stack, "Entity is unknown!", levelColors[0][0], levelColors[0][1]);

            // return render hints
            return renderHints;
        }

        #endregion
    }
}
