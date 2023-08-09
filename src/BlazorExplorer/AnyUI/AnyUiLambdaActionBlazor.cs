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
using System.Globalization;
using System.Linq;
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

namespace AnyUi
{
    /// <summary>
    /// Request to view to clear the status line
    /// </summary>
    public class AnyUiLambdaActionStatusLineClear : AnyUiLambdaActionBase
    {
    }

    /// <summary>
    /// Request to execute the lambda actions for a context menu
    /// </summary>
    public class AnyUiLambdaActionExecuteSpecialAction : AnyUiLambdaActionBase
    {
        public AnyUiSpecialActionContextMenu SpecialAction;
        public object Arg;
    }

    /// <summary>
    /// Request to execute the set value lambda actions 
    /// </summary>
    public class AnyUiLambdaActionExecuteSetValue : AnyUiLambdaActionBase
    {
        public Func<object, Task<AnyUiLambdaActionBase>> SetValueAsyncLambda = null;
        public object Arg;
    }
}
