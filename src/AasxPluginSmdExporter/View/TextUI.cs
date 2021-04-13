/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPluginSmdExporter.Model;

namespace AasxPluginSmdExporter
{
    public class TextUI
    {
        private Exporter exporter;

        private Queue<string> logs;

        public TextUI()
        {
            logs = new Queue<string>();
        }

        public TextUI(Queue<string> logs)
        {
            this.logs = logs;
        }
        public Queue<string> Start(string host, string machine)
        {
            string locstring = "Physical";
            return this.Start(host, machine, locstring);
        }


        public Queue<string> Start(string host, string machine, string modeltype)
        {
            bool usePhysical = modeltype.Equals("Physical");
            exporter = new Exporter(usePhysical);
            Console.WriteLine($"--- {machine} ---");
            Console.WriteLine();
            logs.Enqueue($"---{machine}---");
            logs.Enqueue("");
            if (CreateSMD(host, machine))
            {
                CreateConnections();
                PutSmd();


                CreateSummingPoints();
                CreateMultPoints();
                CreatePhysicalNodes();
                logs.Enqueue("--- Warnings ---");
                ToManyInputThrough();
                ToManyOutputAcross();
                logs.Enqueue("----------------");
                logs.Enqueue("");
                Console.WriteLine();
                PutSmd();
            }
            return logs;
        }

