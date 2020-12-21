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

        public class AnyUiDialogueDataBase
    {
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
}
