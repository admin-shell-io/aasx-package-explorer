﻿using AdminShellNS;
using Newtonsoft.Json.Linq;

namespace AasxSchemaExport
{
    public class SubmodelTemplateJsonSchemaExporterV20 : ISchemaExporter
    {
        public string ExportSchema(AdminShellV20.Submodel submodel)
        {
            var root = new JObject();

            root["$schema"] = "https://json-schema.org/draft/2019-09/schema";

            return root.ToString();

        }
    }
}
