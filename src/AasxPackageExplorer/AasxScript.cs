/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Scripting.SSharp.Runtime;
using Scripting.SSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageLogic;
using AdminShellNS;
using System.ComponentModel;
using System.Threading;

namespace AasxPackageExplorer
{
    public class AasxScript
    {
        public LogInstance Log = null;

        protected int _logLevel = 2;

        public class MessageBox
        {
            public static void Show(string caption)
            {
                Console.WriteLine(caption);
            }

            public static void Tool(string cmd, params string[] args)
            {
                Console.WriteLine($"Execute {cmd} " + string.Join(",", args));
            }
        }

        public class Script_Tool : IInvokable
        {
            bool IInvokable.CanInvoke() => true;

            object IInvokable.Invoke(IScriptContext context, object[] args)
            {
                Console.WriteLine($"Execute tool" + string.Join(",", args));
                return 0;
            }
        }

        public class ScriptInvokableBase : IInvokable
        {
            protected AasxScript _script = null;

            public ScriptInvokableBase(AasxScript script)
            {
                _script = script;
            }

            public bool CanInvoke() => true;

            public virtual object Invoke(IScriptContext context, object[] args)
            {
                return 0;
            }
        }

        public class Script_WriteLine : ScriptInvokableBase
        {
            public Script_WriteLine(AasxScript script) : base(script) { }

            public override object Invoke(IScriptContext context, object[] args)
            {
                _script?.Log?.Info("Script: " + string.Join(",", args));
                return 0;
            }
        }

        public class Script_Sleep : ScriptInvokableBase
        {
            public Script_Sleep(AasxScript script) : base(script) { }

            public override object Invoke(IScriptContext context, object[] args)
            {
                if (args != null && args.Length == 1 && args[0] is int a0i)
                    Thread.Sleep(a0i);
                return 0;
            }
        }

        protected BackgroundWorker _worker = null;

        public bool IsExecuting { get => _worker != null; }

        public void StartEnginBackground(string script, int logLevel)
        {
            // access
            if (script?.HasContent() != true)
                return;
            AasxPackageLogic.Log.Singleton.Info(StoredPrint.Color.Blue, "Starting script with len {0} hash 0x{1:x}",
                script.Length, script.GetHashCode());
            if (_worker != null)
            {
                AasxPackageLogic.Log.Singleton.Error("AasxScript::StartEnginBackground already working!");
                return;
            }

            // use BackgroundWorker
            _worker = new BackgroundWorker();
            _logLevel = logLevel;
            Log = AasxPackageLogic.Log.Singleton;
            AdminShellPackageEnv envToload = null;
            _worker.DoWork += (s1, e1) =>
            {
                try
                {
                    // runtime
                    RuntimeHost.Initialize();
                    if (_logLevel >= 1)
                        AasxPackageLogic.Log.Singleton.Info("Script: Runtime initialized.");

                    // types
                    RuntimeHost.AddType("MessageBox", typeof(MessageBox));
                    if (_logLevel >= 2)
                        AasxPackageLogic.Log.Singleton.Info("Script: Typed added.");

                    // compile
                    Script s = Script.Compile(script);
                    if (_logLevel >= 2)
                        AasxPackageLogic.Log.Singleton.Info("Script: Compiled.");

                    // scope
                    s.Context.Scope.SetItem("Tool", new Script_Tool());
                    s.Context.Scope.SetItem("WriteLine", new Script_WriteLine(this));
                    s.Context.Scope.SetItem("Sleep", new Script_Sleep(this));
                    if (_logLevel >= 2)
                        AasxPackageLogic.Log.Singleton.Info("Script: Scope extened.");

                    // execute
                    if (_logLevel >= 2)
                        AasxPackageLogic.Log.Singleton.Info("Script: Starting execution of custom script ..");
                    s.Execute();
                    if (_logLevel >= 2)
                        AasxPackageLogic.Log.Singleton.Info("Script: .. execution ended.");
                } 
                catch (Exception ex) {
                    AasxPackageLogic.Log.Singleton.Error(ex, "Script: ");
                }
            };
            _worker.RunWorkerCompleted += (s1, e1) =>
            {
                if (_logLevel >= 1)
                    AasxPackageLogic.Log.Singleton.Info("Script: BackgroundWorker completed.");
            };
            _worker.RunWorkerAsync();
        }
    }
}
