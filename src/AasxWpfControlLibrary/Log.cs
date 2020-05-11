using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxGlobalLogging
{
    /// <summary>
    /// Static class, wrapping log instance, to have a logging via Singleton.
    /// (no need to have Log instance in every single class)
    /// </summary>
    public class Log 
    {
        private static AasxIntegrationBase.LogInstance logInstance = new AasxIntegrationBase.LogInstance();

        public static AasxIntegrationBase.LogInstance LogInstance { get { return logInstance; } }

        /// <summary>
        /// Only append to longterm or to file, if file name is set
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
        public static void Info(int level, string msg, params object[] args)
        {
            logInstance?.Info(level, msg, args);
        }

        /// <summary>
        /// Display a message, which is for information only
        /// </summary>
        public static void Info(int level, int color, string msg, params object[] args)
        {
            logInstance?.Info(level, color, msg, args);
        }

        /// <summary>
        /// Display a message, which is for information only        
        /// </summary>
        /// <param name="link">The %LINK% portion in the message will be substituded with a hyperlink</param>
        public static void InfoWithHyperlink(int level, string msg, string linkTxt, string linkUri, params object[] args)
        {
            logInstance?.InfoWithHyperlink(level, msg, linkTxt, linkUri, args);
        }

        /// <summary>
        /// Display a message, which is for errors
        /// </summary>
        public static void Error(string msg, params object[] args)
        {
            logInstance?.Error(msg, args);
        }

        /// <summary>
        /// Display a message, which is for derrors      
        /// </summary>
        /// <param name="link">The %LINK% portion in the message will be substituded with a hyperlink</param>
        public static void ErrorWithHyperlink(string msg, string linkTxt, string linkUri, params object[] args)
        {
            logInstance?.ErrorWithHyperlink(msg, linkTxt, linkUri, args);
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
