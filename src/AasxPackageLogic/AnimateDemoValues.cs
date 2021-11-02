/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;
using AasxIntegrationBase;
using System.Globalization;
using AasxIntegrationBase.AdminShellEvents;

namespace AasxPackageLogic
{
    public class AnimateDemoValues
    {
        public class AnimateArgs
        {
            public enum TypeDef { None, Sin, Cos, Saw, Square }

            /// <summary>
            /// Type of the mapping function.
            /// </summary>
            public TypeDef type;

            /// <summary>
            /// Frequency. Multiplier to the input of the mapping function. Normalized frequency is 1.0 seconds.
            /// </summary>
            public double freq = 0.1;

            /// <summary>
            /// Scale. Multiplier to the output of the mapping function. Default is +/- 1.0.
            /// </summary>
            public double scale = 1.0;

            /// <summary>
            /// Offset to the scaled output of the mapping function. Default is 0.0.
            /// </summary>
            public double ofs = 0.0;

            /// <summary>
            /// Specifies the timer interval in milli-seconds. Minimum value 100ms.
            /// Applicable on: Submodel
            /// </summary>
            public int timer = 1000;
        }

        public class AnimateState
        {
            public DateTime LastTime;
            public double Phase;
        }

        protected Dictionary<AdminShell.Referable, AnimateState> _states = 
            new Dictionary<AdminShell.Referable, AnimateState>();

        public void Clear()
        {
            _states.Clear();
        }

        public AnimateState GetState(
            AdminShell.Referable rf,
            bool createIfNeeded = false)
        {
            if (rf == null)
                return null;
            if (_states.ContainsKey(rf))
                return _states[rf];
            if (createIfNeeded)
            {
                var nas = new AnimateState();
                _states.Add(rf, nas);
                return nas;
            }
            return null;
        }

        public static AnimateArgs Parse(string json)
        {
            if (!json.HasContent())
                return null;

            try
            {
                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<AnimateArgs>(json);
                return res;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        public void Animate(
            AdminShell.Property prop,
            Action<AdminShell.Property, AdminShell.IAasDiaryEntry> emitEvent)
        {
            // prop needs to exists and have qualifiers
            var q = prop.HasQualifierOfType("Animate.Args");
            var args = Parse(q.value);
            if (args == null)
                return;

            // get state
            var state = GetState(prop, createIfNeeded: true);
            if (state == null)
                return;

            // how long till last time
            var deltaSecs = (DateTime.UtcNow - state.LastTime).TotalSeconds;
            if (deltaSecs < 0.001 * Math.Max(100, args.timer))
                return;

            // save new lastTime, phase
            state.LastTime = DateTime.UtcNow;
            var phase = state.Phase;
            state.Phase = (state.Phase + deltaSecs * args.freq) % 2.0;

            // function?
            double? res = null;
            switch (args.type)
            {
                case AnimateArgs.TypeDef.Saw:
                    res = args.ofs + args.scale * Math.Sin(-1.0 + phase);
                    phase = phase % 2.0;
                    break;

                case AnimateArgs.TypeDef.Sin:
                    res = args.ofs + args.scale * Math.Sin(phase * 2 * Math.PI);
                    break;

                case AnimateArgs.TypeDef.Cos:
                    res = args.ofs + args.scale * Math.Cos(phase * 2 * Math.PI);
                    break;

                case AnimateArgs.TypeDef.Square:
                    res = args.ofs + ((phase < 1.0) ? -1.0 : 1.0);
                    phase = phase % 2.0;
                    break;
            }

            // use result?
            if (res.HasValue)
            {
                // set value
                prop.value = res.Value.ToString(CultureInfo.InvariantCulture);

                // send event
                if (emitEvent != null)
                {
                    // create
                    var evi = new AasPayloadUpdateValueItem(
                        path: (prop as AdminShell.IGetReference)?.GetReference()?.Keys,
                        value: prop.ValueAsText());

                    evi.ValueId = prop.valueId;

                    evi.FoundReferable = prop;

                    // add 
                    AdminShell.DiaryDataDef.AddAndSetTimestamps(prop, evi, isCreate: false);

                    // kick lambda
                    emitEvent.Invoke(prop, evi);
                }
            }
        }

        
        
    }
}
