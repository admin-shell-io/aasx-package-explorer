using AdminShellNS;

namespace AasxSchemaExport
{
    public interface ISchemaExporter
    {
        string ExportSchema(AdminShell.Submodel submodel);
    }
}
