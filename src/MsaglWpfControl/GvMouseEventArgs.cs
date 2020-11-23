/*
Microsoft Automatic Graph Layout,MSAGL

Copyright (c) Microsoft Corporation

All rights reserved.

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Msagl.Drawing;
using WIInputElement = System.Windows.IInputElement;
using WMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WMouseButtonState = System.Windows.Input.MouseButtonState;
using WMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WPoint = System.Windows.Point;

namespace Microsoft.Msagl.WpfGraphControl
{
    internal class GvMouseEventArgs : MsaglMouseEventArgs
    {
        WMouseEventArgs args;
        WPoint position;

        internal GvMouseEventArgs(WMouseEventArgs argsPar, GraphViewer graphScrollerP)
        {
            args = argsPar;
            position = args.GetPosition((WIInputElement)graphScrollerP.GraphCanvas.Parent);
        }

        public override bool LeftButtonIsPressed
        {
            get { return args.LeftButton == WMouseButtonState.Pressed; }
        }


        public override bool MiddleButtonIsPressed
        {
            get { return args.MiddleButton == WMouseButtonState.Pressed; }
        }

        public override bool RightButtonIsPressed
        {
            get { return args.RightButton == WMouseButtonState.Pressed; }
        }


        public override bool Handled
        {
            get { return args.Handled; }
            set { args.Handled = value; }
        }

        public override int X
        {
            get { return (int)position.X; }
        }

        public override int Y
        {
            get { return (int)position.Y; }
        }

        /// <summary>
        ///     number of clicks
        /// </summary>
        public override int Clicks
        {
            get
            {
                var e = args as WMouseButtonEventArgs;
                return e != null ? e.ClickCount : 0;
            }
        }
    }
}
