using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendBlob
    {
        public static Blob ConvertFromV10(this Blob blob, AasxCompatibilityModels.AdminShellV10.Blob sourceBlob)
        {
            blob.ContentType = sourceBlob.mimeType;
            blob.Value = Encoding.ASCII.GetBytes(sourceBlob.value);
            return blob;
        }

        public static Blob ConvertFromV20(this Blob blob, AasxCompatibilityModels.AdminShellV20.Blob sourceBlob)
        {
            blob.ContentType = sourceBlob.mimeType;
            blob.Value = Encoding.ASCII.GetBytes(sourceBlob.value);
            return blob;
        }
    }
}
