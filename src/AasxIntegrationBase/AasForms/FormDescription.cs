/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

namespace AasxIntegrationBase.AasForms
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
}

namespace AasxIntegrationBase.AasForms
{
    [DisplayName("FormBase")]
    public class FormDescBase
    {
    }

    /// <summary>
    /// Aim: provide a (abstract) communality for Submodel und SubmodelElement.
    /// Host FormTitle, Info, Presets and Semantic Id
    /// </summary>
    [DisplayName("FormSubmodelReferable")]
    public class FormDescReferable : FormDescBase
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
        public List<Aas.ILangStringTextType> PresetDescription = null;

        /// <summary>
        /// SemanticId of the SubmodelElement. Always required.
        /// </summary>
        [JsonProperty(Order = 8)]
        public Aas.IKey KeySemanticId = new Aas.Key(Aas.KeyTypes.GlobalReference, "");

        // Constructors
        //=============

        public FormDescReferable() { }

        public FormDescReferable(
            string formText, Aas.Key keySemanticId, string presetIdShort, string formInfo = null)
            : base()
        {
            this.FormTitle = formText;
            this.KeySemanticId = keySemanticId;
            this.PresetIdShort = presetIdShort;
            this.FormInfo = formInfo;
        }

        public FormDescReferable(FormDescReferable other)
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

#if !DoNotUseAasxCompatibilityModels

        public static List<Aas.ILangStringTextType> ConvertFromV20(AasxCompatibilityModels.AdminShellV20.Description desc)
        {
            var res = new List<Aas.ILangStringTextType>();
            if (desc?.langString != null)
                foreach (var ls in desc.langString)
                    res.Add(new Aas.LangStringTextType(ls?.lang, ls?.str));
            return res;
        }

        public static Aas.Key ConvertFromV20(AasxCompatibilityModels.AdminShellV20.Key key)
        {
            if (key != null)
                return new Aas.Key(
                    Aas.Stringification.KeyTypesFromString(key.type) ?? Aas.KeyTypes.GlobalReference, key.value);
            return null;
        }

        public FormDescReferable(AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescReferableV20 other)
            : base()
        {
            // this part == static, therefore only shallow copy
            this.FormTitle = other.FormTitle;
            this.FormInfo = other.FormInfo;
            this.KeySemanticId = ConvertFromV20(other.KeySemanticId);
            this.PresetIdShort = other.PresetIdShort;
            this.PresetCategory = other.PresetCategory;
            this.PresetDescription = ConvertFromV20(other.PresetDescription);
        }
#endif


        // Dynamic behaviour
        //==================

