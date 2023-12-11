/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase.AdminShellEvents
{
    /// <summary>
    /// Implements a minimal stack for
    /// </summary>
    public class AasEventMsgStack
    {
        private List<AasEventMsgEnvelope> _stack = new List<AasEventMsgEnvelope>();

        public void PushEvent(AasEventMsgEnvelope ev)
        {
            if (ev == null || this._stack == null)
                return;
            lock (this._stack)
            {
                this._stack.Add(ev);
            }
        }

        public AasEventMsgEnvelope PopEvent()
        {
            // result
            AasEventMsgEnvelope ev = null;

            // get?
            lock (this._stack)
            {
                if (this._stack.Count > 0)
                {
                    ev = this._stack[0];
                    this._stack.RemoveAt(0);
                }
            }

            // return if found or not ..
            return ev;
        }

        public int Count()
        {
            return _stack.Count;
        }

        public void Clear()
        {
            _stack.Clear();
        }

        public AasEventMsgEnvelope this[int i]
        {
            get
            {
                if (i < 0 || i >= _stack.Count)
                    return null;
                return _stack[i];
            }
        }

        public IEnumerable<AasEventMsgEnvelope> All()
        {
            foreach (var msg in _stack)
                yield return msg;
        }

        public IEnumerable<Tuple<AasEventMsgEnvelope, AasPayloadUpdateValueItem>> AllValueItems()
        {
            foreach (var ev in _stack)
                if (ev.PayloadItems != null)
                    foreach (var pl in ev.PayloadItems)
                        if (pl is AasPayloadUpdateValue uv && uv.Values != null)
                            foreach (var uvi in uv.Values)
                                if (uvi != null)
                                    yield return new Tuple<AasEventMsgEnvelope, AasPayloadUpdateValueItem>(ev, uvi);
        }
    }
}
