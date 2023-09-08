/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Aml.Engine.CAEX;
using Mtp.DynamicInstances;
using WpfMtpControl.DataSources;

namespace WpfMtpControl
{
    public class MtpData
    {
        public Dictionary<string, MtpPicture> PictureCollection = new Dictionary<string, MtpPicture>();

        public class MtpBaseObject
        {
            public string Name = "";
            public string RefID = null;
            public int ObjClass = 0;
        }

        public class MtpConnectionObject : MtpBaseObject
        {
            public PointCollection points = null;
        }

        public class MtpPortedObject : MtpBaseObject
        {
            public PointCollection logicalPorts = null, nozzlePoints = null, measurementPoints = null;
        }

        public class MtpVisualObject : MtpPortedObject
        {
            public double? x, y, width, height, rotation;
            public string viewType, eVer, eClass, eIrdi, refID;

            public MtpVisualObjectRecord visObj = null;

            public MtpDynamicInstanceBase dynInstance = null;

            public void Parse(InternalElementType ie)
            {
                x = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie.Attribute, "X");
                y = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie.Attribute, "Y");
                width = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie.Attribute, "Width");
                height = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie.Attribute, "Height");
                rotation = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie.Attribute, "Rotation");
                viewType = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "ViewType");
                eVer = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "eClassVersion");
                eClass = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "eClassClassificationClass");
                eIrdi = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "eClassIRDI");
                refID = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "RefID");
            }

            public bool IsValid()
            {
                // because of PxC temporarily disable check eVer == null
                if (x == null || y == null || width == null || height == null
                    || (eClass == null && eIrdi == null))
                    return false;
                return true;
            }
        }

        public class MtpTopologyObject : MtpPortedObject
        {
            public double? x, y;
            public string refID;

            public MtpVisualObjectRecord visObj = null;

            public void Parse(InternalElementType ie)
            {
                x = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie.Attribute, "X");
                y = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie.Attribute, "Y");
                refID = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "RefID");
            }

            public bool IsValid()
            {
                if (x == null || y == null)
                    return false;
                return true;
            }
        }

        public class MtpPicture
        {
            public string Name = "";
            public SystemUnitClassType Picture = null;

            public Size TotalSize = new Size(0, 0);

            public List<MtpBaseObject> Objects = new List<MtpBaseObject>();

            public static MtpPicture ParsePicture(
                MtpVisualObjectLib objectLib,
                MtpDataSourceSubscriber subscriber,
                Dictionary<string, SystemUnitClassType> refIdToDynamicInstance,
                SystemUnitClassType picture,
                MtpSymbolMapRecordList makeUpConfigRecs = null)
            {
                // result
                var res = new MtpPicture();
                res.Picture = picture;

                // first, set up the canvas
                if (true)
                {
                    var width = MtpAmlHelper.FindAttributeValueByNameFromDouble(picture.Attribute, "Width");
                    var height = MtpAmlHelper.FindAttributeValueByNameFromDouble(picture.Attribute, "Height");
                    if (width == null || height == null || width < 1 || height < 1)
                        return null;
                    res.TotalSize = new Size(width.Value, height.Value);
                }

                // assume, that the elements below are in a list
                foreach (var ie in picture.InternalElement)
                {
                    // the check described in VDI2658 rely on RefBaseSystemUnitPath
                    if (ie == null || ie.Name == null || ie.RefBaseSystemUnitPath == null)
                        continue;

                    var refID = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "RefID");

                    // do a classification based on numbers to easily comapre
                    var ec = 0;
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/Connection/Pipe") ec = 100;
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/Connection/FunctionLine") ec = 101;
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/Connection/MeasurementLine") ec = 102;
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/VisualObject") ec = 200;
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/TopologyObject/Termination/Nozzle") ec = 300;
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/TopologyObject/Termination/Source") ec = 301;
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/TopologyObject/Termination/Sink") ec = 302;

                    //
                    // Pipe, FunctionLine, MeasurementLine
                    //
                    if (ec >= 100 && ec <= 199)
                    {
                        // access (still string) information
                        var edgepath = MtpAmlHelper.FindAttributeValueByName(ie.Attribute, "Edgepath");
                        if (edgepath == null)
                            continue;
                        var points = MtpAmlHelper.PointCollectionFromString(edgepath);
                        if (points == null || points.Count < 2)
                            continue;

                        var co = new MtpConnectionObject();
                        co.Name = ie.Name;
                        co.RefID = refID;
                        co.ObjClass = ec;
                        co.points = points;
                        res.Objects.Add(co);
                    }

                    //
                    // Nozzle information?
                    //
                    var nozzlePoints = new PointCollection();
                    var measurementPoints = new PointCollection();
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (ec >= 200 && ec <= 399)
                    {
                        foreach (var ie2 in ie.InternalElement)
                        {
                            if (ie2 != null
                                && ie2.RefBaseSystemUnitPath?.Trim() == "MTPHMISUCLib/PortObject/Nozzle")
                            {
                                // found nozzle with valid information?
                                var nx = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie2.Attribute, "X");
                                var ny = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie2.Attribute, "Y");
                                if (nx != null && ny != null)
                                    // add
                                    nozzlePoints.Add(new Point(nx.Value, ny.Value));
                            }

                            if (ie2 != null
                                && ie2.RefBaseSystemUnitPath?.Trim() == "MTPHMISUCLib/PortObject/MeasurementPoint")
                            {
                                // found measurement point with valid information?
                                var nx = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie2.Attribute, "X");
                                var ny = MtpAmlHelper.FindAttributeValueByNameFromDouble(ie2.Attribute, "Y");
                                if (nx != null && ny != null)
                                    // add
                                    measurementPoints.Add(new Point(nx.Value, ny.Value));
                            }
                        }
                    }

                    if (ie.Name == "V001")
                    {
                        ;
                    }

                    //
                    // VisualObject
                    //
                    if (ec >= 200 && ec <= 299)
                    {
                        // create object and parse
                        var vo = new MtpVisualObject();
                        vo.Name = ie.Name;
                        vo.RefID = refID;
                        vo.ObjClass = ec;
                        vo.Parse(ie);
                        if (!vo.IsValid())
                            continue;
                        res.Objects.Add(vo);

                        // add ports
                        vo.logicalPorts = null;
                        vo.measurementPoints = measurementPoints;
                        vo.nozzlePoints = nozzlePoints;

                        // try find an XAML object
                        vo.visObj = objectLib.FindVisualObjectByClass(vo.eVer, vo.eClass, vo.eIrdi);

                        // help improving this search
                        if (makeUpConfigRecs != null)
                        {
                            makeUpConfigRecs.Add(new MtpSymbolMapRecord(
                                vo.eVer, vo.eClass, vo.eIrdi, SymbolDefault: "{to be set}",
                                Comment: "" + vo.Name + "," + vo.RefID));
                        }

                        // try find dynamic instances
                        if (vo.refID != null && refIdToDynamicInstance != null
                            && refIdToDynamicInstance.ContainsKey(vo.refID))
                        {
                            // try get the dynamic instance
                            var ieDI = refIdToDynamicInstance[vo.refID] as InternalElementType;

                            if (ieDI != null && ieDI.RefBaseSystemUnitPath != null
                                && ieDI.RefBaseSystemUnitPath.Trim().Length > 0)
                            {
                                var bsup = ieDI.RefBaseSystemUnitPath.Trim();

                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/AnaView"
                                    || bsup == "MTPDataObjectSUCLib/DataAssembly/IndicatorElement/AnaView")
                                    vo.dynInstance = new MtpDiAnaView();
                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/AnaView/AnaMon"
                                    || bsup == "MTPDataObjectSUCLib/DataAssembly/IndicatorElement/AnaView/AnaMon")
                                    vo.dynInstance = new MtpDiAnaMon();

                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/DIntiew"
                                    || bsup == "MTPDataObjectSUCLib/DataAssembly/IndicatorElement/DIntView")
                                    vo.dynInstance = new MtpDiDIntView();
                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/AnaView/DIntMon"
                                    || bsup == "MTPDataObjectSUCLib/DataAssembly/IndicatorElement/AnaView/DIntMon")
                                    vo.dynInstance = new MtpDiDIntMon();

                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/BinView"
                                    || bsup == "MTPDataObjectSUCLib/DataAssembly/IndicatorElement/BinView")
                                    vo.dynInstance = new MtpDiBinView();

                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/BinMon"
                                    || bsup == "MTPDataObjectSUCLib/DataAssembly/IndicatorElement/BinMon")
                                    vo.dynInstance = new MtpDiBinMon();

                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/BinVlv")
                                    vo.dynInstance = new MtpDiBinValve();
                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/MonBinVlv")
                                    vo.dynInstance = new MtpDiMonBinValve();

                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/BinDrv")
                                    vo.dynInstance = new MtpDiBinDrive();

                                if (bsup == "MTPDataObjectSUCLib/DataAssembly/ActiveElement/PIDCtrl")
                                    vo.dynInstance = new MtpDiPIDCntl();
                            }

                            // found?
                            if (vo.dynInstance != null)
                            {
                                vo.dynInstance.PopulateFromAml(ie.Name, ieDI, subscriber);
                            }
                        }
                    }

                    //
                    // Topology Object
                    //
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (ec >= 300 && ec <= 309)
                    {
                        // create object and parse
                        var to = new MtpTopologyObject();
                        to.Name = ie.Name;
                        to.RefID = refID;
                        to.ObjClass = ec;
                        to.Parse(ie);
                        if (!to.IsValid())
                            continue;
                        res.Objects.Add(to);

                        // add ports
                        to.logicalPorts = null;
                        to.measurementPoints = measurementPoints;
                        to.nozzlePoints = nozzlePoints;

                        // draw source / sink?
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (ec >= 301 && ec <= 302)
                        {
                            // get visual object
                            to.visObj = objectLib.FindVisualObjectByName(
                                new[] { "Source_general", "Sink_general" }[ec - 301]);
                        }
                    }
                }

                // return picture
                return res;
            }
        }

        public void LoadStream(
            MtpVisualObjectLib objectLib, IMtpDataSourceFactoryOpcUa dataSourceFactory,
            MtpDataSourceOpcUaPreLoadInfo preLoadInfo,
            MtpDataSourceSubscriber subscriber, Stream stream,
            MtpSymbolMapRecordList makeUpConfigRecs = null)
        {
            // check
            if (stream == null)
                return;

            // try open file
            var doc = CAEXDocument.LoadFromStream(stream);
            if (doc == null)
                return;

            // load dynamic Instances
            var refIdToDynamicInstance = MtpAmlHelper.FindAllDynamicInstances(doc.CAEXFile);

            // load data sources
            if (dataSourceFactory != null)
                MtpAmlHelper.CreateDataSources(dataSourceFactory, preLoadInfo, doc.CAEXFile);

            // index pictures
            var pl = MtpAmlHelper.FindAllMtpPictures(doc.CAEXFile);
            foreach (var pi in pl)
            {
                var p = MtpPicture.ParsePicture(objectLib, subscriber, refIdToDynamicInstance,
                            pi.Item2, makeUpConfigRecs);
                if (p != null)
                    this.PictureCollection.Add(pi.Item1, p);
            }
        }

        public void LoadAmlOrMtp(
            MtpVisualObjectLib objectLib, IMtpDataSourceFactoryOpcUa dataSourceFactory,
            MtpDataSourceOpcUaPreLoadInfo preLoadInfo,
            MtpDataSourceSubscriber subscriber, string fn,
            MtpSymbolMapRecordList makeUpConfigRecs = null)
        {
            // check
            if (fn == null)
                return;

            // check if we have a mtp-file, which needs to be unzipped
            bool unzip = fn.ToLower().EndsWith(".mtp") || fn.ToLower().EndsWith(".zip");

            // easy?
            if (!unzip)
            {
                using (var stream = File.OpenRead(fn))
                {
                    LoadStream(objectLib, dataSourceFactory, preLoadInfo, subscriber, stream, makeUpConfigRecs);
                }
                return;
            }

            // not easy ..
            using (var file = File.OpenRead(fn))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                // simply take the first .aml file
                foreach (var entry in zip.Entries)
                {
                    // take the 1st *.anl file; should be only one on the root level, even for MultiFileMTP
                    if (entry.FullName.ToLower().EndsWith(".aml"))
                    {
                        using (var stream = entry.Open())
                        {
                            LoadStream(objectLib, dataSourceFactory, preLoadInfo, subscriber, stream, makeUpConfigRecs);
                        }
                        break;
                    }
                }
            }
        }
    }
}
