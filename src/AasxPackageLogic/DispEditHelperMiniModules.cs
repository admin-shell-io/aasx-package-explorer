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
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

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
            AdminShell.Qualifier qCurr)
        {
            var qIn = JsonConvert.DeserializeObject<AdminShell.Qualifier>(jsonInput);
            if (qCurr != null && qIn != null)
            {
                qCurr.type = qIn.type;
                qCurr.value = qIn.value;
                qCurr.valueType = qIn.valueType;
                if (qIn.valueId != null)
                    qCurr.valueId = qIn.valueId;
                if (qIn.semanticId != null)
                    qCurr.semanticId = qIn.semanticId;
                Log.Singleton.Info("Qualifier data taken from clipboard.");
                return true;
            }
            return false;
        }

        // ReSharper Disable once ClassNeverInstantiated.Global

        public class QualifierPreset
        {
            public string name = "";
            public AdminShell.Qualifier qualifier = new AdminShell.Qualifier();
        }

        public void QualifierHelper(
            AnyUiStackPanel stack, ModifyRepo repo,
            List<AdminShell.Qualifier> qualifiers,
            AdminShell.Referable relatedReferable = null)
        {
            if (editMode)
            {
                // let the user control the number of references
                AddAction(
                    stack, "Qualifier entities:",
                    new[] { "Add blank", "Add preset", "Add from clipboard", "Delete last" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            qualifiers.Add(new AdminShell.Qualifier());
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        }

                        if (buttonNdx == 1)
                        {
                            var pfn = Options.Curr.QualifiersFile;
                            if (pfn == null || !File.Exists(pfn))
                            {
                                Log.Singleton.Error(
                                    $"JSON file for Quialifer presets not defined nor existing ({pfn}).");
                                return new AnyUiLambdaActionNone();
                            }
                            try
                            {
                                // read file contents
                                var init = File.ReadAllText(pfn);
                                var presets = JsonConvert.DeserializeObject<List<QualifierPreset>>(init);

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
                                var qNew = new AdminShell.Qualifier();
                                var jsonInput = this.context?.ClipboardGet()?.Text;
                                if (PasteQualifierTextIntoExisting(jsonInput, qNew))
                                {
                                    qualifiers.Add(qNew);
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
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
                var substack = AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                int storedI = i;
                AddGroup(
                    substack, $"Qualifier {1 + i}",
                    levelColors.SubSubSection.Bg, levelColors.SubSubSection.Fg, repo,
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
                                    var res = this.MoveElementInListUpwards<AdminShell.Qualifier>(
                                        qualifiers, qualifiers[storedI]);
                                    if (res > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 2:
                                    action = true;
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
                                        action = PasteQualifierTextIntoExisting(jsonInput, qualifiers[storedI]);
                                        if (action)
                                            Log.Singleton.Info("Qualifier taken from clipboard.");
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

                AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return (qual.semanticId == null || qual.semanticId.IsEmpty) &&
                                    (qual.type == null || qual.type.Trim() == "");
                            },
                            "Either a semanticId or a type string specification shall be given!")
                    });
                if (SafeguardAccess(
                        substack, repo, qual.semanticId, "semanticId:", "Create data element!",
                        v =>
                        {
                            qual.semanticId = new AdminShell.SemanticId();
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyListOfIdentifier(
                        substack, "semanticId", qual.semanticId.Value, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: AdminShell.Key.AllElements,
                        addEclassIrdi: true,
                        relatedReferable: relatedReferable);
                }

                AddKeyValueRef(
                    substack, "type", qual, ref qual.type, null, repo,
                    v =>
                    {
                        qual.type = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueRef(
                    substack, "value", qual, ref qual.value, null, repo,
                    v =>
                    {
                        qual.value = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                if (SafeguardAccess(
                        substack, repo, qual.valueId, "valueId:", "Create data element!",
                        v =>
                        {
                            qual.valueId = new AdminShell.GlobalReference();
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyListOfIdentifier(substack, "valueId", qual.valueId.Value, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        relatedReferable: relatedReferable);
                }

            }

        }

        //
        // Key Value Pairs
        //

        private bool PasteIKVPTextIntoExisting(
            string jsonInput,
            AdminShell.IdentifierKeyValuePair qCurr)
        {
            var qIn = JsonConvert.DeserializeObject<AdminShell.IdentifierKeyValuePair>(jsonInput);
            if (qCurr != null && qIn != null)
            {
                qCurr.key = qIn.key;
                qCurr.value = qIn.value;
                if (qIn.externalSubjectId != null)
                    qCurr.externalSubjectId = qIn.externalSubjectId;
                if (qIn.semanticId != null)
                    qCurr.semanticId = qIn.semanticId;
                Log.Singleton.Info("IdentifierKeyValuePair data taken from clipboard.");
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
            public AdminShell.IdentifierKeyValuePair pair = new AdminShell.IdentifierKeyValuePair();
        }

        public void IdentifierKeyValuePairHelper(
            AnyUiStackPanel stack, ModifyRepo repo,
            AdminShell.ListOfIdentifierKeyValuePair pairs,
            AdminShell.Referable relatedReferable = null)
        {
            if (editMode)
            {
                // let the user control the number of references
                AddAction(
                    stack, "IdentifierKeyValuePairs:",
                    new[] { "Add blank", "Add preset", "Add from clipboard", "Delete last" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            pairs.Add(new AdminShell.IdentifierKeyValuePair());
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        }

                        if (buttonNdx == 1)
                        {
                            var pfn = Options.Curr.IdentifierKeyValuePairsFile;
                            if (pfn == null || !File.Exists(pfn))
                            {
                                Log.Singleton.Error(
                                    "JSON file for IdentifierKeyValuePair presets not defined nor existing ({pfn}).");
                                return new AnyUiLambdaActionNone();
                            }
                            try
                            {
                                // read file contents
                                var init = File.ReadAllText(pfn);
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
                                    ex, $"While show IdentifierKeyValuePair presets ({pfn})");
                            }
                        }

                        if (buttonNdx == 2)
                        {
                            try
                            {
                                var pNew = new AdminShell.IdentifierKeyValuePair();
                                var jsonInput = this.context?.ClipboardGet()?.Text;
                                if (PasteIKVPTextIntoExisting(jsonInput, pNew))
                                {
                                    pairs.Add(pNew);
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "while accessing IdentifierKeyValuePair data in clipboard");
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
                var substack = AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                int storedI = i;
                AddGroup(
                    substack, $"Pair {1 + i}",
                    levelColors.SubSubSection.Bg, levelColors.SubSubSection.Fg, repo,
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
                                    var res = this.MoveElementInListUpwards<AdminShell.IdentifierKeyValuePair>(
                                        pairs, pairs[storedI]);
                                    if (res > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 2:
                                    action = true;
                                    break;
                                case 3:
                                    var jsonStr = JsonConvert.SerializeObject(pairs[storedI], Formatting.Indented);
                                    this.context?.ClipboardSet(new AnyUiClipboardData(jsonStr));
                                    Log.Singleton.Info("IdentifierKeyValuePair serialized to clipboard.");
                                    break;
                                case 4:
                                    try
                                    {
                                        var jsonInput = this.context?.ClipboardGet()?.Text;
                                        action = PasteIKVPTextIntoExisting(jsonInput, pairs[storedI]);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex, 
                                            "while accessing IdentifierKeyValuePair data in clipboard");
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

                AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => !(pair.semanticId?.IsValid == true || pair.key.HasContent()),
                            "Either key string specification or (at leaset) a semanticId shall be given!")
                    });
                if (SafeguardAccess(
                        substack, repo, pair.semanticId, "semanticId:", "Create data element!",
                        v =>
                        {
                            pair.semanticId = new AdminShell.SemanticId();
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyListOfIdentifier(
                        substack, "semanticId", pair.semanticId.Value, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: AdminShell.Key.AllElements,
                        addEclassIrdi: true,
                        relatedReferable: relatedReferable);
                }

                AddKeyValueRef(
                    substack, "key", pair, ref pair.key, null, repo,
                    v =>
                    {
                        pair.key = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueRef(
                    substack, "value", pair, ref pair.value, null, repo,
                    v =>
                    {
                        pair.value = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                if (SafeguardAccess(
                        substack, repo, pair.externalSubjectId, "externalSubjectId:", "Create data element!",
                        v =>
                        {
                            pair.externalSubjectId = new AdminShell.GlobalReference();
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyListOfIdentifier(substack, "externalSubjectId", pair.externalSubjectId.Value, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, AdminShell.Key.AllElements,
                        relatedReferable: relatedReferable);
                }

            }

        }

        //
        // Extensions
        //

        private bool PasteExtensionTextIntoExisting(
            string jsonInput,
            AdminShell.Extension qCurr)
        {
            var qIn = JsonConvert.DeserializeObject<AdminShell.Extension>(jsonInput);
            if (qCurr != null && qIn != null)
            {
                qCurr.name = qIn.name;
                qCurr.valueType = qIn.valueType;
                qCurr.value = qIn.value;
                if (qIn.refersTo != null)
                    qCurr.refersTo = qIn.refersTo;
                if (qIn.semanticId != null)
                    qCurr.semanticId = qIn.semanticId;
                Log.Singleton.Info("Extension data taken from clipboard.");
                return true;
            }
            return false;
        }

        // ReSharper Disable once ClassNeverInstantiated.Global
        public class ExtensionPreset
        {
            public string name = "";
            public AdminShell.Extension extension = new AdminShell.Extension();
        }

        public void ExtensionHelper(
            AnyUiStackPanel stack, ModifyRepo repo,
            List<AdminShell.Extension> extensions,
            AdminShell.Referable relatedReferable = null)
        {
            if (editMode)
            {
                // let the user control the number of elements
                AddAction(
                    stack, "Extension entities:",
                    new[] { "Add blank", "Add preset", "Add from clipboard", "Delete last" },
                    repo,
                    (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            extensions.Add(new AdminShell.Extension());
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        }

                        if (buttonNdx == 1)
                        {
                            var pfn = Options.Curr.ExtensionsPresetFile;
                            if (pfn == null || !File.Exists(pfn))
                            {
                                Log.Singleton.Error(
                                    $"JSON file for Referable.extension presets not defined nor existing ({pfn}).");
                                return new AnyUiLambdaActionNone();
                            }
                            try
                            {
                                // read file contents
                                var init = File.ReadAllText(pfn);
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
                                var eNew = new AdminShell.Extension();
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

                        if (buttonNdx == 3 && extensions.Count > 0)
                            extensions.RemoveAt(extensions.Count - 1);

                        return new AnyUiLambdaActionRedrawEntity();
                    });
            }

            for (int i = 0; i < extensions.Count; i++)
            {
                var extension = extensions[i];
                var substack = AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                int storedI = i;
                AddGroup(
                    substack, $"Extension {1 + i}",
                    levelColors.SubSubSection.Bg, levelColors.SubSubSection.Fg, repo,
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
                                    var res = this.MoveElementInListUpwards<AdminShell.Extension>(
                                        extensions, extensions[storedI]);
                                    if (res > -1)
                                    {
                                        action = true;
                                    }
                                    break;
                                case 2:
                                    action = true;
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

                AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => !(extension.semanticId?.IsValid == true || extension.name.HasContent()),
                            "Either a semanticId or a name specification shall be given!")
                    });
                if (SafeguardAccess(
                        substack, repo, extension.semanticId, "semanticId:", "Create data element!",
                        v =>
                        {
                            extension.semanticId = new AdminShell.SemanticId();
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    AddKeyListOfIdentifier(
                        substack, "semanticId", extension.semanticId.Value, repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: AdminShell.Key.AllElements,
                        addEclassIrdi: true,
                        relatedReferable: relatedReferable);
                }

                AddKeyValueRef(
                    substack, "name", extension, ref extension.name, null, repo,
                    v =>
                    {
                        extension.name = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueRef(
                    substack, "valueType", extension, ref extension.valueType, null, repo,
                    comboBoxIsEditable: editMode,
                    comboBoxItems: AdminShell.DataElement.ValueTypeItems,
                    setValue: v =>
                    {
                        extension.valueType = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueRef(
                    substack, "value", extension, ref extension.value, null, repo,
                    v =>
                    {
                        extension.value = v as string;
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
                                                caption: $"Edit Extension '{"" + extension.name}'",
                                                mimeType: extension.valueType,
                                                text: extension.value);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                extension.value = uc.Text;
                                this.AddDiaryEntry(relatedReferable, new DiaryEntryUpdateValue());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });

                // refersTo are MULTIPLE ModelReference<Referable>. That is: multiple x multiple keys!
                this.AddHintBubble(substack, hintMode, new[] {
                new HintCheck(
                    () => false,
                    "Check if a refersTo specification is appropriate here.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice) });
                if (this.SafeguardAccess(
                        substack, this.repo, extension.refersTo, "refersTo:", "Create data element!",
                        v =>
                        {
                            extension.refersTo = new List<AdminShell.ModelReference>();
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    if (editMode)
                    {
                        // let the user control the number of references
                        this.AddAction(
                            substack, "refersTo:",
                            new[] { "Add Reference", "Delete last reference" }, repo,
                            (buttonNdx) =>
                            {
                                if (buttonNdx == 0)
                                    extension.refersTo.Add(new AdminShell.ModelReference());

                                if (buttonNdx == 1 && extension.refersTo.Count > 0)
                                    extension.refersTo.RemoveAt(
                                        extension.refersTo.Count - 1);

                                this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            });
                    }

                    // now use the normal mechanism to deal with editMode or not ..
                    if (extension.refersTo != null && extension.refersTo.Count > 0)
                    {
                        for (int ki = 0; ki < extension.refersTo.Count; ki++)
                            if (extension.refersTo[ki] != null)
                                this.AddKeyListKeys(
                                    substack, String.Format("refersTo[{0}]", ki),
                                    extension.refersTo[ki]?.Keys,
                                    repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                                    addExistingEntities: null /* "All" */);
                    }
                }

            }

        }

    }
}
