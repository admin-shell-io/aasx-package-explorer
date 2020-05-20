using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPackageExplorer
{
    public class AasxFileRepository
    {
        public class FileMap
        {
            public string assetId = "";
            public string description = "";
            public string tag = "";
            public string code = "";
            public string fn = "";
            public object link = null;
        }

        public List<FileMap> filemaps = new List<FileMap>();

        public static AasxFileRepository Load(string fn)
        {
            // from file
            if (!File.Exists(fn))
                return null;
            var init = File.ReadAllText(fn);
            var repo = JsonConvert.DeserializeObject<AasxFileRepository>(init);
            return repo;
        }

        public void SaveAs(string fn)
        {
            using (var s = new StreamWriter(fn))
            {
                // var settings = new JsonSerializerSettings();
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                s.WriteLine(json);
            }
        }

        public static bool GenerateRepositoryFromFileNames(string[] inputFns, string outputFn)
        {
            var res = true;

            // new repo
            var repo = new AasxFileRepository();

            // make records
            foreach (var ifn in inputFns)
            {
                // get one or multiple asset ids
                var assetIds = new List<string>();
                try
                {
                    var pkg = new AdminShellPackageEnv();
                    pkg.Load(ifn);
                    if (pkg.AasEnv != null && pkg.AasEnv.Assets != null)
                        foreach (var asset in pkg.AasEnv.Assets)
                            if (asset.identification != null)
                                assetIds.Add(asset.identification.id);
                }
                catch
                {
                    res = false;
                }

                // make the record(s)
                foreach (var assetid in assetIds)
                {
                    var fmi = new AasxFileRepository.FileMap();
                    fmi.fn = ifn;
                    fmi.code = "DMC";
                    fmi.assetId = assetid;
                    fmi.description = "TODO";
                    fmi.tag = "TODO";

                    // add it
                    repo.filemaps.Add(fmi);
                }
            }

            // save
            using (var s = new StreamWriter(outputFn))
            {
                var settings = new JsonSerializerSettings();
                var json = JsonConvert.SerializeObject(repo, Formatting.Indented);
                s.WriteLine(json);
            }

            return res;
        }
    }
}
