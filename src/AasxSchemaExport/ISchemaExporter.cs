using AdminShellNS;

namespace AasxSchemaExport
{
    public interface ISchemaExporter
    {
        string ExportSchema(AdminShellV20.Submodel submodel);
    }
}
