/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using AdminShellNS.DiaryData;
using Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using Aas = AasCore.Aas3_0;

// ReSharper disable UnassignedField.Global

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
            /// C# string format string to format a double value pretty.
            /// Note: e.g. F4 for 4 floating point precision digits.
            /// Note: D0 for decimal integer
            /// </summary>
            public string fmt;

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

        protected Dictionary<Aas.IReferable, AnimateState> _states =
            new Dictionary<Aas.IReferable, AnimateState>();

        public void Clear()
        {
            _states.Clear();
        }

        public AnimateState GetState(
            Aas.IReferable rf,
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
            Aas.Property prop,
            Action<Aas.Property, IAasDiaryEntry> emitEvent)
        {
            // prop needs to exists and have qualifiers
            var ext = prop.HasExtensionOfName("Animate.Args");
            var args = Parse(ext?.Value);
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
                prop.Value = res.Value.ToString(CultureInfo.InvariantCulture);

                // re-format?
                if (args.fmt.HasContent())
                {
                    try
                    {
                        prop.Value = res.Value.ToString(args.fmt, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        LogInternally.That.CompletelyIgnoredError(ex);
                    }
                }

                // send event
                if (emitEvent != null)
                {
                    // create
                    var evi = new AasPayloadUpdateValueItem(
                        path: (prop)?.GetModelReference()?.Keys,
                        value: prop.ValueAsText());

                    evi.ValueId = prop.ValueId;

                    evi.FoundReferable = prop;

                    // add 
                    DiaryDataDef.AddAndSetTimestamps(prop, evi, isCreate: false);

                    // kick lambda
                    emitEvent.Invoke(prop, evi);
                }
            }
        }



    }
}
