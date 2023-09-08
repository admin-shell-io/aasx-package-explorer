/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasOpcUaServer
{
    public class AasxUaServerOptions
    {
        public enum JobType { None, ExportNodesetXml }

        /// <summary>
        /// This file shall be loaded at start of application
        /// </summary>
        public string AasxToLoad = null;

        /// <summary>
        /// Set special action/ job to be executed by server
        /// </summary>
        public JobType SpecialJob = JobType.None;

        /// <summary>
        /// File Name for exporting
        /// </summary>
        public string ExportFilename = "";

        /// <summary>
        /// If not Null, the Namespace index of exported nodes need to be in the list.
        /// </summary>
        public List<ushort> ExportFilterNamespaceIndex = null;

        /// <summary>
        /// Serialize reference as single string object instead of list of strings. 
        /// Note: set to TRUE for open62541 node-set-compiler
        /// </summary>
        public bool ReferenceKeysAsSingleString = false;

        /// <summary>
        /// Filter duplicated node-ids in th export.
        /// Note: set to TRUE for open62541 node-set-compiler
        /// </summary>
        public bool FilterForSingleNodeIds = false;

        /// <summary>
        /// If not null, then executed after finalizing special jobs. Will be set-up during initialization of server.
        /// </summary>
        public Action FinalizeAction = null;

        /// <summary>
        /// If true, usage of customised data types will be reduced
        /// </summary>
        public bool SimpleDataTypes = false;

        /// <summary>
        /// Set true, if descendants from "Object" (AASROOT, CDs) shall be linked as components, not
        /// via Organizes - relationship.
        /// </summary>
        public bool LinkRootAsComponent = false;

        /// <summary>
        /// Seemingly, in older versions, the dictionaries folder was missing
        /// </summary>
        public bool CeateDictionariesFolder = true;

        /// <summary>
        /// Parse args given by command line or plug-in arguments
        /// </summary>
        public void ParseArgs(string[] args)
        {
            if (args == null)
                return;

            for (int index = 0; index < args.Length; index++)
            {
                var arg = args[index].Trim().ToLower();
                var morearg = (args.Length - 1) - index;

                // flags
                if (arg == "-single-keys")
                {
                    ReferenceKeysAsSingleString = true;
                    continue;
                }

                if (arg == "-single-nodeids")
                {
                    FilterForSingleNodeIds = true;
                    continue;
                }

                if (arg == "-simple-data-types")
                {
                    SimpleDataTypes = true;
                    continue;
                }

                if (arg == "-link-root-as-component")
                {
                    LinkRootAsComponent = true;
                    continue;
                }

                // options
                if (arg == "-ns" && morearg > 0)
                {
                    if (Int32.TryParse(args[index + 1], out var i))
                    {
                        if (this.ExportFilterNamespaceIndex == null)
                            this.ExportFilterNamespaceIndex = new List<ushort>();
                        this.ExportFilterNamespaceIndex.Add((ushort)i);
                    }
                    index++;
                    continue;
                }
                if (arg == "-export-nodeset" && morearg > 0)
                {
                    this.SpecialJob = JobType.ExportNodesetXml;
                    this.ExportFilename = args[index + 1];
                    index++;
                    continue;
                }

                // tail. last argument shall be file to load
                if (System.IO.File.Exists(args[index]))
                    this.AasxToLoad = args[index];
                break;
            }
        }
    }
}
