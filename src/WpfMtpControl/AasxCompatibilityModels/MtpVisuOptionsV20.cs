/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global

#if !DoNotUseAasxCompatibilityModels

namespace AasxCompatibilityModels.WpfMtpControl
{
    public class MtpVisuOptionsV20
    {
        public string Background = "#e0e0e0";

        public string StateColorActive = "#0000ff";
        public string StateColorNonActive = "#000000";

        public string StateColorForward = "#0000ff";
        public string StateColorReverse = "#00ff00";
    }
}

#endif