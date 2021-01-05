using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxWpfControlLibrary.MiniMarkup
{
    public class MiniMarkupBase
    {
    }

    public class MiniMarkupRun : MiniMarkupBase
    {
        public string Text = "";
        public double? FontSize;
        public bool IsBold, IsMonospaced;
        public int? Padsize;

        public MiniMarkupRun() { }

        public MiniMarkupRun(string text, double? fontSize = null,
            bool isBold = false, bool isMonospaced = false, int? padsize = null)
        {
            Text = text;
            FontSize = fontSize;
            IsBold = isBold;
            IsMonospaced = isMonospaced;
            Padsize = padsize;
        }
    }

    public class MiniMarkupSequence : MiniMarkupBase
    {
        public List<MiniMarkupBase> Children = new List<MiniMarkupBase>();

        public MiniMarkupSequence() { }

        public MiniMarkupSequence(List<MiniMarkupBase> blocks)
        {
            if (blocks != null)
                Children.AddRange(blocks);
        }

        public MiniMarkupSequence(params MiniMarkupBase[] blocks)
        {
            if (blocks != null)
                Children.AddRange(blocks);
        }
    }

    public class MiniMarkupLine : MiniMarkupSequence
    {
        public MiniMarkupLine() { }

        public MiniMarkupLine(List<MiniMarkupBase> blocks) : base(blocks) { }

        public MiniMarkupLine(params MiniMarkupBase[] blocks) : base(blocks) { }
    }
}
