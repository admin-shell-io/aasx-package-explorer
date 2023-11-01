/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Samm2_2_0;
using AasxAmlImExport;
using AasxCompatibilityModels;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xaml;
using VDS.RDF.Parsing;
using VDS.RDF;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Aas = AasCore.Aas3_0;
using Samm = AasCore.Samm2_2_0;
using System.Text.RegularExpressions;
using System.Runtime.Intrinsics.X86;
using Lucene.Net.Tartarus.Snowball.Ext;
using Lucene.Net.Util;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class extends the basic helper functionalities of DispEditHelper by providing modules for display/
    /// editing disting modules of the GUI, such as the different (re-usable) Interfaces of the AAS entities
    /// </summary>
    public class DispEditHelperModules : DispEditHelperMiniModules
    {
        //
        // Members
        //

        public class UploadAssistance
        {
            public string SourcePath = "";
            public string TargetPath = "/aasx/files";
        }
        public UploadAssistance uploadAssistance = new UploadAssistance();

        //
        // Inject a number of customised function in modules
        //

        public class DispEditInjectAction
        {
            public string[] auxTitles = null;
            public string[] auxToolTips = null;
            public Func<int, AnyUiLambdaActionBase> auxLambda = null;

            public DispEditInjectAction() { }

            public DispEditInjectAction(string[] auxTitles, Func<int, AnyUiLambdaActionBase> auxLambda)
            {
                this.auxTitles = auxTitles;
                this.auxLambda = auxLambda;
            }

            public DispEditInjectAction(string[] auxTitles, string[] auxToolTips,
                Func<int, AnyUiLambdaActionBase> auxActions)
            {
                this.auxTitles = auxTitles;
                this.auxToolTips = auxToolTips;
                this.auxLambda = auxActions;
            }

            public static string[] GetTitles(string[] fixTitles, DispEditInjectAction action)
            {
                var res = new List<string>();
                if (fixTitles != null)
                    res.AddRange(fixTitles);
                if (action?.auxTitles != null)
                    res.AddRange(action.auxTitles);
                if (res.Count < 1)
                    return null;
                return res.ToArray();
            }

            public static string[] GetToolTips(string[] fixToolTips, DispEditInjectAction action)
            {
                var res = new List<string>();
                if (fixToolTips != null)
                    res.AddRange(fixToolTips);
                if (action?.auxToolTips != null)
                    res.AddRange(action.auxToolTips);
                if (res.Count < 1)
                    return null;
                return res.ToArray();
            }
        }

        //
        // IReferable
        //

        public void DisplayOrEditEntityReferable(AnyUiStackPanel stack,
            Aas.IReferable parentContainer,
            Aas.IReferable referable,
            int indexPosition,
            DispEditInjectAction injectToIdShort = null,
            bool hideExtensions = false)
        {
            // access
            if (stack == null || referable == null)
                return;

            // members
            this.AddGroup(stack, "Referable:", levelColors.SubSection);

            // special case SML ..
            if (parentContainer?.IsIndexed() == true)
            {
                AddKeyValue(stack, "index", $"#{indexPosition:D2}", repo: null);
            }

            // for clarity, have two kind of hints for SML and for other
            var isIndexed = parentContainer.IsIndexed() == true;
            if (!isIndexed)
            {
                // not SML
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck( () => !(referable is Aas.IIdentifiable) && !referable.IdShort.HasContent(),
                        "The idShort is mandatory for all Referables which are not Identifiable. " +
                        "It is a short, unique identifier that is unique just in its context, " +
                        "its name space. ", breakIfTrue: true),
                    new HintCheck(
                        () => {
                            if (referable.IdShort == null) return false;
                            return !AdminShellUtil.ComplyIdShort(referable.IdShort);
                        },
                        "The idShort shall only feature letters, digits, underscore ('_'); " +
                        "starting mandatory with a letter."),
                    new HintCheck(
                        () => {
                            return true == referable.IdShort?.Contains("---");
                        },
                        "The idShort contains 3 dashes. Probably, the entitiy was auto-named " +
                        "to keep it unqiue because of an operation such a copy/ paste.",
                        severityLevel: HintCheck.Severity.Notice)
                    });
            }
            else
            {
                // SML ..
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck( () => referable.IdShort.HasContent(),
                        "Constraint AASd-120: idShort of SubmodelElements being a direct child of a " +
                        "SubmodelElementList shall not be specified.")
                    });
            }
            AddKeyValueExRef(
                stack, "idShort", referable, referable.IdShort, null, repo,
                v =>
                {
                    var dr = new DiaryReference(referable);
                    referable.IdShort = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange(), diaryReference: dr);
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: DispEditInjectAction.GetTitles(null, injectToIdShort),
                auxButtonToolTips: DispEditInjectAction.GetToolTips(null, injectToIdShort),
                auxButtonLambda: injectToIdShort?.auxLambda,
                takeOverLambdaAction: new AnyUiLambdaActionRedrawAllElements(nextFocus: referable)
                );

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => referable.DisplayName?.IsValid() != true,
                        "The use of a display name is recommended to express a human readable name " +
                        "for the Referable in multiple languages.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return referable.DisplayName.Count < 2; },
                        "Consider having Display name in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(stack, repo, referable.DisplayName, "displayName:", "Create data element!", v =>
            {
                referable.DisplayName = new List<Aas.ILangStringNameType>(new List<Aas.LangStringNameType>());
                this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddKeyListLangStr<ILangStringNameType>(stack, "displayName", referable.DisplayName,
                    repo, relatedReferable: referable);
            }

            // category deprecated
            this.AddHintBubble(
                stack, hintMode,
                new HintCheck(() => referable.Category?.HasContent() == true,
                "The use of category is deprecated. Do not plan to use this information in new developments.",
                severityLevel: HintCheck.Severity.Notice));

            AddKeyValueExRef(
                stack, "category", referable, referable.Category, null, repo,
                v =>
                {
                    referable.Category = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxItems: new string[] { "CONSTANT", "PARAMETER", "VARIABLE" }, comboBoxIsEditable: true);

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return referable.Description == null || referable.Description == null ||
                                referable.Description.Count < 1;
                        },
                        "The use of an description is recommended to allow the consumer of an Referable " +
                            "to understand the nature of it.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return referable.Description.Count < 2; },
                        "Consider having description in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(stack, repo, referable.Description, "description:", "Create data element!", v =>
            {
                referable.Description = new List<Aas.ILangStringTextType>();
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () =>
                        {
                            return referable.Description == null
                            || referable.Description.Count < 1;
                        },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Administration shell to understand your intentions.",
                        severityLevel: HintCheck.Severity.Notice));
                this.AddKeyListLangStr<ILangStringTextType>(stack, "description", referable.Description,
                    repo, relatedReferable: referable);
            }

            // Checksum
#if OLD
            this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => referable.Checksum?.HasContent() == true,
                        "The Checksum is deprecated. Do not plan to use this information in new developments.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice) });
            AddKeyValueExRef(
                stack, "checksum", referable, referable.Checksum, null, repo,
                v =>
                {
                    var dr = new DiaryReference(referable);
                    referable.Checksum = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange(), diaryReference: dr);
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: new[] { "Generate" },
                auxButtonToolTips: new[] { "Generate a SHA256 hashcode over this Referable" },
                auxButtonLambda: (i) =>
                {
                    if (i == 0)
                    {
                         //checksum= referable.ComputeHashcode();  
                         //TODO (jtikekar, 0000-00-00): support attributes
                        this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }

                    return new AnyUiLambdaActionNone();
                }
                );
