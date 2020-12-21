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
        // Dialogues
        public virtual bool StartModalDialogue(AnyUiDialogueDataBase dialogueData) 
        {
            return false;
        }
    }
}
