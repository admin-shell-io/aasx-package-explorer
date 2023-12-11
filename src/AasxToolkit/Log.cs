/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.IO;
using System.Text;

namespace AasxToolkit
{

    public static class Log
    {

        public static int verbosity = 2;

        public static void WriteLine(int level, string fmt, params object[] args)
        {
            if (level > verbosity)
                return;
            var st = string.Format(fmt, args);
            Console.Out.WriteLine(st);
        }

        public static void WriteLine(string fmt, params object[] args)
        {
            WriteLine(1, fmt, args);
        }
    }

}
