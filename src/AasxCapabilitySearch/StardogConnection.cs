using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AasxIntegrationBase;
using AasxPackageLogic;
using Newtonsoft.Json;


using VDS.RDF.Storage;

namespace AasxCapabilitySearch
{
    public class StardogConnection
    {
        public string[] Presets;
        public string Connection { get; set; }

        public static StardogConnection GetStardogConnection(string[] connectionsettings)
        {
            string address = connectionsettings[0];
            string kbid = connectionsettings[1];
            string user = connectionsettings[2];
            string pass = connectionsettings[3];

            StardogConnector stardog = new StardogConnector(address, kbid, user, pass);
            StardogConnection star = new StardogConnection();
            try
            {
                star.Connection = CheckConnection(stardog);
            }
            catch(Exception e)
            {
                Log.Singleton.Info(e.ToString());
            }
            

            return star;
        }

        public static string CheckConnection(StardogConnector stardog)
        {
            string s;

            if (stardog.IsReady)
            {
                s = "Connected to Stardog.";
            }
            else
            {
                s = "Connection to Stardog failed.";
            }

            return s;
        }

        public string[] LoadPresets(Newtonsoft.Json.Linq.JToken jtoken)
        {
            string[] result = { "", "", "", "" };
            // access
            if (jtoken == null)
                return result;

            try
            {
                string ad = jtoken[0].SelectToken("ServerAddress.Value").ToString();
                string kb = jtoken[1].SelectToken("KnowledgeBaseID.Value").ToString();
                string us = jtoken[2].SelectToken("Username.Value").ToString();
                string pa = jtoken[3].SelectToken("Password.Value").ToString();

                string[] pl = { ad, kb, us, pa };

                if (pl != null)
                    result = pl;
                
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When loading Stardog Connect Presets from Options");
            }
            return result;
        }
    }
}
