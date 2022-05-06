﻿/*
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class extends the basic helper functionalities of DispEditHelper by providing modules for display/
    /// editing disting modules of the GUI, such as the different (re-usable) Interfaces of the AAS entities
    /// </summary>
    public class DispEditHelperModules : DispEditHelperCopyPaste
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
        // Referable
        //

        public void DisplayOrEditEntityReferable(AnyUiStackPanel stack,
            AdminShell.Referable referable,
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
                new HintCheck( () => { return referable.idShort == null || referable.idShort.Length < 1; },
                    "The idShort is meanwhile mandatory for all Referables. It is a short, " +
                        "unique identifier that is unique just in its context, its name space. ", breakIfTrue: true),
                new HintCheck(
                    () => {
                        if (referable.idShort == null) return false;
                        return !AdminShellUtil.ComplyIdShort(referable.idShort);
                    },
                    "The idShort shall only feature letters, digits, underscore ('_'); " +
                    "starting mandatory with a letter."),
                new HintCheck(
                    () => {
                        return true == referable.idShort?.Contains("---");
                    },
                    "The idShort contains 3 dashes. Probably, the entitiy was auto-named " +
                    "to keep it unqiue because of an operation such a copy/ paste.",
                    severityLevel: HintCheck.Severity.Notice)
            });
            this.AddKeyValueRef(
                stack, "idShort", referable, ref referable.idShort, null, repo,
                v =>
                {
                    var dr = new DiaryReference(referable);
                    referable.idShort = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange(), diaryReference: dr);
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: DispEditInjectAction.GetTitles(null, injectToIdShort),
                auxButtonToolTips: DispEditInjectAction.GetToolTips(null, injectToIdShort),
                auxButtonLambda: injectToIdShort?.auxLambda
                );

            if (!categoryUsual)
                this.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(() => { return referable.category != null && referable.category.Trim().Length >= 1; },
                    "The use of category is unusual here.", severityLevel: HintCheck.Severity.Notice));

            this.AddHintBubble(stack, hintMode, this.ConcatHintChecks(null, addHintsCategory));
            this.AddKeyValueRef(
                stack, "category", referable, ref referable.category, null, repo,
                v =>
                {
                    referable.category = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxItems: AdminShell.Referable.ReferableCategoryNames, comboBoxIsEditable: true);

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return referable.description == null || referable.description.langString == null ||
                                referable.description.langString.Count < 1;
                        },
                        "The use of an description is recommended to allow the consumer of an Referable " +
                            "to understand the nature of it.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return referable.description.langString.Count < 2; },
                        "Consider having description in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(stack, repo, referable.description, "description:", "Create data element!", v =>
            {
                referable.description = new AdminShell.Description();
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () =>
                        {
                            return referable.description.langString == null
                            || referable.description.langString.Count < 1;
                        },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Administration shell to understand your intentions."));
                this.AddKeyListLangStr(stack, "description", referable.description.langString,
                    repo, relatedReferable: referable);
            }
        }

        //
        // Identifiable
        //

        public void DisplayOrEditEntityIdentifiable<T>(
            AdminShell.AdministrationShellEnv env, AnyUiStackPanel stack,
            AdminShell.Identifiable identifiable,
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
                    () => { return identifiable.id == null; },
                    "Providing a worldwide unique identification is mandatory.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return identifiable.id.value.Trim() == ""; },
                    "Identification id shall not be empty. You could use the 'Generate' button in order to " +
                        "generate a worldwide unique id. " +
                        "The template of this id could be set by commandline arguments." )

            });
            if (this.SafeguardAccess(
                    stack, repo, identifiable.id, "identification:", "Create data element!",
                    v =>
                    {
                        identifiable.id = new AdminShell.Identifier();
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddKeyValueRef(
                    stack, "id", identifiable, ref identifiable.id.value, null, repo,
                    v =>
                    {
                        var dr = new DiaryReference(identifiable);
                        identifiable.id.value = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange(), diaryReference: dr);
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: DispEditInjectAction.GetTitles(new[] { "Generate" }, injectToId),
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            var dr = new DiaryReference(identifiable);
                            identifiable.id.value = AdminShellUtil.GenerateIdAccordingTemplate(
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
            }

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return identifiable.administration == null; },
                    "Check if providing admistrative information on version/ revision would be useful. " +
                        "This allows for better version management.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () =>
                    {
                        return identifiable.administration.version.Trim() == "" ||
                            identifiable.administration.revision.Trim() == "";
                    },
                    "Admistrative information fields should not be empty.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (this.SafeguardAccess(
                    stack, repo, identifiable.administration, "administration:", "Create data element!",
                    v =>
                    {
                        identifiable.administration = new AdminShell.Administration();
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddKeyValueRef(
                    stack, "version", identifiable.administration, ref identifiable.administration.version,
                    null, repo,
                    v =>
                    {
                        identifiable.administration.version = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                this.AddKeyValueRef(
                    stack, "revision", identifiable.administration, ref identifiable.administration.revision,
                    null, repo,
                    v =>
                    {
                        identifiable.administration.revision = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
            }
        }

        //
        // Data Specification
        //

        public void DisplayOrEditEntityHasDataSpecificationReferences(AnyUiStackPanel stack,
            AdminShell.HasDataSpecification hasDataSpecification,
            Action<AdminShell.HasDataSpecification> setOutput,
            string[] addPresetNames = null, AdminShell.ListOfIdentifier[] addPresetKeyLists = null,
            bool dataSpecRefsAreUsual = false,
            AdminShell.Referable relatedReferable = null)
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
                        setOutput?.Invoke(new AdminShell.HasDataSpecification());
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
                                    new AdminShell.EmbeddedDataSpecification(
                                        new AdminShell.DataSpecificationRef()));

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
                    for (int i = 0; i < hasDataSpecification.Count; i++)
                        if (hasDataSpecification[i].dataSpecification != null)
                            this.AddKeyListOfIdentifier(
                                stack, String.Format("reference[{0}]", i),
                                hasDataSpecification[i].dataSpecification.Value,
                                repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                                addExistingEntities: null /* "All" */,
                                addPresetNames: addPresetNames, addPresetKeyLists: addPresetKeyLists,
                                relatedReferable: relatedReferable);
                }
            }
        }

        //
        // List of References
        //

        public void DisplayOrEditEntityListOfModelReferences(AnyUiStackPanel stack,
            List<AdminShell.ModelReference> references,
            Action<List<AdminShell.ModelReference>> setOutput,
            string entityName,
            string[] addPresetNames = null, AdminShell.Key[] addPresetKeys = null,
            AdminShell.Referable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // hasDataSpecification, isCaseOf are MULTIPLE references. That is: multiple x multiple keys!
            if (this.SafeguardAccess(
                    stack, this.repo, references, $"{entityName}:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<AdminShell.ModelReference>());
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
                                references.Add(new AdminShell.ModelReference());

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
                            stack, String.Format("ModelRef[{0}]", i), references[i].Keys, repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            AdminShell.Key.AllElements,
                            addEclassIrdi: true,
                            relatedReferable: relatedReferable);
                }
            }
        }


        //
        // Kind
        //

        public void DisplayOrEditEntityAssetKind(AnyUiStackPanel stack,
            AdminShell.AssetKind kind,
            Action<AdminShell.AssetKind> setOutput,
            AdminShell.Referable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind (of Asset):", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return kind == null; },
                    "Providing kind information is mandatory. Typically you want to model instances. " +
                        "A manufacturer would define types of assets, as well.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return kind.kind.Trim().ToLower() != "instance"; },
                    "Check for kind setting. 'Instance' is the usual choice.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (this.SafeguardAccess(
                    stack, repo, kind, "kind:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new AdminShell.AssetKind());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyValueRef(
                    stack, "kind", kind, ref kind.kind, null, repo,
                    v =>
                    {
                        kind.kind = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    new[] { AdminShell.AssetKind.Type, AdminShell.AssetKind.Instance });
        }

        public void DisplayOrEditEntityModelingKind(AnyUiStackPanel stack,
            AdminShell.ModelingKind kind,
            Action<AdminShell.ModelingKind> setOutput,
            string instanceExceptionStatement = null,
            AdminShell.Referable relatedReferable = null)
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
                    () => { return kind.kind.Trim().ToLower() != "instance"; },
                    "Check for kind setting. 'Instance' is the usual choice." + instanceExceptionStatement,
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (this.SafeguardAccess(
                    stack, repo, kind, "kind:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new AdminShell.ModelingKind());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    ))
                this.AddKeyValueRef(
                    stack, "kind", kind, ref kind.kind, null, repo,
                    v =>
                    {
                        kind.kind = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    new[] { AdminShell.ModelingKind.Template, AdminShell.ModelingKind.Instance });
        }

        //
        // HasSemantic
        //

        public void DisplayOrEditEntitySemanticId(AnyUiStackPanel stack,
            AdminShell.SemanticId semanticId,
            Action<AdminShell.SemanticId> setOutput,
            string statement = null,
            bool checkForCD = false,
            string addExistingEntities = null,
            CopyPasteBuffer cpb = null,
            AdminShell.Referable relatedReferable = null)
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
                            () => { return semanticId == null || semanticId.IsEmpty; },
                            "Check if you want to add a semantic reference. " + statement,
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                                () => !(semanticId.First?.IsIRDI() == true
                                        || semanticId.First?.IsIRI() == true),
                                "The semanticId usually refers to a ConceptDescription " +
                                    "identified by IRI or IRDI.",
                                severityLevel: HintCheck.Severity.Notice)
                    });

            // add from Copy Buffer
            var bufferKeys = CopyPasteBuffer.PreparePresetsForListOfIdentifier(cpb);

            // add the keys
            if (this.SafeguardAccess(
                    stack, repo, semanticId, "semanticId:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new AdminShell.SemanticId());
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListOfIdentifier(
                    stack, "semanticId", semanticId.Value, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    addExistingEntities: addExistingEntities, addFromPool: true,
                    addEclassIrdi: true,
                    addPresetNames: bufferKeys.Item1,
                    addPresetKeyLists: bufferKeys.Item2,
                    jumpLambda: (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(
                            AdminShell.ModelReference.CreateNew(AdminShell.Key.ConceptDescription, kl));
                    },
                    relatedReferable: relatedReferable);
        }

        //
        // Qualifiable
        //

        public void DisplayOrEditEntityQualifierCollection(AnyUiStackPanel stack,
            AdminShell.QualifierCollection qualifiers,
            Action<AdminShell.QualifierCollection> setOutput,
            AdminShell.Referable relatedReferable = null)
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
                    setOutput?.Invoke(new AdminShell.QualifierCollection());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.QualifierHelper(stack, repo, qualifiers, relatedReferable: relatedReferable);
            }

        }

        //
        // List of IdentifierKeyValuePair
        //

        public void DisplayOrEditEntityListOfIdentifierKeyValuePair(AnyUiStackPanel stack,
            AdminShell.ListOfIdentifierKeyValuePair pairs,
            Action<AdminShell.ListOfIdentifierKeyValuePair> setOutput,
            AdminShell.Referable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "IdentifierKeyValuePairs:", levelColors.SubSection);

            if (this.SafeguardAccess(
                stack, repo, pairs, "IdentifierKeyValuePairs:", "Create empty list of IdentifierKeyValuePairs!",
                v =>
                {
                    setOutput?.Invoke(new AdminShell.ListOfIdentifierKeyValuePair());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.IdentifierKeyValuePairHelper(stack, repo, pairs, relatedReferable: relatedReferable);
            }

        }

        //
        // DataSpecificationIEC61360
        //

        public void DisplayOrEditEntityDataSpecificationIEC61360(AnyUiStackPanel stack,
            AdminShell.DataSpecificationIEC61360 dsiec,
            AdminShell.Referable relatedReferable = null)
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
                        dsiec.preferredName = new AdminShell.LangStringSetIEC61360();
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
                        dsiec.shortName = new AdminShell.LangStringSetIEC61360();
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
                                return (dsiec.unitId == null || dsiec.unitId.Count < 1) &&
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
                                    ( dsiec.unitId == null || dsiec.unitId.Count < 1);
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
                        dsiec.unitId = new AdminShell.UnitId();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddKeyListOfIdentifier(
                    stack, "unitId", dsiec.unitId.Value, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    AdminShell.Key.GlobalReference, addEclassIrdi: true,
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
                comboBoxItems: AdminShell.DataSpecificationIEC61360.DataTypeNames);

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
                        dsiec.definition = new AdminShell.LangStringSetIEC61360();
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
            AdminShell.SubmodelRef smref,
            Action<AdminShell.SubmodelRef> setOutput,
            string entityName,
            AdminShell.Referable relatedReferable = null)
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
                        setOutput?.Invoke(new AdminShell.SubmodelRef());
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
