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
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using System.IO;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxPackageExplorer
{
    /// <summary>
    /// An application shall implement this interface in order to get
    /// "controlled" by the AASX script.
    /// </summary>
    public interface IAasxScriptRemoteInterface
    {
        Task<int> Tool(object[] args);
        AasCore.Aas3_0_RC02.IReferable Select(object[] args);
        bool Location(object[] args);
    }

    public class AasxScript
    {
        public LogInstance ScriptLog = null;
        public AasxMenu RootMenu = null;
        public IAasxScriptRemoteInterface Remote = null;

        protected int _logLevel = 2;

        public class HelpInfo
        {
            public string Keyword;
            public string Description;
            public AasxMenuListOfArgDefs ArgDefs;
        }

        public List<HelpInfo> ListOfHelp = new List<HelpInfo>();

        public void AddHelpInfo(string key, string desc, AasxMenuListOfArgDefs args = null)
        {
            ListOfHelp.Add(new HelpInfo() { Keyword = key, Description = desc, ArgDefs = args });
        }

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

            public static void Flex(int a = 0, int b = 0, int c = 0)
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
            public Script_WriteLine(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("WriteLine",
                    "Outputs all arguments to script log messages and starts new line.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("<any>", "All arguments are writen to the scriot log."));
            }

            public override object Invoke(IScriptContext context, object[] args)
            {
                _script?.ScriptLog?.Info("Script: " + string.Join(",", args));
                return 0;
            }
        }

        public class Script_Sleep : ScriptInvokableBase
        {
            public Script_Sleep(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("Sleep",
                    "Pauses the execution for a number of given milli seconds.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("<time>", "Pause time in milli seconds."));
            }

            public override object Invoke(IScriptContext context, object[] args)
            {
                if (args != null && args.Length == 1 && args[0] is int a0i)
                    Thread.Sleep(a0i);
                return 0;
            }
        }

        public class Script_FileReadAll : ScriptInvokableBase
        {
            public Script_FileReadAll(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("FileReadAll",
                    "Reads all lines of a text file and returns as one string.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("<filename>", "Filename and path of the text file to read.")
                        .Add("returns:", "All lines of text file as one string; null if file is not accessible."));
            }

            public override object Invoke(IScriptContext context, object[] args)
            {
                if (args != null && args.Length == 1 && args[0] is string fn)
                    try
                    {
                        if (!System.IO.File.Exists(fn))
                            return null;
                        using (var f = new StreamReader(fn))
                            return f.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        if (_script != null && _script._logLevel >= 2)
                            Log.Singleton.Error(ex, $"when reading text contents of {fn}");
                    }
                return null;
            }
        }

        public class Script_FileExists : ScriptInvokableBase
        {
            public Script_FileExists(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("FileExists",
                    "Check if a file or path exists.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("<filename>", "Filename and path of the text file to check.")
                        .Add("returns:", "True, if file or path exists; null if not accessible."));
            }

            public override object Invoke(IScriptContext context, object[] args)
            {
                if (args != null && args.Length == 1 && args[0] is string fn)
                    try
                    {
                        if (System.IO.File.Exists(fn) || Directory.Exists(fn))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        if (_script != null && _script._logLevel >= 2)
                            Log.Singleton.Error(ex, $"when check existence of {fn}");
                    }
                return false;
            }
        }
        public class Script_Tool : ScriptInvokableBase
        {
            public Script_Tool(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("Tool",
                    "Invokes a menu-item (tool) of the application with arguments treated as key/value pairs.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("Toolname", "String identifying command or action.")
                        .Add("<key>", "String which identifies the argument of the command.")
                        .Add("<value>", "Arbitrary type and value for that argument."));
            }

            public override object Invoke(IScriptContext context, object[] args)
            {
                // access
                if (_script == null)
                    return -1;

                if (args == null || args.Length < 1 || !(args[0] is string))
                {
                    _script.ScriptLog?.Error("Script: Invoke Tool: Toolname missing");
                    return -1;
                }

                // debug
                if (_script._logLevel >= 2)
                    Console.WriteLine($"Execute Tool " + string.Join(",", args));

                // invoke action
                // https://stackoverflow.com/questions/39438441/
                var x = Application.Current.Dispatcher.Invoke(async () =>
                {
                    if (_script?.Remote == null)
                        return -1;
                    return await _script?.Remote?.Tool(args);
                });
                if (x != null)
                    Log.Singleton.Silent("" + x);

                // done
                return 0;
            }
        }

        public class Script_Select : ScriptInvokableBase
        {
            public Script_Select(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("Select",
                    "Selects the currently selected item in the elements hierarchy of the main AAS environment.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("<ref. type>", "String indicating Referable type, such as This, AAS, SM, SME, CD. " +
                            "'This' returns the currently selected Referable without changing selection.")
                        .Add("<adr. mode>", "Adressing mode, such as First, Next, Prev, idShort, semanticId.")
                        .Add("<value>", "String search argument for idShort, semanticId.")
                        .Add("returns:", "Referable which is currently selected."));
            }

            public override object Invoke(IScriptContext context, object[] args)
            {
                // access
                if (_script == null)
                    return -1;

                if (args == null || args.Length < 1 || !(args[0] is string))
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
                    Log.Singleton.Silent("" + x.IdShort);

                // done
                return x;
            }
        }

        public class Script_Location : ScriptInvokableBase
        {
            public Script_Location(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("Location",
                    "Stores (\"Push\") or retrieves (\"Pop\") the currently selected item from a stack.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("<cmd>", "Either 'Push' or 'Pop'."));
            }

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
                // ReSharper disable StringIndexOfIsCultureSpecific.1
                if (" push pop ".IndexOf(" " + cmdtl + " ") < 0)
                {
                    _script.ScriptLog?.Error("Script: Location: Command is unknown!");
                    return -1;
                }
                // ReSharper enable StringIndexOfIsCultureSpecific.1

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

        public class Script_System : ScriptInvokableBase
        {
            public Script_System(AasxScript script) : base(script)
            {
                script?.AddHelpInfo("System",
                    "Executes a command-line given by the arguments on the operating system prompt.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("<any>", "All arguments are passed to the command line."));
            }

            public override object Invoke(IScriptContext context, object[] args)
            {
                // access
                if (_script == null)
                    return -1;

                if (args == null || args.Length < 1 || !(args[0] is string))
                {
                    _script.ScriptLog?.Error("Script: System: Command is missing!");
                    return -1;
                }

                if (!Options.Curr.ScriptExecuteSystem)
                {
                    _script.ScriptLog?.Error("Script: Options not allow executing system commands!");
                    return -1;
                }

                // debug
                if (_script._logLevel >= 2)
                    Console.WriteLine($"Execute System " + string.Join(" ", args));

                // Start the child process.
                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = "CMD.exe";
                p.StartInfo.Arguments = "/C " + string.Join(" ", args);
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                //// p.WaitForExit();
                // Read the output stream first and then wait.
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();

                if (stdout?.HasContent() == true)
                    Log.Singleton.Info("Script: System: " + stdout);
                if (stderr?.HasContent() == true)
                    Log.Singleton.Info(StoredPrint.Color.Red, "Script: System: " + stderr);

                // done
                return p.ExitCode;
            }
        }

        protected BackgroundWorker _worker = null;

        public bool IsExecuting { get => _worker != null; }

        public void PrepareHelp()
        {
            var root = typeof(AasxScript);
            foreach (var nt in root.GetNestedTypes())
                if (nt.GetInterfaces().Contains(typeof(IInvokable)))
                {
                    Activator.CreateInstance(nt, this);
                }
        }

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
                    s.Context.Scope.SetItem("FileReadAll", new Script_FileReadAll(this));
                    s.Context.Scope.SetItem("FileExists", new Script_FileExists(this));
                    s.Context.Scope.SetItem("Select", new Script_Select(this));
                    s.Context.Scope.SetItem("Location", new Script_Location(this));
                    s.Context.Scope.SetItem("System", new Script_System(this));
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: Scope extended.");

                    // execute
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: Starting execution of custom script ..");
                    s.Execute();
                    if (_logLevel >= 2)
                        Log.Singleton.Info("Script: .. execution ended.");
                }
                catch (Exception ex)
                {
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
