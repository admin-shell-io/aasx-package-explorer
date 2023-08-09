/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using Newtonsoft.Json;

// ReSharper disable All .. as this is legacy code!

// Note: not clear why, but this file needs to be in the __SAME FOLDER__ as
// the normale options class!

#if !DoNotUseAasxCompatibilityModels

namespace AasxCompatibilityModels.AasxPluginGenericForms
{
    [DisplayName("Record_V20")]
    public class GenericFormsOptionsRecordV20
    {
        /// <summary>
        /// Shall always contain 3-4 digit tag label, individual for each template
        /// </summary>
        public string FormTag = "";

        /// <summary>
        /// Shall always contain 3-4 digit tag title, individual for each template
        /// </summary>
        public string FormTitle = "";

        /// <summary>
        /// Full (recursive) description for Submodel to be generated
        /// </summary>
        public AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescSubmodelV20 FormSubmodel = null;

        /// <summary>
        /// A list with required concept descriptions, if appropriate.
        /// </summary>
        public AdminShellV20.ListOfConceptDescriptions ConceptDescriptions = null;
    }

    [DisplayName("Options_V20")]
    public class GenericFormOptionsV20
    {
        //
        // Constants
        //

        //
        // Option fields
        //

        public List<GenericFormsOptionsRecordV20> Records = new List<GenericFormsOptionsRecordV20>();
    }
}

#endif