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

// ReSharper disable UnusedType.Global .. could be used by other solutions/ plug-ins

namespace AasxIntegrationBase
{
    public class MinimalLogger
    {
        public int verbosity = 2;
        private List<string> list = new List<string>();
        private Action<string> loggingAction = null;

        /// <summary>
        /// Sets the verbosity of the Info statements
        /// </summary>
        public int Verbosity
        {
            get { return this.verbosity; }
            set { this.verbosity = value; }
        }

        /// <summary>
        /// Enables or diables (loggingAction == null) the de-tour of the logmessages to an external Action.
        /// </summary>
        public void UseLogAction(Action<string> loggingAction = null)
        {
            // off
            if (loggingAction == null)
            {
                this.loggingAction = null;
                lock (list)
                {
                    list = new List<string>();
                }
                return;
            }

            // on
            list = null;
            this.loggingAction = loggingAction;
        }

        /// <summary>
        /// Pops a log message
        /// </summary>
        public string CheckForLogMessage()
        {
            if (list == null || list.Count < 1)
                return null;
            lock (list)
            {
                var res = list[0];
                list.RemoveAt(0);
                return res;
            }
        }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="msg">Message in String.Format() format.</param>
        /// <param name="args">Arguments for String.Format()</param>
        private void Log(string msg, params object[] args)
        {
            if (this.loggingAction != null)
            {
                this.loggingAction(string.Format(msg, args));
                return;
            }

            // no, locally
            lock (list)
            {
                list.Add(string.Format(msg, args));
            }
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public void Info(string msg, params object[] args)
        {
            Log(msg, args);
        }

        /// <summary>
        /// Display a message, which is for information only, and only, if verbosity level >= level
        /// </summary>
        public void Info(int level, string msg, params object[] args)
        {
            if (level <= this.verbosity)
                Log(msg, args);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public void Error(string msg, params object[] args)
        {
            Log(msg, args);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public void Error(Exception ex, string where)
        {
            var s = String.Format("Error {0}: {1} {2} at {3}.",
                where,
                ex.Message,
                ((ex.InnerException != null) ? ex.InnerException.Message : ""),
                AdminShellNS.AdminShellUtil.ShortLocation(ex));
            Log(s);
        }
    }
}
