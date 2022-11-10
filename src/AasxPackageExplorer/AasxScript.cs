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
using System.Windows;

namespace AasxPackageExplorer
{
    /// <summary>
    /// An application shall implement this interface in order to get
    /// "controlled" by the AASX script.
    /// </summary>
    public interface IAasxScriptRemoteInterface
    {
        AdminShell.Referable Select(object[] args);
        bool Location(object[] args);
    }

    public class AasxScript
    {
        public LogInstance ScriptLog = null;
        public AasxMenu RootMenu = null;
        public IAasxScriptRemoteInterface Remote = null;

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

            public static void Flex(int a=0, int b=0, int c=0)
            {
                Console.WriteLine($"Flex a {a} b {b} c{c}");
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
                _script?.ScriptLog?.Info("Script: " + string.Join(",", args));
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

        public class Script_Tool : ScriptInvokableBase
        {
            public Script_Tool(AasxScript script) : base(script) { }

            public override object Invoke(IScriptContext context, object[] args)
            {
                // access
                if (_script == null)
                    return -1;

                if (args == null || args.Length < 1 || !(args[0] is string toolName))
                {
                    _script.ScriptLog?.Error("Script: Invoke Tool: Toolname missing");
                    return -1;
                }

                // debug
                if (_script._logLevel >= 2)
                    Console.WriteLine($"Execute Tool " + string.Join(",", args));

                // name of tool, find it
                var mi = _script.RootMenu?.FindName(toolName);
                if (mi == null)
                {
                    _script.ScriptLog?.Error("Script: Invoke Tool: Toolname invalid");
                    return -1;
                }

                // create a ticket
                var ticket = new AasxMenuActionTicket()
                {
                    MenuItem = mi,
                    ScriptMode = true,
                    ArgValue = new Dictionary<AasxMenuArgDef, object>()
                };

                // go thru the remaining arguments and find arg names and values
                var argi = 1;
                while (argi < args.Length)
                {
                    // get arg name
                    if (!(args[argi] is string argname))
                    {
                        _script.ScriptLog?.Error($"Script: Invoke Tool: Argument at index {argi} is " +
                            $"not string type for argument name.");
                        return -1;
                    }

                    // find argname?
                    var ad = mi.ArgDefs?.Find(argname);
                    if (ad == null)
                    {
                        _script.ScriptLog?.Error($"Script: Invoke Tool: Argument at index {argi} is " +
                            $"not valid argument name.");
                        return -1;
                    }

                    // create arg value (not available is okay)
                    object av = null;
                    if (argi + 1 < args.Length)
                        av = args[argi + 1];

                    // into ticket
                    ticket.ArgValue.Add(ad, av);

                    // 2 forward!
                    argi += 2;
                }

                // invoke action
                // https://stackoverflow.com/questions/39438441/
                var x = Application.Current.Dispatcher.Invoke(() =>
                {
                    return _script.RootMenu.ActivateAction(mi, ticket);
                });
                if (x != null)
                    Log.Singleton.Silent("" + x.Id);
                // done
                return 0;
            }
        }

        public class Script_Select : ScriptInvokableBase
        {
            public Script_Select(AasxScript script) : base(script) { }

            public override object Invoke(IScriptContext context, object[] args)
            {
                // access
                if (_script == null)
                    return -1;

                if (args == null || args.Length < 1 || !(args[0] is string refTypeStr))
                {
                    _script.ScriptLog?.Error("Script: Select: Referable type missing");
                    return -1;
                }

                // debug
                if (_script._logLevel >= 2)
                    Console.WriteLine($"Execute Select " + string.Join(",", args));
               
                // invoke action
                // https://stackoverflow.com/questions/39438441/
                var x = Application.Current.Dispatcher.Invoke(() =>
                {
                    return _script.Remote?.Select(args);
                });
                if (x != null)
                    Log.Singleton.Silent("" + x.idShort);
                
                // done
                return x;
            }
        }

        public class Script_Location : ScriptInvokableBase
        {
            public Script_Location(AasxScript script) : base(script) { }

            public override object Invoke(IScriptContext context, object[] args)
            {
                // access
                if (_script == null)
                    return -1;

                if (args == null || args.Length < 1 || !(args[0] is string cmd))
                {
                    _script.ScriptLog?.Error("Script: Location: Command is missing!");
                    return -1;
                }   

                // check for allowed commands
                var cmdtl = cmd.Trim().ToLower();
                if (" push pop ".IndexOf(" " + cmdtl + " ") < 0)
                {
                    _script.ScriptLog?.Error("Script: Location: Command is unknown!");
                    return -1;
                }

                // debug
                if (_script._logLevel >= 2)
                    Console.WriteLine($"Execute Location " + string.Join(",", args));

                // invoke action
                // https://stackoverflow.com/questions/39438441/
                var x = Application.Current.Dispatcher.Invoke(() =>
                {
                    return _script.Remote?.Location(args);
                });
                if (x != null)
                    Log.Singleton.Silent("" + x);
                // done
                return 0;
            }
        }

        protected BackgroundWorker _worker = null;

        public bool IsExecuting { get => _worker != null; }

        public void StartEnginBackground(
            string script, 
            int logLevel,
            AasxMenu rootMenu, 
            IAasxScriptRemoteInterface remote)
        {
            // access
            if (script?.HasContent() != true)
                return;
            if (_logLevel >= 1)
                Log.Singleton.Info(StoredPrint.Color.Blue, "Starting script with len {0} hash 0x{1:x}",
                    script.Length, script.GetHashCode());
            if (_worker != null)
            {
                AasxPackageLogic.Log.Singleton.Error("AasxScript::StartEnginBackground already working!");
                return;
            }

            // use BackgroundWorker
            _worker = new BackgroundWorker();
            _logLevel = logLevel;
            Remote = remote;
            RootMenu = rootMenu;
            ScriptLog = Log.Singleton;
            _worker.DoWork += (s1, e1) =>
            {
                try
                {
                    // runtime
                    RuntimeHost.Initialize();
                    if (_logLevel >= 1)
                        Log.Singleton.Info("Script: Runtime initialized.");

                    // types
                    RuntimeHost.AddType("MessageBox", typeof(MessageBox));
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: Typed added.");

                    // compile
                    Script s = Script.Compile(script);
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: Compiled.");

                    // scope
                    s.Context.Scope.SetItem("Tool", new Script_Tool(this));
                    s.Context.Scope.SetItem("WriteLine", new Script_WriteLine(this));
                    s.Context.Scope.SetItem("Sleep", new Script_Sleep(this));
                    s.Context.Scope.SetItem("Select", new Script_Select(this));
                    s.Context.Scope.SetItem("Location", new Script_Location(this));
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: Scope extened.");

                    // execute
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: Starting execution of custom script ..");
                    s.Execute();
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: .. execution ended.");
                } 
                catch (Exception ex) {
                    Log.Singleton.Error(ex, "Script: ");
                }
            };
            _worker.RunWorkerCompleted += (s1, e1) =>
            {
                if (_logLevel >= 1)
                    Log.Singleton.Info("Script: BackgroundWorker completed.");
                _worker = null;
            };
            _worker.RunWorkerAsync();
        }
    }
}
