/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

namespace AasxPackageLogic
{
    /// <summary>
    /// Static class, wrapping log instance, to have a logging via Singleton.
    /// (no need to have Log instance in every single class)
    /// </summary>
    public static class Log
    {
        private static readonly AasxIntegrationBase.LogInstance LogInstance = new AasxIntegrationBase.LogInstance();

        public static AasxIntegrationBase.LogInstance Singleton => LogInstance;
    }
}