        public bool CreateSMD(string host, string machine)
        {
            if (SemanticPort.GetInstance() == null)
            {
                logs.Enqueue("Missing semantic ID csv");
                Console.WriteLine("Missing semantic ID csv");
            }
            else
            {
                if (EClass.InitEclass(logs) && SemanticPort.GetInstance() != null)
                {

                    try
                    {
                        if (exporter.CreateSMD(host, machine))
                        {
                            List<SimulationModel> noFmu = exporter.GetSimulationModelsWithoutFMU();
                            foreach (var sim in noFmu)
                            {
                                Console.WriteLine($"SimulationModel {sim.Name} has no Fmu");
                                Console.WriteLine();
                                logs.Enqueue($"SimulationModel {sim.Name} has no Fmu");
                                logs.Enqueue("");
                            }

                            foreach (var warning in SimulationModel.Warnings)
                            {
                                Console.WriteLine(warning);
                                logs.Enqueue(warning);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not find or read {machine}." +
                                $" Please check the name of the AAS and structure of the aasx");
                            logs.Enqueue($"Could not find or read {machine}." +
                                $" Please check the name of the AAS and structure of the aasx");
                            return false;
                        }
                    }
                    catch (AggregateException e)
                    {
                        print("No Connection:" + e.Message);
                        Console.WriteLine();
                        logs.Enqueue("No Connection:" + e.Message);
                        logs.Enqueue("");
                        return false;
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine("No mappings.csv found or does not contain all rows");
                    logs.Enqueue("No mappings.csv found or does not contain all rows");
                    return false;
                }
            }
            return false;





        }

        public void CreateConnections()
        {

            foreach (var relationshipElement in exporter.RelationshipElements)
            {


                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("---------------------------------------------------------------------------");
                Console.WriteLine($"--- Checking connections for {relationshipElement.ValueFirst} " +
                    $"and {relationshipElement.ValueSecond} ---");
                Console.WriteLine();
                logs.Enqueue("");
                logs.Enqueue("");
                logs.Enqueue("--------------------------------------------------------------------------------");
                logs.Enqueue($"--- Checking connections for {relationshipElement.ValueFirst}" +
                    $" and {relationshipElement.ValueSecond} ---");
                logs.Enqueue("");
                bool noError = true;

                if (!exporter.CheckCompatibleDomainsAndConnect(relationshipElement,
                    relationshipElement.ValueFirst,
                    relationshipElement.ValueSecond,
                    relationshipElement.FirstInterface,
                    relationshipElement.SecondInterface))
                {

                }

                List<string> difDatatype = exporter.ReturnListWithMessageForDifferentDatatypes(relationshipElement,
                    relationshipElement.ValueFirst,
                    relationshipElement.ValueSecond);
                if (difDatatype.Count != 0)
                {
                    foreach (var dif in difDatatype)
                    {
                        print("-" + dif);
                        logs.Enqueue("-" + dif);
                        noError = false;
                    }
                }

                List<string> difEclass = exporter.CheckeClassConnectionAndRemoveIncompatible(relationshipElement,
                    relationshipElement.ValueFirst,
                    relationshipElement.ValueSecond);
                if (difEclass.Count != 0)
                {
                    foreach (var dif in difEclass)
                    {
                        print("-" + dif);
                        logs.Enqueue("-" + dif);
                        noError = false;
                    }
                }

                if (!exporter.RelationHasConnection(relationshipElement))
                {
                    print($"{relationshipElement.ValueFirst} and" +
                        $"{relationshipElement.ValueSecond} have no connection!");
                    logs.Enqueue($"{relationshipElement.ValueFirst} and" +
                        $"{relationshipElement.ValueSecond} have no connection!");
                    noError = false;
                }


                if (noError)
                {
                    print("-No error", ConsoleColor.DarkGreen);
                    Console.WriteLine("");
                    logs.Enqueue("-No error");
                    logs.Enqueue("");
                }


                logs.Enqueue("------------------------------------------------------------------------------");
                Console.WriteLine("------------------------------------------------------------------------------");
            }

        }

        public void CreateSummingPoints()
        {
            if (exporter.CreateSummingPoints())
            {
                Console.WriteLine("-SummingPoints successfully created");
                logs.Enqueue("-SummingPoints successfully created");
                logs.Enqueue("");
            }
        }

        public void CreateMultPoints()
        {
            if (exporter.CreateMultPoints())
            {
                Console.WriteLine("");
                Console.WriteLine("-MultPoints successfully created");
                logs.Enqueue("-MultPoints successfully created");
                logs.Enqueue("");
            }
        }

        public void CreatePhysicalNodes()
        {
            exporter.CreateNodesForPhysicalPorts();
        }

        private void outputConnections()
        {
            foreach (var sim in exporter.SimulationsModels.Values)
            {
                Console.WriteLine();
                logs.Enqueue("");
                logs.Enqueue("");
                Console.WriteLine(sim.Name);
                logs.Enqueue(sim.Name);
                logs.Enqueue("");
                foreach (var port in sim.Inputs)
                {
                    foreach (var output in port.ConnectedTo)
                    {
                        Console.WriteLine(port.IdShort + " connected to " + output.IdShort);
                        logs.Enqueue(port.IdShort + " connected to " + output.IdShort);
                        logs.Enqueue("");
                    }
                }
                foreach (var port in sim.Outputs)
                {
                    foreach (var output in port.ConnectedTo)
                    {
                        Console.WriteLine(port.IdShort + " connected to " + output.IdShort);
                        logs.Enqueue(port.IdShort + " connected to " + output.IdShort);
                        logs.Enqueue("");
                    }
                }
            }
        }

        public void PutSmd()
        {
            if (exporter.PutSMD())
            {
                print("-Successfully put Submodel", ConsoleColor.DarkGreen);
                logs.Enqueue("-Successfully put Submodel");
                logs.Enqueue("");
            }
            else
            {
                print("-Error while putting Submodel", ConsoleColor.Red);
                logs.Enqueue("-Error while putting Submodel");
                logs.Enqueue("");
            }
        }

        public void ToManyOutputAcross()
        {

            foreach (var warning in exporter.ToManyOutputsAcross())
            {
                print(warning, ConsoleColor.Blue);
                logs.Enqueue(warning);
            }
        }

        public void ToManyInputThrough()
        {
            foreach (var warning in exporter.ToManyOutputThrough())
            {
                print(warning, ConsoleColor.Blue);
                logs.Enqueue(warning);
            }
        }
        public static void print(string str)
        {

            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ResetColor();
        }
        public static void print(string str, ConsoleColor color)
        {
            Console.BackgroundColor = color;
            Console.WriteLine(str);
            Console.ResetColor();
        }

    }
}
