/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Text;
using AdminShellNS;
using Newtonsoft.Json;

namespace AnyUi
{
    public enum AnyUiMessageBoxImage
    {
        None = 0,
        Hand = 16,
        Stop = 16,
        Error = 16,
        Question = 32,
        Exclamation = 48,
        Warning = 48,
        Asterisk = 64,
        Information = 64
    }

    public enum AnyUiMessageBoxResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    public enum AnyUiMessageBoxButton
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }

    /// <summary>
    /// This action can be used to pass minimal information/ errors/ questions from a 
    /// called functionality back to the colling function.
    /// </summary>
    /// <param name="error">True, if an error condition is met.</param>
    /// <param name="message">Message string</param>
    /// <returns>On of AnyUiMessageBoxResult</returns>
    public delegate AnyUiMessageBoxResult AnyUiMinimalInvokeMessageDelegate(bool error, string message);

    public class AnyUiDialogueDataBase
    {
        // flags
        public bool HasModalSpecialOperation = false;

        // In
        public string Caption;
        public double? MaxWidth;

        // Out
        public bool Result;

        public AnyUiDialogueDataBase(string caption = "", double? maxWidth = null)
        {
            this.Caption = caption;
            this.MaxWidth = maxWidth;
        }
    }

    public class AnyUiDialogueDataEmpty : AnyUiDialogueDataBase
    {
        public string Message = "Waiting for user dialogue.";

        public AnyUiDialogueDataEmpty(
            string caption = "",
            double? maxWidth = null,
            string message = null)
            : base(caption, maxWidth)
        {
            if (message != null)
                this.Message = message;
        }
    }

    public class AnyUiDialogueDataOpenFile : AnyUiDialogueDataEmpty
    {
        // out
        public string FileName;

        public AnyUiDialogueDataOpenFile(
            string caption = "",
            double? maxWidth = null,
            string message = null)
            : base(caption, maxWidth)
        {
            this.HasModalSpecialOperation = true;
            this.Message = "Please select a file via dedicated dialogue.";
            if (message != null)
                this.Message = message;
        }
    }

    public class AnyUiDialogueDataTextBox : AnyUiDialogueDataBase
    {
        public enum DialogueOptions { None, FilterAllControlKeys };

        public AnyUiMessageBoxImage Symbol;
        public DialogueOptions Options;
        public string Text = "";

        public AnyUiDialogueDataTextBox(
            string caption = "",
            double? maxWidth = null,
            AnyUiMessageBoxImage symbol = AnyUiMessageBoxImage.None,
            DialogueOptions options = DialogueOptions.None,
            string text = null)
            : base(caption, maxWidth)
        {
            this.Symbol = symbol;
            this.Options = options;
            if (text != null)
                this.Text = text;
        }
    }

    public class AnyUiDialogueDataChangeElementAttributes : AnyUiDialogueDataBase
    {
        public enum AttributeEnum { IdShort = 0, Description, ValueText }
        public static string[] AttributeNames = { "idShort", "description", "value as text" };

        public AttributeEnum AttributeToChange;
        public string AttributeLang = "en";

        public static string[] PatternPresets = new[]
        {
            "*",
            "^",
            "§",
            "ABC_*",
            "*_XYZ",
            "__???__*",
            "__??__<*"
        };

        public string Pattern = "*";

        public AnyUiDialogueDataChangeElementAttributes(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }
    }

    // resharper disable ClassNeverInstantiated.Global
    // resharper disable UnassignedField.Global

    public class AnyUiDialogueDataTextEditor : AnyUiDialogueDataBase
    {
        public class Preset
        {
            public string Name;
            public string[] Lines;

            public string Text { get => string.Join("\n", Lines); }
        }

        public string MimeType = "application/text";
        public string Text = "";
        public List<Preset> Presets;

        public AnyUiDialogueDataTextEditor(
            string caption = "",
            double? maxWidth = null,
            string mimeType = null,
            string text = null)
            : base(caption, maxWidth)
        {
            if (mimeType != null)
                MimeType = mimeType;
            if (text != null)
                Text = text;
        }
    }

    // resharper enable ClassNeverInstantiated.Global
    // resharper enable UnassignedField.Global

    public class AnyUiDialogueDataSelectEclassEntity : AnyUiDialogueDataBase
    {
        public enum SelectMode { General, IRDI, ConceptDescription }

        // in
        public SelectMode Mode = SelectMode.General;

        // out
        public string ResultIRDI = null;
        public AdminShell.ConceptDescription ResultCD = null;

        public AnyUiDialogueDataSelectEclassEntity(
            string caption = "",
            double? maxWidth = null,
            SelectMode mode = SelectMode.General)
            : base(caption, maxWidth)
        {
            Mode = mode;
        }
    }

    public class AnyUiDialogueListItem
    {
        public string Text = "";
        public object Tag = null;

        public AnyUiDialogueListItem() { }

        public AnyUiDialogueListItem(string text, object tag)
        {
            this.Text = text;
            this.Tag = tag;
        }
    }

    public class AnyUiDialogueDataSelectFromList : AnyUiDialogueDataBase
    {
        // in
        public List<AnyUiDialogueListItem> ListOfItems = null;
        public string[] AlternativeSelectButtons = null;

        // out
        public int ResultIndex = -1;
        public AnyUiDialogueListItem ResultItem = null;

        public AnyUiDialogueDataSelectFromList(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }
    }

    public class AnyUiDialogueDataProgress : AnyUiDialogueDataBase
    {
        public event Action<double, string> DataChanged;

        // in
        private double progress;
        private string info = "";

        public double Progress
        {
            get { return progress; }
            set { progress = value; DataChanged?.Invoke(progress, info); }
        }

        public string Info
        {
            get { return info; }
            set { info = value; DataChanged?.Invoke(progress, info); }
        }

        public AnyUiMessageBoxImage Symbol;

        // out
        // currently, no out

        public AnyUiDialogueDataProgress(
            string caption = "",
            double? maxWidth = null,
            string info = "",
            AnyUiMessageBoxImage symbol = AnyUiMessageBoxImage.None)
            : base(caption, maxWidth)
        {
            this.info = info;
            this.Symbol = symbol;
        }
    }

}
