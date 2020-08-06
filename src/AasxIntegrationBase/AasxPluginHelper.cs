using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

            if (File.Exists(fn))
            {
                var licTxt = File.ReadAllText(fn);
                return licTxt;
            }

            // no
            return "";
        }
    }
}
