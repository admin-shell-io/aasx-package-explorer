/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using BlazorExplorer;
using BlazorUI.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using VDS.RDF.Query.Algebra;

namespace AnyUi
{

    public enum AnyUiHtmlBackgroundActionType { None, Dialog, ContextMenu, SetValue }

    public class AnyUiHtmlEventSession
    {
        /// <summary>
        /// The session id matches the one in the BlazorSession.
        /// </summary>
        public int SessionId = 0;

        /// <summary>
        /// JavaScript runtime to call scripts.
        /// </summary>
		public IJSRuntime JsRuntime;

        /// <summary>
        /// Distincts the different working mode of display and
        /// execution of the event loop.
        /// </summary>
        public AnyUiHtmlBackgroundActionType BackgroundAction;

        /// <summary>
        /// If <c>true</c>, a special HTML dialog rendering will occur for
        /// the event.
        /// </summary>
        public bool EventOpen;

        /// <summary>
        /// Will be raised if the modal event shall be closed.
        /// </summary>
        public bool EventDone;

        /// <summary>
        /// If a dialog shall be executed, dialogue data
        /// </summary>
        public AnyUiDialogueDataBase DialogueData;

        /// If a special action shall be executed, data to it
        /// </summary>
        public AnyUiSpecialActionBase SpecialAction;

        /// <summary>
        /// There is still a chance of race condition in modal dialogs.
        /// This hold the last time a modal dialog was deisplayed.
        /// </summary>
        protected DateTime _lastTimeOfModalClose = DateTime.Now;

        // old stuff

        public bool htmlDotnetEventIn = false;
        public bool htmlDotnetEventOut = false;
        public string htmlDotnetEventType = "";
        public List<object> htmlDotnetEventInputs = new List<object>();
        public List<object> htmlDotnetEventOutputs = new List<object>();

        public bool htmlEventIn = false;
        public bool htmlEventOut = false;
        public string htmlEventType = "";
        public List<object> htmlEventInputs = new List<object>();
        public List<object> htmlEventOutputs = new List<object>();

        /// <summary>
        /// Denotes, which event type leads to a special HTML rendering.
        /// </summary>
        // dead-csharp off
        // public AnyUiHtmlEventType EventType;
        // dead-csharp on

        // constructor: guarantee some data

        public AnyUiHtmlEventSession(int sessionId)
        {
            SessionId = sessionId;
        }

        // service function

        /// <summary>
        /// Notify closing of modal dialog, for book (time) keeping.
        /// </summary>
        public void NotifyModalClose()
        {
            _lastTimeOfModalClose = DateTime.Now;
        }

