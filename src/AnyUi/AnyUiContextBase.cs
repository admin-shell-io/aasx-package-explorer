using System;
using System.Collections.Generic;
using System.Text;

namespace AnyUi
{
    /// <summary>
    /// Holds the overall context; for the specific implementations, will contain much more funtionality.
    /// Provides also hooks for the dialogues.
    /// </summary>
    public class AnyUiContextBase
    {
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
    }
}
