using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;

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
        public AdminShell.Description PresetDescription = null;

        /// <summary>
        /// SemanticId of the SubmodelElement. Always required.
        /// </summary>
        [JsonProperty(Order = 8)]
        public AdminShell.Key KeySemanticId = new AdminShell.Key();

        // Constructors
        //=============

        public FormDescReferable() { }

        public FormDescReferable(
            string formText, AdminShell.Key keySemanticId, string presetIdShort, string formInfo = null)
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


        // Dynamic behaviour
        //==================

        protected void InitReferable(AdminShell.Referable rf)
        {
            if (rf == null)
                return;

            rf.idShort = this.PresetIdShort;
            rf.category = this.PresetCategory;
            if (this.PresetDescription != null)
                rf.description = new AdminShell.Description(this.PresetDescription);
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
            string formText, AdminShell.Key keySemanticId, string presetIdShort, string formInfo = null)
            : base(formText, keySemanticId, presetIdShort, formInfo)
        {
        }

        public FormDescSubmodel(FormDescSubmodel other)
            : base(other)
        {
            // this part == static, therefore only shallow copy
            this.SubmodelElements = other.SubmodelElements;
        }


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
        public AdminShell.Submodel GenerateDefault()
        {
            var res = new AdminShell.Submodel();

            // is Referable
            this.InitReferable(res);

            // has SemanticId
            if (this.KeySemanticId != null)
                res.semanticId = AdminShell.SemanticId.CreateFromKey(this.KeySemanticId);

            // has elements
            res.submodelElements = this.SubmodelElements.GenerateDefault();

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

        public AdminShell.SubmodelElementWrapperCollection GenerateDefault()
        {
            var res = new AdminShell.SubmodelElementWrapperCollection();

            foreach (var desc in this)
            {
                AdminShell.SubmodelElement sme = null;

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
            string formText, FormMultiplicity multiplicity, AdminShell.Key keySemanticId, string presetIdShort,
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

        public virtual FormDescSubmodelElement Clone()
        {
            return new FormDescSubmodelElement(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public virtual FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, AdminShell.SubmodelElement source = null)
        {
            return null;
        }

        public void InitSme(AdminShell.SubmodelElement sme)
        {
            // is a Referable
            this.InitReferable(sme);

            // has SemanticId
            if (this.KeySemanticId != null)
                sme.semanticId = AdminShell.SemanticId.CreateFromKey(this.KeySemanticId);
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
            string formText, FormMultiplicity multiplicity, AdminShell.Key smeSemanticId, string presetIdShort,
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

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescSubmodelElementCollection(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, AdminShell.SubmodelElement source = null)
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

        public AdminShell.SubmodelElementCollection GenerateDefault()
        {
            var res = new AdminShell.SubmodelElementCollection();
            this.InitSme(res);

            res.value = this.value.GenerateDefault();

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
            string formText, FormMultiplicity multiplicity, AdminShell.Key smeSemanticId,
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

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescProperty(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, AdminShell.SubmodelElement source = null)
        {
            return new FormInstanceProperty(parentInstance, this, source);
        }

        public AdminShell.Property GenerateDefault()
        {
            var res = new AdminShell.Property();
            this.InitSme(res);
            if (this.presetValue != null)
                res.value = this.presetValue;
            return res;
        }
    }

    [DisplayName("FormMultiLangProp")]
    public class FormDescMultiLangProp : FormDescSubmodelElement
    {
        public static string[] DefaultLanguages = new string[] { "DE", "EN", "FR", "ES", "IT", "CN", "KR" };

        public FormDescMultiLangProp() { }

        // Constructors
        //=============

        public FormDescMultiLangProp(
            string formText, FormMultiplicity multiplicity, AdminShell.Key smeSemanticId, string presetIdShort,
            string formInfo = null, bool isReadOnly = false)
            : base(formText, multiplicity, smeSemanticId, presetIdShort, formInfo, isReadOnly)
        {
        }

        public FormDescMultiLangProp(FormDescMultiLangProp other)
            : base(other)
        {
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, AdminShell.SubmodelElement source = null)
        {
            return new FormInstanceMultiLangProp(parentInstance, this, source);
        }

        public AdminShell.MultiLanguageProperty GenerateDefault()
        {
            var res = new AdminShell.MultiLanguageProperty();
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
            string formText, FormMultiplicity multiplicity, AdminShell.Key smeSemanticId,
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

        public override FormDescSubmodelElement Clone()
        {
            return new FormDescFile(this);
        }

        /// <summary>
        /// Build a new instance, based on the description data
        /// </summary>
        public override FormInstanceSubmodelElement CreateInstance(
            FormInstanceListOfSame parentInstance, AdminShell.SubmodelElement source = null)
        {
            return new FormInstanceFile(parentInstance, this, source);
        }

        public AdminShell.File GenerateDefault()
        {
            var res = new AdminShell.File();
            this.InitSme(res);
            if (this.presetMimeType != null)
                res.mimeType = this.presetMimeType;
            return res;
        }

    }
}
