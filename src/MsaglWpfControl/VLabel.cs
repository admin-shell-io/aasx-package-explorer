using System;
using System.Collections.Generic;
using Microsoft.Msagl.Drawing;
using WBrushes = System.Windows.Media.Brushes;
using WCanvas = System.Windows.Controls.Canvas;
using WDoubleCollection = System.Windows.Media.DoubleCollection;
using WFrameworkElement = System.Windows.FrameworkElement;
using WLine = System.Windows.Shapes.Line;

namespace Microsoft.Msagl.WpfGraphControl
{
    internal class VLabel : IViewerObject, IInvalidatable
    {
        internal readonly WFrameworkElement FrameworkElement;
        bool markedForDragging;

        public VLabel(Edge edge, WFrameworkElement frameworkElement)
        {
            FrameworkElement = frameworkElement;
            DrawingObject = edge.Label;
        }

        public DrawingObject DrawingObject { get; private set; }

        public bool MarkedForDragging
        {
            get { return markedForDragging; }
            set
            {
                markedForDragging = value;
                if (value)
                {
                    AttachmentLine = new WLine
                    {
                        Stroke = WBrushes.Black,
                        StrokeDashArray = new WDoubleCollection(OffsetElems())
                    }; //the line will have 0,0, 0,0 start and end so it would not be rendered

                    ((WCanvas)FrameworkElement.Parent).Children.Add(AttachmentLine);
                }
                else
                {
                    ((WCanvas)FrameworkElement.Parent).Children.Remove(AttachmentLine);
                    AttachmentLine = null;
                }
            }
        }



        IEnumerable<double> OffsetElems()
        {
            yield return 1;
            yield return 2;
        }

        WLine AttachmentLine { get; set; }

#pragma warning disable 67
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
#pragma warning restore 67

        public void Invalidate()
        {
            var label = (Drawing.Label)DrawingObject;
            Common.PositionFrameworkElement(FrameworkElement, label.Center, 1);
            var geomLabel = label.GeometryLabel;
            if (AttachmentLine != null)
            {
                AttachmentLine.X1 = geomLabel.AttachmentSegmentStart.X;
                AttachmentLine.Y1 = geomLabel.AttachmentSegmentStart.Y;

                AttachmentLine.X2 = geomLabel.AttachmentSegmentEnd.X;
                AttachmentLine.Y2 = geomLabel.AttachmentSegmentEnd.Y;
            }
        }
    }
}
