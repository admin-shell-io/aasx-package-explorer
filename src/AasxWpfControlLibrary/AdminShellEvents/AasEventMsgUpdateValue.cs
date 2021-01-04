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

namespace AdminShellEvents
{
    public class AasEventMsgUpdateValueItem
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

        public AasEventMsgUpdateValueItem(
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

    }

    /// <summary>
    /// This event-message transports updated information of values of designated SubmodelElements
    /// </summary>
    public class AasEventMsgUpdateValue : AasEventMsgBase
    {
        /// <summary>
        /// Holds a list of update value items, each of them relative to the Event's Observable.
        /// </summary>
        public List<AasEventMsgUpdateValueItem> Values = new List<AasEventMsgUpdateValueItem>();

        //
        // Constructor
        //

        public AasEventMsgUpdateValue(
            DateTime timestamp,
            AdminShell.Reference source = null,
            AdminShell.SemanticId sourceSemanticId = null,
            AdminShell.Reference observableReference = null,
            AdminShell.SemanticId observableSemanticId = null,
            string topic = null,
            string subject = null)
            : base(timestamp, source, sourceSemanticId, observableReference, observableSemanticId, topic, subject) 
        {
        }

        public AasEventMsgUpdateValue(
            DateTime timestamp,
            AasEventMsgUpdateValueItem[] values,
            AdminShell.Reference source = null,
            AdminShell.SemanticId sourceSemanticId = null,
            AdminShell.Reference observableReference = null,
            AdminShell.SemanticId observableSemanticId = null,
            string topic = null,
            string subject = null)
            : base(timestamp, source, sourceSemanticId, observableReference, observableSemanticId, topic, subject)
        {
            if (values != null)
                Values.AddRange(values);
        }

        public AasEventMsgUpdateValue(
            DateTime timestamp,
            AasEventMsgUpdateValueItem value,
            AasEventMsgUpdateValueItem[] values,
            AdminShell.Reference source = null,
            AdminShell.SemanticId sourceSemanticId = null,
            AdminShell.Reference observableReference = null,
            AdminShell.SemanticId observableSemanticId = null,
            string topic = null,
            string subject = null)
            : base(timestamp, source, sourceSemanticId, observableReference, observableSemanticId, topic, subject)
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
    }
}
