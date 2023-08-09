/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;

// ReSharper disable ClassNeverInstantiated.Global

namespace AasxIntegrationBase
{
    public class AasxPluginHelper
    {
        public static string LoadLicenseTxtFromAssemblyDir(
            string licFileName = "LICENSE.txt", Assembly assy = null)
        {
            // expand assy?
            if (assy == null)
                assy = Assembly.GetExecutingAssembly();

            // build fn
            var fn = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(assy.Location),
                        licFileName);

            if (System.IO.File.Exists(fn))
            {
                var licTxt = System.IO.File.ReadAllText(fn);
                return licTxt;
            }

            // no
            return "";
        }
    }
}