        protected void InitReferable(Aas.IReferable rf)
        {
            if (rf == null)
                return;

            rf.IdShort = this.PresetIdShort;
            rf.Category = this.PresetCategory;
            if (this.PresetDescription != null)
                rf.Description = this.PresetDescription.Copy();
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
    public class FormDescSubmodel : FormDescReferable
    {
        [JsonProperty(Order = 800)]
        public FormDescListOfElement SubmodelElements = null;

        // Constructors
        //=============

        public FormDescSubmodel() { }

        public FormDescSubmodel(
            string formText, Aas.Key keySemanticId, string presetIdShort, string formInfo = null)
            : base(formText, keySemanticId, presetIdShort, formInfo)
        {
        }

        public FormDescSubmodel(FormDescSubmodel other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.SubmodelElements = other.SubmodelElements;
        }

#if !DoNotUseAasxCompatibilityModels
        public FormDescSubmodel(AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescSubmodelV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.SubmodelElements = new FormDescListOfElement(other.SubmodelElements);
        }
#endif

        // Dynamic behaviour
        //==================

        public void Add(FormDescSubmodelElement elem)
        {
            if (elem == null)
                return;
            if (SubmodelElements == null)
                SubmodelElements = new FormDescListOfElement();
            SubmodelElements.Add(elem);
        }

        /// <summary>
        /// Generates a Submodel with default elements based on the description.
        /// Needs to get a unique Identification.
        /// </summary>
        /// <returns></returns>
        public Aas.Submodel GenerateDefault()
        {
            var res = new Aas.Submodel("");

            // is Referable
            this.InitReferable(res);

            // has SemanticId
            if (this.KeySemanticId != null)
                res.SemanticId = ExtendReference.CreateFromKey(this.KeySemanticId);

            // has elements
            res.SubmodelElements = this.SubmodelElements.GenerateDefault();

            return res;
        }
    }


    /// <summary>
    /// An extension of List(FormDescSubmodelElement), in order to unify the lists of Root and SMC
    /// </summary>
    [DisplayName("FormListOfElement")]
    public class FormDescListOfElement : List<FormDescSubmodelElement>
    {

        // Constructors
        //=============

        public FormDescListOfElement() { }

        public FormDescListOfElement(FormDescListOfElement other)
        {
            if (other == null)
                return;
            foreach (var o in other)
                this.Add(o);
        }

#if !DoNotUseAasxCompatibilityModels
        public static FormDescSubmodelElement CloneFromOld(
            AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescSubmodelElementV20 o)
        {
            if (o is AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescPropertyV20 op)
                return new FormDescProperty(op);
            if (o is AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescMultiLangPropV20 omlp)
                return new FormDescMultiLangProp(omlp);
            if (o is AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescFileV20 ofile)
                return new FormDescFile(ofile);
            if (o is AasxCompatibilityModels.AasxIntegrationBase
                     .AasForms.FormDescSubmodelElementCollectionV20 osmc)
                return new FormDescSubmodelElementCollection(osmc);
            return null;
        }

        public FormDescListOfElement(
            AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescListOfElementV20 other)
        {
            if (other == null)
                return;
            foreach (var o in other)
            {
                var sme = CloneFromOld(o);
                if (sme != null)
                    this.Add(sme);
            }
        }
#endif

        public List<Aas.ISubmodelElement> GenerateDefault()
        {
            var res = new List<Aas.ISubmodelElement>();

            foreach (var desc in this)
            {
                Aas.ISubmodelElement sme = null;

                // generate element

                if (desc is FormDescProperty)
                    sme = (desc as FormDescProperty).GenerateDefault();
                if (desc is FormDescMultiLangProp)
                    sme = (desc as FormDescMultiLangProp).GenerateDefault();
                if (desc is FormDescFile)
                    sme = (desc as FormDescFile).GenerateDefault();
                if (desc is FormDescSubmodelElementCollection)
                    sme = (desc as FormDescSubmodelElementCollection).GenerateDefault();

                // multiplicity -> enumerate correctly
                FormInstanceHelper.MakeIdShortUnique(res, sme);

                if (sme != null)
                    res.Add(sme);
            }

            return res;
        }

    }

    [DisplayName("FormSubmodelElement")]
    public class FormDescSubmodelElement : FormDescReferable
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

        public FormDescSubmodelElement() { }

        public FormDescSubmodelElement(
            string formText, FormMultiplicity multiplicity, Aas.Key keySemanticId, string presetIdShort,
            string formInfo = null, bool isReadOnly = false)
            : base(formText, keySemanticId, presetIdShort, formInfo)
        {
            this.Multiplicity = multiplicity;
            this.IsReadOnly = isReadOnly;
        }

        public FormDescSubmodelElement(FormDescSubmodelElement other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.Multiplicity = other.Multiplicity;
            this.IsReadOnly = other.IsReadOnly;
        }

#if !DoNotUseAasxCompatibilityModels
        public FormDescSubmodelElement(
            AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescSubmodelElementV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.Multiplicity = (FormMultiplicity)((int)other.Multiplicity);
            this.IsReadOnly = other.IsReadOnly;
        }
#endif

        public virtual FormDescSubmodelElement Clone()
        {
            return new FormDescSubmodelElement(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public virtual FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return null;
        }

        public void InitSme(Aas.ISubmodelElement sme)
        {
            // is a Referable
            this.InitReferable(sme);

            // has SemanticId
            if (this.KeySemanticId != null)
                sme.SemanticId = ExtendReference.CreateFromKey(this.KeySemanticId);
        }
    }

    [DisplayName("FormSubmodelElementCollection")]
    public class FormDescSubmodelElementCollection : FormDescSubmodelElement
    {
        /// <summary>
        /// Describes possible members of the SMEC.
        /// </summary>
        [JsonProperty(Order = 800)]
        public FormDescListOfElement value = new FormDescListOfElement();

        // Constructors
        //=============

        public FormDescSubmodelElementCollection() { }

        public FormDescSubmodelElementCollection(
            string formText, FormMultiplicity multiplicity, Aas.Key smeSemanticId, string presetIdShort,
            string formInfo = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo)
        {
        }

        public FormDescSubmodelElementCollection(FormDescSubmodelElementCollection other)
            : base(other)
        {
            if (other.value != null)
                foreach (var ov in other.value)
                    this.value.Add(ov.Clone());
        }

#if !DoNotUseAasxCompatibilityModels
        public FormDescSubmodelElementCollection(
            AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescSubmodelElementCollectionV20 other)
            : base(other)
        {
            if (other.value != null)
                foreach (var ov in other.value)
                {
                    var sme = FormDescListOfElement.CloneFromOld(ov);
                    if (sme != null)
                        this.value.Add(sme);
                }
        }
#endif

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescSubmodelElementCollection(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return new FormInstanceSubmodelElementCollection(parentInstance, this, source);
        }

        // Member management
        //==================

        public void Add(FormDescSubmodelElement subDescription)
        {
            if (value == null)
                value = new FormDescListOfElement();
            value.Add(subDescription);
        }

        public Aas.SubmodelElementCollection GenerateDefault()
        {
            var res = new Aas.SubmodelElementCollection();
            this.InitSme(res);

            res.Value = this.value.GenerateDefault();

            return res;
        }
    }

    [DisplayName("FormProperty")]
    public class FormDescProperty : FormDescSubmodelElement
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

        public FormDescProperty() { }

        public FormDescProperty(
            string formText, FormMultiplicity multiplicity, Aas.Key smeSemanticId,
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

        public FormDescProperty(FormDescProperty other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.allowedValueTypes = other.allowedValueTypes;
            this.presetValue = other.presetValue;
            this.comboBoxChoices = other.comboBoxChoices;
            this.valueFromComboBoxIndex = other.valueFromComboBoxIndex;
        }

#if !DoNotUseAasxCompatibilityModels
        public FormDescProperty(AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescPropertyV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.allowedValueTypes = other.allowedValueTypes;
            this.presetValue = other.presetValue;
            this.comboBoxChoices = other.comboBoxChoices;
            this.valueFromComboBoxIndex = other.valueFromComboBoxIndex;
        }
#endif

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescProperty(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return new FormInstanceProperty(parentInstance, this, source);
        }

        public Aas.Property GenerateDefault()
        {
            var res = new Aas.Property(Aas.DataTypeDefXsd.String);
            this.InitSme(res);
            if (this.presetValue != null)
                res.Value = this.presetValue;
            if (this.allowedValueTypes.Length == 1)
            {
                res.ValueType = Aas.Stringification.DataTypeDefXsdFromString(this.allowedValueTypes[0])
                    ?? Aas.DataTypeDefXsd.String;
            }
            return res;
        }
    }

    [DisplayName("FormMultiLangProp")]
    public class FormDescMultiLangProp : FormDescSubmodelElement
    {
        public static string[] DefaultLanguages = new string[] { "de", "en", "fr", "es", "it", "zh", "ko", "ja" };

        public FormDescMultiLangProp() { }

        // Constructors
        //=============

        public FormDescMultiLangProp(
            string formText, FormMultiplicity multiplicity, Aas.Key smeSemanticId, string presetIdShort,
            string formInfo = null, bool isReadOnly = false)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
        }

        public FormDescMultiLangProp(FormDescMultiLangProp other)
            : base(other)
        {
        }

#if !DoNotUseAasxCompatibilityModels
        public FormDescMultiLangProp(
            AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescMultiLangPropV20 other)
            : base(other)
        {
        }
#endif

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return new FormInstanceMultiLangProp(parentInstance, this, source);
        }

