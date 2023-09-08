/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AasxIntegrationBase;
using Aml.Engine.CAEX;
using WpfMtpControl;

namespace Mtp.DynamicInstances
{
    // see: https://stackoverflow.com/questions/25522218/wpf-binding-not-updating-the-view
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raises the PropertyChange event for the property specified
        /// </summary>
        /// <param name="propertyName">Property name to update. Is case-sensitive.</param>
        public virtual void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion // INotifyPropertyChanged Members
    }

    public class MtpDynamicInstanceBase : ObservableObject
    {
        //
        // Important Members, Methods
        //

        /// <summary>
        /// An overloaded object might request drawing the MTP symbol on top of the dynamic instance.
        /// </summary>
        public bool DrawSymbolAsWell;

        /// <summary>
        /// If MTP symbol is drawn, hold the Canvas object of it
        /// </summary>
        public FrameworkElement SymbolElement;

        /// <summary>
        /// Called, if the state of the object has been changed or firstly initialized
        /// </summary>
        public virtual void RedrawSymbol(MtpVisuOptions visuOptions = null)
        {
        }

        /// <summary>
        /// Internal flag for redrawing
        /// </summary>
        private bool doRedrawOnTick = false;

        /// <summary>
        /// Flag for this instance to demand a redraw next time b ythe UI thread.
        /// </summary>
        /// <returns></returns>
        public void DemandRedrawOnTick()
        {
            this.doRedrawOnTick = true;
        }

        /// <summary>
        /// Will be called by the UI thread each 100ms
        /// </summary>
        public virtual void Tick(MtpVisuOptions visuOptions = null)
        {
            if (this.doRedrawOnTick)
            {
                this.doRedrawOnTick = false;
                this.RedrawSymbol(visuOptions);
            }
        }

        public virtual void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
        }

        public virtual UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            return null;
        }

        //
        // Symbol manipulation
        //

        public void SymbolSetStateColor(Brush stateColor)
        {
            // access
            if (this.SymbolElement == null)
                return;

            // fill
            foreach (var c in AasxWpfBaseUtils.LogicalTreeFindAllChildsWithRegexTag<System.Windows.Shapes.Shape>(
                this.SymbolElement, "StateToFill"))
                if (c != null)
                {
                    c.Fill = stateColor;
                }

            // stroke
            foreach (var c in AasxWpfBaseUtils.LogicalTreeFindAllChildsWithRegexTag<System.Windows.Shapes.Shape>(
                this.SymbolElement, "StateToStroke"))
                if (c != null)
                {
                    c.Stroke = stateColor;
                }
        }

        //
        // Demo stuff
        //
        private int DemoIndex = 1;

        public virtual bool Demo(int mode)
        {
            return false;
        }

        public void IncDemo()
        {
            if (!this.Demo(DemoIndex))
            {
                this.Demo(1);
                DemoIndex = 2;
            }
            else
                DemoIndex++;
        }

        protected void ButtonGo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IncDemo();
        }

        //
        // Units
        //

        public class UnitRec
        {
            public int Id;
            public string ShortText;
            public string LongText;

            public UnitRec() { }
            public UnitRec(int Id, string ShortText, string LongText)
            {
                this.Id = Id;
                this.ShortText = ShortText;
                this.LongText = LongText;
            }
        }

        private static UnitRec[] unitRecords = {
            new UnitRec(1000, "K", "Kelvin"),
            new UnitRec(1001, "°C", "Grad Celsius"),
            new UnitRec(1002, "°F", "Grad Fahrenheit"),
            new UnitRec(1005, "°", "Grad"),
            new UnitRec(1006, "'", "Minute"),
            new UnitRec(1007, "''", "Sekunde"),
            new UnitRec(1010, "m", "Meter"),
            new UnitRec(1013, "mm", "Millimeter"),
            new UnitRec(1018, "ft", "Fuß"),
            new UnitRec(1023, "m 2", " Quadratmeter"),
            new UnitRec(1038, "l", "Liter"),
            new UnitRec(1041, "hl", "Hektoliter"),
            new UnitRec(1054, "s", "Sekunde"),
            new UnitRec(1058, "min", "Minute"),
            new UnitRec(1059, "h", "Stunde"),
            new UnitRec(1060, "d", "Tag"),
            new UnitRec(1061, "m/s", "Meter pro Sekunde"),
            new UnitRec(1077, "Hz", "Hertz"),
            new UnitRec(1081, "kHz", "Kilohertz"),
            new UnitRec(1082, "1/s", "pro Sekunde"),
            new UnitRec(1083, "1/min", "pro Minute"),
            new UnitRec(1088, "kg", "Kilogramm"),
            new UnitRec(1092, "t", "metrische Tonne"),
            new UnitRec(1100, "g/cm 3", " Gramm pro Kubikzentimeter"),
            new UnitRec(1105, "g/l", "Gramm pro Liter"),
            new UnitRec(1120, "N", "Newton"),
            new UnitRec(1123, "mN", "Millinewton"),
            new UnitRec(1130, "Pa", "Pascal"),
            new UnitRec(1133, "kPa", "Kilopascal"),
            new UnitRec(1137, "bar", "Bar"),
            new UnitRec(1138, "mbar", "Millibar"),
            new UnitRec(1149, "mmH 2 O", "Millimeter Wassersäule"),
            new UnitRec(1175, "Wh", "Wattstunde"),
            new UnitRec(1179, "kWh", "Kilowattstunde"),
            new UnitRec(1181, "kcalth", "Kilokalorien"),
            new UnitRec(1190, "kW", "Kilowatt"),
            new UnitRec(1209, "A", "Ampere"),
            new UnitRec(1211, "mA", "Milliampere"),
            new UnitRec(1221, "Ah", "Amperestunde"),
            new UnitRec(1240, "V", "Volt"),
            new UnitRec(1342, "%", "Prozent"),
            new UnitRec(1349, "m3/h", "Kubikmeter pro Stunde"),
            new UnitRec(1353, "l/h", "Liter pro Stunde"),
            new UnitRec(1384, "mol", "Mol"),
            new UnitRec(1422, "pH", "pH-Wert")
        };

        private static Dictionary<int, UnitRec> unitIdToText = new Dictionary<int, UnitRec>();

        static MtpDynamicInstanceBase()
        {
            // initialize the patch tables
            foreach (var ur in unitRecords)
                unitIdToText.Add(ur.Id, ur);
        }

        public UnitRec FindUnitById(int id)
        {
            if (unitIdToText.ContainsKey(id))
                return unitIdToText[id];
            return null;
        }

        public string FindUnitTextById(int id)
        {
            var ur = FindUnitById(id);
            if (ur == null)
                return "";
            return ur.ShortText;
        }
    }

    public class MtpDiDataAssembly : MtpDynamicInstanceBase
    {
        // Remark: important to articulate all the properties with Get/Set for having Data Binding in place!!
        // TagName
        private string tagName = "";
        public string TagName
        {
            get { return tagName; }
            set { tagName = value; RaisePropertyChanged("TagName"); }
        }

        // TagDescription
        private string tagDescription = "";
        public string TagDescription
        {
            get { return tagDescription; }
            set { tagDescription = value; RaisePropertyChanged("TagDescription"); }
        }
    }

    public class MtpDiIndicatorElement : MtpDiDataAssembly
    {
        // WorstQualityCode
        private byte wqc = 0xff;
        public byte WorstQualityCode
        {
            get { return wqc; }
            set
            {
                wqc = value; RaisePropertyChanged("WorstQualityCode");
                RaisePropertyChanged("WorstQualityCodeText"); RaisePropertyChanged("WorstQualityCodeBrush");
            }
        }
        public string WorstQualityCodeText
        {
            get { return String.Format("#{0:X02}", wqc); }
        }
        public Brush WorstQualityCodeBrush
        {
            get
            {
                if (wqc == 96) return Brushes.DarkBlue; // Simulation
                if (wqc == 128) return Brushes.Green; // Good
                if (wqc == 164) return Brushes.Yellow; // Maintenance

                if (wqc <= 40) return Brushes.Red; // Bad
                if (wqc < 128) return Brushes.DarkOrange; // Uncertain
                if (wqc < 255) return Brushes.Green; // also good?
                return Brushes.Transparent;
            }
        }
    }

    public class MtpDiAnaView : MtpDiIndicatorElement
    {
        // Value
        private double valuex = 0.0;
        public virtual double Value
        {
            // ReSharper disable once UnusedMemberHierarchy.Global
            get { return valuex; }
            set
            {
                valuex = value; RaisePropertyChanged("Value"); RaisePropertyChanged("ValuePercent");
                RaisePropertyChanged("ValueText");
            }
        }
        public double ValuePercent
        {
            get
            {
                return (valuex - valueScaleLowLimit)
                  / Math.Max(0.001, ValueScaleHighLimit - valueScaleLowLimit) * 100.0;
            }
        }
        public string ValueText
        {
            get
            {
                var st = Value.ToString(CultureInfo.InvariantCulture);
                // check the number of digits / decimal places
                var dpi = st.IndexOf('.');
                if (st.Length >= 8 && st.Length - dpi > 4)
                {
                    st = st.Substring(0, dpi + 4);
                }
                return st;
            }
        }

        // ValueScaleLowLimit
        private double valueScaleLowLimit = 0.0;
        public double ValueScaleLowLimit
        {
            get { return valueScaleLowLimit; }
            set
            {
                valueScaleLowLimit = value; RaisePropertyChanged("ValueScaleLowLimit");
                RaisePropertyChanged("ValuePercent");
            }
        }

        // ValueScaleHighLimit
        private double valueScaleHighLimit = 0.0;
        public double ValueScaleHighLimit
        {
            get { return valueScaleHighLimit; }
            set
            {
                valueScaleHighLimit = value; RaisePropertyChanged("ValueScaleHighLimit");
                RaisePropertyChanged("ValuePercent");
            }
        }

        // ValueUnit
        private int valueUnit = 0;
        public int ValueUnit
        {
            get { return valueUnit; }
            set { valueUnit = value; RaisePropertyChanged("ValueUnit"); RaisePropertyChanged("ValueUnitText"); }
        }
        public string ValueUnitText { get { return this.FindUnitTextById(valueUnit); } }

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            if (ie == null || subscriber == null)
                return;
            this.TagName = "" + Name;

            subscriber.SubscribeToAmlIdRefWith<double>(ie.Attribute, "V",
                (ct, o) => { this.Value = (double)o; });
            subscriber.SubscribeToAmlIdRefWith<byte>(ie.Attribute, "WQC",
                (ct, o) => { this.WorstQualityCode = (byte)o; });
            subscriber.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VSclMin",
                (ct, o) => { this.ValueScaleLowLimit = (double)o; });
            subscriber.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VSclMax",
                (ct, o) => { this.ValueScaleHighLimit = (double)o; });
            subscriber.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VUnit",
                (ct, o) => { this.ValueUnit = (int)o; });
        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            UserControl c = null;
            if (mtpWidth <= 50 || mtpHeight <= 40)
            {
                var bvt = new MtpViewAnaViewTiny();
                c = bvt;
                c.Width = 80;
                c.Height = 30;
                c.DataContext = this;
                bvt.ButtonGo.Click += ButtonGo_Click;
            }
            else
            {
                var bvl = new MtpViewAnaViewLarge();
                c = bvl;
                c.Width = 130;
                c.Height = 96;
                c.DataContext = this;
                bvl.ButtonGo.Click += ButtonGo_Click;
            }
            Demo(2);
            return c;
        }

        public override bool Demo(int mode)
        {
            if (mode == 1)
            {
                TagDescription = "This is a very long description of everything and even more";
                WorstQualityCode = 0xff;
                Value = 345.678;
                ValueUnit = 1001;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            if (mode == 2)
            {
                TagDescription = "Another description";
                WorstQualityCode = 0xa5;
                Value = 23.45;
                ValueUnit = 1001;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            if (mode == 3)
            {
                TagDescription = "Another description";
                WorstQualityCode = 0xa5;
                Value = 390;
                ValueUnit = 1001;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            if (mode == 4)
            {
                TagDescription = "Former description";
                WorstQualityCode = 0x5a;
                Value = 720.1111;
                ValueUnit = 1002;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            return false;
        }
    }

    public class MtpDiAnaMon : MtpDiAnaView
    {
        // Members
        // basically 3 Limit Bands Tolerance, Warning, Alarm each with individual limit values and enables

        public class LimitBand
        {
            public double LimitValueLow, LimitValueHigh;
            public bool LimitEnableLow, LimitEnableHigh;
            public bool LimitActiveLow, LimitActiveHigh;

            public LimitBand() { }

            public LimitBand(double LimitValueLow, double LimitValueHigh,
                bool LimitEnableLow, bool LimitEnableHigh,
                bool LimitActiveLow, bool LimitActiveHigh)
            {
                this.LimitValueLow = LimitValueLow;
                this.LimitValueHigh = LimitValueHigh;
                this.LimitEnableLow = LimitEnableLow;
                this.LimitEnableHigh = LimitEnableHigh;
                this.LimitActiveLow = LimitActiveLow;
                this.LimitActiveHigh = LimitActiveHigh;
            }

            public bool EvalValueForLimitViolation(double value)
            {
                this.LimitActiveLow = this.LimitEnableLow && value < this.LimitValueLow;
                this.LimitActiveHigh = this.LimitEnableHigh && value > this.LimitValueHigh;
                return this.LimitActiveLow || this.LimitActiveHigh;
            }
        }

        public const int BandNone = -1;
        public const int BandTolerance = 0;
        public const int BandWarning = 1;
        public const int BandAlarm = 2;

        public LimitBand[] Band = new LimitBand[] { new LimitBand(), new LimitBand(), new LimitBand() };

        // concept of violatedBand

        private int violatedBand = BandNone;
        public int ViolatedBand { get { return violatedBand; } }

        private string[] violatedBandText = new string[] { "TOL", "WARN", "ALRM" };
        public string ViolatedBandText
        {
            get
            {
                if (violatedBand < BandTolerance || violatedBand > BandAlarm)
                    return "none";
                return violatedBandText[violatedBand];
            }
        }

        private Brush[] violatedBandBrush = new Brush[] { Brushes.DarkOrange, Brushes.OrangeRed, Brushes.Red };
        public Brush ViolatedBandBrush
        {
            get
            {
                if (violatedBand < BandTolerance || violatedBand > BandAlarm)
                    return Brushes.Transparent;
                return violatedBandBrush[violatedBand];
            }
        }

        // now: OVERIDE the getter/setter

        public override double Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                base.Value = value;

                violatedBand = BandNone;
                if (this.Band[BandTolerance].EvalValueForLimitViolation(value))
                    violatedBand = BandTolerance;
                if (this.Band[BandWarning].EvalValueForLimitViolation(value))
                    violatedBand = BandWarning;
                if (this.Band[BandAlarm].EvalValueForLimitViolation(value))
                    violatedBand = BandAlarm;

                RaisePropertyChanged("ViolatedBand");
                RaisePropertyChanged("ViolatedBandText");
                RaisePropertyChanged("ViolatedBandBrush");
            }
        }

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            // call AnaView
            base.PopulateFromAml(Name, ie, subscriber);

            // some more
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VAHEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitEnableHigh = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VAHLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitValueHigh = (double)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VAHAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitActiveHigh = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWHEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitEnableHigh = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VWHLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitValueHigh = (double)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWHAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitActiveHigh = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTHEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitEnableHigh = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VTHLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitValueHigh = (double)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTHAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitActiveHigh = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VALEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitEnableLow = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VALLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitValueLow = (double)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VALAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitActiveLow = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWLEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitEnableLow = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VWLLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitValueLow = (double)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWLAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitActiveLow = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTLEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitEnableLow = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VTLLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitValueLow = (double)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTLAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitActiveLow = (bool)o; });

        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            UserControl c = null;
            if (mtpWidth <= 50 || mtpHeight <= 40)
            {
                var bvt = new MtpViewAnaMonTiny();
                c = bvt;
                c.Width = 80;
                c.Height = 30;
                c.DataContext = this;
                bvt.ButtonGo.Click += ButtonGo_Click;
            }
            else
            {
                var bvl = new MtpViewAnaViewLarge();
                c = bvl;
                c.Width = 130;
                c.Height = 96;
                c.DataContext = this;
                bvl.ButtonGo.Click += ButtonGo_Click;
            }
            Demo(2);
            return c;
        }

        public override bool Demo(int mode)
        {
            // most functionality is already in base class
            base.Demo(mode);

            // often have the same limits
            this.Band[BandTolerance] = new LimitBand(320, 380, true, true, true, true);
            this.Band[BandWarning] = new LimitBand(200, 600, true, true, true, true);
            this.Band[BandAlarm] = new LimitBand(100, 800, true, true, true, true);

            // some more tweaks?
            if (mode == 1)
            {
                return true;
            }
            if (mode == 2)
            {
                return true;
            }
            if (mode == 3)
            {
                return true;
            }
            if (mode == 4)
            {
                return true;
            }
            return false;
        }
    }










    public class MtpDiDIntView : MtpDiIndicatorElement
    {
        // Value
        private int valuex = 0;
        public virtual int Value
        {
            // ReSharper disable once UnusedMemberHierarchy.Global
            get { return valuex; }
            set
            {
                valuex = value; RaisePropertyChanged("Value"); RaisePropertyChanged("ValuePercent");
                RaisePropertyChanged("ValueText");
            }
        }
        public double ValuePercent
        {
            get
            {
                return (valuex - valueScaleLowLimit)
                  / Math.Max(0.001, ValueScaleHighLimit - valueScaleLowLimit) * 100.0;
            }
        }
        public string ValueText
        {
            get
            {
                var st = Value.ToString(CultureInfo.InvariantCulture);
                return st;
            }
        }

        // ValueScaleLowLimit
        private int valueScaleLowLimit = 0;
        public int ValueScaleLowLimit
        {
            get { return valueScaleLowLimit; }
            set
            {
                valueScaleLowLimit = value; RaisePropertyChanged("ValueScaleLowLimit");
                RaisePropertyChanged("ValuePercent");
            }
        }

        // ValueScaleHighLimit
        private int valueScaleHighLimit = 0;
        public int ValueScaleHighLimit
        {
            get { return valueScaleHighLimit; }
            set
            {
                valueScaleHighLimit = value; RaisePropertyChanged("ValueScaleHighLimit");
                RaisePropertyChanged("ValuePercent");
            }
        }

        // ValueUnit
        private int valueUnit = 0;
        public int ValueUnit
        {
            get { return valueUnit; }
            set { valueUnit = value; RaisePropertyChanged("ValueUnit"); RaisePropertyChanged("ValueUnitText"); }
        }
        public string ValueUnitText { get { return this.FindUnitTextById(valueUnit); } }

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            if (ie == null || subscriber == null)
                return;
            this.TagName = "" + Name;

            subscriber.SubscribeToAmlIdRefWith<int>(ie.Attribute, "V",
                (ct, o) => { this.Value = (int)o; });
            subscriber.SubscribeToAmlIdRefWith<byte>(ie.Attribute, "WQC",
                (ct, o) => { this.WorstQualityCode = (byte)o; });
            subscriber.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VSclMin",
                (ct, o) => { this.ValueScaleLowLimit = (int)o; });
            subscriber.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VSclMax",
                (ct, o) => { this.ValueScaleHighLimit = (int)o; });
            subscriber.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VUnit",
                (ct, o) => { this.ValueUnit = (int)o; });
        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            UserControl c = null;
            if (mtpWidth <= 50 || mtpHeight <= 40)
            {
                var bvt = new MtpViewAnaViewTiny();
                c = bvt;
                c.Width = 80;
                c.Height = 30;
                c.DataContext = this;
                bvt.ButtonGo.Click += ButtonGo_Click;
            }
            else
            {
                var bvl = new MtpViewAnaViewLarge();
                c = bvl;
                c.Width = 130;
                c.Height = 96;
                c.DataContext = this;
                bvl.ButtonGo.Click += ButtonGo_Click;
            }
            Demo(2);
            return c;
        }

        public override bool Demo(int mode)
        {
            if (mode == 1)
            {
                TagDescription = "This is a very long description of everything and even more";
                WorstQualityCode = 0xff;
                Value = 345;
                ValueUnit = 1001;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            if (mode == 2)
            {
                TagDescription = "Another description";
                WorstQualityCode = 0xa5;
                Value = 23;
                ValueUnit = 1001;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            if (mode == 3)
            {
                TagDescription = "Another description";
                WorstQualityCode = 0xa5;
                Value = 390;
                ValueUnit = 1001;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            if (mode == 4)
            {
                TagDescription = "Former description";
                WorstQualityCode = 0x5a;
                Value = 720;
                ValueUnit = 1002;
                ValueScaleLowLimit = -23;
                ValueScaleHighLimit = 960;
                return true;
            }
            return false;
        }
    }

    public class MtpDiDIntMon : MtpDiDIntView
    {
        // Members
        // basically 3 Limit Bands Tolerance, Warning, Alarm each with individual limit values and enables

        public class LimitBand
        {
            public int LimitValueLow, LimitValueHigh;
            public bool LimitEnableLow, LimitEnableHigh;
            public bool LimitActiveLow, LimitActiveHigh;

            public LimitBand() { }

            public LimitBand(int LimitValueLow, int LimitValueHigh,
                bool LimitEnableLow, bool LimitEnableHigh,
                bool LimitActiveLow, bool LimitActiveHigh)
            {
                this.LimitValueLow = LimitValueLow;
                this.LimitValueHigh = LimitValueHigh;
                this.LimitEnableLow = LimitEnableLow;
                this.LimitEnableHigh = LimitEnableHigh;
                this.LimitActiveLow = LimitActiveLow;
                this.LimitActiveHigh = LimitActiveHigh;
            }

            public bool EvalValueForLimitViolation(int value)
            {
                this.LimitActiveLow = this.LimitEnableLow && value < this.LimitValueLow;
                this.LimitActiveHigh = this.LimitEnableHigh && value > this.LimitValueHigh;
                return this.LimitActiveLow || this.LimitActiveHigh;
            }
        }

        public const int BandNone = -1;
        public const int BandTolerance = 0;
        public const int BandWarning = 1;
        public const int BandAlarm = 2;

        public LimitBand[] Band = new LimitBand[] { new LimitBand(), new LimitBand(), new LimitBand() };

        // concept of violatedBand

        private int violatedBand = BandNone;
        public int ViolatedBand { get { return violatedBand; } }

        private string[] violatedBandText = new string[] { "TOL", "WARN", "ALRM" };
        public string ViolatedBandText
        {
            get
            {
                if (violatedBand < BandTolerance || violatedBand > BandAlarm)
                    return "none";
                return violatedBandText[violatedBand];
            }
        }

        private Brush[] violatedBandBrush = new Brush[] { Brushes.DarkOrange, Brushes.OrangeRed, Brushes.Red };
        public Brush ViolatedBandBrush
        {
            get
            {
                if (violatedBand < BandTolerance || violatedBand > BandAlarm)
                    return Brushes.Transparent;
                return violatedBandBrush[violatedBand];
            }
        }

        // now: OVERIDE the getter/setter

        public override int Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                base.Value = value;

                violatedBand = BandNone;
                if (this.Band[BandTolerance].EvalValueForLimitViolation(value))
                    violatedBand = BandTolerance;
                if (this.Band[BandWarning].EvalValueForLimitViolation(value))
                    violatedBand = BandWarning;
                if (this.Band[BandAlarm].EvalValueForLimitViolation(value))
                    violatedBand = BandAlarm;

                RaisePropertyChanged("ViolatedBand");
                RaisePropertyChanged("ViolatedBandText");
                RaisePropertyChanged("ViolatedBandBrush");
            }
        }

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            // call AnaView
            base.PopulateFromAml(Name, ie, subscriber);

            // some more
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VAHEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitEnableHigh = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VAHLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitValueHigh = (int)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VAHAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitActiveHigh = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWHEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitEnableHigh = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VWHLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitValueHigh = (int)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWHAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitActiveHigh = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTHEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitEnableHigh = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VTHLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitValueHigh = (int)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTHAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitActiveHigh = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VALEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitEnableLow = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VALLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitValueLow = (int)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VALAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandAlarm].LimitActiveLow = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWLEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitEnableLow = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VWLLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitValueLow = (int)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VWLAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandWarning].LimitActiveLow = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTLEn",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitEnableLow = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VTLLim",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitValueLow = (int)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VTLAct",
                (ct, o) => { if (this.Band?[BandAlarm] != null) this.Band[BandTolerance].LimitActiveLow = (bool)o; });

        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            UserControl c = null;
            if (mtpWidth <= 50 || mtpHeight <= 40)
            {
                var bvt = new MtpViewAnaMonTiny();
                c = bvt;
                c.Width = 80;
                c.Height = 30;
                c.DataContext = this;
                bvt.ButtonGo.Click += ButtonGo_Click;
            }
            else
            {
                var bvl = new MtpViewAnaViewLarge();
                c = bvl;
                c.Width = 130;
                c.Height = 96;
                c.DataContext = this;
                bvl.ButtonGo.Click += ButtonGo_Click;
            }
            Demo(2);
            return c;
        }

        public override bool Demo(int mode)
        {
            // most functionality is already in base class
            base.Demo(mode);

            // often have the same limits
            this.Band[BandTolerance] = new LimitBand(320, 380, true, true, true, true);
            this.Band[BandWarning] = new LimitBand(200, 600, true, true, true, true);
            this.Band[BandAlarm] = new LimitBand(100, 800, true, true, true, true);

            // some more tweaks?
            if (mode == 1)
            {
                return true;
            }
            if (mode == 2)
            {
                return true;
            }
            if (mode == 3)
            {
                return true;
            }
            if (mode == 4)
            {
                return true;
            }
            return false;
        }
    }

    public class MtpDiBinView : MtpDiIndicatorElement
    {
        // Value
        private bool valState = false;
        public bool Value
        {
            get { return valState; }
            set
            {
                valState = value;
                RaisePropertyChanged("Value");
                RaisePropertyChanged("ValueTinyText");
                RaisePropertyChanged("ValueText");
                RaisePropertyChanged("MoreText");
                RaisePropertyChanged("ValueFalseText");
                RaisePropertyChanged("ValueTrueText");
                RaisePropertyChanged("ValueFalseBrush");
                RaisePropertyChanged("ValueTrueBrush");
                RaisePropertyChanged("ValueBrush");
            }
        }

        public string ValueTinyText
        {
            get
            {
                if (!valState)
                {
                    return "FALSE";
                }
                else
                {
                    return "TRUE";
                }
            }
        }

        public string ValueText
        {
            get
            {
                if (!valState)
                {
                    if (valState0 != null && valState0.Trim().Length > 0)
                        return valState0;
                    else
                        return "FALSE";
                }
                else
                {
                    if (valState1 != null && valState1.Trim().Length > 0)
                        return valState1;
                    else
                        return "TRUE";
                }
            }
        }

        public string MoreText
        {
            get
            {
                if (!valState)
                {
                    if (valState0 != null && valState0.Trim().Length > 0)
                        return valState0;
                    else
                        return "";
                }
                else
                {
                    if (valState1 != null && valState1.Trim().Length > 0)
                        return valState1;
                    else
                        return "";
                }
            }
        }

        public string ValueFalseText
        {
            get { if (!valState) { return "FALSE"; } else { return ""; } }
        }
        public string ValueTrueText
        {
            get { if (!valState) { return ""; } else { return "TRUE"; } }
        }

        public Brush ValueFalseBrush
        {
            get { if (!valState) { return Brushes.Red; } else { return Brushes.Transparent; } }
        }
        public Brush ValueTrueBrush
        {
            get { if (!valState) { return Brushes.Transparent; } else { return Brushes.Green; } }
        }
        public Brush ValueBrush
        {
            get { if (valState) { return Brushes.Green; } else { return Brushes.Red; } }
        }

        // String replacements
        private string valState0 = "";
        public string ValState0
        {
            get { return valState0; }
            set { valState0 = value; RaisePropertyChanged("ValState0"); }
        }

        private string valState1 = "";
        public string ValState1
        {
            get { return valState1; }
            set { valState1 = value; RaisePropertyChanged("ValState1"); }
        }

        // Construct

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            this.TagName = "" + Name;

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "V",
                (ct, o) => { this.Value = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<byte>(ie.Attribute, "WQC",
                (ct, o) => { this.WorstQualityCode = (byte)o; });

            subscriber?.SubscribeToAmlIdRefWith<string>(ie.Attribute, "VState0",
                (ct, o) => { this.ValState0 = (string)o; });
            subscriber?.SubscribeToAmlIdRefWith<string>(ie.Attribute, "VState1",
                (ct, o) => { this.ValState1 = (string)o; });
        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            UserControl c = null;
            if (mtpWidth <= 50 || mtpHeight <= 40)
            {
                var bvt = new MtpViewBinViewTiny();
                c = bvt;
                c.Width = 40;
                c.Height = 30;
                c.DataContext = this;
                bvt.ButtonGo.Click += ButtonGo_Click;
            }
            else
            {
                var bvl = new MtpViewBinViewLarge();
                c = bvl;
                c.Width = 130;
                c.Height = 96;
                c.DataContext = this;
                bvl.ButtonGo.Click += ButtonGo_Click;
            }
            Demo(2);
            return c;
        }

        public override bool Demo(int mode)
        {
            if (mode == 1)
            {
                TagDescription = "This is a very long description of everything and even more";
                WorstQualityCode = 0xff;
                Value = true;
                ValState0 = "Falsch ist leer";
                ValState1 = "Wahr ist klar";
                return true;
            }
            if (mode == 2)
            {
                TagDescription = "Another description";
                WorstQualityCode = 0xa5;
                Value = false;
                ValState0 = "Falsch ist leer";
                ValState1 = "Wahr ist klar";
                return true;
            }
            return false;
        }
    }

    public class MtpDiBinMon : MtpDiBinView
    {
        public bool EnableFlutterRecog;
        public double FlutterTimeInterval;
        public int FlutterCounts;
        public bool FlutterIsActive;

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            // call AnaView
            base.PopulateFromAml(Name, ie, subscriber);

            // some more
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VFlutEn",
                (ct, o) => { this.EnableFlutterRecog = (bool)o; });

            subscriber?.SubscribeToAmlIdRefWith<double>(ie.Attribute, "VFlutTi",
                (ct, o) => { this.FlutterTimeInterval = (double)o; });

            subscriber?.SubscribeToAmlIdRefWith<int>(ie.Attribute, "VFlutCnt",
                (ct, o) => { this.FlutterCounts = (int)o; });

            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "VFlutAct",
                (ct, o) => { this.FlutterIsActive = (bool)o; });
        }
    }

    public class MtpDiActiveElement : MtpDiIndicatorElement
    {
        // Note: now spec, yet
        // therefore not sure, what common elements are
    }

    public class MtpDiBinValve : MtpDiActiveElement
    {
        public bool Ctrl;

        public MtpDiBinValve() : base()
        {
            this.DrawSymbolAsWell = true;
        }

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            // call AnaView
            base.PopulateFromAml(Name, ie, subscriber);

            // some more
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "Ctrl",
                (ct, o) => { this.Ctrl = (bool)o; DemandRedrawOnTick(); });
        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            var c = new MtpViewBinValve();
            c.Width = mtpWidth;
            c.Height = mtpHeight;
            c.DataContext = this;
            return c;
        }

        public override void RedrawSymbol(MtpVisuOptions visuOptions = null)
        {
            // what color?
            Brush color = null;
            if (this.Ctrl)
            {
                color = (visuOptions?.StateColorActiveBrush != null)
                    ? visuOptions.StateColorActiveBrush : Brushes.Red;
            }
            else
            {
                color = (visuOptions?.StateColorNonActiveBrush != null)
                    ? visuOptions.StateColorNonActiveBrush : Brushes.Black;
            }

            // set
            if (color != null)
                this.SymbolSetStateColor(color);
        }

    }

    public class MtpDiMonBinValve : MtpDiBinValve
    {
        // members only selected, a lot of "unnecessary" memebrs
        public bool ErrorActiveStatic;
        public bool ErrorActiveDynamic;

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            // call MtpDiBinValve
            base.PopulateFromAml(Name, ie, subscriber);

            // some more
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "MonStatErr",
                (ct, o) => { this.ErrorActiveStatic = (bool)o; });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "MonDynErr",
                (ct, o) => { this.ErrorActiveDynamic = (bool)o; });
        }

    }

    public class MtpDiBinDrive : MtpDiActiveElement
    {
        public bool ForwardCtrl;
        public bool ReverseCtrl;

        public MtpDiBinDrive() : base()
        {
            this.DrawSymbolAsWell = true;
        }

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            // call AnaView
            base.PopulateFromAml(Name, ie, subscriber);

            // some more
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "FwdCtrl",
                (ct, o) => { this.ForwardCtrl = (bool)o; DemandRedrawOnTick(); });
            subscriber?.SubscribeToAmlIdRefWith<bool>(ie.Attribute, "RevCtrl",
                (ct, o) => { this.ReverseCtrl = (bool)o; DemandRedrawOnTick(); });
        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            var c = new MtpViewBinDrive();
            c.Width = mtpWidth;
            c.Height = mtpHeight;
            c.DataContext = this;
            return c;
        }

        public override void RedrawSymbol(MtpVisuOptions visuOptions = null)
        {
            // what color?
            Brush color = null;
            if (this.ForwardCtrl && !this.ReverseCtrl)
            {
                color = (visuOptions?.StateColorForwardBrush != null)
                    ? visuOptions.StateColorForwardBrush : Brushes.Blue;
            }
            else if (!this.ForwardCtrl && this.ReverseCtrl)
            {
                color = (visuOptions?.StateColorReverseBrush != null)
                    ? visuOptions.StateColorReverseBrush : Brushes.Red;
            }
            else
            {
                color = (visuOptions?.StateColorNonActiveBrush != null)
                    ? visuOptions.StateColorNonActiveBrush : Brushes.Black;
            }

            // set
            if (color != null)
                this.SymbolSetStateColor(color);
        }

    }

    public class MtpDiPIDCntl : MtpDiActiveElement
    {
        // Not sure which members to expose here

        public enum OpState { Off = 0, Op, Aut }
        public string[] OpStateText = new string[] { "Off", "Op", "Aut" };
        public Brush[] OpStateBrush = new Brush[] { Brushes.DarkGray, Brushes.Gold, Brushes.Green };

        // Value
        private OpState valState = OpState.Aut;
        public OpState Value
        {
            get { return valState; }
            set
            {
                valState = value;
                RaisePropertyChanged("Value");
                RaisePropertyChanged("ValueText");
                RaisePropertyChanged("ValueBrush");
            }
        }

        public string ValueText
        {
            get
            {
                var i = (int)valState;
                if (i >= 0 && i < OpStateText.Length)
                    return OpStateText[i];
                return "?";
            }
        }

        public Brush ValueBrush
        {
            get
            {
                var i = (int)valState;
                if (i >= 0 && i < OpStateBrush.Length)
                    return OpStateBrush[i];
                return Brushes.Red;
            }
        }

        // Construct

        public override void PopulateFromAml(string Name, InternalElementType ie, MtpDataSourceSubscriber subscriber)
        {
            this.TagName = "" + Name;
        }

        public override UserControl CreateVisualObject(double mtpWidth, double mtpHeight)
        {
            UserControl c = null;
            // ReSharper disable HeuristicUnreachableCode
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
#pragma warning disable 162
            if (true || mtpWidth <= 50 || mtpHeight <= 40)
#pragma warning restore 162
            {
                var bvt = new MtpViewPIDCntlTiny();
                c = bvt;
                c.Width = 40;
                c.Height = 30;
                c.DataContext = this;
                bvt.ButtonGo.Click += ButtonGo_Click;
            }
            else

#pragma warning disable 162
            {
                // ReSharper disable once HeuristicUnreachableCode
                var bvl = new MtpViewBinViewLarge();
                c = bvl;
                c.Width = 130;
                c.Height = 96;
                c.DataContext = this;
                bvl.ButtonGo.Click += ButtonGo_Click;
            }
#pragma warning restore 162
            Demo(2);
            // ReSharper enable HeuristicUnreachableCode
            return c;
        }

        public override bool Demo(int mode)
        {
            if (mode == 1)
            {
                TagDescription = "This is a very long description of everything and even more";
                WorstQualityCode = 0xff;
                Value = OpState.Off;
                return true;
            }
            if (mode == 2)
            {
                TagDescription = "Another description";
                WorstQualityCode = 0xa5;
                Value = OpState.Aut;
                return true;
            }
            if (mode == 3)
            {
                TagDescription = "Yet Another description";
                WorstQualityCode = 0x55;
                Value = OpState.Op;
                return true;
            }
            return false;
        }
    }
}
