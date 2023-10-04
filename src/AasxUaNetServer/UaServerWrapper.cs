// SOURCE: https://github.com/OPCFoundation/UA-.NETStandard/blob/1.4.355.26/
// SampleApplications/Samples/NetCoreConsoleServer/Program.cs
// heavily modified
// the (actual) Source is this: https://github.com/OPCFoundation/UA-.NETStandard/blob/master/Applications/
// ConsoleReferenceServer/Program.cs
// it now features the MIT license!

/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using AasOpcUaServer;
using AasxIntegrationBase;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AasxUaNetServer
{

    public enum ExitCode
    {
        Ok = 0,
        ErrorServerNotStarted = 0x80,
        ErrorServerRunning = 0x81,
        ErrorServerException = 0x82,
        ErrorInvalidCommandLine = 0x100
    };

    public class UaServerWrapper
    {
        private LogInstance Log = null;
        SampleServer server;
        Task status;
        DateTime lastEventTime;
        int serverRunTime = Timeout.Infinite;
        static ExitCode exitCode;
        static AdminShellPackageEnv aasxEnv = null;
        static AasxUaServerOptions aasxServerOptions = null;

        public bool IgnoreFurtherErrors = false;
        public bool AllowFinallyStopped = true;
        public bool FinallyStopped = false;

        public UaServerWrapper(int _stopTimeout, AdminShellPackageEnv _aasxEnv, LogInstance logger = null, AasxUaServerOptions _serverOptions = null)
        {
            aasxEnv = _aasxEnv;
            aasxServerOptions = _serverOptions;
            serverRunTime = _stopTimeout == 0 ? Timeout.Infinite : _stopTimeout * 1000;
            this.Log = logger;
        }

        public void Run()
        {

            try
            {
                exitCode = ExitCode.ErrorServerNotStarted;
                Log.Info("will start..........");
                ConsoleSampleServer().Wait();
                Console.WriteLine("Server started.");
                exitCode = ExitCode.Ok;
            }
            catch (Exception ex)
            {
                var st = ex.Message;
                if (!(IgnoreFurtherErrors && (st.Contains("Mindestens ein") || st.Contains("At least"))))
                {
                    Utils.Trace("ServiceResultException:" + st);
                    Log.Error(ex, "starting server");
                    Console.WriteLine("Exception: {0}", ex.Message);
                    exitCode = ExitCode.ErrorServerException;
                    Stop();
                    FinallyStopped = AllowFinallyStopped;
                }
            }
        }

        public void Stop()
        {
            if (server != null)
            {
                Console.WriteLine("Server stopped. Waiting for exit...");

                using (SampleServer _server = server)
                {
                    // Stop status thread
                    server = null;
                    if (status != null)
                        status.Wait();
                    // Stop server and dispose
                    if (_server != null)
                        _server.Stop();

                    FinallyStopped = AllowFinallyStopped;

                    Log.Info("End of Server stopping!");
                }
            }
        }

        private static bool _traceHandleAttached = false;

        private async Task ConsoleSampleServer()
        {
            ApplicationInstance application = new ApplicationInstance();

            application.ApplicationName = "OPC UA AASX Server plugin";
            application.ApplicationType = ApplicationType.Server;
            application.ConfigSectionName = Utils.IsRunningOnMono() ? "MonoAasxServerPlugin" : "AasxPluginUaNetServer";

            // modify ConfigSectionName with absoluet file?
            if (true)
            {
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                application.ConfigSectionName = Path.Combine(assemblyFolder, application.ConfigSectionName);
            }

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            if (!await application.CheckApplicationInstanceCertificate(false, 0))
            {
                throw new Exception("Application instance certificate invalid!");
            }

            // Important: set appropriate trace mask
            Utils.SetTraceMask(Utils.TraceMasks.Error
                | Utils.TraceMasks.Information
                | Utils.TraceMasks.StartStop
                | Utils.TraceMasks.StackTrace);

            // attach tracing?
            if (!_traceHandleAttached)
            {
                _traceHandleAttached = true;
                Utils.Tracing.TraceEventHandler += (sender, args) =>
                {
                    if (this.Log != null)
                    {
                        // bad hack
                        if (args == null)
                            return;
                        if (args.TraceMask == Utils.TraceMasks.Information
                            || args.TraceMask == Utils.TraceMasks.Service
                            || args.TraceMask == Utils.TraceMasks.ServiceDetail)
                            return;

                        var st = String.Format(args.Format,
                            // ReSharper disable once CoVariantArrayConversion
                            // ReSharper disable once RedundantExplicitArrayCreation
                            (args.Arguments != null ? args.Arguments : new string[] { "" }));

                        // supports specially knows errors
                        if (true == args.Exception?.InnerException?.Message?.Contains("libuv"))
                            return;

                        // suppress more errors?
                        if (st.Contains("Mindestens ein") || st.Contains("At least"))
                        {
                            if (IgnoreFurtherErrors)
                                return;
                        }

                        // errors lead to not automatically close window?
                        if (!IgnoreFurtherErrors && args.TraceMask == Utils.TraceMasks.Error)
                            AllowFinallyStopped = false;

                        this.Log.Info("[{0}] {1} {2} {3} {4}",
                            args.TraceMask, st, args.Message, "" + args.Exception?.Message,
                            "" + args.Exception?.StackTrace);
                    }
                };
            }

            // allow stopping after finalizing special jobs
            if (aasxServerOptions != null)
                aasxServerOptions.FinalizeAction += () =>
                {
                    //// server.Stop();
                    // changed to the following to close the window, as well
                    Stop();
                    IgnoreFurtherErrors = true;
                };

            // start the server.
            server = new SampleServer(aasxEnv, aasxServerOptions);
            await application.Start(server);

            // start the status thread
            // ReSharper disable once RedundantDelegateCreation
            status = Task.Run(new Action(StatusThread));

            // print notification on session events
            server.CurrentInstance.SessionManager.SessionActivated += EventStatus;
            server.CurrentInstance.SessionManager.SessionClosing += EventStatus;
            server.CurrentInstance.SessionManager.SessionCreated += EventStatus;

        }

        private void EventStatus(Session session, SessionEventReason reason)
        {
            lastEventTime = DateTime.UtcNow;
            PrintSessionStatus(session, reason.ToString());
        }

        void PrintSessionStatus(Session session, string reason, bool lastContact = false)
        {
            lock (session.DiagnosticsLock)
            {
                string item = String.Format("{0,9}:{1,20}:", reason, session.SessionDiagnostics.SessionName);
                if (lastContact)
                {
                    item += String.Format("Last Event:{0:HH:mm:ss}",
                                session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
                }
                else
                {
                    if (session.Identity != null)
                    {
                        item += String.Format(":{0,20}", session.Identity.DisplayName);
                    }
                    item += String.Format(":{0}", session.Id);
                }
                Console.WriteLine(item);
            }
        }

        private async void StatusThread()
        {
            while (server != null)
            {
                if (DateTime.UtcNow - lastEventTime > TimeSpan.FromMilliseconds(6000))
                {
                    IList<Session> sessions = server.CurrentInstance.SessionManager.GetSessions();
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (int ii = 0; ii < sessions.Count; ii++)
                    {
                        Session session = sessions[ii];
                        PrintSessionStatus(session, "-Status-", true);
                    }
                    lastEventTime = DateTime.UtcNow;
                }
                await Task.Delay(1000);
            }
        }

    }
}
