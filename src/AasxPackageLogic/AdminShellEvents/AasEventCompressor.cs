using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Text;

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// This class maintains a list of events in a stack-like manner. Upon triggering ("flush"), the
    /// events will be "compressed" without change of semantics.
    /// </summary>
    public class AasEventCompressor
    {
        private List<AasEventMsgEnvelope> _events = new List<AasEventMsgEnvelope>();

        public void Push(AasEventMsgEnvelope ev)
        {
            if (ev != null && _events != null)
                _events.Add(ev);
        }

        public bool AreCompressable(AasEventMsgEnvelope a, AasEventMsgEnvelope b)
        {
            if (!a.IsWellInformed || !b.IsWellInformed)
                return false;

            var defs = AasxPredefinedConcepts.AasEvents.Static;
            var relaxed = AdminShell.Key.MatchMode.Relaxed;

            if (!a.Source.Matches(b.Source)
                || !a.SourceSemanticId.Matches(b.SourceSemanticId)
                || !a.ObservableReference.Matches(b.ObservableReference))
                return false;

            // only some allowed shapes of events
            var res = false;
            if (a.SourceSemanticId.Matches(defs.CD_StructureChangeOutwards, relaxed)
                // a in special format
                && a.PayloadItems != null && a.PayloadItems.Count == 1
                && a.PayloadItems[0] is AasPayloadStructuralChange aplsc
                && aplsc.Changes != null && aplsc.Changes.Count == 1
                && aplsc.Changes[0].Reason == StructuralChangeReason.Modify
                // b in special format
                && b.PayloadItems != null && b.PayloadItems.Count == 1
                && b.PayloadItems[0] is AasPayloadStructuralChange bplsc
                && bplsc.Changes != null && bplsc.Changes.Count == 1
                && bplsc.Changes[0].Reason == StructuralChangeReason.Modify
                // pathes have to be the same
                && aplsc.Changes[0].Path != null
                && aplsc.Changes[0].Path.Matches(bplsc.Changes[0].Path))
            {
                // Phew!
                // outer logic must maintain, that the two events are consecutive!
                res = true;
            }

            // seems to be ok
                return res;
        }

        private AasEventMsgEnvelope JoinEvents_StructuralChangeOneModify(
            AasEventMsgEnvelope a, AasEventMsgEnvelope b)
        {
            // based on the old
            var joint = new AasEventMsgEnvelope(a);
            // add last modified contents
            if (joint.PayloadItems != null && joint.PayloadItems.Count == 1
                && joint.PayloadItems[0] is AasPayloadStructuralChange jsc
                && jsc.Changes != null && jsc.Changes.Count == 1
                // also for b
                && b?.PayloadItems != null && b.PayloadItems.Count == 1
                && b.PayloadItems[0] is AasPayloadStructuralChange bsc
                && bsc.Changes != null && bsc.Changes.Count == 1)
            {
                jsc.Changes[0].Data = "" + bsc.Changes[0].Data;
            }
            // and return
            return joint;
        }

        public List<AasEventMsgEnvelope> Flush()
        {
            // result
            var res = new List<AasEventMsgEnvelope>();
            if (_events.Count < 2)
                return res;

            // split into parts of compressable items

            Action<int, int> lambdaCheckSegment = (s, e) =>
            {
                if (s >= 0 && e >= 0
                    && s < _events.Count && e < _events.Count
                    && e > s
                    && AreCompressable(_events[s], _events[e]))
                {
                    // valid segment
                    if (true == _events[s].SourceSemanticId?.Matches(
                            AasxPredefinedConcepts.AasEvents.Static.CD_StructureChangeOutwards,
                            AdminShell.Key.MatchMode.Relaxed))
                    {
                        var no = JoinEvents_StructuralChangeOneModify(_events[s], _events[e]);
                        res.Add(no);
                    }
                }
            };

            var segStart = 0;
            var segEnd = 1;
            while (segStart < segEnd && segEnd < _events.Count)
            {
                if (AreCompressable(_events[segStart], _events[segEnd]))
                {
                    // try extend
                    segEnd++;
                }
                else
                {
                    // check found segment
                    lambdaCheckSegment(segStart, segEnd - 1);

                    // next segment
                    segStart = segEnd;
                    segEnd = segStart + 1;
                }
            }

            // final segment to check?
            lambdaCheckSegment(segStart, segEnd - 1);

            // clear
            _events.Clear();

            // result
            return res;
        }
    }
}
