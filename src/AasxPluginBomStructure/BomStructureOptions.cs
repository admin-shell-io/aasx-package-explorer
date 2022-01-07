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
using AdminShellNS;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

namespace AasxPluginBomStructure
{
    public enum BomLinkDirection { None, Forward, Backward, Both }

    public class BomLinkStyle : AdminShell.IGetGlobalReference
    {
        public AdminShell.Identifier Match;
        public bool Skip;
        public BomLinkDirection Direction;
        public string Color;
        public double Width;
        public string Text;
        public double FontSize;
        public bool Dashed, Bold, Dotted;

        public AdminShell.GlobalReference GetGlobalReference()
            => new AdminShell.GlobalReference(Match);

        public BomLinkStyle() : base() { }

#if !DoNotUseAasxCompatibilityModels
        public BomLinkStyle(AasxCompatibilityModels.AasxPluginBomStructure.BomLinkStyleV20 src) : base()
        {
            if (src.Match != null)
                Match = new AdminShell.Identifier(src.Match.value);

            Skip = src.Skip;
            Direction = (BomLinkDirection)((int)src.Direction);
            Color = src.Color;
            Width = src.Width;
            Text = src.Text;
            FontSize = src.FontSize;
            Dashed = src.Dashed;
            Bold = src.Bold;
            Dotted = src.Dotted;
        }
#endif
    }

    public class BomLinkStyleList : List<BomLinkStyle>
    {
        [JsonIgnore]
        public AasReferenceStore<BomLinkStyle> Store = new AasReferenceStore<BomLinkStyle>();

        public void Index()
        {
            // as this object is attached the options but could be executed multiple times in the life time
            // clear at first
            Store.Clear();
            // now re-index
            foreach (var ls in this)
                Store.Index(AdminShell.GlobalReference.CreateNew(ls.Match), ls);
        }
    }

    public class BomNodeStyle : AdminShell.IGetGlobalReference
    {
        public AdminShell.Identifier Match;
        public bool Skip;

        public string Shape;
        public string Background, Foreground;
        public double LineWidth;
        public double Radius;
        public string Text;
        public double FontSize;
        public bool Dashed, Bold, Dotted;

        public AdminShell.GlobalReference GetGlobalReference()
            => new AdminShell.GlobalReference(Match);

        public BomNodeStyle() : base() { }

#if !DoNotUseAasxCompatibilityModels
        public BomNodeStyle(AasxCompatibilityModels.AasxPluginBomStructure.BomNodeStyleV20 src) : base()
        {
            if (src.Match != null)
                Match = new AdminShell.Identifier(src.Match.value);

            Skip = src.Skip;
            Shape = src.Shape;
            Background = src.Background;
            Foreground = src.Foreground;
            LineWidth = src.LineWidth;
            Radius = src.Radius;
            Text = src.Text;
            FontSize = src.FontSize;
            Dashed = src.Dashed;
            Bold = src.Bold;
            Dotted = src.Dotted;
        }
#endif
    }

    public class BomNodeStyleList : List<BomNodeStyle>
    {
        [JsonIgnore]
        public AasReferenceStore<BomNodeStyle> Store = new AasReferenceStore<BomNodeStyle>();

        public void Index()
        {
            // as this object is attached the options but could be executed multiple times in the life time
            // clear at first
            Store.Clear();
            // now re-index
            foreach (var ls in this)
                Store.Index(AdminShell.GlobalReference.CreateNew(ls.Match), ls);
        }
    }

    public class BomStructureOptionsRecord : AasxIntegrationBase.AasxPluginOptionsLookupRecordBase
    {
        public int Layout;
        public bool? Compact;

        public BomLinkStyleList LinkStyles = new BomLinkStyleList();
        public BomNodeStyleList NodeStyles = new BomNodeStyleList();

        public BomStructureOptionsRecord() : base() { }

#if !DoNotUseAasxCompatibilityModels
        public BomStructureOptionsRecord(
            AasxCompatibilityModels.AasxPluginBomStructure.BomStructureOptionsRecordV20 src)
            : base(src)
        {
            if (src == null)
                return;

            Layout = src.Layout;
            Compact = src.Compact;

            if (src.LinkStyles != null)
                foreach (var ls in src.LinkStyles)
                    LinkStyles.Add(new BomLinkStyle(ls));

            if (src.NodeStyles != null)
                foreach (var ns in src.NodeStyles)
                    NodeStyles.Add(new BomNodeStyle(ns));
        }
#endif

        public void Index()
        {
            LinkStyles.Index();
            NodeStyles.Index();
        }

        public BomLinkStyle FindFirstLinkStyle(AdminShell.SemanticId semId)
        {
            if (semId == null)
                return null;
            return LinkStyles.Store.FindElementByReference(semId);
        }

        public BomNodeStyle FindFirstNodeStyle(AdminShell.SemanticId semId)
        {
            if (semId == null)
                return null;
            return NodeStyles.Store.FindElementByReference(semId);
        }

    }

    public class BomStructureOptionsRecordList : List<BomStructureOptionsRecord>
    {
        public BomStructureOptionsRecordList() { }

        public BomStructureOptionsRecordList(IEnumerable<BomStructureOptionsRecord> collection) : base(collection) { }

        public BomLinkStyle FindFirstLinkStyle(AdminShell.SemanticId semId)
        {
            foreach (var rec in this)
            {
                var res = rec.FindFirstLinkStyle(semId);
                if (res != null)
                    return res;
            }
            return null;
        }

        public BomNodeStyle FindFirstNodeStyle(AdminShell.SemanticId semId)
        {
            foreach (var rec in this)
            {
                var res = rec.FindFirstNodeStyle(semId);
                if (res != null)
                    return res;
            }
            return null;
        }
    }

    public class BomStructureOptions : AasxIntegrationBase.AasxPluginLookupOptionsBase
    {
        public List<BomStructureOptionsRecord> Records = new List<BomStructureOptionsRecord>();

        public BomStructureOptions() : base() { }

#if !DoNotUseAasxCompatibilityModels
        public BomStructureOptions(AasxCompatibilityModels.AasxPluginBomStructure.BomStructureOptionsV20 src)
            : base(src)
        {
            if (src.Records != null)
                foreach (var rec in src.Records)
                    Records.Add(new BomStructureOptionsRecord(rec));
        }
#endif

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static BomStructureOptions CreateDefault()
        {
            var rec = new BomStructureOptionsRecord();
            rec.AllowSubmodelSemanticId.Add("http://smart.festo.com/id/type/submodel/BOM/1/1");

            var opt = new BomStructureOptions();
            opt.Records.Add(rec);

            return opt;
        }

        /// <summary>
        /// For faster access of styles, index them by a hash map
        /// </summary>
        public void Index()
        {
            foreach (var rec in Records)
                rec.Index();
        }

        /// <summary>
        /// Find matching options records
        /// </summary>
        public IEnumerable<BomStructureOptionsRecord> MatchingRecords(AdminShell.SemanticId semId)
        {
            foreach (var rec in Records)
                if (rec.AllowSubmodelSemanticId != null)
                    foreach (var x in rec.AllowSubmodelSemanticId)
                        if (semId != null && semId.Matches(x))
                            yield return rec;
        }
    }
}
