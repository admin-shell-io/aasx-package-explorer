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
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0_RC02;
//using AasxCompatibilityModels;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Extensions;
using static AasxPackageLogic.DispEditHelperEntities;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class extends the basic helper functionalities of DispEditHelper by providing modules for display/
    /// editing disting modules of the GUI, such as the different (re-usable) Interfaces of the AAS entities
    /// </summary>
    //public class DispEditHelperModules : DispEditHelperCopyPaste
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
            Aas.IReferable referable,
            DispEditInjectAction injectToIdShort = null,
            HintCheck[] addHintsCategory = null,
            bool categoryUsual = false)
        {
            // access
            if (stack == null || referable == null)
                return;

            // members
            this.AddGroup(stack, "Referable:", levelColors.SubSection);

            // members
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
                auxButtonLambda: injectToIdShort?.auxLambda
                );

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => referable.DisplayName?.IsValid() != true,
                        "The use of a Display name is recommended to express a human readable name " +
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
                referable.DisplayName = new List<Aas.LangString>(new List<Aas.LangString>());
                this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddKeyListLangStr(stack, "displayName", referable.DisplayName,
                    repo, relatedReferable: referable);
            }

            if (!categoryUsual)
                this.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(() => referable.Category?.HasContent() == true,
                    "The use of category is deprecated.", severityLevel: HintCheck.Severity.Notice));

            this.AddHintBubble(stack, hintMode, 
                this.ConcatHintChecks(new[] {
                    new HintCheck(
                        () => referable.Category?.HasContent() == true,
                        "The Category is deprecated. Do not plan to use this information in new developments.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice) }, addHintsCategory));
            AddKeyValueExRef(
                stack, "category", referable, referable.Category, null, repo,
                v =>
                {
                    referable.Category = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxItems: new string[] {"CONSTANT", "PARAMETER", "VARIABLE"}, comboBoxIsEditable: true);

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
                referable.Description = new List<Aas.LangString>();
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
                            "of your Administration shell to understand your intentions."));
                this.AddKeyListLangStr(stack, "description", referable.Description,
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
                         //checksum= referable.ComputeHashcode();  //TODO:jtikekar support attributes
                        this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }

                    return new AnyUiLambdaActionNone();
                }
                );
#endif

            // Extensions (at the end to make them not so much impressive!)

            DisplayOrEditEntityListOfExtension(
                stack: stack, extensions: referable.Extensions,
                setOutput: (v) => { referable.Extensions = v; },
                relatedReferable: referable);
        }

        //
        // Extensions
        //

        public void DisplayOrEditEntityListOfExtension(AnyUiStackPanel stack,
            List<Aas.Extension> extensions,
            Action<List<Aas.Extension>> setOutput,
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
                    setOutput?.Invoke(new List<Aas.Extension>());
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
                        return identifiable.Administration.Version.Trim() == "" ||
                            identifiable.Administration.Revision.Trim() == "";
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
            }
        }

        public void DisplayOrEditEntityHasDataSpecificationReferences(AnyUiStackPanel stack,
            List<Aas.Reference> references,
            Action<List<Aas.Reference>> setOutput,
            string[] addPresetNames = null, List<Aas.Key>[] addPresetKeyLists = null,
            bool dataSpecRefsAreUsual = false,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasDataSpecification (Reference):", levelColors.SubSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return !dataSpecRefsAreUsual && references != null
                        && references.Count > 0; },
                    "Check if a data specification is appropriate here. " +
                    "This is only required in a minority of cases.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice) });
            if (this.SafeguardAccess(
                    stack, this.repo, references, "DataSpecification:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<Aas.Reference>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddAction(
                        stack, "Specifications:",
                        new[] { "Add Reference", "Delete last reference" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                references.Add(new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>()));

                            if (buttonNdx == 1)
                            {
                                if (references.Count > 0)
                                    references.RemoveAt(references.Count - 1);
                                else
                                    setOutput?.Invoke(null);
                            }

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (references != null && references.Count > 0)
                {
                    for (int i = 0; i < references.Count; i++)
                    {
                        this.AddHintBubble(stack, hintMode, new[] {
                            new HintCheck(
                                () => references[i]?.IsValid() != true,
                                "A Reference without Keys makes no sense.")});
                        
                        this.AddKeyReference(
                            stack, String.Format("dataSpec.[{0}]", i), references[i], repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            "All",
                            addEclassIrdi: true,
                            relatedReferable: relatedReferable,
                            showRefSemId: false);
                        
                        if (i < references.Count - 1)
                            AddVerticalSpace(stack);
                    }
                }
            }
        } 

        //Added this method only to support embeddedDS from ConceptDescriptions
        public void DisplayOrEditEntityHasDataSpecificationReferences(AnyUiStackPanel stack,
            List<Aas.EmbeddedDataSpecification>? hasDataSpecification,
            Action<List<Aas.EmbeddedDataSpecification>> setOutput,
            string[] addPresetNames = null, List<Aas.Key>[] addPresetKeyLists = null,
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
                        setOutput?.Invoke(new List<Aas.EmbeddedDataSpecification>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddAction(
                        stack, "Specifications:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("add-reference", "Add Aas.Reference",
                                "Adds a reference to a data specification.")
                            .AddAction("delete-reference", "Delete last reference",
                                "Deletes the last reference in the list."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                                hasDataSpecification.Add(
                                    new Aas.EmbeddedDataSpecification(
                                        new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>()),
                                        null));

                            if (buttonNdx == 1)
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
                    //TODO:jtikekar: refactor
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

                        int currentI = i;
                        if (this.SafeguardAccess(
                            stack, this.repo, hasDataSpecification[i].DataSpecification,
                                "DataSpecification:", "Create (inner) data element!",
                            v =>
                            {
                                hasDataSpecification[currentI].DataSpecification =
                                new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>());
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
            List<Aas.EmbeddedDataSpecification> hasDataSpecification,
            Action<List<Aas.EmbeddedDataSpecification>> setOutput,
            string[] addPresetNames = null, List<Aas.Key>[] addPresetKeyLists = null,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
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
                        () => { return hasDataSpecification == null ||
                            hasDataSpecification.Count < 1; },
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
                        setOutput?.Invoke(new List<Aas.EmbeddedDataSpecification>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                // head control
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddAction(
                        stack, "Spec. records:",
                        new[] { "Add record", "Add IEC61360", "Delete last record" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                hasDataSpecification.Add(
                                    new Aas.EmbeddedDataSpecification(
                                        new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>()),
                                        null));

                            if (buttonNdx == 1)
                                hasDataSpecification.Add(
                                    new Aas.EmbeddedDataSpecification(
                                        new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key> { 
                                            ExtendIDataSpecificationContent.GetKeyForIec61360() 
                                        }),
                                        new Aas.DataSpecificationIec61360(new List<Aas.LangString>() { 
                                            new Aas.LangString("EN?", "")
                                        })));

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
                                    new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>());
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

                                if (cntByDs == ExtendIDataSpecificationContent.ContentTypes.PhysicalUnit)
                                    this.DisplayOrEditEntityDataSpecificationPhysicalUnit(
                                        stack,
                                        hasDataSpecification[i].DataSpecificationContent
                                            as Aas.DataSpecificationPhysicalUnit,
                                        relatedReferable: relatedReferable);
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
            List<Aas.Reference> references,
            Action<List<Aas.Reference>> setOutput,
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
                        setOutput?.Invoke(new List<Aas.Reference>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(stack, $"{entityName}:", levelColors.SubSection);

                if (editMode)
                {
                    // let the user control the number of references
                    this.AddAction(
                        stack, $"{entityName}:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("add-reference", "Add Aas.Reference",
                                "Adds a reference to the list.")
                            .AddAction("delete-reference", "Delete last reference",
                                "Deletes the last reference in the list."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                                references.Add(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.Key>()));

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
                    () => true,
                    "In IEC63278, 'not applicable' is a further choice. This will be part of " +
                    "a new release.",
                    severityLevel: HintCheck.Severity.Notice ),
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
            Aas.ModelingKind? kind,
            Action<Aas.ModelingKind> setOutput,
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
                    () => { return kind != Aas.ModelingKind.Instance; },
                    "Check for kind setting. 'Instance' is the usual choice." + instanceExceptionStatement,
                    severityLevel: HintCheck.Severity.Notice )
            });

            if (this.SafeguardAccess(
                stack, repo, kind, "kind:", "Create data element!",
                v =>
                {
                    setOutput?.Invoke(Aas.ModelingKind.Instance);
                    return new AnyUiLambdaActionRedrawEntity();
                }
                ))
            {
                AddKeyValueExRef(
                    stack, "kind", kind, Aas.Stringification.ToString(kind), null, repo,
                    v =>
                    {
                        setOutput?.Invoke((Aas.ModelingKind)Aas.Stringification.ModelingKindFromString((string)v));
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(Aas.ModelingKind)), comboBoxMinWidth: 105);
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
                        semElem.SemanticId =  new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>()); 
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
                        return new AnyUiLambdaActionNavigateTo(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.Key>(kl)));
                    },
                    relatedReferable: relatedReferable,
                    auxContextHeader: new[] { "\u2573", "Delete referredSemanticId" },
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
                    stack, this.repo, semElem.SupplementalSemanticIds, "supplementalSemanticId:", "Create data element!",
                    v =>
                    {
                        semElem.SupplementalSemanticIds = new List<Aas.Reference>();
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddAction(
                        stack, "Suppl.Sem.Id:",
                        new[] { "Add", "Delete last" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                semElem.SupplementalSemanticIds.Add(
                                    new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>()));

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
                                return new AnyUiLambdaActionNavigateTo(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.Key>(kl)));
                            },
                            relatedReferable: relatedReferable,
                            auxContextHeader: new[] { "\u2573", "Delete referredSemanticId" },
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
            List<Aas.Qualifier> qualifiers,
            Action<List<Aas.Qualifier>> setOutput,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Qualifiable:", levelColors.SubSection,
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
                    setOutput?.Invoke(new List<Aas.Qualifier>());
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

        public void DisplayOrEditEntityListOfIdentifierKeyValuePair(AnyUiStackPanel stack,
            List<Aas.SpecificAssetId> pairs,
            Action<List<Aas.SpecificAssetId>> setOutput,
            string key = "IdentifierKeyValuePairs",
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, $"{key}:", levelColors.SubSection);

            if (this.SafeguardAccess(
                stack, repo, pairs, $"{key}:", "Create empty list of IdentifierKeyValuePairs!",
                v =>
                {
                    setOutput?.Invoke(new List<Aas.SpecificAssetId>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.IdentifierKeyValuePairHelper(stack, repo, pairs,
                    key: key,
                    relatedReferable: relatedReferable);
            }

        }

        public void DisplayOrEditEntitySingleIdentifierKeyValuePair(AnyUiStackPanel stack,
            Aas.SpecificAssetId pair,
            Action<Aas.SpecificAssetId> setOutput,
            string key = "IdentifierKeyValuePair",
            Aas.IReferable relatedReferable = null,
            string[] auxContextHeader = null, Func<object, AnyUiLambdaActionBase> auxContextLambda = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, $"{key}:", levelColors.SubSection, repo, 
                auxContextHeader: auxContextHeader, auxContextLambda: auxContextLambda);

            if (this.SafeguardAccess(
                stack, repo, pair, $"{key}:", "Create IdentifierKeyValuePair!",
                v =>
                {
                    setOutput?.Invoke(new Aas.SpecificAssetId("", "", null));
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.IdentifierKeyValueSinglePairHelper(
                    stack, repo, pair,
                    relatedReferable: relatedReferable);
            }
        }

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
            this.AddGroup(
                        stack, "Data Specification Content IEC61360:", levelColors.SubSection);

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
                        dsiec.PreferredName = new List<Aas.LangString>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyListLangStr(stack, "preferredName", dsiec.PreferredName,
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
                        dsiec.ShortName = new List<Aas.LangString>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyListLangStr(stack, "shortName", dsiec.ShortName,
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
                        dsiec.UnitId = new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>());
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
                        dsiec.Definition = new List<Aas.LangString>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "definition", dsiec.Definition,
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
                ValueListHelper(
                    env, stack, repo, "valueList",
                    dsiec.ValueList.ValueReferencePairs,
                    relatedReferable: relatedReferable, superMenu: superMenu);

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
                            () => true,
                            "There seems to be a bug in AAS core / LevelType.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.LevelType, "levelType:", "Create data element!",
                    v =>
                    {
                        dsiec.LevelType = new();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                var subg = AddSubGrid(stack, "levelType:",
                1, 4, new[] { "#", "#", "#", "#" },
                minWidthFirstCol: this.standardFirstColWidth);

                int col = 0;
                foreach (var lt in AdminShellUtil.GetEnumValues<Aas.LevelType>())
                {
                    var currentLT = lt;
                    AnyUiUIElement.RegisterControl(
                        AddSmallCheckBoxTo(subg, 0, col++,
                            content: Aas.Stringification.ToString(lt),
                            isChecked: (dsiec.LevelType.Value & lt) > 0,
                            margin: new AnyUiThickness(0, 0, 15, 0)),
                        (v) =>
                        {
                            dsiec.LevelType = (dsiec.LevelType & ~currentLT);
                            if (v is bool vb && vb)
                                dsiec.LevelType = dsiec.LevelType | currentLT;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }
            }
        }

        //
        // DataSpecificationIEC61360
        //

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
                this.AddAction(
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
                this.AddGroup(stack, "Supplementary file assistance", this.levelColors.SubSection);

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

                this.AddAction(
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
    }
}
