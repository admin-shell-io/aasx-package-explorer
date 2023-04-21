/*
Copyright (c) 2021 Robert Bosch Manufacturing Solutions GmbH

Author: Monisha Macharla Vasu

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// ReSharper disable StringLastIndexOfIsCultureSpecific.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AasxBammRdfImExport.RDFentities;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxBammRdfImExport
{
    public static class BAMMRDFimport
    {

        public static AdminShellNS.AdminShellPackageEnv thePackageEnv;

        public static void ImportInto(
            string rdffn, Aas.Environment env, Aas.Submodel sm,
            Aas.Reference smref)
        {
            thePackageEnv = new AdminShellNS.AdminShellPackageEnv();
            List<string> entity_subject = new List<string>();
            List<string> autos_list = new List<string>();
            List<string> set_list = new List<string>();
            List<string> entity_idshort_prop = new List<string>();
            List<string> entity_property_char = new List<string>();
            List<string> object_extract = new List<string>();

            //parser graphs

            IGraph g = new Graph();
            TurtleParser ttlparser = new TurtleParser();
            ttlparser.Load(g, rdffn);

            //initializing aspect

            Aspect obj_aspect = new Aspect();

            //Lists generated

            List<string> properties_list = new List<string>();
            List<string> properties_true_list = new List<string>();
            List<string> property_set_list = new List<string>();
            Aas.Entity entity = new Aas.Entity(Aas.EntityType.SelfManagedEntity);
            Aas.SubmodelElementCollection set_property = new Aas.SubmodelElementCollection();
            Aas.SubmodelElementCollection et = new Aas.SubmodelElementCollection();
            Aas.SubmodelElementCollection entity_set = new Aas.SubmodelElementCollection();

            // begin new (temporary) objects

            string semantic = "";
            string smc_semantic = "";

            foreach (Triple t in g.Triples)
            {   // Read the aspect name

                entity.IdShort = (t.Subject.ToString());

                if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1)) == "Aspect")
                {
                    obj_aspect.Name = (t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));
                    sm.IdShort = obj_aspect.Name;

                    var msemanticID = ExtendReference.CreateFromKey(Aas.KeyTypes.GlobalReference, t.Subject.ToString());
                    sm.SemanticId = msemanticID;
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

                    if (((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)) == prop)
                        && ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "properties"))
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
                int number = 0;
                int b = 0;

                foreach (Triple t in g.Triples)
                {
                    string subjectstring = "";

                    subjectstring = ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)));
                    if ((subjectstring == a) &&
                        ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)) == "first"))
                    {
                        string object_extract_method = (t.Object.ToString().Substring(
                            t.Object.ToString().LastIndexOf("#") + 1));
                        Console.WriteLine(object_extract_method);
                        object_extract.Add(object_extract_method);
                        b = 1;
                        var modified_value = a.Substring(a.Length - 1);

                        number = int.Parse(modified_value);
                        after_rmov = (a.Remove(a.LastIndexOf("s") + 1) + (number + b).ToString());
                        Console.WriteLine(after_rmov);
                    }
                    if (((subjectstring) == after_rmov)
                        && ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "first"))
                    {

                        string object_extract_method = (t.Object.ToString().Substring(
                            t.Object.ToString().LastIndexOf("#") + 1));
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

            // find set from the g.triples and add a list of subject. Then check from the property_list whether
            // it contains set object, then assign it to another
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

                    if ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1) == set_accept)
                        && set_list.Contains((t.Object.ToString().Substring(
                            t.Object.ToString().LastIndexOf("#") + 1))))

                    {
                        //positions
                        property_set_list.Add(
                            t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));
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
                        var msemanticID = ExtendReference.CreateFromKey(Aas.KeyTypes.GlobalReference, semantic);
                        set_property = new Aas.SubmodelElementCollection(
                            idShort: property_Name, semanticId: msemanticID);
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
                            string entity_set_idshort =
                                (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                            var msemanticID = ExtendReference.CreateFromKey(Aas.KeyTypes.GlobalReference, semantic);
                            entity_set = new Aas.SubmodelElementCollection(
                                idShort: entity_set_idshort, semanticId: msemanticID);
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

                    if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1) == entity_scope)
                        && (t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)
                            == "dataType"))

                    {
                        //list which datatype of the entity
                        entity_idshort_prop.Add(
                            t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));
                    }

                }

            }

            //entity_idshort_prop -- spatialpositioncharacteristic
            foreach (var characteristic_prop in entity_idshort_prop)
            {
                foreach (Triple t in g.Triples)
                {
                    if ((t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1)
                         == characteristic_prop)
                        && (t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1)
                            == "characteristic"))

                    {
                        entity_property_char.Add(
                            t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));
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

                    properties_list.Add(
                        t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1));

                    properties_true_list = properties_list.Except(object_extract).ToList();

                    properties_true_list = properties_true_list.Except(entity_property_char).ToList();

                }
            }

            foreach (string prop in properties_true_list)
            {
                string property_Name = "";
                string property_ExampleValue = "";
                string property_PreferredName = "";
                string property_Description = "";
                string unit_name = "";

                // ReSharper disable once NotAccessedVariable
                string characteristic = "";


                foreach (Triple t in g.Triples)
                {
                    if ((t.Subject.ToString().Substring(t.Subject.ToString().LastIndexOf("#") + 1)) == prop)
                    {

                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "name")
                        {
                            property_Name =
                                (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "preferredName")
                        {
                            property_PreferredName =
                                (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1),
                                t.Object.ToString().Length - 3));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "description")
                        {
                            property_Description =
                                (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1),
                                t.Object.ToString().Length - 3));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "exampleValue")
                        {
                            property_ExampleValue =
                                (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "characteristic")
                        {
                            characteristic = (t.Object.ToString().Substring(t.Object.ToString().LastIndexOf("#") + 1));

                        }
                        semantic = (t.Subject.ToString());

                    }

                }

                {
                    var cd = new Aas.ConceptDescription(idShort: property_Name, id: semantic);
                    env.ConceptDescriptions.Add(cd);
                    cd.SetIEC61360Spec(
                        preferredNames: new[] { "EN", property_PreferredName },
                        shortName: null,

                         unit: null,
                         unitId: ExtendReference.CreateFromKey(Aas.KeyTypes.GlobalReference, unit_name),
                        valueFormat: null,
                          dataType: "BOOLEAN",
                        definition: new[] { "EN", property_Description }

                    );

                    var msemanticID = ExtendReference.CreateFromKey(Aas.KeyTypes.ConceptDescription, semantic);
                    var mp = new Aas.Property(
                        Aas.DataTypeDefXsd.String, idShort: property_Name, semanticId: msemanticID);
                    mp.Value = property_ExampleValue;
                    sm.Add(mp);
                }
            }

            //SMC appended to the Submodel to the AAS

            foreach (var charc_prop in entity_property_char)

            {
                var msemanticID = ExtendReference.CreateFromKey(Aas.KeyTypes.ConceptDescription, smc_semantic);
                et = new Aas.SubmodelElementCollection(idShort: charc_prop, semanticId: msemanticID);
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
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "preferredName")
                        {
                            preferred_name = (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1),
                                t.Object.ToString().Length - 3));
                        }
                        if ((t.Predicate.ToString().Substring(t.Predicate.ToString().LastIndexOf("#") + 1))
                            == "description")
                        {
                            description = (t.Object.ToString().Substring((t.Object.ToString().LastIndexOf("#") + 1),
                                t.Object.ToString().Length - 3));
                        }
                        semantic = (t.Subject.ToString());

                    }
                }

                {
                    var cd = new Aas.ConceptDescription(idShort: "" + name, id: semantic);
                    env.ConceptDescriptions.Add(cd);
                    cd.SetIEC61360Spec(
                        preferredNames: new[] { "EN", preferred_name },
                        shortName: null,

                         unit: null,
                         unitId: ExtendReference.CreateFromKey(Aas.KeyTypes.GlobalReference, unit_name),
                        valueFormat: null,
                          dataType: "BOOLEAN",
                        definition: new[] { "EN", description }

                    );
                    var msemanticID = ExtendReference.CreateFromKey(Aas.KeyTypes.ConceptDescription, semantic);
                    var mp = new Aas.Property(
                        Aas.DataTypeDefXsd.String, idShort: name, semanticId: msemanticID);
                    mp.Value = description;
                    et.Add(mp);

                }


            }

        }
    }
}
