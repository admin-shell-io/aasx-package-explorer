/*
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
using AasxPackageExplorer;
using AasxWpfControlLibrary.MiniMarkup;
using AdminShellNS;
using Newtonsoft.Json;

namespace AdminShellEvents
{
    /// <summary>
    /// Single item of a structural change payload
    /// </summary>
    public class AasPayloadStructuralChangeItem
    {
        /// <summary>
        /// Enum telling the reason for a change. According to CRUD principle.
        /// (Retrieve make no sense, update = modify, in order to avoid mismatch with update value)
        /// </summary>
        public enum ChangeReason { Create, Modify, Delete }

        /// <summary>
        /// Reason for the change. According to CRUD principle.
        /// (Retrieve make no sense, update = modify, in order to avoid mismatch with update value)
        /// </summary>
        public ChangeReason Reason;

        /// <summary>
        /// Timestamp of generated (sending) event in UTC time.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Path of the element which was structurally changed. Contains one or more Keys, relative to the 
        /// Observable of the defined Event. 
        /// Is null / empty, if identical to Observable.
        /// </summary>
        public AdminShell.KeyList Path { get; set; }

        /// <summary>
        /// JSON-Serializatin of the Submodel, SMC, SME which was denoted by Observabale and Path.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// If the reason is create and the data element to be created is part of an (ordered) collection,
        /// and if >= 0, give the index within the collection, where the data element is to be created
        /// </summary>
        public int CreateAtIndex = -1;

        //
        // Constructor
        //

        public AasPayloadStructuralChangeItem(
            ChangeReason reason,
            AdminShell.KeyList path = null)
        {
            Reason = reason;
            Path = path;
        }

        //
        // Serialisation
        //

        public override string ToString()
        {
            var res = "PayloadStructuralChangeItem: {Observable}";
            if (Path != null)
                foreach (var k in Path)
                    res += "/" + k.value;
            res += " -> " + Reason.ToString();
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
            right += " -> " + Reason.ToString();

            return new MiniMarkupLine(
                new MiniMarkupRun(left, isMonospaced: true, padsize: 80),
                new MiniMarkupRun(right));
        }
#endif

        public AdminShell.Referable GetDataAsReferable()
        {
            // access
            if (Data == null)
                return null;

            // try deserialize
            return AdminShellSerializationHelper.DeserializeFromJSON<AdminShell.Referable>(Data);
        }
    }

    /// <summary>
    /// This event payload transports information, if structural elements of the AAS were created, modified or
    /// deleted (CRUD). (Retrieve make no sense, update = modify, in order to avoid mismatch with update value)
    /// </summary>
    public class AasPayloadStructuralChange : AasPayloadBase
    {
        /// <summary>
        /// Holds a list of changes, to be sequentially applied to the Observable (see <c>Path</c>) in numerical
        /// order 0..n.
        /// </summary>
        public List<AasPayloadStructuralChangeItem> Changes = new List<AasPayloadStructuralChangeItem>();

        //
        // Constructor
        //

        public AasPayloadStructuralChange() { }

        public AasPayloadStructuralChange(AasPayloadStructuralChangeItem[] changes)
        {
            if (changes != null)
                Changes.AddRange(changes);
        }

        public AasPayloadStructuralChange(AasPayloadStructuralChangeItem change)
        {
            if (change != null)
                Changes.Add(change);
        }

        //
        // Serialisation
        //

        public override string ToString()
        {
            var res = base.ToString();
            if (Changes != null)
                foreach (var chg in Changes)
                    res += Environment.NewLine + chg.ToString();
            return res;
        }

#if UseMarkup
        public override MiniMarkupBase ToMarkup()
        {
            var res = new MiniMarkupSequence();
            if (Changes != null)
                foreach (var chg in Changes)
                    res.Children.Add(chg.ToMarkup());
            return res;
        }
    }
#endif
}
