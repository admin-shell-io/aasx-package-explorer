using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AasxPredefinedConcepts
{
    public class AasxDefinitionBase
    {
        public class LibraryEntry
        {
            public string name = "";
            public string contents = "";

            public LibraryEntry() { }
            public LibraryEntry(string name, string contents)
            {
                this.name = name;
                this.contents = contents;
            }
        }

        protected Dictionary<string, LibraryEntry> theLibrary = new Dictionary<string, LibraryEntry>();

        protected Dictionary<string, LibraryEntry> BuildLibrary(Assembly assembly, string resourceName)
        {
            // empty result
            var res = new Dictionary<string, LibraryEntry>();

            // access resource 
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return res;

            // read text
            TextReader tr = new StreamReader(stream);
            var jsonStr = tr.ReadToEnd();
            stream.Close();

            // Parse into root
            var root = JObject.Parse(jsonStr);
            if (root == null)
                return res;

            // decompose
            foreach (var child in root.Children())
            {
                // just look for 1. level properties
                var prop = child as JProperty;
                if (prop == null)
                    continue;

                // ok
                var name = prop.Name;
                var contents = prop.Value?.ToString();

                // populate
                try
                {
                    res.Add(name, new LibraryEntry(name, contents));
                }
                catch { }
            }

            return res;
        }

        public LibraryEntry RetrieveEntry(string name)
        {
            // simple access
            if (theLibrary == null || name == null || !theLibrary.ContainsKey(name))
                return null;

            // return
            return theLibrary[name];
        }

        public T RetrieveReferable<T>(string name) where T : AdminShell.Referable
        {
            // entry
            var entry = this.RetrieveEntry(name);
            if (entry == null || entry.contents == null)
                return null;

            // try de-serialize
            try
            {
                var r = JsonConvert.DeserializeObject<T>(entry.contents);
                return r;
            }
            catch 
            {
                return null;
            }
        }
    }
}
