/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AasxPluginSmdExporter
{
    public class RelationshipElement
    {
        #region properties

        public String ValueFirst { get; set; }

        public string FirstAssetId { get; set; }

        public string FirstInterface { get; set; }

        public String ValueSecond { get; set; }

        public string SecondAssetId { get; set; }

        public string SecondInterface { get; set; }

        public String IdShort { get; set; }


        public bool IsCertainConnection { get; set; }



        #endregion

        public RelationshipElement()
        {
            IsCertainConnection = true;
        }

        public RelationshipElement(string valueFirst, string valueSecond, string idShort, bool isCertainConnection)
        {
            ValueFirst = valueFirst.Trim();
            ValueSecond = valueSecond.Trim();
            IdShort = idShort.Trim();
            IsCertainConnection = isCertainConnection;
        }

        /// <summary>
        /// Sets the values according to the asset ids
        /// </summary>
        public void SetValuesAccordingToAsset()
        {
            if (this.FirstAssetId != null && this.FirstAssetId != "")
                this.ValueFirst = getValuesAccordingToAssetHelper(this.FirstAssetId, this.ValueFirst);
            if (this.SecondAssetId != null && this.SecondAssetId != "")
            {
                this.ValueSecond = getValuesAccordingToAssetHelper(this.SecondAssetId, this.ValueSecond);
            }

        }

        /// <summary>
        /// Gets the right value for the assetId
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string getValuesAccordingToAssetHelper(string assetId, string value)
        {
            try
            {
                assetId = WebUtility.UrlEncode(assetId);
                var path = $"assets/@qs?qs=IRI,{assetId}";

                JArray jObject = AASRestClient.GetJArray(path).Result;

                return (string)jObject[0]["idShort"];
            }
            catch (Exception)
            {
                // Could not get asset with assetId
                return value;
            }

        }



        /// <summary>
        /// Parses the JToken into a RelationshipElement and returns the new created RelationshipElement
        /// </summary>
        /// <param name="valueFirst"></param>
        /// <param name="valueSecond"></param>
        /// <param name="idShort"></param>
        /// <param name="ReferenceToAsset"></param>
        /// <param name="interfaceFirst"></param>
        /// <param name="interfaceSecond"></param>
        /// <returns></returns>
        public static RelationshipElement Parse(string valueFirst,
            string valueSecond,
            string idShort,
            Dictionary<string, string> ReferenceToAsset,
            string interfaceFirst = null,
            string interfaceSecond = null)
        {


            RelationshipElement relationship = new RelationshipElement();


            relationship.ValueFirst = valueFirst;
            relationship.ValueSecond = valueSecond;

            if (ReferenceToAsset.ContainsKey(relationship.ValueFirst))
                relationship.FirstAssetId = ReferenceToAsset[relationship.ValueFirst];
            if (ReferenceToAsset.ContainsKey(relationship.ValueSecond))
                relationship.SecondAssetId = ReferenceToAsset[relationship.ValueSecond];



            relationship.SetValuesAccordingToAsset();

            relationship.IdShort = idShort;

            relationship.FirstInterface = interfaceFirst;
            relationship.SecondInterface = interfaceSecond;

            return relationship;

        }

        public static RelationshipElement Parse(JToken statement)
        {
            RelationshipElement relationshipElement = new RelationshipElement();
            relationshipElement.IsCertainConnection = true;
            JToken locTokenFirst = statement.SelectToken("first.keys(0).value");
            if (locTokenFirst != null)
            {
                relationshipElement.FirstAssetId = locTokenFirst.ToString();
            }
            JToken locTokenSecond = statement.SelectToken("second.keys(0).value");
            if (locTokenSecond != null)
            {
                relationshipElement.SecondAssetId = locTokenSecond.ToString();
            }
            relationshipElement.SetValuesAccordingToAsset();
            relationshipElement.FirstInterface = relationshipElement.ValueFirst +
                ((string)statement.SelectToken("first.keys(2).value"));
            relationshipElement.SecondInterface = relationshipElement.ValueSecond +
                ((string)statement.SelectToken("second.keys(2).value"));
            relationshipElement.IdShort = (string)statement.SelectToken("idShort");

            if (relationshipElement.ValueFirst == null || relationshipElement.ValueSecond == null)
            {
                return null;
            }

            return relationshipElement;
        }
    }
}
