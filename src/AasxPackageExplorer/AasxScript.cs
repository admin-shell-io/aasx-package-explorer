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

namespace AasxPackageExplorer
{
    public class AasxScript
    {
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

        public class Script_WriteLine : IInvokable
        {
            bool IInvokable.CanInvoke() => true;

            object IInvokable.Invoke(IScriptContext context, object[] args)
            {
                Log.Singleton.Info("Script: " + string.Join(",", args));
                return 0;
            }
        }

        public static void StartEnginBackground(string script)
        {
            // access
            if (script?.HasContent() != true)
                return;
            Log.Singleton.Info(StoredPrint.Color.Blue, "Starting script with len {0} hash 0x{1:x}",
                script.Length, script.GetHashCode());

            // use BackgroundWorker
            var worker = new BackgroundWorker();
            AdminShellPackageEnv envToload = null;
            worker.DoWork += (s1, e1) =>
            {
                try
                {
                    // runtime
                    RuntimeHost.Initialize();
                    Log.Singleton.Info("Script: Runtime initialized.");

                    // types
                    RuntimeHost.AddType("MessageBox", typeof(MessageBox));
                    Log.Singleton.Info("Script: Typed added.");

                    // compile
                    Script s = Script.Compile(script);
                    Log.Singleton.Info("Script: Compiled.");

                    // scope
                    s.Context.Scope.SetItem("Tool", new Script_Tool());
                    s.Context.Scope.SetItem("WriteLine", new Script_WriteLine());
                    Log.Singleton.Info("Script: Scope extened.");

                    // execute
                    Log.Singleton.Info("Script: Starting execution of custom script ..");
                    s.Execute();
                    Log.Singleton.Info("Script: .. execution ended.");
                } 
                catch (Exception ex) {
                    Log.Singleton.Error(ex, "Script: ");
                }
            };
            worker.RunWorkerCompleted += (s1, e1) =>
            {
                Log.Singleton.Info("Script: BackgroundWorker completed.");
            };
            worker.RunWorkerAsync();
        }
    }
}
