/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// TODO (MIHO, 2020-08-03): check SOURCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasOpcUaServer;
using AasxIntegrationBase;
using AasxUaNetServer;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace Net46ConsoleServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class Program
    {
        static void Main(string[] args)
        {
            // start
            Console.Error.WriteLine(
                "AAS OPC UA Server. (c) 2019 Michael Hoffmeister, Festo AG & Co. KG. See LICENSE.TXT.");

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

            // ReSharper disable HeuristicUnreachableCode
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
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
            UaServerWrapper server = new UaServerWrapper(_stopTimeout: 0, _aasxEnv: env, logger: logger, _serverOptions: options);
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

        public override void Message(string text, bool ask = false)
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
                    return await Task.FromResult((result.KeyChar == 'y') || (result.KeyChar == 'Y')
                        || (result.KeyChar == '\r'));
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }
            return await Task.FromResult(true);
        }
    }

    // ReSharper disable once UnusedType.Global
    public enum ExitCode
    {
        Ok = 0,
        ErrorServerNotStarted = 0x80,
        ErrorServerRunning = 0x81,
        ErrorServerException = 0x82,
        ErrorInvalidCommandLine = 0x100
    };

}
