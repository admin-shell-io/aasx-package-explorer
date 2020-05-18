using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using AdminShellNS;

namespace AasxPackageExplorer
{
    public class OpcUaTools
    {
        public static void ImportNodeSetToSubModel(string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            // OPC UA NodeSet
            // Input
            Boolean is_Iri = false;
            String Uri = "";
            Boolean is_UADataType = false;
            Boolean is_UAVariable = false;
            Boolean is_UAObject = false;
            Boolean is_UAMethod = false;
            Boolean is_DisplayName = false;
            Boolean is_Value = false;
            String DisplayName = "";
            // String Field_Name = "";
            // String Field_Value = "";
            // Output
            String Name = "";
            String Value = "";

            AdminShell.SubmodelElementCollection[] propGroup = new AdminShell.SubmodelElementCollection[10];
            int i_propGroup = 0;

            XmlTextReader reader = new XmlTextReader(inputFn);
            StreamWriter sw = File.CreateText(inputFn + ".log.txt");

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        if (reader.Name == AdminShell.Identification.IRI)
                            is_Iri = true;
                        if (reader.Name == "UADataType")
                            is_UADataType = true;
                        if (reader.Name == "UAVariable")
                            is_UAVariable = true;
                        if (reader.Name == "UAObject")
                            is_UAObject = true;
                        if (reader.Name == "UAMethod")
                            is_UAMethod = true;
                        if (reader.Name == "DisplayName")
                            is_DisplayName = true;
                        if (reader.Name == "Value")
                            is_Value = true;
                        if (reader.Name == "Field")
                        {
                            Name = reader.GetAttribute("Name");
                            Value = reader.GetAttribute("Value");
                        }
                        break;
                    case XmlNodeType.Text: //Display the text in each element.
                        if (is_Iri)
                        {
                            Uri = reader.Value;
                        }
                        if (is_DisplayName)
                        {
                            DisplayName = reader.Value;
                        }
                        if (is_UAVariable && is_Value)
                        {
                            if (reader.Value.Length < 100)
                            {
                                Value = reader.Value;
                            }
                            else
                            {
                                Value = reader.Value.Substring(0, 100) + " ..";
                            }
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        if (is_Iri)
                        {
                            Name = "NamespaceURI";
                            Value = Uri;
                            is_Iri = false;
                            Uri = "";
                        }
                        if (is_UADataType && is_DisplayName)
                        {
                            propGroup[i_propGroup] = AdminShell.SubmodelElementCollection.CreateNew("UADataType " + DisplayName);
                            sm.Add(propGroup[i_propGroup]);
                            i_propGroup++;
                            is_UADataType = false;
                            is_DisplayName = false;
                            DisplayName = "";
                        }
                        if (reader.Name == "UADataType")
                            i_propGroup--;
                        if (reader.Name == "UAVariable")
                        {
                            Name = "UAVariable " + DisplayName;
                            DisplayName = "";
                            is_DisplayName = false;
                            is_UAVariable = false;
                        }
                        if (is_UAObject)
                        {
                            Name = "UAObject " + DisplayName;
                            DisplayName = "";
                            is_DisplayName = false;
                            is_UAObject = false;
                        }
                        if (is_UAMethod)
                        {
                            Name = "UAMethod " + DisplayName;
                            DisplayName = "";
                            is_DisplayName = false;
                            is_UAMethod = false;
                        }
                        if (is_Value)
                        {
                            is_Value = false;
                        }
                        if (is_DisplayName)
                        {
                            is_DisplayName = false;
                        }
                        break;
                }
                if (Name != "")
                {
                    using (var cd = AdminShell.ConceptDescription.CreateNew(AdminShell.Identification.IRDI, ""))
                    {
                        env.ConceptDescriptions.Add(cd);
                        cd.SetIEC61360Spec(
                            preferredNames: new string[] { "EN", Name },
                            shortName: Name,
                            unit: "string",
                            valueFormat: "STRING",
                            definition: new string[] { "EN", Name }
                        );

                        var p = AdminShell.Property.CreateNew(cd.GetDefaultShortName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                        p.valueType = "string";
                        p.value = Value;

                        if (i_propGroup > 0)
                        {
                            propGroup[i_propGroup - 1].Add(p);
                        }
                        else
                        {
                            sm.Add(p);
                        }
                    }
                    Name = "";
                    Value = "";
                }
            }
            sw.Close();
        }
    }
}
