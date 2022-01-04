/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using AasxIntegrationBase;
using Newtonsoft.Json;

#if !DoNotUseAasxCompatibilityModels

namespace AasxCompatibilityModels.AasxIntegrationBase.AasForms
{
    /// <summary>
    /// Possible multiplicities for form description elements. This says, how many of a e.g.
    /// Property with _same__ SemanticId shall be placed in a collection.
    /// Outside the FormDescription class only for shorter names.
    /// </summary>
    public enum FormMultiplicity { ZeroToOne = 0, One, ZeroToMany, OneToMany };

    public static class AasFormConstants
    {
        public static string[] FormMultiplicityAsUmlCardinality = new string[] { "0..1", "1", "0..*", "1..*" };
    }

    [DisplayName("FormBase")]
    public class FormDescBaseV20
    {
    }

    /// <summary>
    /// Aim: provide a (abstract) communality for Submodel und SubmodelElement.
    /// Host FormTitle, Info, Presets and Semantic Id
    /// </summary>
    [DisplayName("FormSubmodelReferable")]
    public class FormDescReferableV20 : FormDescBaseV20
    {
        /// <summary>
        /// Displayed as label in front/ on top of the SubmodelElement
        /// </summary>
        [JsonProperty(Order = 1)]
        public string FormTitle = "";

        /// <summary>
        /// Displayed on demand / very small. Might contain a longer text.
        /// </summary>
        [JsonProperty(Order = 2)]
        public string FormInfo = null;

        /// <summary>
        /// True, if the user shall be able to edit the idShort of the Referable
        /// </summary>
        [JsonProperty(Order = 3)]
        public bool FormEditIdShort = false;

        /// <summary>
        /// True, if the user shall be able to edit the description of the Referable
        /// </summary>
        [JsonProperty(Order = 4)]
        public bool FormEditDescription = false;

        /// <summary>
        /// Indicated by a small (i) symbol allows to jump to web-browser showing the URL (inluding fragment).
        /// </summary>
        public string FormUrl = null;

        /// <summary>
        /// Preset for Referable.idShort. Always required. "{0}" will be replaced by instance number
        /// </summary>
        [JsonProperty(Order = 5)]
        public string PresetIdShort = "SME{0:000}";

        /// <summary>
        /// Preset for Referable.category. Always required
        /// </summary>
        [JsonProperty(Order = 6)]
        public string PresetCategory = "CONSTANT";

        /// <summary>
        /// Preset for Referable.description
        /// </summary>
        [JsonProperty(Order = 7)]
        public AdminShellV20.Description PresetDescription = null;

        /// <summary>
        /// SemanticId of the SubmodelElement. Always required.
        /// </summary>
        [JsonProperty(Order = 8)]
        public AdminShellV20.Key KeySemanticId = new AdminShellV20.Key();

        // Constructors
        //=============

        public FormDescReferableV20() { }

        public FormDescReferableV20(
            string formText, AdminShellV20.Key keySemanticId, string presetIdShort, string formInfo = null)
            : base()
        {
            this.FormTitle = formText;
            this.KeySemanticId = keySemanticId;
            this.PresetIdShort = presetIdShort;
            this.FormInfo = formInfo;
        }

        public FormDescReferableV20(FormDescReferableV20 other)
            : base()
        {
            // this part == static, therefore only shallow copy
            this.FormTitle = other.FormTitle;
            this.FormInfo = other.FormInfo;
            this.KeySemanticId = other.KeySemanticId;
            this.PresetIdShort = other.PresetIdShort;
            this.PresetCategory = other.PresetCategory;
            this.PresetDescription = other.PresetDescription;
        }

    }

