﻿/*
 * Copyright (c) 2021 Robert Bosch Manufacturing Solutions GmbH
 *
 * Author: Monisha Macharla Vasu
 * 
 *
 * This Source Code Form is subject to the terms of the Apache License 2.0. 
 * If a copy of the Apache License 2.0 was not distributed with this
 * file, you can obtain one at https://spdx.org/licenses/Apache-2.0.html.
 * 
 *
 * SPDX-License-Identifier: Apache-2.0
 */


using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;
using VDS.RDF.Writing;
using VDS.RDF.Parsing;
using AdminShellNS;
using AasxBammRdfImExport.RDFentities;
using static AdminShellNS.AdminShellV20;
using System.Linq;

namespace AasxBammRdfImExport

{
    public static class BAMMRDFimport
    {

        public static AdminShellNS.AdminShellPackageEnv thePackageEnv;

        public static void ImportInto(string rdffn, AdminShell.AdministrationShellEnv env, AdminShell.Submodel sm,
        AdminShell.SubmodelRef smref)

        {
            thePackageEnv = new AdminShellNS.AdminShellPackageEnv();
            List<string> entity_subject = new List<string>();
            List<string> autos_list = new List<string>();
            List<string> set_list = new List<string>();
            List<string> entity_idshort_prop = new List<string>();
            List<string> entity_property_char = new List<string>();
            List<string> object_extract = new List<string>();
            
            //Initialize everything needed
            //AdminShell.AdministrationShellEnv env = thePackageEnv.AasEnv;
            var aas = new AdminShell.AdministrationShell();
            //var sm = new AdminShell.Submodel();
            //var env = new AdministrationShellEnv();
            
            
            
            //aas.views = new Views();
            //aas.views.views = new List<View>();
            //env.AdministrationShells.Add(aas);

            
            //parser graphs
            
            IGraph g = new Graph();
            IGraph h = new Graph();
            TurtleParser ttlparser = new TurtleParser();
            ttlparser.Load(g, rdffn);
            
            
            //initializing aspect
            
            Aspect obj_aspect = new Aspect();
            
            //Lists generated
            
            List<string> properties_list = new List<string>();
            List<string> properties_true_list = new List<string>();
            List<string> property_set_list = new List<string>();
            List<string> entity_list = new List<string>();
            Entity entity = new Entity();
            SubmodelElementCollection set_property = new SubmodelElementCollection();
            SubmodelElementCollection et = new SubmodelElementCollection();
            SubmodelElementCollection entity_set = new SubmodelElementCollection();



            // begin new (temporary) objects


            //var sm = new AdminShell.Submodel();
            string semantic_id = "";
            string semantic = "";
            string smc_semantic = "";

            foreach (Triple t in g.Triples)
            {   // Read the aspect name

                entity.idShort = (t.Subject.ToString());

                if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1)) == "Aspect")
                {
                    obj_aspect.Name = (t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));
                    sm.idShort = obj_aspect.Name;
                    semantic_id = t.Subject.ToString();
                    //semantic = (t.Subject.ToString() + (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1)));

