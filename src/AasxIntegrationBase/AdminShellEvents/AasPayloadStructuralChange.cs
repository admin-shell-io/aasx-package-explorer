/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// to be disabled for AASX Server
#define UseMarkup 

using AasxIntegrationBase.MiniMarkup;
using AdminShellNS;
using AdminShellNS.DiaryData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

// ReSharper disable ClassNeverInstantiated.Global

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// Enum telling the reason for a change. According to CRUD principle.
    /// (Retrieve make no sense, update = modify, in order to avoid mismatch with update value)
    /// </summary>
    public enum StructuralChangeReason { Create, Modify, Delete }

    /// <summary>
    /// Single item of a structural change payload
    /// </summary>
    [DisplayName("AasPayloadStructuralChangeItem")]
    public class AasPayloadStructuralChangeItem : IAasPayloadItem, IAasDiaryEntry
    {
        /// <summary>
        /// Reason for the change. According to CRUD principle.
        /// (Retrieve make no sense, update = modify, in order to avoid mismatch with update value)
        /// </summary>
        public StructuralChangeReason Reason;

        /// <summary>
        /// Timestamp of generated (sending) event in UTC time.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Path of the element which was structurally changed. Contains one or more Keys, relative to the 
        /// Observable of the defined Event. 
        /// Is null / empty, if identical to Observable.
        /// </summary>
        public List<Aas.IKey> Path { get; set; }

        /// <summary>
        /// JSON-Serialization of the Submodel, SMC, SME which was denoted by Observabale and Path.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// If the reason is create and the data element to be created is part of an (ordered) collection,
        /// and if >= 0, give the index within the collection, where the data element is to be created
        /// </summary>
        public int CreateAtIndex = -1;

        /// <summary>
        /// Direct reference to IReferable, when change item was successfully processed.
        /// Note: only runtime value; not specified; not interoperable
        /// </summary>
        [JsonIgnore]
        public Aas.IReferable FoundReferable;

        //
        // Constructor
        //

        public AasPayloadStructuralChangeItem(
            DateTime timeStamp,
            StructuralChangeReason reason,
            List<Aas.IKey> path = null,
            int createAtIndex = -1,
            string data = null)
        {
            Timestamp = timeStamp;
            Reason = reason;
            Path = path;
            CreateAtIndex = createAtIndex;
            Data = data;
        }

        //
        // Serialisation
        //

        public override string ToString()
        {
            var res = "PayloadStructuralChangeItem: {Observable}";
            if (Path != null)
                foreach (var k in Path)
                    res += "/" + k.Value;
            res += " -> " + Reason.ToString();
            return res;
        }

#if UseMarkup
        public MiniMarkupBase ToMarkup()
        {
            var left = "  MsgUpdateValueItem: {Observable}";
            if (Path != null)
                foreach (var k in Path)
                    left += "/" + k.Value;

            var right = "";
            right += " -> " + Reason.ToString();
            if (CreateAtIndex >= 0)
                right += $" (CreateAtIndex = {CreateAtIndex})";
            right += "  ";

            var mmData = new MiniMarkupRun("");
            if (Data != null && Data.Length > 0)
                mmData = new MiniMarkupLink("[>>]", "http://127.0.0.1/" + Path?.ToString(), this);

            return new MiniMarkupLine(
                new MiniMarkupRun(left, isMonospaced: true, padsize: 80),
                new MiniMarkupRun(right), mmData);
        }
#endif

        public Aas.IReferable GetDataAsReferable()
        {
            // access
            if (Data == null)
                return null;

            // try deserialize
            return AdminShellSerializationHelper.DeserializeFromJSON<Aas.IReferable>(Data);
        }

        public string GetDetailsText()
        {
            return "" + Data;
        }
    }

    /// <summary>
    /// This event payload transports information, if structural elements of the AAS were created, modified or
    /// deleted (CRUD). (Retrieve make no sense, update = modify, in order to avoid mismatch with update value)
    /// </summary>
    [DisplayName("AasPayloadStructuralChange")]
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

        public AasPayloadStructuralChange(AasPayloadStructuralChange other)
        {
            if (other.Changes != null)
                Changes.AddRange(other.Changes);
        }

        //
        // Serialisation
        //

        public override string ToString()
        {
            var res = base.ToString();
            if (Changes != null)
                foreach (var chg in Changes)
                    res += System.Environment.NewLine + chg.ToString();
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
