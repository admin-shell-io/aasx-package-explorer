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
using AasxIntegrationBase;
using Grapevine.Interfaces.Shared;
using JetBrains.Annotations;

/*
Please notice: the API and REST routes implemented in this version of the source code are not specified and
standardised by the specification Details of the Administration Shell.
The hereby stated approach is solely the opinion of its author(s).
*/

namespace AasxMqttClient
{
    public class GrapevineLoggerSuper : IGrapevineLogger
    {
        // IGrapevineLogger side

        private LogLevel level = LogLevel.Trace;

        public LogLevel Level { get { return level; } set { level = value; } }

        public void Debug(object obj) { if (this.level >= LogLevel.Debug) this.Append("DBG: {0}", obj); }
        public void Debug(string message) { if (this.level >= LogLevel.Debug) this.Append("DBG: {0}", message); }
        public void Debug(string message, Exception ex)
        {
            if (this.level >= LogLevel.Debug)
                this.Append("DBG: Exception when {0}: {1}", message, ex.ToString());
        }
        public void Error(string message, Exception ex)
        {
            if (this.level >= LogLevel.Error) this.Append("ERR: Exception when {0}: {1}", message, ex.ToString());
        }
        public void Error(string message)
        {
            if (this.level >= LogLevel.Error) this.Append("ERR: {0}", message);
        }
        public void Error(object obj)
        {
            if (this.level >= LogLevel.Error) this.Append("ERR: {0}", obj);
        }
        public void Fatal(string message)
        {
            if (this.level >= LogLevel.Fatal) this.Append("FTL: {0}", message);
        }
        public void Fatal(object obj)
        {
            if (this.level >= LogLevel.Fatal) this.Append("FTL: {0}", obj);
        }
        public void Fatal(string message, Exception ex)
        {
            if (this.level >= LogLevel.Fatal) this.Append("FTL: Exception when {0}: {1}", message, ex.ToString());
        }
        public void Info(string message, Exception ex)
        {
            if (this.level >= LogLevel.Info) this.Append("INF: Exception when {0}: {1}", message, ex.ToString());
        }
        public void Info(string message) { if (this.level >= LogLevel.Info) this.Append("INF: {0}", message); }
        public void Info(object obj) { if (this.level >= LogLevel.Info) this.Append("INF: {0}", obj); }
        public void Log(LogEvent evt) { if (this.level >= evt.Level) this.Append("{0}", evt.Message); }
        public void Trace(string message, Exception ex)
        {
            if (this.level >= LogLevel.Debug) this.Append("TRC: Exception when {0}: {1}", message, ex.ToString());
        }
        public void Trace(string message) { if (this.level >= LogLevel.Trace) this.Append("TRC: {0}", message); }
        public void Trace(object obj) { if (this.level >= LogLevel.Trace) this.Append("TRC: {0}", obj); }
        public void Warn(string message) { if (this.level >= LogLevel.Warn) this.Append("WRN: {0}", message); }
        public void Warn(string message, Exception ex)
        {
            if (this.level >= LogLevel.Warn) this.Append("WRN: Exception when {0}: {1}", message, ex.ToString());
        }
        public void Warn(object obj) { if (this.level >= LogLevel.Warn) this.Append("WRN: {0}", obj); }

        // Consumer side

        public virtual void Append(string msg, params object[] args)
        {
        }

    }

    [UsedImplicitlyAttribute] // for eventual use
    public class GrapevineLoggerToConsole : GrapevineLoggerSuper
    {
        public override void Append(string msg, params object[] args)
        {
            Console.Error.WriteLine(msg, args);
            Console.Error.Flush();
        }
    }

    [UsedImplicitlyAttribute] // for eventual use
    public class GrapevineLoggerToListOfStrings : GrapevineLoggerSuper
    {
        private List<string> list = new List<string>();

        public override void Append(string msg, params object[] args)
        {
            lock (list)
            {
                list.Add(string.Format(msg, args));
            }
        }

        public string Pop()
        {
            if (list == null)
                return null;
            lock (list)
            {
                if (list.Count < 1)
                    return null;
                var res = list[0];
                list.RemoveAt(0);
                return res;
            }
        }
    }

    [UsedImplicitlyAttribute] // for eventual use
    public class GrapevineLoggerToStoredPrints : GrapevineLoggerSuper
    {
        private List<StoredPrint> list = new List<StoredPrint>();

        public override void Append(string msg, params object[] args)
        {
            lock (list)
            {
                var str = msg;
                if (args != null && args.Length > 0)
                    str = string.Format(msg, args);
                list.Add(new StoredPrint(str));
            }
        }

        public void Append(StoredPrint sp)
        {
            if (sp == null)
                return;

            lock (list)
            {
                list.Add(sp);
            }
        }

        public StoredPrint Pop()
        {
            if (list == null)
                return null;
            lock (list)
            {
                if (list.Count < 1)
                    return null;
                var res = list[0];
                list.RemoveAt(0);
                return res;
            }
        }
    }

}
