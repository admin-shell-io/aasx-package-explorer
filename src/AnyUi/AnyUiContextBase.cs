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
        /// Show selected dialogues hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public virtual bool StartModalDialogue(AnyUiDialogueDataBase dialogueData)
        {
            return false;
        }
    }
}