        /// <summary>
        /// Wait for a certain time before (again) opening modal dialog
        /// This function is blocking; try to avoid by using Async()
        /// </summary>
        public void WaitMinimumForModalOpen()
        {
            while ((DateTime.Now - _lastTimeOfModalClose).TotalMilliseconds < 1500)
            {
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Wait for a certain time before (again) opening modal dialog
        /// </summary>
        public async Task WaitMinimumForModalOpenAsync()
        {
            while ((DateTime.Now - _lastTimeOfModalClose).TotalMilliseconds < 1500)
            {
                await Task.Delay(50);
            }
        }

        /// <summary>
        /// Will set all important flags in a way, that the event can be executed.
        /// </summary>
        /// <param name="diaData"></param>
        public T StartModal<T>(T diaData) where T : AnyUiDialogueDataBase
        {
            if (diaData == null)
            {
                EventOpen = false;
                EventDone = true;
                return diaData;
            }

            EventOpen = true;
            DialogueData = diaData;
            SpecialAction = null;
            EventDone = false;
            return diaData;
        }

        public void ResetModal()
        {
            DialogueData = null;
            SpecialAction = null;
            EventOpen = false;
            EventDone = false;
        }

        public T StartModalSpecialAction<T>(T sdData) where T : AnyUiSpecialActionBase
        {
            ResetModal();

            if (sdData == null)
            {
                EventDone = true;
                return null;
            }

            EventOpen = true;
            SpecialAction = sdData;
            return sdData;
        }

        public void EndModal(bool result)
        {
            if (DialogueData != null)
                DialogueData.Result = result;
            ResetModal();
            EventDone = true;
        }
    }

    public enum AnyUiHtmlFillMode { None, FillWidth }

    public class AnyUiDisplayDataHtml : AnyUiDisplayDataBase
    {
        [JsonIgnore]
        public AnyUiDisplayContextHtml _context;

        [JsonIgnore]
        public ComponentBase _component;

        [JsonIgnore]
        public Action<AnyUiUIElement> TouchLambda;

        public AnyUiDisplayDataHtml(
            AnyUiDisplayContextHtml context,
            Action<AnyUiUIElement> touchLambda = null)
        {
            _context = context;
            if (touchLambda != null)
                TouchLambda = touchLambda;
        }

        public double GetScale() => 1.2;

        public double ScaleToPixel(double anyUiPx)
        {
            return 1.20f * anyUiPx;
        }

        public static bool DebugFrames = false;
    }

    public class AnyUiDisplayContextHtml : AnyUiContextPlusDialogs
    {
        [JsonIgnore]
        public PackageCentral Packages;

        [JsonIgnore]
        public BlazorUI.Data.BlazorSession _bi;

        [JsonIgnore]
        public IJSRuntime _jsRuntime;

        public AnyUiDisplayContextHtml(
            PackageCentral packageCentral,
            BlazorUI.Data.BlazorSession bi)
        {
            _bi = bi;
            Packages = packageCentral;
        }

        public override IEnumerable<AnyUiContextCapability> EnumCapablities()
        {
            yield return AnyUiContextCapability.Blazor;
        }

        public void SetJsRuntime(IJSRuntime runtime)
        {
            _jsRuntime = runtime;
        }

        object htmlDotnetLock = new object();
        static object staticHtmlDotnetLock = new object();
        public static List<AnyUiHtmlEventSession> sessions = new List<AnyUiHtmlEventSession>();

        public static void AddEventSession(int sessionId)
        {
            lock (staticHtmlDotnetLock)
            {
                var s = new AnyUiHtmlEventSession(sessionId);
                sessions.Add(s);
            }
        }

        public static void DeleteEventSession(int sessionNumber)
        {
            lock (staticHtmlDotnetLock)
            {
                AnyUiHtmlEventSession found = null;
                foreach (var s in sessions)
                {
                    if (s.SessionId == sessionNumber)
                    {
                        found = s;
                        break;
                    }
                }
                if (found != null)
                    sessions.Remove(found);
            }
        }

        public static AnyUiHtmlEventSession FindEventSession(int sessionNumber)
        {
            AnyUiHtmlEventSession found = null;
            lock (staticHtmlDotnetLock)
            {
                foreach (var s in sessions)
                {
                    if (s.SessionId == sessionNumber)
                    {
                        found = s;
                        break;
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// This very special suspicious double-mangled infinite loop serves as a context
        /// for dedicated event loops aside from the general Blazor event loop,
        /// that is: context menu, execution of value lambdas.
        /// This function seems to cyclically scan the event loop "sessions" and will
        /// "lock on" an active one and do some modal things ..
        /// TODO (??, 0000-00-00): Check if this would work with multiple users (!!!)
        /// </summary>
        public static void htmlDotnetLoop()
        {
            AnyUiUIElement el;

            while (true)
            {
                // ReSharper disable InconsistentlySynchronizedField
                int i = 0;
                while (i < sessions.Count)
                {
                    var s = sessions[i];

                    // dead-csharp off
                    //if (s.htmlDotnetEventIn)
                    //{
                    //    switch (s.htmlDotnetEventType)
                    //    {
                    //        case "setValueLambda":
                    //            if (s.htmlDotnetEventInputs != null && s.htmlDotnetEventInputs.Count > 0)
                    //            {
                    //                el = (AnyUiUIElement)s.htmlDotnetEventInputs[0];
                    //                object o = s.htmlDotnetEventInputs[1];
                    //                s.htmlDotnetEventIn = false;
                    //                s.htmlDotnetEventInputs.Clear();
                    //                var ret = el?.setValueLambda?.Invoke(o);

                    //                while (s.htmlDotnetEventOut) Task.Delay(1);

                    //                // determine, which (visual) update has to be done
                    //                int ndm = 2;
                    //                if (ret is AnyUiLambdaActionNone)
                    //                    ndm = 0;
                    //                Program.signalNewData(
                    //                    new Program.NewDataAvailableArgs(
                    //                        Program.DataRedrawMode.SomeStructChange, s.SessionId, 
                    //                        newLambdaAction: ret, onlyUpdatePanel: true)); // build new tree
                    //            }
                    //            break;

                    //        case "contextMenu":
                    //            if (s.htmlDotnetEventInputs != null && s.htmlDotnetEventInputs.Count > 0)
                    //            {
                    //                el = (AnyUiUIElement)s.htmlDotnetEventInputs[0];
                    //                AnyUiSpecialActionContextMenu cntlcm = (AnyUiSpecialActionContextMenu)
                    //                    s.htmlDotnetEventInputs[1];
                    //                s.htmlEventType = "contextMenu";
                    //                s.htmlEventInputs.Add(el);
                    //                s.htmlEventInputs.Add(cntlcm);
                    //                s.htmlDotnetEventIn = false;
                    //                s.htmlDotnetEventInputs.Clear();
                    //                s.htmlEventIn = true;
                    //                Program.signalNewData(
                    //                    new Program.NewDataAvailableArgs(Program.DataRedrawMode.SomeStructChange, s.SessionId,
                    //                    onlyUpdatePanel: true)); // same tree, but structure may change

                    //                while (!s.htmlEventOut) Task.Delay(1);
                    //                int bufferedI = 0;
                    //                if (s.htmlEventOutputs.Count == 1)
                    //                {
                    //                    bufferedI = (int)s.htmlEventOutputs[0];
                    //                    var action2 = cntlcm.MenuItemLambda?.Invoke(bufferedI);
                    //                }
                    //                s.htmlEventOutputs.Clear();
                    //                s.htmlEventType = "";
                    //                s.htmlEventOut = false;
                    //                //// AnyUiLambdaActionBase ret = el.setValueLambda?.Invoke(o);
                    //            }
                    //            break;
                    //    }
                    //}
                    // dead-csharp on

                    if (s.BackgroundAction == AnyUiHtmlBackgroundActionType.ContextMenu
                        && s.SpecialAction is AnyUiSpecialActionContextMenu sacm)
                    {
                        // assumption: context menu already on the screen
                        if (!s.EventOpen)
                        {
                            // emergency exit
                            s.JsRuntime?.InvokeVoidAsync("blazorCloseModalForce");
                            return;
                        }

                        // simply do event loop (but here in the "background")
                        while (!s.EventDone)
                            Thread.Sleep(1);

                        s.EventOpen = false;
                        s.EventDone = false;
                        s.BackgroundAction = AnyUiHtmlBackgroundActionType.None;
                        s.SpecialAction = null;

                        s.JsRuntime?.InvokeVoidAsync("blazorCloseModalForce");

                        // remember close
                        s.NotifyModalClose();

                        // directly concern about the results
                        if (sacm.ResultIndex >= 0)
                        {
                            int bufferedI = sacm.ResultIndex;
                            var action2 = sacm.MenuItemLambda?.Invoke(bufferedI);
                            if (action2 == null && sacm.MenuItemLambdaAsync != null)
                            {
                                Program.signalNewData(
                                    new Program.NewDataAvailableArgs(
                                        Program.DataRedrawMode.None,
                                        s.SessionId,
                                        newLambdaAction: new AnyUiLambdaActionExecuteSpecialAction()
                                        {
                                            SpecialAction = sacm,
                                            Arg = bufferedI
                                        },
                                        onlyUpdatePanel: true));
                            }
                        }
                    }

                    if (s.BackgroundAction == AnyUiHtmlBackgroundActionType.SetValue
                        && s.SpecialAction is AnyUiSpecialActionSetValue sasv)
                    {
                        // reset everything
                        s.EventOpen = false;
                        s.EventDone = false;
                        s.BackgroundAction = AnyUiHtmlBackgroundActionType.None;
                        s.SpecialAction = null;

                        // notify close
                        s.NotifyModalClose();

                        // how to proceed?
                        if (sasv.UiElement?.setValueLambda != null)
                        {
                            // directly here .. execute lambda
                            var ret = sasv.UiElement.setValueLambda?.Invoke(sasv.Argument);

                            // not required?!

                            // trigger handling of lambda return
                            Program.signalNewData(
                                new Program.NewDataAvailableArgs(
                                        Program.DataRedrawMode.SomeStructChange, s.SessionId,
                                        newLambdaAction: ret, onlyUpdatePanel: true));
                        }
                        else
                        if (sasv.UiElement?.setValueAsyncLambda != null)
                        {
                            // refer to Blazor single contact point
                            Program.signalNewData(
                                    new Program.NewDataAvailableArgs(
                                        Program.DataRedrawMode.None,
                                        s.SessionId,
                                        newLambdaAction: new AnyUiLambdaActionExecuteSetValue()
                                        {
                                            SetValueAsyncLambda = sasv.UiElement.setValueAsyncLambda,
                                            Arg = sasv.Argument
                                        },
                                        onlyUpdatePanel: true));
                        }
                    }

                    i++;
                }

                // ReSharper enable InconsistentlySynchronizedField
                Thread.Sleep(50);
            }
        }

        public static void setValueLambdaHtml(AnyUiUIElement uiElement, object argument)
        {
            var dc = (uiElement?.DisplayData as AnyUiDisplayDataHtml)?._context;
            if (dc != null)
            {
                var sessionNumber = dc._bi.SessionId;
                var evs = FindEventSession(sessionNumber);
                if (evs != null)
                {
                    lock (dc.htmlDotnetLock)
                    {
                        // dead-csharp off
                        //while (found.htmlDotnetEventIn) Task.Delay(1);
                        //found.htmlEventInputs.Clear();
                        //found.htmlDotnetEventType = "setValueLambda";
                        //found.htmlDotnetEventInputs.Add(el);
                        //found.htmlDotnetEventInputs.Add(o);
                        //found.htmlDotnetEventIn = true;
                        // dead-csharp on
                        // simply treat woodoo as error!
                        evs.JsRuntime = dc._jsRuntime;

                        // temporarily deactivated (develop modal panel)
                        if (evs.EventOpen)
                        {
                            Log.Singleton.Error("Error in starting special action as some modal dialogue " +
                                "is still active. Aborting!!");
                            return;
                        }

                        evs.BackgroundAction = AnyUiHtmlBackgroundActionType.SetValue;
                        evs.ResetModal();
                        evs.SpecialAction = new AnyUiSpecialActionSetValue(uiElement, argument);
                    }
                }
            }
        }

        public static void specialActionContextMenuHtml(
            AnyUiUIElement el,
            AnyUiSpecialActionContextMenu cntlcm,
            AnyUiDisplayContextHtml context)
        {
            var dc = (el.DisplayData as AnyUiDisplayDataHtml)?._context;
            if (dc != null)
            {
                var sessionNumber = dc._bi.SessionId;
                var evs = FindEventSession(sessionNumber);
                if (evs != null)
                {
                    lock (dc.htmlDotnetLock)
                    {
                        // dead-csharp off
                        // woodoo? -> wait for "old" dislogs????
                        // while (/* evs.htmlDotnetEventIn || evs.EventOpen) Task.Delay(1);

                        // start new thing
                        //evs.htmlEventInputs.Clear();
                        //evs.htmlDotnetEventType = "contextMenu";
                        //evs.htmlDotnetEventInputs.Add(el);
                        //evs.htmlDotnetEventInputs.Add(cntlcm);
                        //evs.htmlDotnetEventIn = true;
                        // dead-csharp on
                        // simply treat woodoo as error!
                        evs.JsRuntime = dc._jsRuntime;

                        // prepare
                        if (evs.EventOpen)
                        {
                            Log.Singleton.Error("Error in starting special action as some modal dialogue " +
                                "is still active. Aborting!!");
                            return;
                        }

                        // wait?
                        evs.WaitMinimumForModalOpen();

                        // start modal
                        evs.BackgroundAction = AnyUiHtmlBackgroundActionType.ContextMenu;
                        evs.StartModalSpecialAction(cntlcm);

                        // trigger display
                        Program.signalNewData(
                            new Program.NewDataAvailableArgs(
                                Program.DataRedrawMode.None, evs.SessionId));
                        // dead-csharp off
                        // wait modal
                        //while (!evs.EventDone) Task.Delay(1);
                        //evs.EventOpen = false;
                        //evs.EventDone = false;

                        // trigger display (again)
                        //Program.signalNewData(
                        //    new Program.NewDataAvailableArgs(
                        //        Program.DataRedrawMode.None, evs.SessionId));
                        // context?._jsRuntime.InvokeVoidAsync("blazorCloseModalForce");
                        // dead-csharp on
                    }

                }
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
            AnyUiMessageBoxResult r = AnyUiMessageBoxResult.None;
            var evs = FindEventSession(_bi.SessionId);
            if (evs == null)
                return r;

            evs.WaitMinimumForModalOpen();

            var dd = evs.StartModal(new AnyUiDialogueDataMessageBox(
                caption, message, buttons, image));


            // trigger display
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, evs.SessionId));

            // wait modal
            while (!evs.EventDone) Thread.Sleep(1);
            evs.EventDone = false;

            // dialog result
            if (dd.Result)
                r = dd.ResultButton;


            return r;
        }

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        public async override Task<AnyUiMessageBoxResult> MessageBoxFlyoutShowAsync(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            AnyUiMessageBoxResult r = AnyUiMessageBoxResult.None;
            var evs = FindEventSession(_bi.SessionId);
            if (evs == null)
                return r;

            await evs.WaitMinimumForModalOpenAsync();

            var dd = evs.StartModal(new AnyUiDialogueDataMessageBox(
                caption, message, buttons, image));

            // trigger display
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, evs.SessionId));

            // wait modal
            while (!evs.EventDone)
            {
                await Task.Delay(1);
            }

            evs.EventOpen = false;
            evs.EventDone = false;

            // make sure its closed
            await _jsRuntime.InvokeVoidAsync("blazorCloseModalForce");

            // notify
            evs.NotifyModalClose();

            // dialog result
            if (dd.Result)
                r = dd.ResultButton;

            return r;
        }

        /// <summary>
        /// Shows an open file dialogue
        /// </summary>
        /// <param name="caption">Top caption of the dialogue</param>
        /// <param name="message">Further information to the user</param>
        /// <param name="filter">Filter specification for certain file extension.</param>
        /// <param name="proposeFn">Filename, which is initially proposed when the dialogue is opened.</param>
        /// <returns>Dialogue data including filenames</returns>
        public override AnyUiDialogueDataOpenFile OpenFileFlyoutShow(
            string caption,
            string message,
            string proposeFn = null,
            string filter = null)
        {
            var evs = FindEventSession(_bi.SessionId);
            if (evs == null)
                return null;

            evs.WaitMinimumForModalOpen();

            var dd = evs.StartModal(new AnyUiDialogueDataOpenFile(
                caption: caption,
                message: message,
                filter: filter,
                proposeFn: proposeFn));

            // trigger display
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, evs.SessionId));

            // wait modal
            while (!evs.EventDone) Thread.Sleep(1);
            evs.EventDone = false;

            // dialog result
            if (dd.Result)
                return dd;
            return null;
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
            var evs = FindEventSession(_bi.SessionId);
            if (dialogueData == null || evs == null)
                return false;

            evs.WaitMinimumForModalOpen();

            var dd = evs.StartModal(dialogueData);

            // trigger display
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, evs.SessionId));

