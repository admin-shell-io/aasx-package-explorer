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
using BlazorUIWebAssembly;
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

        static object htmlDotnetLock = new object();
        public static bool htmlDotnetEventIn = false;
        public static bool htmlDotnetEventOut = false;
        public static string htmlDotnetEventType = "";
        public static List<object> htmlDotnetEventInputs = new List<object>();
        public static List<object> htmlDotnetEventOutputs = new List<object>();

        public static bool htmlEventIn = false;
        public static bool htmlEventOut = false;
        public static string htmlEventType = "";
        public static List<object> htmlEventInputs = new List<object>();
        public static List<object> htmlEventOutputs = new List<object>();

        public static async void htmlDotnetLoop()
        {
            AnyUiUIElement el;

            bool newData = false;
            while (true)
            {
                //Task.Yield();

                lock (htmlDotnetLock)
                {
                    if (htmlDotnetEventIn)
                    {
                        switch (htmlDotnetEventType)
                        {
                            case "setValueLambda":
                                el = (AnyUiUIElement)htmlDotnetEventInputs[0];
                                object o = htmlDotnetEventInputs[1];
                                htmlDotnetEventInputs.Clear();
                                AnyUiLambdaActionBase ret = el.setValueLambda?.Invoke(o);
                                break;
                            case "contextMenu":
                                el = (AnyUiUIElement)htmlDotnetEventInputs[0];
                                AnyUiSpecialActionContextMenu cntlcm = (AnyUiSpecialActionContextMenu)htmlDotnetEventInputs[1];
                                htmlEventType = "contextMenu";
                                htmlEventInputs.Add(el);
                                htmlEventInputs.Add(cntlcm);
                                htmlEventIn = true;
                                Program.signalNewData(1); // same tree, but structure may change

                                while (!htmlEventOut);
                                int bufferedI = 0;
                                if (htmlEventOutputs.Count == 1)
                                {
                                    bufferedI = (int)htmlEventOutputs[0];
                                    var action2 = cntlcm.MenuItemLambda?.Invoke(bufferedI);
                                }
                                htmlEventOutputs.Clear();
                                htmlEventType = "";
                                htmlEventOut = false;
                                htmlDotnetEventIn = false;

                                htmlDotnetEventInputs.Clear();
                                // AnyUiLambdaActionBase ret = el.setValueLambda?.Invoke(o);
                                break;
                        }
                        while (htmlDotnetEventOut);
                        htmlDotnetEventIn = false;
                        newData = true;
                    }
                }
                if (newData)
                {
                    newData = false;
                    Program.signalNewData(2); // build new tree
                }
                // this is some intesive task, since there is no threading in webasssmebly
                // https://www.meziantou.net/don-t-freeze-ui-while-executing-cpu-intensive-work-in-blazor-webassembly.htm
                await Task.Delay(100);
                
            }
        }

        public static void setValueLambdaHtml(AnyUiUIElement el, object o)
        {
            lock (htmlDotnetLock)
            {
                while (htmlDotnetEventIn);
                htmlDotnetEventType = "setValueLambda";
                htmlDotnetEventInputs.Add(el);
                htmlDotnetEventInputs.Add(o);
                htmlDotnetEventIn = true;
            }
        }

        public static void specialActionContextMenuHtml(AnyUiUIElement el, AnyUiSpecialActionContextMenu cntlcm)
        {
            lock (htmlDotnetLock)
            {
                while (htmlDotnetEventIn);
                htmlDotnetEventType = "contextMenu";
                htmlDotnetEventInputs.Add(el);
                htmlDotnetEventInputs.Add(cntlcm);
                htmlDotnetEventIn = true;
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
            htmlEventType = "MessageBoxFlyoutShow";
            htmlEventInputs.Add(message);
            htmlEventInputs.Add(caption);
            htmlEventInputs.Add(buttons);
            htmlEventIn = true;
            Program.signalNewData(2); // build new tree

            while (!htmlEventOut);
            AnyUiMessageBoxResult r = AnyUiMessageBoxResult.None;
            if (htmlEventOutputs.Count == 1)
                r = (AnyUiMessageBoxResult)htmlEventOutputs[0];
            htmlEventOutputs.Clear();
            htmlEventType = "";
            htmlEventOut = false;
            htmlDotnetEventIn = false;
            return r;
        }
    }
}
