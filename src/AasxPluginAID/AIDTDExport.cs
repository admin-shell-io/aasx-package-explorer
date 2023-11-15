using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;
using Extensions;
using AasCore.Aas3_0;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AasxPluginAID
{
    class AIDTDExport
    {
        public static JObject serialize_ds(Aas.ISubmodelElementCollection dsc)
        {
            JObject dsJO = new JObject();
            string[] stringidShorts = { "type", "title", "description" , "contentMediaType", "const",
                                  "default", "unit"};
            string[] unsignedIdShorts = { "minimum", "maximum" };
            string[] floatIdShorts = { "minItems", "maxItems", "minLength", "maxLength" };

            foreach (var dsce in dsc.Value)
            {
                if (stringidShorts.Contains(dsce.IdShort)) { dsJO[dsce.IdShort] = dsce.ValueAsText(); }
                if (unsignedIdShorts.Contains(dsce.IdShort)) { dsJO[dsce.IdShort] = dsce.ValueAsText(); }
                if (floatIdShorts.Contains(dsce.IdShort)) { dsJO[dsce.IdShort] = dsce.ValueAsText(); }
                if (dsce.IdShort == "enum")
                {
                    Aas.ISubmodelElementCollection _enum = dsce as Aas.SubmodelElementCollection;
                    List<String> enumsList = new List<String>();
                    foreach (var enume in _enum.Value)
                    {
                        enumsList.Add(enume.ValueAsText());
                    }
                    dsJO["enum"] = JToken.FromObject(_enum);
                }
                else if (dsce.IdShort == "items")
                {
                    Aas.ISubmodelElementCollection itemsSC = dsce as Aas.SubmodelElementCollection;
                    if (itemsSC.Value.Count == 1)
                    {
                        List<JObject> itemsList = new List<JObject>();
                        foreach (var item in itemsSC.Value)
                        {
                            Aas.ISubmodelElementCollection _item = item as Aas.SubmodelElementCollection;
                            itemsList.Add(serialize_ds(_item));
                        }
                        dsJO["items"] = JToken.FromObject(itemsList);
                    }
                    else
                    {
                        Aas.ISubmodelElementCollection _item = itemsSC.Value[0] as Aas.SubmodelElementCollection;
                        dsJO["items"] = JToken.FromObject(serialize_ds(_item));
                    }

                }

            }
            return dsJO;
        }
        public static JObject serialize_io(Aas.ISubmodelElementCollection dsc)
        {
            JObject ioJB = new JObject();
            string[] stringidShorts = { "title", "description" };
            foreach (var dsce in dsc.Value)
            {
                if (stringidShorts.Contains(dsce.IdShort)) { ioJB[dsce.IdShort] = dsce.ValueAsText(); }
                if (dsce.IdShort == "forms")
                {
                    Aas.ISubmodelElementCollection forms = dsce as Aas.SubmodelElementCollection;
                    List<JObject> formsJD = new List<JObject>();
                    JObject formJD = new JObject();
                    foreach (var fe in forms.Value)
                    {
                        if (fe.IdShort == "href") { formJD["href"] = fe.ValueAsText(); }
                        else if (fe.IdShort == "contentType") { formJD["contentType"] = fe.ValueAsText(); }
                        else if (fe.IdShort == "subprotocol") { formJD["subprotocol"] = fe.ValueAsText(); }
                        else if (fe.IdShort.Split(":")[0] == "htv")
                            {
                                if (fe.IdShort.Split(":")[0] == "htv:headers")
                                {
                                    Aas.ISubmodelElementCollection htv_headerSC = fe as Aas.SubmodelElementCollection;
                                    foreach (var headerElem in htv_headerSC.Value)
                                    {
                                        if (headerElem.IdShort == "htv:fieldName") { formJD["htv:fieldName"] = fe.ValueAsText(); }
                                        else if (headerElem.IdShort == "htv:fieldValue") { formJD["htv:fieldValue"] = fe.ValueAsText(); }
                                    }
                                }
                                else { formJD[fe.IdShort] = fe.ValueAsText(); }
                            }
                        else if (fe.IdShort.Split(":")[0] == "modbus") { formJD[fe.IdShort] = fe.ValueAsText(); }
                        else if (fe.IdShort.Split(":")[0] == "mqv") { formJD[fe.IdShort] = fe.ValueAsText(); }
                    }
                    formsJD.Add(formJD);
                    ioJB["forms"] = JToken.FromObject(formsJD);

                }
            }
            return ioJB;
        }
        public static JObject serialize_p(Aas.ISubmodelElementCollection posc)
        {
            JObject propertyJD = serialize_io(posc);
            propertyJD.Merge(serialize_ds(posc));

            foreach (var pe in posc.Value)
            {
                if (pe.IdShort == "observable")
                {
                    propertyJD[pe.IdShort] = pe.ValueAsText();
                }
            }
            return propertyJD;
        }
        public static JObject EndpointMetadata(Aas.ISubmodelElement enm,JObject TDJson)
        {
            Aas.ISubmodelElementCollection epm = enm as Aas.SubmodelElementCollection;
            
            foreach (var _epm in epm.Value){                
                if (_epm.IdShort == "base")
                    {
                    TDJson["base"] = _epm.ValueAsText();
                    }
                else if (_epm.IdShort == "contentType")
                    {
                    TDJson["contentType"] = _epm.ValueAsText();
                    }
                else if (_epm.IdShort == "securityDefinitions")
                    {
                        Aas.ISubmodelElementCollection securityDefinitions = _epm as Aas.SubmodelElementCollection;
                        JObject securityDefinitionsList = new JObject();
                        foreach (var securityDefinition in securityDefinitions.Value)
                        {
                            Aas.ISubmodelElementCollection _securityDefinition = securityDefinition as Aas.SubmodelElementCollection;
                            JObject securityDefinitionJD = new JObject();
                            foreach (var _sde in _securityDefinition.Value)
                            {
                                securityDefinitionJD[_sde.IdShort] = _sde.ValueAsText();
                            }
                            securityDefinitionsList[_securityDefinition.IdShort] = securityDefinitionJD;
                        }
                    TDJson["securityDefinitions"] = JToken.FromObject(securityDefinitionsList);
                    }
            }
            return TDJson;
        }
        public static JObject ExportInterfacetoTDJson(Aas.ISubmodelElementCollection smc)
        {
            JObject TDJson = new JObject();
            TDJson["@context"] = "https://www.w3.org/2019/wot/td/v1";
            TDJson["@type"] = "Thing";
            try
            {
                foreach (var sme in smc.Value)
                {
                    if (sme.IdShort == "EndpointMetadata")
                    {
                        TDJson = EndpointMetadata(sme, TDJson);
                    }
                    else if (sme.IdShort == "InterfaceMetaData")
                    {
                        Aas.ISubmodelElementCollection imd = sme as Aas.SubmodelElementCollection;
                        JObject imdJson = new JObject();
                        foreach (var imde in imd.Value)
                        {
                            if (imde.IdShort == "properties")
                            {
                                Aas.ISubmodelElementCollection properties = imde as Aas.SubmodelElementCollection;
                                JObject propertiesJO = new JObject();
                                foreach (var _property in properties.Value)
                                {
                                    JObject _propetyJO = new JObject();
                                    Aas.ISubmodelElementCollection _propertysmc = _property as Aas.SubmodelElementCollection;
                                    propertiesJO[_propertysmc.IdShort] = serialize_p(_propertysmc);
                                }
                                TDJson["properties"] = propertiesJO;
                            }
                            else
                            {
                                TDJson[imde.IdShort] = imde.ValueAsText();
                            }
                        }
                    }
                    else
                    {
                        TDJson[sme.IdShort] = sme.ValueAsText();
                    }
                    
                }
            }
            catch (Exception ex)
            {

            }
            return TDJson;
        }
    }
}
