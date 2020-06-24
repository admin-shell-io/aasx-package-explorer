using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable AssignNullToNotNullAttribute .. a bit unclear, why issues here

namespace AasxIntegrationBase
{
    public class AasxPluginOptionsBase
    {
        public virtual void Merge(AasxPluginOptionsBase options)
        {
        }

        public static T LoadDefaultOptionsFromAssemblyDir<T>(
            string pluginName, Assembly assy = null,
            JsonSerializerSettings settings = null) where T : AasxPluginOptionsBase
        {
            // expand assy?
            if (assy == null)
                assy = Assembly.GetExecutingAssembly();
            if (pluginName == null || pluginName == "")
                return null;

            // build fn
            var optfn = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(assy.Location),
                        pluginName + ".options.json");

            if (File.Exists(optfn))
            {
                var optText = File.ReadAllText(optfn);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(optText, settings);
            }

            // no
            return null;
        }

        public void TryLoadAdditionalOptionsFromAssemblyDir<T>(
            string pluginName, Assembly assy = null,
            JsonSerializerSettings settings = null) where T : AasxPluginOptionsBase
        {
            // expand assy?
            if (assy == null)
                assy = Assembly.GetExecutingAssembly();
            if (pluginName == null || pluginName == "")
                return;

            // build dir name
            var baseDir = System.IO.Path.GetDirectoryName(assy.Location);

            // search
            var files = Directory.GetFiles(baseDir, "*.add-options.json");

            // ReSharper disable EmptyGeneralCatchClause
            foreach (var fn in files)
                try
                {
                    var optText = File.ReadAllText(fn);
                    var opts = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(optText, settings);
                    this.Merge(opts);
                }
                catch { }
            // ReSharper enable EmptyGeneralCatchClause
        }
    }
}
