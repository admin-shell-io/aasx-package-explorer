/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// resharper disable EmptyEmbeddedStatement
// resharper disable FunctionNeverReturns
// resharper disable UnusedVariable
// resharper disable TooWideLocalVariableScope
// resharper disable EmptyConstructor

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using BlazorUI;
using Newtonsoft.Json;

namespace AnyUi
{
    public class AnyUiDisplayContextHtml : AnyUiContextBase
    {
        [JsonIgnore]
        public PackageCentral Packages;

        public AnyUiDisplayContextHtml()
        {
        }

        public static Thread htmlDotnetThread = new Thread(htmlDotnetLoop);
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

        private static void htmlDotnetLoop()
        {
            AnyUiUIElement el;

            bool newData = false;
            while (true)
            {
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
                                AnyUiSpecialActionContextMenu cntlcm = (AnyUiSpecialActionContextMenu)
                                    htmlDotnetEventInputs[1];
                                htmlEventType = "contextMenu";
                                htmlEventInputs.Add(el);
                                htmlEventInputs.Add(cntlcm);
                                htmlEventIn = true;
                                Program.signalNewData(1); // same tree, but structure may change

                                while (!htmlEventOut) ;
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
                                //// AnyUiLambdaActionBase ret = el.setValueLambda?.Invoke(o);
                                break;
                        }
                        while (htmlDotnetEventOut) ;
                        htmlDotnetEventIn = false;
                        newData = true;
                    }
                }
                if (newData)
                {
                    newData = false;
                    Program.signalNewData(2); // build new tree
                }
                Thread.Sleep(100);
            }
        }

        public static void setValueLambdaHtml(AnyUiUIElement el, object o)
        {
            lock (htmlDotnetLock)
            {
                while (htmlDotnetEventIn) ;
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
                while (htmlDotnetEventIn) ;
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

            while (!htmlEventOut) ;
            AnyUiMessageBoxResult r = AnyUiMessageBoxResult.None;
            if (htmlEventOutputs.Count == 1)
                r = (AnyUiMessageBoxResult)htmlEventOutputs[0];
            htmlEventOutputs.Clear();
            htmlEventType = "";
            htmlEventOut = false;
            htmlDotnetEventIn = false;
            return r;
        }

        /// <summary>
        /// Shows specified dialogue hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// Modal dialogue: this function will block, until user ends dialogue.
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public override bool StartFlyoverModal(AnyUiDialogueDataBase dialogueData)
        {
            // access
            if (dialogueData == null)
                return false;

            // make sure to reset
            dialogueData.Result = false;

            htmlEventType = "StartFlyoverModal";
            htmlEventInputs.Add(dialogueData);
            
            htmlEventIn = true;
            Program.signalNewData(2); // build new tree

            while (!htmlEventOut) ;
            if (htmlEventOutputs.Count == 2)
            {
                if (dialogueData is AnyUiDialogueDataTextEditor ddte)
                {
                    ddte.Text = (string)htmlEventOutputs[0];
                    ddte.Result = (bool)htmlEventOutputs[1];
                }
            }
            htmlEventOutputs.Clear();
            htmlEventType = "";
            htmlEventOut = false;
            htmlDotnetEventIn = false;

            // result
            return dialogueData.Result;
        }
    }
}