                    var msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", t.Subject.ToString());
                    AdminShellNS.AdminShellV20.SemanticId smid = new AdminShellNS.AdminShellV20.SemanticId(msemanticID);
                    sm.semanticId = smid;
                    //var IdentificationID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", t.Subject.ToString());
                    //AdminShellNS.AdminShellV20.Identification smiden = new AdminShellNS.AdminShellV20.Identification(IdentificationID);
                    //sm.identification = smiden;
                    thePackageEnv.AasEnv.Submodels.Add(sm);


                }
            }





            //checks for BAMM entity, and creates a list of subject of entities

            foreach (Triple t in g.Triples)

            {
                if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1)) == "Entity")
                {
                    entity_subject.Add((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)));
                    foreach (var month in entity_subject)
                    {
                        Console.WriteLine(month);
                    }

                }

            }


            foreach (string prop in entity_subject)
            {


                foreach (Triple t in g.Triples)
                {

                    if (((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)) == prop) &&
                            ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "properties"))
                    {
                        string value = (t.Object.ToString()); //autos7
                        Console.WriteLine(value); //autos7


                        autos_list.Add(value);

                    }

                }

            }

            //algorithm for the child properties to find the SubmodelCollection
            foreach (string a in autos_list)

            {
                string after_rmov = "";
                string modified_value = "";
                string subjectstring = "";
                int number = 0;
                int b = 0;

                foreach (Triple t in g.Triples)
                {
                    subjectstring = ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)));
                    if ((subjectstring == a) &&
                        ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "first"))
                    {
                        string object_extract_method = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                        Console.WriteLine(object_extract_method);  
                        object_extract.Add(object_extract_method);
                        b = 1;
                        modified_value = a.Substring(a.Length - 1);

                        number = int.Parse(modified_value); 
                        after_rmov = (a.Remove(a.LastIndexOf("s") + 1) + (number + b).ToString());
                        Console.WriteLine(after_rmov); 



                    }
                    if (((subjectstring) == after_rmov) &&
                                    ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "first"))
                    {

                        string object_extract_method = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                        Console.WriteLine(object_extract_method);  
                        object_extract.Add(object_extract_method);
                        number = number + b;
                        after_rmov = (a.Remove(a.LastIndexOf("s") + 1) + (number + b).ToString());
                        Console.WriteLine(after_rmov);
                        foreach (var month in object_extract)
                        {
                            Console.WriteLine(month);
                        }
                    }

                }

            }





            //set

            // find set from the g.triples and add a list of subject. Then check from the property_list whether it contains set object, then assign it to another
            //list. 

            foreach (Triple t in g.Triples)
            {

                // Read the set name list

                if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1)) == "Set")

                {
                    set_list.Add(t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));
                }

            }

            foreach (string set_accept in properties_list)
            {
                foreach (Triple t in g.Triples)
                {

                    // Read the properties name list

                    if ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1) == set_accept) &&
                            set_list.Contains((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1))))

                    {
                        property_set_list.Add(t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)); //positions
                    }

                }

            }

            
            
            
            // create a submodelelement for the set subject. list of set smc = set_property

            foreach (string prop_set in property_set_list)

            {
                string property_Name = "";

                foreach (Triple t in g.Triples)
                {
                    if ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)) == prop_set)
                    {
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "name")
                        {
                            property_Name = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                            Console.WriteLine(property_Name);
                        }
                        AdminShell.Key msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", semantic);
                        set_property = AdminShell.SubmodelElementCollection.CreateNew(property_Name, null, msemanticID);
                        sm.Add(set_property);

                    }
                }

            }


            //Identify the entity[n] for the SubmodelCollection (SET)


            foreach (string prop in entity_subject)  
            {
                foreach (Triple t in g.Triples)
                {

                    if (((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)) == prop))

                    {
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "name")
                        {
                           // SubmodelElementCollection set_property = new SubmodelElementCollection();

                            string entity_set_idshort = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                            AdminShell.Key msemanticID = AdminShell.Key.CreateNew("GlobalReference", false, "IRI", semantic);
                            entity_set = AdminShell.SubmodelElementCollection.CreateNew(entity_set_idshort, null, msemanticID);
                            set_property.Add(entity_set);

                        }

                    }
                }

            }



            foreach (var entity_scope in entity_subject)   // entity_subject -- t.object --> "entity"

            {

                foreach (Triple t in g.Triples)
                {

                    // if object == entity_subject and predicate == dataType
                    //Characteristic_name = subject 

                    if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1) == entity_scope) &&
                                (t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1) == "dataType"))

                    {
                        entity_idshort_prop.Add(t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)); //list which datatype of the entity
                    }

                }
                
            }


            foreach (var characteristic_prop in entity_idshort_prop)  //entity_idshort_prop -- spatialpositioncharacteristic

            {


                foreach (Triple t in g.Triples)
                {

                    // if object == entity_subject and predicate == dataType
                    //Chracteristic_name = subect 

                    if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1) == characteristic_prop) &&
                                (t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1) == "characteristic"))

                    {
                        entity_property_char.Add(t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));
                        smc_semantic = t.Subject.ToString();
                    }
                }
              
            }





            //Identifying the true properties -- avoiding the SMC from the aspect properties

            foreach (Triple t in g.Triples)
            {

                // Read the properties name list
                if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1)) == "Property")
                {

                    properties_list.Add(t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));

                    properties_true_list = properties_list.Except(object_extract).ToList();

                    properties_true_list = properties_true_list.Except(entity_property_char).ToList();

                    //properties_true_list = properties_list.Remove(entity_idshort);

                }
            }
            // properties_true_list = properties_list.Except(object_extract).ToList();
            //properties_true_list = properties_true_list.Except(entity_property_char).ToList();
            //properties_true_list.Except(property_set_list).ToList();

            
            
            
            
            
            
            
            
            foreach (string prop in properties_true_list)
            {
                string property_Name = "";
                string property_ExampleValue = "";
                string property_PreferredName = "";
                string property_Description = "";
                string unit_name = "";
                string characteristic = "";


                foreach (Triple t in g.Triples)
                {
                    if ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)) == prop)
                    {

                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "name")
                        {
                            property_Name = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "preferredName")
                        {
                            property_PreferredName = (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1), t.Object.ToString().Length - 3));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "description")
                        {
                            property_Description = (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1), t.Object.ToString().Length - 3));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "exampleValue")
                        {
                            property_ExampleValue = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "characteristic")
                        {
                            characteristic = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));

                        }
                        semantic = (t.Subject.ToString());

                    }

                }


                AdminShell.UnitId unitID = AdminShell.UnitId.CreateNew("GlobalReference", false, "IRI", unit_name);


                using (var cd = AdminShell.ConceptDescription.CreateNew(
                                        "" + property_Name, AdminShell.Identification.IRI, semantic))
                {
                    env.ConceptDescriptions.Add(cd);
                    cd.SetIEC61360Spec(
                        preferredNames: new[] { "EN", property_PreferredName },
                        shortName: null,

                         unit: null,
                         unitID = AdminShell.UnitId.CreateNew("GlobalReference", false, "IRI", unit_name),
                        valueFormat: null,
                          dataType: "BOOLEAN",
                        definition: new[] { "EN", property_Description }

                    );

                    AdminShell.Key msemanticID = AdminShell.Key.CreateNew("ConceptDescription", true, "IRI", semantic);
                    var mp = AdminShell.Property.CreateNew(property_Name, null, msemanticID);
                    mp.valueType = "string";
                    mp.value = property_ExampleValue;
                    sm.Add(mp);


                }

            }