            // wait modal
            while (!evs.EventDone) Task.Delay(1);
            evs.EventOpen = false;
            evs.EventDone = false;

            _jsRuntime.InvokeVoidAsync("blazorCloseModalForce");

            evs.NotifyModalClose();

            return dd.Result;
            // dead-csharp off
            //// access
            //if (dialogueData == null)
            //    return false;

            //// make sure to reset
            //dialogueData.Result = false;

            //AnyUiHtmlEventSession found = null;
            //lock (htmlDotnetLock)
            //{
            //    foreach (var s in sessions)
            //    {
            //        if (_bi.SessionId == s.SessionId)
            //        {
            //            found = s;
            //            break;
            //        }
            //    }
            //}

            //if (found != null)
            //{
            //    found.htmlEventInputs.Clear();
            //    found.htmlEventType = "StartFlyoverModal";
            //    found.htmlEventInputs.Add(dialogueData);

            //    found.htmlEventIn = true;
            //    Program.signalNewData(
            //        new Program.NewDataAvailableArgs(
            //            Program.DataRedrawMode.RebuildTreeKeepOpen, found.SessionId)); // build new tree

            //    while (!found.htmlEventOut) Task.Delay(1);
            //    if (dialogueData is AnyUiDialogueDataTextEditor ddte)
            //    {
            //        if (found.htmlEventOutputs.Count == 2)
            //        {
            //            ddte.Text = (string)found.htmlEventOutputs[0];
            //            ddte.Result = (bool)found.htmlEventOutputs[1];
            //        }
            //    }
            //    if (dialogueData is AnyUiDialogueDataSelectFromList ddsfl)
            //    {
            //        ddsfl.Result = false;
            //        if (found.htmlEventOutputs.Count == 1)
            //        {
            //            int iDdsfl = (int)found.htmlEventOutputs[0];
            //            ddsfl.Result = true;
            //            ddsfl.ResultIndex = iDdsfl;
            //            ddsfl.ResultItem = ddsfl.ListOfItems[iDdsfl];
            //        }
            //    }
            //    found.htmlEventType = "";
            //    found.htmlEventOutputs.Clear();
            //    found.htmlEventOut = false;
            //    found.htmlEventInputs.Clear();
            //    found.htmlDotnetEventIn = false;
            //}
            //// result
            //return dialogueData.Result;
            // dead-csharp om
        }

