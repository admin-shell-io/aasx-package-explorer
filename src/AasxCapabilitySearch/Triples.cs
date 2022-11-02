using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VDS.RDF;
using VDS.RDF.Query;

namespace AasxCapabilitySearch
{
    public class Triples
    {
        public string Name { get; set; }

        public string IRI { get; set; }

        public string Description { get; set; }

        public string[] connectionsettings;

        public SparqlResultSet GetTriples(string search)
        {
            C_Search c_Search = new C_Search();
            c_Search.connectionsettings = connectionsettings;
            c_Search.setConnect();

            SparqlResultSet triples = new SparqlResultSet();

            triples = c_Search.Cap_Search(search);

            return triples;
        }
    }
}
