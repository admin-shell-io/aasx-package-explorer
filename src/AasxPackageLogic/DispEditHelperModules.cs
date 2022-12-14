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
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AasCore.Aas3_0_RC02.HasDataSpecification;
//using AasxCompatibilityModels;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Extenstions;

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
            IReferable referable,
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
                new HintCheck( () => !(referable is IIdentifiable) && !referable.IdShort.HasContent(),
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
            var idShort = referable.IdShort;
            this.AddKeyValueRef(
                stack, "idShort", referable, ref idShort, null, repo,
                v =>
                {
                    var dr = new DiaryReference(referable);
                    idShort = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange(), diaryReference: dr);
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: DispEditInjectAction.GetTitles(null, injectToIdShort),
                auxButtonToolTips: DispEditInjectAction.GetToolTips(null, injectToIdShort),
                auxButtonLambda: injectToIdShort?.auxLambda
                );
            referable.IdShort = idShort;

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
                        () => { return referable.DisplayName.LangStrings.Count < 2; },
                        "Consider having Display name in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(stack, repo, referable.DisplayName, "displayName:", "Create data element!", v =>
            {
                referable.DisplayName = new LangStringSet(new List<LangString>());
                this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddKeyListLangStr(stack, "displayName", referable.DisplayName.LangStrings,
                    repo, relatedReferable: referable);
            }

            if (!categoryUsual)
                this.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(() => { return referable.Category != null && referable.Category.Trim().Length >= 1; },
                    "The use of category is unusual here.", severityLevel: HintCheck.Severity.Notice));

            this.AddHintBubble(stack, hintMode, this.ConcatHintChecks(null, addHintsCategory));
            var category = referable.Category;
            this.AddKeyValueRef(
                stack, "category", referable, ref category, null, repo,
                v =>
                {
                    category = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxItems: new string[] {"CONSTANT, PARAMETER, VARIABLE"}, comboBoxIsEditable: true);
            referable.Category = category;

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return referable.Description == null || referable.Description.LangStrings == null ||
                                referable.Description.LangStrings.Count < 1;
                        },
                        "The use of an description is recommended to allow the consumer of an Referable " +
                            "to understand the nature of it.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return referable.Description.LangStrings.Count < 2; },
                        "Consider having description in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(stack, repo, referable.Description, "description:", "Create data element!", v =>
            {
                referable.Description = new LangStringSet(new List<LangString>());
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () =>
                        {
                            return referable.Description.LangStrings == null
                            || referable.Description.LangStrings.Count < 1;
                        },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Administration shell to understand your intentions."));
                this.AddKeyListLangStr(stack, "description", referable.Description.LangStrings,
                    repo, relatedReferable: referable);
            }

            // Checksum
            var checksum = referable.Checksum;
            this.AddKeyValueRef(
                stack, "checksum", referable, ref checksum, null, repo,
                v =>
                {
                    var dr = new DiaryReference(referable);
                    checksum = v as string;
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
            referable.Checksum = checksum;

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
            List<Extension> extensions,
            Action<List<Extension>> setOutput,
            IReferable relatedReferable = null)
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
                    setOutput?.Invoke(new List<Extension>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.ExtensionHelper(stack, repo, extensions, relatedReferable: relatedReferable);
            }

        }

        //public void DisplayOrEditEntityReferable(AnyUiStackPanel stack,
        //    IReferable referable,
        //    DispEditInjectAction injectToIdShort = null,
        //    HintCheck[] addHintsCategory = null,
        //    bool categoryUsual = false)
        //{
        //    // access
        //    if (stack == null || referable == null)
        //        return;

        //    // members
        //    this.AddGroup(stack, "IReferable:", levelColors.SubSection);

        //    // members
        //    this.AddHintBubble(stack, hintMode, new[] {
        //        new HintCheck( () => { return referable.IdShort == null || referable.IdShort.Length < 1; },
        //            "The idShort is meanwhile mandatory for all Referables. It is a short, " +
        //                "unique identifier that is unique just in its context, its name space. ", breakIfTrue: true),
        //        new HintCheck(
        //            () => {
        //                if (referable.IdShort == null) return false;
        //                return !AdminShellUtil.ComplyIdShort(referable.IdShort);
        //            },
        //            "The idShort shall only feature letters, digits, underscore ('_'); " +
        //            "starting mandatory with a letter."),
        //        new HintCheck(
        //            () => {
        //                return true == referable.IdShort?.Contains("---");
        //            },
        //            "The idShort contains 3 dashes. Probably, the entitiy was auto-named " +
        //            "to keep it unqiue because of an operation such a copy/ paste.",
        //            severityLevel: HintCheck.Severity.Notice)
        //    });

        //    var refIdShort = referable.IdShort;
        //    this.AddKeyValueRef(
        //        stack, "idShort", referable, ref refIdShort, null, repo,
        //        v =>
        //        {
        //            var dr = new DiaryReference(referable);
        //            referable.IdShort = v as string;
        //            this.AddDiaryEntry(referable, new DiaryEntryStructChange(), diaryReference: dr);
        //            return new AnyUiLambdaActionNone();
        //        },
        //        auxButtonTitles: DispEditInjectAction.GetTitles(null, injectToIdShort),
        //        auxButtonToolTips: DispEditInjectAction.GetToolTips(null, injectToIdShort),
        //        auxButtonLambda: injectToIdShort?.auxLambda
        //        );
        //    referable.IdShort = refIdShort;

        //    if (!categoryUsual)
        //        this.AddHintBubble(
        //            stack, hintMode,
        //            new HintCheck(() => { return referable.Category != null && referable.Category.Trim().Length >= 1; },
        //            "The use of category is unusual here.", severityLevel: HintCheck.Severity.Notice));

        //    this.AddHintBubble(stack, hintMode, this.ConcatHintChecks(null, addHintsCategory));

        //    var category = referable.Category;
        //    this.AddKeyValueRef(
        //        stack, "category", referable, ref category, null, repo,
        //        v =>
        //        {
        //            referable.Category = v as string;
        //            this.AddDiaryEntry(referable, new DiaryEntryStructChange());
        //            return new AnyUiLambdaActionNone();
        //        },
        //        comboBoxItems: new string[] { "CONSTANT", "PARAMETER", "VARIABLE" }, comboBoxIsEditable: true);
        //    referable.Category = category;

        //    this.AddHintBubble(
        //        stack, hintMode,
        //        new[] {
        //            new HintCheck(
        //                () => {
        //                    return referable.Description == null || referable.Description.LangStrings == null ||
        //                        referable.Description.LangStrings.Count < 1;
        //                },
        //                "The use of an description is recommended to allow the consumer of an IReferable " +
        //                    "to understand the nature of it.",
        //                breakIfTrue: true,
        //                severityLevel: HintCheck.Severity.Notice),
        //            new HintCheck(
        //                () => { return referable.Description.LangStrings.Count < 2; },
        //                "Consider having description in multiple langauges.",
        //                severityLevel: HintCheck.Severity.Notice)
        //    });
        //    if (this.SafeguardAccess(stack, repo, referable.Description, "description:", "Create data element!", v =>
        //    {
        //        referable.Description = new LangStringSet(new List<LangString>());
        //        return new AnyUiLambdaActionRedrawEntity();
        //    }))
        //    {
        //        this.AddHintBubble(
        //            stack, hintMode,
        //            new HintCheck(
        //                () =>
        //                {
        //                    return referable.Description.LangStrings == null
        //                    || referable.Description.LangStrings.Count < 1;
        //                },
        //                "Please add some descriptions in your main languages here to help consumers " +
        //                    "of your Administration shell to understand your intentions."));
        //        this.AddKeyListLangStr(stack, "description", referable.Description.LangStrings,
        //            repo, relatedReferable: referable);
        //    }
        //}

        //
        // Identifiable
        //

        public void DisplayOrEditEntityIdentifiable(AnyUiStackPanel stack,
            IIdentifiable identifiable,
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
                    stack, repo, identifiable.Id, "identification:", "Create data element!",
                    v =>
                    {
                        identifiable.Id = "";
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                var id = identifiable.Id;
                this.AddKeyValueRef(
                    stack, "id", identifiable, ref id, null, repo,
                    v =>
                    {
                        var dr = new DiaryReference(identifiable);
                        id = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange(), diaryReference: dr);
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: DispEditInjectAction.GetTitles(new[] { "Generate" }, injectToId),
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            var dr = new DiaryReference(identifiable);
                            id = AdminShellUtil.GenerateIdAccordingTemplate(
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
                identifiable.Id = id;
            }

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return identifiable.Administration == null; },
                    "Check if providing admistrative information on version/ revision would be useful. " +
                        "This allows for better version management.",
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
                        identifiable.Administration = new AdministrativeInformation();
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                var version = identifiable.Administration.Version;
                this.AddKeyValueRef(
                    stack, "version", identifiable.Administration, ref version,
                    null, repo,
                    v =>
                    {
                        version = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
                identifiable.Administration.Version = version;

                var revision = identifiable.Administration.Revision;
                this.AddKeyValueRef(
                    stack, "revision", identifiable.Administration, ref revision,
                    null, repo,
                    v =>
                    {
                        revision = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
                identifiable.Administration.Revision = revision;
            }
        }

        public void DisplayOrEditEntityHasDataSpecificationReferences(AnyUiStackPanel stack,
            List<Reference> references,
            Action<List<Reference>> setOutput,
            string[] addPresetNames = null, List<Key>[] addPresetKeyLists = null,
            bool dataSpecRefsAreUsual = false,
            IReferable relatedReferable = null)
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
                    "Check if a data specification is appropriate here.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice) });
            if (this.SafeguardAccess(
                    stack, this.repo, references, "DataSpecification:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<Reference>());
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
                                references.Add(new Reference(ReferenceTypes.GlobalReference, new List<Key>()));

                            if (buttonNdx == 1 && references.Count > 0)
                                references.RemoveAt(
                                    references.Count - 1);

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (references != null && references.Count > 0)
                {
                    for (int i = 0; i < references.Count; i++)
                        this.AddKeyListKeys(
                            stack, String.Format("ModelRef[{0}]", i), references[i].Keys, repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            "All",
                            addEclassIrdi: true,
                            relatedReferable: relatedReferable);
                }
            }
        } 

        //Added this method only to support embeddedDS from ConceptDescriptions
        public void DisplayOrEditEntityHasDataSpecificationReferences(AnyUiStackPanel stack,
            HasDataSpecification hasDataSpecification,
            Action<HasDataSpecification> setOutput,
            string[] addPresetNames = null, List<Key>[] addPresetKeyLists = null,
            bool dataSpecRefsAreUsual = false,
            IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasDataSpecification (Reference):", levelColors.SubSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(stack, hintMode, new[] {
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
                        setOutput?.Invoke(new HasDataSpecification());
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
                                hasDataSpecification.Add(
                                    new EmbeddedDataSpecification(
                                        new Reference(ReferenceTypes.GlobalReference, new List<Key>())));

                            if (buttonNdx == 1 && hasDataSpecification.Count > 0)
                                hasDataSpecification.RemoveAt(
                                    hasDataSpecification.Count - 1);

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (hasDataSpecification != null && hasDataSpecification.Count > 0)
                {
                    //TODO:jtikekar: refactor
                    List<string>[] presetKeys = new List<string>[addPresetKeyLists.Length];
                    for (int j = 0; j < addPresetKeyLists.Length; j++)
                    {
                        List<string> keys = new List<string>();
                        foreach(var key in addPresetKeyLists[j])
                        {
                            keys.Add(key.Value);
                        }
                        presetKeys[j] = keys;
                    }
                    for (int i = 0; i < hasDataSpecification.Count; i++)
                    {
                        List<string> keys = new List<string>();
                        foreach (var key in hasDataSpecification[i].DataSpecification.Keys)
                        {
                            keys.Add(key.Value);
                        }
                        if (hasDataSpecification[i].DataSpecification != null)
                            this.AddKeyListOfIdentifier(
                                stack, String.Format("reference[{0}]", i),
                                keys,
                                repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                                addExistingEntities: null /* "All" */,
                                addPresetNames: addPresetNames, addPresetKeyLists: presetKeys,
                                relatedReferable: relatedReferable);
                    }
                        
                }
            }
        }

        //
        // List of References
        //

        public void DisplayOrEditEntityListOfReferences(AnyUiStackPanel stack,
            List<Reference> references,
            Action<List<Reference>> setOutput,
            string entityName,
            string[] addPresetNames = null, Key[] addPresetKeys = null,
            IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            if (this.SafeguardAccess(
                    stack, this.repo, references, $"{entityName}:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<Reference>());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(stack, $"{entityName}:", levelColors.SubSection);

                if (editMode)
                {
                    // let the user control the number of references
                    this.AddAction(
                        stack, $"{entityName}:", new[] { "Add Reference", "Delete last reference" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                references.Add(new Reference(ReferenceTypes.ModelReference, new List<Key>()));

                            if (buttonNdx == 1 && references.Count > 0)
                                references.RemoveAt(references.Count - 1);

                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (references != null && references.Count > 0)
                {
                    for (int i = 0; i < references.Count; i++)
                        this.AddKeyListKeys(
                            stack, String.Format("reference[{0}]", i), references[i].Keys, repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            "All",
                            addEclassIrdi: true,
                            relatedReferable: relatedReferable);
                }
            }
        }

        //
        // Kind
        //

        public void DisplayOrEditEntityAssetKind(AnyUiStackPanel stack,
            AssetKind kind,
            Action<AssetKind> setOutput,
            IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind (of AssetInformation):", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return kind == null; },
                    "Providing kind information is mandatory. Typically you want to model instances. " +
                        "A manufacturer would define types of assets, as well.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return kind != AssetKind.Instance; },
                    "Check for kind setting. 'Instance' is the usual choice.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            var varKind = Stringification.ToString(kind);
            if (this.SafeguardAccess(
                    stack, repo, kind, "kind:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new AssetKind());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))

                this.AddKeyValueRef(
                    stack, "kind", kind, ref varKind, null, repo,
                    v =>
                    {
                        kind = (AssetKind)Stringification.AssetKindFromString((string)v);
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(AssetKind)));

            kind = (AssetKind)Stringification.AssetKindFromString(varKind);
        }

        public void DisplayOrEditEntityModelingKind(AnyUiStackPanel stack,
            ModelingKind? kind,
            Action<ModelingKind> setOutput,
            string instanceExceptionStatement = null,
            IReferable relatedReferable = null)
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
                    () => { return kind != ModelingKind.Instance; },
                    "Check for kind setting. 'Instance' is the usual choice." + instanceExceptionStatement,
                    severityLevel: HintCheck.Severity.Notice )
            });

            var varKind = Stringification.ToString(kind);
            if (this.SafeguardAccess(
                    stack, repo, kind, "kind:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new ModelingKind());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    ))
                this.AddKeyValueRef(
                    stack, "kind", kind, ref varKind, null, repo,
                    v =>
                    {
                        kind = (ModelingKind)Stringification.ModelingKindFromString((string)v);
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(ModelingKind)));
            kind = Stringification.ModelingKindFromString(varKind);
        }

        //
        // HasSemantic
        //

        public void DisplayOrEditEntitySemanticId(AnyUiStackPanel stack,
            Reference semanticId,
            Action<Reference> setOutput,
            string statement = null,
            bool checkForCD = false,
            string addExistingEntities = null,
            CopyPasteBuffer cpb = null,
            IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Semantic ID:", levelColors.SubSection);

            // hint
            this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return semanticId == null || semanticId.IsEmpty(); },
                            "Check if you want to add a semantic reference. " + statement,
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                                () => { return checkForCD &&
                                    semanticId.Keys[0].Type != KeyTypes.ConceptDescription; },
                                "The semanticId usually refers to a ConceptDescription " +
                                    "within the respective repository.",
                                severityLevel: HintCheck.Severity.Notice)
                    });

            // add from Copy Buffer
            var bufferKeys = CopyPasteBuffer.PreparePresetsForListKeys(cpb);

            // add the keys
            if (this.SafeguardAccess(
                    stack, repo, semanticId, "semanticId:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new Reference(ReferenceTypes.GlobalReference, new List<Key>())); 
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListKeys(
                    stack, "semanticId", semanticId.Keys, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    addExistingEntities: addExistingEntities, addFromPool: true,
                    addEclassIrdi: true,
                    addPresetNames: bufferKeys.Item1,
                    addPresetKeyLists: bufferKeys.Item2,
                    jumpLambda: (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(new Reference(ReferenceTypes.ModelReference, new List<Key>(kl)));
                    },
                    relatedReferable: relatedReferable);
        }

        //
        // Qualifiable
        //

        public void DisplayOrEditEntityQualifierCollection(AnyUiStackPanel stack,
            List<Qualifier> qualifiers,
            Action<List<Qualifier>> setOutput,
            IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Qualifiable:", levelColors.SubSection);

            if (this.SafeguardAccess(
                stack, repo, qualifiers, "Qualifiers:", "Create empty list of Qualifiers!",
                v =>
                {
                    setOutput?.Invoke(new List<Qualifier>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.QualifierHelper(stack, repo, qualifiers, relatedReferable: relatedReferable);
            }

        }

        //
        // List of SpecificAssetId
        //

        public void DisplayOrEditEntityListOfIdentifierKeyValuePair(AnyUiStackPanel stack,
            List<SpecificAssetId> pairs,
            Action<List<SpecificAssetId>> setOutput,
            string key = "IdentifierKeyValuePairs",
            IReferable relatedReferable = null)
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
                    setOutput?.Invoke(new List<SpecificAssetId>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.IdentifierKeyValuePairHelper(stack, repo, pairs,
                    key: key,
                    relatedReferable: relatedReferable);
            }

        }

        //
        // DataSpecificationIEC61360
        //


        public void DisplayOrEditEntityDataSpecificationIEC61360(AnyUiStackPanel stack,
            DataSpecificationIEC61360 dsiec,
            IReferable relatedReferable = null)
        {
            // access
            if (stack == null || dsiec == null)
                return;

            // members
            this.AddGroup(
                        stack, "Data Specification Content IEC61360:", levelColors.SubSection);

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.preferredName == null || dsiec.preferredName.Count < 1; },
                            "Please add a preferred name, which could be used on user interfaces " +
                                "to identify the concept to a human person.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.preferredName.Count <2; },
                            "Please add multiple languanges.")
                });
            if (this.SafeguardAccess(
                    stack, repo, dsiec.preferredName, "preferredName:", "Create data element!",
                    v =>
                    {
                        dsiec.preferredName = new LangStringSetIEC61360();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "preferredName", dsiec.preferredName,
                    repo, relatedReferable: relatedReferable);

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.shortName == null || dsiec.shortName.Count < 1; },
                            "Please check if you can add a short name, which is a reduced, even symbolic version of " +
                                "the preferred name. IEC 61360 defines some symbolic rules " +
                                "(e.g. greek characters) for this name.",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.shortName.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (this.SafeguardAccess(
                    stack, repo, dsiec.shortName, "shortName:", "Create data element!",
                    v =>
                    {
                        dsiec.shortName = new LangStringSetIEC61360();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "shortName", dsiec.shortName,
                    repo, relatedReferable: relatedReferable);

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => {
                                return (dsiec.unitId == null || dsiec.unitId.Keys.Count < 1) &&
                                    ( dsiec.unit == null || dsiec.unit.Trim().Length < 1);
                            },
                            "Please check, if you can provide a unit or a unitId, " +
                                "in which the concept is being measured. " +
                                "Usage of SI-based units is encouraged.",
                            severityLevel: HintCheck.Severity.Notice)
            });
            this.AddKeyValueRef(
                stack, "unit", dsiec, ref dsiec.unit, null, repo,
                v =>
                {
                    dsiec.unit = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => {
                                return ( dsiec.unit == null || dsiec.unit.Trim().Length < 1) &&
                                    ( dsiec.unitId == null || dsiec.unitId.Keys.Count < 1);
                            },
                            "Please check, if you can provide a unit or a unitId, " +
                                "in which the concept is being measured. " +
                                "Usage of SI-based units is encouraged.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (this.SafeguardAccess(
                    stack, repo, dsiec.unitId, "unitId:", "Create data element!",
                    v =>
                    {
                        dsiec.unitId = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                List<string> keys = new();
                foreach (var key in dsiec.unitId.Keys)
                {
                    keys.Add(key.Value);
                }
                this.AddKeyListOfIdentifier(
                    stack, "unitId", keys, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    Stringification.ToString(KeyTypes.GlobalReference), addEclassIrdi: true,
                    relatedReferable: relatedReferable);
            }

            this.AddKeyValueRef(
                stack, "valueFormat", dsiec, ref dsiec.valueFormat, null, repo,
                v =>
                {
                    dsiec.valueFormat = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () =>
                            {
                                return dsiec.sourceOfDefinition == null || dsiec.sourceOfDefinition.Length < 1;
                            },
                            "Please check, if you can provide a source of definition for the concepts. " +
                                "This could be an informal link to a document, glossary item etc.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            this.AddKeyValueRef(
                stack, "sourceOfDef.", dsiec, ref dsiec.sourceOfDefinition, null, repo,
                v =>
                {
                    dsiec.sourceOfDefinition = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.symbol == null || dsiec.symbol.Trim().Length < 1; },
                            "Please check, if you can provide formulaic character for the concept.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            this.AddKeyValueRef(
                stack, "symbol", dsiec, ref dsiec.symbol, null, repo,
                v =>
                {
                    dsiec.symbol = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.dataType == null || dsiec.dataType.Trim().Length < 1; },
                            "Please provide data type for the concept. " +
                                "Data types are provided by the IEC 61360.")
                });
            this.AddKeyValueRef(
                stack, "dataType", dsiec, ref dsiec.dataType, null, repo,
                v =>
                {
                    dsiec.dataType = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxIsEditable: true,
                comboBoxItems: DataSpecificationIEC61360.DataTypeNames);

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.definition == null || dsiec.definition.Count < 1; },
                            "Please check, if you can add a definition, which could be used to describe exactly, " +
                                "how to establish a value/ measurement for the concept.",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.definition.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (this.SafeguardAccess(
                    stack, repo, dsiec.definition, "definition:", "Create data element!",
                    v =>
                    {
                        dsiec.definition = new LangStringSetIEC61360();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "definition", dsiec.definition,
                    repo, relatedReferable: relatedReferable);
        }

        //
        // special Submodel References
        // 

        public void DisplayOrEditEntitySubmodelRef(AnyUiStackPanel stack,
            Reference smref,
            Action<Reference> setOutput,
            string entityName,
            IReferable relatedReferable = null)
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
                        setOutput?.Invoke(new Reference(ReferenceTypes.GlobalReference, new List<Key>()));
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(
                    stack, $"{entityName} - Reference to describing Submodel:",
                    levelColors.SubSection);
                this.AddKeyListKeys(
                    stack, $"{entityName}:", smref.Keys,
                    repo, packages, PackageCentral.PackageCentral.Selector.Main, "Submodel",
                    relatedReferable: relatedReferable);
            }
        }
    }
}
