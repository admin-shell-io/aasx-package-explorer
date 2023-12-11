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
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

#if !DoNotUseAasxCompatibilityModels

namespace AasxCompatibilityModels.AasxIntegrationBase
{
    /// <summary>
    /// Base class for an options record. This is a piece of options information, which is
    /// associated with an id of a Submodel template.
    /// </summary>
    public class AasxPluginOptionsRecordBaseV20
    {
    }

    /// <summary>
    /// Base class of plugin options, which may be also load from file.
    /// </summary>
    public class AasxPluginOptionsBaseV20
    {
    }

    //
    // Extension: options (records) with lookup of semantic ids
    //

    /// <summary>
    /// Base class for an options record. This is a piece of options information, which is
    /// associated with an id of a Submodel template.
    /// This base class is extended for lookup information.
    /// </summary>
    public class AasxPluginOptionsLookupRecordBaseV20 : AasxPluginOptionsRecordBaseV20
    {
        /// <summary>
        /// This keyword is used by the plugin options to code allowed semantic ids for
        /// a Submodel sensitive plugin
        /// </summary>
        public List<AdminShellV20.Key> AllowSubmodelSemanticId = new List<AdminShellV20.Key>();
    }

    /// <summary>
    /// Base class of plugin options, which may be also load from file.
    /// This base class is extended for lookup information.
    /// </summary>
    public class AasxPluginLookupOptionsBaseV20 : AasxPluginOptionsBaseV20
    {
    }
}

#endif