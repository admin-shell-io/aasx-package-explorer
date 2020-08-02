using AasOpcUaServer;
using AasxIntegrationBase;
using AasxUaNetServer;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net46ConsoleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // start
            Console.Error.WriteLine("AAS OPC UA Server. (c) 2019 Michael Hoffmeister, Festo AG & Co. KG. See LICENSE.TXT.");

            // arguments
            var options = new AasOpcUaServer.AasxUaServerOptions();
            options.ParseArgs(args);

            // load aasx
            if (options.AasxToLoad == null)
            {
                Console.Error.WriteLine("No .aasx-file to load given. Exiting!");
                return;
            }
            Console.Error.WriteLine($"loading: {options.AasxToLoad} ..");
            var env = new AdminShellPackageEnv(options.AasxToLoad);
            if (env == null)
            {
                Console.Error.WriteLine($"Cannot open {options.AasxToLoad}. Aborting..");
            }

            // configure UA here a little bit
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();

            // logger
            var logger = new LogInstance();

            // start
            Console.WriteLine("Starting server ...");
            Console.WriteLine("Press 'x' to exit.");
            UaServerWrapper server = new UaServerWrapper(_autoAccept: true, _stopTimeout: 0, _aasxEnv: env, logger: logger, _serverOptions: options);
            server.Run();    
            
            // loop
            while (true)
            {                
                StoredPrint sp;
                while ((sp = logger.PopLastShortTermPrint()) != null)
                    Console.WriteLine(sp.ToString());

                Thread.Sleep(10);

                if (Console.KeyAvailable)
                {
                    var cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.X)
                    {
                        Console.WriteLine("Stopping initiated ...");
                        break;
                    }
                }
            }

            // stop
            server.Stop();
        }
    }

    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string message = string.Empty;
        private bool ask = false;

        public override void Message(string text, bool ask)
        {
            this.message = text;
            this.ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (ask)
            {
                message += " (y/n, default y): ";
                Console.Write(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            if (ask)
            {
                try
                {
                    ConsoleKeyInfo result = Console.ReadKey();
                    Console.WriteLine();
                    return await Task.FromResult((result.KeyChar == 'y') || (result.KeyChar == 'Y') || (result.KeyChar == '\r'));
                }
                catch
                {
                    // intentionally fall through
                }
            }
            return await Task.FromResult(true);
        }
    }

    public enum ExitCode : int
    {
        Ok = 0,
        ErrorServerNotStarted = 0x80,
        ErrorServerRunning = 0x81,
        ErrorServerException = 0x82,
        ErrorInvalidCommandLine = 0x100
    };

    /*
    public class MySampleServer
    {
        SampleServer server;
        Task status;
        DateTime lastEventTime;
        int serverRunTime = Timeout.Infinite;
        static bool autoAccept = false;
        static ExitCode exitCode;
        static AdminShellPackageEnv aasxEnv = null;
        static AasxUaServerOptions aasxServerOptions = null;

        public MySampleServer(bool _autoAccept, int _stopTimeout, AdminShellPackageEnv _aasxEnv, AasxUaServerOptions _serverOptions = null)
        {
            autoAccept = _autoAccept;
            aasxEnv = _aasxEnv;
            aasxServerOptions = _serverOptions;
            serverRunTime = _stopTimeout == 0 ? Timeout.Infinite : _stopTimeout * 1000;
        }

        public void Run()
        {

            try
            {
                exitCode = ExitCode.ErrorServerNotStarted;
                ConsoleSampleServer().Wait();
                Console.WriteLine("Server started. Press Ctrl-C to exit...");
                exitCode = ExitCode.ErrorServerRunning;
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Console.WriteLine("Exception: {0}", ex.Message);
                exitCode = ExitCode.ErrorServerException;
                return;
            }

            ManualResetEvent quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) => {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne(serverRunTime);

            if (server != null)
            {
                Console.WriteLine("Server stopped. Waiting for exit...");

                using (SampleServer _server = server)
                {
                    // Stop status thread
                    server = null;
                    status.Wait();
                    // Stop server and dispose
                    _server.Stop();
                }
            }

            exitCode = ExitCode.Ok;
        }

        public static ExitCode ExitCode { get => exitCode; }

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = autoAccept;
                if (autoAccept)
                {
                    Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
                }
                else
                {
                    Console.WriteLine("Rejected Certificate: {0}", e.Certificate.Subject);
                }
            }
        }

        private async Task ConsoleSampleServer()
        {
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance();

            application.ApplicationName = "UA Core Sample Server";
            application.ApplicationType = ApplicationType.Server;
            application.ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleServer" : "Opc.Ua.SampleServer";

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }

            // Important: set appropriate trace mask
            Utils.SetTraceMask(Utils.TraceMasks.Error | Utils.TraceMasks.Information | Utils.TraceMasks.StartStop | Utils.TraceMasks.StackTrace);

            // attach tracing?
            Utils.Tracing.TraceEventHandler += (sender, args) =>
            {
                var st = String.Format(args.Format, (args.Arguments != null ? args.Arguments : new string[] { "" }));
                Console.WriteLine("[{0}] {1} {2} {3}", args.TraceMask, st, args.Message, args.Exception?.Message ?? "-");
            };

            // allow stopping after finalizing special jobs
            if (aasxServerOptions != null)
                aasxServerOptions.FinalizeAction += () =>
                {
                    server.Stop();
                };

            // start the server.
            server = new SampleServer(aasxEnv, aasxServerOptions);
            await application.Start(server);

            // start the status thread
            status = Task.Run(new Action(StatusThread));

            // print notification on session events
            // MIHO: does not seem to be executed?
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
                    item += String.Format("Last Event:{0:HH:mm:ss}", session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
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
    */
}