    /// <summary>
    /// An specialization of <c>FormDescListOfElement</c>. Defines a total Submodel to be represented by the form,
    /// that is,
    /// no outer structures tha form instances will be in the Submodel. The Plugin's will match to
    /// the <c>SemanticId</c> of the
    /// Submodel, therefore it has to be present.
    /// </summary>
    [DisplayName("FormSubmodel")]
    public class FormDescSubmodelV20 : FormDescReferableV20
    {
        [JsonProperty(Order = 800)]
        public FormDescListOfElementV20 SubmodelElements = null;

        // Constructors
        //=============

        public FormDescSubmodelV20() { }

        public FormDescSubmodelV20(
            string formText, AdminShellV20.Key keySemanticId, string presetIdShort, string formInfo = null)
            : base(formText, keySemanticId, presetIdShort, formInfo)
        {
        }

        public FormDescSubmodelV20(FormDescSubmodelV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.SubmodelElements = other.SubmodelElements;
        }
    }


    /// <summary>
    /// An extension of List(FormDescSubmodelElement), in order to unify the lists of Root and SMC
    /// </summary>
    [DisplayName("FormListOfElement")]
    public class FormDescListOfElementV20 : List<FormDescSubmodelElementV20>
    {

        // Constructors
        //=============

        public FormDescListOfElementV20() { }

        public FormDescListOfElementV20(FormDescListOfElementV20 other)
        {
            if (other == null)
                return;
            foreach (var o in other)
                this.Add(o);
        }

    }

    [DisplayName("FormSubmodelElement")]
    public class FormDescSubmodelElementV20 : FormDescReferableV20
    {
        /// <summary>
        /// In the containing collection, how often shall the SubmodelElement might occur?
        /// </summary>
        [JsonProperty(Order = 10)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public FormMultiplicity Multiplicity = FormMultiplicity.One;

        /// <summary>
        /// If set to true, the property is displayed but not editable by the user.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool IsReadOnly = false;

        /// <summary>
        /// If not null, this SME will subscribe to events from a "master" SME which is identifed
        /// by an IdShort starting with this string.
        /// </summary>
        public string SlaveOfIdShort = null;

        // Constructors
        //=============

        public FormDescSubmodelElementV20() { }

        public FormDescSubmodelElementV20(
            string formText, FormMultiplicity multiplicity, AdminShell.Identifier singleSemanticId, string presetIdShort,
            string formInfo = null, bool isReadOnly = false)
            : base(formText, singleSemanticId, presetIdShort, formInfo)
        {
            this.Multiplicity = multiplicity;
            this.IsReadOnly = isReadOnly;
        }

        public FormDescSubmodelElementV20(FormDescSubmodelElementV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.Multiplicity = other.Multiplicity;
            this.IsReadOnly = other.IsReadOnly;
        }

    }

    [DisplayName("FormSubmodelElementCollection")]
    public class FormDescSubmodelElementCollectionV20 : FormDescSubmodelElementV20
    {
        /// <summary>
        /// Describes possible members of the SMEC.
        /// </summary>
        [JsonProperty(Order = 800)]
        public FormDescListOfElementV20 value = new FormDescListOfElementV20();

        // Constructors
        //=============

        public FormDescSubmodelElementCollectionV20() { }

        public FormDescSubmodelElementCollectionV20(
            string formText, FormMultiplicity multiplicity, AdminShell.Identifier smeSemanticId, string presetIdShort,
            string formInfo = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo)
        {
        }

        public FormDescSubmodelElementCollectionV20(FormDescSubmodelElementCollectionV20 other)
            : base(other)
        {
            if (other.value != null)
                foreach (var ov in other.value)
                    this.value.Add(new FormDescSubmodelElementV20(ov));
        }

    }

    [DisplayName("FormProperty")]
    public class FormDescPropertyV20 : FormDescSubmodelElementV20
    {
        /// <summary>
        /// Pre-set the Property with this valueType. Right now, only one item (and this shall be "string") is allowed!
        /// </summary>
        [JsonProperty(Order = 20)]
        public string[] allowedValueTypes = new string[] { "string" };

