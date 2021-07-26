using BlazorUI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DownloadModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public DownloadModel(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Microsoft.AspNetCore.Mvc.IActionResult OnGet()
    {
        // var filePath = Path.Combine(_env.WebRootPath, "files", "file1.xlsx");

        if (!System.IO.Directory.Exists("./temp/"))
            System.IO.Directory.CreateDirectory("./temp/");
        if (Program.env != null)
        {
            string idShort = Program.env.AasEnv.AdministrationShells[0].idShort.ToUpper();
            string fname = System.IO.Path.GetFileName(idShort + ".AASX");
            Program.env.SaveAs("./temp/" + fname);
            Program.env.Close();
            Program.env = null;

            byte[] fileBytes = System.IO.File.ReadAllBytes("./temp/" + fname);

            return File(fileBytes, "application/force-download", fname);
        }
        return null;
    }
}
