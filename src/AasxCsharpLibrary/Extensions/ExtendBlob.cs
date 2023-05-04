using System.Text;

namespace Extensions
{
    public static class ExtendBlob
    {
        public static void Set(this Blob blob,
            string contentType = "", byte[]? value = null)
        {
            blob.ContentType = contentType;
            blob.Value = value;
        }

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

        public static Blob UpdateFrom(this Blob elem, ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((ISubmodelElement)elem).UpdateFrom(source);

            if (source is Property srcProp)
            {
                if (srcProp.Value != null)
                    elem.Value = Encoding.Default.GetBytes(srcProp.Value);
            }

            if (source is AasCore.Aas3_0.Range srcRng)
            {
                if (srcRng.Min != null)
                    elem.Value = Encoding.Default.GetBytes(srcRng.Min);
            }

            if (source is MultiLanguageProperty srcMlp)
            {
                var s = srcMlp.Value?.GetDefaultString();
                if (s != null)
                    elem.Value = Encoding.Default.GetBytes(s);
            }

            if (source is File srcFile)
            {
                if (srcFile.Value != null)
                    elem.Value = Encoding.Default.GetBytes(srcFile.Value);
            }

            return elem;
        }

    }
}
