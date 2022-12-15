using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AasCore.Aas3_0_RC02.HasDataSpecification
{
    public class HasDataSpecification : List <EmbeddedDataSpecification>
    {
        [XmlIgnore]
        //[JsonIgnore]
        public EmbeddedDataSpecification IEC61360
        {
            get
            {
                foreach (var eds in this)
                {
                    if (eds?.DataSpecificationContent?.DataSpecificationIEC61360 != null)
                    {
                        return eds;
                    }

                    //parallel logic to: eds?.DataSpecification?.Matches(DataSpecificationIEC61360.GetIdentifier(), Key.MatchMode.Identification) == true
                    if (eds.DataSpecification.Keys?.Count == 1)
                    {
                        var key = eds.DataSpecification.Keys[0];
                        if (key != null && key.Value.Equals(DataSpecificationIEC61360.GetIdentifier()))
                        {
                            return eds;
                        }
                    }
                }
                    
                return null;
            }
            set
            {
                // search existing first?
                var eds = this.IEC61360;
                if (eds != null)
                {
                    // replace this
                    /* TODO (MIHO, 2020-08-30): this does not prevent the corner case, that we could have
                        * multiple dataSpecificationIEC61360 in this list, which would be an error */
                    this.Remove(eds);
                    this.Add(value);
                    return;
                }

                // no? .. add!
                this.Add(value);
            }
        }
        [XmlIgnore]
        //[JsonIgnore]
        public DataSpecificationIEC61360 IEC61360Content
        {
            get
            {
                return this.IEC61360?.DataSpecificationContent?.DataSpecificationIEC61360;
            }
            set
            {
                // search existing first?
                var eds = this.IEC61360;
                if (eds != null)
                {
                    // replace this
                    eds.DataSpecificationContent.DataSpecificationIEC61360 = value;
                    return;
                }
                // no? .. add!
                var edsnew = new EmbeddedDataSpecification();
                edsnew.DataSpecificationContent.DataSpecificationIEC61360 = value;
                this.Add(edsnew);
            }
        }

    }
}
