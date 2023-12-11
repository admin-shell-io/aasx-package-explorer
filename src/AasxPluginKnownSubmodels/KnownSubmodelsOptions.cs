/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;

namespace AasxPluginKnownSubmodels
{
    public class KnownSubmodelsOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public string Header;
        public string Content;
        public string ImageLink;
        public string FurtherUrl;
    }

    public class KnownSubmodelsOptions : AasxPluginLookupOptionsBase
    {
        public List<KnownSubmodelsOptionsRecord> Records = new List<KnownSubmodelsOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static KnownSubmodelsOptions CreateDefault()
        {
            var opt = new KnownSubmodelsOptions();

            opt.Records.Add(new KnownSubmodelsOptionsRecord()
            {
                Header = "Contact Information (IDTA) V1.0",
                Content = "This Submodel template aims at interoperable provision of contact information in regard " +
                    "to the asset of the respective Asset Administration Shell. " +
                    "The intended use-case is the provision of a standardized property structure for contact " +
                    "information, which can effectively accelerate the preperation for asset maintenance.",
                ImageLink = "AasxPluginKnownSubmodels.media\\SMT_ZveiContactInformation10.png",
                FurtherUrl = "https://github.com/admin-shell-io/id",
                AllowSubmodelSemanticId = (new Aas.Key[] {
                    AasxPredefinedConcepts.IdtaContactInformationV10.Static.SM_ContactInformations.GetSemanticKey()
                }.ToList())
            });

            opt.Records.Add(new KnownSubmodelsOptionsRecord()
            {
                Header = "ZVEI Digital Nameplate (Version 1.0)",
                Content = "This Submodel template aims at interoperable provision of information describing the " +
                    "nameplate of the asset of the respective Asset Administration Shell. The intended use-case " +
                    "is the provision of a standardized property structure within a digital nameplate, which " +
                    "enables the interoperability of digital nameplates from different manufacturers.",
                ImageLink = "AasxPluginKnownSubmodels.media\\SMT_ZveiDigitalNameplate10.png",
                FurtherUrl = "https://github.com/admin-shell-io/id",
                AllowSubmodelSemanticId = (new Aas.Key[] {
                    AasxPredefinedConcepts.ZveiNameplateV10.Static.SM_Nameplate.GetSemanticKey()
                }.ToList())
            });

            opt.Records.Add(new KnownSubmodelsOptionsRecord()
            {
                Header = "VDMA Article of trade information (Version 0.8)",
                Content = "This Submodel template aims at an interoperable provision of information on articles of " +
                    "trade. These articles of trade are typically provided by manufacturers and suppliers, " +
                    "including dealers, and used by industrial users, e.g. original equipment manufacturers (OEMs), " +
                    "system integrators and producing enterprises (industrial end users). ",
                ImageLink = "AasxPluginKnownSubmodels.media\\SMT_VdmaArticleOfTradeInformation.png",
                FurtherUrl = "https://github.com/admin-shell-io/id",
                AllowSubmodelSemanticId = (new Aas.Key[] {
                    new Aas.Key(Aas.KeyTypes.Submodel, "https://admin-shell.io/sandbox/vdma/article-information/0/8")
                }.ToList())
            });

            opt.Records.Add(new KnownSubmodelsOptionsRecord()
            {
                Header = "VDMA Handover information for engineering authoring systems (Version 0.8)",
                Content = "The aim of this Submodel is to digitialize and interoperably convey sets of information " +
                    "to faciliate and ease engineering tasks using industrial components. Engineering authoring " +
                    "systems are considered all systems concerned with selecting, dimensioning, simulating, " +
                    "constructing and sketching industrial systems. For the time being, this document focuses " +
                    "on electrical and fluidic engineering.",
                ImageLink = "AasxPluginKnownSubmodels.media\\SMT_VdmaHandoverEngSystems.png",
                FurtherUrl = "https://github.com/admin-shell-io/id",
                AllowSubmodelSemanticId = (new Aas.Key[] {
                    new Aas.Key(Aas.KeyTypes.Submodel, "https://admin-shell.io/sandbox/idta/handover/EFCAD/0/1/")
                }.ToList())
            });

            opt.Records.Add(new KnownSubmodelsOptionsRecord()
            {
                Header = "VDMA Parameter information of industrial equipment (Version 0.8)",
                Content = "This Submodel template aims at interoperable provision of basic parameter information " +
                    "of industrial equipment. The aim of this Submodel template is to capture parameter " +
                    "information in specific points of time within the life cycle and for specific interactions " +
                    "between roles of value chain partners. The Submodel template provides an interoperable " +
                    "container for specific parameter information, which comes in varying structure and format.",
                ImageLink = "AasxPluginKnownSubmodels.media\\SMT_VdmaParameterInformation.png",
                FurtherUrl = "https://github.com/admin-shell-io/id",
                AllowSubmodelSemanticId = (new Aas.Key[] {
                    new Aas.Key(Aas.KeyTypes.Submodel, "https://admin-shell.io/sandbox/vdma/parameter-information/0/8")
                }.ToList())
            });

            opt.Records.Add(new KnownSubmodelsOptionsRecord()
            {
                Header = "VDMA Product Change Notifications for industrial product types and items in " +
                    "manufacturing (Version 0.8)",
                Content = "This Submodel template aims at interoperable provision of product change notifications " +
                "between suppliers and users of industrial product types and items, particularly industrial " +
                "components. The intended use-case is, that a manufacturer of industrial product types and items " +
                "makes these product change notifications digitally available in a way, that these are " +
                "interoperable and unambiguously understood by the other market participants, such as OEMs, " +
                "system integrators or operators of industrial equipment. ",
                ImageLink = "AasxPluginKnownSubmodels.media\\SMT_VdmaProductChangeNotification.png",
                FurtherUrl = "https://github.com/admin-shell-io/id",
                AllowSubmodelSemanticId = (new Aas.Key[] {
                    new Aas.Key(Aas.KeyTypes.Submodel, "0173-10029#01-XFB001#001")
                }.ToList())
            });

            return opt;
        }
    }
}
