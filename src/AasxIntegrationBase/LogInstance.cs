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

namespace AasxIntegrationBase
{
    /// <summary>
    /// This class is used to convey "print messages", such for logging, over boundaries of modules and .dll's
    /// </summary>
    public class StoredPrint
    {
        public class StatusItem
        {
            /// <summary>
            /// Text describing the status item; giving it a 'name'
            /// </summary>
            public string Name;

            /// <summary>
            /// Short name for compressed screen space; if <c>null</c>, <c>Name</c> will be taken.
            /// </summary>
            public string NameShort;

            /// <summary>
            /// Value of the item in human-readable representation
            /// </summary>
            public string Value;

            public StatusItem(string name = "", string nameShort = null, string value = "")
            {
                Name = name;
                NameShort = nameShort;
                Value = value;
            }
        }

        public enum Color
        {
            Black = 0,
            Blue = 1,
            Red = 2,
            Yellow = 3
        }

        public enum MessageTypeEnum
        {
            Log = 0,
            Error,
            Status
        }

        public MessageTypeEnum MessageType;
        public Color color = Color.Black;
        public bool isError = false;
        public string msg = "";
        public string linkTxt = null;
        public string linkUri = null;
        public string stackTrace = null;
        public StatusItem[] StatusItems = null;

        /// <param name="msg">The complete message; can contain %LINK% as substitute position for link text</param>
        public StoredPrint(string msg)
        {
            this.color = Color.Black;
            this.msg = msg;
        }

        /// <param name="color">Color code black/ blue/ red</param>
        /// <param name="msg">The complete message; can contain %LINK% as substitute position for link text</param>
        /// <param name="linkTxt">Link text to be shown</param>
        /// <param name="linkUri">Link URI to be navigated to</param>
        /// <param name="isError">Represents an error, e.g. will be counted</param>
        /// <param name="stackTrace">string serialized stack trace information</param>
        /// <param name="messageType">Message type, such as <c>Log</c>, <c>Error</c> but also <c>Status</c></param>
        /// <param name="statusItems">In caase of <c>Status</c> array of status items</param>
        public StoredPrint(
            Color color, string msg, string linkTxt = null, string linkUri = null, bool isError = false,
            string stackTrace = null, MessageTypeEnum messageType = MessageTypeEnum.Log,
            StatusItem[] statusItems = null)
        {
            this.MessageType = messageType;
            if (isError)
                this.MessageType = MessageTypeEnum.Error;
            this.isError = isError;

            this.color = color;
            this.msg = msg;
            this.linkTxt = linkTxt;
            this.linkUri = linkUri;
            this.stackTrace = stackTrace;

            this.StatusItems = statusItems;
        }

        public new string ToString()
        {
            return String.Format("{0}:{1} {2}", color, msg, linkTxt);
        }
    }

    public class StoredPrintsMinimalStore
    {
        // private members
        private List<StoredPrint> StoredPrints = new List<StoredPrint>();

        /// <summary>
        /// Pop the oldest messages as string.
        /// </summary>
        public StoredPrint PopLastStoredPrint()
        {
            lock (StoredPrints)
            {
                if (StoredPrints.Count < 1)
                    return null;

                var sp = StoredPrints.First();
                StoredPrints.Remove(sp);
                return sp;
            }
        }

        /// <summary>
        /// Get all stored prints. Does not clear the buffer.
        /// </summary>
        /// <returns></returns>
        public StoredPrint[] GetStoredPrints()
        {
            lock (StoredPrints)
            {
                return StoredPrints.ToArray();
            }
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
    public class LogInstance
    {
        private StoredPrintsMinimalStore shortTermStore = new StoredPrintsMinimalStore();
        private StoredPrintsMinimalStore longTermStore = null;

        // Enable options

        public void EnableLongTermStore()
        {
            longTermStore = new StoredPrintsMinimalStore();
        }

        /// <summary>
        /// Pop the oldest messages as string.
        /// </summary>
        public StoredPrint PopLastShortTermPrint()
        {
            return shortTermStore?.PopLastStoredPrint();
        }

        /// <summary>
        /// Get all stored prints. Does not clear the buffer.
        /// </summary>
        /// <returns></returns>
        public StoredPrint[] GetStoredLongTermPrints()
        {
            return longTermStore?.GetStoredPrints();
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

        #region //////// Append to Log

        /// <summary>
        /// Writes the message to STDERR skipping the both the short-term and the long-term storages.
        /// </summary>
        public void Silent(string msg, params object[] args)
        {
            System.Console.Error.WriteLine(msg, args);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public void Info(string msg, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.Color.Black, String.Format(msg, args));
            Append(p);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public void Info(StoredPrint.Color color, string msg, params object[] args)
        {
            var p = new StoredPrint(color, String.Format(msg, args));
            Append(p);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public void InfoWithHyperlink(int level, string msg, string linkTxt, string linkUri, params object[] args)
        {
            var p = new StoredPrint(
                StoredPrint.Color.Black, String.Format(msg, args), linkTxt: linkTxt, linkUri: linkUri);
            Append(p);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public void Error(string msg, params object[] args)
        {
            var p = new StoredPrint(StoredPrint.Color.Red, String.Format(msg, args), isError: true);
            NumberErrors++;
            Append(p);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public void Error(Exception ex, string where)
        {
            if (ex == null)
                return;

            var s = AdminShellNS.Logging.FormatError(ex, where);

            var p = new StoredPrint(StoredPrint.Color.Red, s, isError: true);
            p.stackTrace = ex.StackTrace;
            NumberErrors++;
            Append(p);
        }

        #endregion
    }
}
