using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxFormatCst
{
    public class CstRootBase
    {
        public void WriteToFile(string fn)
        {
            var srl = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            using (var sw = new StreamWriter(fn))
            {
                srl.Serialize(sw, this);
            }
        }
    }
}
