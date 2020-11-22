/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;

namespace AasxGlobalLogging
{
    /// <summary>
    /// Static class, wrapping log instance, to have a logging via Singleton.
    /// (no need to have Log instance in every single class)
    /// </summary>
    public static class Log
    {
        private static AasxIntegrationBase.LogInstance logInstance = new AasxIntegrationBase.LogInstance();

        public static AasxIntegrationBase.LogInstance LogInstance { get { return logInstance; } }

        /// <summary>
        /// Writes the message to STDERR skipping the both the short-term and the long-term storages.
        /// </summary>
        public static void Silent(string msg, params object[] args)
        {
            logInstance?.Silent(msg, args);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public static void Info(string msg, params object[] args)
        {
            logInstance?.Info(msg, args);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public static void Info(StoredPrint.Color color, string msg, params object[] args)
        {
            logInstance?.Info(color, msg, args);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public static void Error(string msg, params object[] args)
        {
            logInstance?.Error(msg, args);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public static void Error(Exception ex, string where)
        {
            logInstance?.Error(ex, where);
        }
    }
}
