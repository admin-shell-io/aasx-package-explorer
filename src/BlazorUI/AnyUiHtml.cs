using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using AasxIntegrationBase;
using AdminShellNS;
using System.Windows.Input;
using Newtonsoft.Json;
using AasxPackageLogic;
using BlazorUI;
using System.Threading;
using System.Threading.Tasks;

namespace AnyUi
{
    public class AnyUiDisplayContextHtml : AnyUiContextBase
    {
        [JsonIgnore]
        public PackageCentral Packages;

        public AnyUiDisplayContextHtml()
        {
        }

        public static bool htmlEvent = false;
        public static string htmlMessage = "";
        public static string htmlResponse = "";

        public static Thread setValueLambdaThread = new Thread(setValueLambdaLoop);
        public static AnyUiUIElement setValueLambdaElement = null;
        public static object setValueLambdaObject = null;
        static object setValueLambdaLock = new object();
        private static void setValueLambdaLoop()
        {
            while (true)
            {
                lock(setValueLambdaLock)
                {
                    if (setValueLambdaElement != null && setValueLambdaObject != null)
                    {
                        AnyUiUIElement el = setValueLambdaElement;
                        object o = setValueLambdaObject;
                        setValueLambdaElement = null;
                        setValueLambdaObject = null;
                        el.setValueLambda?.Invoke(o);
                        Program.signalNewData();
                    }
                }
                Thread.Sleep(100);
            }
        }

        public static void setValueLambdaAsync(AnyUiUIElement el, object o)
        {
            lock (setValueLambdaLock)
            {
                while ((setValueLambdaElement != null || setValueLambdaObject != null)) ;
                setValueLambdaElement = el;
                setValueLambdaObject = o;
            }
        }

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        public override AnyUiMessageBoxResult MessageBoxFlyoutShow(
        string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            htmlMessage = message;
            htmlEvent = true;
            Program.signalNewData();
            while (htmlResponse == "");
            string response = htmlResponse;
            htmlResponse = "";
            if (response.ToLower() != "yes")
                return AnyUiMessageBoxResult.Cancel;
            return AnyUiMessageBoxResult.Yes;
        }
    }
}
