/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using Aas = AasCore.Aas3_0;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel Basic model for the modeling of time series data (ZVEI) v1.0
    /// </summary>
    public class ZveiTimeSeriesDataV10 : AasxDefinitionBase
    {
        public static ZveiTimeSeriesDataV10 Static = new ZveiTimeSeriesDataV10();

        public Aas.Submodel
            SM_TimeSeriesData;

        public Aas.ConceptDescription
            CD_TimeSeries,
            CD_Name,
            CD_Description,
            CD_TimeSeriesSegment,
            CD_RecordCount,
            CD_StartTime,
            CD_EndTime,
            CD_SamplingInterval,
            CD_SamplingRate,
            CD_TimeSeriesRecord,
            CD_RecordId,
            CD_UtcTime,
            CD_TaiTime,
            CD_Time,
            CD_TimeDuration,
            CD_TimeSeriesVariable,
            CD_ValueArray,
            CD_ExternalDataFile;

        public ZveiTimeSeriesDataV10()
        {
            // info
            this.DomainInfo = "Basic model for the modeling of time series data (ZVEI) V1.0";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "ZveiTimeSeriesDataV10.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(ZveiTimeSeriesDataV10), useFieldNames: true);
        }
    }
}
