/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using Aas = AasCore.Aas3_0;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel Digital Nameplate 
    /// </summary>
    public class ZveiNameplateV10 : AasxDefinitionBase
    {
        public static ZveiNameplateV10 Static = new ZveiNameplateV10();

        public Aas.Submodel
            SM_Nameplate;

        public Aas.ConceptDescription
            CD_ManNam,
            CD_ManProDes,
            CD_Add,
            CD_Dep,
            CD_Str,
            CD_ZipCod,
            CD_POBox,
            CD_ZipCodOfPOBox,
            CD_CitTow,
            CD_StaCou,
            CD_NatCod,
            CD_VATNum,
            CD_AddRem,
            CD_AddOfAddLin,
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
            CD_ManProFam,
            CD_SerNum,
            CD_YeaOfCon,
            CD_Markings,
            CD_Marking,
            CD_MarkingName,
            CD_MarkingFile,
            CD_MarkingAdditionalText,
            CD_AssetSpecificProperties,
            CD_GuidelineSpecificProperties,
            CD_GuiForConDec;

        public ZveiNameplateV10()
        {
            // info
            this.DomainInfo = "Digital Nameplate (ZVE) V1.0 (deprecated)";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "ZveiNameplateV10.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(ZveiNameplateV10), useFieldNames: true);
        }
    }
}
