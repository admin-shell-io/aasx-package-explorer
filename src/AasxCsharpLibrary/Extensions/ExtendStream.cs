using System.IO;

namespace Extensions
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