        public Aas.MultiLanguageProperty GenerateDefault()
        {
            var res = new Aas.MultiLanguageProperty();
            this.InitSme(res);
            return res;
        }

    }

    [DisplayName("FormFile")]
    public class FormDescFile : FormDescSubmodelElement
    {
        /// <summary>
        /// pre-set the editable MimeType value with this value.
        /// </summary>
        [JsonProperty(Order = 20)]
        public string presetMimeType = "";

        public FormDescFile() { }

        // Constructors
        //=============

        public FormDescFile(
            string formText, FormMultiplicity multiplicity, Aas.Key smeSemanticId,
            string presetIdShort, string formInfo = null, bool isReadOnly = false,
            string presetMimeType = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
            if (presetMimeType != null)
                this.presetMimeType = presetMimeType;
        }

        public FormDescFile(FormDescFile other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.presetMimeType = other.presetMimeType;
        }

#if !DoNotUseAasxCompatibilityModels
        public FormDescFile(AasxCompatibilityModels.AasxIntegrationBase.AasForms.FormDescFileV20 other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.presetMimeType = other.presetMimeType;
        }
#endif


        public override FormDescSubmodelElement Clone()
        {
            return new FormDescFile(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return new FormInstanceFile(parentInstance, this, source);
        }

        public Aas.ISubmodelElement GenerateDefault()
        {
            var res = new Aas.File("");
            this.InitSme(res);
            if (this.presetMimeType != null)
                res.ContentType = this.presetMimeType;
            return res;
        }

    }

    [DisplayName("FormReferenceElement")]
    public class FormDescReferenceElement : FormDescSubmodelElement
    {
        /// <summary>
        /// pre-set a filter for allowed SubmodelElement types.
        /// </summary>
        [JsonProperty(Order = 20)]
        public string presetFilter = "";

        public FormDescReferenceElement() { }

        // Constructors
        //=============

        public FormDescReferenceElement(
            string formText, FormMultiplicity multiplicity, Aas.Key smeSemanticId,
            string presetIdShort, string formInfo = null, bool isReadOnly = false,
            string presetFilter = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
            if (presetFilter != null)
                this.presetFilter = presetFilter;
        }

        public FormDescReferenceElement(FormDescReferenceElement other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.presetFilter = other.presetFilter;
        }

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescReferenceElement(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return new FormInstanceReferenceElement(parentInstance, this, source);
        }

        public Aas.ReferenceElement GenerateDefault()
        {
            var res = new Aas.ReferenceElement();
            this.InitSme(res);
            return res;
        }

    }

    [DisplayName("FormDescRelationshipElement")]
    public class FormDescRelationshipElement : FormDescSubmodelElement
    {
        /// <summary>
        /// pre-set a filter for allowed SubmodelElement types.
        /// </summary>
        [JsonProperty(Order = 20)]
        public string presetFilter = "";

        public FormDescRelationshipElement() { }

        // Constructors
        //=============

        public FormDescRelationshipElement(
            string formText, FormMultiplicity multiplicity, Aas.Key smeSemanticId,
            string presetIdShort, string formInfo = null, bool isReadOnly = false,
            string presetFilter = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
            if (presetFilter != null)
                this.presetFilter = presetFilter;
        }

        public FormDescRelationshipElement(FormDescRelationshipElement other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.presetFilter = other.presetFilter;
        }

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescRelationshipElement(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return new FormInstanceRelationshipElement(parentInstance, this, source);
        }

        public Aas.RelationshipElement GenerateDefault()
        {
            var res = new Aas.RelationshipElement(null, null);
            this.InitSme(res);
            return res;
        }

    }

    [DisplayName("FormDescCapability")]
    public class FormDescCapability : FormDescSubmodelElement
    {
        /// <summary>
        /// pre-set a filter for allowed SubmodelElement types.
        /// </summary>
        [JsonProperty(Order = 20)]
        public string presetFilter = "";

        public FormDescCapability() { }

        // Constructors
        //=============

        public FormDescCapability(
            string formText, FormMultiplicity multiplicity, Aas.Key smeSemanticId,
            string presetIdShort, string formInfo = null, bool isReadOnly = false,
            string presetFilter = null)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
            if (presetFilter != null)
                this.presetFilter = presetFilter;
        }

        public FormDescCapability(FormDescCapability other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.presetFilter = other.presetFilter;
        }

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescCapability(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, Aas.ISubmodelElement source = null)
        {
            return new FormInstanceCapability(parentInstance, this, source);
        }

        public Aas.Capability GenerateDefault()
        {
            var res = new Aas.Capability();
            this.InitSme(res);
            return res;
        }

    }

}