        public async override Task<bool> StartFlyoverModalAsync(AnyUiDialogueDataBase dialogueData, Action rerender = null)
        {
            var evs = FindEventSession(_bi.SessionId);
            if (dialogueData == null || evs == null)
                return false;

            await evs.WaitMinimumForModalOpenAsync();

            var dd = evs.StartModal(dialogueData);

            // trigger display
            if (rerender != null)
            {
                rerender.Invoke();
            }
            else
            {
                Program.signalNewData(
                    new Program.NewDataAvailableArgs(
                        Program.DataRedrawMode.None, evs.SessionId));
            }

            // wait modal
            while (!evs.EventDone)
            {
                await Task.Delay(1);
            }

            evs.EventOpen = false;
            evs.EventDone = false;

            // make sure its closed
            await _jsRuntime.InvokeVoidAsync("blazorCloseModalForce");

            // notify
            evs.NotifyModalClose();

            return dd.Result;
        }

        /// <summary>
        /// Think, this is deprecated
        /// </summary>
        /// <param name="el"></param>
        /// <param name="mode"></param>
        public void UpdateRenderElements(AnyUiUIElement el, AnyUiRenderMode mode)
        {
            //
            // access
            //
            if (el == null)
                return;

            //
            // recurse
            //

            if (el is AnyUi.IEnumerateChildren ien)
                foreach (var elch in ien.GetChildren())
                    UpdateRenderElements(elch, mode: mode);

        }

