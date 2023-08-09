/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// to be disabled for AASX Server
#define UseMarkup

using AasxIntegrationBase.MiniMarkup;
using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using static AdminShellNS.AdminShellConverters;
using Aas = AasCore.Aas3_0;

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// Outer class for any AAS event. AAS events shall interoperably exchage asynchronous information between
    /// different execution environments "operating" AASes. This basic event class is described in AASiD Part 1.
    /// 
    /// Note: This envelope is able to carry one or multiple even payloads.
    /// </summary>
    [DisplayName("AasEventMsgEnvelope")]
    public class AasEventMsgEnvelope : IAasPayloadItem
    {
        /// <summary>
        /// Reference to the source EventElement, including identification of  AAS,  Submodel, SubmodelElements.
        /// </summary>
        public Aas.IReference Source { get; set; }

        /// <summary>
        /// SematicId  of  the  source  EventElement,  if available.
        /// </summary>
        public Aas.IReference SourceSemanticId { get; set; }

        /// <summary>
        /// Reference  to  the  Referable,  which  defines  the scope  of  the  event.  Can  be  AAS,  Submodel, 
        /// SubmodelElementCollection  or SubmodelElement. 
        /// </summary>
        public Aas.IReference ObservableReference { get; set; }

        /// <summary>
        /// SemanticId  of  the  Referable,  which  defines  the scope of the event, if available. 
        /// </summary>
        public Aas.IReference ObservableSemanticId { get; set; }

        /// <summary>
        /// Information for the outer message infrastructure for  scheduling the  event to the  respective 
        /// communication channel.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// ABAC-Subject, who/ which initiated the creation
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Timestamp  in  UTC,  when  this  event  was triggered.
        /// Note: this is the C# native implementation. May be, for serialization, another getter/ setter
        /// might be required.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Carries one or more events payloads.
        /// </summary>
        [JsonIgnore]
        public ListOfAasPayloadBase PayloadItems = new ListOfAasPayloadBase();

        /// <summary>
        /// Put raw content in
        /// </summary>
        [JsonIgnore]
        public string PayloadsRaw = null;

        /// <summary>
        /// This getter/ setter dynamically switches between <c>PayloadItems</c> and <c>PayloadsRaw</c>.
        /// </summary>
        public object Payloads
        {
            get { return (PayloadItems != null) ? (object)PayloadItems : PayloadsRaw; }
            set
            {
                if (value is ListOfAasPayloadBase pi)
                {
                    PayloadItems = pi;
                    PayloadsRaw = null;
                }
                else
                {
                    PayloadsRaw = (string)value;
                    PayloadItems = null;
                }
            }
        }

        /// <summary>
        /// Overall check if a valid event.
        /// </summary>
        [JsonIgnore]
        public bool IsWellformed
        {
            get
            {
                return Source != null && Source.Keys.Count > 0
                    && SourceSemanticId != null && SourceSemanticId.Keys.Count > 0
                    && ObservableReference != null && ObservableReference.Keys.Count > 0;
            }
        }

        //
        // Display
        //

        public static string TimeToString(DateTime dt)
        {
            return dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }

        // see: https://stackoverflow.com/questions/1820915/how-can-i-format-datetime-to-web-utc-format
        [JsonIgnore]
        public string DisplayTimestamp
        {
            get
            {
                return TimeToString(Timestamp.ToUniversalTime());
            }
        }

        [JsonIgnore]
        public string DisplaySource { get { return "" + Source?.MostSignificantInfo(); } }

        [JsonIgnore]
        public string DisplaySourceSemantic { get { return "" + SourceSemanticId?.GetAsExactlyOneKey()?.Value; } }

        [JsonIgnore]
        public string DisplayObservable { get { return "" + ObservableReference?.MostSignificantInfo(); } }

        [JsonIgnore]
        public string DisplayInfo
        {
            get
            {
                var res = "";
                if (PayloadItems != null)
                {
                    res += $"({PayloadItems.Count})";
                    if (PayloadItems.Count > 0)
                        res += " " + PayloadItems[0].GetType();
                }
                return res;
            }
        }

        //
        // Constructor
        //

        public AasEventMsgEnvelope() { }

        public AasEventMsgEnvelope(
            DateTime timestamp,
            Aas.IReference source = null,
            Aas.IReference sourceSemanticId = null,
            Aas.IReference observableReference = null,
            Aas.IReference observableSemanticId = null,
            string topic = null,
            string subject = null,
            AasPayloadBase payload = null,
            ListOfAasPayloadBase payloads = null)
        {
            Timestamp = timestamp;
            Source = source;
            SourceSemanticId = sourceSemanticId;
            ObservableReference = observableReference;
            ObservableSemanticId = observableSemanticId;
            Topic = topic;
            Subject = subject;
            if (payload != null)
                PayloadItems.Add(payload);
            if (payloads != null)
                PayloadItems.AddRange(payloads);
        }

        public AasEventMsgEnvelope(AasEventMsgEnvelope other)
        {
            if (other == null)
                return;

            Timestamp = other.Timestamp;
            Source = other.Source;
            SourceSemanticId = other.SourceSemanticId;
            ObservableReference = other.ObservableReference;
            ObservableSemanticId = other.ObservableSemanticId;
            Topic = other.Topic;
            Subject = other.Subject;
            if (other.PayloadItems != null)
                Payloads = new ListOfAasPayloadBase(other.PayloadItems);
            else if (other.PayloadsRaw != null)
                Payloads = other;
        }

        //
        // Serialisation
        //

        public override string ToString()
        {
            var res = $"{this.GetType()}: " +
                $"{"" + Timestamp.ToString(CultureInfo.InvariantCulture)} @ " +
                $"Source={"" + Source?.ToString()}, " +
                $"SourceSemanticId={"" + SourceSemanticId?.ToString()}, " +
                $"ObservableReference={"" + ObservableReference?.ToString()}, " +
                $"ObservableSemanticId={"" + ObservableSemanticId?.ToString()}, " +
                $"Topic=\"{"" + Topic}\", " +
                $"Subject=\"{"" + Subject}\", ";

            if (PayloadItems != null)
                foreach (var pl in PayloadItems)
                    res += pl.ToString();

            return res;
        }

#if UseMarkup
        public MiniMarkupBase ToMarkup()
        {
            int w1 = 30;
            var ts = Timestamp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            var res =
                new MiniMarkupSequence(
                    new MiniMarkupLine(new MiniMarkupRun(
                        $"AAS event message @ {ts}", fontSize: 16.0f)),
                    new MiniMarkupLine(
                        new MiniMarkupRun("Source:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(Source?.ToStringExtended())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("SourceSemantic:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(SourceSemanticId?.ToStringExtended())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("ObservableReference:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(ObservableReference?.ToStringExtended())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("ObservableSemanticId:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(ObservableSemanticId?.ToStringExtended())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("Topic:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(Topic)),
                    new MiniMarkupLine(
                        new MiniMarkupRun("Subject:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(Subject))
                    );

            var sum = new List<MiniMarkupBase>();

            if (PayloadItems != null)
                foreach (var pl in PayloadItems)
                    sum.Add(pl.ToMarkup());

            if (PayloadsRaw != null)
                sum.Add(
                    new MiniMarkupLine(
                        new MiniMarkupRun("Payload:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun("Raw event data present "),
                        new MiniMarkupLink("[>>]", "http://127.0.0.1/", this)));

            res.Children.AddRange(sum);

            return res;
        }
#endif

        public string GetDetailsText()
        {
            if (PayloadItems != null)
                return "" + PayloadItems.ToString();
            return "" + PayloadsRaw;
        }

        //
        // Payloads
        //

        /// <summary>
        /// Get Payloads of a specific type.
        /// </summary>
        public IEnumerable<T> GetPayloads<T>() where T : AasPayloadBase
        {
            if (PayloadItems == null)
                yield break;
            foreach (var pl in PayloadItems)
                if (pl is T)
                    yield return pl as T;
        }
    }
}
