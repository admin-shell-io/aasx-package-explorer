using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Text;

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

    public class AnyUiDialogueDataTextEditor : AnyUiDialogueDataBase
    {
        public string MimeType = "application/text";
        public string Text = "";

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
        
        public double Progress { 
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