        public override void ClipboardSet(AnyUiClipboardData cb)
        {
            // see: https://www.meziantou.net/copying-text-to-clipboard-in-a-blazor-application.htm

            if (_jsRuntime != null && cb != null)
            {
                try
                {
                    _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", cb.Text);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
        }

        public override AnyUiClipboardData ClipboardGet()
        {
            // see: https://stackoverflow.com/questions/75472912/how-do-i-read-the-clipboard-with-blazor

            if (_jsRuntime != null)
            {
                try
                {
                    var str = _jsRuntime.InvokeAsync<string>("navigator.clipboard.readText").GetAwaiter().GetResult();
                    if (str != null)
                        return new AnyUiClipboardData(str);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            return null;
        }

        //
        // Convenience services
        //

        /// <summary>
        /// Selects a filename to read either from user or from ticket.
        /// </summary>
        /// <returns>The dialog data containing the filename or <c>null</c></returns>
        public async override Task<AnyUiDialogueDataOpenFile> MenuSelectOpenFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            bool requireNoFlyout = false)
        {
            // filename
            var sourceFn = ticket?[argName] as string;

            // prepare
            var uc = new AnyUiDialogueDataOpenFile(
                    caption: caption,
                    message: "Select filename by uploading it or from stored user files.",
                    filter: filter, proposeFn: proposeFn);
            uc.AllowUserFiles = PackageContainerUserFile.CheckForUserFilesPossible();

            // no direct queries
            if (sourceFn?.HasContent() != true && requireNoFlyout)
            {
                uc.Result = false;
                return uc;
            }

            // ok, modal?
            if (sourceFn?.HasContent() != true)
            {
                if (await StartFlyoverModalAsync(uc))
                {
                    // house keeping
                    RememberForInitialDirectory(uc.TargetFileName);

                    // modify
                    if (uc.ResultUserFile)
                        uc.TargetFileName = PackageContainerUserFile.Scheme + uc.TargetFileName;

                    // ok
                    return uc;
                }
            }

            if (sourceFn?.HasContent() != true)
            {
                MainWindowLogic.LogErrorToTicketOrSilentStatic(ticket, msg);
                uc.Result = false;
                return uc;
            }

            return new AnyUiDialogueDataOpenFile()
            {
                OriginalFileName = sourceFn,
                TargetFileName = sourceFn
            };
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public async override Task<bool> MenuSelectOpenFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            var uc = await MenuSelectOpenFilenameAsync(ticket, argName, caption, proposeFn, filter, msg);
            if (uc?.Result == true)
            {
                ticket[argName] = uc.TargetFileName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects a filename to write either from user or from ticket.
        /// </summary>
        /// <returns>The dialog data containing the filename or <c>null</c></returns>
        public async override Task<AnyUiDialogueDataSaveFile> MenuSelectSaveFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            bool requireNoFlyout = false,
            bool reworkSpecialFn = false)
        {
            // filename
            var targetFn = ticket?[argName] as string;

            // prepare
            var uc = new AnyUiDialogueDataSaveFile(
                    caption: caption,
                    message: "Select filename and how to provide the file. " +
                    "It might be possible to store files " +
                    "as user file or on a local file system.",
                    filter: filter, proposeFn: proposeFn);

            uc.AllowUserFiles = PackageContainerUserFile.CheckForUserFilesPossible();
            uc.AllowLocalFiles = Options.Curr.AllowLocalFiles;

            // no direct queries
            if (targetFn?.HasContent() != true && requireNoFlyout)
            {
                uc.Result = false;
                return uc;
            }

            // ok, modal?
            if (targetFn?.HasContent() != true)
            {
                if (await StartFlyoverModalAsync(uc))
                {
                    // house keeping
                    RememberForInitialDirectory(uc.TargetFileName);

                    // maybe rework?
                    if (reworkSpecialFn)
                        MainWindowAnyUiDialogs.SaveFilenameReworkTargetFilename(uc);

                    // ok
                    return uc;
                }
            }

            if (targetFn?.HasContent() != true)
            {
                MainWindowLogic.LogErrorToTicketOrSilentStatic(ticket, msg);
                uc.Result = false;
                return uc;
            }

            return new AnyUiDialogueDataSaveFile()
            {
                TargetFileName = targetFn
            };
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public async override Task<bool> MenuSelectSaveFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            string argFilterIndex = null,
            string argLocation = null,
            bool reworkSpecialFn = false)
        {
            var uc = await MenuSelectSaveFilenameAsync(
                ticket, argName, caption, proposeFn, filter, msg, reworkSpecialFn: reworkSpecialFn);

            if (uc.Result && uc.TargetFileName.HasContent())
            {
                ticket[argName] = uc.TargetFileName;
                if (argFilterIndex?.HasContent() == true)
                    ticket[argFilterIndex] = uc.FilterIndex;
                if (argLocation?.HasContent() == true)
                    ticket[argLocation] = uc.Location.ToString();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public async override Task<AnyUiDialogueDataTextBox> MenuSelectTextAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            // filename
            var targetText = ticket?[argName] as string;

            if (targetText?.HasContent() != true)
            {
                var uc = new AnyUiDialogueDataTextBox(caption, symbol: AnyUiMessageBoxImage.Question);
                uc.Text = proposeText;
                await StartFlyoverModalAsync(uc);
                if (uc.Result)
                    targetText = uc.Text;
            }

            if (targetText?.HasContent() != true)
            {
                MainWindowLogic.LogErrorToTicketOrSilentStatic(ticket, msg);
                return null;
            }

            return new AnyUiDialogueDataTextBox()
            {
                Text = targetText
            };
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public async override Task<bool> MenuSelectTextToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            var uc = await MenuSelectTextAsync(ticket, argName, caption, proposeText, msg);
            if (uc.Result)
            {
                ticket[argName] = uc.Text;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public override async Task<AnyUiDialogueDataLogMessage> MenuExecuteSystemCommand(
            string caption,
            string workDir,
            string cmd,
            string args)
        {
            // create dialogue
            var uc = new AnyUiDialogueDataLogMessage(caption);

            // create logger
            Process proc = null;
            var logError = false;
            var logBuffer = new List<StoredPrint>();

            // wrap to track errors
            try
            {
                // start
                lock (logBuffer)
                {
                    logBuffer.Add(new StoredPrint(StoredPrint.Color.Black,
                        "Starting in " + workDir + " : " + cmd + " " + args + " .."));
                };

                // start process??
                proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = cmd;
                proc.StartInfo.Arguments = args;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;
                proc.StartInfo.WorkingDirectory = workDir;

                // see: https://stackoverflow.com/questions/1390559/
                // how-to-get-the-output-of-a-system-diagnostics-process

                // see: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.beginoutputreadline?
                // view=net-7.0&redirectedfrom=MSDN#System_Diagnostics_Process_BeginOutputReadLine

                uc.CheckForLogAndEnd = () =>
                {
                    StoredPrint[] msgs = null;
                    lock (logBuffer)
                    {
                        if (logBuffer.Count > 0)
                        {
                            foreach (var sp in logBuffer)
                                Log.Singleton.Append(sp);

                            msgs = logBuffer.ToArray();
                            logBuffer.Clear();
                        }
                    };
                    return new Tuple<object[], bool>(msgs, !logError && proc != null && proc.HasExited);
                };

                proc.OutputDataReceived += (s1, e1) =>
                {
                    var msg = e1.Data;
                    if (msg?.HasContent() == true)
                        lock (logBuffer)
                        {
                            logBuffer.Add(new StoredPrint(StoredPrint.Color.Black, "" + msg));
                        };
                };

                proc.ErrorDataReceived += (s2, e2) =>
                {
                    var msg = e2.Data;
                    if (msg?.HasContent() == true)
                        lock (logBuffer)
                        {
                            logError = true;
                            logBuffer.Add(new StoredPrint(StoredPrint.Color.Red, "" + msg));
                        };
                };

                proc.Exited += (s3, e3) =>
                {
                    lock (logBuffer)
                    {
                        logBuffer.Add(new StoredPrint(StoredPrint.Color.Black, "Done."));
                    };
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await StartFlyoverModalAsync(uc);
            }
            catch (Exception ex)
            {
                // mirror exception to inside and outside
                lock (logBuffer)
                {
                    logError = true;
                    logBuffer.Add(new StoredPrint(StoredPrint.Color.Red, "" + ex.Message));
                }
                Log.Singleton.Error(ex, "executing system command");
            }

            return uc;
        }

        /// <summary>
        /// The display context tells, if user files are allowable for the application
        /// </summary>
        public override bool UserFilesAllowed()
        {
            return true;
        }

        /// <summary>
        /// The display context tells, if web browser services are allowable for the application.
        /// These are: download files, open new browser window ..
        /// </summary>
        public override bool WebBrowserServicesAllowed()
        {
            return _jsRuntime != null;
        }

        /// <summary>
        /// Initiates in the web browser the display or download of file content
        /// </summary>
        public override async Task WebBrowserDisplayOrDownloadFile(string fn, string mimeType = null)
        {
            if (_jsRuntime == null)
                return;
            await BlazorUI.Utils.BlazorUtils.DisplayOrDownloadFile(_jsRuntime, fn, mimeType);
        }

        public double ApproxFontWidth(string str, double left, double right, double? minWdidth = null)
        {
            double tl = 8 * str.Length;
            if (minWdidth.HasValue)
                tl = Math.Max(tl, minWdidth.Value);

            return tl + left + right;
        }

        public double GetApproxElementWidth(AnyUiUIElement elem)
        {
            // access
            if (elem == null)
                return 0.0f;

            //
            // simple types
            //
            if (elem is AnyUiLabel lab)
                return ApproxFontWidth(lab.Content, 0.0, 0.0);
            else
            if (elem is AnyUiSelectableTextBlock stb)
                return ApproxFontWidth(stb.Text, 0.0, 0.0);
            else
            if (elem is AnyUiTextBlock tbl)
                return ApproxFontWidth(tbl.Text, 0.0, 0.0);
            else
            if (elem is AnyUiTextBox tbx)
                return ApproxFontWidth(tbx.Text, 4.0, 4.0, tbx.MinWidth);
            else
            if (elem is AnyUiHintBubble hb)
                return ApproxFontWidth(hb.Text, 4.0, 4.0);
            else
            if (elem is AnyUiComboBox com)
                return ApproxFontWidth(com.Text, 4.0, 4.0, com.MinWidth);
            else
            if (elem is AnyUiCheckBox cbx)
                return ApproxFontWidth(cbx.Content, 14.0, 4.0, cbx.MinWidth);
            else
            if (elem is AnyUiButton btn)
                return ApproxFontWidth(btn.Content, 8.0, 8.0, btn.MinWidth);
            else
            //
            // Panels
            //
            if (elem is AnyUiGrid grid)
            {
                var maxdim = grid.GetMaxRowCol();
                double sumOfColMax = 0.0;
                for (int c = 0; c < maxdim.Item2; c++)
                {
                    double colMax = 0.0;
                    for (int r = 0; r < maxdim.Item1; r++)
                        foreach (var ch in grid.GetChildsAt(r, c))
                        {
                            var aw = GetApproxElementWidth(ch);
                            if (aw > colMax)
                                colMax = aw;
                        }
                    sumOfColMax += colMax;
                }
                return sumOfColMax;
            }
            else
            if (elem is AnyUiStackPanel stack)
            {
                if (stack.Orientation == AnyUiOrientation.Vertical)
                {
                    // maximum of all widths
                    double mx = 0.0;
                    if (stack.Children != null)
                        foreach (var ch in stack.Children)
                            mx = Math.Max(mx, GetApproxElementWidth(ch));
                    return mx;
                }

                if (stack.Orientation == AnyUiOrientation.Horizontal)
                {
                    // sum of all widths
                    double sum = 0.0;
                    if (stack.Children != null)
                        foreach (var ch in stack.Children)
                            sum += GetApproxElementWidth(ch);
                    return sum;
                }
            }

            //
            // None of these
            //
            return 0.0;
        }
    }
}
