/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
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
