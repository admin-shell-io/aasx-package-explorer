/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxRestServerLibrary;
using AdminShellNS;

namespace AasxRestConsoleServer
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.Error.WriteLine(
                "AAS Console Server. (c) 2019 Michael Hoffmeister, Festo AG & Co. KG. See LICENSE.TXT.");

            // default command line options
            var fn = "usb-stick-REST-demo.aasx";
            var host = "localhost";
            var port = "1111";

            // parse
            int i = 0;
            while (i < args.Length)
            {
                var x = args[i].Trim().ToLower();

                // real option?
                if (i < args.Length - 1)
                {
                    if (x == "-host")
                    {
                        host = args[i + 1];
                        i += 2;
                        continue;
                    }

                    if (x == "-port")
                    {
                        port = args[i + 1];
                        i += 2;
                        continue;
                    }
                }

                // last??
                fn = args[i];
                i += 1;
            }

            // load?
            var package = new AdminShellPackageEnv(fn);
            AasxRestServer.Start(package, host, port, new GrapevineLoggerToConsole());

            // wait for RETURN
            Console.ReadLine();
            AasxRestServer.Stop();
        }
    }
}
