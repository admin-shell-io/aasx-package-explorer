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
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

#if !DoNotUseAasxCompatibilityModels

namespace AasxCompatibilityModels.AasxPluginBomStructure
{
    public enum BomLinkDirection { None, Forward, Backward, Both }

    public class BomLinkStyleV20
    {
        public AdminShellV20.Key Match;
        public bool Skip;
        public BomLinkDirection Direction;
        public string Color;
        public double Width;
        public string Text;
        public double FontSize;
        public bool Dashed, Bold, Dotted;
    }

    public class BomLinkStyleListV20 : List<BomLinkStyleV20>
    {
    }

    public class BomNodeStyleV20
    {
        public AdminShellV20.Key Match;
        public bool Skip;

        public string Shape;
        public string Background, Foreground;
        public double LineWidth;
        public double Radius;
        public string Text;
        public double FontSize;
        public bool Dashed, Bold, Dotted;
    }

    public class BomNodeStyleListV20 : List<BomNodeStyleV20>
    {
    }

    public class BomStructureOptionsRecordV20
        : AasxCompatibilityModels.AasxIntegrationBase.AasxPluginOptionsLookupRecordBaseV20
    {
        public int Layout;
        public bool? Compact;

        public BomLinkStyleListV20 LinkStyles = new BomLinkStyleListV20();
        public BomNodeStyleListV20 NodeStyles = new BomNodeStyleListV20();
    }

    public class BomStructureOptionsRecordListV20 : List<BomStructureOptionsRecordV20>
    {
    }

    public class BomStructureOptionsV20 : AasxCompatibilityModels.AasxIntegrationBase.AasxPluginLookupOptionsBaseV20
    {
        public List<BomStructureOptionsRecordV20> Records = new List<BomStructureOptionsRecordV20>();
    }
}

#endif