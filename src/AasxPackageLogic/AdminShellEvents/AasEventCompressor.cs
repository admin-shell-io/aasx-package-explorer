/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Annotations;
using static System.Windows.Forms.AxHost;
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

#if __old
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
            {
                res.AddRange(_events);
                _events.Clear();
                return res;
            }

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
#endif

        /// <summary>
        /// The stream of events to be compressed will be mapped to a stream of trace links.
        /// First, 1:1. Second, these trace links will try to "join".
        /// </summary>
        protected class TraceLinkBase
        {
            public AasEventMsgEnvelope MsgEnv;

            /// <summary>
            /// Try to join contents with next element. If possible, take over
            /// the (newer) data from <c>next</c>. In this case, return <c>true</c>.
            /// On <c>true</c>, the next element will be deleted by the caller 
            /// and <c>this</c> will remain.
            /// </summary>
            public virtual bool TryJoinWith(TraceLinkBase next)
            {
                return false;
            }

            public virtual AasEventMsgEnvelope GetResult()
            {
                return MsgEnv;
            }
        }

        /// <summary>
        /// Various different payloads found; not to be handled anymore.
        /// </summary>
        protected class TraceLinkOther : TraceLinkBase
        {
        }

        /// <summary>
        /// Exactly one structure changed.
        /// Could be join with another structure change of same path.
        /// </summary>
        protected class TraceLinkOneStructChange : TraceLinkBase
        {
            public AasPayloadStructuralChangeItem OneStructChange;
            public List<Aas.IKey> Path;

            public override bool TryJoinWith(TraceLinkBase next)
            {
                // ok?
                if (next == null || !(next is TraceLinkOneStructChange nsc)
                    || this.OneStructChange == null || nsc.OneStructChange == null
                    || this.OneStructChange.Reason != StructuralChangeReason.Modify
                    || nsc.OneStructChange.Reason != StructuralChangeReason.Modify
                    || this.Path == null || this.Path.Count < 1
                    || nsc.Path == null || nsc.Path.Count < 1)
                    // first stage NO
                    return false;

                // now, we have similar changes, but just checking if the pathes are equal
                // will not work, as the e.g. id/ idShort is also subject of change in these changes.
                // Therefore, from the older event (this), we will construct a path INCLUDE the contained
                // changes and will match these pathes with each other. This should cover both cases
                // (id/ idShort modified / not modified)

                // get the path updated
                var rf = this.OneStructChange.GetDataAsReferable();
                rf.Parent = null;
                Aas.IKey currKey = rf.GetReference()?.Keys.Last();
                var updatedPath = this.OneStructChange.Path.ReplaceLastKey(new List<Aas.IKey>() { currKey });

                // now compare
                if (!updatedPath.Matches(nsc.Path, MatchMode.Relaxed))
                    // second stage NO
                    return false;

                // take over next
                this.OneStructChange.Data = "" + nsc.OneStructChange.Data;
                this.OneStructChange.Timestamp = nsc.OneStructChange.Timestamp;

                // take over the updated path
                this.OneStructChange.Path = updatedPath;

                // ok!
                return true;
            }
        }

        /// <summary>
        /// Exactly one structure changed.
        /// Could be accummulated with another structure change of same path.
        /// </summary>
        protected class TraceLinkOneValueUpdate : TraceLinkBase
        {
            public AasPayloadUpdateValueItem OneValueUpdate;
            public List<Aas.IKey> Path;

            public override bool TryJoinWith(TraceLinkBase next)
            {
                // ok?
                if (next == null || !(next is TraceLinkOneValueUpdate nvu)
                    || this.OneValueUpdate == null || nvu.OneValueUpdate == null
                    || this.Path == null || this.Path.Count < 1
                    || nvu.Path == null || nvu.Path.Count < 1
                    || !this.Path.Matches(nvu.Path, MatchMode.Relaxed))
                    return false;

                // take over next
                this.OneValueUpdate.Value = nvu.OneValueUpdate.Value;
                this.OneValueUpdate.ValueId = nvu.OneValueUpdate.ValueId;

                // ok!
                return true;
            }
        }

        protected TraceLinkBase MapMsgEnvelope(AasEventMsgEnvelope ev)
        {
            // access
            if (ev?.PayloadItems == null || ev?.PayloadItems.Count < 1)
                return new TraceLinkBase() { MsgEnv = ev };

            // one structural change?
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

                // whole path
                var currentPath = evplsc.Changes[0].Path;
                // dead-csharp off
                // evplsc.Changes[0].Path.ReplaceLastKey(new List<Aas.IKey>() { currKey });
                // dead-csharp on
                // create a mapping
                return new TraceLinkOneStructChange()
                {
                    MsgEnv = ev,
                    OneStructChange = evplsc.Changes[0],
                    Path = currentPath
                };
            }

            // one value update?
            if (ev.SourceSemanticId.Matches(AasxPredefinedConcepts.AasEvents.Static.CD_UpdateValueOutwards.GetReference(),
                    MatchMode.Relaxed)
                // a in special format
                && ev.PayloadItems != null && ev.PayloadItems.Count == 1
                && ev.PayloadItems[0] is AasPayloadUpdateValue evpluv
                && evpluv.Values != null && evpluv.Values.Count == 1)
            {
                // update value with exactly 1 modification 

                // whole path
                var currentPath = evpluv.Values[0].Path;

                // create a mapping
                return new TraceLinkOneValueUpdate()
                {
                    MsgEnv = ev,
                    OneValueUpdate = evpluv.Values[0],
                    Path = currentPath
                };
            }

            // not found
            return new TraceLinkBase() { MsgEnv = ev };
        }

        public List<AasEventMsgEnvelope> Flush()
        {
            // result
            var res = new List<AasEventMsgEnvelope>();
            if (_events.Count < 2)
            {
                res.AddRange(_events);
                _events.Clear();
                return res;
            }

            // ok, initial map, consume events
            var traceLinks = _events.Select((ev) => MapMsgEnvelope(ev)).ToList();
            _events.Clear();

            // try to do the joining
            int i = 0;
            while (i < traceLinks.Count - 1)
            {
                // test
                var join = traceLinks[i].TryJoinWith(traceLinks[i + 1]);
                if (!join)
                {
                    i++;
                    continue;
                }

                // ok, [i] already joined .. ignore next .. keep index
                traceLinks.RemoveAt(i + 1);
            }

            // reconstruct
            res = traceLinks.Select((tl) => tl.GetResult()).ToList();
            return res;
        }

    }
}