#endif

            if (!hideExtensions)
            {
                // Extensions (at the end to make them not so much impressive!)
                DisplayOrEditEntityListOfExtension(
                    stack: stack, extensions: referable.Extensions,
                    setOutput: (v) => { referable.Extensions = v; },
                    relatedReferable: referable);
            }
        }

        //
        // Extensions
        //

        public void DisplayOrEditEntityListOfExtension(AnyUiStackPanel stack,
            List<Aas.IExtension> extensions,
            Action<List<Aas.IExtension>> setOutput,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasExtension:", levelColors.SubSection);

            if (this.SafeguardAccess(
                stack, repo, extensions, "extensions:", "Create empty list of Extensions!",
                v =>
                {
                    setOutput?.Invoke(new List<Aas.IExtension>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.ExtensionHelper(stack, repo, extensions, setOutput, relatedReferable: relatedReferable);
            }

        }

        //
        // Identifiable
        //

        public void DisplayOrEditEntityIdentifiable(AnyUiStackPanel stack,
            Aas.IIdentifiable identifiable,
            string templateForIdString,
            DispEditInjectAction injectToId = null)
        {
            // access
            if (stack == null || identifiable == null)
                return;

            // members
            this.AddGroup(stack, "Identifiable:", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return identifiable.Id == null; },
                    "Providing a worldwide unique identification is mandatory.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return identifiable.Id == ""; },
                    "Identification id shall not be empty. You could use the 'Generate' button in order to " +
                        "generate a worldwide unique id. " +
                        "The template of this id could be set by commandline arguments." )

            });
            if (this.SafeguardAccess(
                    stack, repo, identifiable.Id, "id:", "Create data element!",
                    v =>
                    {
                        identifiable.Id = "";
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                AddKeyValueExRef(
                    stack, "id", identifiable, identifiable.Id, null, repo,
                    v =>
                    {
                        var dr = new DiaryReference(identifiable);
                        identifiable.Id = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange(), diaryReference: dr);
                        return new AnyUiLambdaActionNone();
                    },
                    takeOverLambdaAction: new AnyUiLambdaActionRedrawAllElements(nextFocus: identifiable),
                    auxButtonTitles: DispEditInjectAction.GetTitles(new[] { "Generate" }, injectToId),
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            var dr = new DiaryReference(identifiable);
                            identifiable.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                templateForIdString);
                            this.AddDiaryEntry(identifiable, new DiaryEntryStructChange(), diaryReference: dr);
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: identifiable);
                        }
                        if (i >= 1)
                        {
                            var la = injectToId?.auxLambda?.Invoke(i - 1);
                            return la;
                        }
                        return new AnyUiLambdaActionNone();
                    });

                // further info?
                if (identifiable.Id.HasContent())
                {
                    this.AddKeyValue(
                        stack, "id (Base64)", AdminShellUtil.Base64Encode(identifiable.Id),
                        repo: null);
                }

            }

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return identifiable.Administration == null; },
                    "Check if providing admistrative information on version/ revision would be useful. " +
                        "This allows for better life-cycle management.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () =>
                    {
                        return identifiable.Administration.Version?.HasContent() != true ||
                            identifiable.Administration.Revision?.HasContent() != true;
                    },
                    "Admistrative information fields should not be empty.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (this.SafeguardAccess(
                    stack, repo, identifiable.Administration, "administration:", "Create data element!",
                    v =>
                    {
                        identifiable.Administration = new Aas.AdministrativeInformation();
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                // Allow administrative information to be deleted again
                this.AddGroup(stack, "administration:", levelColors.SubSection,
                    requestAuxButton: repo != null,
                    auxContextHeader: new[] { "\u2702", "Delete" },
                    auxContextLambda: (o) =>
                    {
                        if (o is int i && i == 0)
                        {
                            identifiable.Administration = null;
                            this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueExRef(
                    stack, "version", identifiable.Administration, identifiable.Administration.Version,
                    null, repo,
                    v =>
                    {
                        identifiable.Administration.Version = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueExRef(
                    stack, "revision", identifiable.Administration, identifiable.Administration.Revision,
                    null, repo,
                    v =>
                    {
                        identifiable.Administration.Revision = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                if (this.SafeguardAccess(
                    stack, repo, identifiable.Administration.Creator, "creator:", "Create data element!",
                    v =>
                    {
                        identifiable.Administration.Creator =
                            new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                {
                    this.AddKeyReference(
                        stack, "creator", identifiable.Administration.Creator, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All", // no restriction
                        relatedReferable: identifiable,
                        showRefSemId: false,
                        auxContextHeader: new[] { "\u2702", "Delete" },
                        auxContextLambda: (i) =>
                        {
                            if (i == 0)
                            {
                                identifiable.Administration.Creator = null;
                                this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            };
                            return new AnyUiLambdaActionNone();
                        },
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }

                AddKeyValueExRef(
                    stack, "templateId", identifiable.Administration, identifiable.Administration.TemplateId,
                    null, repo,
                    v =>
                    {
                        identifiable.Administration.TemplateId = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
            }
        }

        //Added this method only to support embeddedDS from ConceptDescriptions
        public void DisplayOrEditEntityHasDataSpecificationReferences(AnyUiStackPanel stack,
            List<Aas.IEmbeddedDataSpecification>? hasDataSpecification,
            Action<List<Aas.IEmbeddedDataSpecification>> setOutput,
            string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
            bool dataSpecRefsAreUsual = false,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasDataSpecification (Reference):", levelColors.SubSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => dataSpecRefsAreUsual && (hasDataSpecification == null
                        ||  hasDataSpecification.Count < 1),
                    "Check if a data specification is appropriate here. " +
                    "A ConceptDescription typically goes along with a data specification, e.g. " +
                    "according IEC61360.",
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () => { return !dataSpecRefsAreUsual && hasDataSpecification != null
                        && hasDataSpecification.Count > 0; },
                    "Check if a data specification is appropriate here.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice) });
            if (this.SafeguardAccess(
                    stack, this.repo, hasDataSpecification, "DataSpecification:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<Aas.IEmbeddedDataSpecification>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddActionPanel(
                        stack, "Specifications:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("add-reference", "Add Reference",
                                "Adds a reference to a data specification.")
                            .AddAction("add-preset", "Add Preset",
                                "Adds a reference to a data specification given by preset file.")
                            .AddAction("delete-reference", "Delete last reference",
                                "Deletes the last reference in the list."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                                hasDataSpecification.Add(
                                    new Aas.EmbeddedDataSpecification(
                                        new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>()),
                                        null));

                            if (buttonNdx == 1)
                            {
                                var pfn = Options.Curr.DataSpecPresetFile;
                                if (pfn == null || !System.IO.File.Exists(pfn))
                                {
                                    Log.Singleton.Error(
                                        $"JSON file for data specifcation presets not defined nor existing ({pfn}).");
                                    return new AnyUiLambdaActionNone();
                                }
                                try
                                {
                                    // read file contents
                                    var init = System.IO.File.ReadAllText(pfn);
                                    var presets = JsonConvert.DeserializeObject<List<DataSpecPreset>>(init);

                                    // define dialogue and map presets into dialogue items
                                    var uc = new AnyUiDialogueDataSelectFromList();
                                    uc.ListOfItems = presets.Select((pr)
                                            => new AnyUiDialogueListItem() { Text = pr.name, Tag = pr }).ToList();

                                    // perform dialogue
                                    this.context.StartFlyoverModal(uc);
                                    if (uc.Result && uc.ResultItem?.Tag is DataSpecPreset preset
                                        && preset.value != null)
                                    {
                                        hasDataSpecification.Add(
                                            new Aas.EmbeddedDataSpecification(
                                                new Aas.Reference(
                                                    Aas.ReferenceTypes.ExternalReference,
                                                    new Aas.IKey[] {
                                                        new Aas.Key(KeyTypes.GlobalReference, preset.value) }
                                                    .ToList()),
                                            null));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"While show Qualifier presets ({pfn})");
                                }
                            }

                            if (buttonNdx == 2)
                            {
                                if (hasDataSpecification.Count > 0)
                                    hasDataSpecification.RemoveAt(hasDataSpecification.Count - 1);
                                else
                                    setOutput?.Invoke(null);
                            }

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (hasDataSpecification != null && hasDataSpecification.Count > 0)
                {
                    // dead-csharp off
                    //TODO (jtikekar, 0000-00-00): refactor
                    // MIHO: is this at all required?
                    //List<string>[] presetKeys = new List<string>[addPresetKeyLists.Length];
                    //for (int j = 0; j < addPresetKeyLists.Length; j++)
                    //{
                    //    List<string> keys = new List<string>();
                    //    foreach(var key in addPresetKeyLists[j])
                    //    {
                    //        keys.Add(key.Value);
                    //    }
                    //    presetKeys[j] = keys;
                    //}

                    for (int i = 0; i < hasDataSpecification.Count; i++)
                    {
                        //List<string> keys = new List<string>();
                        //foreach (var key in hasDataSpecification[i].DataSpecification.Keys)
                        //{
                        //    keys.Add(key.Value);
                        //}
                        // if (hasDataSpecification[i].DataSpecification != null)
                        // dead-csharp on
                        int currentI = i;
                        if (this.SafeguardAccess(
                            stack, this.repo, hasDataSpecification[i].DataSpecification,
                                "DataSpecification:", "Create (inner) data element!",
                            v =>
                            {
                                hasDataSpecification[currentI].DataSpecification =
                                new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                                return new AnyUiLambdaActionRedrawEntity();
                            }))
                        {
                            this.AddKeyReference(
                            stack, String.Format("dataSpec.[{0}]", i),
                            hasDataSpecification[i].DataSpecification,
                            repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                            addExistingEntities: null /* "All" */,
                            addPresetNames: addPresetNames, addPresetKeyLists: addPresetKeyLists,
                            relatedReferable: relatedReferable,
                            showRefSemId: false,
                            auxContextHeader: new[] { "\u2573", "Delete this dataSpec." },
                            auxContextLambda: (choice) =>
                            {
                                if (choice == 0)
                                {
                                    if (currentI >= 0 && currentI <= hasDataSpecification.Count)
                                        hasDataSpecification.RemoveAt(currentI);
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                                return new AnyUiLambdaActionNone();
                            });
                        }
                    }
                }
            }
        }

        public void DisplayOrEditEntityHasEmbeddedSpecification(
            Aas.Environment env, AnyUiStackPanel stack,
            List<Aas.IEmbeddedDataSpecification> hasDataSpecification,
            Action<List<Aas.IEmbeddedDataSpecification>> setOutput,
            string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null,
            bool suppressNoEdsWarning = false)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasDataSpecification (records of embedded data specification):", levelColors.MainSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return !suppressNoEdsWarning && (hasDataSpecification == null ||
                            hasDataSpecification.Count < 1); },
                        "For ConceptDescriptions, the main data carrier lies in the embedded data specification. " +
                        "In these elements, a Reference to a data specification is combined with content " +
                        "attributes, which are attached to the ConceptDescription. These attributes hold the " +
                        "descriptive information on a concept and thus allow for an off-line understanding of " +
                        "the meaning of a concept/ SubmodelElement. Multiple data specifications " +
                        "could be possible. The most used is the IEC61360, which is also used by ECLASS. " +
                        "Please create this data element.",
                        breakIfTrue: true),
                });
            if (this.SafeguardAccess(
                    stack, this.repo, hasDataSpecification, "Specifications:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<Aas.IEmbeddedDataSpecification>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                // head control
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddActionPanel(
                        stack, "Spec. records:", repo: repo,
						superMenu: superMenu,
				        ticketMenu: new AasxMenu()
					        .AddAction("add-record", "Add record",
						        "Adds a record for data specification reference and content.")
							.AddAction("add-iec61360", "Add IEC61360",
								"Adds a record initialized for IEC 61360 content.")
							.AddAction("auto-detect", "Auto detect content",
								"Auto dectects known data specification contents and sets valid references.")
							.AddAction("delete-last", "Delete last record",
								"Deletes last record (data specification reference and content)."),
						ticketAction: (buttonNdx, ticket) =>
						{
                            if (buttonNdx == 0)
                                hasDataSpecification.Add(
                                    new Aas.EmbeddedDataSpecification(
                                        new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>()),
                                        null));

                            if (buttonNdx == 1)
                                hasDataSpecification.Add(
                                    new Aas.EmbeddedDataSpecification(
                                        new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey> {
                                            ExtendIDataSpecificationContent.GetKeyForIec61360()
                                        }),
                                        new Aas.DataSpecificationIec61360(new List<Aas.ILangStringPreferredNameTypeIec61360>() {
                                            new Aas.LangStringPreferredNameTypeIec61360(
                                                AdminShellUtil.GetDefaultLngIso639(), "")
                                        })));

                            if (buttonNdx == 2)
                            {
                                var fix = 0;
                                foreach (var eds in hasDataSpecification)
                                    if (eds != null && eds.FixReferenceWrtContent())
                                        fix++;
                                Log.Singleton.Info($"Fixed {fix} records of embedded data specification.");
                            }

                            if (buttonNdx == 3)
                            {
                                if (hasDataSpecification.Count > 0)
                                    hasDataSpecification.RemoveAt(hasDataSpecification.Count - 1);
                                else
                                    setOutput?.Invoke(null);
                            }

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (hasDataSpecification != null && hasDataSpecification.Count > 0)
                {
                    for (int i = 0; i < hasDataSpecification.Count; i++)
                    {
                        // indicate
                        this.AddGroup(stack, $"dataSpec.[{i}] / Reference:", levelColors.SubSection);

                        // Reference
                        int currentI = i;
                        if (SafeguardAccess(
                            stack, this.repo, hasDataSpecification[i].DataSpecification,
                                "DataSpecification:", "Create (inner) data element!",
                            v =>
                            {
                                hasDataSpecification[currentI].DataSpecification =
                                    new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                                return new AnyUiLambdaActionRedrawEntity();
                            }))
                        {
                            AddKeyReference(
                                stack, String.Format("dataSpec.[{0}]", i),
                                hasDataSpecification[i].DataSpecification,
                                repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                                addExistingEntities: null /* "All" */,
                                addPresetNames: addPresetNames, addPresetKeyLists: addPresetKeyLists,
                                relatedReferable: relatedReferable,
                                showRefSemId: false,
                                auxContextHeader: new[] { "\u2573", "Delete this dataSpec." },
                                auxContextLambda: (choice) =>
                                {
                                    if (choice == 0)
                                    {
                                        if (currentI >= 0 && currentI <= hasDataSpecification.Count)
                                            hasDataSpecification.RemoveAt(currentI);
                                        return new AnyUiLambdaActionRedrawEntity();
                                    }
                                    return new AnyUiLambdaActionNone();
                                });
                        }

                        // which content is possible?
                        var cntByDs = ExtendIDataSpecificationContent.GuessContentTypeFor(
                                        hasDataSpecification[i].DataSpecification);

                        AddHintBubble(
                            stack, hintMode, new[] {
                            new HintCheck(
                                () => cntByDs == ExtendIDataSpecificationContent.ContentTypes.NoInfo,
                                "No valid data specification Reference could be identified. Thus, no content " +
                                "attributes could be provided. Check the Reference.")
                            });

                        // indicate new section
                        AddGroup(stack, $"dataSpec.[{i}] / Content:", levelColors.SubSection);

                        // edit content?
                        if (cntByDs != ExtendIDataSpecificationContent.ContentTypes.NoInfo)
                        {
                            var cntNone = hasDataSpecification[i].DataSpecificationContent == null;
                            var cntMismatch = ExtendIDataSpecificationContent.GuessContentTypeFor(
                                            hasDataSpecification[i].DataSpecificationContent) !=
                                                ExtendIDataSpecificationContent.ContentTypes.NoInfo
                                            && ExtendIDataSpecificationContent.GuessContentTypeFor(
                                            hasDataSpecification[i].DataSpecificationContent) != cntByDs;

                            this.AddHintBubble(
                                stack, hintMode,
                                new[] {
                                new HintCheck(
                                    () => cntNone,
                                    "No data specification content is available for this record. " +
                                    "Create content in order to create this important descriptinve " +
                                    "information.",
                                    breakIfTrue: true),
                                new HintCheck(
                                    () => cntMismatch,
                                    "Mismatch between data specification Reference and stored content " +
                                    "of data specification.")
                                });

                            if (SafeguardAccess(
                                stack, this.repo, (cntNone || cntMismatch) ? null : "NotNull",
                                    "Content:", "Create (reset) content data element!",
                                v =>
                                {
                                    hasDataSpecification[currentI].DataSpecificationContent =
                                        ExtendIDataSpecificationContent.ContentFactoryFor(cntByDs);

                                    return new AnyUiLambdaActionRedrawEntity();
                                }))
                            {
                                if (cntByDs == ExtendIDataSpecificationContent.ContentTypes.Iec61360)
                                    this.DisplayOrEditEntityDataSpecificationIec61360(
                                        env, stack,
                                        hasDataSpecification[i].DataSpecificationContent
                                            as Aas.DataSpecificationIec61360,
                                        relatedReferable: relatedReferable, superMenu: superMenu);

                                //TODO (jtikekar, 0000-00-00): support DataSpecificationPhysicalUnit
#if SupportDataSpecificationPhysicalUnit
                                if (cntByDs == ExtendIDataSpecificationContent.ContentTypes.PhysicalUnit)
                                    this.DisplayOrEditEntityDataSpecificationPhysicalUnit(
                                        stack,
                                        hasDataSpecification[i].DataSpecificationContent
                                            as Aas.DataSpecificationPhysicalUnit,
                                        relatedReferable: relatedReferable); 
#endif
                            }
                        }
                    }
                }
            }
        }
        //
        // List of References (used for isCaseOf..)
        //

        public void DisplayOrEditEntityListOfReferences(AnyUiStackPanel stack,
            List<Aas.IReference> references,
            Action<List<Aas.IReference>> setOutput,
            string entityName,
            string[] addPresetNames = null, Aas.Key[] addPresetKeys = null,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            if (this.SafeguardAccess(
                    stack, this.repo, references, $"{entityName}:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<Aas.IReference>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(stack, $"{entityName}:", levelColors.SubSection);

                if (editMode)
                {
                    // let the user control the number of references
                    this.AddActionPanel(
                        stack, $"{entityName}:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("add-reference", "Add Reference",
                                "Adds a reference to the list.")
                            .AddAction("delete-reference", "Delete last reference",
                                "Deletes the last reference in the list."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                                references.Add(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>()));

                            if (buttonNdx == 1 && references.Count > 0)
                                references.RemoveAt(references.Count - 1);

                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (references != null && references.Count > 0)
                {
                    for (int i = 0; i < references.Count; i++)
                        this.AddKeyReference(
                            stack, String.Format("reference[{0}]", i), references[i], repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            "All",
                            addEclassIrdi: true,
                            relatedReferable: relatedReferable,
                            showRefSemId: false);
                }
            }
        }

        //
        // Kind
        //

        public void DisplayOrEditEntityAssetKind(AnyUiStackPanel stack,
            Aas.AssetKind kind,
            Action<Aas.AssetKind> setOutput,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind (of AssetInformation):", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return kind != Aas.AssetKind.Instance; },
                    "Check for kind setting. 'Instance' is the usual choice.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (SafeguardAccess(
                stack, repo, kind, "kind:", "Create data element!",
                v =>
                {
                    setOutput?.Invoke(new Aas.AssetKind());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                AddKeyValueExRef(
                    stack, "kind", kind, Aas.Stringification.ToString(kind), null, repo,
                    v =>
                    {
                        setOutput?.Invoke((Aas.AssetKind)Aas.Stringification.AssetKindFromString((string)v));
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(Aas.AssetKind)), comboBoxMinWidth: 105);
            }
        }

        public void DisplayOrEditEntityModelingKind(AnyUiStackPanel stack,
            Aas.ModellingKind? kind,
            Action<Aas.ModellingKind> setOutput,
            string instanceExceptionStatement = null,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind (of model):", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return kind == null; },
                    "Providing kind information is mandatory. Typically you want to model instances. " +
                        "A manufacturer would define types of assets, as well.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return kind != Aas.ModellingKind.Instance; },
                    "Check for kind setting. 'Instance' is the usual choice." + instanceExceptionStatement,
                    severityLevel: HintCheck.Severity.Notice )
            });

            if (this.SafeguardAccess(
                stack, repo, kind, "kind:", "Create data element!",
                v =>
                {
                    setOutput?.Invoke(Aas.ModellingKind.Instance);
                    return new AnyUiLambdaActionRedrawEntity();
                }
                ))
            {
                AddKeyValueExRef(
                    stack, "kind", kind, Aas.Stringification.ToString(kind), null, repo,
                    v =>
                    {
                        setOutput?.Invoke((Aas.ModellingKind)Aas.Stringification.ModellingKindFromString((string)v));
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(Aas.ModellingKind)), comboBoxMinWidth: 105);
            }
        }

        //
        // HasSemantic
        //

        public void DisplayOrEditEntitySemanticId(AnyUiStackPanel stack,
            Aas.IHasSemantics semElem,
            string statement = null,
            bool checkForCD = false,
            string addExistingEntities = null,
            CopyPasteBuffer cpb = null,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null || semElem == null)
                return;

            //
            // SemanticId
            //

            this.AddGroup(stack, "Semantic ID:", levelColors.SubSection);

            // hint
            this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return semElem.SemanticId == null
                                || semElem.SemanticId.IsEmpty(); },
                            "Check if you want to add a semantic reference to an external " +
                            "concept repository entry. " + statement,
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                                () => { return checkForCD &&
                                    semElem.SemanticId.Keys[0].Type != Aas.KeyTypes.GlobalReference; },
                                "The semanticId usually features a GlobalReference to a concept " +
                                "within a respective concept repository.",
                                severityLevel: HintCheck.Severity.Notice)
                    });

            // add from Copy Buffer
            var bufferKeys = CopyPasteBuffer.PreparePresetsForListKeys(cpb);

            // add the keys
            if (this.SafeguardAccess(
                    stack, repo, semElem.SemanticId, "semanticId:", "Create data element!",
                    v =>
                    {
                        semElem.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyReference(
                    stack, "semanticId", semElem.SemanticId, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    showRefSemId: false,
                    addExistingEntities: addExistingEntities, addFromKnown: true,
                    addEclassIrdi: true,
                    addPresetNames: bufferKeys.Item1,
                    addPresetKeyLists: bufferKeys.Item2,
                    jumpLambda: (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)));
                    },
                    relatedReferable: relatedReferable,
                    auxContextHeader: new[] { "\u2573", "Delete semanticId" },
                    auxContextLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            semElem.SemanticId = null;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    });

            //
            // Supplemenatal SemanticId
            //

            this.AddGroup(stack, "Supplemental Semantic IDs:", levelColors.SubSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => semElem.SupplementalSemanticIds == null
                        || semElem.SupplementalSemanticIds.Count < 1,
                    "Check if a supplemental semanticId is appropriate here. This only make sense, when " +
                    "the primary semanticId does not semantically identifies all relevant aspects of the " +
                    "AAS element.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice) });
            if (this.SafeguardAccess(
                    stack, this.repo, semElem.SupplementalSemanticIds, "supplementalSem.Id:", "Create data element!",
                    action: v =>
                    {
                        semElem.SupplementalSemanticIds = new List<Aas.IReference>();
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddActionPanel(
                        stack, "supplementalSem.Id:",
                        new[] { "Add", "Delete last" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                semElem.SupplementalSemanticIds.Add(
                                    new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>()));

                            if (buttonNdx == 1)
                            {
                                if (semElem.SupplementalSemanticIds.Count > 0)
                                    semElem.SupplementalSemanticIds.RemoveAt(
                                        semElem.SupplementalSemanticIds.Count - 1);
                                else
                                    semElem.SupplementalSemanticIds = null;
                            }

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (semElem.SupplementalSemanticIds != null
                    && semElem.SupplementalSemanticIds.Count > 0)
                {
                    for (int i = 0; i < semElem.SupplementalSemanticIds.Count; i++)
                    {
                        AddKeyReference(
                            stack, String.Format("Suppl.Sem.Id[{0}]", i),
                            semElem.SupplementalSemanticIds[i], repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            showRefSemId: false,
                            addExistingEntities: addExistingEntities, addFromKnown: true,
                            addEclassIrdi: true,
                            addPresetNames: bufferKeys.Item1,
                            addPresetKeyLists: bufferKeys.Item2,
                            jumpLambda: (kl) =>
                            {
                                return new AnyUiLambdaActionNavigateTo(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)));
                            },
                            relatedReferable: relatedReferable,
                            auxContextHeader: new[] { "\u2573", "Delete supplementalSemanticId" },
                            auxContextLambda: (i) =>
                            {
                                if (i == 0)
                                {
                                    semElem.SemanticId = null;
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                                return new AnyUiLambdaActionNone();
                            });

                        // small vertical space
                        if (i < semElem.SupplementalSemanticIds.Count - 1)
                            AddVerticalSpace(stack);
                    }
                }
            }
        }

        //
        // Qualifiable
        //

        public void DisplayOrEditEntityQualifierCollection(AnyUiStackPanel stack,
            List<Aas.IQualifier> qualifiers,
            Action<List<Aas.IQualifier>> setOutput,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Qualifiable:", levelColors.SubSection,
                requestAuxButton: repo != null,
                auxContextHeader: new[] { "\u27f4", "Migrate to Extensions" },
                auxContextLambda: (o) =>
                {
                    if (o is int i && i == 0 && relatedReferable != null)
                    {
                        if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                "Migrate particular Qualifiers (V2.0) to Extensions (V3.0) " +
                                "for this element and all child elements? " +
                                "This operation cannot be reverted!", "Qualifiers",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            return new AnyUiLambdaActionNone();

                        relatedReferable.RecurseOnReferables(null,
                            includeThis: true,
                            lambda: (o, parents, rf) =>
                            {
                                rf?.MigrateV20QualifiersToExtensions();
                                return true;
                            });

                        Log.Singleton.Info("Migration of particular Qualifiers (V2.0) to Extensions (V3.0).");
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    return new AnyUiLambdaActionNone();
                });

            if (this.SafeguardAccess(
                stack, repo, qualifiers, "Qualifiers:", "Create empty list of Qualifiers!",
                v =>
                {
                    setOutput?.Invoke(new List<Aas.IQualifier>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.QualifierHelper(stack, repo, qualifiers, relatedReferable: relatedReferable,
                        superMenu: superMenu);
            }

        }

        //
        // List of SpecificAssetId
        //

        public void DisplayOrEditEntityListOfSpecificAssetIds(AnyUiStackPanel stack,
            List<Aas.ISpecificAssetId> pairs,
            Action<List<Aas.ISpecificAssetId>> setOutput,
            string key = "IdentifierKeyValuePairs",
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, $"{key}:", levelColors.SubSection);

            if (this.SafeguardAccess(
                stack, repo, pairs, $"{key}:", "Create data element!",
                v =>
                {
                    setOutput?.Invoke(new List<Aas.ISpecificAssetId>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.SpecificAssetIdHelper(stack, repo, pairs,
                    key: key,
                    relatedReferable: relatedReferable);
            }

        }
        // dead-csharp off
        //not anymore required?!
        //public void DisplayOrEditEntitySingleIdentifierKeyValuePair(AnyUiStackPanel stack,
        //    List<Aas.ISpecificAssetId> pair,
        //    Action<List<Aas.ISpecificAssetId>> setOutput,
        //    string key = "IdentifierKeyValuePair",
        //    Aas.IReferable relatedReferable = null,
        //    string[] auxContextHeader = null, Func<object, AnyUiLambdaActionBase> auxContextLambda = null)
        //{
        //    // access
        //    if (stack == null)
        //        return;

        //    // members
        //    this.AddGroup(stack, $"{key}:", levelColors.SubSection, 
        //        requestAuxButton: repo != null,
        //        auxContextHeader: auxContextHeader, auxContextLambda: auxContextLambda);

        //    if (this.SafeguardAccess(
        //        stack, repo, pair, $"{key}:", "Create data element!",
        //        v =>
        //        {
        //            setOutput?.Invoke(new List<ISpecificAssetId>());
        //            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
        //            return new AnyUiLambdaActionRedrawEntity();
        //        }))
        //    {
        //        //TODO (jtikekar, 0000-00-00): need to test
        //        foreach (var specificAssetId in pair)
        //        {
        //            this.IdentifierKeyValueSinglePairHelper(
        //                        stack, repo, specificAssetId,
        //                        relatedReferable: relatedReferable);
        //        }
        //    }
        //}
        // dead-csharp on

        //
        // DataSpecificationIEC61360
        //


        public void DisplayOrEditEntityDataSpecificationIec61360(
            Aas.Environment env,
            AnyUiStackPanel stack,
            Aas.DataSpecificationIec61360 dsiec,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null || dsiec == null)
                return;

            // members
            this.AddGroup(stack, "Data Specification Content IEC61360:", levelColors.SubSection);

            // PreferredName

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.PreferredName == null || dsiec.PreferredName.Count < 1; },
                            "Please add a preferred name, which could be used on user interfaces " +
                                "to identify the concept to a human person.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.PreferredName.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.PreferredName, "preferredName:", "Create data element!",
                    v =>
                    {
                        dsiec.PreferredName = new List<Aas.ILangStringPreferredNameTypeIec61360>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyListLangStr<ILangStringPreferredNameTypeIec61360>(stack, "preferredName", dsiec.PreferredName,
                    repo, relatedReferable: relatedReferable);

            // ShortName

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.ShortName == null || dsiec.ShortName.Count < 1; },
                            "Please check if you can add a short name, which is a reduced, even symbolic version of " +
                                "the preferred name. IEC 61360 defines some symbolic rules " +
                                "(e.g. greek characters) for this name.",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.ShortName.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice),
                        new HintCheck(
                            () => { return dsiec.ShortName
                                .Select((ls) => ls.Text != null && ls.Text.Length > 18)
                                .Any((c) => c == true); },
                            "ShortNameTypeIEC61360 only allows 1..18 characters.")
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.ShortName, "shortName:", "Create data element!",
                    v =>
                    {
                        dsiec.ShortName = new List<Aas.ILangStringShortNameTypeIec61360>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyListLangStr<ILangStringShortNameTypeIec61360>(stack, "shortName", dsiec.ShortName,
                    repo, relatedReferable: relatedReferable);

            // Unit

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => {
                                return (dsiec.UnitId == null || dsiec.UnitId.Keys.Count < 1) &&
                                    ( dsiec.Unit == null || dsiec.Unit.Trim().Length < 1);
                            },
                            "Please check, if you can provide a unit or a unitId, " +
                                "in which the concept is being measured. " +
                                "Usage of SI-based units is encouraged.",
                            severityLevel: HintCheck.Severity.Notice)
            });
            AddKeyValueExRef(
                stack, "unit", dsiec, dsiec.Unit, null, repo,
                v =>
                {
                    dsiec.Unit = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // UnitId

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => {
                                return ( dsiec.Unit == null || dsiec.Unit.Trim().Length < 1) &&
                                    ( dsiec.UnitId == null || dsiec.UnitId.Keys.Count < 1);
                            },
                            "Please check, if you can provide a unit or a unitId, " +
                                "in which the concept is being measured. " +
                                "Usage of SI-based units is encouraged.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.UnitId, "unitId:", "Create data element!",
                    v =>
                    {
                        dsiec.UnitId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                List<string> keys = new();
                foreach (var key in dsiec.UnitId.Keys)
                {
                    keys.Add(key.Value);
                }
                this.AddKeyReference(
                    stack, "unitId", dsiec.UnitId, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    addExistingEntities: Aas.Stringification.ToString(Aas.KeyTypes.GlobalReference),
                    addEclassIrdi: true,
                    relatedReferable: relatedReferable);
            }

            // source of definition

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () =>
                            {
                                return dsiec.SourceOfDefinition == null || dsiec.SourceOfDefinition.Length < 1;
                            },
                            "Please check, if you can provide a source of definition for the concepts. " +
                                "This could be an informal link to a document, glossary item etc.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "sourceOfDef.", dsiec, dsiec.SourceOfDefinition, null, repo,
                v =>
                {
                    dsiec.SourceOfDefinition = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // Symbol

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.Symbol == null || dsiec.Symbol.Trim().Length < 1; },
                            "Please check, if you can provide formulaic character for the concept.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "symbol", dsiec, dsiec.Symbol, null, repo,
                v =>
                {
                    dsiec.Symbol = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // DataType

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.DataType == null; },
                            "Please provide a data type for the concept. " +
                                "Data types are provided by the IEC 61360.")
                });
            AddKeyValueExRef(
                stack, "dataType", dsiec, Aas.Stringification.ToString(dsiec.DataType), null, repo,
                v =>
                {
                    dsiec.DataType = Aas.Stringification.DataTypeIec61360FromString(v as string);
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxIsEditable: true,
                comboBoxMinWidth: 190,
                comboBoxItems: Aas.Constants.DataTypeIec61360ForPropertyOrValue.Select(
                    (dt) => Aas.Stringification.ToString(dt)).ToArray());

            // Definition

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.Definition == null || dsiec.Definition.Count < 1; },
                            "Please check, if you can add a definition, which could be used to describe exactly, " +
                                "how to establish a value/ measurement for the concept.",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.Definition.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.Definition, "definition:", "Create data element!",
                    v =>
                    {
                        dsiec.Definition = new List<Aas.ILangStringDefinitionTypeIec61360>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr<ILangStringDefinitionTypeIec61360>(stack, "definition", dsiec.Definition,
                    repo, relatedReferable: relatedReferable);

            // ValueFormat

            AddKeyValueExRef(
                stack, "valueFormat", dsiec, dsiec.ValueFormat, null, repo,
                v =>
                {
                    dsiec.ValueFormat = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // ValueList

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.ValueList == null
                                || dsiec.ValueList.ValueReferencePairs == null
                                || dsiec.ValueList.ValueReferencePairs.Count < 1; },
                            "If the concept features multiple possible discrete values (enumeration), " +
                            "please check, if you can add pairs of name and References to concepts " +
                            "representing the single values.",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.ValueList.ValueReferencePairs.Count < 2; },
                            "Please add multiple pairs of name and Reference.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.ValueList?.ValueReferencePairs, "valueList:", "Create data element!",
                    v =>
                    {
                        dsiec.ValueList ??= new Aas.ValueList(null);
                        dsiec.ValueList.ValueReferencePairs = new();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(stack, "IEC61360 value list items", levelColors.SubSection);

                ValueListHelper(
                    env, stack, repo, "valueList",
                    dsiec.ValueList.ValueReferencePairs,
                    relatedReferable: relatedReferable, superMenu: superMenu);
            }

            // Value

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.Value == null || dsiec.Value.Trim().Length < 1; },
                            "If the concepts stands for a single vlaue, please provide the value. " +
                            "Not required for enumerations or properties.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "value", dsiec, dsiec.Value, null, repo,
                v =>
                {
                    dsiec.Value = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // LevelType

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => dsiec.LevelType == null ||
                                !(dsiec.LevelType.Min || dsiec.LevelType.Max
                                  || dsiec.LevelType.Nom || dsiec.LevelType.Typ),
                            "Consider specifying a IEC61360 level type attribute for the " +
                            "intended values.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.LevelType, "levelType:", "Create data element!",
                    v =>
                    {
                        dsiec.LevelType = new Aas.LevelType(false, false, false, false);
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                var subg = AddSubGrid(stack, "levelType:",
                1, 4, new[] { "#", "#", "#", "#" },
                paddingCaption: new AnyUiThickness(5, 0, 0, 0),
                marginGrid: new AnyUiThickness(4, 0, 0, 0),
                minWidthFirstCol: GetWidth(FirstColumnWidth.Standard));

                Action<int, string, bool, Action<bool>> lambda = (col, name, value, setValue) =>
                {
                    AnyUiUIElement.RegisterControl(
                        AddSmallCheckBoxTo(subg, 0, col,
                            content: name,
                            isChecked: value,
                            margin: new AnyUiThickness(0, 0, 15, 0)),
                        (v) =>
                        {
                            setValue?.Invoke(!value);
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                };

                lambda(0, "min", dsiec.LevelType.Min, (sv) => { dsiec.LevelType.Min = sv; });
                lambda(1, "max", dsiec.LevelType.Max, (sv) => { dsiec.LevelType.Max = sv; });
                lambda(2, "nom", dsiec.LevelType.Nom, (sv) => { dsiec.LevelType.Nom = sv; });
                lambda(3, "typ", dsiec.LevelType.Typ, (sv) => { dsiec.LevelType.Typ = sv; });
            }
        }

        //
        // DataSpecificationIEC61360
        //

        //TODO (jtikekar, 0000-00-00): support DataSpecificationPhysicalUnit
#if SupportDataSpecificationPhysicalUnit
        public void DisplayOrEditEntityDataSpecificationPhysicalUnit(
    AnyUiStackPanel stack,
    Aas.DataSpecificationPhysicalUnit dspu,
    Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null || dspu == null)
                return;

            // members
            AddGroup(
                stack, "Data Specification Content Physical Unit:", levelColors.SubSection);

            // UnitName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.UnitName.HasContent() != true,
                        "Please name the phyiscal unit. This is mandatory information.")
                });
            AddKeyValueExRef(
                stack, "unitName", dspu, dspu.UnitName, null, repo,
                v =>
                {
                    dspu.UnitName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // UnitSymbol

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.UnitSymbol.HasContent() != true,
                        "Please provide a symbol representation to the phyiscal unit. " +
                        "This is mandatory information, if available.")
                });
            AddKeyValueExRef(
                stack, "unitSymbol", dspu, dspu.UnitSymbol, null, repo,
                v =>
                {
                    dspu.UnitSymbol = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // Definition

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dspu.Definition == null || dspu.Definition.Count < 1; },
                        "Please check, if you can add a definition, which could be used to describe exactly, " +
                            "how to the unit is defined or measured concept.",
                        severityLevel: HintCheck.Severity.Notice,
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return dspu.Definition.Count <2; },
                        "Please add multiple languanges for the definition.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (this.SafeguardAccess(
                    stack, repo, dspu.Definition, "definition:", "Create data element!",
                    v =>
                    {
                        dspu.Definition = new List<Aas.LangString>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "definition", dspu.Definition,
                    repo, relatedReferable: relatedReferable);

            // SiNotation

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.SiNotation.HasContent() != true,
                        "Please check, if you can provide a notation according to SI.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "SI notation", dspu, dspu.SiNotation, null, repo,
                v =>
                {
                    dspu.SiNotation = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // SiName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.SiName.HasContent() != true,
                        "Please check, if you can provide a name according to SI.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "SI name", dspu, dspu.SiName, null, repo,
                v =>
                {
                    dspu.SiName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // DinNotation

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.DinNotation.HasContent() != true,
                        "Please check, if you can provide a notation according to DIN.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "DIN notation", dspu, dspu.DinNotation, null, repo,
                v =>
                {
                    dspu.DinNotation = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // EceName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.EceName.HasContent() != true,
                        "Please check, if you can provide a name according to ECE.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "ECE name", dspu, dspu.EceName, null, repo,
                v =>
                {
                    dspu.EceName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // EceCode

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.EceCode.HasContent() != true,
                        "Please check, if you can provide a code according to DIN.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "ECE code", dspu, dspu.EceCode, null, repo,
                v =>
                {
                    dspu.EceCode = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // NistName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.NistName.HasContent() != true,
                        "Please check, if you can provide a name according to NIST.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "NIST name", dspu, dspu.NistName, null, repo,
                v =>
                {
                    dspu.EceName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // source of definition

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.SourceOfDefinition.HasContent() != true,
                        "Please check, if you can provide a source of definition for the unit. " +
                        "This could be an informal link to a document, glossary item etc.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "sourceOfDef.", dspu, dspu.SourceOfDefinition, null, repo,
                v =>
                {
                    dspu.SourceOfDefinition = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // conversion factor

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.ConversionFactor.HasContent() != true,
                        "Please check, if you can provide a conversion factor. " +
                        "Example could be: 1.0/60 .",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "conversionFac.", dspu, dspu.ConversionFactor, null, repo,
                v =>
                {
                    dspu.ConversionFactor = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // registration authority id

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.RegistrationAuthorityId.HasContent() != true,
                        "Please check, if you can provide a registration authority id.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "regAuthId.", dspu, dspu.RegistrationAuthorityId, null, repo,
                v =>
                {
                    dspu.RegistrationAuthorityId = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // Supplier

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.Supplier.HasContent() != true,
                        "Please check, if you can provide a supplier.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "supplier", dspu, dspu.Supplier, null, repo,
                v =>
                {
                    dspu.Supplier = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });
        } 
#endif

        //
        // special Submodel References
        // 


        // TODO (MIHO, 2022-12-26): seems not to be used
#if OLD
        public void DisplayOrEditEntitySubmodelRef(AnyUiStackPanel stack,
            Aas.Reference smref,
            Action<Aas.Reference> setOutput,
            string entityName,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return smref == null; },
                    $"No {entityName}. Please consider adding a reference " +
                        "to an adequate Submodel."),
            });
            if (this.SafeguardAccess(
                    stack, repo, smref, $"{entityName}:",
                    "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>()));
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(
                    stack, $"{entityName} - Aas.Reference to describing Submodel:",
                    levelColors.SubSection);
                this.AddKeyListKeys(
                    stack, $"{entityName}:", smref.Keys,
                    repo, packages, PackageCentral.PackageCentral.Selector.Main, "Submodel",
                    relatedReferable: relatedReferable);
            }
        }
#endif

        //
        // File / Resource attributes
        // 

        public void DisplayOrEditEntityFileResource(AnyUiStackPanel stack,
            Aas.IReferable containingObject,
            ModifyRepo repo, AasxMenu superMenu,
            string valuePath,
            string valueContent,
            Action<string, string> setOutput,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members

            // Value

            AddKeyValueExRef(
                stack, "value", containingObject, valuePath, null, repo,
                v =>
                {
                    valuePath = v as string;
                    setOutput?.Invoke(valuePath, valueContent);
                    this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: new[] { "Choose supplemental file", },
                auxButtonToolTips: new[] { "Select existing supplemental file" },
                auxButtonLambda: (bi) =>
                {
                    if (bi == 0)
                    {
                        // Select
                        var ve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.Main, "SupplementalFile");
                        if (ve != null)
                        {
                            var sf = (ve.GetMainDataObject()) as AdminShellPackageSupplementaryFile;
                            if (sf != null)
                            {
                                valuePath = sf.Uri.ToString();
                                setOutput?.Invoke(valuePath, valueContent);
                                this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                    }

                    return new AnyUiLambdaActionNone();
                });

            // ContentType

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () =>
                        {
                            return valueContent == null || valueContent.Trim().Length < 1 ||
                                valueContent.IndexOf('/') < 1 || valueContent.EndsWith("/");
                        },
                        "The content type of the file. Former known as MIME type. This is " +
                        "mandatory information. See RFC2046.")
                });

            AddKeyValueExRef(
                stack, "contentType", containingObject, valueContent, null, repo,
                v =>
                {
                    valueContent = v as string;
                    setOutput?.Invoke(valuePath, valueContent);
                    this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxIsEditable: true, comboBoxMinWidth: 140,
                comboBoxItems: AdminShellUtil.GetPopularMimeTypes());

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return valuePath == null || valuePath.Trim().Length < 1; },
                            "The path to an external file or a file relative the AASX package root('/'). " +
                                "Files are typically relative to '/aasx/' or sub-directories of it. " +
                                "External files typically comply to an URL, e.g. starting with 'https://..'.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return valuePath.IndexOf('\\') >= 0; },
                            "Backslashes ('\') are not allow. Please use '/' as path delimiter.",
                            severityLevel: HintCheck.Severity.Notice)
                });

            // Further actions

            if (editMode && uploadAssistance != null && packages.Main != null)
            {
                // Remove, create text, edit
                // More file actions
                this.AddActionPanel(
                    stack, "Action",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("remove-file", "Remove existing file",
                            "Removes the file from the AASX environment.")
                        .AddAction("create-text", "Create text file",
                            "Creates a text file and adds it to the AAS environment.")
                        .AddAction("edit-text", "Edit text file",
                            "Edits the associated text file and updates it to the AAS environment."),
                    ticketAction: (buttonNdx, ticket) =>

                    {
                        if (buttonNdx == 0 && valuePath.HasContent())
                        {
                            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            {
                                try
                                {
                                    // try find ..
                                    var psfs = packages.Main.GetListOfSupplementaryFiles();
                                    var psf = psfs?.FindByUri(valuePath);
                                    if (psf == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Not able to locate supplmentary file {valuePath} for removal! " +
                                            $"Aborting!");
                                    }
                                    else
                                    {
                                        Log.Singleton.Info($"Removing file {valuePath} ..");
                                        packages.Main.DeleteSupplementaryFile(psf);
                                        Log.Singleton.Info(
                                            $"Added {valuePath} to pending package items to be deleted. " +
                                            "A save-operation might be required.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"Removing file {valuePath} in package");
                                }

                                // clear value
                                valuePath = "";

                                // value event
                                setOutput?.Invoke(valuePath, valueContent);
                                this.AddDiaryEntry(containingObject, new DiaryEntryUpdateValue());

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
                            this.context?.StartFlyoverModal(uc);
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
                                    $"The supplemental file {ptd + ptfn} is already existing in the " +
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
                                    valueContent = mimeType;
                                    valuePath = targetPath;
                                    setOutput?.Invoke(valuePath, valueContent);

                                    // value + struct event
                                    this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                                    this.AddDiaryEntry(containingObject, new DiaryEntryUpdateValue());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"Creating text-file {ptd + ptfn} within package");
                            }
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: containingObject);
                        }

                        if (buttonNdx == 2)
                        {
                            try
                            {
                                // try find ..
                                var psfs = packages.Main.GetListOfSupplementaryFiles();
                                var psf = psfs?.FindByUri(valuePath);
                                if (psf == null)
                                {
                                    Log.Singleton.Error(
                                        $"Not able to locate supplmentary file {valuePath} for edit. " +
                                        $"Aborting!");
                                    return new AnyUiLambdaActionNone();
                                }

                                // try read ..
                                Log.Singleton.Info($"Reading text-file {valuePath} ..");
                                string contents;
                                using (var stream = packages.Main.GetStreamFromUriOrLocalPackage(valuePath))
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
                                        $"Not able to read contents from  supplmentary file {valuePath} " +
                                        $"for edit. Aborting!");
                                    return new AnyUiLambdaActionNone();
                                }

                                // edit
                                var uc = new AnyUiDialogueDataTextEditor(
                                            caption: $"Edit text-file '{valuePath}'",
                                            mimeType: valueContent,
                                            text: contents);
                                if (!this.context.StartFlyoverModal(uc))
                                    return new AnyUiLambdaActionNone();

                                // save
                                using (var stream = packages.Main.GetStreamFromUriOrLocalPackage(
                                    valuePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                {
                                    using (var sw = new StreamWriter(stream))
                                    {
                                        // write contents
                                        sw.Write(uc.Text);
                                    }
                                }

                                // value event
                                this.AddDiaryEntry(containingObject, new DiaryEntryUpdateValue());
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"Edit text-file {valuePath} in package.");
                            }

                            // reshow
                            return new AnyUiLambdaActionRedrawEntity();
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // Further file assistance
                this.AddGroup(stack, "Supplemental file assistance", this.levelColors.SubSection);

                AddKeyValueExRef(
                    stack, "Target path", this.uploadAssistance, this.uploadAssistance.TargetPath, null, repo,
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

                this.AddActionPanel(
                    stack, "Action",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("select-source", "Select source file",
                            "Select a filename to be added later.")
                        .AddAction("add-to-aasx", "Add or update to AASX",
                            "Add or update file given by selected filename to the AAS environment."),
                    ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var uc = new AnyUiDialogueDataOpenFile(
                                message: "Select a supplemental file to add..");
                                this.context?.StartFlyoverModal(uc);
                                if (uc.Result && uc.TargetFileName != null)
                                {
                                    this.uploadAssistance.SourcePath = uc.TargetFileName;
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
                                        valueContent = mimeType;
                                        valuePath = targetPath;
                                        setOutput?.Invoke(valuePath, valueContent);

                                        // value + struct event
                                        this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                                        this.AddDiaryEntry(containingObject, new DiaryEntryUpdateValue());
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

		//
		//
		// SAMM
		//
		//

        public Type SammExtensionHelperSelectSammType(Type[] addableElements)
        {
			// create choices
			var fol = new List<AnyUiDialogueListItem>();
			foreach (var stp in addableElements)
				fol.Add(new AnyUiDialogueListItem("" + stp.Name, stp));

			// prompt for this list
			var uc = new AnyUiDialogueDataSelectFromList(
				caption: "Select SAMM element type to add ..");
			uc.ListOfItems = fol;
			this.context.StartFlyoverModal(uc);
            if (uc.Result && uc.ResultItem != null && uc.ResultItem.Tag != null &&
                ((Type)uc.ResultItem.Tag).IsAssignableTo(typeof(Samm.ModelElement)))
                return (Type)uc.ResultItem.Tag;
            return null;
		}

        public static void SammExtensionHelperUpdateJson(Aas.IExtension se, Type sammType, Samm.ModelElement sammInst)
        {
            // trivial
            if (se == null || sammType == null || sammInst == null)
                return;

			// do a full fledged, carefull serialization
			string json = "";
			try
			{
				var settings = new JsonSerializerSettings
				{
					// SerializationBinder = new DisplayNameSerializationBinder(new[] { typeof(AasEventMsgEnvelope) }),
					NullValueHandling = NullValueHandling.Ignore,
					ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
					TypeNameHandling = TypeNameHandling.None,
					Formatting = Formatting.Indented
				};
				settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                //settings.Converters.Add(new AdminShellConverters.AdaptiveAasIClassConverter(
                //	AdminShellConverters.AdaptiveAasIClassConverter.ConversionMode.AasCore));
				json = JsonConvert.SerializeObject(sammInst, sammType, settings);
			}
			catch (Exception ex)
			{
				LogInternally.That.SilentlyIgnoredError(ex);
			}

			// save this to the extension
			se.Value = json;
			se.ValueType = DataTypeDefXsd.String;
		}

		public AnyUiLambdaActionBase SammExtensionHelperSammReferenceAction<T>(
			Aas.Environment env,
			Aas.IReferable relatedReferable,
			int actionIndex,
			T sr,
			Action<T> setValue,
			Func<string, T> createInstance,
			string[] presetList = null) where T : SammReference
        {
            if (actionIndex == 0 && presetList != null && presetList.Length > 0)
            {
				// prompt for this list
				var uc = new AnyUiDialogueDataSelectFromList(
					caption: "Select preset value to add ..");
                uc.ListOfItems = presetList.Select((st) => new AnyUiDialogueListItem("" + st, st)).ToList();
				this.context.StartFlyoverModal(uc);
                if (uc.Result && uc.ResultItem != null && uc.ResultItem.Tag != null &&
                    uc.ResultItem.Tag is string prs)
                {
					setValue?.Invoke(createInstance?.Invoke("" + prs));
					return new AnyUiLambdaActionRedrawEntity();
				}
			}

			if (actionIndex == 1)
			{
				var k2 = SmartSelectAasEntityKeys(
					packages,
					PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
					"ConceptDescription");
				if (k2 != null && k2.Count >= 1)
				{
					setValue?.Invoke(createInstance?.Invoke("" + k2[0].Value));
					return new AnyUiLambdaActionRedrawEntity();
				}
			}

			if (actionIndex == 2)
			{
				// select type
				var sammTypeToCreate = SammExtensionHelperSelectSammType(Samm.Constants.AddableElements);
				if (sammTypeToCreate == null)
					return new AnyUiLambdaActionNone();

				// select name
				var newUri = Samm.Util.ShortenUri(
					"" + (relatedReferable as Aas.IIdentifiable)?.Id);
				var uc = new AnyUiDialogueDataTextBox(
					"New Id for SAMM element:",
					symbol: AnyUiMessageBoxImage.Question,
					maxWidth: 1400,
					text: "" + newUri);
				if (!this.context.StartFlyoverModal(uc))
					return new AnyUiLambdaActionNone();
				newUri = uc.Text;

				// select idShort
				var newIdShort = Samm.Util.LastWordOfUri(newUri);
				var uc2 = new AnyUiDialogueDataTextBox(
					"New idShort for SAMM element:",
					symbol: AnyUiMessageBoxImage.Question,
					maxWidth: 1400,
					text: "" + newIdShort);
				if (!this.context.StartFlyoverModal(uc2))
					return new AnyUiLambdaActionNone();
				newIdShort = uc2.Text;
				if (newIdShort.HasContent() != true)
				{
					newIdShort = env?.ConceptDescriptions?
						.IterateIdShortTemplateToBeUnique("samm{0:0000}", 9999);
				}

				// make sure, the name is a new, valid Id for CDs
				if (newUri?.HasContent() != true ||
					null != env?.FindConceptDescriptionById(newUri))
				{
					Log.Singleton.Error("Invalid (used?) Id for a new ConceptDescriptin. Aborting!");
					return new AnyUiLambdaActionNone();
				}

				// add the new name to the current element
				setValue?.Invoke(createInstance?.Invoke(newUri));

				// now create a new CD for the new SAMM element
				var newCD = new Aas.ConceptDescription(
					id: newUri,
					idShort: newIdShort);

				// create new SAMM element 
				var newSamm = Activator.CreateInstance(
					sammTypeToCreate, new object[] { }) as Samm.ModelElement;

				var newSammSsd = newSamm as Samm.ISammSelfDescription;

				var newSammExt = new Aas.Extension(
						name: "" + newSammSsd?.GetSelfName(),
						semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
							(new[] { 
                                new Aas.Key(KeyTypes.GlobalReference,
								"" + Samm.Constants.SelfNamespaces.ExtendUri(newSammSsd.GetSelfUrn())) 
                            })
								.Cast<Aas.IKey>().ToList()),
						value: "");
				newCD.Extensions = new List<IExtension> { newSammExt };

				// fill with empty data content for SAMM
				SammExtensionHelperUpdateJson(newSammExt, sammTypeToCreate, newSamm);

				// save CD
				env?.ConceptDescriptions?.Add(newCD);

				// now, jump to this new CD
				return new AnyUiLambdaActionRedrawAllElements(nextFocus: newCD, isExpanded: true);
			}

			if (actionIndex == 3 && sr?.Value?.HasContent() == true)
			{
				return new AnyUiLambdaActionNavigateTo(
					new Aas.Reference(
							Aas.ReferenceTypes.ModelReference,
							new Aas.IKey[] {
										new Aas.Key(KeyTypes.ConceptDescription, sr.Value)
							}.ToList()));
			}

			return new AnyUiLambdaActionNone();
		}

		public void SammExtensionHelperAddSammReference<T>(
			Aas.Environment env, AnyUiStackPanel stack, string caption,
            Samm.ModelElement sammInst,
            Aas.IReferable relatedReferable,
			T sr,
            Action<T> setValue,
			Func<string, T> createInstance,
			bool noFirstColumnWidth = false,
			string[] presetList = null,
            bool showButtons = true,
			bool editOptionalFlag = false) where T : SammReference
		{
            var grid = AddSmallGrid(1, 2, colWidths: new[] { "*", "#" });
            stack.Add(grid);
            var g1stack = AddSmallStackPanelTo(grid, 0, 0, margin: new AnyUiThickness(0));

			AddKeyValueExRef(
				g1stack, "" + caption, sammInst,
				value: "" + sr?.Value, null, repo,
				setValue: v =>
				{
                    setValue?.Invoke(createInstance?.Invoke((string)v));
					return new AnyUiLambdaActionNone();
				},
                keyVertCenter: true,
                noFirstColumnWidth: noFirstColumnWidth,
				auxButtonTitles: !showButtons ? null : new[] { "Preset", "Existing", "New", "Jump" },
				auxButtonToolTips: !showButtons ? null : new[] {
                    "Select from given presets.",
					"Select existing ConceptDescription.",
					"Create a new ConceptDescription for SAMM use.",
					"Jump to ConceptDescription with given Id."
				},
				auxButtonLambda: (i) =>
				{
                    return SammExtensionHelperSammReferenceAction<T>(
                        env, relatedReferable, 
                        i,
                        sr: sr, 
                        setValue: setValue, 
                        createInstance: createInstance,
						presetList: presetList);
				});

            if (editOptionalFlag && sr is OptionalSammReference osr)
            {
				AnyUiUIElement.RegisterControl(
					AddSmallCheckBoxTo(grid, 0, 1,
					    margin: new AnyUiThickness(2, 2, 2, 2),
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center,
					    content: "Opt.",
                        isChecked: osr.Optional),
					    (v) =>
					    {
                            osr.Optional = (bool) v;
						    setValue?.Invoke(sr);
						    return new AnyUiLambdaActionNone();
					    });
			}
		}

		public void SammExtensionHelperAddListOfSammReference<T>(
			Aas.Environment env, AnyUiStackPanel stack, string caption,
			Samm.ModelElement sammInst,
			Aas.IReferable relatedReferable,
			List<T> value,
			Action<List<T>> setValue,
            Func<string,T> createInstance,
            bool editOptionalFlag) where T : SammReference
        {
			this.AddVerticalSpace(stack);

			if (this.SafeguardAccess(stack, repo, value, "" + caption + ":",
				"Create data element!",
				v => {
					setValue?.Invoke(new List<T>(new T[] { createInstance?.Invoke("") }));
					return new AnyUiLambdaActionRedrawEntity();
				}))
			{
				// Head
				var sg = this.AddSubGrid(stack, "" + caption + ":",
				rows: 1 + value.Count, cols: 2,
				minWidthFirstCol: GetWidth(FirstColumnWidth.Standard),
				paddingCaption: new AnyUiThickness(5, 0, 0, 0),
				colWidths: new[] { "*", "#" });

				AnyUiUIElement.RegisterControl(
					AddSmallButtonTo(sg, 0, 1,
					margin: new AnyUiThickness(2, 2, 2, 2),
					padding: new AnyUiThickness(1, 0, 1, 0),
					content: "\u2795"),
					(v) =>
					{
						value.Add(createInstance?.Invoke(""));
						setValue?.Invoke(value);
						return new AnyUiLambdaActionRedrawEntity();
					});

				// individual references
				for (int lsri = 0; lsri < value.Count; lsri++)
				{
					// remember lambda safe
					var theLsri = lsri;

					// Stack in the 1st column
					var sp1 = AddSmallStackPanelTo(sg, 1 + lsri, 0);
					SammExtensionHelperAddSammReference(
						env, sp1, $"[{1 + lsri}]",
						(Samm.ModelElement)sammInst, relatedReferable,
						value[lsri],
						noFirstColumnWidth: true,
						showButtons: false,
                        editOptionalFlag: editOptionalFlag,
						setValue: (v) => {
							value[theLsri] = v;
							setValue?.Invoke(value);
						},
                        createInstance: createInstance);

					if (false)
					{
						// remove button
						AnyUiUIElement.RegisterControl(
						    AddSmallButtonTo(sg, 1 + lsri, 1,
						    margin: new AnyUiThickness(2, 2, 2, 2),
						    padding: new AnyUiThickness(5, 0, 5, 0),
						    content: "-"),
						    (v) =>
						    {
							    value.RemoveAt(theLsri);
							    setValue?.Invoke(value);
							    return new AnyUiLambdaActionRedrawEntity();
						    });
					}
					else
					{
						// button [hamburger]
						AddSmallContextMenuItemTo(
							sg, 1 + lsri, 1,
							"\u22ee",
							repo, new[] {
								"\u2702", "Delete",
								"\u25b2", "Move Up",
								"\u25bc", "Move Down",
								"\U0001F4D1", "Select from preset",
								"\U0001F517", "Select from existing CDs",
								"\U0001f516", "Create new CD for SAMM",
								"\U0001f872", "Jump to"
							},
							margin: new AnyUiThickness(2, 2, 2, 2),
							padding: new AnyUiThickness(5, 0, 5, 0),
							menuItemLambda: (o) =>
							{
								var action = false;

								if (o is int ti)
									switch (ti)
									{
										case 0:
											value.RemoveAt(theLsri);
											action = true;
											break;
										case 1:
											MoveElementInListUpwards<T>(value, value[theLsri]);
											action = true;
											break;
										case 2:
											MoveElementInListDownwards<T>(value, value[theLsri]);
											action = true;
											break;
										case 3:
										case 4:
										case 5:
										case 6:
											return SammExtensionHelperSammReferenceAction<T>(
												env, relatedReferable,
												sr: value[theLsri],
												actionIndex: ti - 3,
												presetList: null,
												setValue: (srv) =>
												{
													value[theLsri] = srv;
													setValue?.Invoke(value);
												},
                                                createInstance: createInstance);
									}

								if (action)
								{
									setValue?.Invoke(value);
									return new AnyUiLambdaActionRedrawEntity();
								}
								return new AnyUiLambdaActionNone();
							});
					}
				}
			}

		}

		/// <summary>
		/// Shall provide rather quick access to information ..
		/// </summary>
		public static Type CheckReferableForSammExtensionType(Aas.IReferable rf)
        {
            // access
            if (rf?.Extensions == null)
                return null;

            // find any?
            foreach (var se in rf.Extensions)
            {
				var sammType = Samm.Util.GetTypeFromUrn(Samm.Util.GetSammUrn(se));
                if (sammType != null)
                    return sammType;
			}

            // no?
            return null;
        }

        public static IEnumerable<ModelElement> CheckReferableForSammElements(Aas.IReferable rf)
        {
            // access
            if (rf?.Extensions == null)
                yield break;

            // find any?
            foreach (var se in rf.Extensions)
            { 
				// get type 
			    var sammType = Samm.Util.GetTypeFromUrn(Samm.Util.GetSammUrn(se));
                if (sammType == null)
                    continue;

    			// get instance data
	    		ModelElement sammInst = null;
		
                // try to de-serializa extension value
				try
				{
					if (se.Value != null)
						sammInst = JsonConvert.DeserializeObject(se.Value, sammType) as ModelElement;
				}
				catch (Exception ex)
				{
					LogInternally.That.SilentlyIgnoredError(ex);
					sammInst = null;
				}

                if (sammInst == null)
                    continue;

                // give back
                yield return sammInst;
			}
		}

		/// <summary>
		/// Shall provide rather quick access to information ..
		/// </summary>
		/// <returns>Null, if not a SAMM model element</returns>
		public static string CheckReferableForSammExtensionTypeName(Type sammType)
		{
			return Samm.Util.GetNameFromSammType(sammType);
		}

		public void DisplayOrEditEntitySammExtensions(
			Aas.Environment env, AnyUiStackPanel stack,
			List<Aas.IExtension> sammExtension,
			Action<List<Aas.IExtension>> setOutput,
			string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
			Aas.IReferable relatedReferable = null,
			AasxMenu superMenu = null)
		{
			// access
			if (stack == null)
				return;

			// members
			this.AddGroup(stack, "SAMM extensions \u00ab experimental \u00bb :", levelColors.MainSection);

			this.AddHintBubble(
				stack, hintMode,
				new[] {
					new HintCheck(
						() => { return sammExtension == null ||
							sammExtension.Count < 1; },
						"Eclipse Semantic Aspect Meta Model (SAMM) allows the creation of models to describe " +
                        "the semantics of digital twins by defining their domain specific aspects. " + 
                        "This version of the AASX Package Explorer allows expressing Characteristics of SAMM " +
                        "as an extension of ConceptDescriptions. In later versions, this is assumed to be " +
                        "realized by DataSpecifications.",
						breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
					new HintCheck(
						() => { return sammExtension.Where(p => Samm.Util.HasSammSemanticId(p)).Count() > 1; },
						"Only one SAMM extension is allowed per concept.",
						breakIfTrue: true),
				});
			if (this.SafeguardAccess(
					stack, this.repo, sammExtension, "SAMM extensions:", "Create data element!",
					v =>
					{
						setOutput?.Invoke(new List<Aas.IExtension>());
						return new AnyUiLambdaActionRedrawEntity();
					}))
			{
				// head control
				if (editMode)
				{
					// let the user control the number of references
					this.AddActionPanel(
						stack, "Spec. records:", repo: repo,
						superMenu: superMenu,
						ticketMenu: new AasxMenu()
							.AddAction("add-aspect", "Add Aspect",
								"Add single top level of any SAMM aspect model.")
							.AddAction("add-property", "Add Property",
								"Add a named value element to the aspect or its sub-entities.")
							.AddAction("add-characteristic", "Add Characteristic",
								"Characteristics describe abstract concepts that must be made specific when they are used.")
							.AddAction("auto-entity", "Add Entity",
								"An entity is the main element to collect a set of properties.")
							.AddAction("auto-other", "Add other ..",
								"Adds an other Characteristic by selecting from a list.")
							.AddAction("delete-last", "Delete last extension",
								"Deletes last extension."),
						ticketAction: (buttonNdx, ticket) =>
						{
                            Samm.ModelElement newChar = null;
                            switch (buttonNdx)
                            {
                                case 0: 
                                    newChar = new Samm.Aspect();
                                    break;
								case 1:
									newChar = new Samm.Property();
									break;
								case 2:
									newChar = new Samm.Characteristic();
									break;
								case 3:
									newChar = new Samm.Entity();
									break;
							}

                            if (buttonNdx == 4)
                            {
                                // select
                                var sammTypeToCreate = SammExtensionHelperSelectSammType(Samm.Constants.AddableElements);

								if (sammTypeToCreate != null)
								{
                                    // to which?
                                    newChar = Activator.CreateInstance(
										sammTypeToCreate, new object[] { }) as Samm.ModelElement;
								}
							
							    if (newChar != null && newChar is Samm.ISammSelfDescription ssd)
                                    sammExtension.Add(
                                        new Aas.Extension(
                                            name: ssd.GetSelfName(),
                                            semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
                                                (new[] { 
                                                    new Aas.Key(KeyTypes.GlobalReference,
													"" + Samm.Constants.SelfNamespaces.ExtendUri(ssd.GetSelfUrn())) 
                                                })
                                                .Cast<Aas.IKey>().ToList()),
                                            value: ""));
							}

							if (buttonNdx == 5)
							{
								if (sammExtension.Count > 0)
									sammExtension.RemoveAt(sammExtension.Count - 1);
								else
									setOutput?.Invoke(null);
							}

							this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
							return new AnyUiLambdaActionRedrawEntity();
						});
				}

				// now use the normal mechanism to deal with editMode or not ..
				if (sammExtension != null && sammExtension.Count > 0)
				{
                    var numSammExt = 0;

					for (int i = 0; i < sammExtension.Count; i++)
					{
                        // get type 
						var se = sammExtension[i];
                        var sammType = Samm.Util.GetTypeFromUrn(Samm.Util.GetSammUrn(se));
                        if (sammType == null)
                        {
                            continue;
                        }

						// more then one?
						this.AddHintBubble(
							stack, hintMode,
							new[] {
                            new HintCheck(
                                () => numSammExt > 0,
                                "Only one SAMM extension per ConceptDescription allowed!",
                                breakIfTrue: true)});

						// indicate
						numSammExt++;

                        AnyUiFrameworkElement iconElem = null;
                        var ri = Samm.Constants.GetRenderInfo(sammType);
                        if (ri != null)
                        {
                            iconElem = new AnyUiBorder()
                            {
                                Background = new AnyUiBrush(ri.Background),
                                BorderBrush = new AnyUiBrush(ri.Foreground),
                                BorderThickness = new AnyUiThickness(2.0f),
                                MinHeight = 50,
                                MinWidth = 50,
                                Child = new AnyUiTextBlock()
                                {
                                    Text = "" + ri.Abbreviation,
									HorizontalAlignment = AnyUiHorizontalAlignment.Center,
									VerticalAlignment = AnyUiVerticalAlignment.Center,
									Foreground = new AnyUiBrush(ri.Foreground),
                                    Background = AnyUi.AnyUiBrushes.Transparent, 
                                    FontSize = 2.0,
                                    FontWeight = AnyUiFontWeight.Bold
                                },
								HorizontalAlignment = AnyUiHorizontalAlignment.Center,
								VerticalAlignment = AnyUiVerticalAlignment.Center,
								Margin = new AnyUiThickness(5, 0, 10, 0),
                                SkipForTarget = AnyUiTargetPlatform.Browser
                            };
                        }

                        this.AddGroup(stack, $"SAMM extension [{i + 1}]: {sammType.Name}",
                            levelColors.SubSection.Bg, levelColors.SubSection.Fg,
                            iconElement: iconElem);

						// get instance data
						object sammInst = null;
                        if (false)
                        {
                            // Note: right now, create fresh instance
                            sammInst = Activator.CreateInstance(sammType, new object[] { });
                            if (sammInst == null)
                            {
                                stack.Add(new AnyUiLabel() { Content = "(unable to create instance data)" });
                                continue;
                            }
                        }
                        else
                        {
							// try to de-serializa extension value
							try
							{
                                if (se.Value != null)
                                    sammInst = JsonConvert.DeserializeObject(se.Value, sammType);
							}
							catch (Exception ex)
							{
								LogInternally.That.SilentlyIgnoredError(ex);
                                sammInst = null;
							}

                            if (sammInst == null)
                            {
								sammInst = Activator.CreateInstance(sammType, new object[] { });
							}
						}

                        // editing actions need to asynchronously write back values
                        Action WriteSammInstBack = () =>
                        {
                            SammExtensionHelperUpdateJson(se, sammType, sammInst as Samm.ModelElement);
                        };

                        // okay, try to build up a edit field by reflection
                        var propInfo = sammInst.GetType().GetProperties();
                        for (int pi=0; pi < propInfo.Length; pi++)
                        {
                            //// is the object marked to be skipped?
                            //var x3 = pi.GetCustomAttribute<AdminShell.SkipForReflection>();
                            //if (x3 != null)
                            //	continue;

                            var pii = propInfo[pi];

                            // List of SammReference?
                            if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.SammReference>)))
                            {
                                SammExtensionHelperAddListOfSammReference<Samm.SammReference>(
                                    env, stack, caption: "" + pii.Name,
                                    (ModelElement)sammInst,
                                    relatedReferable,
                                    editOptionalFlag: false,
                                    value: (List<Samm.SammReference>)pii.GetValue(sammInst),
                                    setValue: (v) =>
                                    {
                                        pii.SetValue(sammInst, v);
                                        WriteSammInstBack();
                                    },
                                    createInstance: (sr) => new SammReference(sr));
							}

							// List of optional SammReference?
							if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.OptionalSammReference>)))
							{
								SammExtensionHelperAddListOfSammReference<Samm.OptionalSammReference>(
									env, stack, caption: "" + pii.Name,
									(ModelElement)sammInst,
									relatedReferable,
                                    editOptionalFlag: true,
									value: (List<Samm.OptionalSammReference>)pii.GetValue(sammInst),
									setValue: (v) =>
									{
										pii.SetValue(sammInst, v);
										WriteSammInstBack();
									},
									createInstance: (sr) => new OptionalSammReference(sr));
							}

							// NamespaceMap
							if (pii.PropertyType.IsAssignableTo(typeof(Samm.NamespaceMap)))
                            {
								this.AddVerticalSpace(stack);
								
                                var lsr = (Samm.NamespaceMap)pii.GetValue(sammInst);

                                Action<Samm.NamespaceMap> lambdaSetValue = (v) =>
                                {
                                    pii.SetValue(sammInst, v);
                                    WriteSammInstBack();
                                };

                                if (this.SafeguardAccess(stack, repo, lsr, "" + pii.Name + ":",
                                    "Create data element!",
                                    v =>
                                    {
                                        lambdaSetValue(new Samm.NamespaceMap());
                                        return new AnyUiLambdaActionRedrawEntity();
                                    }))
                                {
                                    // Head
                                    var sg = this.AddSubGrid(stack, "" + pii.Name + ":",
                                                rows: 1 + lsr.Count(), cols: 3,
                                                minWidthFirstCol: GetWidth(FirstColumnWidth.Standard),
												paddingCaption: new AnyUiThickness(5, 0, 0, 0),
												colWidths: new[] { "80:", "*", "#" });

                                    AnyUiUIElement.RegisterControl(
                                        AddSmallButtonTo(sg, 0, 2,
                                        margin: new AnyUiThickness(2, 2, 2, 2),
                                        padding: new AnyUiThickness(1, 0, 1, 0),
                                        content: "\u2795"),
                                        (v) =>
                                        {
                                            lsr.AddOrIgnore(":", "");
                                            lambdaSetValue(lsr);
                                            return new AnyUiLambdaActionRedrawEntity();
                                        });

                                    // individual references
                                    for (int lsri = 0; lsri < lsr.Count(); lsri++)
                                    {
										var theLsri = lsri;

                                        // prefix										
                                        AnyUiUIElement.RegisterControl(
											AddSmallTextBoxTo(sg, 1 + theLsri, 0,
											    text: lsr[theLsri].Prefix,
											    margin: new AnyUiThickness(4, 2, 2, 2)),
											    (v) =>
											    {
													lsr[theLsri].Prefix = (string)v;
												    pii.SetValue(sammInst, lsr);
												    WriteSammInstBack();
												    return new AnyUiLambdaActionNone();
											    });

										// uri										
										AnyUiUIElement.RegisterControl(
											AddSmallTextBoxTo(sg, 1 + theLsri, 1,
												text: lsr[theLsri].Uri,
												margin: new AnyUiThickness(2, 2, 2, 2)),
												(v) =>
												{
													lsr[theLsri].Uri = (string)v;
													pii.SetValue(sammInst, lsr);
													WriteSammInstBack();
													return new AnyUiLambdaActionNone();
												});

                                        // minus
										AnyUiUIElement.RegisterControl(
										    AddSmallButtonTo(sg, 1 + theLsri, 2,
										    margin: new AnyUiThickness(2, 2, 2, 2),
										    padding: new AnyUiThickness(5, 0, 5, 0),
										    content: "-"),
										    (v) =>
										    {
											    lsr.RemoveAt(theLsri);
											    pii.SetValue(sammInst, lsr);
											    WriteSammInstBack();
											    return new AnyUiLambdaActionRedrawEntity();
										    });
									}
								}
                            }

							// List of Constraint?
							if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.Constraint>)))
							{
								;
							}

							// single SammReference?
							if (pii.PropertyType.IsAssignableTo(typeof(Samm.SammReference)))
							{
								this.AddVerticalSpace(stack);
								
                                var sr = (Samm.SammReference)pii.GetValue(sammInst);

                                // preset attribute
                                string[] presetValues = null;
                                var x3 = pii.GetCustomAttribute<Samm.SammPresetListAttribute>();
                                if (x3 != null)
                                {
                                    presetValues = Samm.Constants.GetPresetsForListName(x3.PresetListName);
                                }

                                SammExtensionHelperAddSammReference<SammReference>(
                                    env, stack, "" + pii.Name, (Samm.ModelElement) sammInst, relatedReferable,
                                    sr,
                                    presetList: presetValues,
                                    setValue: (v) => {
										pii.SetValue(sammInst, v);
										WriteSammInstBack();
									},
                                    createInstance: (sr) => new SammReference(sr));
							}

							// List of string?
							if (pii.PropertyType.IsAssignableTo(typeof(List<string>)))
							{
								this.AddVerticalSpace(stack);
								
                                var ls = (List<string>)pii.GetValue(sammInst);
                                if (ls == null)
                                {
                                    // Log.Singleton.Error("Internal error in SAMM element. Aborting.");
                                    continue;
                                }

                                var sg = this.AddSubGrid(stack, "" + pii.Name + ":",
                                    rows: 1 + ls.Count, cols: 2,
                                    minWidthFirstCol: GetWidth(FirstColumnWidth.Standard),
									paddingCaption: new AnyUiThickness(5, 0, 0, 0),
									colWidths: new[] { "*", "#" });

                                AnyUiUIElement.RegisterControl(
                                    AddSmallButtonTo(sg, 0, 1, 
									    margin: new AnyUiThickness(2, 2, 2, 2),
						                padding: new AnyUiThickness(5, 0, 5, 0),
									    content: "Add blank"),
                                        (v) =>
                                        {
                                            ls.Add("");
                                            pii.SetValue(sammInst, ls);
                                            WriteSammInstBack();
                                            return new AnyUiLambdaActionRedrawEntity();
                                        });

                                for (int lsi=0; lsi<ls.Count; lsi++)
                                {
                                    var theLsi = lsi;
									var tb = AnyUiUIElement.RegisterControl(
									    AddSmallTextBoxTo(sg, 1+lsi, 0, 
                                            text: ls[lsi],
										    margin: new AnyUiThickness(2, 2, 2, 2)),
										    (v) =>
										    {
                                                ls[theLsi] = (string)v;
											    pii.SetValue(sammInst, ls);
											    WriteSammInstBack();
											    return new AnyUiLambdaActionRedrawEntity();
										    });

									AnyUiUIElement.RegisterControl(
									    AddSmallButtonTo(sg, 1 + lsi, 1,
									        margin: new AnyUiThickness(2, 2, 2, 2),
									        padding: new AnyUiThickness(5, 0, 5, 0),
									        content: "-"),
									        (v) =>
									        {
                                                ls.RemoveAt(theLsi);
										        pii.SetValue(sammInst, ls);
										        WriteSammInstBack();
										        return new AnyUiLambdaActionRedrawEntity();
									        });
								}
							}

							// single string?
							if (pii.PropertyType.IsAssignableTo(typeof(string)))
							{
								var isMultiLineAttr = pii.GetCustomAttribute<Samm.SammMultiLineAttribute>();

                                Func<object, AnyUiLambdaActionBase> setValueLambda = (v) =>
                                {
                                    pii.SetValue(sammInst, v);
                                    WriteSammInstBack();
                                    return new AnyUiLambdaActionNone();
                                };

                                if (isMultiLineAttr == null)
                                {
                                    // 1 line
                                    AddKeyValueExRef(
                                        stack, "" + pii.Name, sammInst, (string)pii.GetValue(sammInst), null, repo,
                                        setValue: setValueLambda);
                                } 
                                else
                                {
                                    // makes sense to have a bit vertical space
                                    AddVerticalSpace(stack);

									// multi line
									AddKeyValueExRef(
										stack, "" + pii.Name, sammInst, (string)pii.GetValue(sammInst), null, repo,
										setValue: setValueLambda,
                                        limitToOneRowForNoEdit: true,
                                        maxLines: isMultiLineAttr.MaxLines.Value,
						                auxButtonTitles: new[] { "\u2261" },
						                auxButtonToolTips: new[] { "Edit in multiline editor" },
						                auxButtonLambda: (buttonNdx) =>
						                {
							                if (buttonNdx == 0)
							                {
								                var uc = new AnyUiDialogueDataTextEditor(
													caption: $"Edit " + pii.Name,
													mimeType: System.Net.Mime.MediaTypeNames.Text.Plain,
													text: (string)pii.GetValue(sammInst));
								                if (this.context.StartFlyoverModal(uc))
								                {
													pii.SetValue(sammInst, uc.Text);
													WriteSammInstBack();
													return new AnyUiLambdaActionRedrawEntity();
								                }
							                }
							                return new AnyUiLambdaActionNone();
						                });
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Parses an rdf:Collection and reads out either <c>SammReference</c> or <c>OptionalSammReference</c>
		/// </summary>
		public static List<T> ImportSammModelParseRdfCollection<T>(
            IGraph g,
			INode collectionStart,
            Func<string, bool, T> createInstance) where T : SammReference
        {
            // Try parse a rdf:Collection
            // see: https://ontola.io/blog/ordered-data-in-rdf

            var lsr = new List<T>();
			INode collPtr = collectionStart;

            if (collPtr != null && (collPtr.NodeType == NodeType.Uri || collPtr.NodeType == NodeType.Literal))
            {
				// only a single member is given
				lsr.Add(createInstance?.Invoke(RdfHelper.GetLiteralStrValue(collPtr), false));
			}
            else
            {
                // a chain of instances is given
                while (collPtr != null && collPtr.NodeType == NodeType.Blank)
                {
                    // the collection pointer needs to have a first relationship
                    var firstRel = g.GetTriplesWithSubjectPredicate(
                        subj: collPtr,
                        pred: new UriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#first")))
                                    .FirstOrDefault();
                    if (firstRel?.Object == null)
                        break;

                    // investigate, if first.object is a automatic/composite or an end node
                    if (firstRel.Object.NodeType == NodeType.Uri
                        || firstRel.Object.NodeType == NodeType.Literal)
                    {
                        // first.object is something tangible
                        lsr.Add(createInstance?.Invoke(firstRel.Object.ToSafeString(), false));
                    }
                    else
                    {
                        // crawl firstRel.Object further to get individual end notes
                        string propElem = null;
                        bool? optional = null;

                        foreach (var x3 in g.GetTriplesWithSubject(firstRel.Object))
                        {
                            if (x3.Predicate.Equals(new UriNode(
                                    new Uri("urn:bamm:io.openmanufacturing:meta-model:1.0.0#property"))))
                                propElem = x3.Object.ToSafeString();
                            if (x3.Predicate.Equals(
                                    new UriNode(new Uri("urn:bamm:io.openmanufacturing:meta-model:1.0.0#optional"))))
                                optional = x3.Object.ToSafeString() == "true^^http://www.w3.org/2001/XMLSchema#boolean";
                        }

                        if (propElem != null)
                            lsr.Add(createInstance?.Invoke(propElem, optional.Value));
                    }

                    // iterate further
                    var restRel = g.GetTriplesWithSubjectPredicate(
                        subj: collPtr,
                        pred: new UriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#rest")))
                                .FirstOrDefault();
                    collPtr = restRel?.Object;
                }
            }

            return lsr;
		}

        public static class RdfHelper
        {
            public static string GetLiteralStrValue(INode node)
            {
                if (node == null)
                    return "";
                if (node is LiteralNode ln)
                    return ln.Value;
                return node.ToSafeString();
            }
        }

        public static void ImportSammModelToConceptDescriptions(
			Aas.Environment env, 
            string fn)
        {
			// do it
			IGraph g = new Graph();
			TurtleParser ttlparser = new TurtleParser();

            // Load text to find header comments
            Log.Singleton.Info($"Reading SAMM file for text cmments: {fn} ..");
            var globalComments = string.Join(System.Environment.NewLine,
                    System.IO.File.ReadAllLines(fn)
                        .Where((ln) => ln.Trim().StartsWith('#')));

			// Load graph using a Filename
			Log.Singleton.Info($"Reading SAMM file for tutle graph: {fn} ..");
			ttlparser.Load(g, fn);

            // Load namespace map
            var globalNamespaces = new Samm.NamespaceMap();
            if (g.NamespaceMap != null)
                foreach (var pf in g.NamespaceMap.Prefixes)
                {
                    var prefix = pf.Trim();
                    if (!prefix.EndsWith(':'))
                        prefix += ":";
                    globalNamespaces.AddOrIgnore(prefix, g.NamespaceMap.GetNamespaceUri(pf).ToSafeString());
                }

			// find all potential SAMM elements " :xxx a bamm:XXXX"
			foreach (var trpSammElem in g.GetTriplesWithPredicate(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")))
			{
                // check, if there is a SAMM type behind the object
                var sammElemUri = trpSammElem.Object.ToString();
				var sammType = Samm.Util.GetTypeFromUrn(sammElemUri);
                if (sammType == null)
                {
                    Log.Singleton.Info($"Potential SAMM element found but unknown URI={sammElemUri}");
                    continue;
                }

				// okay, create an instance
				var sammInst = Activator.CreateInstance(sammType, new object[] { }) as Samm.ModelElement;
                if (sammInst == null)
                {
                    Log.Singleton.Error($"Error creating instance for SAMM element URI={sammElemUri}");
                    continue;
                }
                
				// okay, try to find elements driven by the properties in the class
                // by reflection
				var propInfo = sammInst.GetType().GetProperties();
                for (int pi = 0; pi < propInfo.Length; pi++)
                {
                    //// is the object marked to be skipped?
                    //var x3 = pi.GetCustomAttribute<AdminShell.SkipForReflection>();
                    //if (x3 != null)
                    //	continue;
                    var pii = propInfo[pi];

                    // need to have a custom attribute to identify the subject uri of the turtle triples
                    var propSearchUri = pii.GetCustomAttribute<Samm.SammPropertyUriAttribute>()?.Uri;
                    if (propSearchUri == null)
                        continue;

                    // extend this
                    propSearchUri = Samm.Constants.SelfNamespaces.ExtendUri(propSearchUri);

					//// now try to find triples with:
					//// Subject = trpSammElem.Subject and
					//// Predicate = propSearchUri
					foreach (var trpProp in g.GetTriplesWithSubjectPredicate(
						subj: trpSammElem.Subject,
						pred: new VDS.RDF.UriNode(new Uri(propSearchUri))))
                    {
                        // now let the property type decide, how to 
                        // put in the property

                        var objStr = RdfHelper.GetLiteralStrValue(trpProp.Object);

						// List of Samm.LangString
						if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.LangString>)))
                        {
							// multiple triples; each will go into one LangStr
							var m = Regex.Match(objStr, @"(.*?)@([A-Za-z_-]+)");
                            var ls = (!m.Success)
                                ? new Samm.LangString("en?", "" + objStr)
                                : new Samm.LangString(m.Groups[2].ToSafeString(), m.Groups[1].ToSafeString());

                            // now, access the property
                            var lls = (List<Samm.LangString>)pii.GetValue(sammInst);
                            if (lls == null)
                                lls = new List<Samm.LangString>();
                            lls.Add(ls);
							pii.SetValue(sammInst, lls);
						}

						// List of string
						if (pii.PropertyType.IsAssignableTo(typeof(List<string>)))
						{
							// now, access the property
							var lls = (List<string>)pii.GetValue(sammInst);
							if (lls == null)
								lls = new List<string>();
							lls.Add(objStr);
							pii.SetValue(sammInst, lls);
						}

						// List of SammReference
						if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.SammReference>)))
                        {
							var lsr = (List<Samm.SammReference>)pii.GetValue(sammInst);
							if (lsr == null)
								lsr = new List<Samm.SammReference>();

                            lsr.AddRange(
                                ImportSammModelParseRdfCollection(
                                    g, collectionStart: trpProp.Object,
                                    createInstance: (sr, opt) => new SammReference(sr)));

                            // write found references back
							pii.SetValue(sammInst, lsr);
						}

						// List of OptionalSammReference
						if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.OptionalSammReference>)))
						{
							var lsr = (List<Samm.OptionalSammReference>)pii.GetValue(sammInst);
							if (lsr == null)
								lsr = new List<Samm.OptionalSammReference>();

							lsr.AddRange(
								ImportSammModelParseRdfCollection(
									g, collectionStart: trpProp.Object,
									createInstance: (sr, opt) => new OptionalSammReference(sr, opt)));

							// write found references back
							pii.SetValue(sammInst, lsr);
						}

						// just SammReference
						if (pii.PropertyType.IsAssignableTo(typeof(Samm.SammReference)))
						{
                            // simply set the value
							pii.SetValue(sammInst, new SammReference(objStr));
						}

						// just string
						if (pii.PropertyType.IsAssignableTo(typeof(string)))
						{
							// simply set the value
							pii.SetValue(sammInst, objStr);
						}
					}
				}

				// description of Referable is a special case
				List<Aas.LangStringTextType> cdDesc = null;
                var descPred = Samm.Constants.SelfNamespaces.ExtendUri("bamm:description");
                foreach (var trpProp in g.GetTriplesWithSubjectPredicate(
						subj: trpSammElem.Subject,
						pred: new VDS.RDF.UriNode(new Uri(descPred))))
                {
					// decompose
					var objStr = trpProp.Object.ToSafeString();
					var m = Regex.Match(objStr, @"(.*?)@([A-Za-z_-]+)");
					var ls = (!m.Success)
						? new Aas.LangStringTextType("en?", "" + objStr)
						: new Aas.LangStringTextType(m.Groups[2].ToSafeString(), m.Groups[1].ToSafeString());

                    // add
                    if (cdDesc == null)
                        cdDesc = new List<LangStringTextType>();
                    cdDesc.Add(ls);
				}

                // name of elements is a special case. Can become idShort
                string elemName = null;
				var elemPred = Samm.Constants.SelfNamespaces.ExtendUri("bamm:name");
                foreach (var trpProp in g.GetTriplesWithSubjectPredicate(
                        subj: trpSammElem.Subject,
                        pred: new VDS.RDF.UriNode(new Uri(elemPred))))
                {
                    elemName = RdfHelper.GetLiteralStrValue(trpProp.Object);
				}

				// Aspect is another special case
				if (sammInst is Samm.Aspect siAspect)
                {
                    siAspect.Namespaces = globalNamespaces;
                    siAspect.Comments = globalComments;
                }

				// after this, the sammInst is fine; we need to prepare the outside

				// which identifiers?
				var newId = trpSammElem.Subject.ToSafeString();
				var newIdShort = Samm.Util.LastWordOfUri(newId);
                if (elemName?.HasContent() == true)
                    newIdShort = elemName;
				if (newIdShort.HasContent() != true)
				{
					newIdShort = env?.ConceptDescriptions?
						.IterateIdShortTemplateToBeUnique("samm{0:0000}", 9999);
				}

				// now create a new CD for the new SAMM element
				var newCD = new Aas.ConceptDescription(
					id: newId,
					idShort: newIdShort,
                    description: cdDesc?.Cast<Aas.ILangStringTextType>().ToList());

				// create new SAMM element 
				var newSammSsd = sammInst as Samm.ISammSelfDescription;
				var newSammExt = new Aas.Extension(
						name: "" + newSammSsd?.GetSelfName(),
						semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
							(new[] { 
                                new Aas.Key(KeyTypes.GlobalReference,
								"" + Samm.Constants.SelfNamespaces.ExtendUri(newSammSsd.GetSelfUrn()))
                            })
							.Cast<Aas.IKey>().ToList()),
						value: "");
				newCD.Extensions = new List<IExtension> { newSammExt };

				// fill with empty data content for SAMM
				SammExtensionHelperUpdateJson(newSammExt, sammType, sammInst);

				// save CD
				env?.ConceptDescriptions?.Add(newCD);
			}
		}
	}
}
