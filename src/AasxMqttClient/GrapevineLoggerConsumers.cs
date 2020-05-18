using Grapevine.Interfaces.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). 
The Grapevine REST server framework is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxMqttClient
{
    public class GrapevineLoggerSuper : IGrapevineLogger
    {
        // IGrapevineLogger side

        private LogLevel level = LogLevel.Trace;

        public LogLevel Level { get { return level; } set { level = value; } }

        public void Debug(object obj) { if (this.level >= LogLevel.Debug) this.Append("DBG: {0}", obj); }
        public void Debug(string message) { if (this.level >= LogLevel.Debug) this.Append("DBG: {0}", message); }
        public void Debug(string message, Exception ex) { if (this.level >= LogLevel.Debug) this.Append("DBG: Exception when {0}: {1}", message, ex.ToString()); }
        public void Error(string message, Exception ex) { if (this.level >= LogLevel.Error) this.Append("ERR: Exception when {0}: {1}", message, ex.ToString()); }
        public void Error(string message) { if (this.level >= LogLevel.Error) this.Append("ERR: {0}", message); }
        public void Error(object obj) { if (this.level >= LogLevel.Error) this.Append("ERR: {0}", obj); }
        public void Fatal(string message) { if (this.level >= LogLevel.Fatal) this.Append("FTL: {0}", message); }
        public void Fatal(object obj) { if (this.level >= LogLevel.Fatal) this.Append("FTL: {0}", obj); }
        public void Fatal(string message, Exception ex) { if (this.level >= LogLevel.Fatal) this.Append("FTL: Exception when {0}: {1}", message, ex.ToString()); }
        public void Info(string message, Exception ex) { if (this.level >= LogLevel.Info) this.Append("INF: Exception when {0}: {1}", message, ex.ToString()); }
        public void Info(string message) { if (this.level >= LogLevel.Info) this.Append("INF: {0}", message); }
        public void Info(object obj) { if (this.level >= LogLevel.Info) this.Append("INF: {0}", obj); }
        public void Log(LogEvent evt) { if (this.level >= evt.Level) this.Append("{0}", evt.Message); }
        public void Trace(string message, Exception ex) { if (this.level >= LogLevel.Debug) this.Append("TRC: Exception when {0}: {1}", message, ex.ToString()); }
        public void Trace(string message) { if (this.level >= LogLevel.Trace) this.Append("TRC: {0}", message); }
        public void Trace(object obj) { if (this.level >= LogLevel.Trace) this.Append("TRC: {0}", obj); }
        public void Warn(string message) { if (this.level >= LogLevel.Warn) this.Append("WRN: {0}", message); }
        public void Warn(string message, Exception ex) { if (this.level >= LogLevel.Warn) this.Append("WRN: Exception when {0}: {1}", message, ex.ToString()); }
        public void Warn(object obj) { if (this.level >= LogLevel.Warn) this.Append("WRN: {0}", obj); }

        // Consumer side

        public virtual void Append(string msg, params object[] args)
        {
        }

    }

    public class GrapevineLoggerToConsole : GrapevineLoggerSuper
    {
        public override void Append(string msg, params object[] args)
        {
            Console.Error.WriteLine(msg, args);
            Console.Error.Flush();
        }
    }

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
            if (list == null || list.Count < 1)
                return null;
            lock (list)
            {
                var res = list[0];
                list.RemoveAt(0);
                return res;
            }
        }
    }

}
