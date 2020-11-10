﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AasxGlobalLogging;
using AasxIntegrationBase;
using AdminShellNS;

namespace AasxPackageExplorer
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
            public Func<int, ModifyRepo.LambdaAction> auxLambda = null;

            public DispEditInjectAction() { }

            public DispEditInjectAction(string[] auxTitles, Func<int, ModifyRepo.LambdaAction> auxLambda)
            {
                this.auxTitles = auxTitles;
                this.auxLambda = auxLambda;
            }

            public DispEditInjectAction(string[] auxTitles, string[] auxToolTips,
                Func<int, ModifyRepo.LambdaAction> auxActions)
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

        public void DisplayOrEditEntityReferable(StackPanel stack,
            AdminShell.Referable referable,
            DispEditInjectAction injectToIdShort = null,
            HintCheck[] addHintsCategory = null,
            bool categoryUsual = false)
        {
            // access
            if (stack == null || referable == null)
                return;

            // members
            this.AddGroup(stack, "Referable:", levelColors[1][0], levelColors[1][1]);

            // members
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck( () => { return referable.idShort == null || referable.idShort.Length < 1; },
                    "idShort is meanwhile mandatory for all Referables. It is a short, " +
                        "unique identifier that is unique just in its context, its name space. ", breakIfTrue: true),

                new HintCheck(
                    () => {
                        if (referable.idShort == null) return false;
                        return !AdminShellUtil.ComplyIdShort(referable.idShort);
                    },
                    "idShort shall only feature letters, digits, underscore ('_'); " +
                        "starting mandatory with a letter..")
            });
            this.AddKeyValueRef(
                stack, "idShort", referable, ref referable.idShort, null, repo,
                v => { referable.idShort = v as string; return new ModifyRepo.LambdaActionNone(); },
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
                v => { referable.category = v as string; return new ModifyRepo.LambdaActionNone(); },
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
                return new ModifyRepo.LambdaActionRedrawEntity();
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
                this.AddKeyListLangStr(stack, "description", referable.description.langString, repo);
            }
        }

        //
        // Identifiable
        //

        public void DisplayOrEditEntityIdentifiable(StackPanel stack,
            AdminShell.Identifiable identifiable,
            string templateForIdString,
            DispEditInjectAction injectToId = null,
            bool checkForIri = true)
        {
            // access
            if (stack == null || identifiable == null)
                return;

            // members
            this.AddGroup(stack, "Identifiable:", levelColors[1][0], levelColors[1][1]);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return identifiable.identification == null; },
                    "Providing a worldwide unique identification is mandatory.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return checkForIri
                        && identifiable.identification.idType != AdminShell.Identification.IRI;
                    },
                    "Check if identification type is correct. Use of IRIs is usual here.",
                    severityLevel: HintCheck.Severity.Notice ),
                new HintCheck(
                    () => { return identifiable.identification.id.Trim() == ""; },
                    "Identification id shall not be empty. You could use the 'Generate' button in order to " +
                        "generate a worldwide unique id. " +
                        "The template of this id could be set by commandline arguments." )

            });
            if (this.SafeguardAccess(
                    stack, repo, identifiable.identification, "identification:", "Create data element!",
                    v =>
                    {
                        identifiable.identification = new AdminShell.Identification();
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                this.AddKeyValueRef(
                    stack, "idType", identifiable, ref identifiable.identification.idType, null, repo,
                    v =>
                    {
                        identifiable.identification.idType = v as string;
                        return new ModifyRepo.LambdaActionNone();
                    },
                    comboBoxItems: AdminShell.Key.IdentifierTypeNames);

                this.AddKeyValueRef(
                    stack, "id", identifiable, ref identifiable.identification.id, null, repo,
                    v => { identifiable.identification.id = v as string; return new ModifyRepo.LambdaActionNone(); },
                    auxButtonTitles: DispEditInjectAction.GetTitles(new[] { "Generate" }, injectToId),
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            identifiable.identification.idType = AdminShell.Identification.IRI;
                            identifiable.identification.id = Options.Curr.GenerateIdAccordingTemplate(
                                templateForIdString);
                            return new ModifyRepo.LambdaActionRedrawAllElements(nextFocus: identifiable);
                        }
                        if (i >= 1)
                        {
                            var la = injectToId?.auxLambda?.Invoke(i - 1);
                            return la;
                        }
                        return new ModifyRepo.LambdaActionNone();
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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                this.AddKeyValueRef(
                    stack, "version", identifiable.administration, ref identifiable.administration.version,
                    null, repo,
                    v =>
                    {
                        identifiable.administration.version = v as string;
                        return new ModifyRepo.LambdaActionNone();
                    });

                this.AddKeyValueRef(
                    stack, "revision", identifiable.administration, ref identifiable.administration.revision,
                    null, repo,
                    v =>
                    {
                        identifiable.administration.revision = v as string;
                        return new ModifyRepo.LambdaActionNone();
                    });
            }
        }

        //
        // Data Specification
        //

        public void DisplayOrEditEntityHasDataSpecificationReferences(StackPanel stack,
            AdminShell.HasDataSpecification hasDataSpecification,
            Action<AdminShell.HasDataSpecification> setOutput,
            string[] addPresetNames = null, AdminShell.KeyList[] addPresetKeyLists = null,
            bool dataSpecRefsAreUsual = false)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasDataSpecification (Reference):", levelColors[1][0], levelColors[1][1]);

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
                        return new ModifyRepo.LambdaActionRedrawEntity();
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

                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (hasDataSpecification != null && hasDataSpecification.Count > 0)
                {
                    for (int i = 0; i < hasDataSpecification.Count; i++)
                        if (hasDataSpecification[i].dataSpecification != null)
                            this.AddKeyListKeys(
                                stack, String.Format("reference[{0}]", i),
                                hasDataSpecification[i].dataSpecification.Keys,
                                repo, packages, AasxWpfControlLibrary.PackageCentral.Selector.MainAux, 
                                addExistingEntities: null /* "All" */,
                                addPresetNames: addPresetNames, addPresetKeyLists: addPresetKeyLists);
                }
            }
        }

        //
        // List of References
        //

        public void DisplayOrEditEntityListOfReferences(StackPanel stack,
            List<AdminShell.Reference> references,
            Action<List<AdminShell.Reference>> setOutput,
            string entityName,
            string[] addPresetNames = null, AdminShell.Key[] addPresetKeys = null)
        {
            // access
            if (stack == null)
                return;

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            if (this.SafeguardAccess(
                    stack, this.repo, references, $"{entityName}:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new List<AdminShell.Reference>());
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(stack, $"{entityName}:", levelColors[1][0], levelColors[1][1]);

                if (editMode)
                {
                    // let the user control the number of references
                    this.AddAction(
                        stack, $"{entityName}:", new[] { "Add Reference", "Delete last reference" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                references.Add(new AdminShell.Reference());

                            if (buttonNdx == 1 && references.Count > 0)
                                references.RemoveAt(references.Count - 1);

                            return new ModifyRepo.LambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (references != null && references.Count > 0)
                {
                    for (int i = 0; i < references.Count; i++)
                        this.AddKeyListKeys(
                            stack, String.Format("reference[{0}]", i), references[i].Keys, repo,
                            packages, AasxWpfControlLibrary.PackageCentral.Selector.MainAux, 
                            AdminShell.Key.AllElements,
                            addEclassIrdi: true);
                }
            }
        }

        //
        // Kind
        //

        public void DisplayOrEditEntityAssetKind(StackPanel stack,
            AdminShell.AssetKind kind,
            Action<AdminShell.AssetKind> setOutput)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind:", levelColors[1][0], levelColors[1][1]);

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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }
                    ))
                this.AddKeyValueRef(
                    stack, "kind", kind, ref kind.kind, null, repo,
                    v => { kind.kind = v as string; return new ModifyRepo.LambdaActionNone(); },
                    new[] { "Template", "Instance" });
        }

        public void DisplayOrEditEntityModelingKind(StackPanel stack,
            AdminShell.ModelingKind kind,
            Action<AdminShell.ModelingKind> setOutput,
            string instanceExceptionStatement = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind:", levelColors[1][0], levelColors[1][1]);

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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }
                    ))
                this.AddKeyValueRef(
                    stack, "kind", kind, ref kind.kind, null, repo,
                    v => { kind.kind = v as string; return new ModifyRepo.LambdaActionNone(); },
                    new[] { "Template", "Instance" });
        }

        //
        // HasSemantic
        //

        public void DisplayOrEditEntitySemanticId(StackPanel stack,
            AdminShell.SemanticId semanticId,
            Action<AdminShell.SemanticId> setOutput,
            string statement = null,
            bool checkForCD = false,
            string addExistingEntities = null,
            CopyPasteBuffer cpb = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Semantic ID:", levelColors[1][0], levelColors[1][1]);

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
                                () => { return checkForCD &&
                                    semanticId[0].type != AdminShell.Key.ConceptDescription; },
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
                        setOutput?.Invoke(new AdminShell.SemanticId());
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                this.AddKeyListKeys(
                    stack, "semanticId", semanticId.Keys, repo,
                    packages, AasxWpfControlLibrary.PackageCentral.Selector.MainAux,
                    addExistingEntities: addExistingEntities, addFromPool: true,
                    addPresetNames: bufferKeys.Item1,
                    addPresetKeyLists: bufferKeys.Item2,
                    jumpLambda: (kl) => {
                        return new ModifyRepo.LambdaActionNavigateTo(AdminShell.Reference.CreateNew(kl));
                    });
        }

        //
        // Qualifiable
        //

        public void DisplayOrEditEntityQualifierCollection(StackPanel stack,
            AdminShell.QualifierCollection qualifiers,
            Action<AdminShell.QualifierCollection> setOutput)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Qualifiable:", levelColors[1][0], levelColors[1][1]);

            if (this.SafeguardAccess(
                stack, repo, qualifiers, "Qualifiers:", "Create empty list of Qualifiers!",
                v =>
                {
                    setOutput?.Invoke(new AdminShell.QualifierCollection());
                    return new ModifyRepo.LambdaActionRedrawEntity();
                }))
            {
                this.QualifierHelper(stack, repo, qualifiers);
            }

        }

        //
        // DataSpecificationIEC61360
        //

        public void DisplayOrEditEntityDataSpecificationIEC61360(StackPanel stack,
            AdminShell.DataSpecificationIEC61360 dsiec)
        {
            // access
            if (stack == null || dsiec == null)
                return;

            // members
            this.AddGroup(
                        stack, "Data Specification Content IEC61360:", levelColors[1][0], levelColors[1][1]);

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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "preferredName", dsiec.preferredName, repo);

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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "shortName", dsiec.shortName, repo);

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
                v => { dsiec.unit = v as string; return new ModifyRepo.LambdaActionNone(); });

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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                this.AddKeyListKeys(
                    stack, "unitId", dsiec.unitId.Keys, repo, 
                    packages, AasxWpfControlLibrary.PackageCentral.Selector.MainAux,
                    AdminShell.Key.GlobalReference, addEclassIrdi: true);
            }

            this.AddKeyValueRef(
                stack, "valueFormat", dsiec, ref dsiec.valueFormat, null, repo,
                v => { dsiec.valueFormat = v as string; return new ModifyRepo.LambdaActionNone(); });

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
                    return new ModifyRepo.LambdaActionNone();
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
                v => { dsiec.symbol = v as string; return new ModifyRepo.LambdaActionNone(); });

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
                v => { dsiec.dataType = v as string; return new ModifyRepo.LambdaActionNone(); },
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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "definition", dsiec.definition, repo);
        }

        //
        // special Submodel References
        // 

        public void DisplayOrEditEntitySubmodelRef(StackPanel stack,
            AdminShell.SubmodelRef smref,
            Action<AdminShell.SubmodelRef> setOutput,
            string entityName)
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
                        return new ModifyRepo.LambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(
                    stack, $"{entityName} - Reference to describing Submodel:",
                    levelColors[1][0], levelColors[1][1]);
                this.AddKeyListKeys(
                    stack, $"{entityName}:", smref.Keys,
                    repo, packages, AasxWpfControlLibrary.PackageCentral.Selector.Main, "Submodel");
            }
        }
    }
}