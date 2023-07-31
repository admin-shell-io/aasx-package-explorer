/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System.Collections.Generic;

namespace Extensions
{
    // TODO (Jui, 2022-12-21): I do not know, if to put the List<> extension here or in a separate file
    public static class ExtendListOfEmbeddedDataSpecification
    {
        public static IEmbeddedDataSpecification FindFirstIEC61360Spec(this List<IEmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationIec61360
                    || eds?.DataSpecification?.MatchesExactlyOneKey(
                        ExtendIDataSpecificationContent.GetKeyForIec61360()) == true)
                    return eds;
            return null;
        }

        public static DataSpecificationIec61360 GetIEC61360Content(this List<IEmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationIec61360 dsiec)
                    return dsiec;
            return null;
        }

        //TODO (jtikekar, 0000-00-00): DataSpecificationPhysicalUnit
#if SupportDataSpecificationPhysicalUnit
        public static DataSpecificationPhysicalUnit GetPhysicalUnitContent(this List<EmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationPhysicalUnit dspu)
                    return dspu;
            return null;
        } 
#endif
    }

    public static class ExtendEmbeddedDataSpecification
    {
        public static EmbeddedDataSpecification ConvertFromV20(this EmbeddedDataSpecification embeddedDataSpecification, AasxCompatibilityModels.AdminShellV20.EmbeddedDataSpecification sourceEmbeddedSpec)
        {
            if (sourceEmbeddedSpec != null)
            {
                embeddedDataSpecification.DataSpecification = ExtensionsUtil.ConvertReferenceFromV20(sourceEmbeddedSpec.dataSpecification, ReferenceTypes.ExternalReference);

                // TODO (MIHO, 2022-19-12): check again, see questions
                var oldid = new[] {
                    "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/2/0",
					"http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360",
					"www.admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360"
				};
                var newid = "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/3/0";

                // map all "usable" old ids to new one ..
                foreach (var oi in oldid)
                    if (sourceEmbeddedSpec.dataSpecification?.Matches("", false, "IRI", oi,
                        AasxCompatibilityModels.AdminShellV20.Key.MatchMode.Identification) == true)
                    {
                        embeddedDataSpecification.DataSpecification.Keys[0].Value = newid;
                    }
            }

            if (sourceEmbeddedSpec.dataSpecificationContent?.dataSpecificationIEC61360 != null)
            {
                embeddedDataSpecification.DataSpecificationContent =
                    new DataSpecificationIec61360(null).ConvertFromV20(
                        sourceEmbeddedSpec.dataSpecificationContent.dataSpecificationIEC61360);
            }
            return embeddedDataSpecification;
        }

        public static EmbeddedDataSpecification CreateIec61360WithContent(DataSpecificationIec61360 content = null)
        {
            if (content == null)
                content = new DataSpecificationIec61360(
                    new List<ILangStringPreferredNameTypeIec61360>());

            var res = new EmbeddedDataSpecification(
                new Reference(ReferenceTypes.ExternalReference,
                    new List<IKey>(new[] { ExtendIDataSpecificationContent.GetKeyForIec61360() })),
                content);
            return res;
        }

        public static bool FixReferenceWrtContent(this IEmbeddedDataSpecification eds)
        {
            // does content tell something?
            var ctc = ExtendIDataSpecificationContent.GuessContentTypeFor(eds?.DataSpecificationContent);
            var ctr = ExtendIDataSpecificationContent.GuessContentTypeFor(eds?.DataSpecification);

            if (ctc == ExtendIDataSpecificationContent.ContentTypes.NoInfo)
                return false;

            if (ctr == ctc)
                return false;

            // ok, fix
            eds.DataSpecification = new Reference(ReferenceTypes.ExternalReference,
                new List<IKey> { ExtendIDataSpecificationContent.GetKeyFor(ctc) });
            return true;
        }
    }
}
