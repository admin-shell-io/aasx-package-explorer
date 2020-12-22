using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPackageExplorer;
using AasxWpfControlLibrary;
using AdminShellNS;

namespace AnyUi.AAS
{
    public class AnyUiDialogueDataSelectAasEntity : AnyUiDialogueDataBase
    {
        // in
        public PackageCentral.Selector Selector;
        public string Filter;

        // out
        public AdminShell.KeyList ResultKeys;
        public VisualElementGeneric ResultVisualElement;

        public AnyUiDialogueDataSelectAasEntity(
            string caption = "",
            double? maxWidth = null,
            PackageCentral.Selector? selector = PackageCentral.Selector.Main,
            string filter = null)
            : base(caption, maxWidth)
        {
            if (selector.HasValue)
                Selector = selector.Value;
            if (filter != null)
                Filter = filter;
        }
    }

    public class AnyUiDialogueDataSelectReferableFromPool : AnyUiDialogueDataBase
    {
        // in
        // (the pool will be provided by the technology implementation)

        // out
        public int ResultIndex = -1;
        public object ResultItem = null;

        public AnyUiDialogueDataSelectReferableFromPool(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }
    }
}
