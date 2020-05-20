﻿using System;
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
    public class BMEcatTools
    {
        static string[] IRDIs_AXIS1D = new string[] {
                                    "0173-1#02-AAM656#002", "0173-1#02-AAM655#002", "0173-1#02-AAN501#002",
                                    "0173-1#02-AAN505#002", "0173-1#02-AAN449#002", "0173-1#02-AAS448#001",
                                    "0173-1#02-AAN503#002", "0173-1#02-AAC378#002", "0173-1#02-AAN504#002",
                                    "0173-1#02-AAM650#002", "0173-1#02-AAR937#002", "0173-1#02-AAZ377#001",
                                    "0173-1#02-AAS360#001", "0173-1#02-AAN502#002"
                                };
        static string[] names_AXIS1D = new string[] { "AXIS_X", "AXIS_Y", "AXIS_Z",
                                      "AXIS_DIRECTION_X", "AXIS_DIRECTION_Y", "AXIS_DIRECTION_Z"
                                };

        static string[] names_LEVELTYPE = new string[] { "MIN", "MAX", "TYP", "NOM" };

        public static void ImportBMEcatToSubModel(string inputFn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm, AdminShell.SubmodelRef smref)
        {
            // Select between BMEcat and XML publication
            // Tag "<BMECAT" for BMEcat File
            // Tag "<Publication" for XML from GWIS
            Boolean isBMEcat = false;
            Boolean isPublication = false;

            XmlTextReader reader = new XmlTextReader(inputFn);
            StreamWriter sw = File.CreateText(inputFn + ".log.txt");

            // BMEcat or Publication?
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        if (reader.Name == "BMECAT")
                            isBMEcat = true;
                        if (reader.Name == "Publication")
                            isPublication = true;
                        break;
                }
                if (isBMEcat || isPublication)
                    break;
            }

            // BMEcat
            String FT_ID = "";
            String FT_NAME = "";
            String[] FVALUE = new string[] { "", "", "", "", "", "", "", "", "", "" };
            int i_FVALUE = 0;
            String FUNIT = "";
            String FID = "";
            String FPARENT_ID = "";
            Boolean is_FT_ID = false;
            Boolean is_FT_NAME = false;
            Boolean is_FVALUE = false;
            Boolean is_FUNIT = false;
            Boolean is_FID = false;
            Boolean is_FPARENT_ID = false;
            String[] Stack_FID = new string[10];
            int StackPointer_FID = 0;
            // AML OPC
            String node = "";
            String rString = "";
            /*
            Boolean externalInterface = false;
            Boolean is_value = false;
            int count_value = 0;
            String value = "";
            String propertyName = "";
            String propertyValue = "";
            */
            // GWIS XML Publication
            String attribute_label_id = "";
            String attribute_value = "";
            String subheadline = "";
            Boolean is_technical_data = false;
            Boolean is_attribute_list = false;
            Boolean is_subheadline = false;
            Boolean is_attribute = false;
            Boolean is_attribute_label = false;
            Boolean is_attribute_value = false;
            AdminShell.SubmodelElementCollection[] propGroup = new AdminShell.SubmodelElementCollection[10];

            // GWIS XML Publication
            if (isPublication)
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            if (reader.Name == "technical_data")
                                is_technical_data = true;
                            if (reader.Name == "attribute_list")
                                is_attribute_list = true;
                            if (reader.Name == "subheadline")
                                is_subheadline = true;
                            if (reader.Name == "attribute")
                                is_attribute = true;
                            if (reader.Name == "label")
                                is_attribute_label = true;
                            if (reader.Name == "value")
                                is_attribute_value = true;
                            node = reader.Name;
                            break;
                        case XmlNodeType.Text: //Display the text in each element.
                            if (is_technical_data && is_attribute_list && is_attribute && is_attribute_label)
                            {
                                attribute_label_id = reader.Value;
                                is_attribute_label = false;
                            }
                            if (is_technical_data && is_attribute_list && is_attribute && is_attribute_value == true)
                            {
                                attribute_value = reader.Value;
                                is_attribute_value = false;
                            }
                            if (is_technical_data && is_attribute_list && is_subheadline == true)
                            {
                                subheadline = reader.Value;
                            }
                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            if (reader.Name == "subheadline")
                            {
                                if (subheadline != "")
                                {
                                    propGroup[0] = AdminShell.SubmodelElementCollection.CreateNew(subheadline);
                                    sm.Add(propGroup[0]);
                                }
                            }
                            if (reader.Name == "attribute")
                            {
                                if (attribute_label_id != "" && attribute_value != "")
                                {
                                    sw.WriteLine(attribute_label_id + " | " + attribute_value);
                                    using (var cd = AdminShell.ConceptDescription.CreateNew(AdminShell.Identification.IRDI, FT_ID))
                                    {
                                        env.ConceptDescriptions.Add(cd);
                                        cd.SetIEC61360Spec(
                                            preferredNames: new string[] { "EN", attribute_label_id },
                                            shortName: attribute_label_id,
                                            unit: "string",
                                            valueFormat: "STRING",
                                            definition: new string[] { "EN", attribute_label_id }
                                        );

                                        var p = AdminShell.Property.CreateNew(cd.GetDefaultShortName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                                        if (is_subheadline)
                                        {
                                            propGroup[0].Add(p);
                                        }
                                        else
                                        {
                                            sm.Add(p);
                                        }
                                        p.valueType = "string";
                                        p.value = attribute_value;
                                    }

                                }
                                is_attribute = false;
                                attribute_value = "";
                                attribute_label_id = "";
                            }
                            if (reader.Name == "attribute_list")
                            {
                                is_attribute_list = false;
                                is_subheadline = false;
                                subheadline = "";
                            }
                            break;
                    }
                }
                sw.Close();
                return;
            }

            // BMEcat
            if (isBMEcat)
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            if (reader.Name == "FT_ID")
                                is_FT_ID = true;
                            if (reader.Name == "FT_NAME")
                                is_FT_NAME = true;
                            if (reader.Name == "FVALUE")
                            {
                                rString = reader.GetAttribute("lang");
                                if (rString == null || rString == "" || rString == "eng") // only no language or English values
                                    is_FVALUE = true;
                            }
                            if (reader.Name == "FUNIT")
                                is_FUNIT = true;
                            if (reader.Name == "FID")
                                is_FID = true;
                            if (reader.Name == "FPARENT_ID")
                                is_FPARENT_ID = true;
                            // AML OPC
                            /*
                            if (reader.Name == "Value")
                            {
                                is_value = true;
                            }
                            if (reader.Name == "ExternalInterface")
                            {
                                rString = reader.GetAttribute("RefBaseClassPath");
                                if (rString == "MTPCommunicationICLib/DataItem/OPCUAItem")
                                {
                                    externalInterface = true;
                                    if (count_value != 0)
                                    {
                                        propertyName = "OPC-Server: ";
                                        propertyValue = value;
                                        // sw.WriteLine("OPC-Server" + value);
                                    }
                                    count_value = 0;

                                }
                            }
                            */
                            node = reader.Name;
                            break;
                        case XmlNodeType.Text: //Display the text in each element.
                            // AML OPC
                            /*
                            if (is_value)
                            {
                                value = reader.Value;
                                is_value = false;
                                count_value++;
                                if (externalInterface)
                                {
                                    if (count_value == 2)
                                    {
                                        propertyName = "OPC-Variable: ";
                                        propertyValue = value;
                                        // sw.WriteLine("OPC-Variable" + value);
                                    }
                                }
                            }
                            */
                            // BMEcat
                            if (is_FT_ID == true)
                            {
                                FT_ID = reader.Value;
                                is_FT_ID = false;
                            }
                            if (is_FT_NAME == true)
                            {
                                FT_NAME = reader.Value;
                                is_FT_NAME = false;
                            }
                            if (is_FVALUE == true)
                            {
                                FVALUE[i_FVALUE++] = reader.Value;
                                is_FVALUE = false;
                            }
                            if (is_FUNIT == true)
                            {
                                FUNIT = reader.Value;
                                is_FUNIT = false;
                            }
                            if (is_FUNIT == true)
                            {
                                FUNIT = reader.Value;
                                is_FUNIT = false;
                            }
                            if (is_FID == true)
                            {
                                FID = reader.Value;
                                is_FID = false;
                            }
                            if (is_FPARENT_ID == true)
                            {
                                FPARENT_ID = reader.Value;
                                is_FPARENT_ID = false;
                            }
                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            // AML OPC
                            /*
                            if (propertyName != "" && propertyValue != "")
                            {
                                sw.WriteLine(propertyName + " : " + propertyValue);
                                var p = AdminShell.Property.CreateNew(propertyName, "PARAMETER");
                                sm.Add(p);
                                p.valueType = "string";
                                p.value = propertyValue;
                            }
                            propertyName = "";
                            propertyValue = "";

                            if (reader.Name == "ExternalInterface")
                            {
                                externalInterface = false;
                                count_value = 0;
                            }
                            */
                            // BMEcat
                            if (reader.Name == "FEATURE")
                            {
                                // Boolean is_AXIS1D = IRDIs_AXIS1D.Contains(FT_ID);
                                Boolean is_AXIS1D = (i_FVALUE == 6);
                                int k;

                                for (k = 0; k < i_FVALUE; k++)
                                    sw.WriteLine(FT_ID + " | " + FT_NAME + " | " + FVALUE[k] + " | " + FUNIT + " | " + FID + " | " + FPARENT_ID);

                                if (FT_ID != "" && FT_NAME != "") // korrekter Eintrag
                                {
                                    if (FPARENT_ID == "-1")
                                    {
                                        StackPointer_FID = 0;
                                    }
                                    if (i_FVALUE > 0) // Property
                                    {
                                        for (k = 0; k < i_FVALUE; k++)
                                        {
                                            if (FUNIT != "")
                                            {
                                                if (FUNIT == "0173-1#05-AAA480#002")
                                                {
                                                    FUNIT = "mm | " + FUNIT;
                                                }
                                                else if (FUNIT == "0173-1#05-AAA731#002")
                                                {
                                                    FUNIT = "kg | " + FUNIT;
                                                }
                                                else if (FUNIT == "0173-1#05-AAA153#002")
                                                {
                                                    FUNIT = "V | " + FUNIT;
                                                }
                                                else if (FUNIT == "0173-1#05-AAA295#002")
                                                {
                                                    FUNIT = "mm² | " + FUNIT;
                                                }
                                                else if (FUNIT == "0173-1#05-AAA723#002")
                                                {
                                                    FUNIT = "mA | " + FUNIT;
                                                }
                                                else if (FUNIT == "0173-1#05-AAA114#002")
                                                {
                                                    FUNIT = "ms | " + FUNIT;
                                                }
                                                else if (FUNIT == "0173-1#05-AAA220#002")
                                                {
                                                    FUNIT = "A | " + FUNIT;
                                                }
                                            }

                                            string extendedname = FT_NAME;
                                            if (is_AXIS1D) // checked by IRDIs
                                            {
                                                extendedname += " " + names_AXIS1D[k];
                                            }
                                            else if (i_FVALUE > 1 && i_FVALUE <= 4)
                                            {
                                                extendedname += " " + names_LEVELTYPE[k]; // MIN, MAX, ...
                                            }

                                            // sw.WriteLine(FT_ID + " | " + extendedname + " | " + FVALUE + " | " + FUNIT + " | " + FID +" | " + FPARENT_ID);
                                            using (var cd = AdminShell.ConceptDescription.CreateNew(AdminShell.Identification.IRDI, FT_ID))
                                            {
                                                env.ConceptDescriptions.Add(cd);
                                                cd.SetIEC61360Spec(
                                                    preferredNames: new string[] { "DE", extendedname, "EN", extendedname },
                                                    shortName: extendedname,
                                                    unit: FUNIT,
                                                    valueFormat: "REAL_MEASURE",
                                                    definition: new string[] { "DE", extendedname,
                                                                                "EN", extendedname }
                                                );

                                                var p = AdminShell.Property.CreateNew(cd.GetDefaultShortName(), "PARAMETER", AdminShell.Key.GetFromRef(cd.GetReference()));
                                                p.valueType = "double";
                                                p.value = FVALUE[k];

                                                if (StackPointer_FID == 0) // am Submodell
                                                {
                                                    sm.Add(p);
                                                }
                                                else // an Collection
                                                {
                                                    for (int j = 0; j < StackPointer_FID; j++)
                                                    {
                                                        if (Stack_FID[j] == FPARENT_ID) // Vater gefunden
                                                        {
                                                            propGroup[j].Add(p);
                                                            StackPointer_FID = j + 1;
                                                            break;
                                                        }
                                                    }

                                                }
                                            }
                                        }
                                    }
                                    else // Collection
                                    {
                                        if (StackPointer_FID == 0) // oberste Collection
                                        {
                                            Stack_FID[0] = FID;
                                            propGroup[0] = AdminShell.SubmodelElementCollection.CreateNew(FT_NAME);
                                            sm.Add(propGroup[0]);
                                            StackPointer_FID++; // nächste Ebene
                                        }
                                        else // Collection suchen
                                        {
                                            for (int j = 0; j < StackPointer_FID; j++)
                                            {
                                                if (Stack_FID[j] == FPARENT_ID) // Vater gefunden
                                                {
                                                    StackPointer_FID = j + 1;
                                                    Stack_FID[StackPointer_FID] = FID;
                                                    propGroup[StackPointer_FID] = AdminShell.SubmodelElementCollection.CreateNew(FT_NAME);
                                                    propGroup[StackPointer_FID - 1].Add(propGroup[StackPointer_FID]);
                                                    StackPointer_FID++; // nächste Ebene
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                FT_ID = "";
                                FT_NAME = "";
                                i_FVALUE = 0;
                                FUNIT = "";
                                FID = "";
                                FPARENT_ID = "";
                            }
                            break;
                    }
                }
            }
            sw.Close();
            // OZ end
        }
    }
}
