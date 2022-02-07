﻿/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// to be disabled for AASX Server
#define UseMarkup 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AasxIntegrationBase;
using AasxIntegrationBase.MiniMarkup;
using AdminShellNS;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// Single item of a update value payload.
    /// The element denoted by <c>Path</c> is changed in its value and shall not be devided into further
    /// single payloads.
    /// </summary>
    [DisplayName("AasPayloadUpdateValueItem")]
    public class AasPayloadUpdateValueItem : IAasPayloadItem, AdminShell.IAasDiaryEntry
    {
        /// <summary>
        /// Path of the element to be updated. Contains one or more Keys, relative to the Observable of
        /// the defined Event.
        /// </summary>
        public AdminShell.KeyList Path { get; set; }

        /// <summary>
        /// Serialized updated value of the updated element.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// ValueId of the update element.
        /// </summary>
        public AdminShell.Reference ValueId { get; set; }

        /// <summary>
        /// Direct reference to Referable, when value item was successfully processed.
        /// Note: only runtime value; not specified; not interoperable
        /// </summary>
        [JsonIgnore]
        public AdminShell.Referable FoundReferable;

        //
        // Constructor
        //

        public AasPayloadUpdateValueItem(
            AdminShell.KeyList path = null,
            string value = null,
            AdminShell.Reference valueId = null)
        {
            Path = path;
            Value = value;
            ValueId = valueId;
        }

        //
        // Serialisation
        //

        public override string ToString()
        {
            var res = "PayloadUpdateValueItem: {Observable}";
            if (Path != null)
                foreach (var k in Path)
                    res += "/" + k.value;
            if (Value != null)
                res += " = " + Value;
            if (ValueId != null)
                res += " = " + ValueId.ToString();
            return res;
        }

#if UseMarkup
        public MiniMarkupBase ToMarkup()
        {
            var left = "  MsgUpdateValueItem: {Observable}";
            if (Path != null)
                foreach (var k in Path)
                    left += "/" + k.value;

            var right = "";
            if (Value != null)
                right += " = " + Value;
            if (ValueId != null)
                right += " = " + ValueId.ToString();

            return new MiniMarkupLine(
                new MiniMarkupRun(left, isMonospaced: true, padsize: 80),
                new MiniMarkupRun(right));
        }

        public string GetDetailsText()
        {
            return "";
        }
#endif
    }

    /// <summary>
    /// This event payload transports updated information of values of designated SubmodelElements
    /// </summary>
    [DisplayName("AasPayloadUpdateValue")]
    public class AasPayloadUpdateValue : AasPayloadBase
    {
        /// <summary>
        /// Holds a list of update value items, each of them relative to the Event's Observable.
        /// </summary>
        public List<AasPayloadUpdateValueItem> Values = new List<AasPayloadUpdateValueItem>();

        /// <summary>
        /// Flags, if the update value changes reported in the event message are already indorporated
        /// in the AAS, e.g. by the event producing/ transmitting entity or not.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public bool IsAlreadyUpdatedToAAS;

        //
        // Constructor
        //

        public AasPayloadUpdateValue() { }

        public AasPayloadUpdateValue(AasPayloadUpdateValueItem[] values)
        {
            if (values != null)
                Values.AddRange(values);
        }

        public AasPayloadUpdateValue(AasPayloadUpdateValueItem value)
        {
            if (value != null)
                Values.Add(value);
        }

        public AasPayloadUpdateValue(AasPayloadUpdateValue other)
        {
            if (other.Values != null)
                Values.AddRange(other.Values);
        }

        //
        // Serialisation
        //

        public override string ToString()
        {
            var res = base.ToString();
            if (Values != null)
                foreach (var val in Values)
                    res += Environment.NewLine + val.ToString();
            return res;
        }

#if UseMarkup
        public override MiniMarkupBase ToMarkup()
        {
            var res = new MiniMarkupSequence();
            if (Values != null)
                foreach (var val in Values)
                    res.Children.Add(val.ToMarkup());
            return res;
        }
#endif
    }
}
