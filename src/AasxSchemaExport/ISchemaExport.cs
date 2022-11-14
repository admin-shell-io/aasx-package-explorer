using AdminShellNS;

namespace AasxSchemaExport
{
    internal interface ISchemaExport
    {
        string ExportSchema(AdminShell.Submodel submodel);
    }
}
