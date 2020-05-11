using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md). */

namespace AasxIntegrationBase
{
    /// <summary>
    /// This class is used to convey "print messages", such for logging, over boundaries of modules and .dll's
    /// </summary>
    public class StoredPrint
    {
        // constants 
        public const int ColorBlack = 0;
        public const int ColorBlue = 1;
        public const int ColorRed = 2;
        public const int ColorNoDisplay = 3;

        // members
        public int color = 0;
        public bool isError = false;
        public string msg = "";
        public string linkTxt = null;
        public string linkUri = null;
        public string stackTrace = null;
        public Exception origException = null;

        // constructurs

        /// <param name="msg">The complete message; can contain %LINK% as substitude position for link text</param>
        public StoredPrint(string msg)
        {
            this.color = ColorBlack;
            this.msg = msg;
        }

        /// <param name="color">Color code black/ blue/ red</param>
        /// <param name="msg">The complete message; can contain %LINK% as substitude position for link text</param>
        /// <param name="linkTxt">Link text to be shown</param>
        /// <param name="linkUri">Link URI to be navigated to</param>
        /// <param name="isError">Represents an error, e.g. will be counted</param>
        /// <param name="stackTrace">string serialized stack trace information</param>
        /// <param name="origException">original exception information</param>
        public StoredPrint(int color, string msg, string linkTxt = null, string linkUri = null, bool isError = false, string stackTrace = null, Exception origException = null)
        {
            this.color = color;
            this.msg = msg;
            this.linkTxt = linkTxt;
            this.linkUri = linkUri;
            this.isError = isError;
            this.stackTrace = stackTrace;
            this.origException = origException;
        }

        // serialization

        public new string ToString()
        {
            return String.Format("{0}:{1} {2}", color, msg, linkTxt);
        }
    }

    /// <summary>
    /// Implements the management of stores prints, such as in a store.
    /// </summary>
    public interface IManageStoredPrints
    {
        /// <summary>
        /// For compatibility reasons with MinimalLogger. Pop the oldest messages as string.
        /// </summary>
        string CheckForLogMessage();

        /// <summary>
        /// Pop the oldest messages as string.
        /// </summary>
        StoredPrint PopLastStoredPrint();

        /// <summary>
        /// Clear all stored prints
        /// </summary>
        void ClearStoredPrints();

        /// <summary>
        /// Directly apped a stored print.
        /// </summary>
        void Append(StoredPrint sp);
    }

    /// <summary>
    /// This is the standardized interface to provide logging facilities
    /// </summary>
    public interface ILogProvider
    {
        /// <summary>
        /// Only append to longterm or to file, if file name is set
        /// </summary>
        void Silent(string msg, params object[] args);

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        void Info(string msg, params object[] args);

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        void Info(int level, string msg, params object[] args);

        /// <summary>
        /// Display a message, which is for information only        
        /// </summary>
        /// <param name="link">The %LINK% portion in the message will be substituded with a hyperlink</param>
        void InfoWithHyperlink(int level, string msg, string linkTxt, string linkUri, params object[] args);

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        void Error(string msg, params object[] args);

        /// <summary>
        /// Display a message, which is for derrors      
        /// </summary>
        /// <param name="link">The %LINK% portion in the message will be substituded with a hyperlink</param>
        void ErrorWithHyperlink(string msg, string linkTxt, string linkUri, params object[] args);

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        void Error(Exception ex, string where);
    }

    public class StoredPrintsMinimalStore : IManageStoredPrints
    {
        // private members
        private List<StoredPrint> StoredPrints = new List<StoredPrint>();

        // Interface

        /// <summary>
        /// For compatibility reasons with MinimalLogger. Pop the oldest messages as string.
        /// </summary>
        public string CheckForLogMessage()
        {
            var sp = PopLastStoredPrint();
            if (sp == null)
                return null;
            return sp.ToString();
        }

        /// <summary>
        /// Pop the oldest messages as string.
        /// </summary>
        public StoredPrint PopLastStoredPrint()
        {
            if (StoredPrints.Count < 1)
                return null;

            lock (StoredPrints)
            {
                var sp = StoredPrints.First();
                StoredPrints.Remove(sp);
                return sp;
            }
        }

        /// <summary>
        /// Get all stored prints. Does not clear the bffer.
        /// </summary>
        /// <returns></returns>
        public StoredPrint[] GetStoredPrints()
        {
            return StoredPrints.ToArray();
        }

        /// <summary>
        /// Clear all stored prints
        /// </summary>
        public void ClearStoredPrints()
        {
            StoredPrints.Clear();
        }

        /// <summary>
        /// Directly append a stored print.
        /// </summary>
        public void Append(StoredPrint sp)
        {
            if (sp == null || StoredPrints == null)
                return;

            // in any case, add to stored prints
            lock (StoredPrints)
            {
                StoredPrints.Add(sp);
            }
        }

    }

    /// <summary>
    /// This class is intended to be used as static Log facility.
    /// </summary>
    public class LogInstance : ILogProvider
    {
        /// <summary>
        /// Will display debug messages only if level is smaller/equal than this level
        /// </summary>
        public int DebugLevel = 0;

