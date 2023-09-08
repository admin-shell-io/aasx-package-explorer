/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using System;

namespace AnyUi
{

    public class AnyUiDialogueDataTextEditorWithContextMenu : AnyUiDialogueDataTextEditor
    {
        public Func<AasxMenu> ContextMenuCreate = null;
        public AasxMenuActionDelegate ContextMenuAction = null;

        public AnyUiDialogueDataTextEditorWithContextMenu(
            string caption = "",
            double? maxWidth = null,
            string mimeType = null,
            string text = null)
            : base(caption, maxWidth, mimeType, text)
        {
        }
    }
}
