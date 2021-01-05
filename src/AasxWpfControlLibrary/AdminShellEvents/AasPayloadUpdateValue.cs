/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxPackageExplorer;
using Newtonsoft.Json;
using System.Xml.Serialization;
using AasxWpfControlLibrary.MiniMarkup;

namespace AdminShellEvents
{
    public class AasPayloadUpdateValueItem
    {
        /// <summary>
        /// Path of the element to be updated. Contains one or more Keys, relative to the Observable of
        /// the defined Event.
        /// </summary>
        public AdminShell.KeyList Path { get; set; }

        /// <summary>
        /// Serialized updated value of the updated element.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// ValueId of the update element.
        /// </summary>
        public AdminShell.Reference ValueId { get; set; }

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

        public string ToString()
        {
            var res = "MsgUpdateValueItem: {Observable}";
            if (Path != null)
                foreach (var k in Path)
                    res += "/" + k.value;
            if (Value != null)
                res += " = " + Value;
            if (ValueId != null)
                res += " = " + ValueId.ToString();
            return res;
        }

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

    }

    /// <summary>
    /// This event-message transports updated information of values of designated SubmodelElements
    /// </summary>
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

        public override MiniMarkupBase ToMarkup()
        {
            var res = new MiniMarkupSequence();
            if (Values != null)
                foreach (var val in Values)
                    res.Children.Add(val.ToMarkup());            
            return res;
        }
    }
}
