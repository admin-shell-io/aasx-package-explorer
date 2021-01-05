using AasxPackageExplorer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;

namespace AasxWpfControlLibrary.WpfControls
{
    public class MiniMarkupRichTextBox : RichTextBox
    {
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
            } catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Rendering Markup for event message details");
            }
        }
    }
}
