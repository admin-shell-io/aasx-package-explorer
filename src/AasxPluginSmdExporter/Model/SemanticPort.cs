/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginSmdExporter.Model
{
    public class SemanticPort
    {

        static SemanticPort semanticPort;

        /// <summary>
        /// Returns a Instance of a class which implements
        /// </summary>
        /// <returns></returns>
        public static SemanticPort GetInstance()
        {
            if (semanticPort == null)
            {
                semanticPort = new SemanticPort();
            }
            return semanticPort;
        }

        /// <summary>
        /// Returns the semanticID  for the given domain
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public string GetSemanticForPort(string domain)
        {
            if (IdTables.SemanticPortsDomain.ContainsKey(domain))
            {
                return IdTables.SemanticPortsDomain[domain];
            }
            return "";
        }

        protected SemanticPort()
        {

        }
    }
}
