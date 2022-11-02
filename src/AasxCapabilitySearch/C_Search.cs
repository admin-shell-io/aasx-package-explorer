using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VDS.RDF;
using VDS.RDF.Storage;
using VDS.RDF.Query;

namespace AasxCapabilitySearch
{
    internal class C_Search
    {
        public string[] connectionsettings;
        public string address;
        public string kbid;
        public string user;
        public string pass;

        public C_Search(){
        }

        public void setConnect()
        {
            this.address = connectionsettings[0];
            this.kbid = connectionsettings[1];
            this.user = connectionsettings[2];
            this.pass = connectionsettings[3];
        }


        public SparqlResultSet Cap_Search(string search)
        {


            SparqlResultSet resultSet = new SparqlResultSet();

            StardogConnector star = new StardogConnector(this.address, this.kbid, this.user, this.pass);

            IGraph graph = new Graph();

            string a = "SELECT ?s ?p ?o \n";
            string b = "WHERE{ ?s ?p ?o. \n";
            string c = "FILTER(REGEX(LCASE(STR(?s)),LCASE(\"" + search + "\"))||REGEX(LCASE(STR(?o)),LCASE(\"" + search + "\")))\n";
            string d = "}";

            object results = star.Query(a + b + c + d, true);

            if (results is SparqlResultSet)
            {
                resultSet = (SparqlResultSet)results;
            }
            return resultSet;
        }

        public SparqlResultSet reasoning(string iri)
        {

            SparqlResultSet set = new SparqlResultSet();

            StardogConnector star = new StardogConnector(this.address, this.kbid, this.user, this.pass);

            string e = "SELECT ?s ?p ?o \n";
            string f = "WHERE{ ?s ?p ?o. \n";
            string g = "?s rdfs:subClassOf <" + iri + ">";
            string h = "}";

            object temp = star.Query(e + f + g + h, true);

            if (temp is SparqlResultSet)
            {
                set = (SparqlResultSet)temp;
            }

            return set;
        }

        //Method to check if strings match
        public static bool Text_Search(string text, string word)
        {
            bool b = text.ToLower().Contains(word.ToLower());
            return b;
        }

    }
}
