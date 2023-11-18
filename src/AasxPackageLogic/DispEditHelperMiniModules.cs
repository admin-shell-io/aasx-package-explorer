/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxCompatibilityModels;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using AnyUi;
using Extensions;
using Lucene.Net.Util;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using Aas = AasCore.Aas3_0;
using Samm = AasCore.Samm2_2_0;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class extends the basic helper functionalities of DispEditHelper by providing modules for display/
    /// editing disting modules of the GUI, such as the different (re-usable) Interfaces of the AAS entities
    /// </summary>
    public class DispEditHelperMiniModules : DispEditHelperCopyPaste
    {
        //
        // Inject a number of functions bigger than basics but smaller than modules
        //
        // In particular, some helper for lists of Qualifiers, IdentifierKeyValuePairs, Extensions
        // are provided.
        // Note: In theorey, these 3 functions have VERY SIMILARY structure, but are not unified to
        //       a generic one (yet), as the ratio between UI/glue logic and business logic per function
        //       is quite high.
        //

        //
        // Qualifiers
        //

        private bool PasteQualifierTextIntoExisting(
            string jsonInput,
            Aas.IQualifier qCurr)
        {
            var qIn = JsonConvert.DeserializeObject<Aas.Qualifier>(jsonInput);
            if (qCurr != null && qIn != null)
            {
                qCurr.Type = qIn.Type;
                qCurr.Value = qIn.Value;
                qCurr.ValueType = qIn.ValueType;
                if (qIn.ValueId != null)
                    qCurr.ValueId = qIn.ValueId;
                if (qIn.SemanticId != null)
                    qCurr.SemanticId = qIn.SemanticId;
                Log.Singleton.Info("Qualifier data taken from clipboard.");
                return true;
            }
            return false;
        }

        // ReSharper Disable once ClassNeverInstantiated.Global

        public class QualifierPreset
        {
            public string name = "";
            public List<string> upgradeFrom;
            public Aas.Qualifier qualifier = new Aas.Qualifier("", Aas.DataTypeDefXsd.String);
        }

        public List<QualifierPreset> ReadQualiferPresets(string pfn)
        {
            // access options
            if (pfn == null || !System.IO.File.Exists(pfn))
                return null;

            // read file contents
            var init = System.IO.File.ReadAllText(pfn);

            // special read
            JsonTextReader reader = new JsonTextReader(new StringReader(init));
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new AdminShellConverters.AdaptiveAasIClassConverter(
                AdminShellConverters.AdaptiveAasIClassConverter.ConversionMode.AasCore));
            var presets = serializer.Deserialize<List<QualifierPreset>>(reader);

            // give back
            return presets;
        }

        public QualifierPreset FindQualiferInPresets(List<QualifierPreset> presets, string name)
        {
            // access
            if (presets == null || name == null)
                return null;

            // iterate
            foreach (var qp in presets)
                if (true == qp.qualifier?.Type?.Trim().Equals(
                    name.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    return qp;

            // :-( no 
            return null;
        }

        public void QualiferUpgradeReferable(List<QualifierPreset> presets, IReferable rf)
        {
            // access
            if (presets == null || rf == null)
                return;
            if (!(rf is IQualifiable qlf && qlf.Qualifiers != null))
                return;

            // over presets
            foreach (var prs in presets)
            {
                // find old updgrade type in list of Qualifiers of Referable
                if (prs.upgradeFrom == null)
                    continue;

                // cross match
                List<IQualifier> toReplace = new List<IQualifier>();
                foreach (var uf in prs.upgradeFrom)
                    foreach (var q in qlf.Qualifiers)
                        if (uf != null && uf.Trim().Equals(
                            q?.Type?.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            toReplace.Add(q);

                // execute replace
                foreach (var tr in toReplace)
                {
                    qlf.Qualifiers.Remove(tr);
                    var nq = prs.qualifier.Copy();
                    nq.Value = tr.Value;
                    nq.ValueId = tr.ValueId;
                    qlf.Qualifiers.Add(nq);
                }
            }
        }

        public void QualifierHelper(
            AnyUiStackPanel stack, ModifyRepo repo,
            List<Aas.IQualifier> qualifiers,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            if (editMode)
            {
                // let the user control the number of references
                AddActionPanel(
                    stack, "Qualifier entities:",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("qualifier-blank", "Add blank",
                            "Adds an empty qualifier.")
                        .AddAction("qualifier-preset", "Add preset",
                            "Adds an qualifier given from the list of presets.")
                        .AddAction("qualifier-clipboard", "Add from clipboard",
                            "Adds an qualifier from parsed clipboard data (JSON).")
                        .AddAction("qualifier-del", "Delete last",
                            "Deletes last qualifier in the list."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            qualifiers.Add(new Aas.Qualifier("", Aas.DataTypeDefXsd.String));
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        }

                        if (buttonNdx == 1)
                        {
                            var pfn = Options.Curr.QualifiersFile;
                            try
                            {
                                // read
                                var presets = ReadQualiferPresets(pfn);
                                if (presets == null)
                                {
                                    Log.Singleton.Error(
                                        $"JSON file for Quialifer presets not defined nor existing ({pfn}).");
                                    return new AnyUiLambdaActionNone();
                                }

                                // define dialogue and map presets into dialogue items
                                var uc = new AnyUiDialogueDataSelectFromList();
                                uc.ListOfItems = presets.Select((pr)
                                        => new AnyUiDialogueListItem() { Text = pr.name, Tag = pr }).ToList();

                                // perform dialogue
                                this.context.StartFlyoverModal(uc);
                                if (uc.Result && uc.ResultItem?.Tag is QualifierPreset preset
                                    && preset.qualifier != null)
                                {
                                    qualifiers.Add(preset.qualifier);
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
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
                            try
                            {
                                var qNew = new Aas.Qualifier("", Aas.DataTypeDefXsd.String);
                                var jsonInput = this.context?.ClipboardGet()?.Text;
                                if (jsonInput?.HasContent() == true)
                                {
                                    if (PasteQualifierTextIntoExisting(jsonInput, qNew))
                                    {
                                        qualifiers.Add(qNew);
                                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                    }
                                }
                                else
                                {
                                    Log.Singleton.Error("Nothing found in the clipboard! Aborting.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "while accessing Qualifier data in clipboard");
                            }
                        }

                        if (buttonNdx == 3 && qualifiers.Count > 0)
                            qualifiers.RemoveAt(qualifiers.Count - 1);

                        return new AnyUiLambdaActionRedrawEntity();
                    });
            }

            for (int i = 0; i < qualifiers.Count; i++)
            {
                var qual = qualifiers[i];
                var substack = AddSubStackPanel(stack, "  ", minWidthFirstCol: GetWidth(FirstColumnWidth.Small));

                int storedI = i;
                AddGroup(
                    substack, $"Qualifier {1 + i}",
                    levelColors.SubSubSection.Bg, levelColors.SubSubSection.Fg, requestContextMenu: repo != null,
                    contextMenuText: "\u22ee",
                    menuHeaders: new[] {
                        "\u2702", "Delete",
                        "\u25b2", "Move Up",
                        "\u25bc", "Move Down",
                        "\u29c9", "Copy to clipboard",
                        "\u2398", "Paste from clipboard",
                    },
                    menuItemLambda: (o) =>
                    {
                        var action = false;

                        if (o is int ti)
                            switch (ti)
                            {
                                case 0:
                                    qualifiers.Remove(qual);
                                    action = true;
                                    break;
                                case 1:
                                    var resu = this.MoveElementInListUpwards<Aas.IQualifier>(
                                        qualifiers, qualifiers[storedI]);
                                    if (resu > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 2:
                                    var resd = this.MoveElementInListDownwards<Aas.IQualifier>(
                                        qualifiers, qualifiers[storedI]);
                                    if (resd > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 3:
                                    var jsonStr = JsonConvert.SerializeObject(
                                        qualifiers[storedI], Formatting.Indented);
                                    this.context?.ClipboardSet(new AnyUiClipboardData(jsonStr));
                                    Log.Singleton.Info("Qualified serialized to clipboard.");
                                    break;
                                case 4:
                                    try
                                    {
                                        var jsonInput = this.context?.ClipboardGet()?.Text;
                                        if (jsonInput?.HasContent() == true)
                                        {
                                            action = PasteQualifierTextIntoExisting(jsonInput, qualifiers[storedI]);
                                            if (action)
                                                Log.Singleton.Info("Qualifier taken from clipboard.");
                                        }
                                        else
                                        {
                                            Log.Singleton.Error("Nothing found in the clipboard! Aborting.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex, "while accessing Qualifier data in clipboard");
                                    }
                                    break;

                            }

                        if (action)
                        {
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    },
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0));

                // Qualifier members

                // SemanticId

                AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return (qual.SemanticId == null || qual.SemanticId.IsEmpty()) &&
                                    (qual.Type == null || qual.Type.Trim() == "");
                            },
                            "Either a semanticId or a type string specification shall be given!")
                    });
                if (SafeguardAccess(
                        substack, repo, qual.SemanticId, "semanticId:", "Create data element!",
                        v =>
                        {
                            qual.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyReference(
                        substack, "semanticId", qual.SemanticId, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All",
                        addEclassIrdi: true, addFromKnown: true,
                        showRefSemId: false,
                        relatedReferable: relatedReferable,
                        auxContextHeader: new[] { "\u2573", "Delete semanticId" },
                        auxContextLambda: (i) =>
                        {
                            if (i == 0)
                            {
                                qual.SemanticId = null;
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                            return new AnyUiLambdaActionNone();
                        });
                }

                // Kind

                if (this.SafeguardAccess(
                    substack, repo, qual.Kind, "kind:", "Create data element!",
                    v =>
                    {
                        qual.Kind = Aas.QualifierKind.ConceptQualifier;
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    ))
                {
                    AddKeyValueExRef(
                    substack, "kind", qual, Aas.Stringification.ToString(qual.Kind), null, repo,
                    v =>
                    {
                        qual.Kind = Aas.Stringification.QualifierKindFromString((string)v);
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(Aas.QualifierKind)));
                }

                // Type

                AddKeyValueExRef(
                    substack, "type", qual, qual.Type, null, repo,
                    v =>
                    {
                        qual.Type = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                // ValueType

                AddKeyValueExRef(
                    substack, "valueType", qual, Aas.Stringification.ToString(qual.ValueType), null, repo,
                    comboBoxIsEditable: editMode,
                    comboBoxItems: ExtendStringification.DataTypeXsdToStringArray().ToArray(),
                    comboBoxMinWidth: 190,
                    setValue: v =>
                    {
                        var vt = Aas.Stringification.DataTypeDefXsdFromString((string)v);
                        if (vt.HasValue)
                            qual.ValueType = vt.Value;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                // Value

                AddKeyValueExRef(
                    substack, "value", qual, qual.Value, null, repo,
                    v =>
                    {
                        qual.Value = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    limitToOneRowForNoEdit: true,
                    auxButtonTitles: new[] { "\u2261" },
                    auxButtonToolTips: new[] { "Edit in multiline editor" },
                    auxButtonLambda: (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var qInfo = (qual.Type.HasContent() ? qual.Type : "<no type given>");
                            var uc = new AnyUiDialogueDataTextEditor(
                                caption: $"Edit Extension '{qInfo}'",
                                mimeType: Aas.Stringification.ToString(qual.ValueType),
                                text: qual.Value);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                qual.Value = uc.Text;
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryUpdateValue());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });

                // ValueId

                if (SafeguardAccess(
                        substack, repo, qual.ValueId, "valueId:", "Create data element!",
                        v =>
                        {
                            qual.ValueId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyReference(substack, "valueId", qual.ValueId, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All", addFromKnown: true,
                        showRefSemId: false,
                        relatedReferable: relatedReferable,
                        auxContextHeader: new[] { "\u2573", "Delete valueId" },
                        auxContextLambda: (i) =>
                        {
                            if (i == 0)
                            {
                                qual.ValueId = null;
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                            return new AnyUiLambdaActionNone();
                        });
                }

            }

        }

        //
        // Key Value Pairs
        //

        private bool PasteIKVPTextIntoExisting(
            string jsonInput,
            Aas.ISpecificAssetId qCurr)
        {
            var qIn = JsonConvert.DeserializeObject<Aas.SpecificAssetId>(jsonInput);
            if (qCurr != null && qIn != null)
            {
                qCurr.Name = qIn.Name;
                qCurr.Value = qIn.Value;
                if (qIn.ExternalSubjectId != null)
                    qCurr.ExternalSubjectId = qIn.ExternalSubjectId;
                if (qIn.SemanticId != null)
                    qCurr.SemanticId = qIn.SemanticId;
                Log.Singleton.Info("SpecificAssetId data taken from clipboard.");
                return true;
            }
            return false;
        }

        // ReSharper Disable once ClassNeverInstantiated.Global

        /// <summary>
        /// This class defines the JSON format for presets.
        /// </summary>
        public class IdentifierKeyValuePairPreset
        {
            public string name = "";
            public Aas.SpecificAssetId pair = new("", "", null);
        }

        public void SpecificAssetIdSingleItemHelper(
            AnyUiStackPanel substack, ModifyRepo repo,
            Aas.ISpecificAssetId pair,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (substack == null || pair == null)
                return;

            // semanticId

            AddHintBubble(
                substack, hintMode,
                new[] {
                        new HintCheck(
                            () => pair.SemanticId?.IsValid() != true,
                            "Check, if a semanticId can be given in addition the key!",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    substack, repo, pair.SemanticId, "semanticId:", "Create data element!",
                    v =>
                    {
                        pair.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                AddKeyReference(
                    substack, "semanticId", pair.SemanticId, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                    addExistingEntities: "All",
                    addEclassIrdi: true, showRefSemId: false,
                    relatedReferable: relatedReferable);
            }

            // Name

            AddHintBubble(
                substack, hintMode,
                new[] {
                        new HintCheck(
                            () => !pair.Name.HasContent(),
                            "A key string specification shall be given!")
                });
            AddKeyValueExRef(
                substack, "name", pair, pair.Name, null, repo,
                v =>
                {
                    pair.Name = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // Value

            AddKeyValueExRef(
                substack, "value", pair, pair.Value, null, repo,
                v =>
                {
                    pair.Value = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            if (SafeguardAccess(
                    substack, repo, pair.ExternalSubjectId, "externalSubjectId:", "Create data element!",
                    v =>
                    {
                        pair.ExternalSubjectId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                AddKeyReference(substack, "externalSubjectId", pair.ExternalSubjectId, repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                    addExistingEntities: "All", addFromKnown: true, showRefSemId: false,
                    relatedReferable: relatedReferable);
            }

        }

        public void SpecificAssetIdHelper(
            AnyUiStackPanel stack, ModifyRepo repo,
            List<Aas.ISpecificAssetId> pairs,
            string key = "IdentifierKeyValuePairs",
            Aas.IReferable relatedReferable = null,
            bool constrainToOne = false)
        {
            if (editMode)
            {
                // let the user control the number of references
                AddActionPanel(
                    stack, $"{key}:",
                    new[] { "Add blank", "Add preset", "Add from clipboard", "Delete last" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0 && (!constrainToOne || pairs.Count < 1))
                        {
                            pairs.Add(new Aas.SpecificAssetId("", "", null));
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        }

                        if (buttonNdx == 1 && (!constrainToOne || pairs.Count < 1))
                        {
                            var pfn = Options.Curr.IdentifierKeyValuePairsFile;
                            if (pfn == null || !System.IO.File.Exists(pfn))
                            {
                                Log.Singleton.Error(
                                    $"JSON file for SpecificAssetId presets not defined nor existing ({pfn}).");
                                return new AnyUiLambdaActionNone();
                            }
                            try
                            {
                                // read file contents
                                var init = System.IO.File.ReadAllText(pfn);
                                var presets = JsonConvert.DeserializeObject<List<IdentifierKeyValuePairPreset>>(init);

                                // define dialogue and map presets into dialogue items
                                var uc = new AnyUiDialogueDataSelectFromList();
                                uc.ListOfItems = presets.Select((pr)
                                        => new AnyUiDialogueListItem() { Text = pr.name, Tag = pr }).ToList();

                                // perform dialogue
                                this.context.StartFlyoverModal(uc);
                                if (uc.Result && uc.ResultItem?.Tag is IdentifierKeyValuePairPreset preset
                                    && preset.pair != null)
                                {
                                    pairs.Add(preset.pair);
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"While show SpecificAssetId presets ({pfn})");
                            }
                        }

                        if (buttonNdx == 2 && (!constrainToOne || pairs.Count < 1))
                        {
                            try
                            {
                                var pNew = new Aas.SpecificAssetId("", "", null);
                                var jsonInput = this.context?.ClipboardGet()?.Text;
                                if (jsonInput?.HasContent() == true)
                                {
                                    if (PasteIKVPTextIntoExisting(jsonInput, pNew))
                                    {
                                        pairs.Add(pNew);
                                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                    }
                                }
                                else
                                {
                                    Log.Singleton.Error("Nothing found in the clipboard! Aborting.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "while accessing SpecificAssetId data in clipboard");
                            }
                        }

                        if (buttonNdx == 3 && pairs.Count > 0)
                            pairs.RemoveAt(pairs.Count - 1);

                        return new AnyUiLambdaActionRedrawEntity();
                    });
            }

            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                var substack = AddSubStackPanel(stack, "  ", GetWidth(FirstColumnWidth.Small));

                int storedI = i;
                AddGroup(
                    substack, $"Element {1 + i}: {AdminShellUtil.ShortenWithEllipses(pairs[storedI].Name, 30)}",
                    levelColors.SubSubSection.Bg, levelColors.SubSubSection.Fg, requestContextMenu: repo != null,
                    contextMenuText: "\u22ee",
                    menuHeaders: new[] {
                        "\u2702", "Delete",
                        "\u25b2", "Move Up",
                        "\u25bc", "Move Down",
                        "\u29c9", "Copy to clipboard",
                        "\u2398", "Paste from clipboard",
                    },
                    menuItemLambda: (o) =>
                    {
                        var action = false;

                        if (o is int ti)
                            switch (ti)
                            {
                                case 0:
                                    pairs.Remove(pair);
                                    action = true;
                                    break;
                                case 1:
                                    var resu = this.MoveElementInListUpwards<Aas.ISpecificAssetId>(
                                        pairs, pairs[storedI]);
                                    if (resu > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 2:
                                    var resd = this.MoveElementInListDownwards<Aas.ISpecificAssetId>(
                                        pairs, pairs[storedI]);
                                    if (resd > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 3:
                                    var jsonStr = JsonConvert.SerializeObject(pairs[storedI], Formatting.Indented);
                                    this.context?.ClipboardSet(new AnyUiClipboardData(jsonStr));
                                    Log.Singleton.Info("SpecificAssetId serialized to clipboard.");
                                    break;
                                case 4:
                                    try
                                    {
                                        var jsonInput = this.context?.ClipboardGet()?.Text;
                                        if (jsonInput?.HasContent() == true)
                                        {
                                            action = PasteIKVPTextIntoExisting(jsonInput, pairs[storedI]);
                                        }
                                        else
                                        {
                                            Log.Singleton.Error("Nothing found in the clipboard! Aborting.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex,
                                            "while accessing SpecificAssetId data in clipboard");
                                    }
                                    break;

                            }

                        if (action)
                        {
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    },
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0));

                // extra function for single pair, because of special cases (sigh!)
                SpecificAssetIdSingleItemHelper(substack, repo, pair, relatedReferable);
            }

        }

        //
        // Extensions
        //

        private bool PasteExtensionTextIntoExisting(
            string jsonInput,
            Aas.IExtension qCurr)
        {
            var qIn = JsonConvert.DeserializeObject<Aas.Extension>(jsonInput);
            if (qCurr != null && qIn != null)
            {
                qCurr.Name = qIn.Name;
                qCurr.ValueType = qIn.ValueType;
                qCurr.Value = qIn.Value;
                if (qIn.RefersTo != null)
                    qCurr.RefersTo = qIn.RefersTo;
                if (qIn.SemanticId != null)
                    qCurr.SemanticId = qIn.SemanticId;
                Log.Singleton.Info("Extension data taken from clipboard.");
                return true;
            }
            return false;
        }

        // ReSharper Disable once ClassNeverInstantiated.Global
        public class ExtensionPreset
        {
            public string name = "";
            public Aas.Extension extension = new Aas.Extension("");
        }

        // ReSharper Disable once ClassNeverInstantiated.Global
        public class DataSpecPreset
        {
            public string name = "";
            public string value = "";
        }

        public void ExtensionHelper(
            AnyUiStackPanel stack, ModifyRepo repo,
            List<Aas.IExtension> extensions,
            Action<List<Aas.IExtension>> setOutput,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (extensions == null)
                return;

            // header
            if (editMode)
            {
                // let the user control the number of elements
                AddActionPanel(
                    stack, "Extension entities:",
                    new[] { "Add blank", "Add preset", "Add from clipboard", "Delete last" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            extensions.Add(new Aas.Extension(""));
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        }

                        if (buttonNdx == 1)
                        {
                            var pfn = Options.Curr.ExtensionsPresetFile;
                            if (pfn == null || !System.IO.File.Exists(pfn))
                            {
                                Log.Singleton.Error(
                                    $"JSON file for IReferable.extension presets not defined nor existing ({pfn}).");
                                return new AnyUiLambdaActionNone();
                            }
                            try
                            {
                                // read file contents
                                var init = System.IO.File.ReadAllText(pfn);
                                var presets = JsonConvert.DeserializeObject<List<ExtensionPreset>>(init);

                                // define dialogue and map presets into dialogue items
                                var uc = new AnyUiDialogueDataSelectFromList();
                                uc.ListOfItems = presets.Select((pr)
                                        => new AnyUiDialogueListItem() { Text = pr.name, Tag = pr }).ToList();

                                // perform dialogue
                                this.context.StartFlyoverModal(uc);
                                if (uc.Result && uc.ResultItem?.Tag is ExtensionPreset preset
                                    && preset.extension != null)
                                {
                                    extensions.Add(preset.extension);
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
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
                            try
                            {
                                var eNew = new Aas.Extension("");
                                var jsonInput = this.context?.ClipboardGet()?.Text;
                                if (PasteExtensionTextIntoExisting(jsonInput, eNew))
                                {
                                    extensions.Add(eNew);
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "while accessing Extension data in clipboard");
                            }
                        }

                        if (buttonNdx == 3)
                        {
                            if (extensions.Count > 0)
                                extensions.RemoveAt(extensions.Count - 1);
                            else
                                setOutput?.Invoke(null);
                        }

                        return new AnyUiLambdaActionRedrawEntity();
                    });
            }

            for (int i = 0; i < extensions.Count; i++)
            {
                var extension = extensions[i];
                var substack = AddSubStackPanel(stack, "  ", minWidthFirstCol: GetWidth(FirstColumnWidth.Small));

                int storedI = i;
                AddGroup(
                    substack, $"Extension {1 + i}: {AdminShellUtil.ShortenWithEllipses(extension.Name, 30)}",
                    levelColors.SubSubSection.Bg, levelColors.SubSubSection.Fg, requestContextMenu: repo != null,
                    contextMenuText: "\u22ee",
                    menuHeaders: new[] {
                        "\u2702", "Delete",
                        "\u25b2", "Move Up",
                        "\u25bc", "Move Down",
                        "\u29c9", "Copy to clipboard",
                        "\u2398", "Paste from clipboard",
                    },
                    menuItemLambda: (o) =>
                    {
                        var action = false;

                        if (o is int ti)
                            switch (ti)
                            {
                                case 0:
                                    extensions.Remove(extension);
                                    action = true;
                                    break;
                                case 1:
                                    var resu = this.MoveElementInListUpwards<Aas.IExtension>(
                                        extensions, extensions[storedI]);
                                    if (resu > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 2:
                                    var resd = this.MoveElementInListDownwards<Aas.IExtension>(
                                        extensions, extensions[storedI]);
                                    if (resd > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 3:
                                    var jsonStr = JsonConvert.SerializeObject(
                                        extensions[storedI], Formatting.Indented);
                                    this.context?.ClipboardSet(new AnyUiClipboardData(jsonStr));
                                    Log.Singleton.Info("Extension serialized to clipboard.");
                                    break;
                                case 4:
                                    try
                                    {
                                        var jsonInput = this.context?.ClipboardGet()?.Text;
                                        action = PasteExtensionTextIntoExisting(jsonInput, extensions[storedI]);
                                        if (action)
                                            Log.Singleton.Info("Extension taken from clipboard.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex, "while accessing Extension data in clipboard");
                                    }
                                    break;

                            }

                        if (action)
                        {
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    },
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0));

                // special case: SAMM extension
                // TODO: enable
                if (false && Samm.Util.HasSammSemanticId(extension))
                {
                    substack.Add(new AnyUiLabel()
                    {
                        Content = "(special extension; see below)"
                    });
                }
                else
                {
                    AddHintBubble(
                        substack, hintMode,
                        new[] {
                        new HintCheck(
                            () => !extension.Name.HasContent(),
                            "A name specification shall be given and unqiue within this list!")
                        });
                    AddKeyValueExRef(
                        substack, "name", extension, extension.Name, null, repo,
                        v =>
                        {
                            extension.Name = v as string;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionNone();
                        });

                    AddHintBubble(
                        substack, hintMode,
                        new[] {
                        new HintCheck(
                            () => extension.SemanticId?.IsValid() != true,
                            "Check, if a semanticId can be given in addition the key!",
                            severityLevel: HintCheck.Severity.Notice)
                        });
                    if (SafeguardAccess(
                            substack, repo, extension.SemanticId, "semanticId:", "Create data element!",
                            v =>
                            {
                                extension.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            }))
                    {
                        AddVerticalSpace(substack);
                        AddKeyReference(
                            substack, "semanticId", extension.SemanticId, repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            showRefSemId: false,
                            addExistingEntities: "All", addFromKnown: true,
                            addEclassIrdi: true,
                            relatedReferable: relatedReferable,
                            auxContextHeader: new[] { "\u2573", "Delete semanticId" },
                            auxContextLambda: (i) =>
                            {
                                if (i == 0)
                                {
                                    extension.SemanticId = null;
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                                return new AnyUiLambdaActionNone();
                            });
                        AddVerticalSpace(substack);
                    }

                    AddKeyValueExRef(
                        substack, "valueType", extension, Aas.Stringification.ToString(extension.ValueType), null, repo,
                        comboBoxIsEditable: editMode,
                        //comboBoxItems: DataElement.ValueTypeItems,
                        //TODO (jtikekar, 0000-00-00): change
                        comboBoxItems: ExtendStringification.DataTypeXsdToStringArray().ToArray(),
                        comboBoxMinWidth: 190,
                        // dead-csharp off
                        //new string[] {
                        //"anyURI", "base64Binary",
                        //"boolean", "date", "dateTime",
                        //"dateTimeStamp", "decimal", "integer", "long", "int", "short", "byte", "nonNegativeInteger",
                        //"positiveInteger",
                        //"unsignedLong", "unsignedInt", "unsignedShort", "unsignedByte",
                        //"nonPositiveInteger", "negativeInteger",
                        //"double", "duration",
                        //"dayTimeDuration", "yearMonthDuration", "float", "hexBinary", "string", "langString", "time" },
                        // dead-csharp on
                        setValue: v =>
                        {
                            var vt = Aas.Stringification.DataTypeDefXsdFromString((string)v);
                            if (vt.HasValue)
                                extension.ValueType = vt.Value;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionNone();
                        });

                    AddKeyValueExRef(
                        substack, "value", extension, extension.Value, null, repo,
                        v =>
                        {
                            extension.Value = v as string;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionNone();
                        },
                        limitToOneRowForNoEdit: true,
                        auxButtonTitles: new[] { "\u2261" },
                        auxButtonToolTips: new[] { "Edit in multiline editor" },
                        auxButtonLambda: (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var uc = new AnyUiDialogueDataTextEditor(
                                                    caption: $"Edit Extension '{"" + extension.Name}'",
                                                    mimeType: Aas.Stringification.ToString(extension.ValueType),
                                                    text: extension.Value);
                                if (this.context.StartFlyoverModal(uc))
                                {
                                    extension.Value = uc.Text;
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryUpdateValue());
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        });

                    // refersTo are MULTIPLE ModelReference<IReferable>. That is: multiple x multiple keys!                
                    if (this.SafeguardAccess(
                            substack, this.repo, extension.RefersTo, "refersTo:", "Create data element!",
                            v =>
                            {
                                extension.RefersTo = new List<IReference>() { new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>()) };
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            }))
                    {
                        if (editMode)
                        {
                            // let the user control the number of references
                            this.AddActionPanel(
                                substack, "refersTo:",
                                new[] { "Add Reference", "Delete last reference" }, repo,
                                (buttonNdx) =>
                                {
                                    if (buttonNdx == 0)
                                    {
                                        if (extension.RefersTo == null)
                                            extension.RefersTo = new List<IReference>();
                                        extension.RefersTo.Add(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>()));
                                    }

                                    if (buttonNdx == 1 && extension.RefersTo != null)
                                    {
                                        if (extension.RefersTo.Count > 0)
                                            extension.RefersTo.Remove(extension.RefersTo.Last());
                                        else
                                            extension.RefersTo = null;
                                    }

                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                    return new AnyUiLambdaActionRedrawEntity();
                                });
                        }

                        // now use the normal mechanism to deal with editMode or not..
                        if (extension.RefersTo != null && extension.RefersTo != null)
                        {
                            for (int ki = 0; ki < extension.RefersTo.Count; ki++)
                                if (extension.RefersTo[ki] != null)
                                    this.AddKeyReference(
                                        substack, String.Format("refersTo[{0}]", ki),
                                        extension.RefersTo[ki],
                                        repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                                        addExistingEntities: "All", addFromKnown: true,
                                        addEclassIrdi: true,
                                        showRefSemId: false);
                        }
                    }
                }
            }

        }

        //
        // References
        //

        public void AddKeyReference(
            AnyUiStackPanel view, string key,
            Aas.IReference refkeys,
            ModifyRepo repo = null,
            PackageCentral.PackageCentral packages = null,
            PackageCentral.PackageCentral.Selector selector = PackageCentral.PackageCentral.Selector.Main,
            string addExistingEntities = null,
            bool addEclassIrdi = false,
            bool addFromKnown = false,
            string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
            Func<List<Aas.IKey>, AnyUiLambdaActionBase> jumpLambda = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            Func<List<Aas.IKey>, AnyUiLambdaActionBase> noEditJumpLambda = null,
            Aas.IReferable relatedReferable = null,
            Action<Aas.IReferable> emitCustomEvent = null,
            bool showRefSemId = true,
            Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
            string[] auxButtonTitles = null, string[] auxButtonToolTips = null,
            string[] auxContextHeader = null, Func<int, AnyUiLambdaActionBase> auxContextLambda = null)
        {
            // default
            if (emitCustomEvent == null)
                emitCustomEvent = (rf) => { this.AddDiaryEntry(rf, new DiaryEntryStructChange()); };

            //
            // extended Front panel
            //

            var frontPanel = AddSmallGrid(1, 3, new[] { "#", "#", "*" });

            AnyUiUIElement.RegisterControl(
                AddSmallComboBoxTo(
                    frontPanel, 0, 0,
                    margin: NormalOrCapa(
                        new AnyUiThickness(4, 2, 2, 2),
                        AnyUiContextCapability.Blazor, new AnyUiThickness(2, 1, 2, -1)),
                    padding: NormalOrCapa(
                        new AnyUiThickness(2, -1, 0, -1),
                        AnyUiContextCapability.Blazor, new AnyUiThickness(2, 1, 2, 3)),
                    text: "" + Aas.Stringification.ToString(refkeys.Type),
                    minWidth: 120,
                    items: Enum.GetValues(typeof(Aas.ReferenceTypes)).OfType<Aas.ReferenceTypes>().Select((rt) => Aas.Stringification.ToString(rt)).ToArray(),
                    isEditable: false,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                (o) =>
                {
                    if (o is string os)
                        refkeys.Type = Aas.Stringification.ReferenceTypesFromString(os).Value;
                    return new AnyUiLambdaActionNone();
                },
                takeOverLambda: takeOverLambdaAction);

            AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            frontPanel, 0, 1,
                            margin: NormalOrCapa(
                                new AnyUiThickness(2, 2, 2, 2),
                                AnyUiContextCapability.Blazor, new AnyUiThickness(6, 0, 0, 0)),
                            padding: NormalOrCapa(
                                new AnyUiThickness(5, 0, 5, 0),
                                AnyUiContextCapability.Blazor, new AnyUiThickness(0, 0, 0, 0)),
                            content: "\u21bb"),
                        (o) =>
                        {
                            refkeys.Type = refkeys.GuessType();
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        );

            //
            // referredSemanticId?
            //

            AnyUiStackPanel footerPanel = null;
            if (showRefSemId)
            {
                footerPanel = new AnyUiStackPanel()
                {
                    Margin = new AnyUiThickness(2, 2, 0, 0),
                    Background = AnyUiBrushes.LightGray
                };

                if (refkeys.ReferredSemanticId == null)
                {
                    if (repo != null)
                    {
                        var gx = AddSmallGrid(1, 3, new[] { "*", "#", "*" }, margin: new AnyUiThickness(2, 2, 0, 0));
                        footerPanel.Add(gx);

                        AddSmallBasicLabelTo(gx, 0, 0, content: "referredSemanticId:", verticalAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.RegisterControl(
                            AddSmallButtonTo(gx, 0, 1, content: "+",  // content: "\u25be",
                            margin: new AnyUiThickness(1),
                            padding: new AnyUiThickness(2, -2, 2, -2)),
                            (o) =>
                            {
                                refkeys.ReferredSemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                                return new AnyUiLambdaActionRedrawEntity();
                            });
                    }
                }
                else
                {
                    // careful! Full recursion of edit function
                    AddKeyReference(
                        footerPanel, "referredSem.Id", refkeys.ReferredSemanticId, repo,
                        packages, PackageCentral.PackageCentral.Selector.Main, addExistingEntities: "All",
                        showRefSemId: false,
                        addEclassIrdi: true,
                        addFromKnown: true,
                        relatedReferable: relatedReferable,
                        emitCustomEvent: emitCustomEvent,
                        auxContextHeader: new[] { "\u2573", "Delete referredSemanticId" },
                        auxContextLambda: (i) =>
                        {
                            if (i == 0)
                            {
                                refkeys.ReferredSemanticId = null;
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                            return new AnyUiLambdaActionNone();
                        });
                }
            }

            //
            // refer further
            //

            AddKeyListKeys(
                view, key, refkeys.Keys, repo, packages, selector,
                addExistingEntities, addEclassIrdi, addFromKnown, addPresetNames, addPresetKeyLists,
                jumpLambda, takeOverLambdaAction, noEditJumpLambda,
                relatedReferable,
                frontPanel: frontPanel,
                topContextMenu: true,
                footerPanel: footerPanel,
                auxButtonTitles: auxButtonTitles, auxButtonToolTips: auxButtonToolTips,
                auxButtonLambda: auxButtonLambda,
                auxContextHeader: auxContextHeader,
                auxContextLambda: auxContextLambda,
                emitCustomEvent: (o) =>
                {
                    // use the custom event as event for fired when changed keys
                    refkeys.Type = refkeys.GuessType();

                    // pass on
                    emitCustomEvent?.Invoke(o);
                });
        }

        //
        // ValueList of CD
        //

        private bool PasteValueReferencePairTextIntoExisting(
            string jsonInput,
            Aas.IValueReferencePair pCurr)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(jsonInput);
            var pIn = Aas.Jsonization.Deserialize.ValueReferencePairFrom(node);
            if (pCurr != null && pIn != null)
            {
                pCurr.Value = pIn.Value;
                if (pIn.ValueId != null)
                    pCurr.ValueId = pIn.ValueId.Copy();
                Log.Singleton.Info("ValueReferencePair data taken from clipboard.");
                return true;
            }
            return false;
        }


        public void ValueListHelper(
            Aas.Environment env,
            AnyUiStackPanel stack, ModifyRepo repo, string key,
            List<Aas.IValueReferencePair> valuePairs,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            if (editMode)
            {
                // let the user control the number of pairs
                AddActionPanel(
                    stack, $"{key}:",
                    new[] { "Add blank", "Add from clipboard", "Delete last" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            valuePairs.Add(new Aas.ValueReferencePair(
                                "",
                                new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey> {
                                    new Aas.Key(Aas.KeyTypes.GlobalReference, "")
                                })));
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        }

                        if (buttonNdx == 1)
                        {
                            try
                            {
                                var pNew = new Aas.ValueReferencePair("", null);
                                var jsonInput = this.context?.ClipboardGet()?.Text;
                                if (PasteValueReferencePairTextIntoExisting(jsonInput, pNew))
                                {
                                    valuePairs.Add(pNew);
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "while accessing ValueReferencePair data in clipboard");
                            }
                        }

                        if (buttonNdx == 2 && valuePairs.Count > 0)
                            valuePairs.RemoveAt(valuePairs.Count - 1);

                        return new AnyUiLambdaActionRedrawEntity();
                    });

                AddActionPanel(
                    stack, "Create:",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("create-cds", "CDs \U0001f844 pairs",
                            "For each Value /Reference pair, create a separate ConceptDescription."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // make sure
                            if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will create additional ConceptDescriptions for each " +
                                    "pair of Value and Reference. Do you want to proceed?",
                                    "Create CDs",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            // do it
                            for (int i = 0; i < valuePairs.Count; i++)
                            {
                                var eds = new Aas.EmbeddedDataSpecification(
                                    ExtendIDataSpecificationContent.GetReferencForIec61360(),
                                    new Aas.DataSpecificationIec61360(
                                        preferredName: ExtendILangStringPreferredNameTypeIec61360
                                            .CreateLangStringPreferredNameType(
                                                AdminShellUtil.GetDefaultLngIso639(), "" + valuePairs[i].Value),
                                        shortName: ExtendILangStringShortNameTypeIec61360
                                            .CreateLangStringShortNameType(
                                                AdminShellUtil.GetDefaultLngIso639(), "" + valuePairs[i].Value),
                                        definition: ExtendILangStringDefinitionTypeIec61360
                                            .CreateLangStringDefinitionType(
                                                AdminShellUtil.GetDefaultLngIso639(), "" + valuePairs[i].Value),
                                        dataType: Aas.DataTypeIec61360.StringTranslatable));

                                var cd = new Aas.ConceptDescription(
                                    id: valuePairs[i].ValueId?.GetAsIdentifier(),
                                    idShort: "" + valuePairs[i].Value,
                                    displayName: ExtendLangStringSet.CreateLangStringNameType(
                                        AdminShellUtil.GetDefaultLngIso639(), "" + valuePairs[i].Value),
                                    embeddedDataSpecifications: new List<Aas.IEmbeddedDataSpecification> { eds });

                                env?.Add(cd);
                            }

                            // display
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: relatedReferable);
                        }

                        return new AnyUiLambdaActionNone();
                    });
            }

            for (int i = 0; i < valuePairs.Count; i++)
            {
                var vp = valuePairs[i];
                var substack = AddSubStackPanel(stack, "", minWidthFirstCol: GetWidth(FirstColumnWidth.Small));

                int storedI = i;
                var txt = AdminShellUtil.ShortenWithEllipses(valuePairs[i].Value, 30);
                AddGroup(
                    substack, $"Pair {1 + i}: {txt}",
                    levelColors.SubSubSection.Bg, levelColors.SubSubSection.Fg, requestContextMenu: repo != null,
                    contextMenuText: "\u22ee",
                    menuHeaders: new[] {
                        "\u2702", "Delete",
                        "\u25b2", "Move Up",
                        "\u25bc", "Move Down",
                        "\u29c9", "Copy to clipboard",
                        "\u2398", "Paste from clipboard",
                    },
                    menuItemLambda: (o) =>
                    {
                        var action = false;

                        if (o is int ti)
                            switch (ti)
                            {
                                case 0:
                                    valuePairs.Remove(vp);
                                    action = true;
                                    break;
                                case 1:
                                    var resu = this.MoveElementInListUpwards<Aas.IValueReferencePair>(
                                        valuePairs, valuePairs[storedI]);
                                    if (resu > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 2:
                                    var resd = this.MoveElementInListDownwards<Aas.IValueReferencePair>(
                                        valuePairs, valuePairs[storedI]);
                                    if (resd > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 3:

                                    var jsonStr = Aas.Jsonization.Serialize.ToJsonObject(valuePairs[storedI])
                                            .ToJsonString(new System.Text.Json.JsonSerializerOptions()
                                            {
                                                WriteIndented = true
                                            });

                                    this.context?.ClipboardSet(new AnyUiClipboardData(jsonStr));
                                    Log.Singleton.Info("Value pair serialized to clipboard.");
                                    break;
                                case 4:
                                    try
                                    {
                                        var jsonInput = this.context?.ClipboardGet()?.Text;
                                        action = PasteValueReferencePairTextIntoExisting(jsonInput, valuePairs[storedI]);
                                        if (action)
                                            Log.Singleton.Info("Value pair taken from clipboard.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex, "while accessing ValueReferencePair data in clipboard");
                                    }
                                    break;

                            }

                        if (action)
                        {
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    },
                    margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0));

                AddKeyValueExRef(
                    substack, "value", vp, vp.Value, null, repo,
                    v =>
                    {
                        vp.Value = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                if (SafeguardAccess(
                        substack, repo, vp.ValueId, "valueId:", "Create data element!",
                        v =>
                        {
                            vp.ValueId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyReference(substack, "valueId", vp.ValueId, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: Aas.Stringification.ToString(Aas.KeyTypes.ConceptDescription),
                        addFromKnown: true, showRefSemId: false,
                        relatedReferable: relatedReferable);
                }
            }
        }

        /// <summary>
        /// Item of the organized relations expressed by SMT or SAMM concepts
        /// </summary>
        public class ConceptOrganizedChildItem
        {
            public Aas.IConceptDescription Cd;
            public SmtAttributeRecord SmtRec;
        }

        /// <summary>
        /// Information for the <c>Tag</c> of the data grid for new SMT guided elements
        /// </summary>
        protected class DispSmeListAddNewSmtItemRecord
        {
            public Aas.IConceptDescription Cd;
            public SmtAttributeRecord SmtRec;
            public Samm.ModelElement SammMe;
            public AasSubmodelElements? Sme;
        }

		protected static List<AnyUiDialogueDataGridRow> DispSmeListAddNewCheckForSmtItems(
            PackageCentral.PackageCentral packages,
			Aas.IReference basedOnSemanticId)
        {
            // access 
            var res = new List<AnyUiDialogueDataGridRow>();
            if (packages == null
                || basedOnSemanticId?.IsValid() != true
                || basedOnSemanticId.Count() != 1)
                return res;

            // check all ConceptDescriptions for the semanticId
            var cdId = basedOnSemanticId.Keys[0].Value;
            var candidates = new List<ConceptOrganizedChildItem>();
            foreach (var rftup in packages.QuickLookupAllIdent(cdId))
                if (rftup?.Item2 is Aas.ConceptDescription cd)
                {
                    // SMT extension
                    foreach (var smtRec in DispEditHelperExtensions
                        .CheckReferableForExtensionRecords<SmtAttributeRecord>(cd))
                    {    
                        foreach (var item in SmtAttributeRecord.FindChildElementsForConcept(packages, cd, smtRec))
                            candidates.Add(item);
                    }
                    
                    // SAMM extension
                    foreach (var me in DispEditHelperSammModules.CheckReferableForSammElements(cd))
                    {
                        foreach (var item in SammTransformation.FindChildElementsForConcept(packages, cd, me))
                            candidates.Add(item);
                    }
                }

            // check candidates further
            foreach (var cand in candidates)
            {
                // access
                if (cand.Cd == null || cand.SmtRec == null)
                    continue;                

				// Submodel
				if (cand.SmtRec.IsSubmodel)
                {
                    // basically makes no sense
                    res.Add(new AnyUiDialogueDataGridRow()
                    {
                        // Text = $"{cd.IdShort} (Submodel) {cd.Id}",
                        Cells = (new[] { "-", cand.SmtRec.CardinalityShort(), "SM", 
                            cand.Cd.IdShort, cand.Cd.Id }).ToList(),
                        Tag = new DispSmeListAddNewSmtItemRecord()
                        {
                            Cd = cand.Cd,
                            SmtRec = cand.SmtRec,
                            Sme = null
                        }
					});
                }
                else
                {
                    if (cand.SmtRec.SubmodelElements != null)
                        foreach (var smet in cand.SmtRec.SubmodelElements)
						    res.Add(new AnyUiDialogueDataGridRow()
						    {
							    // Text = $"{cd.IdShort} ({smet.ToString()}) {cd.Id}",
								Cells = (new[] { "-", cand.SmtRec.CardinalityShort(), 
                                    ExtendISubmodelElement.ToString(smet), 
                                    cand.Cd.IdShort, cand.Cd.Id }).ToList(),
								Tag = new DispSmeListAddNewSmtItemRecord()
								{
									Cd = cand.Cd,
									SmtRec = cand.SmtRec,
									Sme = smet
								}
							});
				}
            }

            // ok
            return res;
		}

        protected void DispSmeListAddNewDetailOnItems<T>(
            List<T> smeList,
            List<AnyUiDialogueDataGridRow> items) where T : class, ISubmodelElement
		{
            // access
            if (smeList == null || items == null)
                return;

            // for all items
            foreach (var item in items)
            {
                // access
                if (!(item.Tag is DispSmeListAddNewSmtItemRecord itrec)
                    || itrec.Cd?.Id == null)
                    continue;

                // search for sme's having the specific semantic id
                var cnt = smeList.Where((sme) => (sme?.SemanticId?
                            .Matches(KeyTypes.GlobalReference, itrec.Cd.Id, MatchMode.Relaxed) == true)).Count();
                if (cnt > 0)
                    // make sense to rework
                    item.Cells[0] = "" + cnt;
            }
        }

		/// <summary>
		/// Provides a menu to add a new SubmodelElement to a list of these.
		/// </summary>
		public void DispSmeListAddNewHelper<T>(
            Aas.Environment env,
            AnyUiStackPanel stack, ModifyRepo repo, string key,
            List<T> smeList,
            Action<List<T>> setValueLambda = null,
            AasxMenu superMenu = null,
            Aas.IReference basedOnSemanticId = null) where T : class, ISubmodelElement
        {
            // access
            if (stack == null)
                return;

            // gather potential SMT element items
            var smtElemItem = DispSmeListAddNewCheckForSmtItems(packages, basedOnSemanticId);

			// hint
			this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return smeList == null || smeList.Count < 1; },
                        "This element currently has no SubmodelElements, yet. " +
                            "These are the actual carriers of information. " +
                            "You could create them by clicking the 'Add ..' buttons below. " +
                            "Subsequently, when having a SubmodelElement established, " +
                            "you could add meaning by relating it to a ConceptDefinition.",
                        severityLevel: HintCheck.Severity.Notice)
                });

            // menu
            var isDataElem = typeof(IDataElement).IsAssignableFrom(typeof(T));
            var menu = new AasxMenu()
                    .AddAction("add-prop", "Add Property",
                        "Adds a new Property to the containing collection.")
                    .AddAction("add-mlp", "Add MultiLang.Prop.",
                        "Adds a new MultiLanguageProperty to the containing collection.");

            if (!isDataElem)
                menu.AddAction("add-smc", "Add Collection",
                   "Adds a new SubmodelElementCollection to the containing collection.");
            else
                menu.AddAction("add-range", "Add Range",
                   "Adds a new Range to the containing collection.");

            menu.AddAction("add-named", "Add other ..",
                        "Adds a selected kind of SubmodelElement to the containing collection.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Kind", "Name (not abbreviated) of kind of SubmodelElement."));

            if (smtElemItem.Count > 0)
            {
				menu.AddAction("add-smt-guided", "Add SMT guided ..",
				    "Adds a element based on SMT organized elements given by semanticId.");
			}

            this.AddActionPanel(
                stack, key,
                repo: repo, superMenu: superMenu,
                ticketMenu: menu,
                ticketAction: (buttonNdx, ticket) =>
                {
                    if (buttonNdx >= 0 && buttonNdx <= 3)
                    {
                        // which adequate type?
                        var en = Aas.AasSubmodelElements.SubmodelElement;
                        if (buttonNdx == 0)
                            en = Aas.AasSubmodelElements.Property;
                        if (buttonNdx == 1)
                            en = Aas.AasSubmodelElements.MultiLanguageProperty;
                        if (buttonNdx == 2 && !isDataElem)
                            en = Aas.AasSubmodelElements.SubmodelElementCollection;
                        if (buttonNdx == 2 && isDataElem)
                            en = Aas.AasSubmodelElements.Range;
                        if (buttonNdx == 3)
                        {
                            Aas.AasSubmodelElements[] includes = null;
                            if (isDataElem) includes = new Aas.AasSubmodelElements[] {
                                        Aas.AasSubmodelElements.SubmodelElementCollection,
                                        Aas.AasSubmodelElements.RelationshipElement,
                                        Aas.AasSubmodelElements.AnnotatedRelationshipElement,
                                        Aas.AasSubmodelElements.Capability,
                                        Aas.AasSubmodelElements.Operation,
                                        Aas.AasSubmodelElements.BasicEventElement,
                                        Aas.AasSubmodelElements.Entity};

                            en = this.SelectAdequateEnum("Select SubmodelElement to create ..", ticket: ticket,
                                includeValues: includes);
                        }

                        // ok?
                        if (en != Aas.AasSubmodelElements.SubmodelElement)
                        {
                            T sme2 = (T)
                                AdminShellUtil.CreateSubmodelElementFromEnum(en);

                            // add
                            T smw = sme2;
                            if (smeList == null)
                            {
                                smeList = new List<T>();
                                setValueLambda?.Invoke(smeList);
                            }

                            smeList = smeList ?? new List<T>();
                            smeList.Add(smw);
                            setValueLambda(smeList);

                            // make some more adjustments
                            if (sme2 is IMultiLanguageProperty mlp)
                            {
                                // create
                                mlp.Value = new List<ILangStringTextType>();

                                // add defaults?
                                if (Options.Curr.DefaultLang.HasContent())
                                    foreach (var lng in Options.Curr.DefaultLang.Split(',',
                                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                        mlp.Value.Add(new LangStringTextType("" + lng, ""));
                            }

                            // emit event
                            this.AddDiaryEntry(sme2, new DiaryEntryStructChange(StructuralChangeReason.Create));

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme2, isExpanded: true);
                        }
                    }

                    if (buttonNdx == 4)
                    {
                        // new SMT

                        // rework list
                        DispSmeListAddNewDetailOnItems(smeList, smtElemItem);

						// show list
						var uc = new AnyUiDialogueDataSelectFromDataGrid(
                                    "Select element(s) to be created guided by SMT attributes ..",
                                    maxWidth: 1400);
                        
                        uc.ColumnDefs = AnyUiListOfGridLength.Parse(new[] { "1*", "1*", "1*", "5*", "8*" });
                        uc.ColumnHeaders = new[] { "Present", "Card.", "Type", "IdShort", "Id" };
                        uc.Rows = smtElemItem;
                        
						this.context.StartFlyoverModal(uc);
                        var itemsAdded = 0;
                        ISubmodelElement lastSme = null;
                        if (uc.ResultItems != null)
                            foreach (var ucr in uc.ResultItems)
                                if (ucr?.Tag is DispSmeListAddNewSmtItemRecord item)
                                {
                                    // create from SMT and do NOT allow Submodels here
                                    if (item.SmtRec != null && item.Sme.HasValue && item.Cd != null)
                                    {
                                        // create a new SME
                                        var sme = AdminShellUtil.CreateSubmodelElementFromEnum(item.Sme.Value);
                                        if (sme == null)
                                        {
                                            Log.Singleton.Error("Creating type provided by SMT attributes.");
                                            return new AnyUiLambdaActionNone();
                                        }

                                        // populate by SMT attributes
                                        item.SmtRec.PopulateReferable(sme, item.Cd);

                                        // add & confirm
                                        var smw = sme as T;
                                        if (smw != null)
                                        {
                                            smeList = smeList ?? new List<T>();
                                            smeList.Add(smw);
                                            setValueLambda(smeList);

                                            this.AddDiaryEntry(sme, new DiaryEntryStructChange(
                                                StructuralChangeReason.Create));

                                            // statistics
                                            lastSme = sme;
                                            itemsAdded++;
                                        }
                                    }
                                }

                        // finalize
                        if (itemsAdded > 0)
                            Log.Singleton.Info($"{itemsAdded} elements guided by SMT were added.");

                        if (lastSme != null)
							return new AnyUiLambdaActionRedrawAllElements(nextFocus: lastSme, isExpanded: true);
					}

                    return new AnyUiLambdaActionNone();
                });
        }

        public static Aas.ISubmodel DispEditHelperCreateSubmodelFromSmtSamm(
            PackageCentral.PackageCentral packages,
            Aas.IEnvironment env,
            Aas.IConceptDescription rootCd,
            bool createChilds)
        {
            // access
            if (packages == null || env == null || rootCd == null)
                return null;

            // create a Submodel and link it to the first AAS
            var submodel = new Aas.Submodel(
                idShort: rootCd.IdShort,
                id: "" + AdminShellUtil.GenerateIdAccordingTemplate(Options.Curr.TemplateIdSubmodelInstance),
                administration: rootCd.Administration?.Copy(),
                semanticId: rootCd.GetCdReference());
            env.Submodels.Add(submodel);

            var aas1 = env?.AssetAdministrationShells?.FirstOrDefault();
            if (aas1 != null)
                aas1.Submodels.Add(submodel.GetReference());

            // lambda to recurse
            int numAdded = 0;
            int numErrors = 0;
            Func<Aas.IConceptDescription, List<Aas.ISubmodelElement>> lambdaCreateSmes = null;
            lambdaCreateSmes = (parentCd) =>
            {
                // start
                var res = new List<Aas.ISubmodelElement>();
                if (parentCd?.Id == null)
                    return res;

                // childs?
                var childInfo = DispSmeListAddNewCheckForSmtItems(
                    packages, basedOnSemanticId: parentCd.GetCdReference());

                // try to create
                foreach (var ci in childInfo)
                {
                    // get item?
                    if (!(ci.Tag is DispSmeListAddNewSmtItemRecord item))
                        continue;

                    // create a new SME
                    var sme = AdminShellUtil.CreateSubmodelElementFromEnum(item.Sme.Value);
                    if (sme == null)
                    {
                        Log.Singleton.Error("Creating type provided by SMT attributes.");
                        numErrors++;
                        continue;
                    }

                    // populate by SMT attributes
                    item.SmtRec.PopulateReferable(sme, item.Cd);

                    // add
                    res.Add(sme);
                    numAdded++;

                    // recurse
                    if (createChilds)
                    {
                        var childs2 = lambdaCreateSmes(item.Cd);
                        if (childs2 != null)
                            foreach (var c2 in childs2)
                                sme.Add(c2);
                    }
                }

                // result
                return res;
            };

            // into Submodel?
            submodel.SubmodelElements = new List<ISubmodelElement>();
            submodel.SubmodelElements.AddRange(lambdaCreateSmes(rootCd));

            // info
            Log.Singleton.Info($"Added {numAdded}. {numErrors} errors.");

            // ok 
            return submodel;
        }

    }
}
