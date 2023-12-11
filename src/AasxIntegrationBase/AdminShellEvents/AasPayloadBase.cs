/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// to be disabled for AASX Server
#define UseMarkup

using AasxIntegrationBase.MiniMarkup;
using System;
using System.Collections.Generic;

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// Base class for any AAS event payload. 
    /// Payloads are wrapped in AAS event envelopes or transactions.
    /// </summary>
    [DisplayName("AasPayloadBase")]
    public class AasPayloadBase
    {
#if UseMarkup
        public virtual MiniMarkupBase ToMarkup()
        {
            return null;
        }
#endif
    }

    /// <summary>
    /// Denotes a list of <c>AasPayloadBase</c> or derived payloads
    /// </summary>
    [DisplayName("ListOfAasPayloadBase")]
    public class ListOfAasPayloadBase : List<AasPayloadBase>
    {

        public ListOfAasPayloadBase() : base() { }

        public ListOfAasPayloadBase(ListOfAasPayloadBase other) : base()
        {
            if (other != null)
                foreach (var pl in other)
                {
                    // ReSharper disable once RedundantExplicitParamsArrayCreation
                    var opl = Activator.CreateInstance(pl.GetType(), new object[] { pl });
                    if (opl is AasPayloadBase npl)
                        this.Add(npl);
                }
        }

    }

    /// <summary>
    /// Marks a single payload items, even if from different event types
    /// </summary>
    public interface IAasPayloadItem
    {
        string GetDetailsText();
    }
}
