/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Aas = AasCore.Aas3_0;

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

        protected class TraceStateBase
        {
        }

        protected class TraceStateStructuralChangeOneModify : TraceStateBase
        {
            public List<Aas.IKey> CurrentPath;
        }

        protected TraceStateBase FollowTraceState(TraceStateBase stateIn, AasEventMsgEnvelope ev)
        {
            // trivial
            if (ev == null)
                return null;

            // basically a unfold state machine
            if (ev.SourceSemanticId.Matches(AasxPredefinedConcepts.AasEvents.Static.CD_StructureChangeOutwards.GetReference(),
                    MatchMode.Relaxed)
                // a in special format
                && ev.PayloadItems != null && ev.PayloadItems.Count == 1
                && ev.PayloadItems[0] is AasPayloadStructuralChange evplsc
                && evplsc.Changes != null && evplsc.Changes.Count == 1
                && evplsc.Changes[0].Reason == StructuralChangeReason.Modify)
            {
                // structural change with exactly 1 modification 

                // get a current key
                var rf = evplsc.Changes[0].GetDataAsReferable();
                rf.Parent = null;
                Aas.IKey currKey = rf.GetReference()?.Keys.Last();

                // Transition NULL -> structural change
                if (currKey != null && stateIn == null)
                {
                    // start new state
                    var res = new TraceStateStructuralChangeOneModify()
                    {
                        CurrentPath = evplsc.Changes[0].Path.ReplaceLastKey(new List<Aas.IKey>() { currKey })
                    };
                    return res;
                }
                else
                if (currKey != null
                    && stateIn is TraceStateStructuralChangeOneModify stateCurr
                    && evplsc.Changes[0].Path.Matches(stateCurr.CurrentPath, MatchMode.Relaxed))
                {
                    // happy path: continue state
                    stateCurr.CurrentPath = stateCurr.CurrentPath.ReplaceLastKey(new List<Aas.IKey>() { currKey });
                    return stateCurr;
                }
            }

            // ok, no condition met. Return "false"
            return null;
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
                    && e > s)
                {
                    // basic validity given. Waht to do?
                    var traceState = FollowTraceState(null, _events[s]);

                    // valid segment
                    if (traceState is TraceStateStructuralChangeOneModify)
                    {
                        var no = JoinEvents_StructuralChangeOneModify(_events[s], _events[e]);
                        res.Add(no);
                    }
                }
            };

            var segStart = 0;
            var segEnd = 1;
            var currState = FollowTraceState(null, _events[segStart]);
            while (segStart < segEnd && segEnd < _events.Count)
            {
                // investigate current end
                var newState = FollowTraceState(currState, _events[segEnd]);

                if (currState != null && newState != null)
                {
                    // try extend
                    segEnd++;
                    currState = newState;

                    continue;
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (currState != null && newState == null)
                {
                    // ending here
                    lambdaCheckSegment(segStart, segEnd - 1);

                    // re-investigate new state
                    segStart = segEnd;
                    segEnd = segStart + 1;
                    currState = FollowTraceState(null, _events[segStart]);

                    continue;
                }

                // else: restart with end
                segStart = segEnd;
                segEnd = segStart + 1;
                currState = FollowTraceState(null, _events[segStart]);
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
