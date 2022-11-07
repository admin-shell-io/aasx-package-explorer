using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendStream
    {
        public static byte[] ToByteArray(this Stream stream)
        {
            using (stream)
            {
                using MemoryStream memStream = new();
                stream.CopyTo(memStream);
                return memStream.ToArray();
            }
        }
    }
}
