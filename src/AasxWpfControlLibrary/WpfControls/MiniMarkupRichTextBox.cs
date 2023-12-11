/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using AasxIntegrationBase.MiniMarkup;
using AasxPackageExplorer;
using AasxPackageLogic;

namespace AasxWpfControlLibrary.WpfControls
{
    public class MiniMarkupRichTextBox : RichTextBox
    {
        public delegate void MiniMarkupLinkClickDelegate(MiniMarkupBase markup, string link);

        public event MiniMarkupLinkClickDelegate MiniMarkupLinkClick;

        public void SetMarkup(string md)
        {
            this.Document.Blocks.Clear();
            this.Document.Blocks.Add(new Paragraph(new Run("" + md)));
        }

        public void SetXaml(string xamlString)
        {
            var flowDocHeader = @"<Section 
              xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">";

            var flowDocFooter = @"</Section>";

            var doc = flowDocHeader + Environment.NewLine
                + xamlString + Environment.NewLine
                + flowDocFooter + Environment.NewLine;

            try
            {
                var stringReader = new StringReader(doc);
                var xmlReader = XmlReader.Create(stringReader);
                var blocks = (Section)XamlReader.Load(xmlReader);

                this.Document.Blocks.Clear();
                this.Document.Blocks.Add(blocks);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Rendering Markup for event message details");
            }
        }

        private object Render(MiniMarkupBase markup)
        {
            this.IsDocumentEnabled = true;
            this.IsEnabled = true;
            this.IsReadOnly = true;

            if (markup is MiniMarkupLine line)
            {
                var p = new Paragraph();

                p.Margin = new Thickness(0);
                if (line.Children != null)
                    foreach (var ch in line.Children)
                    {
                        var x = Render(ch);
                        if (x is Run r)
                            p.Inlines.Add(r);
                        if (x is Hyperlink hl)
                            p.Inlines.Add(hl);
                    }
                return p;
            }
            else
            if (markup is MiniMarkupLink mml)
            {
                var link = new Hyperlink();
                link.IsEnabled = true;

                try
                {
                    link.Inlines.Add("" + mml.Text);
                    link.NavigateUri = new Uri("" + mml.LinkUri);
                    link.RequestNavigate += (s, a) =>
                    {
                        MiniMarkupLinkClick?.Invoke(markup, mml.LinkUri);
                    };
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

                return link;
            }
            else
            if (markup is MiniMarkupRun run)
            {
                var txt = run.Text;
                if (run.Padsize.HasValue)
                    txt = txt.PadRight(run.Padsize.Value);

                var r = new Run(txt);
                if (run.FontSize.HasValue)
                    r.FontSize = run.FontSize.Value;
                if (run.IsBold)
                    r.FontWeight = FontWeights.Bold;
                if (run.IsMonospaced)
                    r.FontFamily = new FontFamily("Courier New");

                return r;
            }
            else
            if (markup is MiniMarkupSequence seq)
            {
                var s = new Section();

                if (seq.Children != null)
                    foreach (var ch in seq.Children)
                    {
                        var x = Render(ch);
                        if (x is Block r)
                            s.Blocks.Add(r);
                    }
                return s;
            }

            return null;
        }

        public void SetMarkup(MiniMarkupBase markup)
        {
            this.Document.Blocks.Clear();
            var x = Render(markup);
            if (x is Section sect)
                this.Document.Blocks.Add(sect);
        }
    }
}
