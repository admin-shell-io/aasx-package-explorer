/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AnyUi
{
    /// <summary>
    /// This interface marks items which stand for selected items in the tree of AAS elements. 
    /// </summary>
    public interface IAnyUiSelectedItem { }

    public class AnyUiClipboardData
    {
        /// <summary>
        /// If a special watermark is found in the system clipboard, this is returned.
        /// Else <c>null</c>.
        /// </summary>
        public string Watermark;

        /// <summary>
        /// If text content is present in the clipboard.
        /// </summary>
        public string Text;

        public AnyUiClipboardData(string text = null, string watermark = null)
        {
            Watermark = watermark;
            Text = text;
        }
    }

    /// <summary>
    /// Holds the overall context; for the specific implementations, will contain much more funtionality.
    /// Provides also hooks for the dialogues.
    /// </summary>
    public class AnyUiContextBase
    {
        /// <summary>
        /// This function is called from multiple places inside this class to emit an labda action
        /// to the superior logic of the application
        /// </summary>
        /// <param name="action"></param>
        public virtual void EmitOutsideAction(AnyUiLambdaActionBase action)
        {

        }

        /// <summary>
        /// Tries to revert changes in some controls.
        /// </summary>
        /// <returns>True, if changes were applied</returns>
        public virtual bool CallUndoChanges(AnyUiUIElement root)
        {
            return false;
        }

        /// <summary>
        /// If supported by implementation technology, will set Clipboard (copy/ paste buffer)
        /// of the main application computer.
        /// </summary>
        public virtual void ClipboardSet(AnyUiClipboardData cb)
        {
        }

        /// <summary>
        /// If supported by implementation technology, will get Clipboard (copy/ paste buffer)
        /// of the main application computer.
        /// </summary>
        public virtual AnyUiClipboardData ClipboardGet()
        {
            return null;
        }

        /// <summary>
        /// Graphically highlights/ marks an element to be "selected", e.g for seacg/ replace
        /// operations.
        /// </summary>
        /// <param name="el">AnyUiElement</param>
        /// <param name="highlighted">True for highlighted, set for clear state</param>
        public virtual void HighlightElement(AnyUiFrameworkElement el, bool highlighted)
        {
        }

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        public virtual AnyUiMessageBoxResult MessageBoxFlyoutShow(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            return AnyUiMessageBoxResult.Cancel;
        }

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        public async virtual Task<AnyUiMessageBoxResult> MessageBoxFlyoutShowAsync(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            await Task.Yield();
            return AnyUiMessageBoxResult.Cancel;
        }

        /// <summary>
        /// Shows an open file dialogue
        /// </summary>
        /// <param name="caption">Top caption of the dialogue</param>
        /// <param name="message">Further information to the user</param>
        /// <param name="filter">Filter specification for certain file extension.</param>
        /// <param name="proposeFn">Filename, which is initially proposed when the dialogue is opened.</param>
        /// <returns>Dialogue data including filenames</returns>
        public virtual AnyUiDialogueDataOpenFile OpenFileFlyoutShow(
            string caption,
            string message,
            string proposeFn = null,
            string filter = null)
        {
            return null;
        }

        /// <summary>
        /// Shows specified dialogue hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// Modal dialogue: this function will block, until user ends dialogue.
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public virtual bool StartFlyoverModal(AnyUiDialogueDataBase dialogueData)
        {
            return false;
        }

        /// <summary>
		/// Shows specified dialogue hardware-independent. The technology implementation will show the
		/// dialogue based on the type of provided <c>dialogueData</c>. 
		/// Modal dialogue: this function will block, until user ends dialogue.
		/// </summary>
		/// <param name="dialogueData"></param>
		/// <returns>If the dialogue was end with "OK" or similar success.</returns>
		public async virtual Task<bool> StartFlyoverModalAsync(AnyUiDialogueDataBase dialogueData, Action rerender = null)
        {
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// Shows specified dialogue hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// Non-modal: This function wil return immideately after initially displaying the dialogue.
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public virtual void StartFlyover(AnyUiDialogueDataBase dialogueData)
        {
        }

        /// <summary>
        /// Closes started flyover dialogue-
        /// </summary>
        public virtual void CloseFlyover()
        {
        }

        //
        // some special functions
        // TODO (MIHO, 2020-12-24): check if to move/ refactor these functions
        //

        public virtual void PrintSingleAssetCodeSheet(
            string assetId, string description, string title = "Single asset code sheet")
        { }

        /// <summary>
        /// Set by the implementation technology (derived class of this), if Shift is pressed
        /// </summary>
        public bool ActualShiftState = false;

        /// <summary>
        /// Set by the implementation technology (derived class of this), if Cntrl is pressed
        /// </summary>
        public bool ActualControlState = false;

        /// <summary>
        /// Set by the implementation technology (derived class of this), if Alt is pressed
        /// </summary>
        public bool ActualAltState = false;

        /// <summary>
        /// Returns the selected items in the tree, which are provided by the implementation technology
        /// (derived class of this).
        /// Note: these would be of type <c>VisualElementGeneric</c>, but is in other assembly.
        /// </summary>
        /// <returns></returns>
        public virtual List<IAnyUiSelectedItem> GetSelectedItems()
        {
            return null;
        }

    }
}
