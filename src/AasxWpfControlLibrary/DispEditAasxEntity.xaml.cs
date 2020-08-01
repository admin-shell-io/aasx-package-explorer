using System;
using System.Collections.Generic;
using System.Globalization;
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

        private AdminShellPackageEnv thePackage = null;
        private VisualElementGeneric theEntity = null;

        private ModifyRepo theModifyRepo = new ModifyRepo();

        private DispEditHelper helper = new DispEditHelper();

        private class CopyPasteBuffer
        {
            public bool duplicate = false;
            public AdminShell.Referable parentContainer = null;
            public AdminShell.SubmodelElementWrapper wrapper = null;
            public AdminShell.SubmodelElement sme = null;
        }

        private CopyPasteBuffer theCopyPaste = null;

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

                    // what?
                    if (temp is ModifyRepo.LambdaActionRedrawEntity)
                        if (thePackage != null && theEntity != null)
                            DisplayOrEditVisualAasxElement(
                                thePackage, theEntity, helper.editMode, helper.hintMode,
                                auxPackages: helper.auxPackages, flyoutProvider: helper.flyoutProvider);
                    if (temp is ModifyRepo.LambdaActionRedrawAllElements
                        || temp is ModifyRepo.LambdaActionContentsChanged
                        || temp is ModifyRepo.LambdaActionContentsTakeOver)
                        // Unfortunately twice as ugly
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
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env, AdminShell.Asset asset,
            bool editMode, ModifyRepo repo, StackPanel stack, Brush[][] levelColors, bool embedded = false,
            bool hintMode = false)
        {
            helper.AddGroup(stack, "Asset", levelColors[0][0], levelColors[0][1]);

            // Up/ down/ del
            if (editMode && !embedded)
            {
                helper.EntityListUpDownDeleteHelper<AdminShell.Asset>(stack, repo, env.Assets, asset, env, "Asset:");
            }

            // print code sheet
            helper.AddAction(stack, "Actions:", new[] { "Print asset code sheet .." }, repo, (buttonNdx) =>
           {
               if (buttonNdx is int)
               {
                   if ((int)buttonNdx == 0)
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
               }
               return new ModifyRepo.LambdaActionNone();
           });

            // Referable
            helper.AddGroup(stack, "Referable members:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck( () => { return asset.idShort == null || asset.idShort.Length < 1; },
                    "idShort is mandatory for assets. It is a short, " +
                        "unique identifier that is unique just in its context, its name space. ", breakIfTrue: true),

                new HintCheck(
                    () => {
                        if (asset.idShort == null) return false; return !AdminShellUtil.ComplyIdShort(asset.idShort);
                    },
                    "idShort shall only feature letters, digits, underscore ('_'); " +
                        "starting mandatory with a letter..")
            });
            helper.AddKeyValueRef(
                stack, "idShort", asset, ref asset.idShort, null, repo,
                v => { asset.idShort = v as string; return new ModifyRepo.LambdaActionNone(); });

            helper.AddHintBubble(
                stack, hintMode,
                new HintCheck(() => { return asset.category != null && asset.category.Trim().Length >= 1; },
                "The use of category is unusual here.", severityLevel: HintCheck.Severity.Notice));

            helper.AddKeyValueRef(
                stack, "category", asset, ref asset.category, null, repo,
                v => { asset.category = v as string; return new ModifyRepo.LambdaActionNone(); },
                comboBoxItems: AdminShell.Referable.ReferableCategoryNames, comboBoxIsEditable: true);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return asset.description == null || asset.description.langString == null ||
                                asset.description.langString.Count < 1;
                        },
                        "The use of an description is recommended to allow the consumer of an asset " +
                            "to understand the nature of it.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return asset.description.langString.Count < 2; },
                        "Consider having description in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (helper.SafeguardAccess(stack, repo, asset.description, "description:", "Create data element!", v =>
            {
                asset.description = new AdminShell.Description();
                return new ModifyRepo.LambdaActionRedrawEntity();
            }))
            {
                helper.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () =>
                        {
                            return asset.description.langString == null || asset.description.langString.Count < 1;
                        },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Administration shell to understand your intentions."));
                helper.AddKeyListLangStr(stack, "description", asset.description.langString, repo);
            }

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            if (helper.SafeguardAccess(
                    stack, repo, asset.hasDataSpecification, "HasDataSpecification:", "Create data element!",
                    v =>
                    {
                        asset.hasDataSpecification = new AdminShell.HasDataSpecification();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddGroup(stack, "HasDataSpecification", levelColors[1][0], levelColors[1][1]);

                if (editMode)
                {
                    // let the user control the number of references
                    helper.AddAction(
                        stack, "Specifications:",
                        new[] { "Add Reference", "Delete last reference" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx is int)
                            {
                                if ((int)buttonNdx == 0)
                                    asset.hasDataSpecification.reference.Add(new AdminShell.Reference());

                                if ((int)buttonNdx == 1 && asset.hasDataSpecification.reference.Count > 0)
                                    asset.hasDataSpecification.reference.RemoveAt(
                                       asset.hasDataSpecification.reference.Count - 1);
                            }
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (asset.hasDataSpecification != null && asset.hasDataSpecification.reference != null &&
                        asset.hasDataSpecification.reference.Count > 0)
                {
                    for (int i = 0; i < asset.hasDataSpecification.reference.Count; i++)
                        helper.AddKeyListKeys(
                            stack, String.Format("reference[{0}]", i), asset.hasDataSpecification.reference[i].Keys,
                            repo, package, addExistingEntities: null /* "All" */);
                }
            }

            // Identifiable

            helper.AddGroup(stack, "Identifiable members:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return asset.identification == null; },
                    "Providing a worldwide unique identification is mandatory.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return asset.identification.idType != AdminShell.Identification.IRI; },
                    "Check if identification type is correct. Use of IRIs is usual here.",
                    severityLevel: HintCheck.Severity.Notice ),
                new HintCheck(
                    () => { return asset.identification.id.Trim() == ""; },
                    "Identification id shall not be empty. You could use the 'Generate' button in order to " +
                        "generate a worldwide unique id. " +
                        "The template of this id could be set by commandline arguments." )

            });
            if (helper.SafeguardAccess(
                    stack, repo, asset.identification, "identification:", "Create data element!",
                    v =>
                    {
                        asset.identification = new AdminShell.Identification();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddKeyValueRef(
                    stack, "idType", asset, ref asset.identification.idType, null, repo,
                    v => { asset.identification.idType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxItems: AdminShell.Key.IdentifierTypeNames);

                helper.AddKeyValueRef(
                    stack, "id", asset, ref asset.identification.id, null, repo,
                    v => { asset.identification.id = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitles: new[] { "Generate", "Input" },
                    auxButtonLambda: (i) =>
                    {
                        if (i is int && (int)i == 0)
                        {
                            asset.identification.idType = AdminShell.Identification.IRI;
                            asset.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdAsset);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: asset);
                        }
                        if (i is int && (int)i == 1)
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
                        return new ModifyRepo.LambdaActionNone();
                    });
            }

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return asset.administration == null; },
                    "Check if providing admistrative information on version/ revision would be useful. " +
                        "This allows for better version management.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () =>
                    {
                        return asset.administration.version.Trim() == "" ||
                            asset.administration.revision.Trim() == "";
                    },
                    "Admistrative information fields should not be empty.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (helper.SafeguardAccess(
                    stack, repo, asset.administration, "administration:", "Create data element!",
                    v =>
                    {
                        asset.administration = new AdminShell.Administration();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddKeyValueRef(
                    stack, "version", asset.administration, ref asset.administration.version, null, repo,
                    v => { asset.administration.version = v as string; return new ModifyRepo.LambdaActionNone(); });

                helper.AddKeyValueRef(
                    stack, "revision", asset.administration, ref asset.administration.revision, null, repo,
                    v => { asset.administration.revision = v as string; return new ModifyRepo.LambdaActionNone(); });
            }

            // Kind

            helper.AddGroup(stack, "Kind:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return asset.kind == null; },
                    "Providing kind information is mandatory. Typically you want to model instances. " +
                        "A manufacturer would define types of assets, as well.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return asset.kind.kind.Trim().ToLower() != "instance"; },
                    "Check for kind setting. 'Instance' is the usual choice.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (helper.SafeguardAccess(
                    stack, repo, asset.kind, "kind:", "Create data element!",
                    v =>
                    {
                        asset.kind = new AdminShell.AssetKind();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                helper.AddKeyValueRef(
                    stack, "kind", asset.kind, ref asset.kind.kind, null, repo,
                    v => { asset.kind.kind = v as string; return new ModifyRepo.LambdaActionNone(); },
                    new[] { "Template", "Instance" });

            // AssetIdentificationModelRef

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return asset.assetIdentificationModelRef == null; },
                    "No asset identification model. please consider adding a reference " +
                        "to an identification Submodel."),
            });
            if (helper.SafeguardAccess(
                    stack, repo, asset.assetIdentificationModelRef, "assetIdentificationModel:",
                    "Create data element!",
                    v =>
                    {
                        asset.assetIdentificationModelRef = new AdminShell.SubmodelRef();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddGroup(
                    stack, "Asset Identification Model - Reference to describing Submodel",
                    levelColors[1][0], levelColors[1][1]);
                helper.AddKeyListKeys(
                    stack, "assetIdentificationModelRef", asset.assetIdentificationModelRef.Keys,
                    repo, package, "Submodel");
            }

            // BillOfMaterialRef

            if (helper.SafeguardAccess(
                    stack, repo, asset.billOfMaterialRef, "billOfMaterial:", "Create data element!",
                    v =>
                    {
                        asset.billOfMaterialRef = new AdminShell.SubmodelRef();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddGroup(
                    stack, "Bill of Material - Reference to describing Submodel",
                    levelColors[1][0], levelColors[1][1]);
                helper.AddKeyListKeys(
                    stack, "billOfMaterial", asset.billOfMaterialRef.Keys, repo, package, "Submodel");
            }
        }

        //
        //
        // --- AAS Env
        //
        //

        static string PackageSourcePath = "";
        static string PackageTargetFn = "";
        static string PackageTargetDir = "/aasx";
        static bool PackageEmbedAsThumbnail = false;

        public void DisplayOrEditAasEntityAasEnv(
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env,
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
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0)
                            {
                                var asset = new AdminShell.Asset();
                                env.Assets.Add(asset);
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: asset);
                            }

                            if ((int)buttonNdx == 1)
                            {
                                var aas = new AdminShell.AdministrationShell();
                                env.AdministrationShells.Add(aas);
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: aas);
                            }

                            if ((int)buttonNdx == 2)
                            {
                                var cd = new AdminShell.ConceptDescription();
                                env.ConceptDescriptions.Add(cd);
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: cd);
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });

                // Copy AAS
                if (envItemType == VisualElementEnvironmentItem.ItemType.Shells)
                {
                    helper.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return helper.auxPackages != null;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                    helper.AddAction(
                        stack, "Copy from existing AAS:",
                        new[] { "Copy single entity ", "Copy recursively", "Copy rec. w/ suppl. files" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx is int)
                            {
                                if ((int)buttonNdx == 0 || (int)buttonNdx == 1 || (int)buttonNdx == 2)
                                {
                                    var rve = helper.SmartSelectAasEntityVisualElement(
                                        package.AasEnv, AdminShell.Key.AAS, package: package,
                                        auxPackages: helper.auxPackages) as VisualElementAdminShell;

                                    if (rve != null)
                                    {
                                        var copyRecursively = (int)buttonNdx == 1 || (int)buttonNdx == 2;
                                        var createNewIds = env == rve.theEnv;
                                        var copySupplFiles = (int)buttonNdx == 2;

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
                                                    destAAS.assetRef = destAsset.GetReference();
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
                                            if (copySupplFiles && rve.thePackage != null && package != rve.thePackage)
                                            {
                                                // copy conditions met
                                                foreach (var fn in potentialSupplFilesToCopy.Values)
                                                {
                                                    try
                                                    {
                                                        // copy ONLY if not existing in destination
                                                        // rationale: do not potential harm the source content, 
                                                        // even when voiding destination integrity
                                                        if (rve.thePackage.IsLocalFile(fn) && !package.IsLocalFile(fn))
                                                        {
                                                            var tmpFile =
                                                                rve.thePackage.MakePackageFileAvailableAsTempFile(fn);
                                                            var targetDir = System.IO.Path.GetDirectoryName(fn);
                                                            var targetFn = System.IO.Path.GetFileName(fn);
                                                            package.AddSupplementaryFileToStore(
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
                            }
                            return new ModifyRepo.LambdaActionNone();
                        });
                }

                // Copy Concept Descriptions
                if (envItemType == VisualElementEnvironmentItem.ItemType.ConceptDescriptions)
                {
                    helper.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return helper.auxPackages != null;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                    helper.AddAction(
                        stack, "Copy from existing ConceptDescription:",
                        new[] { "Copy single entity" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx is int)
                            {
                                if ((int)buttonNdx == 0)
                                {
                                    var rve = helper.SmartSelectAasEntityVisualElement(
                                        package.AasEnv, "ConceptDescription", package: package,
                                        auxPackages: helper.auxPackages) as VisualElementConceptDescription;
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
                            }
                            return new ModifyRepo.LambdaActionNone();
                        });
                }
            }
            else if (envItemType == VisualElementEnvironmentItem.ItemType.SupplFiles && package != null)
            {
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
                            package.AddSupplementaryFileToStore(
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
            AdminShellPackageEnv package, AdminShellPackageSupplementaryFile psf, bool editMode, ModifyRepo repo,
            StackPanel stack, Brush[][] levelColors)
        {
            //
            // Package
            //
            helper.AddGroup(stack, "Supplementary file for package of AASX", levelColors[0][0], levelColors[0][1]);

            if (editMode && package != null && psf != null)
            {
                helper.AddAction(stack, "Action", new[] { "Delete" }, repo, (buttonNdx) =>
               {
                   if (buttonNdx is int)
                   {
                       if ((int)buttonNdx == 0)
                           if (helper.flyoutProvider != null &&
                                MessageBoxResult.Yes == helper.flyoutProvider.MessageBoxFlyoutShow(
                                    "Delete selected entity? This operation can not be reverted!", "AASX",
                                    MessageBoxButton.YesNo, MessageBoxImage.Warning))
                           {
                               try
                               {
                                   package.DeleteSupplementaryFile(psf);
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
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas,
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
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0)
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
                                var ks = helper.SmartSelectAasEntityKeys(package.AasEnv, "Submodel");
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

                            if ((int)buttonNdx == 1 || (int)buttonNdx == 2)
                            {
                                // create new submodel
                                var submodel = new AdminShell.Submodel();
                                env.Submodels.Add(submodel);

                                // directly create identification, as we need it!
                                submodel.identification.idType = AdminShell.Identification.IRI;
                                if ((int)buttonNdx == 1)
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

                        }
                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return helper.auxPackages != null;  },
                        "You have opened an auxiliary AASX package. You can copy elements from it!",
                        severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "Copy from existing Submodel:",
                    new[] { "Copy single entity ", "Copy recursively" }, repo,
                    (buttonNdx) =>
                   {
                       if (buttonNdx is int)
                       {
                           if ((int)buttonNdx == 0 || (int)buttonNdx == 1)
                           {
                               var rve = helper.SmartSelectAasEntityVisualElement(
                                package.AasEnv, "SubmodelRef", package: package,
                                auxPackages: helper.auxPackages) as VisualElementSubmodelRef;

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
                                                copyCD: true, shallowCopy: (int)buttonNdx == 0);
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
                       }
                       return new ModifyRepo.LambdaActionNone();
                   });

                // let the user control the number of entities
                helper.AddAction(stack, "Entities:", new[] { "Add View" }, repo, (buttonNdx) =>
               {
                   if (buttonNdx is int)
                   {
                       if ((int)buttonNdx == 0)
                       {
                           var view = new AdminShell.View();
                           aas.AddView(view);
                           return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: view);
                       }
                   }
                   return new ModifyRepo.LambdaActionNone();
               });
            }

            // Referable
            helper.AddGroup(stack, "Referable members:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return aas.idShort != null && aas.idShort.Length > 0; },
                        "idShort is at least not required here. " +
                            "The specification lists it as 'n/a' for Administration Shells.",
                        severityLevel: HintCheck.Severity.Notice ),
                    new HintCheck(
                        () => {
                            if (aas.idShort == null) return false;
                            return !AdminShellUtil.ComplyIdShort(aas.idShort);
                        },
                        "idShort shall only feature letters, digits, underscore ('_'); " +
                            "starting mandatory with a letter..")
            });
            helper.AddKeyValueRef(
                stack, "idShort", aas, ref aas.idShort, null, repo,
                v => { aas.idShort = v as string; return new ModifyRepo.LambdaActionNone(); });

            helper.AddHintBubble(
                stack, hintMode,
                new HintCheck(
                    () => { return aas.category != null && aas.category.Length >= 1; },
                    "The use of category is unusual here.",
                    severityLevel: HintCheck.Severity.Notice));
            helper.AddKeyValueRef(
                stack, "category", aas, ref aas.category, null, repo,
                v => { aas.category = v as string; return new ModifyRepo.LambdaActionNone(); },
                comboBoxItems: AdminShell.Referable.ReferableCategoryNames, comboBoxIsEditable: true);

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => {
                        return aas.description == null || aas.description.langString == null ||
                            aas.description.langString.Count < 1;  },
                    "The use of an description is recommended to allow the consumer of an Administration Shell " +
                        "to understand the nature of it.",
                    breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () => { return aas.description.langString.Count < 2; },
                    "Consider having description in multiple langauges.",
                    severityLevel: HintCheck.Severity.Notice)
            });
            if (helper.SafeguardAccess(
                stack, repo, aas.description, "description:", "Create data element!",
                v =>
                {
                    aas.description = new AdminShell.Description();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
            {
                helper.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () => { return aas.description.langString == null || aas.description.langString.Count < 1; },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Admin shell to understand your intentions."));
                helper.AddKeyListLangStr(stack, "description", aas.description.langString, repo);
            }

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            if (helper.SafeguardAccess(
                stack, repo, aas.hasDataSpecification, "HasDataSpecification:", "Create data element!",
                v =>
                {
                    aas.hasDataSpecification = new AdminShell.HasDataSpecification();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
            {
                helper.AddGroup(stack, "HasDataSpecification", levelColors[1][0], levelColors[1][1]);

                if (editMode)
                {
                    // let the user control the number of references
                    helper.AddAction(
                        stack, "Specifications:",
                        new[] { "Add Reference", "Delete last reference" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx is int)
                            {
                                if ((int)buttonNdx == 0)
                                    aas.hasDataSpecification.reference.Add(new AdminShell.Reference());

                                if ((int)buttonNdx == 1 && aas.hasDataSpecification.reference.Count > 0)
                                    aas.hasDataSpecification.reference.RemoveAt(
                                        aas.hasDataSpecification.reference.Count - 1);
                            }
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (aas.hasDataSpecification != null && aas.hasDataSpecification.reference != null &&
                    aas.hasDataSpecification.reference.Count > 0)
                {
                    for (int i = 0; i < aas.hasDataSpecification.reference.Count; i++)
                        helper.AddKeyListKeys(
                            stack, String.Format("reference[{0}]", i), aas.hasDataSpecification.reference[i].Keys,
                            repo, package, addExistingEntities: null /* "All" */);
                }
            }

            helper.AddGroup(stack, "Identifiable members:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return aas.identification == null; },
                        "Providing a worldwide unique identification is mandatory.",
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return aas.identification.idType != AdminShell.Identification.IRI; },
                        "Check if identification type is correct. Use of IRIs is usual here.",
                        severityLevel: HintCheck.Severity.Notice ),
                    new HintCheck(
                        () => { return aas.identification.id.Trim() == ""; },
                        "Identification id shall not be empty. You could use the 'Generate' button in order to " +
                            "generate a worldwide unique id. " +
                            "The template of this id could be set by commandline arguments." )

            });
            if (helper.SafeguardAccess(
                    stack, repo, aas.identification, "identification:", "Create data element!",
                    v =>
                    {
                        aas.identification = new AdminShell.Identification();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddKeyValueRef(
                    stack, "idType", aas.identification, ref aas.identification.idType, null, repo,
                    v => { aas.identification.idType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxItems: AdminShell.Key.IdentifierTypeNames);

                helper.AddKeyValueRef(
                    stack, "id", aas.identification, ref aas.identification.id, null, repo,
                    v => { aas.identification.id = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitle: "Generate",
                    auxButtonLambda: v =>
                    {
                        aas.identification.idType = AdminShell.Identification.IRI;
                        aas.identification.id = Options.Curr.GenerateIdAccordingTemplate(Options.Curr.TemplateIdAas);
                        return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: aas);
                    });
            }

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return aas.administration == null; },
                    "Check if providing admistrative information on version/ revision would be useful. " +
                        "This allows for better version management.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () =>
                    {
                        return aas.administration.version.Trim() == "" || aas.administration.revision.Trim() == "";
                    },
                    "Admistrative information fields should not be empty.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (helper.SafeguardAccess(stack, repo, aas.administration, "administration:", "Create data element!", v =>
            {
                aas.administration = new AdminShell.Administration();
                return new ModifyRepo.LambdaActionRedrawEntity();
            }))
            {
                helper.AddKeyValueRef(
                    stack, "version", aas.administration, ref aas.administration.version, null, repo,
                    v => { aas.administration.version = v as string; return new ModifyRepo.LambdaActionNone(); });
                helper.AddKeyValueRef(
                    stack, "revision", aas.administration, ref aas.administration.revision, null, repo,
                    v => { aas.administration.revision = v as string; return new ModifyRepo.LambdaActionNone(); });
            }

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
                    stack, "derivedFrom", aas.derivedFrom.Keys, repo, package, "AssetAdministrationShell");
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
                helper.AddKeyListKeys(stack, "assetRef", aas.assetRef.Keys, repo, package, "Asset");
            }

            //
            // Asset linked with AAS
            //

            if (asset != null)
            {
                DisplayOrEditAasEntityAsset(
                    package, env, asset, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
        }

        //
        //
        // --- Submodel Ref
        //
        //

        public void DisplayOrEditAasEntitySubmodelOrRef(
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas,
            AdminShell.SubmodelRef smref, AdminShell.Submodel submodel, bool editMode, ModifyRepo repo,
            StackPanel stack, Brush[][] levelColors, bool hintMode = false)
        {
            // This panel renders first the SubmodelReference and then the Submodel, below
            if (smref != null)
            {
                helper.AddGroup(stack, "SubmodelReference", levelColors[0][0], levelColors[0][1]);
                helper.AddKeyListKeys(
                    stack, "submodelRef", smref.Keys, repo, package, "SubmodelRef Submodel ",
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
                   if ((int)buttonNdx == 0)
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
                        if (buttonNdx is int && (int)buttonNdx >= 0 && (int)buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                            if ((int)buttonNdx == 0)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                            if ((int)buttonNdx == 1)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.MultiLanguageProperty;
                            if ((int)buttonNdx == 2)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.SubmodelElementCollection;
                            if ((int)buttonNdx == 3)
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
                        () => { return helper.auxPackages != null;  },
                        "You have opened an auxiliary AASX package. You can copy elements from it!",
                        severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "Copy from existing SubmodelElement:",
                    new[] { "Copy single entity", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0 || (int)buttonNdx == 1)
                            {
                                var rve = helper.SmartSelectAasEntityVisualElement(
                                    package.AasEnv,
                                    "SubmodelElement",
                                    package: package,
                                    auxPackages: helper.auxPackages) as VisualElementSubmodelElement;

                                if (rve != null)
                                {
                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is AdminShell.SubmodelElement)
                                    {
                                        var clone = env.CopySubmodelElementAndCD(
                                            rve.theEnv, mdo as AdminShell.SubmodelElement,
                                            copyCD: true, shallowCopy: (int)buttonNdx == 0);

                                        if (submodel.submodelElements == null)
                                            submodel.submodelElements =
                                                new AdminShellV20.SubmodelElementWrapperCollection();
                                        submodel.submodelElements.Add(clone);
                                        return new ModifyRepo.LambdaActionRedrawAllElements(
                                            submodel, isExpanded: true);
                                    }
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
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0)
                            {
                                // ReSharper disable RedundantCast
                                helper.ImportEclassCDsForTargets(
                                    env, (smref != null) ? (object)smref : (object)submodel, targets);
                                // ReSharper enable RedundantCast
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddAction(
                    stack, "Submodel & -elements:",
                    new[] { "Turn to kind Template", "Turn to kind Instance" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (!(buttonNdx is int))
                            return new ModifyRepo.LambdaActionNone();

                        if (helper.flyoutProvider != null &&
                            MessageBoxResult.Yes != helper.flyoutProvider.MessageBoxFlyoutShow(
                                "This operation will affect all Kind attributes of " +
                                    "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                "Setting Kind",
                                MessageBoxButton.YesNo, MessageBoxImage.Warning))
                            return new ModifyRepo.LambdaActionNone();

                        submodel.kind = ((int)buttonNdx == 0)
                            ? AdminShell.ModelingKind.CreateAsTemplate()
                            : AdminShell.ModelingKind.CreateAsInstance();

                        submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                        {
                            sme.kind = ((int)buttonNdx == 0)
                                ? AdminShell.ModelingKind.CreateAsTemplate()
                                : AdminShell.ModelingKind.CreateAsInstance();
                        });

                        return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                    });

            }

            if (submodel != null)
            {

                // referable
                helper.AddGroup(stack, "Submodel", levelColors[0][0], levelColors[0][1]);
                helper.AddGroup(stack, "Referable members:", levelColors[1][0], levelColors[1][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return submodel.idShort == null || submodel.idShort.Length < 1; },
                            "idShort is mandatory for Submodels. It is a short, " +
                                "unique identifier that is unique just in its context, its name space. ",
                            breakIfTrue: true),
                        new HintCheck(
                            () =>
                            {
                                if (submodel.idShort == null) return false;
                                return !AdminShellUtil.ComplyIdShort(submodel.idShort);
                            },
                            "idShort shall only feature letters, digits, underscore ('_'); " +
                                "starting mandatory with a letter..")
                    });
                helper.AddKeyValueRef(
                    stack, "idShort", submodel, ref submodel.idShort, null, repo,
                    v =>
                    {
                        submodel.idShort = v as string;
                        return new ModifyRepo.LambdaActionNone();
                    });

                helper.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () => { return submodel.category != null && submodel.category.Length >= 1; },
                        "The use of category is unusual here.",
                        severityLevel: HintCheck.Severity.Notice));

                helper.AddKeyValueRef(
                    stack, "category", submodel, ref submodel.category, null, repo,
                    v => { submodel.category = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxItems: AdminShell.Referable.ReferableCategoryNames, comboBoxIsEditable: true);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                    new HintCheck(
                        () => {
                            return submodel.description == null || submodel.description.langString == null ||
                                submodel.description.langString.Count < 1;  },
                        "The use of an description is recommended to allow the consumer of an Submodel " +
                            "to understand the nature of it.",
                        breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return submodel.description.langString.Count < 2; },
                        "Consider having description in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
                    });
                if (helper.SafeguardAccess(
                        stack, repo, submodel.description, "description:", "Create data element!",
                        v =>
                        {
                            submodel.description = new AdminShell.Description();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddHintBubble(
                        stack, hintMode,
                        new HintCheck(() =>
                        {
                            return submodel.description.langString == null ||
                                submodel.description.langString.Count < 1;
                        },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Admin shell to understand your intentions."));
                    helper.AddKeyListLangStr(stack, "description", submodel.description.langString, repo);
                }

                // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                if (helper.SafeguardAccess(
                        stack, repo, submodel.hasDataSpecification, "HasDataSpecification:", "Create data element!",
                        v =>
                        {
                            submodel.hasDataSpecification = new AdminShell.HasDataSpecification();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "HasDataSpecification", levelColors[1][0], levelColors[1][1]);

                    if (editMode)
                    {
                        // let the user control the number of references
                        helper.AddAction(
                            stack, "Specifications:",
                            new[] { "Add Reference", "Delete last reference" },
                            repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx is int)
                                {
                                    if ((int)buttonNdx == 0)
                                        submodel.hasDataSpecification.reference.Add(new AdminShell.Reference());

                                    if ((int)buttonNdx == 1 && submodel.hasDataSpecification.reference.Count > 0)
                                        submodel.hasDataSpecification.reference.RemoveAt(
                                            submodel.hasDataSpecification.reference.Count - 1);
                                }
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            });
                    }

                    // now use the normal mechanism to deal with editMode or not ..
                    if (submodel.hasDataSpecification != null &&
                            submodel.hasDataSpecification.reference != null &&
                            submodel.hasDataSpecification.reference.Count > 0)
                    {
                        for (int i = 0; i < submodel.hasDataSpecification.reference.Count; i++)
                            helper.AddKeyListKeys(
                                stack, String.Format("reference[{0}]", i),
                                submodel.hasDataSpecification.reference[i].Keys, repo, package,
                                addExistingEntities: null /* "All" */ );
                    }
                }

                // identification
                helper.AddGroup(stack, "Identifiable members:", levelColors[1][0], levelColors[1][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return submodel.identification == null; },
                            "Providing a worldwide unique identification is mandatory.",
                            breakIfTrue: true),
                        new HintCheck(
                            () =>
                            {
                                return submodel.kind != null && submodel.kind.IsInstance &&
                                    submodel.identification.idType != AdminShell.Identification.IRI;
                            },
                            "Check if identification type is correct. " +
                                "Use of URIs is usual for instances of Submodels.",
                            severityLevel: HintCheck.Severity.Notice ),
                        new HintCheck(
                            () => { return submodel.identification.id.Trim() == ""; },
                            "Identification id shall not be empty. " +
                                "You could use the 'Generate' button in order to generate a worldwide unique id. " +
                                "The template of this id could be set by commandline arguments." )
                    });
                if (helper.SafeguardAccess(
                        stack, repo, submodel.identification, "identification:", "Create data element!",
                        v =>
                        {
                            submodel.identification = new AdminShell.Identification();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    // modifying these events will require redraw (and re-select the correct entity!)
                    ModifyRepo.LambdaAction takeOverLambda = null;
                    if (smref != null)
                        takeOverLambda = new ModifyRepo.LambdaActionRedrawAllElements(smref);
                    else
                        takeOverLambda = new ModifyRepo.LambdaActionRedrawAllElements(submodel);

                    // ask
                    helper.AddKeyValueRef(
                        stack, "idType", submodel.identification, ref submodel.identification.idType, null, repo,
                        v =>
                        {
                            submodel.identification.idType = v as string;
                            return new ModifyRepo.LambdaActionNone();
                        },
                        comboBoxItems: AdminShell.Key.IdentifierTypeNames,
                        takeOverLambdaAction: takeOverLambda);

                    helper.AddKeyValueRef(
                        stack, "id", submodel.identification, ref submodel.identification.id, null, repo,
                        v => { submodel.identification.id = v as string; return new ModifyRepo.LambdaActionNone(); },
                        auxButtonTitle: "Generate",
                        auxButtonLambda: v =>
                        {
                            submodel.identification.idType = AdminShell.Identification.IRI;
                            if (submodel.kind.kind.Trim().ToLower() == "template")
                                submodel.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelTemplate);
                            else
                                submodel.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelInstance);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: submodel);
                        }, takeOverLambdaAction: takeOverLambda);
                }

                // administration
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                    new HintCheck(
                        () => { return submodel.administration == null; },
                        "Check if providing admistrative information on version/ revision would be useful. " +
                            "This allows for better version management.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () =>
                        {
                            return submodel.administration.version.Trim() == "" ||
                                submodel.administration.revision.Trim() == "";
                        },
                        "Admistrative information fields should not be empty.",
                        severityLevel: HintCheck.Severity.Notice )
                });
                if (helper.SafeguardAccess(
                        stack, repo, submodel.administration, "administration:", "Create data element!",
                        v =>
                        {
                            submodel.administration = new AdminShell.Administration();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddKeyValueRef(
                        stack, "version", submodel.administration, ref submodel.administration.version, null, repo,
                        v =>
                        {
                            submodel.administration.version = v as string;
                            return new ModifyRepo.LambdaActionNone();
                        });
                    helper.AddKeyValueRef(
                        stack, "revision", submodel.administration, ref submodel.administration.revision, null, repo,
                        v =>
                        {
                            submodel.administration.revision = v as string;
                            return new ModifyRepo.LambdaActionNone();
                        });
                }

                // semantic Id
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return submodel.semanticId == null || submodel.semanticId.IsEmpty; },
                            "Check if you want to add a semantic reference. " +
                                "The semanticId may be either a reference to a submodel " +
                                "with kind=Type (within the same or another Administration Shell) or " +
                                "it can be an external reference to an external standard " +
                                "defining the semantics of the submodel  (for example an PDF if a standard).",
                            severityLevel: HintCheck.Severity.Notice )
                    });
                helper.AddGroup(stack, "Semantic ID", levelColors[1][0], levelColors[1][1]);
                if (helper.SafeguardAccess(
                        stack, repo, submodel.semanticId, "semanticId:", "Create data element!",
                        v =>
                        {
                            submodel.semanticId = new AdminShell.SemanticId();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                    helper.AddKeyListKeys(
                        stack, "semanticId", submodel.semanticId.Keys, repo,
                        package: package,
                        addExistingEntities: AdminShell.Key.SubmodelRef);


                // kind
                helper.AddGroup(stack, "Kind:", levelColors[1][0], levelColors[1][1]);
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return submodel.kind == null; },
                            "Providing kind information is mandatory. Typically you want to model instances. " +
                                "A manufacturer would define types of assets, as well.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return submodel.kind.IsTemplate; },
                            "Check for kind setting. 'Instance' is the usual choice, " +
                                "except if you want to declare a Submodel, which is been standardised " +
                                "by you or a standardisation body.",
                            severityLevel: HintCheck.Severity.Notice )
                    });
                if (helper.SafeguardAccess(
                    stack, repo, submodel.kind, "kind:", "Create data element!",
                    v =>
                    {
                        submodel.kind = new AdminShell.ModelingKind();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                    helper.AddKeyValueRef(
                        stack, "kind", submodel.kind, ref submodel.kind.kind, null, repo,
                        v => { submodel.kind.kind = v as string; return new ModifyRepo.LambdaActionNone(); },
                        new[] { "Template", "Instance" });

                // qualifiers are MULTIPLE structures with possible references. That is: multiple x multiple keys!
                if (helper.SafeguardAccess(
                        stack, repo, submodel.qualifiers, "Qualifiers:", "Create empty list of Qualifiers!",
                        v =>
                        {
                            submodel.qualifiers = new AdminShell.QualifierCollection();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "Qualifier", levelColors[1][0], levelColors[1][1]);
                    helper.QualifierHelper(stack, repo, submodel.qualifiers);
                }
            }
        }

        //
        //
        // --- Concept Description
        //
        //

        public void DisplayOrEditAasEntityConceptDescription(
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env,
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

            // Referable
            helper.AddGroup(stack, "Referable members:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return cd.idShort == null || cd.idShort.Length < 1; },
                        "idShort is not mandatory for concept descriptions. " +
                            "It is a short, unique identifier that is unique just in its context, its name space. " +
                            "Recommendation of the specification is to make it same as " +
                            "the short name of the concept, referred. ",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () =>
                        {
                            if (cd.idShort == null) return false;
                            return !AdminShellUtil.ComplyIdShort(cd.idShort);
                        },
                        "idShort shall only feature letters, digits, underscore ('_'); " +
                            "starting mandatory with a letter..")
                });

            helper.AddKeyValueRef(
                stack, "idShort", cd, ref cd.idShort, null, repo,
                v => { cd.idShort = v as string; return new ModifyRepo.LambdaActionNone(); },
                auxButtonTitle: "Sync",
                auxButtonToolTip: "Copy (if target is empty) idShort to shortName and SubmodelElement idShort.",
                auxButtonLambda: (v) =>
                {
                    ModifyRepo.LambdaAction la = new ModifyRepo.LambdaActionNone();

                    var ds = cd.embeddedDataSpecification?.dataSpecificationContent?.dataSpecificationIEC61360;
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
                });

            helper.AddHintBubble(
                stack, hintMode,
                new HintCheck(
                    () => { return cd.category != null && cd.category.Length >= 1; },
                    "The use of category is unusual here.",
                    severityLevel: HintCheck.Severity.Notice));
            helper.AddKeyValueRef(
                stack, "category", cd, ref cd.category, null, repo,
                v => { cd.category = v as string; return new ModifyRepo.LambdaActionNone(); },
                comboBoxItems: AdminShell.Referable.ReferableCategoryNames, comboBoxIsEditable: true);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return cd.description == null || cd.description.langString == null ||
                                cd.description.langString.Count < 1;
                        },
                        "The use of an description is recommended to allow the consumer " +
                            "of an ConceptDescription to understand the nature of it.",
                        breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return cd.description.langString.Count < 2; },
                        "Consider having description in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (helper.SafeguardAccess(
                    stack, repo, cd.description, "description:", "Create data element!",
                    v =>
                    {
                        cd.description = new AdminShell.Description();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () => { return cd.description.langString == null || cd.description.langString.Count < 1; },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Administration shell to understand your intentions."));
                helper.AddKeyListLangStr(stack, "description", cd.description.langString, repo);
            }

            helper.AddGroup(stack, "Identifiable members:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return cd.identification == null; },
                        "Providing a worldwide unique identification is mandatory. " +
                            "If the concept description is a copy from an external dictionary like eCl@ss " +
                            "it may use the same global id as it is used in the external dictionary.  ",
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return cd.identification.id.Trim() == ""; },
                        "Identification id shall not be empty. " +
                            "You could use the 'Generate' button in order to generate a worldwide unique id. " +
                            "The template of this id could be set by commandline arguments." )

            });
            if (helper.SafeguardAccess(stack, repo, cd.identification, "identification:", "Create data element!", v =>
            {
                cd.identification = new AdminShell.Identification();
                return new ModifyRepo.LambdaActionRedrawEntity();
            }))
            {
                helper.AddKeyValueRef(
                    stack, "idType", cd.identification, ref cd.identification.idType, null, repo,
                    v => { cd.identification.idType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxItems: AdminShell.Key.IdentifierTypeNames,
                    auxButtonTitles: new[] { "Generate", "Rename" },
                    auxButtonLambda: (i) =>
                    {
                        if (i is int && (int)i == 0)
                        {
                            cd.identification.idType = AdminShell.Identification.IRI;
                            cd.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                               Options.Curr.TemplateIdConceptDescription);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: cd);
                        }
                        if (i is int && (int)i == 1)
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
                    });
                helper.AddKeyValueRef(
                    stack, "id", cd.identification, ref cd.identification.id, null, repo,
                    v => { cd.identification.id = v as string; return new ModifyRepo.LambdaActionNone(); }
                // dead-csharp off
                /* , auxButtonTitle: "Generate", auxButtonLambda: v => {
                    cd.identification.idType = AdminShell.Identification.IRI;
                    cd.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                        Options.Curr.TemplateIdConceptDescription);
                    return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: cd);
                } */);
                // dead-csharp on
            }

            helper.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return cd.administration == null; },
                    "Check if providing admistrative information on version/ revision would be useful. " +
                        "This allows for better version management.",
                    breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () => {
                        return cd.administration.version.Trim() == "" || cd.administration.revision.Trim() == "";
                    },
                    "Admistrative information fields should not be empty.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (helper.SafeguardAccess(
                    stack, repo, cd.administration, "administration:", "Create data element!",
                    v =>
                    {
                        cd.administration = new AdminShell.Administration();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                helper.AddKeyValueRef(
                    stack, "version", cd.administration, ref cd.administration.version, null, repo,
                    v => { cd.administration.version = v as string; return new ModifyRepo.LambdaActionNone(); });

                helper.AddKeyValueRef(
                    stack, "revision", cd.administration, ref cd.administration.revision, null, repo,
                    v => { cd.administration.revision = v as string; return new ModifyRepo.LambdaActionNone(); });
            }

            // isCaseOf are MULTIPLE references. That is: multiple x multiple keys!
            if (helper.SafeguardAccess(
                stack, repo, cd.IsCaseOf, "isCaseOf:", "Create data element!",
                v =>
                {
                    cd.IsCaseOf = new List<AdminShell.Reference>();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
            {
                helper.AddGroup(stack, "IsCaseOf", levelColors[1][0], levelColors[1][1]);

                if (editMode)
                {
                    // let the user control the number of references
                    helper.AddAction(
                        stack, "IsCaseOf:", new[] { "Add Reference", "Delete last reference" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx is int)
                            {
                                if ((int)buttonNdx == 0)
                                    cd.IsCaseOf.Add(new AdminShell.Reference());

                                if ((int)buttonNdx == 1 && cd.IsCaseOf.Count > 0)
                                    cd.IsCaseOf.RemoveAt(cd.IsCaseOf.Count - 1);
                            }
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (cd.IsCaseOf != null && cd.IsCaseOf.Count > 0)
                {
                    for (int i = 0; i < cd.IsCaseOf.Count; i++)
                        helper.AddKeyListKeys(
                            stack, String.Format("reference[{0}]", i), cd.IsCaseOf[i].Keys, repo, package,
                            AdminShell.Key.AllElements,
                            addEclassIrdi: true);
                }
            }

            // dead-csharp off
            /* OLD
            if (helper.SafeguardAccess(stack, repo, cd.conceptDefinitionRef, "conceptDefinitionRef:",
                "Create data element!", v =>
            {
                cd.conceptDefinitionRef = new AdminShell.Reference();
                return new ModifyRepo.LambdaActionRedrawEntity();
            }))
            {
                helper.AddGroup(stack, "Concept Definition Reference", levelColors[1][0], levelColors[1][1]);
                helper.AddKeyListKeys(stack, "reference", cd.conceptDefinitionRef.Keys, repo, package, "All");
            }
            */
            // dead-csharp on

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return cd.embeddedDataSpecification == null; },
                        "Providing embeddedDataSpecification is mandatory. This holds the descriptive information " +
                            "of an concept and allows for an off-line understanding of the meaning " +
                            "of an concept/ SubmodelElement. Please create this data element.",
                        breakIfTrue: true),
                });
            if (helper.SafeguardAccess(
                    stack, repo, cd.embeddedDataSpecification, "embeddedDataSpecification:", "Create data element!",
                    v =>
                    {
                        cd.embeddedDataSpecification = new AdminShell.EmbeddedDataSpecification();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                // has data spec
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return cd.embeddedDataSpecification.dataSpecification == null; },
                            "Providing hasDataSpecification is mandatory. " +
                                "This holds the external global reference to the specification, " +
                                "which defines the data template, which attributes are featured within " +
                                "the ConceptDescription. Typically, it refers to " +
                                "www.admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360.")
                    });
                if (helper.SafeguardAccess(
                        stack, repo, cd.embeddedDataSpecification.dataSpecification, "hasDataSpecification:",
                        "Create data element!",
                        v =>
                        {
                            cd.embeddedDataSpecification.dataSpecification = new AdminShell.DataSpecificationRef();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "HasDataSpecification", levelColors[1][0], levelColors[1][1]);

                    helper.AddHintBubble(
                        stack, hintMode,
                        new[] {
                            new HintCheck(
                                () =>
                                {
                                    return cd.embeddedDataSpecification.dataSpecification == null ||
                                        cd.embeddedDataSpecification.dataSpecification.Count != 1 ||
                                        cd.embeddedDataSpecification.dataSpecification[0].type !=
                                            AdminShell.Key.GlobalReference;
                                },
                                "hasDataSpecification holds the external global reference to the specification, " +
                                    "which defines the data template, which attributes are featured " +
                                    "within the ConceptDescription. " +
                                    "Typically, it refers to " +
                                    "www.admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360.",
                                severityLevel: HintCheck.Severity.Notice),
                        });
                    helper.AddKeyListKeys(
                        stack, "hasDataSpecification", cd.embeddedDataSpecification.dataSpecification.Keys,
                        repo, package, addExistingEntities: null /* "All" */,
                        addPresetNames: new[] { "IEC61360" },
                        addPresetKeys: new[] {
                            AdminShell.Key.CreateNew(
                                AdminShell.Key.GlobalReference, false, AdminShell.Identification.IRI,
                                "www.admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360")
                        });
                }

                // data spec content
                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return cd.embeddedDataSpecification.dataSpecificationContent == null; },
                            "Providing dataSpecificationContent is mandatory. " +
                                "This holds the attributes describing the concept. Please create this data element.")
                });
                if (helper.SafeguardAccess(
                        stack, repo, cd.embeddedDataSpecification.dataSpecificationContent,
                        "dataSpecificationContent:", "Create data element!",
                        v =>
                        {
                            cd.embeddedDataSpecification.dataSpecificationContent =
                                new AdminShell.DataSpecificationContent();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "DataSpecificationContent", levelColors[1][0], levelColors[1][1]);

                    // 61360?
                    helper.AddHintBubble(
                        stack, hintMode,
                        new[] {
                            new HintCheck(
                                () => {
                                    return cd
                                        .embeddedDataSpecification
                                        .dataSpecificationContent
                                        .dataSpecificationIEC61360 == null;
                                },
                                "As of January 2019, there is only a data specification for IEC 61360. " +
                                    "Please create this data element.")
                        });
                    if (helper.SafeguardAccess(
                            stack, repo,
                            cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360,
                            "dataSpecificationIEC61360:", "Create data element!",
                            v =>
                            {
                                cd
                                    .embeddedDataSpecification
                                    .dataSpecificationContent
                                    .dataSpecificationIEC61360 = new AdminShell.DataSpecificationIEC61360();
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            }))
                    {
                        var dsiec = cd.embeddedDataSpecification.dataSpecificationContent.dataSpecificationIEC61360;
                        helper.AddGroup(
                            stack, "Data Specification Content IEC61360", levelColors[1][0], levelColors[1][1]);

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () => { return dsiec.preferredName == null || dsiec.preferredName.Count < 1; },
                                    "Please add a preferred name, which could be used on user interfaces " +
                                        "to identify the concept to a human person.",
                                    breakIfTrue: true),
                                new HintCheck(
                                    () => { return dsiec.preferredName.Count <2; },
                                    "Please add multiple languanges.",
                                    severityLevel: HintCheck.Severity.Notice)
                            });
                        if (helper.SafeguardAccess(
                                stack, repo, dsiec.preferredName, "preferredName:", "Create data element!",
                                v =>
                                {
                                    dsiec.preferredName = new AdminShell.LangStringSetIEC61360();
                                    return new ModifyRepo.LambdaActionRedrawEntity();
                                }))
                            helper.AddKeyListLangStr(stack, "preferredName", dsiec.preferredName.langString, repo);

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () => { return dsiec.shortName == null || dsiec.shortName.Count < 1; },
                                    "Please add a short name, which is a reduced, even symbolic version of " +
                                        "the preferred name. IEC 61360 defines some symbolic rules " +
                                        "(e.g. greek characters) for this name.",
                                    breakIfTrue: true),
                                new HintCheck(
                                    () => { return dsiec.shortName.Count <2; },
                                    "Please add multiple languanges.",
                                    severityLevel: HintCheck.Severity.Notice)
                            });
                        if (helper.SafeguardAccess(
                                stack, repo, dsiec.shortName, "shortName:", "Create data element!",
                                v =>
                                {
                                    dsiec.shortName = new AdminShell.LangStringSetIEC61360();
                                    return new ModifyRepo.LambdaActionRedrawEntity();
                                }))
                            helper.AddKeyListLangStr(stack, "shortName", dsiec.shortName.langString, repo);

                        // dead-csharp off
                        // TODO (Michael Hoffmeister, 1970-01-01): add Sync to shortName
                        /*
                        helper.AddHintBubble(stack, hintMode, new [] {
                            new HintCheck( () => { return dsiec.shortName == null || dsiec.shortName.Count < 1; },
                                "Please provide a shortName, which is a reduced, even symbolic version of the " +
                                "preferred name. IEC 61360 defines some symbolic rules (e.g. greek characters) for " +
                                "this name.")
                        });
                        helper.AddKeyValue(stack, "shortName", dsiec.shortName, null, repo,
                            v => { dsiec.shortName = v as string; return new ModifyRepo.LambdaActionNone(); },
                            auxButtonTitle: "Sync",
                            auxButtonToolTip: "Copy (if target is empty) idShort to idShort and SubmodelElement " +
                                "idShort.",
                            auxButtonLambda: (v) =>
                            {
                                ModifyRepo.LambdaAction la = new ModifyRepo.LambdaActionNone();

                                if (cd.idShort == null || cd.idShort.Trim() == "")
                                {
                                    cd.idShort = dsiec.shortName;
                                    la = new ModifyRepo.LambdaActionRedrawEntity();
                                }

                                if (parentContainer != null & parentContainer is AdminShell.SubmodelElement)
                                {
                                    var sme = parentContainer as AdminShell.SubmodelElement;
                                    if (sme.idShort == null || sme.idShort.Trim() == "")
                                    {
                                        sme.idShort = dsiec.shortName;
                                        la = new ModifyRepo.LambdaActionRedrawEntity();
                                    }
                                }
                                return la;
                            });
                        */
                        // dead-csharp on

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () => {
                                        return (dsiec.unitId == null || dsiec.unitId.Count < 1) &&
                                            ( dsiec.unit == null || dsiec.unit.Trim().Length < 1);
                                    },
                                    "Please check, if you can provide a unit or a unitId, " +
                                        "in which the concept is being measured. " +
                                        "Usage of SI-based units is encouraged.")
                        });
                        helper.AddKeyValueRef(
                            stack, "unit", dsiec, ref dsiec.unit, null, repo,
                            v => { dsiec.unit = v as string; return new ModifyRepo.LambdaActionNone(); });

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () => {
                                        return ( dsiec.unit == null || dsiec.unit.Trim().Length < 1) &&
                                            ( dsiec.unitId == null || dsiec.unitId.Count < 1);
                                    },
                                    "Please check, if you can provide a unit or a unitId, " +
                                        "in which the concept is being measured. " +
                                        "Usage of SI-based units is encouraged.")
                            });
                        if (helper.SafeguardAccess(
                                stack, repo, dsiec.unitId, "unitId:", "Create data element!",
                                v =>
                                {
                                    dsiec.unitId = new AdminShell.UnitId();
                                    return new ModifyRepo.LambdaActionRedrawEntity();
                                }))
                        {
                            // dead-csharp off
                            // helper.AddGroup(stack, "UnitID", levelColors[1][0], levelColors[1][1]);
                            // dead-csharp on
                            helper.AddKeyListKeys(
                                stack, "unitId", dsiec.unitId.Keys, repo, package,
                                AdminShell.Key.GlobalReference, addEclassIrdi: true);
                        }

                        helper.AddKeyValueRef(
                            stack, "valueFormat", dsiec, ref dsiec.valueFormat, null, repo,
                            v => { dsiec.valueFormat = v as string; return new ModifyRepo.LambdaActionNone(); });

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () =>
                                    {
                                        return dsiec.sourceOfDefinition == null || dsiec.sourceOfDefinition.Length < 1;
                                    },
                                    "Please check, if you can provide a source of definition for the concepts. " +
                                        "This could be an informal link to a document, glossary item etc.")
                            });
                        helper.AddKeyValueRef(
                            stack, "sourceOfDefinition", dsiec, ref dsiec.sourceOfDefinition, null, repo,
                            v =>
                            {
                                dsiec.sourceOfDefinition = v as string;
                                return new ModifyRepo.LambdaActionNone();
                            });

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () => { return dsiec.symbol == null || dsiec.symbol.Trim().Length < 1; },
                                    "Please check, if you can provide formulaic character for the concept.",
                                    severityLevel: HintCheck.Severity.Notice)
                            });
                        helper.AddKeyValueRef(
                            stack, "symbol", dsiec, ref dsiec.symbol, null, repo,
                            v => { dsiec.symbol = v as string; return new ModifyRepo.LambdaActionNone(); });

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () => { return dsiec.dataType == null || dsiec.dataType.Trim().Length < 1; },
                                    "Please check, if you can provide data type for the concept. " +
                                        "Data types are provided by the IEC 61360.",
                                    severityLevel: HintCheck.Severity.Notice)
                            });
                        helper.AddKeyValueRef(
                            stack, "dataType", dsiec, ref dsiec.dataType, null, repo,
                            v => { dsiec.dataType = v as string; return new ModifyRepo.LambdaActionNone(); },
                            comboBoxIsEditable: true,
                            comboBoxItems: AdminShell.DataSpecificationIEC61360.DataTypeNames);

                        helper.AddHintBubble(
                            stack, hintMode,
                            new[] {
                                new HintCheck(
                                    () => { return dsiec.definition == null || dsiec.definition.Count < 1; },
                                    "Please add a definition, which could be used to describe exactly, " +
                                        "how to establish a value/ measurement for the concept.",
                                    breakIfTrue: true),
                                new HintCheck(
                                    () => { return dsiec.definition.Count <2; },
                                    "Please add multiple languanges.",
                                    severityLevel: HintCheck.Severity.Notice)
                            });
                        if (helper.SafeguardAccess(
                                stack, repo, dsiec.definition, "definition:", "Create data element!",
                                v =>
                                {
                                    dsiec.definition = new AdminShell.LangStringSetIEC61360();
                                    return new ModifyRepo.LambdaActionRedrawEntity();
                                }))
                            helper.AddKeyListLangStr(stack, "definition", dsiec.definition.langString, repo);
                    }

                }

            }
        }

        //
        //
        // --- Operation Variable
        //
        //

        public void DisplayOrEditAasEntityOperationVariable(
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env,
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
                                if (buttonNdx is int && (int)buttonNdx >= 0 && (int)buttonNdx <= 3)
                                {
                                    // which adequate type?
                                    var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                                    if ((int)buttonNdx == 0)
                                        en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                                    if ((int)buttonNdx == 1)
                                        en = AdminShell
                                            .SubmodelElementWrapper
                                            .AdequateElementEnum
                                            .MultiLanguageProperty;
                                    if ((int)buttonNdx == 2)
                                        en = AdminShell
                                            .SubmodelElementWrapper
                                            .AdequateElementEnum
                                            .SubmodelElementCollection;
                                    if ((int)buttonNdx == 3)
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
                           if ((buttonNdx is int) && (int)buttonNdx == 0)
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
                                () => { return helper.auxPackages != null;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                        helper.AddAction(
                            stack, "Copy from existing SubmodelElement:",
                            new[] { "Copy single", "Copy recursively" }, repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx is int)
                                {
                                    if ((int)buttonNdx == 0 || (int)buttonNdx == 1)
                                    {
                                        var rve = helper.SmartSelectAasEntityVisualElement(
                                            package.AasEnv, "SubmodelElement",
                                            package: package,
                                            auxPackages: helper.auxPackages) as VisualElementSubmodelElement;

                                        if (rve != null)
                                        {
                                            var mdo = rve.GetMainDataObject();
                                            if (mdo != null && mdo is AdminShell.SubmodelElement)
                                            {
                                                var clone = env.CopySubmodelElementAndCD(
                                                    rve.theEnv, mdo as AdminShell.SubmodelElement,
                                                    copyCD: true,
                                                    shallowCopy: (int)buttonNdx == 0);

                                                ov.value = clone;
                                                return new ModifyRepo.LambdaActionRedrawEntity();
                                            }
                                        }
                                    }
                                }
                                return new ModifyRepo.LambdaActionNone();
                            });
                    }

                    // value == SubmodelElement is displayed
                    helper.AddGroup(
                        stack, "OperationVariable value (is a SubmodelElement)", levelColors[1][0], levelColors[1][1]);
                    var substack = helper.AddSubStackPanel(stack, "     "); // just a bit spacing to the left
                    // huh, recursion in a lambda based GUI feedback function??!!
                    if (ov.value != null && ov.value.submodelElement != null) // avoid at least direct recursions!
                        DisplayOrEditAasEntitySubmodelElement(
                            package, env, parentContainer, ov.value, null, editMode, repo,
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
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env,
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

                // refactor?
                if (parentContainer != null && parentContainer is AdminShell.IManageSubmodelElements)
                    helper.AddAction(
                        horizStack, "Refactoring:",
                        new[] { "Refactor" }, repo,
                        (buttonNdx) =>
                        {
                            if ((int)buttonNdx == 0)
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
                helper.AddAction(
                    stack, "Buffer:",
                    new[] { "Cut", "Copy", "Paste above", "Paste below", "Paste into" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0 || (int)buttonNdx == 1)
                            {
                                // store info
                                var cpb = new CopyPasteBuffer();
                                cpb.duplicate = (int)buttonNdx == 1;
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

                            if ((int)buttonNdx == 2 || (int)buttonNdx == 3 || (int)buttonNdx == 4)
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
                                if ((int)buttonNdx == 2)
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

                                    // TODO (Michael Hoffmeister, 1970-01-01): Operation mssing here?
                                }
                                if ((int)buttonNdx == 3)
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

                                    // TODO (Michael Hoffmeister, 1970-01-01): Operation mssing here?
                                }
                                if ((int)buttonNdx == 4)
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
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });
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
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0)
                            {
                                // select existing CD
                                var ks = helper.SmartSelectAasEntityKeys(package.AasEnv);
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

                            if ((int)buttonNdx == 1)
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

                            if ((int)buttonNdx == 2)
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
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0)
                            {
                                helper.ImportEclassCDsForTargets(env, sme, targets);
                            }
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
                        if (buttonNdx is int && (int)buttonNdx >= 0 && (int)buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                            if ((int)buttonNdx == 0)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                            if ((int)buttonNdx == 1)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.MultiLanguageProperty;
                            if ((int)buttonNdx == 2)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.SubmodelElementCollection;
                            if ((int)buttonNdx == 3)
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
                            () => { return helper.auxPackages != null;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                });
                helper.AddAction(
                    stack, "Copy from existing SubmodelElement:", new[] { "Copy single", "Copy recursively" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0 || (int)buttonNdx == 1)
                            {
                                var rve = helper.SmartSelectAasEntityVisualElement(
                                    package.AasEnv, "SubmodelElement", package: package,
                                    auxPackages: helper.auxPackages) as VisualElementSubmodelElement;

                                if (rve != null)
                                {
                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is AdminShell.SubmodelElement)
                                    {
                                        var clone = env.CopySubmodelElementAndCD(
                                            rve.theEnv, mdo as AdminShell.SubmodelElement, copyCD: true,
                                            shallowCopy: (int)buttonNdx == 0);
                                        // dead-csharp off
                                        /*
                                         * TO BE DELETED, if SMWC works..
                                        if (listOfSMEW == null)
                                        {
                                            listOfSMEW = new List<AdminShell.SubmodelElementWrapper>();
                                            if (sme is AdminShell.SubmodelElementCollection)
                                                (sme as AdminShell.SubmodelElementCollection).value = listOfSMEW;
                                            if (sme is AdminShell.Entity)
                                                (sme as AdminShell.Entity).statements = listOfSMEW;
                                        }
                                        listOfSMEW.Add(clone);
                                        */
                                        // dead-csharp on
                                        if (sme is AdminShell.SubmodelElementCollection smesmc)
                                            smesmc.value.Add(clone);
                                        if (sme is AdminShell.Entity smeent)
                                            smeent.statements.Add(clone);
                                        return new ModifyRepo.LambdaActionRedrawAllElements(
                                            nextFocus: sme, isExpanded: true);
                                    }
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
                   if ((buttonNdx is int) && (int)buttonNdx == 0)
                   {
                       return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: jumpToCD, isExpanded: true);
                   }
                   return new ModifyRepo.LambdaActionNone();
               });
            }

            if (editMode && sme is AdminShell.Operation smo)
            {
                helper.AddGroup(stack, "Editing of sub-ordinate entities", levelColors[0][0], levelColors[0][1]);

                var substack = helper.AddSubStackPanel(stack, "     "); // just a bit spacing to the left

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
                            if (buttonNdx is int && (int)buttonNdx == 0)
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
                                () => { return helper.auxPackages != null;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                    helper.AddAction(
                        substack, "Copy from existing OperationVariable:", new[] { "Copy single", "Copy recursively" },
                        repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx is int)
                            {
                                if ((int)buttonNdx == 0 || (int)buttonNdx == 1)
                                {
                                    var rve = helper.SmartSelectAasEntityVisualElement(
                                     package.AasEnv, "OperationVariable", package: package,
                                     auxPackages: helper.auxPackages) as VisualElementOperationVariable;

                                    if (rve != null)
                                    {
                                        var mdo = rve.GetMainDataObject();
                                        if (mdo != null && mdo is AdminShell.OperationVariable)
                                        {
                                            var clone = new AdminShell.OperationVariable(
                                                mdo as AdminShell.OperationVariable, shallowCopy: (int)buttonNdx == 0);

                                            if (smo[dir] == null)
                                                smo[dir] = new List<AdminShell.OperationVariable>();

                                            smo[dir].Add(clone);
                                            return new ModifyRepo.LambdaActionRedrawAllElements(
                                                nextFocus: smo, isExpanded: true);
                                        }
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

                var substack = helper.AddSubStackPanel(stack, "     "); // just a bit spacing to the left

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
                        if (buttonNdx is int && (int)buttonNdx >= 0 && (int)buttonNdx <= 3)
                        {
                            // which adequate type?
                            var en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Unknown;
                            if ((int)buttonNdx == 0)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Property;
                            if ((int)buttonNdx == 1)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.MultiLanguageProperty;
                            if ((int)buttonNdx == 2)
                                en = AdminShell.SubmodelElementWrapper.AdequateElementEnum.Range;
                            if ((int)buttonNdx == 3)
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
                            () => { return helper.auxPackages != null;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddAction(
                    substack, "Copy from existing DataElement:", new[] { "Copy single" }, repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0)
                            {
                                var rve = helper.SmartSelectAasEntityVisualElement(
                                    package.AasEnv, "SubmodelElement", package: package,
                                    auxPackages: helper.auxPackages) as VisualElementSubmodelElement;

                                if (rve != null)
                                {
                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is AdminShell.DataElement)
                                    {
                                        var clonesmw = new AdminShell.SubmodelElementWrapper(
                                            mdo as AdminShell.DataElement, shallowCopy: true);

                                        if (are.annotations == null)
                                            are.annotations = new AdminShell.DataElementWrapperCollection();

                                        are.annotations.Add(clonesmw);

                                        return new ModifyRepo.LambdaActionRedrawAllElements(
                                            nextFocus: clonesmw.submodelElement, isExpanded: true);
                                    }
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

                helper.AddGroup(stack, "Referable members:", levelColors[1][0], levelColors[1][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return sme.idShort == null || sme.idShort.Length < 1; },
                            "idShort is mandatory for SubmodelElements. " +
                                "It is a short, unique identifier that is unique just in its context, " +
                                "its name space. It is not required to be unique " +
                                "over multiple SubmodelElementCollections.",
                            breakIfTrue: true),
                        new HintCheck(
                            () =>
                            {
                                if (sme.idShort == null) return false;
                                return !AdminShellUtil.ComplyIdShort(sme.idShort);
                            },
                            "idShort shall only feature letters, digits, underscore ('_'); " +
                                "starting mandatory with a letter..")
                    });
                helper.AddKeyValueRef(
                    stack, "idShort", sme, ref sme.idShort, null, repo,
                    v => { sme.idShort = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitle: "Sync",
                    auxButtonToolTip: "Copy (if target is empty) idShort " +
                        "to concept desctiption idShort and shortName.",
                    auxButtonLambda: (v) =>
                    {
                        if (sme.semanticId != null && sme.semanticId.Count > 0)
                        {
                            var cd = env.FindConceptDescription(sme.semanticId.Keys);
                            if (cd != null)
                            {
                                if (cd.idShort == null || cd.idShort.Trim() == "")
                                    cd.idShort = sme.idShort;

                                var ds = cd
                                    .embeddedDataSpecification?
                                    .dataSpecificationContent?
                                    .dataSpecificationIEC61360;

                                if (ds != null && (ds.shortName == null || ds.shortName.Count < 1))
                                    ds.shortName = new AdminShellV20.LangStringSetIEC61360("EN?", sme.idShort);
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
                helper.AddKeyValueRef(
                    stack, "category", sme, ref sme.category, null, repo,
                    v => { sme.category = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxItems: AdminShell.Referable.ReferableCategoryNames,
                    comboBoxIsEditable: true);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return sme.description == null || sme.description.langString == null ||
                                    sme.description.langString.Count < 1;
                            },
                            "The use of an description is recommended to allow " +
                                "the consumer of an SubmodelElement to understand the nature of it.",
                            breakIfTrue: true,
                            severityLevel: HintCheck.Severity.Notice),
                        new HintCheck(
                            () => { return sme.description.langString.Count < 2; },
                            "Consider having description in multiple langauges.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                if (helper.SafeguardAccess(
                        stack, repo, sme.description, "description:", "Create data element!",
                        v =>
                        {
                            sme.description = new AdminShell.Description();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                    helper.AddKeyListLangStr(stack, "description", sme.description.langString, repo);

                // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                if (helper.SafeguardAccess(
                        stack, repo, sme.hasDataSpecification, "HasDataSpecification:", "Create data element!",
                        v =>
                        {
                            sme.hasDataSpecification = new AdminShell.HasDataSpecification();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "HasDataSpecification", levelColors[1][0], levelColors[1][1]);

                    if (editMode)
                    {
                        // let the user control the number of references
                        helper.AddAction(
                            stack, "Specifications:", new[] { "Add Reference", "Delete last reference" }, repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx is int)
                                {
                                    if ((int)buttonNdx == 0)
                                        sme.hasDataSpecification.reference.Add(new AdminShell.Reference());

                                    if ((int)buttonNdx == 1 && sme.hasDataSpecification.reference.Count > 0)
                                        sme.hasDataSpecification.reference.RemoveAt(
                                            sme.hasDataSpecification.reference.Count - 1);
                                }
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            });
                    }

                    // now use the normal mechanism to deal with editMode or not ..
                    if (sme.hasDataSpecification != null && sme.hasDataSpecification.reference != null &&
                        sme.hasDataSpecification.reference.Count > 0)
                    {
                        for (int i = 0; i < sme.hasDataSpecification.reference.Count; i++)
                            helper.AddKeyListKeys(
                                stack, String.Format("reference[{0}]", i),
                                sme.hasDataSpecification.reference[i].Keys,
                                repo, package, addExistingEntities: null /* "All" */ );
                    }
                }

                helper.AddGroup(stack, "Kind:", levelColors[1][0], levelColors[1][1]);

                helper.AddHintBubble(
                    stack, hintMode, new[] {
                        new HintCheck(
                            () => { return sme.kind == null; },
                            "Providing kind information is mandatory. Typically you want to model instances. " +
                                "A manufacturer would define types of assets, as well.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return !sme.kind.IsInstance; },
                            "Please check for kind setting. 'Instance' is the usual choice.",
                            severityLevel: HintCheck.Severity.Notice )
                    });
                if (helper.SafeguardAccess(
                        stack, repo, sme.kind, "kind:", "Create data element!",
                        v =>
                        {
                            sme.kind = new AdminShell.ModelingKind();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                    helper.AddKeyValueRef(
                        stack, "kind", sme.kind, ref sme.kind.kind, null, repo,
                        v => { sme.kind.kind = v as string; return new ModifyRepo.LambdaActionNone(); },
                        new[] { "Template", "Instance" });

                helper.AddGroup(stack, "Semantic ID", levelColors[1][0], levelColors[1][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return sme.semanticId == null; },
                            "The use of semanticId for SubmodelElements is mandatory! " +
                                "Only by this means, an automatic system can identify and " +
                                "understand the meaning of the SubmodelElements and, for example, " +
                                "its unit or logical datatype. " +
                                "The semanticId shall reference to a ConceptDescription within the AAS environment " +
                                "or an external repository, such as IEC CDD or eCl@ss or " +
                                "a company / consortia repository.")
                    });
                if (helper.SafeguardAccess(
                        stack, repo, sme.semanticId, "semanticId:", "Create data element!",
                        v =>
                        {
                            sme.semanticId = new AdminShell.SemanticId();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                // dead-csharp off
                /* OZ
                {
                    sme.semanticId = new AdminShell.SemanticId();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
                */
                // dead-csharp on
                {
                    helper.AddHintBubble(
                        stack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return sme.semanticId.IsEmpty; },
                                "The use of semanticId for SubmodelElements is mandatory! " +
                                    "Only by this means, an automatic system can identify and " +
                                    "understand the meaning of the SubmodelElements and, for example, " +
                                    "its unit or logical datatype. The semanticId shall reference " +
                                    "to a ConceptDescription within the AAS environment or an external repository, " +
                                    "such as IEC CDD or eCl@ss or a company / consortia repository.",
                                breakIfTrue: true),
                            new HintCheck(
                                () => { return sme.semanticId[0].type != AdminShell.Key.ConceptDescription; },
                                "The semanticId usually refers to a ConceptDescription " +
                                    "within the respective repository.",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                    helper.AddKeyListKeys(
                        stack, "semanticId", sme.semanticId.Keys, repo, package: package,
                        addExistingEntities: AdminShell.Key.ConceptDescription, addEclassIrdi: true);
                }

                // qualifiers are MULTIPLE structures with possible references. That is: multiple x multiple keys!
                if (helper.SafeguardAccess(
                        stack, repo, sme.qualifiers, "Qualifiers:", "Create empty list of Qualifiers!",
                        v =>
                        {
                            sme.qualifiers = new AdminShellV20.QualifierCollection();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddGroup(stack, "Qualifier", levelColors[1][0], levelColors[1][1]);
                    helper.QualifierHelper(stack, repo, sme.qualifiers);
                }

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
                            package, env, sme, cd, editMode, repo, stack, levelColors,
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
                            severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddKeyValueRef(
                    stack, "value", p, ref p.value, null, repo,
                    v => { p.value = v as string; return new ModifyRepo.LambdaActionNone(); });

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
                        stack, "valueId", p.valueId.Keys, repo, package, AdminShell.Key.GlobalReference);
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
                        stack, "valueId", mlp.valueId.Keys, repo, package, AdminShell.Key.GlobalReference);
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
            else if (sme is AdminShell.File)
            {
                var p = sme as AdminShell.File;
                helper.AddGroup(stack, "File", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return p.mimeType == null || p.mimeType.Trim().Length < 1 ||
                                    p.mimeType.IndexOf('/') < 1 || p.mimeType.EndsWith("/");
                            },
                            "The mime-type of the file. Mandatory information. See RFC2046.")
                    });
                helper.AddKeyValueRef(
                    stack, "mimeType", p, ref p.mimeType, null, repo,
                    v => { p.mimeType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.File.GetPopularMimeTypes());

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p.value == null || p.value.Trim().Length < 1; },
                            "The path to an external file or a file relative the AASX package root('/'). " +
                                "Files are typically relative to '/aasx/' or sub-directories of it. " +
                                "External files typically comply to an URL, e.g. starting with 'https://..'.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return p.value.IndexOf('\\') >= 0; },
                            "Backslashes ('\') are not allow. Please use '/' as path delimiter.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                helper.AddKeyValueRef(
                    stack, "value", p, ref p.value, null, repo,
                    v => { p.value = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitle: "Choose supplementary file",
                    auxButtonLambda: (o) =>
                    {
                        var ve = helper.SmartSelectAasEntityVisualElement(package.AasEnv, "File", package: package);
                        if (ve != null)
                        {
                            var sf = (ve.GetMainDataObject()) as AdminShellPackageSupplementaryFile;
                            if (sf != null)
                            {
                                p.value = sf.uri.ToString();
                                return new ModifyRepo.LambdaActionRedrawEntity();
                            }
                        }
                        return new ModifyRepo.LambdaActionNone();
                    });
            }
            else if (sme is AdminShell.Blob)
            {
                var p = sme as AdminShell.Blob;
                helper.AddGroup(stack, "Blob", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return p.mimeType == null || p.mimeType.Trim().Length < 1 ||
                                    p.mimeType.IndexOf('/') < 1 || p.mimeType.EndsWith("/");
                            },
                            "The mime-type of the file. Mandatory information. See RFC2046.")
                    });
                helper.AddKeyValueRef(
                    stack, "mimeType", p, ref p.mimeType, null, repo,
                    v => { p.mimeType = v as string; return new ModifyRepo.LambdaActionNone(); },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShell.File.GetPopularMimeTypes());

                helper.AddKeyValueRef(
                    stack, "value", p, ref p.value, null, repo,
                    v => { p.value = v as string; return new ModifyRepo.LambdaActionNone(); });
            }
            else if (sme is AdminShell.ReferenceElement)
            {
                var p = sme as AdminShell.ReferenceElement;
                helper.AddGroup(stack, "ReferenceElement", levelColors[0][0], levelColors[0][1]);

                helper.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p.value == null || p.value.IsEmpty; },
                            "Please choose the target of the reference. " +
                                "You refer to any Referable, if local within the AAS environment or outside. " +
                                "The semantics of your reference shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (helper.SafeguardAccess(
                        stack, repo, p.value, "Target reference:", "Create data element!",
                        v =>
                        {
                            p.value = new AdminShell.Reference();
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        }))
                {
                    helper.AddKeyListKeys(stack, "value", p.value.Keys, repo, thePackage, AdminShell.Key.AllElements);
                }
            }
            else
            if (sme is AdminShell.RelationshipElement rele)
            {
                helper.AddGroup(stack, "" + sme.GetElementName(), levelColors[0][0], levelColors[0][1]);

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
                    helper.AddKeyListKeys(stack, "first", rele.first.Keys, repo, thePackage, AdminShell.Key.AllElements,
                        jumpLambda: (kl) => { return new ModifyRepo.LambdaActionNone(); });
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
                        stack, "second", rele.second.Keys, repo, thePackage, AdminShell.Key.AllElements);
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

                // TODO (Michael Hoffmeister, 1970-01-01): ordered, allowDuplicates

                // dead-csharp off
                /* non-edit mode fails

                var g = helper.AddSmallGrid(2, 2, new[] { "#", "*" });

                helper.AddSmallLabelTo(g, 0, 0, padding: new Thickness(2, 0, 0, 0), content: "ordered: ");
                repo.RegisterControl(helper.AddSmallCheckBoxTo(g, 0, 1, margin: new Thickness(2, 2, 2, 2),
                    content: "(true e.g. for indexed array)", isChecked: smc.ordered),
                        (o) =>
                        {
                            if (o is bool)
                                smc.ordered = (bool)o;
                            return new ModifyRepo.LambdaActionNone();
                        });

                helper.AddSmallLabelTo(g, 1, 0, padding: new Thickness(2, 0, 0, 0), content: "allowDuplicates: ");
                repo.RegisterControl(helper.AddSmallCheckBoxTo(g, 1, 1, margin: new Thickness(2, 2, 2, 2),
                    content: "(true for multiple same element)", isChecked: smc.allowDuplicates),
                        (o) =>
                        {
                            if (o is bool)
                                smc.allowDuplicates = (bool)o;
                            return new ModifyRepo.LambdaActionNone();
                        });

                stack.Children.Add(g);

                */
                // dead-csharp on

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
                        stack, "Asset", ent.assetRef.Keys, repo, thePackage, AdminShell.Key.AllElements);
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
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell shell,
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
                        if (buttonNdx is int)
                        {
                            if ((int)buttonNdx == 0)
                            {
                                var ks = helper.SmartSelectAasEntityKeys(package.AasEnv, "SubmodelElement");
                                if (ks != null)
                                {
                                    view.AddContainedElement(ks);
                                }
                                return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: view);
                            }
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

            helper.AddGroup(stack, "Referable members:", levelColors[1][0], levelColors[1][1]);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return view.idShort == null || view.idShort.Length < 1; },
                        "idShort is mandatory for SubmodelElements. " +
                            "It is a short, unique identifier that is unique just in its context, its name space.",
                        breakIfTrue: true),
                    new HintCheck(
                        () => {
                            if (view.idShort == null) return false;
                            return !AdminShellUtil.ComplyIdShort(view.idShort);
                        },
                        "idShort shall only feature letters, digits, underscore ('_'); " +
                            "starting mandatory with a letter..")
                });
            helper.AddKeyValueRef(
                stack, "idShort", view, ref view.idShort, null, repo,
                v => { view.idShort = v as string; return new ModifyRepo.LambdaActionNone(); });

            helper.AddHintBubble(
                stack, hintMode,
                new HintCheck(
                    () => { return view.category != null && view.category.Trim().Length >= 1; },
                    "The use of category is unusual here.",
                    severityLevel: HintCheck.Severity.Notice));

            helper.AddKeyValueRef(
                stack, "category", view, ref view.category, null, repo,
                v => { view.category = v as string; return new ModifyRepo.LambdaActionNone(); },
                comboBoxItems: AdminShell.Referable.ReferableCategoryNames, comboBoxIsEditable: true);

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return view.description == null || view.description.langString == null ||
                                view.description.langString.Count < 1;
                        },
                        "The use of an description is recommended to allow the consumer " +
                            "of an view to understand the nature of it.",
                        breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return view.description.langString.Count < 2; },
                        "Consider having description in multiple langauges.", severityLevel: HintCheck.Severity.Notice)
                });
            if (helper.SafeguardAccess(
                    stack, repo, view.description, "description:", "Create data element!",
                    v =>
                    {
                        view.description = new AdminShell.Description();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                helper.AddKeyListLangStr(stack, "description", view.description.langString, repo);

            // HasSemantics

            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return view.semanticId == null || view.semanticId.IsEmpty; },
                        "Check if you want to add a semantic reference to an external repository. " +
                            "Only by adding this, a computer can distinguish, for what the view is really meant for.",
                        severityLevel: HintCheck.Severity.Notice )
                });
            helper.AddGroup(stack, "Semantic ID", levelColors[1][0], levelColors[1][1]);
            if (helper.SafeguardAccess(
                    stack, repo, view.semanticId, "semanticId:", "Create data element!",
                    v =>
                    {
                        view.semanticId = new AdminShell.SemanticId();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                helper.AddKeyListKeys(stack, "semanticId", view.semanticId.Keys, repo);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            helper.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return view.hasDataSpecification == null || view.hasDataSpecification.reference == null ||
                                view.hasDataSpecification.reference.Count<1;
                        },
                        "Check if you want to add a data specification link to a global, external ressource. " +
                            "Only by adding this, a human can understand, for what the view is really meant for.",
                            severityLevel: HintCheck.Severity.Notice )
                });
            if (helper.SafeguardAccess(
                stack, repo, view.hasDataSpecification, "HasDataSpecification:", "Create data element!",
                v =>
                {
                    view.hasDataSpecification = new AdminShell.HasDataSpecification();
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
            {
                helper.AddGroup(stack, "HasDataSpecification", levelColors[1][0], levelColors[1][1]);

                if (editMode)
                {
                    // let the user control the number of references
                    helper.AddAction(
                        stack, "Specifications:", new[] { "Add Reference", "Delete last reference" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx is int)
                            {
                                if ((int)buttonNdx == 0)
                                    view.hasDataSpecification.reference.Add(new AdminShell.Reference());

                                if ((int)buttonNdx == 1 && view.hasDataSpecification.reference.Count > 0)
                                    view.hasDataSpecification.reference.RemoveAt(
                                        view.hasDataSpecification.reference.Count - 1);
                            }
                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (view.hasDataSpecification != null && view.hasDataSpecification.reference != null &&
                    view.hasDataSpecification.reference.Count > 0)
                {
                    for (int i = 0; i < view.hasDataSpecification.reference.Count; i++)
                        helper.AddKeyListKeys(
                            stack, String.Format("reference[{0}]", i), view.hasDataSpecification.reference[i].Keys,
                            repo, package, addExistingEntities: null /* "All" */ );
                }
            }

        }

        public void DisplayOrEditAasEntityViewReference(
            AdminShellPackageEnv package, AdminShell.AdministrationShellEnv env, AdminShell.View view,
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
            helper.AddKeyListKeys(stack, "containedElement", reference.Keys, repo, package, "Asset");
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
            AdminShellPackageEnv package,
            VisualElementGeneric entity,
            bool editMode, bool hintMode = false,
            AdminShellPackageEnv[] auxPackages = null,
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
            this.thePackage = package;
            this.theEntity = entity;
            helper.package = package;
            helper.auxPackages = auxPackages;
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
                    package, x.theEnv, x.theItemType, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementAdminShell)
            {
                var x = entity as VisualElementAdminShell;
                DisplayOrEditAasEntityAas(
                    package, x.theEnv, x.theAas, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementAsset)
            {
                var x = entity as VisualElementAsset;
                DisplayOrEditAasEntityAsset(
                    package, x.theEnv, x.theAsset, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodelRef)
            {
                var x = entity as VisualElementSubmodelRef;
                AdminShell.AdministrationShell aas = null;
                if (x.Parent is VisualElementAdminShell xpaas)
                    aas = xpaas.theAas;
                DisplayOrEditAasEntitySubmodelOrRef(
                    package, x.theEnv, aas, x.theSubmodelRef, x.theSubmodel, editMode, repo, stack, levelColors,
                    hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodel)
            {
                var x = entity as VisualElementSubmodel;
                DisplayOrEditAasEntitySubmodelOrRef(
                    package, x.theEnv, null, null, x.theSubmodel, editMode, repo, stack, levelColors,
                    hintMode: hintMode);
            }
            else if (entity is VisualElementSubmodelElement)
            {
                var x = entity as VisualElementSubmodelElement;
                DisplayOrEditAasEntitySubmodelElement(
                    package, x.theEnv, x.theContainer, x.theWrapper, x.theWrapper.submodelElement, editMode,
                    repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementOperationVariable)
            {
                var x = entity as VisualElementOperationVariable;
                DisplayOrEditAasEntityOperationVariable(
                    package, x.theEnv, x.theContainer, x.theOpVar, editMode, repo,
                    stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementConceptDescription)
            {
                var x = entity as VisualElementConceptDescription;
                DisplayOrEditAasEntityConceptDescription(
                    package, x.theEnv, null, x.theCD, editMode, repo, stack, levelColors, hintMode: hintMode);
            }
            else if (entity is VisualElementView)
            {
                var x = entity as VisualElementView;
                if (x.Parent != null && x.Parent is VisualElementAdminShell xpaas)
                    DisplayOrEditAasEntityView(
                        package, x.theEnv, xpaas.theAas, x.theView, editMode, repo, stack, levelColors,
                        hintMode: hintMode);
                else
                    helper.AddGroup(stack, "View is corrupted!", levelColors[0][0], levelColors[0][1]);
            }
            else if (entity is VisualElementReference)
            {
                var x = entity as VisualElementReference;
                if (x.Parent != null && x.Parent is VisualElementView xpev)
                    DisplayOrEditAasEntityViewReference(
                        package, x.theEnv, xpev.theView, (AdminShell.ContainedElementRef)x.theReference,
                        editMode, repo, stack, levelColors);
                else
                    helper.AddGroup(stack, "Reference is corrupted!", levelColors[0][0], levelColors[0][1]);
            }
            else
            if (entity is VisualElementSupplementalFile)
            {
                var x = entity as VisualElementSupplementalFile;
                DisplayOrEditAasEntitySupplementaryFile(package, x.theFile, editMode, repo, stack, levelColors);
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
