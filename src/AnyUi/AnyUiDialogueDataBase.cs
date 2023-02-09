/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Text;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Newtonsoft.Json;
using static AnyUi.AnyUiDialogueDataOpenFile;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
    /// called functionality back to the calling function.
    /// </summary>
    /// <param name="error">True, if an error condition is met.</param>
    /// <param name="message">Message string</param>
    /// <returns>On of AnyUiMessageBoxResult</returns>
    public delegate Task<AnyUiMessageBoxResult> AnyUiMinimalInvokeMessageDelegate(bool error, string message);

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

    public class AnyUiDialogueDataMessageBox : AnyUiDialogueDataEmpty
    {
        public AnyUiMessageBoxButton Buttons = AnyUiMessageBoxButton.OKCancel;
        public AnyUiMessageBoxImage Image = AnyUiMessageBoxImage.None;
        public AnyUiMessageBoxResult ResultButton = AnyUiMessageBoxResult.None;

        public AnyUiDialogueDataMessageBox(
            string caption = "",
            string message = "",
            AnyUiMessageBoxButton buttons = AnyUiMessageBoxButton.OKCancel, 
            AnyUiMessageBoxImage image = AnyUiMessageBoxImage.None,
            double? maxWidth = null)
            : base(caption, maxWidth, message)
        {
            Buttons = buttons;
            Image = image;
        }
    }

    public class AnyUiDialogueDataOpenFile : AnyUiDialogueDataEmpty
    {
		/// <summary>
		/// Original file name. <c>TargetFileName</c> might be altered,
        /// when made available by upload or similar.
		/// </summary>
		public string OriginalFileName;

        /// <summary>
        /// Filename, under which the file is (at least temporarily) available)
        /// </summary>
        public string TargetFileName;

        /// <summary>
        /// The <c>Filter</c> string can be decomposed in single items.
        /// </summary>
		public class FilterItem
		{
			public string Name;
			public string Pattern;
		}

		/// <summary>
		/// Filter specification for certain file extension. Description and
		/// filter pattern each delimited by pipe ("|").
		/// Example: "AASX package files (*.aasx)|*.aasx|All files (*.*)|*.*"
		/// </summary>
		public string Filter;

        /// <summary>
        /// Filename, which is initially proposed when the dialogue is opened.
        /// </summary>
        public string ProposeFileName;

        /// <summary>
        /// If true will offer the user to select a user file (only Blazor) or
        /// a local file.
        /// </summary>
        public bool AllowUserFiles;

        /// <summary>
        /// True, if a user file name was selected instead of a local file.
        /// </summary>
        public bool ResultUserFile;

        /// <summary>
        /// Selection of multiple files is allowed.
        /// </summary>
        public bool Multiselect = false;

        /// <summary>
        /// If <c>Multiselect</c>, all selected filenames are listed here.
        /// </summary>
        public List<string> Filenames = new List<string>();

        public AnyUiDialogueDataOpenFile(
            string caption = "",
            double? maxWidth = null,
            string message = null,            
            string filter = null,
			string proposeFn = null)
            : base(caption, maxWidth)
        {
            HasModalSpecialOperation = true;
            Caption = "Open file";
            if (caption != null)
                Caption = caption;
            Message = "Please select a file via dedicated dialogue.";
            if (message != null)
                Message = message;
            Filter = filter;
            ProposeFileName = proposeFn;
        }

        public static IList<FilterItem> DecomposeFilter(string filter)
        {
            var res = new List<FilterItem>();
			
            if (filter != null)
			{
				var parts = filter.Split('|');
				for (int i = 0; i < parts.Length; i += 2)
					res.Add(new FilterItem()
					{
						Name = parts[i],
						Pattern = parts[i + 1]
					});
			}

            return res;
		}

		/// <summary>
		/// Takes decomposed filter item and applies its pattern to the provide file name
		/// </summary>
		/// <param name="fi">Decomposed filter item</param>
		/// <param name="fn">Ingoing filename</param>
		/// <param name="final">If 1, will set extension, if no extension is provided. 
        /// If 2, will enfoce that result filename has correct extension</param>
		public static string ApplyFilterItem(FilterItem fi, string fn, int final = 0)
        {
            // access
            if (fi == null || fn == null)
                return fn;

            // identif pattern?
			var m = Regex.Match(fi.Pattern, @"(\.\w+)");
			if (!m.Success)
				return fn;

            // extract
			var fiExt = m.Groups[1].ToString();
			var fnExt = System.IO.Path.GetExtension(fn);

			if (final == 0)
			{
				// only change if needed

				if (fnExt != "")
					fn = fn.Substring(0, fn.Length - fnExt.Length) + fiExt;
			}
			if (final == 1)
			{
				// add if empty
				if (fnExt == "" && fiExt != "" && fiExt != "*")
					fn += fiExt;
			}
			else
			{
				// enforce always
				fn = fn.Substring(0, fn.Length - fnExt.Length) + fiExt;
			}

            return fn;
		}
    }

	public class AnyUiDialogueDataSaveFile : AnyUiDialogueDataEmpty
	{
		/// <summary>
		/// Filename, under which the file shall be available.
		/// </summary>
		public string TargetFileName;

		/// <summary>
		/// Filter specification for certain file extension. Description and
		/// filter pattern each delimited by pipe ("|").
		/// Example: "AASX package files (*.aasx)|*.aasx|All files (*.*)|*.*"
		/// </summary>
		public string Filter;

		/// <summary>
		/// Filename, which is initially proposed when the dialogue is opened.
		/// </summary>
		public string ProposeFileName;

		/// <summary>
		/// If true will offer the user to select a user file (only Blazor) or
		/// a local file.
		/// </summary>
		public bool AllowUserFiles;

		/// <summary>
		/// If true will offer the user to select a local file.
		/// </summary>
		public bool AllowLocalFiles;

		/// <summary>
		/// This dialog distincts 3 kinds of location, how a "save as" file could
		/// be provided to the user.
		/// </summary>
		public enum LocationKind { Download, User, Local }

        /// <summary>
        /// Index of the filter selected by the user-
        /// </summary>
        public int FilterIndex;

		/// <summary>
		/// True, if a user file name was selected instead of a local file.
		/// </summary>
		public LocationKind Location;

		public AnyUiDialogueDataSaveFile(
			string caption = "",
			double? maxWidth = null,
			string message = null,
			string filter = null,
			string proposeFn = null)
			: base(caption, maxWidth)
		{
			HasModalSpecialOperation = true;
			Caption = "Open file";
			if (caption != null)
				Caption = caption;
			Message = "Please select a file via dedicated dialogue.";
			if (message != null)
				Message = message;
			Filter = filter;
			ProposeFileName = proposeFn;
		}
	}

	public class AnyUiDialogueDataDownloadFile : AnyUiDialogueDataEmpty
	{
		/// <summary>
		/// Filename, under which the file shall be available.
		/// </summary>
		public string Source;

		public AnyUiDialogueDataDownloadFile(
			string caption = "",
			double? maxWidth = null,
			string message = null,
			string source = "")
			: base(caption, maxWidth)
		{
			HasModalSpecialOperation = true;
			Caption = "Download file";
			if (caption != null)
				Caption = caption;
			Message = "Please select to download file.";
			if (message != null)
				Message = message;
			Source = source;
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
        public bool ReadOnly = false;
       
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
        public Aas.ConceptDescription ResultCD = null;

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
