/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMtpControl
{

    public class CanvasClickObjectBase
    {
        public int Zlevel = 0;
        public object Tag;

        public virtual double Area { get { return 0.0; } }
        public virtual bool CheckClick(double x, double y) { return false; }
    }

    // ReSharper disable once UnusedType.Global
    public class CanvasClickObjectBox : CanvasClickObjectBase
    {
        public double X0, Y0, Width, Height;
        public override double Area { get { return X0 * Y0; } }
        public override bool CheckClick(double x, double y)
        {
            return (x >= X0 && x <= X0 + Width && y >= Y0 && y <= Y0 + Height);
        }

        public CanvasClickObjectBox() { }
        public CanvasClickObjectBox(double X0, double Y0, double Width, double Height, int Zlevel, object Tag)
        {
            this.X0 = X0;
            this.Y0 = Y0;
            this.Width = Width;
            this.Height = Height;
            this.Zlevel = Zlevel;
            this.Tag = Tag;
        }
    }

    // ReSharper disable once UnusedType.Global
    public class CanvasClickObjectList : List<CanvasClickObjectBase>
    {
        private static int CompareCanvasClickObject(CanvasClickObjectBase x, CanvasClickObjectBase y)
        {
            if (x == null)
                return +1;
            if (y == null)
                return -1;
            if (x.Zlevel < y.Zlevel)
                return +1;
            if (x.Zlevel > y.Zlevel)
                return -1;
            if (x.Area < y.Area)
                return -1;
            if (x.Area > y.Area)
                return +1;
            return 0;
        }

        public void FinalizeForUse()
        {
            this.Sort(CompareCanvasClickObject);
        }

        public CanvasClickObjectBase FindClick(double x, double y)
        {
            foreach (var co in this)
                if (co.CheckClick(x, y))
                    return co;
            return null;
        }
    }
}
