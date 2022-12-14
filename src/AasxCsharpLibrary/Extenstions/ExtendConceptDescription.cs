using AasCore.Aas3_0_RC02;
using AasCore.Aas3_0_RC02.HasDataSpecification;
using AasxCompatibilityModels;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendConceptDescription
    {
        #region AasxPackageExplorer

        public static string GetDefaultPreferredName(this ConceptDescription conceptDescription,string defaultLang = null)
        {
            return "" +
                conceptDescription.GetIEC61360()?
                    .preferredName?.GetDefaultString(defaultLang);
        }

        public static void SetIEC61360Spec(this ConceptDescription conceptDescription,
                string[] preferredNames = null,
                string shortName = "",
                string unit = "",
                Reference unitId = null,
                string valueFormat = null,
                string sourceOfDefinition = null,
                string symbol = null,
                string dataType = "",
                string[] definition = null
            )
        {
            var eds = new EmbeddedDataSpecification(new Reference(ReferenceTypes.GlobalReference, new List<Key>()), new AasCore.Aas3_0_RC02.HasDataSpecification.DataSpecificationContent());
            eds.DataSpecification.Keys.Add(
                new Key(KeyTypes.GlobalReference, DataSpecificationIEC61360.GetIdentifier()));
            eds.DataSpecificationContent.DataSpecificationIEC61360 =
                DataSpecificationIEC61360.CreateNew(
                    preferredNames, shortName, unit, unitId, valueFormat, sourceOfDefinition, symbol,
                    dataType, definition);

            conceptDescription.EmbeddedDataSpecification = new HasDataSpecification();
            conceptDescription.EmbeddedDataSpecification.Add(eds);
            conceptDescription.IsCaseOf ??= new List<Reference>();
            conceptDescription.IsCaseOf.Add(new Reference(ReferenceTypes.ModelReference, new List<Key>() { new Key(KeyTypes.ConceptDescription, conceptDescription.Id) }));
        }

        public static DataSpecificationIEC61360 CreateDataSpecWithContentIec61360(this ConceptDescription conceptDescription)
        {
            var eds = EmbeddedDataSpecification.CreateIEC61360WithContent();
            conceptDescription.EmbeddedDataSpecification ??= new HasDataSpecification();
            conceptDescription.EmbeddedDataSpecification.Add(eds);
            return eds.DataSpecificationContent?.DataSpecificationIEC61360;
        }

        public static Tuple<string, string> ToCaptionInfo(this ConceptDescription conceptDescription)
        {
            var caption = "";
            if (!string.IsNullOrEmpty(conceptDescription.IdShort))
                caption = $"\"{conceptDescription.IdShort.Trim()}\"";
            if (conceptDescription.Id != null)
                caption = (caption + " " + conceptDescription.Id).Trim();

            var info = "" + conceptDescription.GetDefaultShortName();

            return Tuple.Create(caption, info);
        }

        public static string GetDefaultShortName(this ConceptDescription conceptDescription, string defaultLang = null)
        {
            return "" +
                    conceptDescription.GetIEC61360()?
                        .shortName?.GetDefaultString(defaultLang);
        }

        public static DataSpecificationIEC61360 GetIEC61360(this ConceptDescription conceptDescription)
        {
            return conceptDescription.EmbeddedDataSpecification?.IEC61360Content;
        }

        public static IEnumerable<Reference> FindAllReferences(this ConceptDescription conceptDescription)
        {
            yield break;
        }

        #endregion
        #region ListOfConceptDescription
        public static ConceptDescription AddConceptDescription(this List<ConceptDescription> conceptDescriptions, ConceptDescription newConceptDescription)
        {
            if(newConceptDescription == null)
            {
                return null;
            }
            if(conceptDescriptions != null)
            {
                var existingCd = conceptDescriptions.Where(c => c.Id == newConceptDescription.Id).First();
                if(existingCd != null)
                {
                    return existingCd;
                }
                else
                {
                    conceptDescriptions.Add(newConceptDescription);
                }
            }

            return newConceptDescription;
        }
        #endregion

        public static void Validate(this ConceptDescription conceptDescription,AasValidationRecordList results)
        {
            // access
            if (results == null)
                return;

            // check CD itself
            //Handled by BaseValidation Method
            //conceptDescription.Validate(results);

            // check IEC61360 spec

            //TODO:jtikekar Temporarily Removed
            //var eds61360 = this.IEC61360DataSpec;
            //if (eds61360 != null)
            //{
            //    // check data spec
            //    if (eds61360.dataSpecification == null ||
            //        !(eds61360.dataSpecification.MatchesExactlyOneKey(DataSpecificationIEC61360.GetKey())))
            //        results.Add(new AasValidationRecord(
            //            AasValidationSeverity.SpecViolation, this,
            //            "HasDataSpecification: data specification content set to IEC61360, but no " +
            //            "data specification reference set!",
            //            () =>
            //            {
            //                eds61360.dataSpecification = new DataSpecificationRef(
            //                    new Reference(
            //                        DataSpecificationIEC61360.GetKey()));
            //            }));

            //    // validate content
            //    if (eds61360.dataSpecificationContent?.dataSpecificationIEC61360 == null)
            //    {
            //        results.Add(new AasValidationRecord(
            //            AasValidationSeverity.SpecViolation, this,
            //            "HasDataSpecification: data specification reference set to IEC61360, but no " +
            //            "data specification content set!",
            //            () =>
            //            {
            //                eds61360.dataSpecificationContent = new DataSpecificationContent();
            //                eds61360.dataSpecificationContent.dataSpecificationIEC61360 =
            //                new DataSpecificationIEC61360();
            //            }));
            //    }
            //    else
            //    {
            //        // validate
            //        eds61360.dataSpecificationContent.dataSpecificationIEC61360.Validate(results, this);
            //    }
        }

        public static Key GetSingleKey(this ConceptDescription conceptDescription)
        {
            return new Key(KeyTypes.ConceptDescription, conceptDescription.Id);
        }
        public static ConceptDescription ConvertFromV10(this ConceptDescription conceptDescription, AasxCompatibilityModels.AdminShellV10.ConceptDescription sourceConceptDescription)
        {
            if (sourceConceptDescription == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceConceptDescription.idShort))
            {
                conceptDescription.IdShort = "";
            }
            else
            {
                conceptDescription.IdShort = sourceConceptDescription.idShort;
            }

            if (sourceConceptDescription.description != null)
            {
                conceptDescription.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceConceptDescription.description);
            }

            if (sourceConceptDescription.administration != null)
            {
                conceptDescription.Administration = new AdministrativeInformation(version: sourceConceptDescription.administration.version, revision: sourceConceptDescription.administration.revision);
            }

            if (sourceConceptDescription.IsCaseOf != null && sourceConceptDescription.IsCaseOf.Count != 0)
            {
                if (conceptDescription.IsCaseOf == null)
                {
                    conceptDescription.IsCaseOf = new List<Reference>();
                }
                foreach (var caseOf in sourceConceptDescription.IsCaseOf)
                {
                    conceptDescription.IsCaseOf.Add(ExtensionsUtil.ConvertReferenceFromV10(caseOf, ReferenceTypes.ModelReference));
                }
            }

            return conceptDescription;
        }

        public static ConceptDescription ConvertFromV20(this ConceptDescription conceptDescription, AasxCompatibilityModels.AdminShellV20.ConceptDescription sourceConceptDescription)
        {
            if (sourceConceptDescription == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceConceptDescription.idShort))
            {
                conceptDescription.IdShort = "";
            }
            else
            {
                conceptDescription.IdShort = sourceConceptDescription.idShort;
            }

            if (sourceConceptDescription.description != null)
            {
                conceptDescription.Description = ExtensionsUtil.ConvertDescriptionFromV20(sourceConceptDescription.description);
            }

            if (sourceConceptDescription.administration != null)
            {
                conceptDescription.Administration = new AdministrativeInformation(version: sourceConceptDescription.administration.version, revision: sourceConceptDescription.administration.revision);
            }

            if (sourceConceptDescription.IsCaseOf != null && sourceConceptDescription.IsCaseOf.Count != 0)
            {
                if (conceptDescription.IsCaseOf == null)
                {
                    conceptDescription.IsCaseOf = new List<Reference>();
                }
                foreach (var caseOf in sourceConceptDescription.IsCaseOf)
                {
                    conceptDescription.IsCaseOf.Add(ExtensionsUtil.ConvertReferenceFromV20(caseOf, ReferenceTypes.ModelReference));
                }
            }

            return conceptDescription;
        }

        public static Reference GetCdReference(this ConceptDescription conceptDescription)
        {
            var key = new Key(KeyTypes.ConceptDescription, conceptDescription.Id);
            return new Reference(ReferenceTypes.ModelReference, new List<Key> { key });
        }
    }
}