        /*
        private static string appendToFileFN = "";
        private static StreamWriter appendToFileWriter = null;
        private static string AppendToFile
        {
            get
            {
                return appendToFileFN;
            }
            set
            {
                appendToFileFN = value.Trim();
                if (appendToFileFN.Length < 1 && appendToFileWriter != null)
                {
                    appendToFileWriter.Close();
                    appendToFileWriter = null;
                }
                else
                if (appendToFileFN.Length > 0 && appendToFileWriter == null)
                {
                    appendToFileWriter = new StreamWriter(appendToFileFN, true);
                }
            }
        }

        public static void CloseFiles()
        {
            if (appendToFileWriter != null)
                appendToFileWriter.Close();
        }*/


        // private static List<StoredPrint> LoggedPrints = new List<StoredPrint>();

        private StoredPrintsMinimalStore shortTermStore = new StoredPrintsMinimalStore();
        private StoredPrintsMinimalStore longTermStore = null;

        // Enable options

        public void EnableLongTermStore()
        {
            longTermStore = new StoredPrintsMinimalStore();
        }

        // Interface

        /// <summary>
        /// For compatibility reasons with MinimalLogger. Pop the oldest messages as string.
        /// </summary>
        public string CheckForShortTermMessage()
        {
            return shortTermStore?.CheckForLogMessage();
        }

        /// <summary>
        /// Pop the oldest messages as string.
        /// </summary>
        public StoredPrint PopLastShortTermPrint()
        {
            return shortTermStore?.PopLastStoredPrint();
        }

        /// <summary>
        /// Get all stored prints. Does not clear the bffer.
        /// </summary>
        /// <returns></returns>
        public StoredPrint[] GetStoredLongTermPrints()
        {
            return longTermStore?.GetStoredPrints();
        }

        /// <summary>
        /// Clear all stored prints
        /// </summary>
        public void ClearStoredPrints()
        {
            shortTermStore?.ClearStoredPrints();
            longTermStore?.ClearStoredPrints();
        }

        /// <summary>
        /// Directly append a stored print.
        /// </summary>
        public void Append(StoredPrint sp)
        {
            shortTermStore?.Append(sp);
            longTermStore?.Append(sp);
        }

        /// <summary>
        /// Incremented for each error
        /// </summary>
        public int NumberErrors = 0;

        /// <summary>
        /// Clears errors
        /// </summary>
        public void ClearNumberErrors()
        {
            NumberErrors = 0;
        }

        /*
        private static void InternalAppendFile(StoredPrint p)
        {
            if (p == null || appendToFileWriter == null)
                return;
            appendToFileWriter.WriteLine(p.ToString());
            appendToFileWriter.Flush();
        }
        */
        

        private void InternalPrint(int color, string msg, params object[] args)
        {
            var s = String.Format(msg, args);
            var p = new StoredPrint(color, s);
            Append(p);
            // Console.WriteLine(s);
        }

        private void InternalPrintWithHyperlink(int color, string msg, string link, params object[] args)
        {
            var s = String.Format(msg, args);
            var p = new StoredPrint(color, s, link);
            Append(p);
            // Console.WriteLine(s);
        }

        #region //////// Append to Log

        /// <summary>
        /// Only append to longterm or to file, if file name is set
        /// </summary>
        public void Silent(string msg, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.ColorNoDisplay, String.Format(msg, args));
            Append(p);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public void Info(string msg, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.ColorBlack, String.Format(msg, args));
            Append(p);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public void Info(int level, string msg, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.ColorBlack, String.Format(msg, args));
            if (level <= DebugLevel)
                Append(p);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public void Info(int level, int color, string msg, params object[] args)
        {
            var p = new StoredPrint(color, String.Format(msg, args));
            if (level <= DebugLevel)
                Append(p);
        }

        /// <summary>
        /// Display a message, which is for information only        
        /// </summary>
        /// <param name="link">The %LINK% portion in the message will be substituded with a hyperlink</param>
        public void InfoWithHyperlink(int level, string msg, string linkTxt, string linkUri, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.ColorBlack, String.Format(msg, args), linkTxt: linkTxt, linkUri: linkUri);
            if (level <= DebugLevel)
                Append(p);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public void Error(string msg, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.ColorRed, String.Format(msg, args), isError: true);
            NumberErrors++;
            Append(p);
        }

        /// <summary>
        /// Display a message, which is for derrors      
        /// </summary>
        /// <param name="link">The %LINK% portion in the message will be substituded with a hyperlink</param>
        public void ErrorWithHyperlink(string msg, string linkTxt, string linkUri, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.ColorRed, String.Format(msg, args), linkTxt: linkTxt, linkUri: linkUri, isError: true);
            // InternalAppendFile(p);
            NumberErrors++;
            shortTermStore?.Append(p);
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
            var p = new StoredPrint(StoredPrint.ColorRed, s, isError: true);
            if (ex != null)
                p.stackTrace = ex.StackTrace;
            NumberErrors++;
            Append(p);
        }

        #endregion

    }

}
