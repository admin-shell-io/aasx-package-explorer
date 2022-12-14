using Newtonsoft.Json;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AasCore.Aas3_0_RC02.HasDataSpecification
{
    public class EmbeddedDataSpecification
    {
        [JsonIgnore]
        public DataSpecificationContent DataSpecificationContent = null;

        [XmlIgnore]
        [JsonProperty("dataSpecificationContent")]
        public DataSpecificationIEC61360 JsonWrongDataSpec61360
        {
            get { return DataSpecificationContent?.DataSpecificationIEC61360; }
            set
            {
                if (DataSpecificationContent == null)
                    DataSpecificationContent = new DataSpecificationContent();
                DataSpecificationContent.DataSpecificationIEC61360 = value;
            }
        }

        //Global Reference
        public Reference DataSpecification = null;

        //Constructors
        public EmbeddedDataSpecification() { }
        public EmbeddedDataSpecification(Reference src)
        {
            if (src != null)
                this.DataSpecification = new Reference(ReferenceTypes.GlobalReference, src.Keys);
        }

        public EmbeddedDataSpecification(
                Reference dataSpecification,
                DataSpecificationContent dataSpecificationContent)
        {
            this.DataSpecification = dataSpecification;
            this.DataSpecificationContent = dataSpecificationContent;
        }

        public static EmbeddedDataSpecification CreateIEC61360WithContent()
        {
            var eds = new EmbeddedDataSpecification(new Reference(ReferenceTypes.GlobalReference, new List<Key>()), new DataSpecificationContent());

            eds.DataSpecification.Keys.Add(new Key(KeyTypes.GlobalReference, DataSpecificationIEC61360.GetIdentifier()));

            eds.DataSpecificationContent.DataSpecificationIEC61360 =
                DataSpecificationIEC61360.CreateNew();

            return eds;
        }
    }
}