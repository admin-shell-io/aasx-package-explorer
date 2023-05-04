/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

using AasxPackageLogic;
using AdminShellNS;
using Extensions;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Aas = AasCore.Aas3_0;

namespace AasxDictionaryImport
{
    /// <summary>
    /// A generic import dialog for submodels and submodel elements.  For the data model, see the Model namespace.  For
    /// implementations of this data model, see the Cdd and Eclass namespaces.  For the actual dialog, see the
    /// ImportDialog.xaml file.
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
    /// <description>adding support for ECLASS Advanced</description>
    /// </item>
    /// <item>
    /// <description>fetching data from the network (IEC CDD HTML web service, ECLASS REST API)</description>
    /// </item>
    /// <item>
    /// <description>supporting import of concept descriptions for existing elements</description>
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
    /// A generic import dialog for submodels and submodel elements.
    /// </summary>
    public static class Import
    {
        /// <summary>
        /// Shows the import dialog and allows the user to select data from a data provider implementation that is
        /// converted into an AAS submodel and imported into the given admin shell.  If <paramref name="adminShell"/> is
        /// null, a new empty admin shell is created in the given environment.
        /// </summary>
        /// <param name="window">The parent window for the import dialog</param>
        /// <param name="env">The AAS environment to import into</param>
        /// <param name="defaultSourceDir">The search path for the default data sources, e. g. the current working
        /// directory</param>
        /// <param name="adminShell">The admin shell to import into, or null if a new admin shell should be
        /// created</param>
        /// <returns>true if at least one submodel was imported</returns>
        public static bool ImportSubmodel(Window window, Aas.Environment env,
            string defaultSourceDir, Aas.IAssetAdministrationShell? adminShell = null)
        {
            adminShell ??= CreateAdminShell(env);
            return PerformImport(window, ImportMode.Submodels, defaultSourceDir,
                    e => e.ImportSubmodelInto(env, adminShell));
        }

        /// <summary>
        /// Shows the import dialog and allows the user to select data from a data provider implementation that is
        /// converted into AAS submodel elements (usually properties and collections) and imported into the given parent
        /// element (usually a submodel).
        /// </summary>
        /// <param name="window">The parent window for the import dialog</param>
        /// <param name="env">The AAS environment to import into</param>
        /// <param name="defaultSourceDir">The search path for the default data sources, e. g. the current working
        /// directory</param>
        /// <param name="parent">The parent element to import into</param>
        /// <returns>true if at least one submodel element was imported</returns>
        public static bool ImportSubmodelElements(Window window, Aas.Environment env,
            string defaultSourceDir, Aas.IReferable parent)
        {
            return PerformImport(window, ImportMode.SubmodelElements, defaultSourceDir,
                e => e.ImportSubmodelElementsInto(env, parent));
        }

        private static bool PerformImport(Window window, ImportMode importMode, string defaultSourceDir,
                Func<Model.IElement, bool> f)
        {
            var dialog = new ImportDialog(window, importMode, defaultSourceDir);
            if (dialog.ShowDialog() != true || dialog.Context == null)
                return false;

            int imported;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                imported = dialog.GetResult().Count(f);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            CheckUnresolvedReferences(dialog.Context);

            return imported > 0;
        }

        private static Aas.AssetAdministrationShell CreateAdminShell(Aas.Environment env)
        {
            var adminShell = new Aas.AssetAdministrationShell("", new Aas.AssetInformation(Aas.AssetKind.Instance))
            {
                Id = AdminShellUtil.GenerateIdAccordingTemplate(Options.Curr.TemplateIdAas)
            };
            env.Add(adminShell);
            return adminShell;
        }

        private static void CheckUnresolvedReferences(Model.IDataContext context)
        {
            if (context.UnknownReferences.Count > 0)
            {
                Log.Singleton.Info(
                    $"Found {context.UnknownReferences.Count} unknown references during import: " +
                    string.Join(", ", context.UnknownReferences));
            }
        }
    }
}
