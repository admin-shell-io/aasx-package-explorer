/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

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
