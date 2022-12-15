using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendFile
    {
        public static string ValueAsText(this File file)
        {
            return "" + file.Value;
        }

        public static File ConvertFromV10(this File file, AasxCompatibilityModels.AdminShellV10.File sourceFile)
        {
            file.ContentType = sourceFile.mimeType;
            file.Value = sourceFile.value;
            return file;
        }
        public static File ConvertFromV20(this File file, AasxCompatibilityModels.AdminShellV20.File sourceFile)
        {
            file.ContentType = sourceFile.mimeType;
            file.Value = sourceFile.value;
            return file;
        }


    }
}