        /// <summary>
        /// Pre-set the editable Property value with this value.
        /// </summary>
        [JsonProperty(Order = 21)]
        public string presetValue = "";

        /// <summary>
        /// Allows to define combox box items for the value.
        /// </summary>
        [JsonProperty(Order = 22)]
        public string[] comboBoxChoices = null;

        /// <summary>
        /// If not null, take the combox box index and map into to the given field of values.
        /// To be used in combination with comboBoxChoices[].
        /// If null, then use the comboBoxChoices[] to set the value, which is also editable.
        /// </summary>
        [JsonProperty(Order = 23)]
        public string[] valueFromComboBoxIndex = null;

        /// <summary>
        /// If not null, contains a dictionary mapping possible master values (that is: strings)
        /// into this SME's values.
        /// </summary>
        [JsonProperty(Order = 23)]
        public Dictionary<string, string> valueFromMasterValue = null;

        // Constructors
        //=============

        public FormDescPropertyV20() { }

        public FormDescPropertyV20(
            string formText, FormMultiplicity multiplicity, AdminShell.Identifier smeSemanticId,
            string presetIdShort, string formInfo = null, bool isReadOnly = false, string valueType = null,
            string presetValue = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
            // init
            if (valueType != null)
                this.allowedValueTypes = new[] { valueType };
            if (presetValue != null)
                this.presetValue = presetValue;
        }

        public FormDescPropertyV20(FormDescPropertyV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.allowedValueTypes = other.allowedValueTypes;
            this.presetValue = other.presetValue;
            this.comboBoxChoices = other.comboBoxChoices;
            this.valueFromComboBoxIndex = other.valueFromComboBoxIndex;
        }

    }

    [DisplayName("FormMultiLangProp")]
    public class FormDescMultiLangPropV20 : FormDescSubmodelElementV20
    {
        public static string[] DefaultLanguages = new string[] { "de", "en", "fr", "es", "it", "cn", "kr" };

        public FormDescMultiLangPropV20() { }

        // Constructors
        //=============

        public FormDescMultiLangPropV20(
            string formText, FormMultiplicity multiplicity, AdminShell.Identifier smeSemanticId, string presetIdShort,
            string formInfo = null, bool isReadOnly = false)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
        }

        public FormDescMultiLangPropV20(FormDescMultiLangPropV20 other)
            : base(other)
        {
        }
    }

    [DisplayName("FormFile")]
    public class FormDescFileV20 : FormDescSubmodelElementV20
    {
        /// <summary>
        /// pre-set the editable MimeType value with this value.
        /// </summary>
        [JsonProperty(Order = 20)]
        public string presetMimeType = "";

        public FormDescFileV20() { }

        // Constructors
        //=============

        public FormDescFileV20(
            string formText, FormMultiplicity multiplicity, AdminShell.Identifier smeSemanticId,
            string presetIdShort, string formInfo = null, bool isReadOnly = false,
            string presetMimeType = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
            if (presetMimeType != null)
                this.presetMimeType = presetMimeType;
        }

        public FormDescFileV20(FormDescFileV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.presetMimeType = other.presetMimeType;
        }
    }

    [DisplayName("FormReferenceElement")]
    public class FormDescReferenceElementV20 : FormDescSubmodelElementV20
    {
        /// <summary>
        /// pre-set a filter for allowed SubmodelElement types.
        /// </summary>
        [JsonProperty(Order = 20)]
        public string presetFilter = "";

        public FormDescReferenceElementV20() { }

        // Constructors
        //=============

        public FormDescReferenceElementV20(
            string formText, FormMultiplicity multiplicity, AdminShell.Identifier smeSemanticId,
            string presetIdShort, string formInfo = null, bool isReadOnly = false,
            string presetFilter = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
            if (presetFilter != null)
                this.presetFilter = presetFilter;
        }

        public FormDescReferenceElementV20(FormDescReferenceElementV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.presetFilter = other.presetFilter;
        }
    }

}

#endif