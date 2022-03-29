/* 
 * DotAAS Part 2 | HTTP/REST | Entire Interface Collection
 *
 * The entire interface collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: Final-Draft
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;

namespace IO.Swagger.Model
{
    /// <summary>
    /// AssetAdministrationShell
    /// </summary>
    [DataContract]
    public partial class AssetAdministrationShell : Identifiable, IEquatable<AssetAdministrationShell>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetAdministrationShell" /> class.
        /// </summary>
        /// <param name="embeddedDataSpecifications">embeddedDataSpecifications.</param>
        /// <param name="assetInformation">assetInformation (required).</param>
        /// <param name="derivedFrom">derivedFrom.</param>
        /// <param name="security">security.</param>
        /// <param name="submodels">submodels.</param>
        /// <param name="views">views.</param>
        public AssetAdministrationShell(List<EmbeddedDataSpecification> embeddedDataSpecifications = default(List<EmbeddedDataSpecification>), AssetInformation assetInformation = default(AssetInformation), Reference derivedFrom = default(Reference), Security security = default(Security), List<Reference> submodels = default(List<Reference>), List<View> views = default(List<View>), List<EmbeddedDataSpecification> embeddedDataSpecifications2 = default(List<EmbeddedDataSpecification>), AdministrativeInformation administration = default(AdministrativeInformation), string identification = default(string)) : base(administration, identification)
        {
            // to ensure "assetInformation" is required (not null)
            if (assetInformation == null)
            {
                throw new InvalidDataException("assetInformation is a required property for AssetAdministrationShell and cannot be null");
            }
            else
            {
                this.AssetInformation = assetInformation;
            }
            //this.EmbeddedDataSpecifications = embeddedDataSpecifications;
            this.DerivedFrom = derivedFrom;
            this.Security = security;
            this.Submodels = submodels;
            this.Views = views;
        }

        /// <summary>
        /// Gets or Sets EmbeddedDataSpecifications
        /// </summary>
        [DataMember(Name = "embeddedDataSpecifications", EmitDefaultValue = false)]
        public List<EmbeddedDataSpecification> EmbeddedDataSpecifications { get; set; }

        /// <summary>
        /// Gets or Sets AssetInformation
        /// </summary>
        [DataMember(Name = "assetInformation", EmitDefaultValue = false)]
        public AssetInformation AssetInformation { get; set; }

        /// <summary>
        /// Gets or Sets DerivedFrom
        /// </summary>
        [DataMember(Name = "derivedFrom", EmitDefaultValue = false)]
        public Reference DerivedFrom { get; set; }

        /// <summary>
        /// Gets or Sets Security
        /// </summary>
        [DataMember(Name = "security", EmitDefaultValue = false)]
        public Security Security { get; set; }

        /// <summary>
        /// Gets or Sets Submodels
        /// </summary>
        [DataMember(Name = "submodels", EmitDefaultValue = false)]
        public List<Reference> Submodels { get; set; }

        /// <summary>
        /// Gets or Sets Views
        /// </summary>
        [DataMember(Name = "views", EmitDefaultValue = false)]
        public List<View> Views { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AssetAdministrationShell {\n");
            sb.Append("  ").Append(base.ToString().Replace("\n", "\n  ")).Append("\n");
            sb.Append("  EmbeddedDataSpecifications: ").Append(EmbeddedDataSpecifications).Append("\n");
            sb.Append("  AssetInformation: ").Append(AssetInformation).Append("\n");
            sb.Append("  DerivedFrom: ").Append(DerivedFrom).Append("\n");
            sb.Append("  Security: ").Append(Security).Append("\n");
            sb.Append("  Submodels: ").Append(Submodels).Append("\n");
            sb.Append("  Views: ").Append(Views).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public override string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as AssetAdministrationShell);
        }

        /// <summary>
        /// Returns true if AssetAdministrationShell instances are equal
        /// </summary>
        /// <param name="input">Instance of AssetAdministrationShell to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AssetAdministrationShell input)
        {
            if (input == null)
                return false;

            return base.Equals(input) &&
                (
                    this.EmbeddedDataSpecifications == input.EmbeddedDataSpecifications ||
                    this.EmbeddedDataSpecifications != null &&
                    input.EmbeddedDataSpecifications != null &&
                    this.EmbeddedDataSpecifications.SequenceEqual(input.EmbeddedDataSpecifications)
                ) && base.Equals(input) &&
                (
                    this.AssetInformation == input.AssetInformation ||
                    (this.AssetInformation != null &&
                    this.AssetInformation.Equals(input.AssetInformation))
                ) && base.Equals(input) &&
                (
                    this.DerivedFrom == input.DerivedFrom ||
                    (this.DerivedFrom != null &&
                    this.DerivedFrom.Equals(input.DerivedFrom))
                ) && base.Equals(input) &&
                (
                    this.Security == input.Security ||
                    (this.Security != null &&
                    this.Security.Equals(input.Security))
                ) && base.Equals(input) &&
                (
                    this.Submodels == input.Submodels ||
                    this.Submodels != null &&
                    input.Submodels != null &&
                    this.Submodels.SequenceEqual(input.Submodels)
                ) && base.Equals(input) &&
                (
                    this.Views == input.Views ||
                    this.Views != null &&
                    input.Views != null &&
                    this.Views.SequenceEqual(input.Views)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = base.GetHashCode();
                if (this.EmbeddedDataSpecifications != null)
                    hashCode = hashCode * 59 + this.EmbeddedDataSpecifications.GetHashCode();
                if (this.AssetInformation != null)
                    hashCode = hashCode * 59 + this.AssetInformation.GetHashCode();
                if (this.DerivedFrom != null)
                    hashCode = hashCode * 59 + this.DerivedFrom.GetHashCode();
                if (this.Security != null)
                    hashCode = hashCode * 59 + this.Security.GetHashCode();
                if (this.Submodels != null)
                    hashCode = hashCode * 59 + this.Submodels.GetHashCode();
                if (this.Views != null)
                    hashCode = hashCode * 59 + this.Views.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
