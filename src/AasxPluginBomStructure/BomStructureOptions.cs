/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using AasxIntegrationBase;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

namespace AasxPluginBomStructure
{
    //public enum BomLinkDirection { None, Forward, Backward, Both }

    //public class BomLinkStyle // : IReferable
    //{
    //    public Aas.Key Match;
    //    public bool Skip;
    //    public BomLinkDirection Direction;
    //    public string Color;
    //    public double Width;
    //    public string Text;
    //    public double FontSize;
    //    public bool Dashed, Bold, Dotted;

    //    // public Reference GetReference(bool includeParents = true) => new Aas.Reference(Match);
    //}

    //public class BomLinkStyleList : List<BomLinkStyle>
    //{
    //    [JsonIgnore]
    //    public AasReferenceStore<BomLinkStyle> Store = new AasReferenceStore<BomLinkStyle>();

    //    public void Index()
    //    {
    //        foreach (var ls in this)
    //            Store.Index(ExtendReference.CreateFromKey(ls.Match), ls);
    //    }
    //}

    //public class BomNodeStyle // : IGetReference
    //{
    //    public Aas.Key Match;
    //    public bool Skip;

    //    public string Shape;
    //    public string Background, Foreground;
    //    public double LineWidth;
    //    public double Radius;
    //    public string Text;
    //    public double FontSize;
    //    public bool Dashed, Bold, Dotted;

    //    // public Reference GetReference(bool includeParents = true) => new Aas.Reference(Match);
    //}

    //public class BomNodeStyleList : List<BomNodeStyle>
    //{
    //    [JsonIgnore]
    //    public AasReferenceStore<BomNodeStyle> Store = new AasReferenceStore<BomNodeStyle>();

    //    public void Index()
    //    {
    //        foreach (var ls in this)
    //            Store.Index(ExtendReference.CreateFromKey(ls.Match), ls);
    //    }
    //}

    public class BomStructureOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public int Layout;
        public bool? Compact;

        public ListOfBomArguments LinkStyles = new ListOfBomArguments();
        public ListOfBomArguments NodeStyles = new ListOfBomArguments();

        public void Index()
        {
            LinkStyles.Index();
            NodeStyles.Index();
        }

        public BomArguments FindFirstLinkStyle(Aas.IReference semId)
        {
            if (semId == null)
                return null;
            return LinkStyles.Store.FindElementByReference(semId, MatchMode.Relaxed);
        }

        public BomArguments FindFirstNodeStyle(Aas.IReference semId)
        {
            if (semId == null)
                return null;
            return NodeStyles.Store.FindElementByReference(semId, MatchMode.Relaxed);
        }

    }

    public class BomStructureOptionsRecordList : List<BomStructureOptionsRecord>
    {
        public BomStructureOptionsRecordList() { }

        public BomStructureOptionsRecordList(IEnumerable<BomStructureOptionsRecord> collection) : base(collection) { }

        public BomArguments FindFirstLinkStyle(Aas.IReference semId)
        {
            foreach (var rec in this)
            {
                var res = rec.FindFirstLinkStyle(semId);
                if (res != null)
                    return res;
            }
            return null;
        }

        public BomArguments FindFirstNodeStyle(Aas.IReference semId)
        {
            foreach (var rec in this)
            {
                var res = rec.FindFirstNodeStyle(semId);
                if (res != null)
                    return res;
            }
            return null;
        }

        public void Index()
        {
            foreach (var rec in this)
                rec?.Index();
        }
    }

    public class BomStructureOptions : AasxPluginLookupOptionsBase
    {
        public BomStructureOptionsRecordList Records = new BomStructureOptionsRecordList();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static BomStructureOptions CreateDefault()
        {
            var rec = new BomStructureOptionsRecord();
            rec.AllowSubmodelSemanticId.Add(
                new Aas.Key(Aas.KeyTypes.Submodel, "http://smart.festo.com/id/type/submodel/BOM/1/1"));

            var opt = new BomStructureOptions();
            opt.Records.Add(rec);

            return opt;
        }

        public void Index()
        {
            Records?.Index();
        }
    }
}
