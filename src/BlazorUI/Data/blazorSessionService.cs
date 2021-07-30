/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BlazorUI.Data
{
    public class blazorSessionService:IDisposable
    {
        public AdminShellPackageEnv env = null;
        public string[] aasxFiles = new string[1];
        public string aasxFileSelected = "";
        public bool editMode = false;
        public bool hintMode = true;
        public PackageCentral packages = null;
        public PackageContainerListHttpRestRepository repository = null;
        public DispEditHelperEntities helper = null;
        public ModifyRepo repo = null;
        public PackageCentral _packageCentral = null;
        public PackageContainerBase container = null;

        public AnyUiStackPanel stack = new AnyUiStackPanel();
        public AnyUiStackPanel stack2 = new AnyUiStackPanel();
        public AnyUiStackPanel stack17 = new AnyUiStackPanel();

        public string thumbNail = null;

        public static int sessionCounter = 0;
        public int sessionNumber = 0;
        public static int sessionTotal = 0;
        public List<Item> items = null;
        public Thread htmlDotnetThread = null;

        public blazorSessionService()
        {
            sessionNumber = ++sessionCounter;
            sessionTotal++;
            AnyUiDisplayContextHtml.addSession(sessionNumber);

            packages = new PackageCentral();
            _packageCentral = packages;

            env = null;

            helper = new DispEditHelperEntities();
            helper.levelColors = DispLevelColors.GetLevelColorsFromOptions(Options.Curr);
            // some functionality still uses repo != null to detect editMode!!
            repo = new ModifyRepo();
            helper.editMode = editMode;
            helper.hintMode = hintMode;
            helper.repo = repo;
            helper.context = null;
            helper.packages = packages;

            stack17 = new AnyUiStackPanel();
            stack17.Orientation = AnyUiOrientation.Vertical;

            if (env != null && env.AasEnv != null && env.AasEnv.AdministrationShells != null)
                helper.DisplayOrEditAasEntityAas(
                        packages, env.AasEnv, env.AasEnv.AdministrationShells[0], editMode, stack17, hintMode: hintMode);

            htmlDotnetThread = new Thread(AnyUiDisplayContextHtml.htmlDotnetLoop);
            htmlDotnetThread.Start();
        }
    public void Dispose()
        {
            AnyUiDisplayContextHtml.deleteSession(sessionNumber);
            sessionTotal--;
            if (env != null)
                env.Close();
            // throw new NotImplementedException();
        }
    }
}