//SMC appended to the Submodel to the AAS


            foreach (var charc_prop in entity_property_char)

            {

                AdminShell.Key msemanticID = AdminShell.Key.CreateNew("ConceptDescription", true, "IRI", smc_semantic);
                et = AdminShell.SubmodelElementCollection.CreateNew(charc_prop, null, msemanticID);
                sm.Add(et);

            }



            //SMC elements


            foreach (string b in object_extract)

            {
                string name = "";
                string preferred_name = "";
                string description = "";
                string unit_name = "";

                foreach (Triple t in g.Triples)


                {

                    if ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)) == b)
                    {

                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "name")
                        {
                            name = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "preferredName")
                        {
                            preferred_name = (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1), t.Object.ToString().Length - 3));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "description")
                        {
                            description = (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1), t.Object.ToString().Length - 3));
                        }
                        semantic = (t.Subject.ToString());

                    }
                }
                AdminShell.UnitId unitID = AdminShell.UnitId.CreateNew("GlobalReference", false, "IRI", unit_name);


                using (var cd = AdminShell.ConceptDescription.CreateNew(
                                        "" + name, AdminShell.Identification.IRI, semantic))
                {
                    env.ConceptDescriptions.Add(cd);
                    cd.SetIEC61360Spec(
                        preferredNames: new[] { "EN", preferred_name },
                        shortName: null,

                         unit: null,
                         unitID = AdminShell.UnitId.CreateNew("GlobalReference", false, "IRI", unit_name),
                        valueFormat: null,
                          dataType: "BOOLEAN",
                        definition: new[] { "EN", description }

                    );

                    AdminShell.Key msemanticID = AdminShell.Key.CreateNew("ConceptDescription", true, "IRI", semantic);
                    var mp = AdminShell.Property.CreateNew(name, null, msemanticID);
                    mp.valueType = "string";
                    mp.value = description;
                    et.Add(mp);

                }


            }

        }
    }
}
