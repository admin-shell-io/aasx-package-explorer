/*
 * Copyright (c) 2020 SICK AG <info@sick.de>
 *
 * This software is licensed under the Apache License 2.0 (Apache-2.0).
 * The ExcelDataReder dependency is licensed under the MIT license
 * (https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/LICENSE).
 */

#nullable enable

using System;
using System.Linq;
using System.Windows.Input;
using AasxGlobalLogging;
using AdminShellNS;

namespace AasxDictionaryImport
{
    /// <summary>
    /// A generic import dialog for submodels.  For the data model, see the Model namespace.  For implementations of
    /// this data model, see the Cdd namespace.  For the actual dialog, see the ImportDialog.xaml file.
    /// <para>
    /// This project uses nullable types.  Methods may only throw exceptions if they are mentioned in the doc comment.
    /// </para>
    /// <para>
    /// This project could be extended and improved by:
    /// <list type="bullet">
    /// <item>
    /// <description>supporting multi-language strings in the user interface (see e. g.
    /// ElementDetailsDialog)</description>
    /// </item>
    /// <item>
    /// <description>supporting other data providers, especially eCl@ss</description>
    /// </item>
    /// <item>
    /// <description>fetching data from the network (IEC CDD HTML web service, eCl@ss REST API)</description>
    /// </item>
    /// <item>
    /// <description>supporting import of properties and concept descriptions</description>
    /// </item>
    /// <item>
    /// <description>better resize handling in ElementDetailsDialog</description>
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    // ReSharper disable once UnusedType.Global
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// A generic import dialog for submodels.
    /// </summary>
    public static class Import
    {
        /// <summary>
        /// Shows the import dialog and allows the user to select data from a data provider implementation that is
        /// converted into an AAS submodel and imported into the given admin shell.  If <paramref name="adminShell"/> is
        /// null, a new empty admin shell is created in the given environment.
        /// </summary>
        /// <param name="env">The AAS environment to import into</param>
        /// <param name="adminShell">The admin shell to import into, or null if a new admin shell should be
        /// created</param>
        /// <returns>true if at least one submodel was imported</returns>
        public static bool ImportSubmodel(AdminShellV20.AdministrationShellEnv env,
            AdminShellV20.AdministrationShell? adminShell = null)
        {
            adminShell ??= CreateAdminShell(env);
            var dialog = new ImportDialog();
            if (dialog.ShowDialog() != true || dialog.Context == null)
                return false;


            int imported;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                imported = dialog.GetResult().Count(e => e.ImportSubmodelInto(env, adminShell));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (dialog.Context.UnknownReferences.Count > 0)
            {
                Log.Info($"Found {dialog.Context.UnknownReferences.Count} unknown references during import: " +
                    string.Join(", ", dialog.Context.UnknownReferences));
            }

            return imported > 0;
        }

        private static AdminShellV20.AdministrationShell CreateAdminShell(AdminShellV20.AdministrationShellEnv env)
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
