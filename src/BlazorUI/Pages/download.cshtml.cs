using BlazorUI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DownloadModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private BlazorUI.Data.blazorSessionService _bi;
    public DownloadModel(IWebHostEnvironment env)
    {
        _env = env;
    }

    public void setBi(BlazorUI.Data.blazorSessionService bi)
    {
        _bi = bi;
    }

    public Microsoft.AspNetCore.Mvc.IActionResult OnGet(BlazorUI.Data.blazorSessionService bi)
    {
        // _bi = bi;

        // var filePath = Path.Combine(_env.WebRootPath, "files", "file1.xlsx");

        //
        if (!System.IO.Directory.Exists("./temp/"))
            System.IO.Directory.CreateDirectory("./temp/");
        if (_bi?.env != null)
        {
            string idShort = _bi.env.AasEnv.AdministrationShells[0].idShort.ToUpper();
            string fname = System.IO.Path.GetFileName(idShort + ".AASX");
            _bi.env.SaveAs("./temp/" + fname);
            _bi.env.Close();
            _bi.env = null;

            byte[] fileBytes = System.IO.File.ReadAllBytes("./temp/" + fname);

            return File(fileBytes, "application/force-download", fname);
        }
        //

        return null;
    }
}
