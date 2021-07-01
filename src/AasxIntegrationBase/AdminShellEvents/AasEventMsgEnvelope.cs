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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.MiniMarkup;
using AdminShellNS;

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// Outer class for any AAS event. AAS events shall interoperably exchage asynchronous information between
    /// different execution environments "operating" AASes. This basic event class is described in AASiD Part 1.
    /// 
    /// Note: This envelope is able to carry one or multiple even payloads.
    /// </summary>
    [DisplayName("AasEventMsgEnvelope")]
    public class AasEventMsgEnvelope
    {
        /// <summary>
        /// Reference to the source EventElement, including identification of  AAS,  Submodel, SubmodelElements.
        /// </summary>
        public AdminShell.Reference Source { get; set; }

        /// <summary>
        /// SemanticId  of  the  source  EventElement,  if available.
        /// </summary>
        public AdminShell.SemanticId SourceSemanticId { get; set; }

        /// <summary>
        /// Reference  to  the  Referable,  which  defines  the scope  of  the  event.  Can  be  AAS,  Submodel, 
        /// SubmodelElementCollection  or SubmodelElement. 
        /// </summary>
        public AdminShell.Reference ObservableReference { get; set; }

        /// <summary>
        /// SemanticId  of  the  Referable,  which  defines  the scope of the event, if available. 
        /// </summary>
        public AdminShell.SemanticId ObservableSemanticId { get; set; }

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
        public List<AasPayloadBase> Payloads = new List<AasPayloadBase>();

        //
        // Display
        //

        public static string TimeToString(DateTime dt)
        {
            return dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }

        // see: https://stackoverflow.com/questions/1820915/how-can-i-format-datetime-to-web-utc-format
        public string DisplayTimestamp
        {
            get
            {
                return TimeToString(Timestamp.ToUniversalTime());
            }
        }

        public string DisplaySource { get { return "" + Source?.Keys?.MostSignificantInfo(); } }

        public string DisplaySourceSemantic { get { return "" + SourceSemanticId?.GetAsExactlyOneKey()?.value; } }

        public string DisplayObservable { get { return "" + ObservableReference?.Keys?.MostSignificantInfo(); } }

        public string DisplayInfo
        {
            get
            {
                var res = "";
                if (Payloads != null)
                {
                    res += $"({Payloads.Count})";
                    if (Payloads.Count > 0)
                        res += " " + Payloads[0].GetType();
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
            AdminShell.Reference source = null,
            AdminShell.SemanticId sourceSemanticId = null,
            AdminShell.Reference observableReference = null,
            AdminShell.SemanticId observableSemanticId = null,
            string topic = null,
            string subject = null,
            AasPayloadBase payload = null,
            List<AasPayloadBase> payloads = null)
        {
            Timestamp = timestamp;
            Source = source;
            SourceSemanticId = sourceSemanticId;
            ObservableReference = observableReference;
            ObservableSemanticId = observableSemanticId;
            Topic = topic;
            Subject = subject;
            if (payload != null)
                Payloads.Add(payload);
            if (payloads != null)
                Payloads.AddRange(payloads);
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

            if (Payloads != null)
                foreach (var pl in Payloads)
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
                        new MiniMarkupRun(Source?.ToString())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("SourceSemantic:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(SourceSemanticId?.ToString())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("ObservableReference:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(ObservableReference?.ToString())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("ObservableSemanticId:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(ObservableSemanticId?.ToString())),
                    new MiniMarkupLine(
                        new MiniMarkupRun("Topic:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(Topic)),
                    new MiniMarkupLine(
                        new MiniMarkupRun("Subject:", isMonospaced: true, padsize: w1),
                        new MiniMarkupRun(Subject))
                    );

            var sum = new List<MiniMarkupBase>();
            if (Payloads != null)
                foreach (var pl in Payloads)
                    sum.Add(pl.ToMarkup());
            res.Children.AddRange(sum);

            return res;
        }
#endif

        //
        // Payloads
        //

        /// <summary>
        /// Get Payloads of a specific type.
        /// </summary>
        public IEnumerable<T> GetPayloads<T>() where T : AasPayloadBase
        {
            if (Payloads == null)
                yield break;
            foreach (var pl in Payloads)
                if (pl is T)
                    yield return pl as T;
        }
    }
}
