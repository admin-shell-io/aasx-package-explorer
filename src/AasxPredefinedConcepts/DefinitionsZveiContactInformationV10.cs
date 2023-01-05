/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Stefan Erler

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using AdminShellNS;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel ContactInformation
    /// </summary>
    public class ZveiContactInformationV10 : AasxDefinitionBase
    {
        public static ZveiContactInformationV10 Static = new ZveiContactInformationV10();

        public Submodel
            SM_ContactInformation;

        public ConceptDescription
        CD_ContactInformation,
        CD_RolOfConPer,
        CD_NatCod,
        CD_Lan,
        CD_CitTow,
        CD_NamOfSup,
        CD_Dep,

        CD_Pho,
        CD_TelNum,
        CD_TypOfTel,

        CD_Fax,
        CD_FaxNum,
        CD_TypOfFaxNum,

        CD_Ema,
        CD_EmaAdd,
        CD_PubKey,
        CD_TypOfEmaAdd,
        CD_TypOfPubKey,

        CD_IPCom,
        CD_AddOfAddLin,
        CD_TypOfCom,

        CD_Str,
        CD_ZipCod,
        CD_POBox,
        CD_ZipCodOfPOBox,
        CD_StaCou,
        CD_NamOfCon,
        CD_FirNam,
        CD_MidNam,
        CD_Tit,
        CD_ActTit,
        CD_FurDetOfCon;

        public ZveiContactInformationV10()
        {
            // info
            this.DomainInfo = "ZVEI Contact Information (V1.0)";

            // AasCore.Aas3_0_RC02.IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(),
                "AasxPredefinedConcepts.Resources." + "ZveiContactInformationV10.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(ZveiContactInformationV10), useFieldNames: true);
        }
    }
}
