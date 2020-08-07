﻿/*
 * Copyright (c) 2020 SICK AG <info@sick.de>
 *
 * This software is licensed under the Apache License 2.0 (Apache-2.0).
 * The ExcelDataReder dependency is licensed under the MIT license
 * (https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/LICENSE).
 */

#nullable enable

using System.IO;
using AdminShellNS;
using NUnit.Framework;

namespace AasxDictionaryImport.Cdd.Tests
{
    public class CddTest
    {
        protected static string GetDataDir()
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, "Cdd", "data");
        }

        protected static string GetExportFileName(string type, string id)
        {
            return $"export_{type.ToUpper()}_TSTM-{id}.xls";
        }

        protected static void CopyXls(string sourceName, string targetPath)
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
            File.Copy(Path.Combine(GetDataDir(), sourceName), targetPath);
        }

        protected static void CreateEmptyXls(string path, string name)
        {
            CopyXls("empty.xls", Path.Combine(path, name));
        }

        protected static System.Collections.Generic.Dictionary<string, string> CreateElementDictionary()
        {
            return new System.Collections.Generic.Dictionary<string, string>
            {
                { "MDC_P002_1", "" },
                { "MDC_P002_2", "" },
                { "MDC_P004_1.en", "" },
                { "MDC_P004_1.de", "" },
                { "MDC_P005.en", "" },
                { "MDC_P005.de", "" },
            };
        }

        protected static System.Collections.Generic.Dictionary<string, string> CreateClassDictionary()
        {
            var dict = CreateElementDictionary();
            dict.Add("MDC_P001_5", "");
            dict.Add("MDC_P006_1", "");
            dict.Add("MDC_P010", "");
            dict.Add("MDC_P014", "");
            dict.Add("MDC_P090", "");
            return dict;
        }

        protected static System.Collections.Generic.Dictionary<string, string> CreatePropertyDictionary()
        {
            var dict = CreateElementDictionary();
            dict.Add("MDC_P001_6", "");
            dict.Add("MDC_P004_3.en", "");
            dict.Add("MDC_P004_3.de", "");
            dict.Add("MDC_P006_1", "");
            dict.Add("MDC_P025_1", "");
            dict.Add("MDC_P023", "");
            dict.Add("MDC_P041", "");
            dict.Add("MDC_P022", "");
            return dict;
        }

        protected static AdminShellV20.AdministrationShell CreateAdminShell(AdminShellV20.AdministrationShellEnv env)
        {
            var adminShell = new AdminShellV20.AdministrationShell()
            {
                identification = new AdminShellV20.Identification(
                        AdminShellV20.Identification.IRI,
                        AasxPackageExplorer.Options.Curr.GenerateIdAccordingTemplate(
                            AasxPackageExplorer.Options.Curr.TemplateIdAas)),
            };
            env.AdministrationShells.Add(adminShell);
            return adminShell;
        }
    }
}
