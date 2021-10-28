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
            public enum TypeDef { None, Sin, Cos, Saw }
            public TypeDef type;

            public double ofs = 1.0;
            public double scale = 1.0;
            public double freq = 1.0;
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

        public static void Animate(
            AdminShell.Property prop,
            double phaseSecs,
            double deltaSecs,
            Action<AdminShell.Property, AdminShell.IAasDiaryEntry> emitEvent)
        {
            // prop needs to exists and have qualifiers
            var q = prop.HasQualifierOfType("Animate.Args");
            var args = Parse(q.value);
            if (args == null)
                return;

            // function?
            double? res = null;
            switch (args.type)
            {
                case AnimateArgs.TypeDef.Sin:
                    res = args.ofs + args.scale * Math.Sin(phaseSecs + deltaSecs * args.freq);
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

                    // add 
                    AdminShell.DiaryDataDef.AddAndSetTimestamps(prop, evi, isCreate: false);

                    // kick lambda
                    emitEvent.Invoke(prop, evi);
                }
            }
        }
    }
}
