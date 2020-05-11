using WFrameworkElement =  System.Windows.FrameworkElement;
using WSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WMatrixTransform = System.Windows.Media.MatrixTransform;
using WMatrix = System.Windows.Media.Matrix;
using WColor = System.Windows.Media.Color;
using WBrush = System.Windows.Media.Brush;
using WPoint = System.Windows.Point;
using MPoint = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.WpfGraphControl {
    internal class Common {
        internal static WPoint WpfPoint(MPoint p) {
            return new WPoint(p.X, p.Y);
        }

        internal static MPoint MsaglPoint(WPoint p) {
            return new MPoint(p.X, p.Y);
        }


        public static WBrush BrushFromMsaglColor(Microsoft.Msagl.Drawing.Color color) {
            WColor avalonColor = new WColor {A = color.A, B = color.B, G = color.G, R = color.R};
            return new WSolidColorBrush(avalonColor);
        }

        public static WBrush BrushFromMsaglColor(byte colorA, byte colorR, byte colorG, byte colorB) {
            WColor avalonColor = new WColor {A = colorA, R = colorR, G = colorG, B = colorB};
            return new WSolidColorBrush(avalonColor);
        }
        internal static void PositionFrameworkElement(WFrameworkElement frameworkElement, MPoint center, double scale) {
            PositionFrameworkElement(frameworkElement, center.X, center.Y, scale);
        }

        static void PositionFrameworkElement(WFrameworkElement frameworkElement, double x, double y, double scale) {
            if (frameworkElement == null)
                return;
            frameworkElement.RenderTransform =
                new WMatrixTransform(new WMatrix(scale, 0, 0, -scale, x - scale*frameworkElement.Width/2,
                    y + scale*frameworkElement.Height/2));
        }
    }
}